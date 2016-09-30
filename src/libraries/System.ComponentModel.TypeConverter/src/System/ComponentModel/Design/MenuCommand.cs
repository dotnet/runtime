// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Permissions;

namespace System.ComponentModel.Design
{
    /// <summary>
    ///    <para>
    ///       Represents a Windows
    ///       menu or toolbar item.
    ///    </para>
    /// </summary>
    public class MenuCommand
    {
        // Events that we suface or call on
        //
        private EventHandler _execHandler;
        private EventHandler _statusHandler;

        private CommandID _commandID;
        private int _status;
        private IDictionary _properties;

        /// <summary>
        ///     Indicates that the given command is enabled.  An enabled command may
        ///     be selected by the user (it's not greyed out).
        /// </summary>
        private const int ENABLED = 0x02;  //tagOLECMDF.OLECMDF_ENABLED;

        /// <summary>
        ///     Indicates that the given command is not visible on the command bar.
        /// </summary>
        private const int INVISIBLE = 0x10;

        /// <summary>
        ///     Indicates that the given command is checked in the "on" state.
        /// </summary>
        private const int CHECKED = 0x04; // tagOLECMDF.OLECMDF_LATCHED;

        /// <summary>
        ///     Indicates that the given command is supported.  Marking a command
        ///     as supported indicates that the shell will not look any further up
        ///     the command target chain.
        /// </summary>
        private const int SUPPORTED = 0x01; // tagOLECMDF.OLECMDF_SUPPORTED


        /// <summary>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.ComponentModel.Design.MenuCommand'/>.
        ///    </para>
        /// </summary>
        public MenuCommand(EventHandler handler, CommandID command)
        {
            _execHandler = handler;
            _commandID = command;
            _status = SUPPORTED | ENABLED;
        }

        /// <summary>
        ///    <para> Gets or sets a value indicating whether this menu item is checked.</para>
        /// </summary>
        public virtual bool Checked
        {
            get
            {
                return (_status & CHECKED) != 0;
            }

            set
            {
                SetStatus(CHECKED, value);
            }
        }

        /// <summary>
        ///    <para> Gets or
        ///       sets a value indicating whether this menu item is available.</para>
        /// </summary>
        public virtual bool Enabled
        {
            get
            {
                return (_status & ENABLED) != 0;
            }

            set
            {
                SetStatus(ENABLED, value);
            }
        }

        private void SetStatus(int mask, bool value)
        {
            int newStatus = _status;

            if (value)
            {
                newStatus |= mask;
            }
            else
            {
                newStatus &= ~mask;
            }

            if (newStatus != _status)
            {
                _status = newStatus;
                OnCommandChanged(EventArgs.Empty);
            }
        }

        /// <summary>
        /// </summary>
        public virtual IDictionary Properties
        {
            get
            {
                if (_properties == null)
                {
                    _properties = new HybridDictionary();
                }

                return _properties;
            }
        }




        /// <summary>
        ///    <para> Gets or sets a value
        ///       indicating whether this menu item is supported.</para>
        /// </summary>
        public virtual bool Supported
        {
            get
            {
                return (_status & SUPPORTED) != 0;
            }
            set
            {
                SetStatus(SUPPORTED, value);
            }
        }

        /// <summary>
        ///    <para> Gets or sets a value
        ///       indicating if this menu item is visible.</para>
        /// </summary>
        public virtual bool Visible
        {
            get
            {
                return (_status & INVISIBLE) == 0;
            }
            set
            {
                SetStatus(INVISIBLE, !value);
            }
        }

        /// <summary>
        ///    <para>
        ///       Occurs when the menu command changes.
        ///    </para>
        /// </summary>
        public event EventHandler CommandChanged
        {
            add
            {
                _statusHandler += value;
            }
            remove
            {
                _statusHandler -= value;
            }
        }

        /// <summary>
        /// <para>Gets the <see cref='System.ComponentModel.Design.CommandID'/> associated with this menu command.</para>
        /// </summary>
        public virtual CommandID CommandID
        {
            get
            {
                return _commandID;
            }
        }

        /// <summary>
        ///    <para>
        ///       Invokes a menu item.
        ///    </para>
        /// </summary>
        public virtual void Invoke()
        {
            if (_execHandler != null)
            {
                try
                {
                    _execHandler(this, EventArgs.Empty);
                }
                catch (CheckoutException cxe)
                {
                    if (cxe == CheckoutException.Canceled)
                        return;

                    throw;
                }
            }
        }

        /// <summary>
        ///    <para>
        ///       Invokes a menu item.  The default implementation of this method ignores 
        ///       the argument, but deriving classes may override this method.
        ///    </para>
        /// </summary>
        public virtual void Invoke(object arg)
        {
            Invoke();
        }

        /// <summary>
        ///    <para>
        ///       Gets the OLE command status code for this menu item.
        ///    </para>
        /// </summary>
        public virtual int OleStatus
        {
            get
            {
                return _status;
            }
        }

        /// <summary>
        ///    <para>Provides notification and is called in response to 
        ///       a <see cref='System.ComponentModel.Design.MenuCommand.CommandChanged'/> event.</para>
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2109:ReviewVisibleEventHandlers")] // Safe: FullTrust LinkDemand to instantiate an object of this class.
        protected virtual void OnCommandChanged(EventArgs e)
        {
            if (_statusHandler != null)
            {
                _statusHandler(this, e);
            }
        }

        /// <summary>
        ///    Overrides object's ToString().
        /// </summary>
        public override string ToString()
        {
            string str = CommandID.ToString() + " : ";
            if ((_status & SUPPORTED) != 0)
            {
                str += "Supported";
            }
            if ((_status & ENABLED) != 0)
            {
                str += "|Enabled";
            }
            if ((_status & INVISIBLE) == 0)
            {
                str += "|Visible";
            }
            if ((_status & CHECKED) != 0)
            {
                str += "|Checked";
            }
            return str;
        }
    }
}
