// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.ComponentModel.Design
{
    /// <summary>
    /// Represents a numeric Command ID and globally unique ID (GUID) menu
    /// identifier that together uniquely identify a command.
    /// </summary>
    public class CommandID
    {
        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.Design.CommandID'/>
        /// class. Creates a new command ID.
        /// </summary>
        public CommandID(Guid menuGroup, int commandID)
        {
            Guid = menuGroup;
            ID = commandID;
        }

        /// <summary>
        /// Gets or sets the numeric command ID.
        /// </summary>
        public virtual int ID { get; }

        /// <summary>
        /// Overrides Object's Equals method.
        /// </summary>
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is CommandID cid && cid.Guid.Equals(Guid) && cid.ID == ID;
        }

        public override int GetHashCode() => Guid.GetHashCode() << 2 | ID;

        /// <summary>
        /// Gets or sets the globally unique ID (GUID) of the menu group that the
        /// menu command this CommandID represents belongs to.
        /// </summary>
        public virtual Guid Guid { get; }

        /// <summary>
        /// Overrides Object's ToString method.
        /// </summary>
        public override string ToString() => $"{Guid} : {ID}";
    }
}
