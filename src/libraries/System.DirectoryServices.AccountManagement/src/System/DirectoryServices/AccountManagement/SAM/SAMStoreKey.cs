// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Globalization;

namespace System.DirectoryServices.AccountManagement
{
    internal class SAMStoreKey : StoreKey
    {
        private byte[] _sid;
        private string _machineName;

        public SAMStoreKey(string machineName, byte[] sid)
        {
            Debug.Assert(machineName != null && machineName.Length > 0);
            Debug.Assert(sid != null && sid.Length > 0);

            _machineName = machineName;

            // Make a copy of the SID, since a byte[] is mutable
            _sid = new byte[sid.Length];
            Array.Copy(sid, _sid, sid.Length);

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

            SAMStoreKey that = (SAMStoreKey)o;

            if (String.Compare(_machineName, that._machineName, StringComparison.OrdinalIgnoreCase) != 0)
                return false;

            return Utils.AreBytesEqual(_sid, that._sid);
        }

        override public int GetHashCode()
        {
            return _machineName.GetHashCode() ^ _sid.GetHashCode();
        }
    }
}

