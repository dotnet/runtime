// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.Data
{
    internal static partial class LocalDBAPI
    {
        internal static string GetLocalDBMessage(int hrCode) =>
            throw new PlatformNotSupportedException(SR.LocalDBNotSupported); // LocalDB is not available for Unix and hence it cannot be supported.
    }
}
