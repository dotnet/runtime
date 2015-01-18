// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace System.ComponentModel
{
    /// <summary>
    /// Provides support for rolling back the changes
    /// </summary>
    public interface IRevertibleChangeTracking : IChangeTracking
    {
        /// <summary>
        /// Resets the object's state to unchanged by rejecting the modifications.
        ///
        void RejectChanges();
    }
}