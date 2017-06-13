// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    /// Defines the mechanism for querying the object for changes and resetting of
    /// the changed status.
    /// </summary>
    public interface IChangeTracking
    {
        /// <summary>
        /// Gets a value indicating whether the object's content has changed since 
        /// the last call to System.ComponentModel.IChangeTracking.AcceptChanges().
        /// </summary>
        bool IsChanged { get; }

        /// <summary>
        /// Resets the object's state to unchanged by accepting the modifications.
        /// </summary>
        void AcceptChanges();
    }
}
