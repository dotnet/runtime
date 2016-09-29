// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Permissions;

namespace System.ComponentModel.Design.Serialization
{
    /// <summary>
    ///     EventArgs for the ResolveNameEventHandler.  This event is used
    ///     by the serialization process to match a name to an object
    ///     instance.
    /// </summary>
    public class ResolveNameEventArgs : EventArgs
    {
        private string _name;
        private object _value;

        /// <summary>
        ///     Creates a new resolve name event args object.
        /// </summary>
        public ResolveNameEventArgs(string name)
        {
            _name = name;
            _value = null;
        }

        /// <summary>
        ///     The name of the object that needs to be resolved.
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }
        }

        /// <summary>
        ///     The object that matches the name.
        /// </summary>
        public object Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
            }
        }
    }
}

