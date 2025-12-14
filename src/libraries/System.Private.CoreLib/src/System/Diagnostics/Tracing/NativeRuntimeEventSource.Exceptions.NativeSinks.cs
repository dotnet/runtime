// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Diagnostics.Tracing
{
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "NativeRuntimeEventSource is a special case where event methods don't use WriteEvent/WriteEventCore but still need to be instance methods.")]
    internal sealed partial class NativeRuntimeEventSource : EventSource
    {
        [Event(80, Version = 1, Level = EventLevel.Error, Keywords = Keywords.ExceptionKeyword | Keywords.MonitoringKeyword)]
        public void ExceptionThrown_V1(string ExceptionType, string ExceptionMessage, IntPtr ExceptionEIP, uint ExceptionHRESULT, ushort ExceptionFlags, ushort ClrInstanceID = DefaultClrInstanceId)
        {
#if NATIVEAOT
            if (!IsEnabled(EventLevel.Error, Keywords.ExceptionKeyword | Keywords.MonitoringKeyword))
            {
                return;
            }
            LogExceptionThrown(ExceptionType, ExceptionMessage, ExceptionEIP, ExceptionHRESULT, ExceptionFlags, ClrInstanceID);
#else
            // QCall not implemented elsewhere
            throw new NotImplementedException();
#endif
        }
    }
}
