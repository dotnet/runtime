// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.ComponentModel.Composition.Diagnostics
{
    public class TraceEventDetails
    {
        public TraceEventDetails(TraceEventCache eventCache, string source, TraceEventType eventType, TraceId id, string format, params object[] args)
        {
            EventCache = eventCache;
            Source = source;
            EventType = eventType;
            Id = id;
            Format = format;
            Args = args;
        }

        public TraceEventCache EventCache
        {
            get;
            private set;
        }

        public string Source
        {
            get;
            private set;
        }

        public TraceEventType EventType
        {
            get;
            private set;
        }

        public TraceId Id
        {
            get;
            private set;
        }

        public string Format
        {
            get;
            private set;
        }

        public object[] Args
        {
            get;
            private set;
        }
    }
}
