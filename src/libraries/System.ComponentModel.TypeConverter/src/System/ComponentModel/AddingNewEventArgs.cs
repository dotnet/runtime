// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <summary>
    ///     Provides data for an event that signals the adding of a new object
    ///     to a list, allowing any event handler to supply the new object. If
    ///     no event handler supplies a new object to use, the list should create
    ///     one itself.
    /// </summary>
    public class AddingNewEventArgs : EventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref='System.ComponentModel.AddingNewEventArgs'/> class,
        ///     with no new object defined.
        /// </summary>
        public AddingNewEventArgs()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref='System.ComponentModel.AddingNewEventArgs'/> class,
        ///     with the specified object defined as the default new object.
        /// </summary>
        public AddingNewEventArgs(object newObject)
        {
            NewObject = newObject;
        }

        /// <summary>
        ///     Gets or sets the new object that will be added to the list.
        /// </summary>
        public object NewObject { get; set; }
    }
}
