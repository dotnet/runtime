// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.



//------------------------------------------------------------------------------

namespace System.Data.SqlClient
{
    [Flags]
    public enum SqlBulkCopyOptions
    {
        Default = 0,
        KeepIdentity = 1 << 0,
        CheckConstraints = 1 << 1,
        TableLock = 1 << 2,
        KeepNulls = 1 << 3,
        FireTriggers = 1 << 4,
        UseInternalTransaction = 1 << 5,
    }
}




