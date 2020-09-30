// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting.Internal
{
    internal static class LoggerEventIds
    {
        public static readonly EventId Starting = new EventId(1, "Starting");
        public static readonly EventId Started = new EventId(2, "Started");
        public static readonly EventId Stopping = new EventId(3, "Stopping");
        public static readonly EventId Stopped = new EventId(4, "Stopped");
        public static readonly EventId StoppedWithException = new EventId(5, "StoppedWithException");
        public static readonly EventId ApplicationStartupException = new EventId(6, "ApplicationStartupException");
        public static readonly EventId ApplicationStoppingException = new EventId(7, "ApplicationStoppingException");
        public static readonly EventId ApplicationStoppedException = new EventId(8, "ApplicationStoppedException");
    }
}
