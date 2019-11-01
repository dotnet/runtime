// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Security
{
    // WebRequest-specific authentication flags
    public enum AuthenticationLevel
    {
        None = 0,
        MutualAuthRequested = 1, // default setting
        MutualAuthRequired = 2
    }
}
