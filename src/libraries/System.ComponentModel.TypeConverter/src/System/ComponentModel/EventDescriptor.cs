// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>
    ///       Provides a description of an event.
    ///    </para>
    /// </summary>
    public abstract class EventDescriptor : MemberDescriptor
    {
        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.EventDescriptor'/> class with the
        ///       specified name and attribute array.
        ///    </para>
        /// </summary>
        protected EventDescriptor(string name, Attribute[] attrs)
            : base(name, attrs)
        {
        }

        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.EventDescriptor'/> class with the name and attributes in
        ///       the specified <see cref='System.ComponentModel.MemberDescriptor'/>.
        ///    </para>
        /// </summary>
        protected EventDescriptor(MemberDescriptor descr)
            : base(descr)
        {
        }

        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.EventDescriptor'/> class with
        ///       the name in the specified <see cref='System.ComponentModel.MemberDescriptor'/> and the
        ///       attributes in both the <see cref='System.ComponentModel.MemberDescriptor'/> and the <see cref='System.Attribute'/> array.
        ///    </para>
        /// </summary>
        protected EventDescriptor(MemberDescriptor descr, Attribute[] attrs)
            : base(descr, attrs)
        {
        }

        /// <summary>
        ///    <para>
        ///       When overridden in a derived class, gets the type of the component this event is bound to.
        ///    </para>
        /// </summary>
        public abstract Type ComponentType { get; }

        /// <summary>
        ///    <para>
        ///       When overridden in a derived class, gets the type of delegate for the event.
        ///    </para>
        /// </summary>
        public abstract Type EventType { get; }

        /// <summary>
        ///    <para>
        ///       When overridden in a derived class, gets a value
        ///       indicating whether the event delegate is a multicast
        ///       delegate.
        ///    </para>
        /// </summary>
        public abstract bool IsMulticast { get; }

        /// <summary>
        ///    <para>
        ///       When overridden in a derived class, binds the event to the component.
        ///    </para>
        /// </summary>
        public abstract void AddEventHandler(object component, Delegate value);

        /// <summary>
        ///    <para>
        ///       When overridden in a derived class, unbinds the delegate from the component
        ///       so that the delegate will no longer receive events from the component.
        ///    </para>
        /// </summary>
        public abstract void RemoveEventHandler(object component, Delegate value);
    }
}
