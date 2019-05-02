// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using System.Data.ProviderBase;
using System.Runtime.InteropServices;

namespace System.Data.OleDb
{
    internal sealed class PropertyIDSet : DbBuffer
    {
        static private readonly int PropertyIDSetAndValueSize = ODB.SizeOf_tagDBPROPIDSET + ADP.PtrSize; // sizeof(tagDBPROPIDSET) + sizeof(int)
        static private readonly int PropertyIDSetSize = ODB.SizeOf_tagDBPROPIDSET;

        private int _count;

        // the PropertyID is stored at the end of the tagDBPROPIDSET structure
        // this way only a single memory allocation is required instead of two
        internal PropertyIDSet(Guid propertySet, int propertyID) : base(PropertyIDSetAndValueSize)
        {
            _count = 1;

            // rgPropertyIDs references where that PropertyID is stored
            // depending on IntPtr.Size, tagDBPROPIDSET is either 24 or 28 bytes long
            IntPtr ptr = ADP.IntPtrOffset(base.handle, PropertyIDSetSize);
            Marshal.WriteIntPtr(base.handle, 0, ptr);

            Marshal.WriteInt32(base.handle, ADP.PtrSize, /*propertyid count*/1);

            ptr = ADP.IntPtrOffset(base.handle, ODB.OffsetOf_tagDBPROPIDSET_PropertySet);
            Marshal.StructureToPtr(propertySet, ptr, false/*deleteold*/);

            // write the propertyID at the same offset
            Marshal.WriteInt32(base.handle, PropertyIDSetSize, propertyID);
        }

        // no propertyIDs, just the propertyset guids
        internal PropertyIDSet(Guid[] propertySets) : base(PropertyIDSetSize * propertySets.Length)
        {
            _count = propertySets.Length;
            for (int i = 0; i < propertySets.Length; ++i)
            {
                IntPtr ptr = ADP.IntPtrOffset(base.handle, (i * PropertyIDSetSize) + ODB.OffsetOf_tagDBPROPIDSET_PropertySet);
                Marshal.StructureToPtr(propertySets[i], ptr, false/*deleteold*/);
            }
        }

        internal int Count
        {
            get
            {
                return _count;
            }
        }
    }
}
