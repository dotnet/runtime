// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System
{
    partial struct Guid
    {
        // This will create a new random guid based on the https://www.ietf.org/rfc/rfc4122.txt 
        public static unsafe Guid NewGuid()
        {
            Guid g;
            Interop.GetRandomBytes((byte*)&g, sizeof(Guid));
            
            const ushort VersionMask = 0xF000;
            const ushort RandomGuidVersion = 0x4000;

            const byte ClockSeqHiAndReservedMask = 0xC0;
            const byte ClockSeqHiAndReservedValue = 0x80;

            // Modify bits indicating the type of the GUID

            unchecked
            {
                // time_hi_and_version
                g._c = (short)((g._c & ~VersionMask) | RandomGuidVersion);
                // clock_seq_hi_and_reserved
                g._d = (byte)((g._d & ~ClockSeqHiAndReservedMask) | ClockSeqHiAndReservedValue);
            }

            return g;
        }
    }
}
