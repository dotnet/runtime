// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <devdoc>
    ///    <para>
    ///       Provides a description
    ///       of an event.
    ///    </para>
    /// </devdoc>
    [HostProtection(SharedState = true)]
    [System.Runtime.InteropServices.ComVisible(true)]
    public abstract class EventDescriptor : MemberDescriptor
    {
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.EventDescriptor'/> class with the
        ///       specified name and attribute
        ///       array.
        ///    </para>
        /// </devdoc>
        protected EventDescriptor(string name, Attribute[] attrs)
            : base(name, attrs)
        {
        }
        
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.EventDescriptor'/> class with the name and attributes in
        ///       the specified <see cref='System.ComponentModel.MemberDescriptor'/>
        ///       .
        ///    </para>
        /// </devdoc>
        protected EventDescriptor(MemberDescriptor descr)
            : base(descr)
        {
        }
        
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.EventDescriptor'/> class with
        ///       the name in the specified <see cref='System.ComponentModel.MemberDescriptor'/> and the
        ///       attributes in both the <see cref='System.ComponentModel.MemberDescriptor'/> and the <see cref='System.Attribute'/>
        ///       array.
        ///    </para>
        /// </devdoc>
        protected EventDescriptor(MemberDescriptor descr, Attribute[] attrs)
            : base(descr, attrs)
        {
        }

        /// <devdoc>
        ///    <para>
        ///       When overridden in a derived
        ///       class,
        ///       gets the type of the component this event is bound to.
        ///    </para>
        /// </devdoc>
        public abstract Type ComponentType { get; }

        /// <devdoc>
        ///    <para>
        ///       When overridden in a derived
        ///       class, gets the type of delegate for the event.
        ///    </para>
        /// </devdoc>
        public abstract Type EventType { get; }

        /// <devdoc>
        ///    <para>
        ///       When overridden in a derived class, gets a value
        ///       indicating whether the event delegate is a multicast
        ///       delegate.
        ///    </para>
        /// </devdoc>
        public abstract bool IsMulticast { get; }

        /// <devdoc>
        ///    <para>
        ///       When overridden in
        ///       a derived class,
        ///       binds the event to the component.
        ///    </para>
        /// </devdoc>
        public abstract void AddEventHandler(object component, Delegate value);

        /// <devdoc>
        ///    <para>
        ///       When
        ///       overridden
        ///       in a derived class, unbinds the delegate from the
        ///       component
        ///       so that the delegate will no
        ///       longer receive events from the component.
        ///    </para>
        /// </devdoc>
        public abstract void RemoveEventHandler(object component, Delegate value);
    }
}
