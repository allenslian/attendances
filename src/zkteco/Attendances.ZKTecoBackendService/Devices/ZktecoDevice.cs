using Attendances.ZKTecoBackendService.Models;
using System.Threading.Tasks;
using Topshelf.Logging;
using zkemkeeper;
using System.Timers;
using System.Threading;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using Attendances.ZKTecoBackendService.Utils;
using Attendances.ZKTecoBackendService.Events;

namespace Attendances.ZKTecoBackendService.Devices
{
    public class ZktecoDevice
    {
        /// <summary>
        /// 0: disconnected, 1: connected.
        /// </summary>
        private int _connected = 0;

        /// <summary>
        /// Timer is running?
        /// 0: no, 1: yes.
        /// </summary>
        private int _running = 0;

        /// <summary>
        /// If failing to connect to the device, 
        /// it will reconnect several times.
        /// </summary>
        private int RetryTimes { get; set; }

        /// <summary>
        /// Grab logs from device periodically
        /// </summary>
        private System.Timers.Timer Timer { get; set; }

        /// <summary>
        /// Log Writer
        /// </summary>
        private LogWriter Logger { get; set; }

        /// <summary>
        /// One signal
        /// </summary>
        private ManualResetEvent Signal { get; set; }

        /// <summary>
        /// One event hub
        /// </summary>
        private EventHub Hub { get; set; }

        #region Constructors

        private ZktecoDevice(CZKEMClass device)
        {
            Device = device;            

            Timer = new System.Timers.Timer(1000);
            Timer.Elapsed += OnReadAttendances;

            Signal = new ManualResetEvent(false);

            Logger = HostLogger.Get<ZktecoDevice>();
        }        

        public ZktecoDevice(CZKEMClass device, DeviceInfo info, EventHub hub) : this(device)
        {
            Hub = hub;

            IP = info.IP;
            Port = info.Port;
            DeviceName = info.DeviceName;
            DeviceType = info.Type;
        }

        #endregion

        public CZKEMClass Device { get; private set; }

        public string IP { get; private set; }

        public int Port { get; private set; }
        /// <summary>
        /// Device's machine number, default is 1.
        /// </summary>
        public int MachineNumber { get { return 1; } }

        public string DeviceName { get; private set; }

        public DeviceType DeviceType { get; private set; }

        public void StartAsync()
        {
            Task.Run(() =>
            {
                Logger.Debug("ZKtecoDevice StartAsync is starting...");
                while (!Connnect())
                {
                    if (RetryTimes >= GlobalConfig.MaxRetryTimes)
                    {
                        // ToDo: send one email to administrator later.
                        Logger.ErrorFormat("Unable to connect the device({ip}:{port}), ErrorCode({error}).",
                            IP, Port, GetLastError());
                        return;
                    }

                    Thread.SpinWait(10000);
                    RetryTimes++;
                    Logger.DebugFormat("Reconnect the device({ip}:{port}) at {times} times.", IP, Port, RetryTimes);
                }

                Logger.Debug("ZKtecoDevice: RegisterEvents.");
                RegisterEvents();

                Logger.Debug("ZKtecoDevice: StartRealTimeMonitor.");
                StartRealTimeMonitor();

                Logger.Debug("ZKtecoDevice: Signal.WaitOne.");
                Signal.WaitOne();

                Logger.Debug("ZKtecoDevice StartAsync ends.");
            });            
        }

        public void Stop()
        {
            Logger.Debug("ZKtecoDevice is stoping...");

            Logger.Debug("ZKtecoDevice: Signal.Set");
            Signal.Set();

            Disconnect();
            Logger.Debug("ZKtecoDevice stops.");
        }

        [HandleProcessCorruptedStateExceptions]
        private bool Connnect()
        {
            try
            {
                return Device.Connect_Net(IP, Port);
            }
            catch
            {
                return false;
            }
        }

        private void Disconnect()
        {
            StopRealTimeMonitor();

            Interlocked.CompareExchange(ref _connected, 0, 1);
            UnregisterEvents();
            Device.Disconnect();
            Device = null;
        }

        /// <summary>
        /// Get last error. it is used to check connection status of the device.
        /// </summary>
        /// <returns></returns>
        private int GetLastError()
        {
            int errorCode = 0;
            Device.GetLastError(ref errorCode);
            return errorCode;
        }


        private void RegisterEvents()
        {
            Interlocked.CompareExchange(ref _connected, 1, 0);
            if (Device.RegEvent(MachineNumber, 65535))
            {
                Device.OnAttTransactionEx += OnAttTransactionEx;
            }
        }

