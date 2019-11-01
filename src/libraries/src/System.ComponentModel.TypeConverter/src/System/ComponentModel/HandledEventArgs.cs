// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    /// Provides data for the <see cref='System.ComponentModel.HandledEventArgs.Handled'/>
    /// event.
    /// </summary>
    public class HandledEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.HandledEventArgs'/> class with
        /// handled set to <see langword='false'/>.
        /// </summary>
        public HandledEventArgs() : this(false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.HandledEventArgs'/> class with
        /// handled set to the given value.
        /// </summary>
        public HandledEventArgs(bool defaultHandledValue)
        {
            Handled = defaultHandledValue;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the event was handled in the application's event handler.
        /// </summary>
        public bool Handled { get; set; }
    }
}
