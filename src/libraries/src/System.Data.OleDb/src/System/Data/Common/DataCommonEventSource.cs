// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Tracing;
using System.Threading;

namespace System.Data
{
    [EventSource(Name = "System.Data.DataCommonEventSource")]
#if uapaot
    public
#else
    internal
#endif
    class DataCommonEventSource : EventSource
    {
        internal static readonly DataCommonEventSource Log = new DataCommonEventSource();
#if uapaot
        public
#else
        private
#endif
        const int TraceEventId = 1;

        [Event(TraceEventId, Level = EventLevel.Informational)]
#if uapaot
        public
#else
        internal
#endif
        void Trace(string message)
        {
            WriteEvent(TraceEventId, message);
        }

        [NonEvent]
#if uapaot
        public
#else
        internal
#endif
        void Trace<T0>(string format, T0 arg0)
        {
            if (!Log.IsEnabled())
                return;
            Trace(string.Format(format, arg0));
        }

#if uapaot
        private static long s_nextScopeId = 0;
        public const int EnterScopeId = 2;
        public const int ExitScopeId = 3;

        [NonEvent]
        public void Trace<T0, T1>(string format, T0 arg0, T1 arg1)
        {
            if (!Log.IsEnabled())
                return;
            Trace(string.Format(format, arg0, arg1));
        }

        [NonEvent]
        public void Trace<T0, T1, T2>(string format, T0 arg0, T1 arg1, T2 arg2)
        {
            if (!Log.IsEnabled())
                return;
            Trace(string.Format(format, arg0, arg1, arg2));
        }

        [NonEvent]
        public void Trace<T0, T1, T2, T3>(string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
        {
            if (!Log.IsEnabled())
                return;
            Trace(string.Format(format, arg0, arg1, arg2, arg3));
        }

        [NonEvent]
        public void Trace<T0, T1, T2, T3, T4>(string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (!Log.IsEnabled())
                return;
            Trace(string.Format(format, arg0, arg1, arg2, arg3, arg4));
        }

        [NonEvent]
        public void Trace<T0, T1, T2, T3, T4, T5, T6>(string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            if (!Log.IsEnabled())
                return;
            Trace(string.Format(format, arg0, arg1, arg2, arg3, arg4, arg5, arg6));
        }

        [Event(EnterScopeId, Level = EventLevel.Verbose)]
        public long EnterScope(string message)
        {
            long scopeId = 0;
            if (Log.IsEnabled())
            {
                scopeId = Interlocked.Increment(ref s_nextScopeId);
                WriteEvent(EnterScopeId, scopeId, message);
            }
            return scopeId;
        }

        [NonEvent]
        public long EnterScope<T1>(string format, T1 arg1) => Log.IsEnabled() ? EnterScope(string.Format(format, arg1)) : 0;

        [NonEvent]
        public long EnterScope<T1, T2>(string format, T1 arg1, T2 arg2) => Log.IsEnabled() ? EnterScope(string.Format(format, arg1, arg2)) : 0;

        [NonEvent]
        public long EnterScope<T1, T2, T3>(string format, T1 arg1, T2 arg2, T3 arg3) => Log.IsEnabled() ? EnterScope(string.Format(format, arg1, arg2, arg3)) : 0;

        [NonEvent]
        public long EnterScope<T1, T2, T3, T4>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => Log.IsEnabled() ? EnterScope(string.Format(format, arg1, arg2, arg3, arg4)) : 0;

        [Event(ExitScopeId, Level = EventLevel.Verbose)]
        public void ExitScope(long scopeId)
        {
            WriteEvent(ExitScopeId, scopeId);
        }
#endif
    }
}
