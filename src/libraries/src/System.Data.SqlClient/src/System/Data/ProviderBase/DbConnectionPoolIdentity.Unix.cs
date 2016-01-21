// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


//------------------------------------------------------------------------------

using System.Security.Principal;


namespace System.Data.ProviderBase
{
    partial class DbConnectionPoolIdentity
    {
        static internal DbConnectionPoolIdentity GetCurrent()
        {
            throw new PlatformNotSupportedException();
        }
    }
}

