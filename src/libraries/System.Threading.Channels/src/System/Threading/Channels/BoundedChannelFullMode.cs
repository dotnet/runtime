// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Threading.Channels
{
    /// <summary>Specifies the behavior to use when writing to a bounded channel that is already full.</summary>
    public enum BoundedChannelFullMode
    {
        /// <summary>Wait for space to be available in order to complete the write operation.</summary>
        Wait,
        /// <summary>Remove and ignore the newest item in the channel in order to make room for the item being written.</summary>
        DropNewest,
        /// <summary>Remove and ignore the oldest item in the channel in order to make room for the item being written.</summary>
        DropOldest,
        /// <summary>Drop the item being written.</summary>
        DropWrite
    }
}
