// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class PerfCounter
    {
        [GeneratedDllImport(Libraries.Advapi32, ExactSpelling = true)]
        internal static partial uint PerfStopProvider(
            IntPtr hProvider
        );

        internal unsafe delegate uint PERFLIBREQUEST(
            uint RequestCode,
            void* Buffer,
            uint BufferSize
        );

        // Native PERFLIB V2 Provider APIs.
        internal struct PerfCounterSetInfoStruct
        {
            // PERF_COUNTERSET_INFO structure defined in perflib.h
            internal Guid CounterSetGuid;
            internal Guid ProviderGuid;
            internal uint NumCounters;
            internal uint InstanceType;
        }

        internal struct PerfCounterInfoStruct
        {
            // PERF_COUNTER_INFO structure defined in perflib.h
            internal uint CounterId;
            internal uint CounterType;
            internal long Attrib;
            internal uint Size;
            internal uint DetailLevel;
            internal uint Scale;
            internal uint Offset;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct PerfCounterSetInstanceStruct
        {
            // PERF_COUNTERSET_INSTANCE structure defined in perflib.h
            internal Guid CounterSetGuid;
            internal uint dwSize;
            internal uint InstanceId;
            internal uint InstanceNameOffset;
            internal uint InstanceNameSize;
        }

#pragma warning disable DLLIMPORTGENANALYZER015 // Use 'GeneratedDllImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
        // TODO: [DllImportGenerator] Switch to use GeneratedDllImport once we annotate blittable types used in interop in CoreLib (like Guid)
        [DllImport(Libraries.Advapi32, ExactSpelling = true)]
        internal static extern uint PerfStartProvider(
            ref Guid ProviderGuid,
            PERFLIBREQUEST ControlCallback,
            out SafePerfProviderHandle phProvider
        );

        // TODO: [DllImportGenerator] Switch to use GeneratedDllImport once we annotate blittable types used in interop in CoreLib (like Guid)
        [DllImport(Libraries.Advapi32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
        internal static extern unsafe PerfCounterSetInstanceStruct* PerfCreateInstance(
            SafePerfProviderHandle hProvider,
            ref Guid CounterSetGuid,
            string szInstanceName,
            uint dwInstance
        );
#pragma warning restore DLLIMPORTGENANALYZER015

        [GeneratedDllImport(Libraries.Advapi32, ExactSpelling = true)]
        internal static unsafe partial uint PerfSetCounterSetInfo(
            SafePerfProviderHandle hProvider,
            PerfCounterSetInfoStruct* pTemplate,
            uint dwTemplateSize
        );

        [GeneratedDllImport(Libraries.Advapi32, ExactSpelling = true)]
        internal static unsafe partial uint PerfDeleteInstance(
            SafePerfProviderHandle hProvider,
            PerfCounterSetInstanceStruct* InstanceBlock
        );

        [GeneratedDllImport(Libraries.Advapi32, ExactSpelling = true)]
        internal static unsafe partial uint PerfSetCounterRefValue(
            SafePerfProviderHandle hProvider,
            PerfCounterSetInstanceStruct* pInstance,
            uint CounterId,
            void* lpAddr
        );
    }
}
