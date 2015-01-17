// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        /// Discards changes since the last BeginEdit call. 
        /// </summary>
        void EndEdit();

        /// <summary>
        /// Pushes changes since the last BeginEdit into the underlying object. 
        /// </summary>
        void CancelEdit();
    }
}