        private void UnregisterEvents()
        {
            Device.OnAttTransactionEx -= OnAttTransactionEx;
        }

        private void StartRealTimeMonitor()
        {
            Timer.Start();
        }

        private void StopRealTimeMonitor()
        {
            Timer.Elapsed -= OnReadAttendances;
            Timer.Stop();
            Timer = null;
        }
        
        #region Device's events
        /// <summary>
        /// If your fingerprint(or your card) passes the verification,this event will be triggered
        /// </summary>
        /// <param name="enrollNumber">UserID of a user</param>
        /// <param name="isInValid">Whether a record is valid. 1: Not valid. 0: Valid.</param>
        /// <param name="attState"></param>
        /// <param name="verifyMethod"></param>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        /// <param name="workCode">
        /// work code returned during verification. 
        /// Return 0 when the device does not support work code.
        /// </param>
        private void OnAttTransactionEx(string enrollNumber, int isInValid, int attState, int verifyMethod,
            int year, int month, int day, int hour, int minute, int second, int workCode)
        {
            var watch = Stopwatch.StartNew();

            var log = new AttendanceLog(enrollNumber, attState, verifyMethod,
                year, month, day, hour, minute, second, workCode, MachineNumber, DeviceName, DeviceType);

            Hub.PublishAsync(new EventMessage(EventType.AttTransactionEx, log));

            watch.Stop();

            Logger.InfoFormat("Time:{time}, OnAttTransactionEx:[{@AttendanceLog}], IsInValid[{IsInValid}].",
                watch.ElapsedMilliseconds, log, isInValid);
        }

        /// <summary>
        /// This event is triggered when a fingerprint is placed on the fingerprint sensor of the device.
        /// </summary>
        private void OnFinger()
        {
            Logger.Debug("OnFinger.");
        }

        /// <summary>
        /// This event is triggered when a new user is successfully enrolled.
        /// </summary>
        /// <param name="enrollNumber">UserID of the newly enrolled user.</param>
        private void OnNewUser(int enrollNumber)
        {
            var number = enrollNumber.ToString("{0:D1}");
            Hub.PublishAsync(new EventMessage(EventType.NewUser, 
                new ArgumentItem(number, new ArgumentItem.ArgumentValuePair("EnrollNumber", number))));
            Logger.DebugFormat("OnNewUser:EnrollNumber[{EnrollNumber}].", number);
        }

        /// <summary>
        /// This event is triggered when a fingerprint is registered
        /// </summary>
        /// <param name="enrollNumber">User ID of the fingerprint being registered</param>
        /// <param name="fingerIndex">Index of the current fingerprint</param>
        /// <param name="actionResult">Operation result. Return 0 if the operation is successful, or return a value greater than 0.</param>
        /// <param name="templateLength">Length of the fingerprint template</param>
        private void OnEnrollFingerEx(string enrollNumber, int fingerIndex, int actionResult, int templateLength)
        {
            Hub.PublishAsync(new EventMessage(EventType.EnrollFingerEx, 
                new ArgumentItem(enrollNumber, 
                new ArgumentItem.ArgumentValuePair("EnrollNumber", enrollNumber),
                new ArgumentItem.ArgumentValuePair("FingerIndex", fingerIndex),
                new ArgumentItem.ArgumentValuePair("ActionResult", actionResult),
                new ArgumentItem.ArgumentValuePair("TemplateLength", templateLength))));

            Logger.DebugFormat("OnEnrollFingerEx:EnrollNumber[{EnrollNumber}], FingerIndex[{FingerIndex}], ActionResult[{ActionResult}], TemplateLength[{TemplateLength}].",
                enrollNumber, fingerIndex, actionResult, templateLength);
        }

        /// <summary>
        /// This event is triggered when a user is verified.
        /// </summary>
        /// <param name="userID">When verification succeeds, UserID indicates the ID of the user. Return the card number if card verification is successful, or return -1.</param>
        private void OnVerify(int userID)
        {
            Logger.DebugFormat("OnVerify:UserID[{UserID}].", userID);
        }

        /// <summary>
        /// This event is triggered when a user places a finger and the device registers the fingerprint.
        /// </summary>
        /// <param name="score"></param>
        private void OnFingerFeature(int score)
        {
            Logger.DebugFormat("OnFingerFeature:Score[{Score}]", score);
        }

        /// <summary>
        /// This event is triggered when the device opens the door.
        /// </summary>
        /// <param name="eventType">
        /// Open door type 
        /// 4: The door is not closed. 53: Exit button. 5: The door is closed. 1: The door is opened unexpectedly.
        /// </param>
        private void OnDoor(int eventType)
        {
            Logger.DebugFormat("OnDoor:EventType[{EventType}]", eventType);
        }

