// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>
    ///       Provides data for the <see cref='System.ComponentModel.HandledEventArgs.Handled'/>
    ///       event.
    ///    </para>
    /// </summary>
    public class HandledEventArgs : EventArgs
    {
        /// <summary>
        ///     Indicates, on return, whether or not the event was handled in the application's event handler.  
        ///     'true' means the application handled the event, 'false' means it didn't.
        /// </summary>
        private bool _handled;

        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.HandledEventArgs'/> class with
        ///       handled set to <see langword='false'/>.
        ///    </para>
        /// </summary>
        public HandledEventArgs() : this(false)
        {
        }

        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.HandledEventArgs'/> class with
        ///       handled set to the given value.
        ///    </para>
        /// </summary>
        public HandledEventArgs(bool defaultHandledValue)
        : base()
        {
            _handled = defaultHandledValue;
        }

        /// <summary>
        ///    <para>
        ///       Gets or sets a value
        ///       indicating whether the event is handled.
        ///    </para>
        /// </summary>
        public bool Handled
        {
            get
            {
                return _handled;
            }
            set
            {
                _handled = value;
            }
        }
    }
}
