// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    // Helper functions to manually marshal data between .NET and WinRT
    public static class WindowsRuntimeMarshal
    {
        // Add an event handler to a Windows Runtime style event, such that it can be removed via a delegate
        // lookup at a later time.  This method adds the handler to the add method using the supplied
        // delegate.  It then stores the corresponding token in a dictionary for easy access by RemoveEventHandler
        // later.  Note that the dictionary is indexed by the remove method that will be used for RemoveEventHandler
        // so the removeMethod given here must match the remove method supplied there exactly.
        public static void AddEventHandler<T>(Func<T, EventRegistrationToken> addMethod,
                                              Action<EventRegistrationToken> removeMethod,
                                              T handler)
        {
            if (addMethod == null)
                throw new ArgumentNullException(nameof(addMethod));
            if (removeMethod == null)
                throw new ArgumentNullException(nameof(removeMethod));

            // Managed code allows adding a null event handler, the effect is a no-op.  To match this behavior
            // for WinRT events, we simply ignore attempts to add null.
            if (handler == null)
            {
                return;
            }

            // Delegate to managed event registration implementation or native event registration implementation
            // They have completely different implementation because native side has its own unique problem to solve -
            // there could be more than one RCW for the same COM object
            // it would be more confusing and less-performant if we were to merge them together
            object? target = removeMethod.Target;
            if (target == null || Marshal.IsComObject(target))
                NativeOrStaticEventRegistrationImpl.AddEventHandler<T>(addMethod, removeMethod, handler);
            else
                ManagedEventRegistrationImpl.AddEventHandler<T>(addMethod, removeMethod, handler);
        }

        // Remove the delegate handler from the Windows Runtime style event registration by looking for
        // its token, previously stored via AddEventHandler<T>
        public static void RemoveEventHandler<T>(Action<EventRegistrationToken> removeMethod, T handler)
        {
            if (removeMethod == null)
                throw new ArgumentNullException(nameof(removeMethod));

            // Managed code allows removing a null event handler, the effect is a no-op.  To match this behavior
            // for WinRT events, we simply ignore attempts to remove null.
            if (handler == null)
            {
                return;
            }

            // Delegate to managed event registration implementation or native event registration implementation
            // They have completely different implementation because native side has its own unique problem to solve -
            // there could be more than one RCW for the same COM object
            // it would be more confusing and less-performant if we were to merge them together
            object? target = removeMethod.Target;
            if (target == null || Marshal.IsComObject(target))
                NativeOrStaticEventRegistrationImpl.RemoveEventHandler<T>(removeMethod, handler);
            else
                ManagedEventRegistrationImpl.RemoveEventHandler<T>(removeMethod, handler);
        }

        public static void RemoveAllEventHandlers(Action<EventRegistrationToken> removeMethod)
        {
            if (removeMethod == null)
                throw new ArgumentNullException(nameof(removeMethod));

            // Delegate to managed event registration implementation or native event registration implementation
            // They have completely different implementation because native side has its own unique problem to solve -
            // there could be more than one RCW for the same COM object
            // it would be more confusing and less-performant if we were to merge them together
            object? target = removeMethod.Target;
            if (target == null || Marshal.IsComObject(target))
                NativeOrStaticEventRegistrationImpl.RemoveAllEventHandlers(removeMethod);
            else
                ManagedEventRegistrationImpl.RemoveAllEventHandlers(removeMethod);
        }

        // Returns the total cache size
        // Used by test only to verify we don't leak event cache
        internal static int GetRegistrationTokenCacheSize()
        {
            int count = 0;

            if (ManagedEventRegistrationImpl.s_eventRegistrations != null)
            {
                lock (ManagedEventRegistrationImpl.s_eventRegistrations)
                {
                    foreach (var item in ManagedEventRegistrationImpl.s_eventRegistrations)
                        count++;
                }
            }

            if (NativeOrStaticEventRegistrationImpl.s_eventRegistrations != null)
            {
                lock (NativeOrStaticEventRegistrationImpl.s_eventRegistrations)
                {
                    count += NativeOrStaticEventRegistrationImpl.s_eventRegistrations.Count;
                }
            }

            return count;
        }

        //
        // Optimized version of List of EventRegistrationToken
        // It is made a struct to reduce overhead
        //
        internal struct EventRegistrationTokenList
        {
            private EventRegistrationToken firstToken;     // Optimization for common case where there is only one token
            private List<EventRegistrationToken>? restTokens;     // Rest of the tokens

            internal EventRegistrationTokenList(EventRegistrationToken token)
            {
                firstToken = token;
                restTokens = null;
            }

            // Push a new token into this list
            // Returns true if you need to copy back this list into the dictionary (so that you
            // don't lose change outside the dictionary). false otherwise.
            public bool Push(EventRegistrationToken token)
            {
                bool needCopy = false;

                if (restTokens == null)
                {
                    restTokens = new List<EventRegistrationToken>();
                    needCopy = true;
                }

                restTokens.Add(token);

                return needCopy;
            }

            // Pops the last token
            // Returns false if no more tokens left, true otherwise
            public bool Pop(out EventRegistrationToken token)
            {
                // Only 1 token in this list and we just removed the last token
                if (restTokens == null || restTokens.Count == 0)
                {
                    token = firstToken;
                    return false;
                }

                int last = restTokens.Count - 1;
                token = restTokens[last];
                restTokens.RemoveAt(last);

                return true;
            }

            public void CopyTo(List<EventRegistrationToken> tokens)
            {
                tokens.Add(firstToken);
                if (restTokens != null)
                    tokens.AddRange(restTokens);
            }
        }

        //
        // Event registration support for managed objects events & static events
        //
        internal static class ManagedEventRegistrationImpl
        {
            // Mappings of delegates registered for events -> their registration tokens.
            // These mappings are stored indexed by the remove method which can be used to undo the registrations.
            //
            // The full structure of this table is:
            //   object the event is being registered on ->
            //      Table [RemoveMethod] ->
            //        Table [Handler] -> Token
            //
            // Note: There are a couple of optimizations I didn't do here because they don't make sense for managed events:
            // 1.  Flatten the event cache (see EventCacheKey in native WinRT event implementation below)
            //
            //     This is because managed events use ConditionalWeakTable to hold Objects->(Event->(Handler->Tokens)),
            //     and when object goes away everything else will be nicely cleaned up. If I flatten it like native WinRT events,
            //     I'll have to use Dictionary (as ConditionalWeakTable won't work - nobody will hold the new key alive anymore)
            //     instead, and that means I'll have to add more code from native WinRT events into managed WinRT event to support
            //     self-cleanup in the finalization, as well as reader/writer lock to protect against race conditions in the finalization,
            //     which adds a lot more complexity and doesn't really worth it.
            //
            // 2.  Use conditionalWeakTable to hold Handler->Tokens.
            //
            //     The reason is very simple - managed object use dictionary (see EventRegistrationTokenTable) to hold delegates alive.
            //     If the delegates aren't alive, it means either they have been unsubscribed, or the object itself is gone,
            //     and in either case, they've been already taken care of.
            //
            internal volatile static
                ConditionalWeakTable<object, Dictionary<MethodInfo, Dictionary<object, EventRegistrationTokenList>>> s_eventRegistrations =
                    new ConditionalWeakTable<object, Dictionary<MethodInfo, Dictionary<object, EventRegistrationTokenList>>>();

            internal static void AddEventHandler<T>(Func<T, EventRegistrationToken> addMethod,
                                                  Action<EventRegistrationToken> removeMethod,
                                                  T handler)
            {
                Debug.Assert(addMethod != null);
                Debug.Assert(removeMethod != null);
                Debug.Assert(removeMethod.Target != null);
                Debug.Assert(handler != null);

                // Add the method, and make a note of the token -> delegate mapping.
                object instance = removeMethod.Target;
                Dictionary<object, EventRegistrationTokenList> registrationTokens = GetEventRegistrationTokenTable(instance, removeMethod);
                EventRegistrationToken token = addMethod(handler);
                lock (registrationTokens)
                {
                    EventRegistrationTokenList tokens;
                    if (!registrationTokens.TryGetValue(handler, out tokens))
                    {
                        tokens = new EventRegistrationTokenList(token);
                        registrationTokens[handler] = tokens;
                    }
                    else
                    {
                        bool needCopy = tokens.Push(token);

                        // You need to copy back this list into the dictionary (so that you don't lose change outside dictionary)
                        if (needCopy)
                            registrationTokens[handler] = tokens;
                    }

                    Log("[WinRT_Eventing] Event subscribed for managed instance = " + instance + ", handler = " + handler + "\n");
                }
            }

            // Get the event registration token table for an event.  These are indexed by the remove method of the event.
            private static Dictionary<object, EventRegistrationTokenList> GetEventRegistrationTokenTable(object instance, Action<EventRegistrationToken> removeMethod)
            {
                Debug.Assert(instance != null);
                Debug.Assert(removeMethod != null);
                Debug.Assert(s_eventRegistrations != null);

                lock (s_eventRegistrations)
                {
                    Dictionary<MethodInfo, Dictionary<object, EventRegistrationTokenList>>? instanceMap = null;
                    if (!s_eventRegistrations.TryGetValue(instance, out instanceMap))
                    {
                        instanceMap = new Dictionary<MethodInfo, Dictionary<object, EventRegistrationTokenList>>();
                        s_eventRegistrations.Add(instance, instanceMap);
                    }

                    Dictionary<object, EventRegistrationTokenList>? tokens = null;
                    if (!instanceMap.TryGetValue(removeMethod.Method, out tokens))
                    {
                        tokens = new Dictionary<object, EventRegistrationTokenList>();
                        instanceMap.Add(removeMethod.Method, tokens);
                    }

                    return tokens;
                }
            }

            internal static void RemoveEventHandler<T>(Action<EventRegistrationToken> removeMethod, T handler)
            {
                Debug.Assert(removeMethod != null);
                Debug.Assert(removeMethod.Target != null);
                Debug.Assert(handler != null);

                object instance = removeMethod.Target;
                Dictionary<object, EventRegistrationTokenList> registrationTokens = GetEventRegistrationTokenTable(instance, removeMethod);
                EventRegistrationToken token;

                lock (registrationTokens)
                {
                    EventRegistrationTokenList tokens;

                    // Failure to find a registration for a token is not an error - it's simply a no-op.
                    if (!registrationTokens.TryGetValue(handler, out tokens))
                    {
                        Log("[WinRT_Eventing] no registrationTokens found for instance=" + instance + ", handler= " + handler + "\n");

                        return;
                    }

                    // Select a registration token to unregister
                    // We don't care which one but I'm returning the last registered token to be consistent
                    // with native event registration implementation
                    bool moreItems = tokens.Pop(out token);
                    if (!moreItems)
                    {
                        // Remove it from cache if this list become empty
                        // This must be done because EventRegistrationTokenList now becomes invalid
                        // (mostly because there is no safe default value for EventRegistrationToken to express 'no token')
                        // NOTE: We should try to remove registrationTokens itself from cache if it is empty, otherwise
                        // we could run into a race condition where one thread removes it from cache and another thread adds
                        // into the empty registrationToken table
                        registrationTokens.Remove(handler);
                    }
                }

                removeMethod(token);

                Log("[WinRT_Eventing] Event unsubscribed for managed instance = " + instance + ", handler = " + handler + ", token = " + token.Value + "\n");
            }

            internal static void RemoveAllEventHandlers(Action<EventRegistrationToken> removeMethod)
            {
                Debug.Assert(removeMethod != null);
                Debug.Assert(removeMethod.Target != null);

                object instance = removeMethod.Target;
                Dictionary<object, EventRegistrationTokenList> registrationTokens = GetEventRegistrationTokenTable(instance, removeMethod);

                List<EventRegistrationToken> tokensToRemove = new List<EventRegistrationToken>();

                lock (registrationTokens)
                {
                    // Copy all tokens to tokensToRemove array which later we'll call removeMethod on
                    // outside this lock
                    foreach (EventRegistrationTokenList tokens in registrationTokens.Values)
                    {
                        tokens.CopyTo(tokensToRemove);
                    }

                    // Clear the dictionary - at this point all event handlers are no longer in the cache
                    // but they are not removed yet
                    registrationTokens.Clear();
                    Log("[WinRT_Eventing] Cache cleared for managed instance = " + instance + "\n");
                }

                //
                // Remove all handlers outside the lock
                //
                Log("[WinRT_Eventing] Start removing all events for instance = " + instance + "\n");
                CallRemoveMethods(removeMethod, tokensToRemove);
                Log("[WinRT_Eventing] Finished removing all events for instance = " + instance + "\n");
            }
        }

        //
        // WinRT event registration implementation code
        //
        internal static class NativeOrStaticEventRegistrationImpl
        {
            //
            // Key = (target object, event)
            // We use a key of object+event to save an extra dictionary
            //
            internal struct EventCacheKey : IEquatable<EventCacheKey>
            {
                internal object target;
                internal MethodInfo method;

                public override string ToString()
                {
                    return "(" + target + ", " + method + ")";
                }

                public bool Equals(EventCacheKey other)
                {
                    return (object.Equals(target, other.target) && object.Equals(method, other.method));
                }

                public int GetHashCode(EventCacheKey key)
                {
                    return key.target.GetHashCode() ^ key.method.GetHashCode();
                }
            }

            //
            // EventRegistrationTokenListWithCount
            //
            // A list of EventRegistrationTokens that maintains a count
            //
            // The reason this needs to be a separate class is that we need a finalizer for this class
            // If the delegate is collected, it will take this list away with it (due to dependent handles),
            // and we need to remove the PerInstancEntry from cache
            // See ~EventRegistrationTokenListWithCount for more details
            //
            internal class EventRegistrationTokenListWithCount
            {
                private TokenListCount _tokenListCount;
                private EventRegistrationTokenList _tokenList;

                internal EventRegistrationTokenListWithCount(TokenListCount tokenListCount, EventRegistrationToken token)
                {
                    _tokenListCount = tokenListCount;
                    _tokenListCount.Inc();

                    _tokenList = new EventRegistrationTokenList(token);
                }

                ~EventRegistrationTokenListWithCount()
                {
                    // Decrement token list count
                    // This is need to correctly keep trace of number of tokens for EventCacheKey
                    // and remove it from cache when the token count drop to 0
                    // we don't need to take locks for decrement the count - we only need to take a global
                    // lock when we decide to destroy cache for the IUnknown */type instance
                    Log("[WinRT_Eventing] Finalizing EventRegistrationTokenList for " + _tokenListCount.Key + "\n");
                    _tokenListCount.Dec();
                }

                public void Push(EventRegistrationToken token)
                {
                    // Since EventRegistrationTokenListWithCount is a reference type, there is no need
                    // to copy back. Ignore the return value
                    _tokenList.Push(token);
                }

                public bool Pop(out EventRegistrationToken token)
                {
                    return _tokenList.Pop(out token);
                }

                public void CopyTo(List<EventRegistrationToken> tokens)
                {
                    _tokenList.CopyTo(tokens);
                }
            }

            //
            // Maintains the number of tokens for a particular EventCacheKey
            // TokenListCount is a class for two reasons:
            // 1. Efficient update in the Dictionary to avoid lookup twice to update the value
            // 2. Update token count without taking a global lock. Only takes a global lock when drop to 0
            //
            internal class TokenListCount
            {
                private int _count;
                private EventCacheKey _key;

                internal TokenListCount(EventCacheKey key)
                {
                    _key = key;
                }

                internal EventCacheKey Key
                {
                    get { return _key; }
                }

                internal void Inc()
                {
                    int newCount = Interlocked.Increment(ref _count);
                    Log("[WinRT_Eventing] Incremented TokenListCount for " + _key + ", Value = " + newCount + "\n");
                }

                internal void Dec()
                {
                    // Avoid racing with Add/Remove event entries into the cache
                    // You don't want this removing the key in the middle of a Add/Remove
                    s_eventCacheRWLock.AcquireWriterLock(Timeout.Infinite);
                    try
                    {
                        int newCount = Interlocked.Decrement(ref _count);
                        Log("[WinRT_Eventing] Decremented TokenListCount for " + _key + ", Value = " + newCount + "\n");
                        if (newCount == 0)
                            CleanupCache();
                    }
                    finally
                    {
                        s_eventCacheRWLock.ReleaseWriterLock();
                    }
                }

                private void CleanupCache()
                {
                    // Time to destroy cache for this IUnknown */type instance
                    // because the total token list count has dropped to 0 and we don't have any events subscribed
                    Debug.Assert(s_eventRegistrations != null);

                    Log("[WinRT_Eventing] Removing " + _key + " from cache" + "\n");
                    s_eventRegistrations.Remove(_key);
                    Log("[WinRT_Eventing] s_eventRegistrations size = " + s_eventRegistrations.Count + "\n");
                }
            }

            internal struct EventCacheEntry
            {
                // [Handler] -> Token
                internal ConditionalWeakTable<object, EventRegistrationTokenListWithCount> registrationTable;

                // Maintains current total count for the EventRegistrationTokenListWithCount for this event cache key
                internal TokenListCount tokenListCount;
            }

            // Mappings of delegates registered for events -> their registration tokens.
            // These mappings are stored indexed by the remove method which can be used to undo the registrations.
            //
            // The full structure of this table is:
            //   EventCacheKey (instanceKey, eventMethod) -> EventCacheEntry (Handler->tokens)
            //
            // A InstanceKey is the IUnknown * or static type instance
            //
            // Couple of things to note:
            // 1. We need to use IUnknown* because we want to be able to unscribe to the event for another RCW
            // based on the same COM object. For example:
            //    m_canvas.GetAt(0).Event += Func;
            //    m_canvas.GetAt(0).Event -= Func;  // GetAt(0) might create a new RCW
            //
            // 2. Handler->Token is a ConditionalWeakTable because we don't want to keep the delegate alive
            // and we want EventRegistrationTokenListWithCount to be finalized after the delegate is no longer alive
            // 3. It is possible another COM object is created at the same address
            // before the entry in cache is destroyed. More specifically,
            //   a. The same delegate is being unsubscribed. In this case we'll give them a
            //   stale token - unlikely to be a problem
            //   b. The same delegate is subscribed then unsubscribed. We need to make sure give
            //   them the latest token in this case. This is guaranteed by always giving the last token and always use equality to
            //   add/remove event handlers
            internal volatile static Dictionary<EventCacheKey, EventCacheEntry> s_eventRegistrations =
                new Dictionary<EventCacheKey, EventCacheEntry>();

            // Prevent add/remove handler code to run at the same with with cache cleanup code
            private volatile static MyReaderWriterLock s_eventCacheRWLock = new MyReaderWriterLock();

            // Get InstanceKey to use in the cache
            private static object GetInstanceKey(Action<EventRegistrationToken> removeMethod)
            {
                object? target = removeMethod.Target;
                Debug.Assert(target == null || Marshal.IsComObject(target), "Must be null or a RCW");
                if (target == null)
                    return removeMethod.Method.DeclaringType!;

                // Need the "Raw" IUnknown pointer for the RCW that is not bound to the current context
                return (object)Marshal.GetRawIUnknownForComObjectNoAddRef(target);
            }

            private static object? FindEquivalentKeyUnsafe(ConditionalWeakTable<object, EventRegistrationTokenListWithCount> registrationTable, object handler, out EventRegistrationTokenListWithCount? tokens)
            {
                foreach (KeyValuePair<object, EventRegistrationTokenListWithCount> item in registrationTable)
                {
                    if (object.Equals(item.Key, handler))
                    {
                        tokens = item.Value;
                        return item.Key;
                    }
                }
                tokens = null;
                return null;
            }

            internal static void AddEventHandler<T>(Func<T, EventRegistrationToken> addMethod,
                                                  Action<EventRegistrationToken> removeMethod,
                                                  T handler)
            {
                Debug.Assert(handler != null);

                // The instanceKey will be IUnknown * of the target object
                object instanceKey = GetInstanceKey(removeMethod);

                // Call addMethod outside of RW lock
                // At this point we don't need to worry about race conditions and we can avoid deadlocks
                // if addMethod waits on finalizer thread
                // If we later throw we need to remove the method
                EventRegistrationToken token = addMethod(handler);

                bool tokenAdded = false;

                try
                {
                    EventRegistrationTokenListWithCount? tokens;

                    //
                    // The whole add/remove code has to be protected by a reader/writer lock
                    // Add/Remove cannot run at the same time with cache cleanup but Add/Remove can run at the same time
                    //
                    s_eventCacheRWLock.AcquireReaderLock(Timeout.Infinite);
                    try
                    {
                        // Add the method, and make a note of the delegate -> token mapping.
                        TokenListCount? tokenListCount;
                        ConditionalWeakTable<object, EventRegistrationTokenListWithCount> registrationTokens = GetOrCreateEventRegistrationTokenTable(instanceKey, removeMethod, out tokenListCount);
                        lock (registrationTokens)
                        {
                            //
                            // We need to find the key that equals to this handler
                            // Suppose we have 3 handlers A, B, C that are equal (refer to the same object and method),
                            // the first handler (let's say A) will be used as the key and holds all the tokens.
                            // We don't need to hold onto B and C, because the COM object itself will keep them alive,
                            // and they won't die anyway unless the COM object dies or they get unsubscribed.
                            // It may appear that it is fine to hold A, B, C, and add them and their corresponding tokens
                            // into registrationTokens table. However, this is very dangerous, because this COM object
                            // may die, but A, B, C might not get collected yet, and another COM object comes into life
                            // with the same IUnknown address, and we subscribe event B. In this case, the right token
                            // will be added into B's token list, but once we unsubscribe B, we might end up removing
                            // the last token in C, and that may lead to crash.
                            //
                            object? key = FindEquivalentKeyUnsafe(registrationTokens, handler, out tokens);
                            if (key == null)
                            {
                                tokens = new EventRegistrationTokenListWithCount(tokenListCount, token);
                                registrationTokens.Add(handler, tokens);
                            }
                            else
                            {
                                tokens!.Push(token);
                            }

                            tokenAdded = true;
                        }
                    }
                    finally
                    {
                        s_eventCacheRWLock.ReleaseReaderLock();
                    }

                    Log("[WinRT_Eventing] Event subscribed for instance = " + instanceKey + ", handler = " + handler + "\n");
                }
                catch (Exception)
                {
                    // If we've already added the token and go there, we don't need to "UNDO" anything
                    if (!tokenAdded)
                    {
                        // Otherwise, "Undo" addMethod if any exception occurs
                        // There is no need to cleanup our data structure as we haven't added the token yet
                        removeMethod(token);
                    }


                    throw;
                }
            }

            private static ConditionalWeakTable<object, EventRegistrationTokenListWithCount>? GetEventRegistrationTokenTableNoCreate(object instance, Action<EventRegistrationToken> removeMethod, out TokenListCount? tokenListCount)
            {
                Debug.Assert(instance != null);
                Debug.Assert(removeMethod != null);

                return GetEventRegistrationTokenTableInternal(instance, removeMethod, out tokenListCount, /* createIfNotFound = */ false);
            }

            private static ConditionalWeakTable<object, EventRegistrationTokenListWithCount> GetOrCreateEventRegistrationTokenTable(object instance, Action<EventRegistrationToken> removeMethod, out TokenListCount tokenListCount)
            {
                Debug.Assert(instance != null);
                Debug.Assert(removeMethod != null);

                return GetEventRegistrationTokenTableInternal(instance, removeMethod, out tokenListCount!, /* createIfNotFound = */ true)!;
            }

            // Get the event registration token table for an event.  These are indexed by the remove method of the event.
            private static ConditionalWeakTable<object, EventRegistrationTokenListWithCount>? GetEventRegistrationTokenTableInternal(object instance, Action<EventRegistrationToken> removeMethod, out TokenListCount? tokenListCount, bool createIfNotFound)
            {
                Debug.Assert(instance != null);
                Debug.Assert(removeMethod != null);
                Debug.Assert(s_eventRegistrations != null);

                EventCacheKey eventCacheKey;
                eventCacheKey.target = instance;
                eventCacheKey.method = removeMethod.Method;

                lock (s_eventRegistrations)
                {
                    EventCacheEntry eventCacheEntry;
                    if (!s_eventRegistrations.TryGetValue(eventCacheKey, out eventCacheEntry))
                    {
                        if (!createIfNotFound)
                        {
                            // No need to create an entry in this case
                            tokenListCount = null;
                            return null;
                        }

                        Log("[WinRT_Eventing] Adding (" + instance + "," + removeMethod.Method + ") into cache" + "\n");

                        eventCacheEntry = new EventCacheEntry();
                        eventCacheEntry.registrationTable = new ConditionalWeakTable<object, EventRegistrationTokenListWithCount>();
                        eventCacheEntry.tokenListCount = new TokenListCount(eventCacheKey);

                        s_eventRegistrations.Add(eventCacheKey, eventCacheEntry);
                    }

                    tokenListCount = eventCacheEntry.tokenListCount;

                    return eventCacheEntry.registrationTable;
                }
            }

            internal static void RemoveEventHandler<T>(Action<EventRegistrationToken> removeMethod, T handler)
            {
                Debug.Assert(handler != null);

                object instanceKey = GetInstanceKey(removeMethod);

                EventRegistrationToken token;

                //
                // The whole add/remove code has to be protected by a reader/writer lock
                // Add/Remove cannot run at the same time with cache cleanup but Add/Remove can run at the same time
                //
                s_eventCacheRWLock.AcquireReaderLock(Timeout.Infinite);
                try
                {
                    TokenListCount? tokenListCount;
                    ConditionalWeakTable<object, EventRegistrationTokenListWithCount>? registrationTokens = GetEventRegistrationTokenTableNoCreate(instanceKey, removeMethod, out tokenListCount);
                    if (registrationTokens == null)
                    {
                        // We have no information regarding this particular instance (IUnknown*/type) - just return
                        // This is necessary to avoid leaking empty dictionary/conditionalWeakTables for this instance
                        Log("[WinRT_Eventing] no registrationTokens found for instance=" + instanceKey + ", handler= " + handler + "\n");
                        return;
                    }

                    lock (registrationTokens)
                    {
                        EventRegistrationTokenListWithCount? tokens;

                        // Note:
                        // When unsubscribing events, we allow subscribing the event using a different delegate
                        // (but with the same object/method), so we need to find the first delegate that matches
                        // and unsubscribe it
                        // It actually doesn't matter which delegate - as long as it matches
                        // Note that inside TryGetValueWithValueEquality we assumes that any delegate
                        // with the same value equality would have the same hash code
                        object? key = FindEquivalentKeyUnsafe(registrationTokens, handler, out tokens);
                        Debug.Assert((key != null && tokens != null) || (key == null && tokens == null),
                                        "key and tokens must be both null or non-null");
                        if (tokens == null)
                        {
                            // Failure to find a registration for a token is not an error - it's simply a no-op.
                            Log("[WinRT_Eventing] no token list found for instance=" + instanceKey + ", handler= " + handler + "\n");
                            return;
                        }

                        // Select a registration token to unregister
                        // Note that we need to always get the last token just in case another COM object
                        // is created at the same address before the entry for the old one goes away.
                        // See comments above s_eventRegistrations for more details
                        bool moreItems = tokens.Pop(out token);

                        // If the last token is removed from token list, we need to remove it from the cache
                        // otherwise FindEquivalentKeyUnsafe may found this empty token list even though there could be other
                        // equivalent keys in there with non-0 token list
                        if (!moreItems)
                        {
                            // Remove it from (handler)->(tokens)
                            // NOTE: We should not check whether registrationTokens has 0 entries and remove it from the cache
                            // (just like managed event implementation), because this might have raced with the finalizer of
                            // EventRegistrationTokenList
                            registrationTokens.Remove(key!);
                        }

                        Log("[WinRT_Eventing] Event unsubscribed for managed instance = " + instanceKey + ", handler = " + handler + ", token = " + token.Value + "\n");
                    }
                }
                finally
                {
                    s_eventCacheRWLock.ReleaseReaderLock();
                }

                // Call removeMethod outside of RW lock
                // At this point we don't need to worry about race conditions and we can avoid deadlocks
                // if removeMethod waits on finalizer thread
                removeMethod(token);
            }

            internal static void RemoveAllEventHandlers(Action<EventRegistrationToken> removeMethod)
            {
                object instanceKey = GetInstanceKey(removeMethod);

                List<EventRegistrationToken> tokensToRemove = new List<EventRegistrationToken>();

                //
                // The whole add/remove code has to be protected by a reader/writer lock
                // Add/Remove cannot run at the same time with cache cleanup but Add/Remove can run at the same time
                //
                s_eventCacheRWLock.AcquireReaderLock(Timeout.Infinite);
                try
                {
                    ConditionalWeakTable<object, EventRegistrationTokenListWithCount>? registrationTokens = GetEventRegistrationTokenTableNoCreate(instanceKey, removeMethod, out _);
                    if (registrationTokens == null)
                    {
                        // We have no information regarding this particular instance (IUnknown*/type) - just return
                        // This is necessary to avoid leaking empty dictionary/conditionalWeakTables for this instance
                        return;
                    }

                    lock (registrationTokens)
                    {
                        // Copy all tokens to tokensToRemove array which later we'll call removeMethod on
                        // outside this lock
                        foreach (KeyValuePair<object, EventRegistrationTokenListWithCount> item in registrationTokens)
                        {
                            item.Value.CopyTo(tokensToRemove);
                        }

                        // Clear the table - at this point all event handlers are no longer in the cache
                        // but they are not removed yet
                        registrationTokens.Clear();
                        Log("[WinRT_Eventing] Cache cleared for managed instance = " + instanceKey + "\n");
                    }
                }
                finally
                {
                    s_eventCacheRWLock.ReleaseReaderLock();
                }

                //
                // Remove all handlers outside the lock
                //
                Log("[WinRT_Eventing] Start removing all events for instance = " + instanceKey + "\n");
                CallRemoveMethods(removeMethod, tokensToRemove);
                Log("[WinRT_Eventing] Finished removing all events for instance = " + instanceKey + "\n");
            }


            internal class ReaderWriterLockTimedOutException : ApplicationException
            {
            }

            /// Discussed @ https://blogs.msdn.microsoft.com/vancem/2006/03/29/analysis-of-reader-writer-lock/
            ///
            /// <summary>
            /// A reader-writer lock implementation that is intended to be simple, yet very
            /// efficient.  In particular only 1 interlocked operation is taken for any lock
            /// operation (we use spin locks to achieve this).  The spin lock is never held
            /// for more than a few instructions (in particular, we never call event APIs
            /// or in fact any non-trivial API while holding the spin lock).
            ///
            /// Currently this ReaderWriterLock does not support recursion, however it is
            /// not hard to add
            /// </summary>
            internal class MyReaderWriterLock
            {
                // Lock specifiation for myLock:  This lock protects exactly the local fields associted
                // instance of MyReaderWriterLock.  It does NOT protect the memory associted with the
                // the events that hang off this lock (eg writeEvent, readEvent upgradeEvent).
                private int myLock;

                // Who owns the lock owners > 0 => readers
                // owners = -1 means there is one writer.  Owners must be >= -1.
                private int owners;

                // These variables allow use to avoid Setting events (which is expensive) if we don't have to.
                private uint numWriteWaiters;        // maximum number of threads that can be doing a WaitOne on the writeEvent
                private uint numReadWaiters;         // maximum number of threads that can be doing a WaitOne on the readEvent

                // conditions we wait on.
                private EventWaitHandle? writeEvent;    // threads waiting to acquire a write lock go here.
                private EventWaitHandle? readEvent;     // threads waiting to acquire a read lock go here (will be released in bulk)

                internal MyReaderWriterLock()
                {
                    // All state can start out zeroed.
                }

                internal void AcquireReaderLock(int millisecondsTimeout)
                {
                    EnterMyLock();
                    for (;;)
                    {
                        // We can enter a read lock if there are only read-locks have been given out
                        // and a writer is not trying to get in.
                        if (owners >= 0 && numWriteWaiters == 0)
                        {
                            // Good case, there is no contention, we are basically done
                            owners++;       // Indicate we have another reader
                            break;
                        }

                        // Drat, we need to wait.  Mark that we have waiters and wait.
                        if (readEvent == null)      // Create the needed event
                        {
                            LazyCreateEvent(ref readEvent, false);
                            continue;   // since we left the lock, start over.
                        }

                        WaitOnEvent(readEvent, ref numReadWaiters, millisecondsTimeout);
                    }
                    ExitMyLock();
                }

                internal void AcquireWriterLock(int millisecondsTimeout)
                {
                    EnterMyLock();
                    for (;;)
                    {
                        if (owners == 0)
                        {
                            // Good case, there is no contention, we are basically done
                            owners = -1;    // indicate we have a writer.
                            break;
                        }

                        // Drat, we need to wait.  Mark that we have waiters and wait.
                        if (writeEvent == null)     // create the needed event.
                        {
                            LazyCreateEvent(ref writeEvent, true);
                            continue;   // since we left the lock, start over.
                        }

                        WaitOnEvent(writeEvent, ref numWriteWaiters, millisecondsTimeout);
                    }
                    ExitMyLock();
                }

                internal void ReleaseReaderLock()
                {
                    EnterMyLock();
                    Debug.Assert(owners > 0, "ReleasingReaderLock: releasing lock and no read lock taken");
                    --owners;
                    ExitAndWakeUpAppropriateWaiters();
                }

                internal void ReleaseWriterLock()
                {
                    EnterMyLock();
                    Debug.Assert(owners == -1, "Calling ReleaseWriterLock when no write lock is held");
                    owners++;
                    ExitAndWakeUpAppropriateWaiters();
                }

                /// <summary>
                /// A routine for lazily creating a event outside the lock (so if errors
                /// happen they are outside the lock and that we don't do much work
                /// while holding a spin lock).  If all goes well, reenter the lock and
                /// set 'waitEvent'
                /// </summary>
                private void LazyCreateEvent(ref EventWaitHandle? waitEvent, bool makeAutoResetEvent)
                {
                    Debug.Assert(myLock != 0, "Lock must be held");
                    Debug.Assert(waitEvent == null, "Wait event must be null");

                    ExitMyLock();
                    EventWaitHandle newEvent;
                    if (makeAutoResetEvent)
                        newEvent = new AutoResetEvent(false);
                    else
                        newEvent = new ManualResetEvent(false);
                    EnterMyLock();
                    if (waitEvent == null)          // maybe someone snuck in.
                        waitEvent = newEvent;
                }

                /// <summary>
                /// Waits on 'waitEvent' with a timeout of 'millisceondsTimeout.
                /// Before the wait 'numWaiters' is incremented and is restored before leaving this routine.
                /// </summary>
                private void WaitOnEvent(EventWaitHandle waitEvent, ref uint numWaiters, int millisecondsTimeout)
                {
                    Debug.Assert(myLock != 0, "Lock must be held");

                    waitEvent.Reset();
                    numWaiters++;

                    bool waitSuccessful = false;
                    ExitMyLock();      // Do the wait outside of any lock
                    try
                    {
                        if (!waitEvent.WaitOne(millisecondsTimeout, false))
                            throw new ReaderWriterLockTimedOutException();

                        waitSuccessful = true;
                    }
                    finally
                    {
                        EnterMyLock();
                        --numWaiters;
                        if (!waitSuccessful)        // We are going to throw for some reason.  Exit myLock.
                            ExitMyLock();
                    }
                }

                /// <summary>
                /// Determines the appropriate events to set, leaves the locks, and sets the events.
                /// </summary>
                private void ExitAndWakeUpAppropriateWaiters()
                {
                    Debug.Assert(myLock != 0, "Lock must be held");

                    if (owners == 0 && numWriteWaiters > 0)
                    {
                        ExitMyLock();      // Exit before signaling to improve efficiency (wakee will need the lock)
                        writeEvent!.Set(); // release one writer.  Must be non-null if there were waiters.
                    }
                    else if (owners >= 0 && numReadWaiters != 0)
                    {
                        ExitMyLock();    // Exit before signaling to improve efficiency (wakee will need the lock)
                        readEvent!.Set();  // release all readers.  Must be non-null if there were waiters.
                    }
                    else
                    {
                        ExitMyLock();
                    }
                }

                private void EnterMyLock()
                {
                    if (Interlocked.CompareExchange(ref myLock, 1, 0) != 0)
                        EnterMyLockSpin();
                }

                private void EnterMyLockSpin()
                {
                    for (int i = 0; ; i++)
                    {
                        if (i < 3 && Environment.ProcessorCount > 1)
                            Thread.SpinWait(20);    // Wait a few dozen instructions to let another processor release lock.
                        else
                            Thread.Sleep(0);        // Give up my quantum.

                        if (Interlocked.CompareExchange(ref myLock, 1, 0) == 0)
                            return;
                    }
                }
                private void ExitMyLock()
                {
                    Debug.Assert(myLock != 0, "Exiting spin lock that is not held");
                    myLock = 0;
                }
            };
        }

        //
        // Call removeMethod on each token and aggregate all exceptions thrown from removeMethod into one in case of failure
        //
        internal static void CallRemoveMethods(Action<EventRegistrationToken> removeMethod, List<EventRegistrationToken> tokensToRemove)
        {
            List<Exception> exceptions = new List<Exception>();

            foreach (EventRegistrationToken token in tokensToRemove)
            {
                try
                {
                    removeMethod(token);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }

                Log("[WinRT_Eventing] Event unsubscribed for token = " + token.Value + "\n");
            }

            if (exceptions.Count > 0)
                throw new AggregateException(exceptions.ToArray());
        }

        internal static unsafe string HStringToString(IntPtr hstring)
        {
            Debug.Assert(Environment.IsWinRTSupported);

            // There is no difference between a null and empty HSTRING
            if (hstring == IntPtr.Zero)
            {
                return string.Empty;
            }

            unsafe
            {
                uint length;
                char* rawBuffer = UnsafeNativeMethods.WindowsGetStringRawBuffer(hstring, &length);
                return new string(rawBuffer, 0, checked((int)length));
            }
        }

        internal static Exception GetExceptionForHR(int hresult, Exception? innerException, string? messageResource)
        {
            Exception? e = null;
            if (innerException != null)
            {
                string? message = innerException.Message;
                if (message == null && messageResource != null)
                {
                    message = SR.GetResourceString(messageResource);
                }
                e = new Exception(message, innerException);
            }
            else
            {
                string? message = (messageResource != null ? SR.GetResourceString(messageResource): null);
                e = new Exception(message);
            }

            e.HResult = hresult;
            return e;
        }

        internal static Exception GetExceptionForHR(int hresult, Exception? innerException)
        {
            return GetExceptionForHR(hresult, innerException, null);
        }

        private static bool s_haveBlueErrorApis = true;

        private static bool RoOriginateLanguageException(int error, string message, IntPtr languageException)
        {
            if (s_haveBlueErrorApis)
            {
                try
                {
                    return UnsafeNativeMethods.RoOriginateLanguageException(error, message, languageException);
                }
                catch (EntryPointNotFoundException)
                {
                    s_haveBlueErrorApis = false;
                }
            }

            return false;
        }

        private static void RoReportUnhandledError(IRestrictedErrorInfo error)
        {
            if (s_haveBlueErrorApis)
            {
                try
                {
                    UnsafeNativeMethods.RoReportUnhandledError(error);
                }
                catch (EntryPointNotFoundException)
                {
                    s_haveBlueErrorApis = false;
                }
            }
        }

        private static Guid s_iidIErrorInfo = new Guid(0x1CF2B120, 0x547D, 0x101B, 0x8E, 0x65, 0x08, 0x00, 0x2B, 0x2B, 0xD1, 0x19);

        /// <summary>
        /// Report that an exception has occurred which went user unhandled.  This allows the global error handler
        /// for the application to be invoked to process the error.
        /// </summary>
        /// <returns>true if the error was reported, false if not (ie running on Win8)</returns>
        internal static bool ReportUnhandledError(Exception? e)
        {
            // Only report to the WinRT global exception handler in modern apps
            if (!ApplicationModel.IsUap)
            {
                return false;
            }

            // If we don't have the capability to report to the global error handler, early out
            if (!s_haveBlueErrorApis)
            {
                return false;
            }

            if (e != null)
            {
                IntPtr exceptionIUnknown = IntPtr.Zero;
                IntPtr exceptionIErrorInfo = IntPtr.Zero;
                try
                {
                    // Get an IErrorInfo for the current exception and originate it as a langauge error in order to have
                    // Windows generate an IRestrictedErrorInfo corresponding to the exception object.  We can then
                    // notify the global error handler that this IRestrictedErrorInfo instance represents an exception that
                    // went unhandled in managed code.
                    //
                    // Note that we need to get an IUnknown for the exception object and then QI for IErrorInfo since Exception
                    // doesn't implement IErrorInfo in managed code - only its CCW does.
                    exceptionIUnknown = Marshal.GetIUnknownForObject(e);
                    if (exceptionIUnknown != IntPtr.Zero)
                    {
                        Marshal.QueryInterface(exceptionIUnknown, ref s_iidIErrorInfo, out exceptionIErrorInfo);
                        if (exceptionIErrorInfo != IntPtr.Zero)
                        {
                            if (RoOriginateLanguageException(Marshal.GetHRForException(e), e.Message, exceptionIErrorInfo))
                            {
                                IRestrictedErrorInfo restrictedError = UnsafeNativeMethods.GetRestrictedErrorInfo();
                                if (restrictedError != null)
                                {
                                    RoReportUnhandledError(restrictedError);
                                    return true;
                                }
                            }
                        }
                    }
                }
                finally
                {
                    if (exceptionIErrorInfo != IntPtr.Zero)
                    {
                        Marshal.Release(exceptionIErrorInfo);
                    }

                    if (exceptionIUnknown != IntPtr.Zero)
                    {
                        Marshal.Release(exceptionIUnknown);
                    }
                }
            }

            // If we got here, then some step of the marshaling failed, which means the GEH was not invoked
            return false;
        }

#if FEATURE_COMINTEROP_WINRT_MANAGED_ACTIVATION
        // Get an IActivationFactory * for a managed type
        internal static IntPtr GetActivationFactoryForType(Type type)
        {
            ManagedActivationFactory activationFactory = GetManagedActivationFactory(type);
            return Marshal.GetComInterfaceForObject(activationFactory, typeof(IActivationFactory));
        }

        internal static ManagedActivationFactory GetManagedActivationFactory(Type type)
        {
            ManagedActivationFactory activationFactory = new ManagedActivationFactory(type);

            // If the type has any associated factory interfaces (i.e. supports non-default activation
            // or has statics), the CCW for this instance of ManagedActivationFactory must support them.
            InitializeManagedWinRTFactoryObject(activationFactory, (RuntimeType)type);
            return activationFactory;
        }


#endif // FEATURE_COMINTEROP_WINRT_MANAGED_ACTIVATION

        //
        // Get activation factory object for a specified WinRT type
        // If the WinRT type is a native type, we'll always create a unique RCW for it,
        // This is necessary because WinRT factories are often implemented as a singleton,
        // and getting back a RCW for such WinRT factory would usually get back a RCW from
        // another apartment, even if the interface pointe returned from GetActivationFactory
        // is a raw pointer. As a result, user would randomly get back RCWs for activation
        // factories from other apartments and make transiton to those apartments and cause
        // deadlocks and create objects in incorrect apartments
        //
        public static IActivationFactory GetActivationFactory(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (type.IsWindowsRuntimeObject && type.IsImport)
            {
                return (IActivationFactory)GetNativeActivationFactory(type);
            }
            else
            {
#if FEATURE_COMINTEROP_WINRT_MANAGED_ACTIVATION
                return GetManagedActivationFactory(type);
#else
                // Managed factories are not supported so as to minimize public surface (and test effort)
                throw new NotSupportedException();
#endif
            }
        }

        // HSTRING marshaling methods:

        public static IntPtr StringToHString(string s)
        {
            if (!Environment.IsWinRTSupported)
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_WinRT);

            if (s == null)
                throw new ArgumentNullException(nameof(s));

            unsafe
            {
                IntPtr hstring;
                int hrCreate = UnsafeNativeMethods.WindowsCreateString(s, s.Length, &hstring);
                Marshal.ThrowExceptionForHR(hrCreate, new IntPtr(-1));
                return hstring;
            }
        }

        public static string PtrToStringHString(IntPtr ptr)
        {
            if (!Environment.IsWinRTSupported)
            {
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_WinRT);
            }

            return HStringToString(ptr);
        }

        public static void FreeHString(IntPtr ptr)
        {
            if (!Environment.IsWinRTSupported)
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_WinRT);

            if (ptr != IntPtr.Zero)
            {
                UnsafeNativeMethods.WindowsDeleteString(ptr);
            }
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern object GetUniqueObjectForIUnknownWithoutUnboxing(IntPtr unknown);
        
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void InitializeWrapper(object o, ref IntPtr pUnk);
        
        /// <summary>
        /// Converts the CLR exception to an HRESULT. This function also sets
        /// up an IErrorInfo for the exception.
        /// This function is only used in WinRT and converts ObjectDisposedException
        /// to RO_E_CLOSED
        /// </summary>
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern int GetHRForException(Exception e);

        
#if FEATURE_COMINTEROP_WINRT_MANAGED_ACTIVATION
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void InitializeManagedWinRTFactoryObject(object o, RuntimeType runtimeClassType);
#endif

        /// <summary>
        /// Create activation factory and wraps it with a unique RCW.
        /// </summary>
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern object GetNativeActivationFactory(Type type);

        [Conditional("_LOGGING")]
        private static void Log(string s)
        {
            // Internal.Console.WriteLine(s);
        }
    }
}
