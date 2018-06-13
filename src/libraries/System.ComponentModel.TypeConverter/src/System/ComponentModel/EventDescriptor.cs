// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    /// Provides a description of an event.
    /// </summary>
    public abstract class EventDescriptor : MemberDescriptor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.EventDescriptor'/> class with the
        /// specified name and attribute array.
        /// </summary>
        protected EventDescriptor(string name, Attribute[] attrs) : base(name, attrs)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.EventDescriptor'/> class with the name and attributes in
        /// the specified <see cref='System.ComponentModel.MemberDescriptor'/>.
        /// </summary>
        protected EventDescriptor(MemberDescriptor descr) : base(descr)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.EventDescriptor'/> class with
        /// the name in the specified <see cref='System.ComponentModel.MemberDescriptor'/> and the
        /// attributes in both the <see cref='System.ComponentModel.MemberDescriptor'/> and the <see cref='System.Attribute'/> array.
        /// </summary>
        protected EventDescriptor(MemberDescriptor descr, Attribute[] attrs) : base(descr, attrs)
        {
        }

        /// <summary>
        /// When overridden in a derived class, gets the type of the component this event is bound to.
        /// </summary>
        public abstract Type ComponentType { get; }

        /// <summary>
        /// When overridden in a derived class, gets the type of delegate for the event.
        /// </summary>
        public abstract Type EventType { get; }

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the event delegate is
        /// a multicast delegate.
        /// </summary>
        public abstract bool IsMulticast { get; }

        /// <summary>
        /// When overridden in a derived class, binds the event to the component.
        /// </summary>
        public abstract void AddEventHandler(object component, Delegate value);

        /// <summary>
        /// When overridden in a derived class, unbinds the delegate from the component
        /// so that the delegate will no longer receive events from the component.
        /// </summary>
        public abstract void RemoveEventHandler(object component, Delegate value);
    }
}
