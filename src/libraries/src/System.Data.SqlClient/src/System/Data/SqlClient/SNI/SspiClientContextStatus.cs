// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Net;
using System.Net.Security;


namespace System.Data.SqlClient.SNI
{
    internal class SspiClientContextStatus
    {
        public SafeFreeCredentials CredentialsHandle
        {
            get;
            set;
        }

        public SafeDeleteContext SecurityContext
        {
            get;
            set;
        }

        public ContextFlagsPal ContextFlags
        {
            get;
            set;
        }
    }
}
