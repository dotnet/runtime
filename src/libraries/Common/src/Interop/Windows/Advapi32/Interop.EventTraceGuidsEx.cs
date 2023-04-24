// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        internal enum TRACE_QUERY_INFO_CLASS
        {
            TraceGuidQueryList,
            TraceGuidQueryInfo,
            TraceGuidQueryProcess,
            TraceStackTracingInfo,
            MaxTraceSetInfoClass
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct TRACE_GUID_INFO
        {
            public int InstanceCount;
            public int Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct TRACE_PROVIDER_INSTANCE_INFO
        {
            public int NextOffset;
            public int EnableCount;
            public int Pid;
            public int Flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct TRACE_ENABLE_INFO
        {
            public int IsEnabled;
            public byte Level;
            public byte Reserved1;
            public ushort LoggerId;
            public int EnableProperty;
            public int Reserved2;
            public long MatchAnyKeyword;
            public long MatchAllKeyword;
        }

        [LibraryImport(Interop.Libraries.Advapi32)]
        internal static unsafe partial int EnumerateTraceGuidsEx(
            TRACE_QUERY_INFO_CLASS TraceQueryInfoClass,
            void* InBuffer,
            int InBufferSize,
            void* OutBuffer,
            int OutBufferSize,
            out int ReturnLength);
    }
}
