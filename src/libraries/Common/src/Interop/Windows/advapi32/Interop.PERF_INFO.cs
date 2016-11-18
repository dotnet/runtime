// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Advapi32
    {
        [StructLayout(LayoutKind.Sequential)]
        internal class PERF_COUNTER_BLOCK
        {
            internal int ByteLength = 0;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal class PERF_COUNTER_DEFINITION
        {
            internal int ByteLength = 0;
            internal int CounterNameTitleIndex = 0;
            internal int CounterNameTitlePtr = 0;
            internal int CounterHelpTitleIndex = 0;
            internal int CounterHelpTitlePtr = 0;
            internal int DefaultScale = 0;
            internal int DetailLevel = 0;
            internal int CounterType = 0;
            internal int CounterSize = 0;
            internal int CounterOffset = 0;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal class PERF_DATA_BLOCK
        {
            internal int Signature1 = 0;
            internal int Signature2 = 0;
            internal int LittleEndian = 0;
            internal int Version = 0;
            internal int Revision = 0;
            internal int TotalByteLength = 0;
            internal int HeaderLength = 0;
            internal int NumObjectTypes = 0;
            internal int DefaultObject = 0;
            internal SYSTEMTIME SystemTime = null;
            internal int pad1 = 0;  // Need to pad the struct to get quadword alignment for the 'long' after SystemTime
            internal long PerfTime = 0;
            internal long PerfFreq = 0;
            internal long PerfTime100nSec = 0;
            internal int SystemNameLength = 0;
            internal int SystemNameOffset = 0;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal class PERF_INSTANCE_DEFINITION
        {
            internal int ByteLength = 0;
            internal int ParentObjectTitleIndex = 0;
            internal int ParentObjectInstance = 0;
            internal int UniqueID = 0;
            internal int NameOffset = 0;
            internal int NameLength = 0;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal class PERF_OBJECT_TYPE
        {
            internal int TotalByteLength = 0;
            internal int DefinitionLength = 0;
            internal int HeaderLength = 0;
            internal int ObjectNameTitleIndex = 0;
            internal int ObjectNameTitlePtr = 0;
            internal int ObjectHelpTitleIndex = 0;
            internal int ObjectHelpTitlePtr = 0;
            internal int DetailLevel = 0;
            internal int NumCounters = 0;
            internal int DefaultCounter = 0;
            internal int NumInstances = 0;
            internal int CodePage = 0;
            internal long PerfTime = 0;
            internal long PerfFreq = 0;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
        [StructLayout(LayoutKind.Sequential)]
        internal class SYSTEMTIME
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
