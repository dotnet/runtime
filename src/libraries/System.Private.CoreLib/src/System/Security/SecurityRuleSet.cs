// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security
{
    public enum SecurityRuleSet : byte
    {
        None = 0,
        Level1 = 1,    // v2.0 transparency model
        Level2 = 2,    // v4.0 transparency model
    }
}
