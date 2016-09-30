// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Security.Permissions;

namespace System.ComponentModel.Design
{
    /// <summary>
    ///    <para>
    ///       Represents a
    ///       numeric Command ID and globally unique
    ///       ID (GUID) menu identifier that together uniquely identify a command.
    ///    </para>
    /// </summary>
    public class CommandID
    {
        private readonly Guid _menuGroup;
        private readonly int _commandID;

        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.Design.CommandID'/>
        ///       class. Creates a new command
        ///       ID.
        ///    </para>
        /// </summary>
        public CommandID(Guid menuGroup, int commandID)
        {
            _menuGroup = menuGroup;
            _commandID = commandID;
        }

        /// <summary>
        ///    <para>
        ///       Gets or sets the numeric command ID.
        ///    </para>
        /// </summary>
        public virtual int ID
        {
            get
            {
                return _commandID;
            }
        }

        /// <summary>
        ///    <para>
        ///       Overrides Object's Equals method.
        ///    </para>
        /// </summary>
        public override bool Equals(object obj)
        {
            if (!(obj is CommandID))
            {
                return false;
            }
            CommandID cid = (CommandID)obj;
            return cid._menuGroup.Equals(_menuGroup) && cid._commandID == _commandID;
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public override int GetHashCode()
        {
            return _menuGroup.GetHashCode() << 2 | _commandID;
        }

        /// <summary>
        ///    <para>
        ///       Gets or sets the globally
        ///       unique ID
        ///       (GUID) of the menu group that the menu command this CommandID
        ///       represents belongs to.
        ///    </para>
        /// </summary>
        public virtual Guid Guid
        {
            get
            {
                return _menuGroup;
            }
        }

        /// <summary>
        ///    <para>
        ///       Overrides Object's ToString method.
        ///    </para>
        /// </summary>
        public override string ToString()
        {
            return _menuGroup.ToString() + " : " + _commandID.ToString(CultureInfo.CurrentCulture);
        }
    }
}
