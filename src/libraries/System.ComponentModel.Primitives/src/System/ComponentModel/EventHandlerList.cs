// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>Provides a simple list of delegates. This class cannot be inherited.</para>
    /// </summary>
    public sealed class EventHandlerList : IDisposable
    {
        private ListEntry _head;
        private Component _parent;

        /// <summary>
        ///     Creates a new event handler list.  The parent component is used to check the component's
        ///     CanRaiseEvents property.
        /// </summary>
        internal EventHandlerList(Component parent)
        {
            _parent = parent;
        }

        /// <summary>
        ///    Creates a new event handler list.
        /// </summary>
        public EventHandlerList()
        {
        }

        /// <summary>
        ///    <para>Gets or sets the delegate for the specified key.</para>
        /// </summary>
        public Delegate this[object key]
        {
            get
            {
                ListEntry e = null;
                if (_parent == null || _parent.CanRaiseEventsInternal)
                {
                    e = Find(key);
                }

                if (e != null)
                {
                    return e.handler;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                ListEntry e = Find(key);
                if (e != null)
                {
                    e.handler = value;
                }
                else
                {
                    _head = new ListEntry(key, value, _head);
                }
            }
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public void AddHandler(object key, Delegate value)
        {
            ListEntry e = Find(key);
            if (e != null)
            {
                e.handler = Delegate.Combine(e.handler, value);
            }
            else
            {
                _head = new ListEntry(key, value, _head);
            }
        }

        /// <summary> allows you to add a list of events to this list </summary>
        public void AddHandlers(EventHandlerList listToAddFrom)
        {
            ListEntry currentListEntry = listToAddFrom._head;
            while (currentListEntry != null)
            {
                AddHandler(currentListEntry.key, currentListEntry.handler);
                currentListEntry = currentListEntry.next;
            }
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public void Dispose()
        {
            _head = null;
        }

        private ListEntry Find(object key)
        {
            ListEntry found = _head;
            while (found != null)
            {
                if (found.key == key)
                {
                    break;
                }
                found = found.next;
            }
            return found;
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public void RemoveHandler(object key, Delegate value)
        {
            ListEntry e = Find(key);
            if (e != null)
            {
                e.handler = Delegate.Remove(e.handler, value);
            }
            // else... no error for removal of non-existant delegate
            //
        }

        private sealed class ListEntry
        {
            internal ListEntry next;
            internal object key;
            internal Delegate handler;

            public ListEntry(object key, Delegate handler, ListEntry next)
            {
                this.next = next;
                this.key = key;
                this.handler = handler;
            }
        }
    }
}