        /// <summary>
        /// This event is triggered when the device reports an alarm.
        /// </summary>
        /// <param name="alarmType">
        /// Type of an alarm. 
        /// 55: Tamper alarm. 58: False alarm. 32: Threatened alarm. 34: Anti-pass back alarm.
        /// </param>
        /// <param name="enrollNumber">
        /// User ID. 
        /// The value is 0 when a tamper alarm, false alarm, or threatened alarm is given.The value is the user ID when
        /// other threatened alarm or anti-pass back alarm is given.
        /// </param>
        /// <param name="verified">
        /// Whether to verify 
        /// The value is 0 when a tamper alarm, false alarm, or threatened alarm is given.The value is 1 when other alarms are given.
        /// </param>
        private void OnAlarm(int alarmType, int enrollNumber, int verified)
        {
            Logger.DebugFormat("OnAlarm:AlarmType[{AlarmType}],EnrollNumber[{EnrollNumber}],Verified[{Verified}]", alarmType, enrollNumber, verified);
        }

        /// <summary>
        /// This event is triggered when a card is swiped.
        /// </summary>
        /// <param name="cardNumber">
        /// Number of a card that can be an ID card or an HID card. If the card is a Mifare card, 
        /// the event is triggered only when the card is used as an ID card.
        /// </param>
        private void OnHIDNum(int cardNumber)
        {
            Logger.DebugFormat("OnHIDNum: CardNumber[{CardNumber}]", cardNumber);
        }

        /// <summary>
        /// This event is triggered when the device writes a card.
        /// </summary>
        /// <param name="enrollNumber">ID of the user to be written into a card</param>
        /// <param name="actionResult">Result of writing user information into a card. 0: Success. Other values:Failure.</param>
        /// <param name="length">Size of the data to be written into a card</param>
        private void OnWriteCard(int enrollNumber, int actionResult, int length)
        {
            Logger.DebugFormat("OnWriteCard: EnrollNumber[{EnrollNumber}], ActionResult[{ActionResult}], Length[{Length}].", enrollNumber);
        }

        /// <summary>
        /// This event is triggered when a Mifare card is emptied.
        /// </summary>
        /// <param name="actionResult">
        /// Result of emptying a card. 0: Success. Other values: Failure.
        /// </param>
        private void OnEmptyCard(int actionResult)
        {
            Logger.DebugFormat("OnEmptyCard: ActionResult[{ActionResult}]", actionResult);
        }

        /// <summary>
        /// When you have deleted one one fingerprint template,this event will be triggered.
        /// </summary>
        /// <param name="enrollNumber">User ID of the fingerprint being registered</param>
        /// <param name="fingerIndex">Index of the current fingerprint</param>
        private void OnDeleteTemplate(int enrollNumber, int fingerIndex)
        {
            Logger.DebugFormat("OnDeleteTemplate:EnrollNumber[{EnrollNumber}], FingerIndex[{FingerIndex}].", enrollNumber, fingerIndex);
        }

        #endregion

        /// <summary>
        /// Read attendance logs from device at real-time.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnReadAttendances(object sender, ElapsedEventArgs e)
        {
            if (Interlocked.CompareExchange(ref _running, 1, 0) == 1)
            {
                // running.
                Logger.DebugFormat("OnReadAttendances({ip}) is running under thread({id}).", 
                    IP, Thread.CurrentThread.ManagedThreadId);
                return;
            }

            var target = (System.Timers.Timer)sender;
            if (!target.Enabled)
            {
                Logger.DebugFormat("Device's timer({ip}) has stopped.", IP);
                return;
            }

            if (_connected == 0)
            {
                Logger.DebugFormat("Device's connection({ip}) has disconnected.", IP);
                return;
            }

            Logger.DebugFormat("Read attendances({ip}) starts under thread({id}).", 
                IP, Thread.CurrentThread.ManagedThreadId);

            Timer.Stop();           

            var watch = new Stopwatch();
            watch.Start();
            if (Device.ReadRTLog(MachineNumber))
            {
                Logger.DebugFormat("Invoking ReadRTLog from device({ip}).", IP);
                while (Device.GetRTLog(MachineNumber))
                {
                    ;
                }
                Logger.Debug("Invoking GetRTLog to complete storing in the memory.");
            }
            watch.Stop();
            Logger.InfoFormat("ReadRTLog consumes {ms} ms.", watch.ElapsedMilliseconds);

            Interlocked.CompareExchange(ref _running, 0, 1);
            // restart timer.
            Timer.Start();
        }
    }
}
