// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    /// Provides functionality to commit or rollback changes to an object that is used as a data source.
    /// </summary>
    public interface IEditableObject
    {
        /// <summary>
        /// Begins an edit on an object.
        /// </summary>
        void BeginEdit();

        /// <summary>
        /// Pushes changes since the last BeginEdit into the underlying object.
        /// </summary>
        void EndEdit();

        /// <summary>
        /// Discards changes since the last BeginEdit call.
        /// </summary>
        void CancelEdit();
    }
}
