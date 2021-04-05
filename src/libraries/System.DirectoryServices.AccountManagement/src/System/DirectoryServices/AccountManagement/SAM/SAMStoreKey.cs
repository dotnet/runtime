// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Globalization;

namespace System.DirectoryServices.AccountManagement
{
    internal sealed class SAMStoreKey : StoreKey
    {
        private readonly byte[] _sid;
        private readonly string _machineName;

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

            if (!string.Equals(_machineName, that._machineName, StringComparison.OrdinalIgnoreCase))
                return false;

            return Utils.AreBytesEqual(_sid, that._sid);
        }

        public override int GetHashCode()
        {
            return _machineName.GetHashCode() ^ _sid.GetHashCode();
        }
    }
}
