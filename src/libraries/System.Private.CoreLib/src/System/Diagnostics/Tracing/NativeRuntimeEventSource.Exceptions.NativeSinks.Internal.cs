// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Diagnostics.Tracing
{
    internal sealed partial class NativeRuntimeEventSource : EventSource
    {
        [NonEvent]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "NativeRuntimeEventSource_LogExceptionThrown", StringMarshalling = StringMarshalling.Utf16)]
        private static unsafe partial void LogExceptionThrown(string exceptionTypeName, string exceptionMessage, IntPtr faultingIP, uint hresult, ushort flags, ushort ClrInstanceID);
    }
}
