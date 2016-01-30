// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

namespace System.Collections.Specialized
{
    /// <summary>
    /// A collection implementing this interface will notify listeners of dynamic changes,
    /// e.g. when items get added and removed or the whole list is refreshed.
    /// </summary>
    public interface INotifyCollectionChanged
    {
        /// <summary>
        /// Occurs when the collection changes, either by adding or removing an item.
        /// </summary>
        /// <remarks>
        /// The event handler receives an argument of type
        /// <seealso cref="System.Collections.Specialized.NotifyCollectionChangedEventArgs" />
        /// containing data related to this event.
        /// </remarks>
        event NotifyCollectionChangedEventHandler CollectionChanged;
    }
}


