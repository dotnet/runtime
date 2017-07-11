// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data.SqlClient;
using System.Security.Principal;

namespace System.Data.ProviderBase
{
    partial class DbConnectionPoolIdentity
    {
        private static DbConnectionPoolIdentity s_lastIdentity = null;

        internal static DbConnectionPoolIdentity GetCurrent()
        {
            return TdsParserStateObjectFactory.UseManagedSNI ? GetCurrentManaged() : GetCurrentNative();
        }

        private static DbConnectionPoolIdentity GetCurrentNative()
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
    }
}

