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
    /// <para>Provides data for the <see cref='System.ComponentModel.Design.IComponentChangeService.ComponentChanging'/> event.</para>
    /// </summary>
    public sealed class ComponentChangingEventArgs : EventArgs
    {
        private object _component;
        private MemberDescriptor _member;

        /// <summary>
        ///    <para>
        ///       Gets or sets the component that is being changed or that is the parent container of the member being changed.      
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
        ///       Gets or sets the member of the component that is about to be changed.
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
        ///       Initializes a new instance of the <see cref='System.ComponentModel.Design.ComponentChangingEventArgs'/> class.
        ///    </para>
        /// </summary>
        public ComponentChangingEventArgs(object component, MemberDescriptor member)
        {
            _component = component;
            _member = member;
        }
    }
}
