// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct PERF_COUNTER_BLOCK
        {
            internal int ByteLength;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct PERF_COUNTER_DEFINITION
        {
            internal int ByteLength;
            internal int CounterNameTitleIndex;
            internal int CounterNameTitlePtr;
            internal int CounterHelpTitleIndex;
            internal int CounterHelpTitlePtr;
            internal int DefaultScale;
            internal int DetailLevel;
            internal int CounterType;
            internal int CounterSize;
            internal int CounterOffset;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct PERF_DATA_BLOCK
        {
            internal int Signature1;
            internal int Signature2;
            internal int LittleEndian;
            internal int Version;
            internal int Revision;
            internal int TotalByteLength;
            internal int HeaderLength;
            internal int NumObjectTypes;
            internal int DefaultObject;
            internal SYSTEMTIME SystemTime;
            internal int pad1;  // Need to pad the struct to get quadword alignment for the 'long' after SystemTime
            internal long PerfTime;
            internal long PerfFreq;
            internal long PerfTime100nSec;
            internal int SystemNameLength;
            internal int SystemNameOffset;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct PERF_INSTANCE_DEFINITION
        {
            internal int ByteLength;
            internal int ParentObjectTitleIndex;
            internal int ParentObjectInstance;
            internal int UniqueID;
            internal int NameOffset;
            internal int NameLength;

            internal static ReadOnlySpan<char> GetName(in PERF_INSTANCE_DEFINITION instance, ReadOnlySpan<byte> data)
                => (instance.NameLength == 0) ? default
                    : MemoryMarshal.Cast<byte, char>(data.Slice(instance.NameOffset, instance.NameLength - sizeof(char))); // NameLength includes the null-terminator
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct PERF_OBJECT_TYPE
        {
            internal int TotalByteLength;
            internal int DefinitionLength;
            internal int HeaderLength;
            internal int ObjectNameTitleIndex;
            internal int ObjectNameTitlePtr;
            internal int ObjectHelpTitleIndex;
            internal int ObjectHelpTitlePtr;
            internal int DetailLevel;
            internal int NumCounters;
            internal int DefaultCounter;
            internal int NumInstances;
            internal int CodePage;
            internal long PerfTime;
            internal long PerfFreq;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SYSTEMTIME
        {
            internal short wYear;
            internal short wMonth;
            internal short wDayOfWeek;
            internal short wDay;
            internal short wHour;
            internal short wMinute;
            internal short wSecond;
            internal short wMilliseconds;

            public override string ToString()
            {
                return "[SYSTEMTIME: "
                + wDay.ToString(CultureInfo.CurrentCulture) + "/" + wMonth.ToString(CultureInfo.CurrentCulture) + "/" + wYear.ToString(CultureInfo.CurrentCulture)
                + " " + wHour.ToString(CultureInfo.CurrentCulture) + ":" + wMinute.ToString(CultureInfo.CurrentCulture) + ":" + wSecond.ToString(CultureInfo.CurrentCulture)
                + "]";
            }
        }
    }
}
