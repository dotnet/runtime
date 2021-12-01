// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.Data
{
    /// <summary>
    /// Describes the version of data in a <see cref='System.Data.DataRow'/>.
    /// </summary>
    [Flags]
    [Editor("Microsoft.VSDesigner.Data.Design.DataViewRowStateEditor, Microsoft.VSDesigner, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
            "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
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
