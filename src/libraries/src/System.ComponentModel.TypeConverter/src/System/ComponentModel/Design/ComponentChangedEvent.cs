// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Permissions;

namespace System.ComponentModel.Design
{
    /// <summary>
    /// <para>Provides data for the <see cref='System.ComponentModel.Design.IComponentChangeService.ComponentChanged'/> event.</para>
    /// </summary>
    public sealed class ComponentChangedEventArgs : EventArgs
    {
        private object _component;
        private MemberDescriptor _member;
        private object _oldValue;
        private object _newValue;

        /// <summary>
        ///    <para>
        ///       Gets or sets the component that is the cause of this event.      
        ///    </para>
        /// </summary>
        public object Component
        {
            get
            {
                return _component;
            }
        }

        /// <summary>
        ///    <para>
        ///       Gets or sets the member that is about to change.      
        ///    </para>
        /// </summary>
        public MemberDescriptor Member
        {
            get
            {
                return _member;
            }
        }

        /// <summary>
        ///    <para>
        ///       Gets or sets the new value of the changed member.
        ///    </para>
        /// </summary>
        public object NewValue
        {
            get
            {
                return _newValue;
            }
        }

        /// <summary>
        ///    <para>
        ///       Gets or sets the old value of the changed member.      
        ///    </para>
        /// </summary>
        public object OldValue
        {
            get
            {
                return _oldValue;
            }
        }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.Design.ComponentChangedEventArgs'/> class.</para>
        /// </summary>
        public ComponentChangedEventArgs(object component, MemberDescriptor member, object oldValue, object newValue)
        {
            _component = component;
            _member = member;
            _oldValue = oldValue;
            _newValue = newValue;
        }
    }
}
