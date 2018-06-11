// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    /// Interface implemented by a list that allows the addition of a new item
    /// to be either cancelled or committed.
    ///
    /// Note: In some scenarios, specifically Windows Forms complex data binding,
    /// the list may receive CancelNew or EndNew calls for items other than the
    /// new item. These calls should be ignored, ie. the new item should only be
    /// cancelled or committed when that item's index is specified.
    /// </summary>
    public interface ICancelAddNew
    {
        /// <summary>
        /// If a new item has been added to the list, and <paramref name="itemIndex"/> is the position of that item,
        /// then this method should remove it from the list and cancel the add operation.
        /// </summary>
        void CancelNew(int itemIndex);

        /// <summary>
        /// If a new item has been added to the list, and <paramref name="itemIndex"/> is the position of that item,
        /// then this method should leave it in the list and complete the add operation.
        /// </summary>
        void EndNew(int itemIndex);
    }
}
