// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
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

            internal static readonly int SizeOf = Marshal.SizeOf<PERF_COUNTER_BLOCK>();

            public readonly void Validate(int bufferSize)
            {
                if (ByteLength < SizeOf ||
                    ByteLength > bufferSize)
                {
                    ThrowInvalidOperationException(typeof(PERF_COUNTER_BLOCK));
                }
            }
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

            internal static readonly int SizeOf = Marshal.SizeOf<PERF_COUNTER_DEFINITION>();

            public readonly void Validate(int bufferSize)
            {
                if (ByteLength < SizeOf ||
                    ByteLength > bufferSize ||
                    CounterSize < 0 ||
                    CounterOffset < 0 ||
                    CounterOffset > bufferSize)
                {
                    ThrowInvalidOperationException(typeof(PERF_COUNTER_DEFINITION));
                }
            }
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

            internal const int Signature1Int = (int)'P' + ('E' << 16);
            internal const int Signature2Int = (int)'R' + ('F' << 16);
            internal static readonly int SizeOf = Marshal.SizeOf<PERF_DATA_BLOCK>();

            public readonly void Validate(int bufferSize)
            {
                if (Signature1 != Signature1Int ||
                    Signature2 != Signature2Int ||
                    TotalByteLength < SizeOf ||
                    TotalByteLength > bufferSize ||
                    HeaderLength < SizeOf ||
                    HeaderLength > TotalByteLength)
                {
                    ThrowInvalidOperationException(typeof(PERF_DATA_BLOCK));
                }
            }
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

            internal static readonly int SizeOf = Marshal.SizeOf<PERF_INSTANCE_DEFINITION>();

            internal static ReadOnlySpan<char> GetName(in PERF_INSTANCE_DEFINITION instance, ReadOnlySpan<byte> data)
                => (instance.NameLength == 0) ? default
                    : MemoryMarshal.Cast<byte, char>(data.Slice(instance.NameOffset, instance.NameLength - sizeof(char))); // NameLength includes the null-terminator

            public readonly void Validate(int bufferSize)
            {
                if (ByteLength < SizeOf ||
                    ByteLength > bufferSize ||
                    NameOffset < 0 ||
                    NameLength < 0 ||
                    checked(NameOffset + NameLength) > ByteLength)
                {
                    ThrowInvalidOperationException(typeof(PERF_INSTANCE_DEFINITION));
                }
            }
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

            internal static readonly int SizeOf = Marshal.SizeOf<PERF_OBJECT_TYPE>();

            public readonly void Validate(int bufferSize)
            {
                if (HeaderLength < SizeOf ||
                    HeaderLength > TotalByteLength ||
                    HeaderLength > DefinitionLength ||
                    DefinitionLength < SizeOf ||
                    DefinitionLength > TotalByteLength ||
                    TotalByteLength > bufferSize ||
                    NumCounters < 0 ||
                    checked
                    (
                        // This is a simple check, not exact, since it depends on how instances are specified.
                        (NumInstances <= 0 ? 0 : NumInstances * PERF_INSTANCE_DEFINITION.SizeOf) +
                        NumCounters * PERF_COUNTER_DEFINITION.SizeOf
                    ) > bufferSize
                )
                {
                    ThrowInvalidOperationException(typeof(PERF_OBJECT_TYPE));
                }
            }
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

    [DoesNotReturn]
    public static void ThrowInvalidOperationException(Type type) =>
        throw new InvalidOperationException(SR.Format(SR.InvalidPerfData, type.Name));
}
