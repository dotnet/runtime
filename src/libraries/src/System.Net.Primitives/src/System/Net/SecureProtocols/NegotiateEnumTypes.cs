// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
