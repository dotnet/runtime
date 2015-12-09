// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using System.Diagnostics.Tracing;

// We wish to test both Microsoft.Diagnostics.Tracing (Nuget)
// and System.Diagnostics.Tracing (Framewwork), we use this Ifdef make each kind 

namespace SdtEventSources
{
    public interface IMyLogging
    {
        void Error(int errorCode, string msg);
        void Warning(string msg);
    }

    public sealed class MyLoggingEventSource : EventSource, IMyLogging
    {
        public static MyLoggingEventSource Log = new MyLoggingEventSource();

        [Event(1)]
        public void Error(int errorCode, string msg)
        { WriteEvent(1, errorCode, msg); }

        [Event(2)]
        public void Warning(string msg)
        { WriteEvent(2, msg); }
    }
}
