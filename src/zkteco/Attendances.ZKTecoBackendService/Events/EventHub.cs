﻿using Attendances.ZKTecoBackendService.Connectors;
using Attendances.ZKTecoBackendService.Interfaces;
using Attendances.ZKTecoBackendService.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Topshelf.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Attendances.ZKTecoBackendService.Errors;

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
            
            Timer = new System.Timers.Timer(10000);
            Timer.Elapsed += OnHandleEventMessages;
        }

        /// <summary>
        /// Publish event message into queue.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public Task PublishAsync(EventMessage msg)
        {
            return Task.Run(() => {
                if (msg == null)
                {
                    return;
                }

                try
                {
                    EnqueueMessageQueue(msg);
                }
                catch (Exception ex)
                {
                    Logger.ErrorFormat("PublishAsync error: {@error}.", ex);
                }
            });                      
        }

        /// <summary>
        /// Publish failed message into queue.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public Task PublishAsync(FailedMessage msg)
        {
            return Task.Run(() => {
                if (msg == null)
                {
                    return;
                }

                try
                {
                    EnqueueFailedQueue(msg);
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

            Subscribers.AddOrUpdate(kind, new List<IEventHandler>(10) { handler }, (key, list) =>
            {
                var index = list.FindIndex(m => m == handler);
                if (index == -1)
                {
                    list.Add(handler);
                }
                return list;
            });

            Timer.Start();
        }

        private void OnHandleEventMessages(object sender, System.Timers.ElapsedEventArgs e)
        {           
            if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
            {
                Logger.DebugFormat("OnHandleEventMessages is running. Current Thread[{thread}]", Thread.CurrentThread.ManagedThreadId);
                return;
            }

            Logger.DebugFormat("OnHandleEventMessages starts on the thread({threadid}).", Thread.CurrentThread.ManagedThreadId);

            var target = (System.Timers.Timer)sender;
            if (!target.Enabled)
            {
                Logger.Debug("EventHub's timer has stopped.");
                return;
            }

            Logger.Debug("EventHub's timer stops.");
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
                            Logger.DebugFormat("Executing the {handler}.", h.GetType().FullName);
                            h.Handle(msg);
                        }
                        catch (FailedHandleException ex)
                        {
                            Logger.ErrorFormat("Invoking handler error[FailedHandleException]:{@ex}.", ex);                            
                            PublishAsync(ex.FailedMessage);
                            Logger.DebugFormat("Failed message: {@msg}.", ex.FailedMessage);
                            continue;
                        }
                        catch (Exception ex)
                        {
                            Logger.ErrorFormat("Invoking handler error[Exception]:{@ex}.", ex);
                            continue;
                        }
                    }
                    Logger.DebugFormat("Destroy message({id}).", msg.Id);
                    DestroyMessage(msg.Id);
                }
            }

            Interlocked.CompareExchange(ref _running, 0, 1);

            Logger.Debug("EventHub's timer starts again.");
            target.Start();

            Logger.DebugFormat("OnHandleEventMessages ends on the thread({threadid}).", Thread.CurrentThread.ManagedThreadId);
        }

        private void EnqueueMessageQueue(EventMessage msg)
        {
            Database.Execute(
                "INSERT INTO queue(id, refer_id, message, event_type, create_at) VALUES(@id, @refer_id, @message, @event_type, @create_at);",
                new Dictionary<string, object> {
                    { "@id", msg.Id },
                    { "@refer_id", msg.ReferenceId },
                    { "@message", msg.DataToJSON() },
                    { "@event_type", (int)msg.Kind },
                    { "@create_at", msg.OccurredOn }
                });
        }

        private void EnqueueFailedQueue(EventMessage msg)
        {
            Database.Execute(
                "INSERT INTO failed_queue(id, refer_id, message, create_at, retry_times) VALUES(@id, @refer_id, @message, @create_at, 0);",
                new Dictionary<string, object> {
                    { "@id", msg.Id },
                    { "@refer_id", msg.ReferenceId },
                    { "@message", msg.DataToJSON() },
                    { "@create_at", msg.OccurredOn }
                });
        }

        private void EnqueueFailedQueue(FailedMessage msg)
        {
            var count = Database.QueryScalar(
                "SELECT COUNT(*) FROM failed_queue WHERE refer_id=@refer_id;", 
                new Dictionary<string, object> { { "@refer_id", msg.ReferenceId } });
            // one record exists.
            if (count != null && (long)count > 0)
            {
                return;
            }

            Database.Execute(
                "INSERT INTO failed_queue(id, refer_id, message, create_at, retry_times, kind, handler) VALUES(@id, @refer_id, @message, @create_at, 0, @kind, @handler);",
                new Dictionary<string, object> {
                    { "@id", msg.Id },
                    { "@refer_id", msg.ReferenceId },
                    { "@message", msg.DataToJSON() },
                    { "@create_at", msg.OccurredOn },
                    { "@kind", (int)msg.Kind },
                    { "@handler", msg.Handler }
                });
        }

        /// <summary>
        /// Get all the pending messages
        /// </summary>
        /// <returns></returns>
        private List<EventMessage> GetPendingMessages()
        {
            var results = new List<EventMessage>();
            var reader = Database.QuerySet("SELECT id, event_type, refer_id, message, create_at FROM queue ORDER BY create_at ASC;");
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
        private void DestroyMessage(string msgId)
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
            Logger.Debug("EventHub is disposing...");

            Timer.Stop();
            Timer.Elapsed -= OnHandleEventMessages;
            Timer = null;

            Subscribers.Clear();
            Subscribers = null;

            Logger.Debug("EventHub disposes completely.");
        }
    }
}
