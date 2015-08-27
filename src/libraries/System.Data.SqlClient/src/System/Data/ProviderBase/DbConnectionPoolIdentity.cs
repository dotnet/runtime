// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


//------------------------------------------------------------------------------

using System.Security.Principal;


namespace System.Data.ProviderBase
{
    sealed internal class DbConnectionPoolIdentity
    {
        static public readonly DbConnectionPoolIdentity NoIdentity = new DbConnectionPoolIdentity(String.Empty, false, true);
        static private DbConnectionPoolIdentity s_lastIdentity = null;

        private readonly string _sidString;
        private readonly bool _isRestricted;
        private readonly bool _isNetwork;
        private readonly int _hashCode;

        private DbConnectionPoolIdentity(string sidString, bool isRestricted, bool isNetwork)
        {
            _sidString = sidString;
            _isRestricted = isRestricted;
            _isNetwork = isNetwork;
            _hashCode = sidString == null ? 0 : sidString.GetHashCode();
        }

        internal bool IsRestricted
        {
            get { return _isRestricted; }
        }


        override public bool Equals(object value)
        {
            bool result = ((this == NoIdentity) || (this == value));
            if (!result && (null != value))
            {
                DbConnectionPoolIdentity that = ((DbConnectionPoolIdentity)value);
                result = ((_sidString == that._sidString) && (_isRestricted == that._isRestricted) && (_isNetwork == that._isNetwork));
            }
            return result;
        }

        static internal DbConnectionPoolIdentity GetCurrent()
        {
            DbConnectionPoolIdentity current;
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                IntPtr token = identity.AccessToken.DangerousGetHandle();
                bool isNetwork = identity.User.IsWellKnown(WellKnownSidType.NetworkSid);
                string sidString = identity.User.Value;

                // Win32NativeMethods.IsTokenRestricted will raise exception if the native call fails
                bool isRestricted = Win32NativeMethods.IsTokenRestrictedWrapper(token);

                var lastIdentity = s_lastIdentity;
                if ((lastIdentity != null) && (lastIdentity._sidString == sidString) && (lastIdentity._isRestricted == isRestricted) && (lastIdentity._isNetwork == isNetwork))
                {
                    current = lastIdentity;
                }
                else
                {
                    current = new DbConnectionPoolIdentity(sidString, isRestricted, isNetwork);
                }
            }
            s_lastIdentity = current;
            return current;
        }

        override public int GetHashCode()
        {
            return _hashCode;
        }
    }
}

