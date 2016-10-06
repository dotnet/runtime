// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;

namespace System.Data
{
    /// <summary>
    /// Describes the version of data in a <see cref='System.Data.DataRow'/>.
    /// </summary>
    [Flags]
    public enum DataViewRowState
    {
        None = 0x00000000,
        Unchanged = DataRowState.Unchanged,
        Added = DataRowState.Added,
        Deleted = DataRowState.Deleted,
        ModifiedCurrent = DataRowState.Modified,
        ModifiedOriginal = ModifiedCurrent << 1,
        OriginalRows = Unchanged | Deleted | ModifiedOriginal,
        CurrentRows = Unchanged | Added | ModifiedCurrent
    }
}
