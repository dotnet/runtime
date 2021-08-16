// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel.Design
{
    /// <summary>
    /// Represents a Windows menu or toolbar item.
    /// </summary>
    public class MenuCommand
    {
        // Events that we suface or call on
        private readonly EventHandler? _execHandler;

        private int _status;
        private IDictionary? _properties;

        /// <summary>
        /// Indicates that the given command is enabled. An enabled command may
        /// be selected by the user (it's not greyed out).
        /// </summary>
        private const int ENABLED = 0x02;  //tagOLECMDF.OLECMDF_ENABLED;

        /// <summary>
        /// Indicates that the given command is not visible on the command bar.
        /// </summary>
        private const int INVISIBLE = 0x10;

        /// <summary>
        /// Indicates that the given command is checked in the "on" state.
        /// </summary>
        private const int CHECKED = 0x04; // tagOLECMDF.OLECMDF_LATCHED;

        /// <summary>
        /// Indicates that the given command is supported. Marking a command
        /// as supported indicates that the shell will not look any further up
        /// the command target chain.
        /// </summary>
        private const int SUPPORTED = 0x01; // tagOLECMDF.OLECMDF_SUPPORTED

        /// <summary>
        /// Initializes a new instance of <see cref='System.ComponentModel.Design.MenuCommand'/>.
        /// </summary>
        public MenuCommand(EventHandler? handler, CommandID? command)
        {
            _execHandler = handler;
            CommandID = command;
            _status = SUPPORTED | ENABLED;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this menu item is checked.
        /// </summary>
        public virtual bool Checked
        {
            get => (_status & CHECKED) != 0;
            set => SetStatus(CHECKED, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this menu item is available.
        /// </summary>
        public virtual bool Enabled
        {
            get => (_status & ENABLED) != 0;
            set => SetStatus(ENABLED, value);
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

        public virtual IDictionary Properties => _properties ?? (_properties = new HybridDictionary());

        /// <summary>
        /// Gets or sets a value indicating whether this menu item is supported.
        /// </summary>
        public virtual bool Supported
        {
            get => (_status & SUPPORTED) != 0;
            set => SetStatus(SUPPORTED, value);
        }

        /// <summary>
        /// Gets or sets a value indicating if this menu item is visible.
        /// </summary>
        public virtual bool Visible
        {
            get => (_status & INVISIBLE) == 0;
            set => SetStatus(INVISIBLE, !value);
        }

        /// <summary>
        /// Occurs when the menu command changes.
        /// </summary>
        public event EventHandler? CommandChanged;


        /// <summary>
        /// Gets the <see cref='System.ComponentModel.Design.CommandID'/> associated with this menu command.
        /// </summary>
        public virtual CommandID? CommandID { get; }

        /// <summary>
        /// Invokes a menu item.
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
        /// Invokes a menu item. The default implementation of this method ignores
        /// the argument, but deriving classes may override this method.
        /// </summary>
        public virtual void Invoke(object arg) => Invoke();

        /// <summary>
        /// Gets the OLE command status code for this menu item.
        /// </summary>
        public virtual int OleStatus => _status;

        /// <summary>
        /// Provides notification and is called in response to
        /// a <see cref='System.ComponentModel.Design.MenuCommand.CommandChanged'/> event.
        /// </summary>
        protected virtual void OnCommandChanged(EventArgs e) => CommandChanged?.Invoke(this, e);

        /// <summary>
        /// Overrides object's ToString().
        /// </summary>
        public override string ToString()
        {
            string str = (CommandID?.ToString() ?? "") + " : ";
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
