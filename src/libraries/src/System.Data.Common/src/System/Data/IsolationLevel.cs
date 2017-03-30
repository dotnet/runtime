// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Data
{
    public enum IsolationLevel
    {
        Unspecified = unchecked((int)0xffffffff),
        Chaos = 0x10,
        ReadUncommitted = 0x100,
        ReadCommitted = 0x1000,
        RepeatableRead = 0x10000,
        Serializable = 0x100000,
        Snapshot = 0x1000000,
    }
}
