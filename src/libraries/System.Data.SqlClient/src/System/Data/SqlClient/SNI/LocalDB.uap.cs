// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Data.SqlClient.SNI
{
    internal class LocalDB
    {
        internal static string GetLocalDBConnectionString(string localDbInstance)
        {
            throw new PlatformNotSupportedException(SR.LocalDBNotSupported); // No Registry support on UAP
        }
    }
}