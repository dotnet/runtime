// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

#pragma warning disable 0649 // fields never explicitly assigned to
#pragma warning disable 0169 // fields never used

internal partial class Interop
{
    internal partial class Kernel32
    {
        [DllImport(Libraries.Kernel32, SetLastError = true)]
        internal static extern bool GetLogicalProcessorInformationEx(LOGICAL_PROCESSOR_RELATIONSHIP RelationshipType, IntPtr Buffer, ref uint ReturnedLength);

        internal enum LOGICAL_PROCESSOR_RELATIONSHIP
        {
            RelationGroup = 4
        }

        internal struct SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX
        {
            public LOGICAL_PROCESSOR_RELATIONSHIP Relationship;
            public uint Size;
            public GROUP_RELATIONSHIP Group; // part of a union, but we only need the Group
        }

        internal unsafe struct GROUP_RELATIONSHIP
        {
            private byte MaximumGroupCount;
            public ushort ActiveGroupCount;
            private fixed byte Reserved[20];
            public PROCESSOR_GROUP_INFO GroupInfo; // actually a GroupInfo[ANYSIZE_ARRAY], so used for its address
        }

        internal unsafe struct PROCESSOR_GROUP_INFO
        {
            public byte MaximumProcessorCount;
            public byte ActiveProcessorCount;
            public fixed byte Reserved[38];
            public IntPtr ActiveProcessorMask;
        }
    }
}
