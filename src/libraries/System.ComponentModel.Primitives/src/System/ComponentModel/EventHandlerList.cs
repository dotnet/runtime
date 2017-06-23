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
        internal EventHandlerList(Component parent) => _parent = parent;

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

                return e?._handler;
            }
            set
            {
                ListEntry e = Find(key);
                if (e != null)
                {
                    e._handler = value;
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
                e._handler = Delegate.Combine(e._handler, value);
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
                AddHandler(currentListEntry._key, currentListEntry._handler);
                currentListEntry = currentListEntry._next;
            }
        }

        public void Dispose() => _head = null;

        private ListEntry Find(object key)
        {
            ListEntry found = _head;
            while (found != null)
            {
                if (found._key == key)
                {
                    break;
                }
                found = found._next;
            }
            return found;
        }

        public void RemoveHandler(object key, Delegate value)
        {
            ListEntry e = Find(key);
            if (e != null)
            {
                e._handler = Delegate.Remove(e._handler, value);
            }
            // else... no error for removal of non-existent delegate
            //
        }

        private sealed class ListEntry
        {
            internal ListEntry _next;
            internal object _key;
            internal Delegate _handler;

            public ListEntry(object key, Delegate handler, ListEntry next)
            {
                _next = next;
                _key = key;
                _handler = handler;
            }
        }
    }
}
