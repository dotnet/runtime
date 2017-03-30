// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Data
{
    // Gets the state of a DataRow object.
    [Flags]
    public enum DataRowState
    {
        // DataViewRowState.None = 00000000;
        // The row has been created but is not part of any DataRowCollection.
        // A DataRow is in this state immediately after it has been created and 
        // before it is added to a collection, or if it has been removed from a collection.
        Detached = 0x00000001,
        // The row has not changed since AcceptChanges was last called.
        Unchanged = 0x00000002,
        // The row was added to a DataRowCollection, and AcceptChanges has not been called.
        Added = 0x00000004,
        // The row was deleted using the Delete method of the DataRow.
        Deleted = 0x00000008,
        // The row was modified and AcceptChanges has not been called.
        Modified = 0x000000010
    }
}
