// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Data
{
    [Flags]
    public enum DataRowAction
    {
        Nothing = 0,        //  0 0x00
        Delete = (1 << 0), //  1 0x01
        Change = (1 << 1), //  2 0x02
        Rollback = (1 << 2), //  4 0x04
        Commit = (1 << 3), //  8 0x08
        Add = (1 << 4), // 16 0x10
        ChangeOriginal = (1 << 5), // 32 0x20
        ChangeCurrentAndOriginal = (1 << 6), // 64 0x40
    }
}
