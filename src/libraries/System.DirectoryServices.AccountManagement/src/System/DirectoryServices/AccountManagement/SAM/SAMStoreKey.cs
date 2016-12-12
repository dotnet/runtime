/*++

Copyright (c) 2004  Microsoft Corporation

Module Name:

    SAMStoreKey.cs

Abstract:

    Implements the SAMStoreKey class.

History:

    05-May-2004    MattRim     Created

--*/

using System;
using System.Diagnostics;
using System.Globalization;

namespace System.DirectoryServices.AccountManagement
{
    class SAMStoreKey : StoreKey
    {
        byte[] sid;
        string machineName;

        public SAMStoreKey(string machineName, byte[] sid)
        {
            Debug.Assert(machineName != null && machineName.Length > 0);
            Debug.Assert(sid != null && sid.Length > 0);

            this.machineName = machineName;

            // Make a copy of the SID, since a byte[] is mutable
            this.sid = new byte[sid.Length];
            Array.Copy(sid, this.sid, sid.Length);

            GlobalDebug.WriteLineIf(
                            GlobalDebug.Info,
                            "SAMStoreKey",
                            "creating key for machineName={0}, sid={1}",
                            machineName,
                            Utils.ByteArrayToString(sid));
            
        }

        public override bool Equals(object o)
        {
            if (!(o is SAMStoreKey))
                return false;

            SAMStoreKey that = (SAMStoreKey) o;

            if (String.Compare(this.machineName, that.machineName, StringComparison.OrdinalIgnoreCase) != 0)
                return false;
            
            return Utils.AreBytesEqual(this.sid, that.sid);
        }

        override public int GetHashCode()
        {
            return this.machineName.GetHashCode() ^ this.sid.GetHashCode(); 
        }        
    }
}

