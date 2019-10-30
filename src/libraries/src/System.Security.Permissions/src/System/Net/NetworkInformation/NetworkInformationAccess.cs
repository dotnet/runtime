// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.NetworkInformation
{
    [Flags]
    public enum NetworkInformationAccess
    {
        None = 0,
        Read = 1,
        Ping = 4
    };
}
