using Attendances.ZKTecoBackendService.Connectors;
using Attendances.ZKTecoBackendService.Interfaces;
using Attendances.ZKTecoBackendService.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Topshelf.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Attendances.ZKTecoBackendService.Events
{
    public class EventHub
    {
        /// <summary>
        /// Subscribers
        /// </summary>
        private ConcurrentDictionary<EventType, List<IEventHandler>> Subscribers { get; set; }

        /// <summary>
        /// Database helper
        /// </summary>
        private SqliteConnector Database { get; set; }

        /// <summary>
        /// Inner timer is used for checking queue.
        /// </summary>
        private System.Timers.Timer Timer { get; set; }

        /// <summary>
        /// 
        /// </summary>
        private LogWriter Logger { get; set; }

        /// <summary>
        /// running flag, 0: not run, 1: running.
        /// </summary>
        private int _running = 0;

        public EventHub(SqliteConnector db)
        {
            Database = db;

            Subscribers = new ConcurrentDictionary<EventType, List<IEventHandler>>();
            Logger = HostLogger.Get<EventHub>();
            
            Timer = new System.Timers.Timer(1000);
            Timer.Elapsed += OnHandleEventMessages;
            Timer.Start();
        }        

        public void PublishAsync(EventMessage msg)
        {
            Task.Run(() => {
                if (msg == null)
                {
                    return;
                }

                try
                {
                    Database.Execute(
                        "INSERT INTO queue(id, refer_id, message, event_type, create_at) VALUES(@id, @refer_id, @message, @event_type, @create_at);",
                        new Dictionary<string, object> {
                            { "@id", msg.Id },
                            { "@refer_id", msg.ReferenceId },
                            { "@message", msg.ToString() },
                            { "@event_type", (int)msg.Kind },
                            { "@create_at", msg.OccurredOn }
                        });                    
                }
                catch (Exception ex)
                {
                    Logger.ErrorFormat("PublishAsync error: {@error}.", ex);
                }                
            });                      
        }

        public void Subscribe(EventType kind, IEventHandler handler)
        {
            if (handler == null || string.IsNullOrWhiteSpace(handler.HandlerKey))
            {
                throw new ArgumentNullException("handler");
            }

            Subscribers.AddOrUpdate(kind, new List<IEventHandler>(10), (key, list) =>
            {
                var index = list.FindIndex(m => m == handler);
                if (index == -1)
                {
                    list.Add(handler);
                }
                return list;
            });
        }

        private void OnHandleEventMessages(object sender, System.Timers.ElapsedEventArgs e)
        {
            Logger.DebugFormat("OnHandleEventMessages starts on the thread({threadid}).", Thread.CurrentThread.ManagedThreadId);

            if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
            {
                Logger.DebugFormat("OnHandleEventMessages is running. Current Thread[{thread}]", Thread.CurrentThread.ManagedThreadId);
                return;
            }

            var target = (System.Timers.Timer)sender;
            if (target.Enabled == false)
            {
                Logger.Debug("EventHub's timer has stopped.");
                return;
            }

            Logger.Info("EventHub's timer stops.");
            target.Stop();

            var messages = GetPendingMessages();
            foreach (var msg in messages)
            {
                List<IEventHandler> handlers;
                if (Subscribers.TryGetValue(msg.Kind, out handlers))
                {
                    foreach (var h in handlers)
                    {
                        try
                        {
                            h.Handle(msg);
                        }
                        catch(Exception ex)
                        {
                            Logger.ErrorFormat("Invoking handler error:{@ex}.", ex);
                            var message = new EventMessage(EventType.Failed,
                                new ArgumentItem(h.HandlerKey, new ArgumentItem.ArgumentValuePair("Message", msg)));
                            PublishAsync(message);
                            Logger.DebugFormat("Failed message: {@msg}.", message);
                            continue;
                        }
                    }
                    Logger.DebugFormat("Destroy message({id}).", msg.Id);
                    Destroy(msg.Id);
                }
            }

            Interlocked.CompareExchange(ref _running, 0, 1);

            Logger.Info("EventHub's timer starts again.");
            target.Start();

            Logger.DebugFormat("OnHandleEventMessages ends on the thread({threadid}).", Thread.CurrentThread.ManagedThreadId);
        }

        /// <summary>
        /// Get all the pending messages
        /// </summary>
        /// <returns></returns>
        private List<EventMessage> GetPendingMessages()
        {
            var results = new List<EventMessage>();
            var reader = Database.QuerySet("SELECT id, event_type, refer_id, message, create_at FROM queue;");
            while (reader.Read())
            {
                results.Add(new EventMessage(
                    reader.GetString(0),
                    (EventType)reader.GetInt32(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetDateTime(4)));
            }
            return results;
        }

        /// <summary>
        /// Destroy the event message after it is handled.
        /// </summary>
        /// <param name="msgId"></param>
        private void Destroy(string msgId)
        {
            if (string.IsNullOrWhiteSpace(msgId))
            {
                return;
            }

            Database.Execute("DELETE FROM queue WHERE id=@id;", 
                new Dictionary<string, object>
                {
                    {"@id", msgId}
                });
        }

        public void Dispose()
        {
            Timer.Stop();
            Timer.Elapsed -= OnHandleEventMessages;
            Timer = null;

            Subscribers.Clear();
            Subscribers = null;
        }
    }
}
