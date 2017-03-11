// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//------------------------------------------------------------------------------

using System.Security.Principal;


namespace System.Data.ProviderBase
{
    partial class DbConnectionPoolIdentity
    {
        internal static DbConnectionPoolIdentity GetCurrent()
        {
            string sidString = (!string.IsNullOrWhiteSpace(System.Environment.UserDomainName) ? System.Environment.UserDomainName + "\\" : "")
                                + System.Environment.UserName;
            bool isNetwork = false;
            bool isRestricted = false;
            return new DbConnectionPoolIdentity(sidString, isRestricted, isNetwork);
        }
    }
}
