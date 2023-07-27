// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;

namespace Microsoft.Extensions.Http
{
    [EventSource(Name = "Private.InternalDiagnostics.Microsoft.Extensions.Http")]
    internal sealed class NetEventSource : EventSource
    {
        public static readonly NetEventSource Log = new NetEventSource();

        public static class Keywords
        {
            public const EventKeywords Default = (EventKeywords)0x0001;
            public const EventKeywords Debug = (EventKeywords)0x0002;
        }

        private const int CleanupCycleStartEventId = 1;
        private const int CleanupCycleEndEventId = 2;
        private const int CleanupItemFailedEventId = 3;
        private const int HandlerExpiredEventId = 4;

        [NonEvent]
        public static void CleanupCycleStart(int initialCount)
        {
            Debug.Assert(Log.IsEnabled());
            Log.CleanupCycleStart($"Starting HttpMessageHandler cleanup cycle with {initialCount} items");
        }

        [Event(CleanupCycleStartEventId, Keywords = Keywords.Debug, Level = EventLevel.Verbose)]
        private void CleanupCycleStart(string message) =>
            WriteEvent(CleanupCycleStartEventId, message);

        [NonEvent]
        public static void CleanupCycleEnd(TimeSpan duration, int disposedCount, int finalCount)
        {
            Debug.Assert(Log.IsEnabled());
            Log.CleanupCycleEnd($"Ending HttpMessageHandler cleanup cycle after {duration.TotalMilliseconds}ms - processed: {disposedCount} items - remaining: {finalCount} items");
        }

        [Event(CleanupCycleEndEventId, Keywords = Keywords.Debug, Level = EventLevel.Verbose)]
        private void CleanupCycleEnd(string message) =>
            WriteEvent(CleanupCycleEndEventId, message);

        [NonEvent]
        public static void CleanupItemFailed(string clientName, Exception exception)
        {
            Debug.Assert(Log.IsEnabled());
            Log.CleanupItemFailed(clientName, exception.ToString(), $"HttpMessageHandler.Dispose() threw an unhandled exception for client: '{clientName}'");
        }

        [Event(CleanupItemFailedEventId, Keywords = Keywords.Debug, Level = EventLevel.Error)]
        private void CleanupItemFailed(string clientName, string exception, string message) =>
            WriteEvent(CleanupItemFailedEventId, clientName, exception, message);

        [NonEvent]
        public static void HandlerExpired(string clientName, TimeSpan lifetime)
        {
            Debug.Assert(Log.IsEnabled());
            Log.HandlerExpired(clientName, $"HttpMessageHandler expired after {lifetime} for client '{clientName}'");
        }

        [Event(HandlerExpiredEventId, Keywords = Keywords.Debug, Level = EventLevel.Verbose)]
        private void HandlerExpired(string clientName, string message) =>
            WriteEvent(HandlerExpiredEventId, clientName, message);
    }
}
