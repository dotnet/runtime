// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    // An event registration token table stores mappings from delegates to event tokens, in order to support
    // sourcing WinRT style events from managed code.
    public sealed class EventRegistrationTokenTable<T> where T : class
    {
        // Note this dictionary is also used as the synchronization object for this table
        private Dictionary<EventRegistrationToken, T> m_tokens = new Dictionary<EventRegistrationToken, T>();

        // Cached multicast delegate which will invoke all of the currently registered delegates.  This
        // will be accessed frequently in common coding paterns, so we don't want to calculate it repeatedly.
        private volatile T m_invokeList = null!; // TODO-NULLABLE-GENERIC

        public EventRegistrationTokenTable()
        {
            // T must be a delegate type, but we cannot constrain on being a delegate.  Therefore, we'll do a
            // static check at construction time
            if (!typeof(Delegate).IsAssignableFrom(typeof(T)))
            {
                throw new InvalidOperationException(SR.Format(SR.InvalidOperation_EventTokenTableRequiresDelegate, typeof (T)));
            }
        }

        // The InvocationList property provides access to a delegate which will invoke every registered event handler
        // in this table.  If the property is set, the new value will replace any existing token registrations.
        public T InvocationList
        {
            get
            {
                return m_invokeList;
            }

            set
            {
                lock (m_tokens)
                {
                    // The value being set replaces any of the existing values
                    m_tokens.Clear();
                    m_invokeList = null!; // TODO-NULLABLE-GENERIC

                    if (value != null)
                    {
                        AddEventHandlerNoLock(value);
                    }
                }
            }
        }

        public EventRegistrationToken AddEventHandler(T handler)
        {
            // Windows Runtime allows null handlers.  Assign those a token value of 0 for easy identity
            if (handler == null)
            {
                return new EventRegistrationToken(0);
            }

            lock (m_tokens)
            {
                return AddEventHandlerNoLock(handler);
            }
        }

        private EventRegistrationToken AddEventHandlerNoLock(T handler)
        {
            Debug.Assert(handler != null);

            // Get a registration token, making sure that we haven't already used the value.  This should be quite
            // rare, but in the case it does happen, just keep trying until we find one that's unused.
            EventRegistrationToken token = GetPreferredToken(handler);
            while (m_tokens.ContainsKey(token))
            {
                token = new EventRegistrationToken(token.Value + 1);
            }
            m_tokens[token] = handler;

            // Update the current invocation list to include the newly added delegate
            Delegate? invokeList = (Delegate?)(object?)m_invokeList;
            invokeList = MulticastDelegate.Combine(invokeList, (Delegate)(object)handler);
            m_invokeList = (T)(object?)invokeList!; // TODO-NULLABLE-GENERIC

            return token;
        }

        // Generate a token that may be used for a particular event handler.  We will frequently be called
        // upon to look up a token value given only a delegate to start from.  Therefore, we want to make
        // an initial token value that is easily determined using only the delegate instance itself.  Although
        // in the common case this token value will be used to uniquely identify the handler, it is not
        // the only possible token that can represent the handler.
        //
        // This means that both:
        //  * if there is a handler assigned to the generated initial token value, it is not necessarily
        //    this handler.
        //  * if there is no handler assigned to the generated initial token value, the handler may still
        //    be registered under a different token
        //
        // Effectively the only reasonable thing to do with this value is either to:
        //  1. Use it as a good starting point for generating a token for handler
        //  2. Use it as a guess to quickly see if the handler was really assigned this token value
        private static EventRegistrationToken GetPreferredToken(T handler)
        {
            Debug.Assert(handler != null);

            // We want to generate a token value that has the following properties:
            //  1. is quickly obtained from the handler instance
            //  2. uses bits in the upper 32 bits of the 64 bit value, in order to avoid bugs where code
            //     may assume the value is realy just 32 bits
            //  3. uses bits in the bottom 32 bits of the 64 bit value, in order to ensure that code doesn't
            //     take a dependency on them always being 0.
            //
            // The simple algorithm chosen here is to simply assign the upper 32 bits the metadata token of the
            // event handler type, and the lower 32 bits the hash code of the handler instance itself. Using the
            // metadata token for the upper 32 bits gives us at least a small chance of being able to identify a
            // totally corrupted token if we ever come across one in a minidump or other scenario.
            //
            // The hash code of a unicast delegate is not tied to the method being invoked, so in the case
            // of a unicast delegate, the hash code of the target method is used instead of the full delegate
            // hash code.
            //
            // While calculating this initial value will be somewhat more expensive than just using a counter
            // for events that have few registrations, it will also gives us a shot at preventing unregistration
            // from becoming an O(N) operation.
            //
            // We should feel free to change this algorithm as other requirements / optimizations become
            // available.  This implementation is sufficiently random that code cannot simply guess the value to
            // take a dependency upon it.  (Simply applying the hash-value algorithm directly won't work in the
            // case of collisions, where we'll use a different token value).

            uint handlerHashCode = 0;
            Delegate[] invocationList = ((Delegate)(object)handler).GetInvocationList();
            if (invocationList.Length == 1)
            {
                handlerHashCode = (uint)invocationList[0].Method.GetHashCode();
            }
            else
            {
                handlerHashCode = (uint)handler.GetHashCode();
            }

            ulong tokenValue = ((ulong)(uint)typeof(T).MetadataToken << 32) | handlerHashCode;
            return new EventRegistrationToken(tokenValue);
        }

        // Remove the event handler from the table and 
        // Get the delegate associated with an event registration token if it exists
        // If the event registration token is not registered, returns false
        public bool RemoveEventHandler(EventRegistrationToken token, out T handler)
        {
            lock (m_tokens)
            {
                if (m_tokens.TryGetValue(token, out handler))
                {
                    RemoveEventHandlerNoLock(token);
                    return true;
                }
            }

            return false;
        }

        public void RemoveEventHandler(EventRegistrationToken token)
        {
            // The 0 token is assigned to null handlers, so there's nothing to do
            if (token.Value == 0)
            {
                return;
            }

            lock (m_tokens)
            {
                RemoveEventHandlerNoLock(token);
            }
        }

        public void RemoveEventHandler(T handler)
        {
            // To match the Windows Runtime behaivor when adding a null handler, removing one is a no-op
            if (handler == null)
            {
                return;
            }

            lock (m_tokens)
            {
                // Fast path - if the delegate is stored with its preferred token, then there's no need to do
                // a full search of the table for it.  Note that even if we find something stored using the
                // preferred token value, it's possible we have a collision and another delegate was using that
                // value.  Therefore we need to make sure we really have the handler we want before taking the
                // fast path.
                EventRegistrationToken preferredToken = GetPreferredToken(handler);
                T registeredHandler;
                if (m_tokens.TryGetValue(preferredToken, out registeredHandler))
                {
                    if (registeredHandler == handler)
                    {
                        RemoveEventHandlerNoLock(preferredToken);
                        return;
                    }
                }

                // Slow path - we didn't find the delegate with its preferred token, so we need to fall
                // back to a search of the table
                foreach (KeyValuePair<EventRegistrationToken, T> registration in m_tokens)
                {
                    if (registration.Value == (T)(object)handler)
                    {
                        RemoveEventHandlerNoLock(registration.Key);

                        // If a delegate has been added multiple times to handle an event, then it
                        // needs to be removed the same number of times to stop handling the event.
                        // Stop after the first one we find.
                        return;
                    }
                }

                // Note that falling off the end of the loop is not an error, as removing a registration
                // for a handler that is not currently registered is simply a no-op
            }
        }

        private void RemoveEventHandlerNoLock(EventRegistrationToken token)
        {
            T handler;
            if (m_tokens.TryGetValue(token, out handler))
            {
                m_tokens.Remove(token);

                // Update the current invocation list to remove the delegate
                Delegate? invokeList = (Delegate?)(object?)m_invokeList;
                invokeList = MulticastDelegate.Remove(invokeList, (Delegate?)(object?)handler);
                m_invokeList = (T)(object?)invokeList!; // TODO-NULLABLE-GENERIC
            }
        }

        public static EventRegistrationTokenTable<T> GetOrCreateEventRegistrationTokenTable(ref EventRegistrationTokenTable<T>? refEventTable)
        {
            if (refEventTable == null)
            {
                Interlocked.CompareExchange(ref refEventTable, new EventRegistrationTokenTable<T>(), null);
            }
            return refEventTable!; // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/26761
        }
    }
}
