// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Collections.Generic;

// TODO when we upgrade to C# V6 you can remove this.  
// warning CS0420: 'P.x': a reference to a volatile field will not be treated as volatile
// This happens when you pass a _subcribers (a volatile field) to interlocked operations (which are byref). 
// This was fixed in C# V6.  
#pragma warning disable 0420

namespace System.Diagnostics
{
    /// <summary>
    /// A DiagnosticListener is something that forwards on events written with DiagnosticSource.
    /// It is an IObservable (has Subscribe method), and it also has a Subscribe overloads that
    /// lets you specify a 'IsEnabled' predicate that users of DiagnosticSource will use for 
    /// 'quick checks'.   
    /// 
    /// The item in the stream is a KeyValuePair[string, object] where the string is the name
    /// of the diagnostic item and the object is the payload (typically an anonymous type).  
    /// 
    /// There may be many DiagnosticListeners in the system, but we encourage the use of
    /// The DiagnosticSource.DefaultSource which goes to the DiagnosticListener.DefaultListener.
    /// 
    /// If you need to see 'everything' you can subscribe to the 'AllListeners' event that
    /// will fire for every live DiagnosticListener in the appdomain (past or present). 
    /// 
    /// Please See the DiagnosticSource Users Guide 
    /// https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/DiagnosticSourceUsersGuide.md
    /// for instructions on its use.  
    /// </summary>
    public class DiagnosticListener : DiagnosticSource, IObservable<KeyValuePair<string, object>>, IDisposable
    {
        /// <summary>
        /// When you subscribe to this you get callbacks for all NotificationListeners in the appdomain
        /// as well as those that occurred in the past, and all future Listeners created in the future. 
        /// </summary>
        public static IObservable<DiagnosticListener> AllListeners
        {
            get
            {
#if ENABLE_HTTP_HANDLER
                GC.KeepAlive(HttpHandlerDiagnosticListener.s_instance);
#endif

                if (s_allListenerObservable == null)
                {
                    s_allListenerObservable = new AllListenerObservable();
                }
                return s_allListenerObservable;
            }
        }

        // Subscription implementation 
        /// <summary>
        /// Add a subscriber (Observer).  If 'IsEnabled' == null (or not present), then the Source's IsEnabled 
        /// will always return true.  
        /// </summary>
        public virtual IDisposable Subscribe(IObserver<KeyValuePair<string, object>> observer, Predicate<string> isEnabled)
        {
            IDisposable subscription;
            if (isEnabled == null)
            {
                subscription = SubscribeInternal(observer, null, null);
            }
            else
            {
                Predicate<string> localIsEnabled = isEnabled;
                subscription = SubscribeInternal(observer, isEnabled, (name, arg1, arg2) => localIsEnabled(name));
            }

            return subscription;
        }

        /// <summary>
        /// Add a subscriber (Observer).  If 'IsEnabled' == null (or not present), then the Source's IsEnabled 
        /// will always return true.  
        /// </summary>
        /// <param name="observer">Subscriber (IObserver)</param>
        /// <param name="isEnabled">Filters events based on their name (string) and context objects that could be null.
        /// Note that producer may first call filter with event name only and null context arguments and filter should
        /// return true if consumer is interested in any of such events. Producers that support 
        /// context-based filtering will invoke isEnabled again with context for more prcise filtering.
        /// Use Subscribe overload with name-based filtering if producer does NOT support context-based filtering</param>
        public virtual IDisposable Subscribe(IObserver<KeyValuePair<string, object>> observer, Func<string, object, object, bool> isEnabled)
        {
            return isEnabled == null ?
             SubscribeInternal(observer, null, null) :
             SubscribeInternal(observer, name => IsEnabled(name, null, null), isEnabled);
        }

        /// <summary>
        /// Same as other Subscribe overload where the predicate is assumed to always return true.  
        /// </summary>
        public virtual IDisposable Subscribe(IObserver<KeyValuePair<string, object>> observer)
        {
            return SubscribeInternal(observer, null, null);
        }

        /// <summary>
        /// Make a new DiagnosticListener, it is a NotificationSource, which means the returned result can be used to 
        /// log notifications, but it also has a Subscribe method so notifications can be forwarded
        /// arbitrarily.  Thus its job is to forward things from the producer to all the listeners
        /// (multi-casting).    Generally you should not be making your own DiagnosticListener but use the
        /// DiagnosticListener.Default, so that notifications are as 'public' as possible.  
        /// </summary>
        public DiagnosticListener(string name)
        {
            Name = name;

            // Insert myself into the list of all Listeners.   
            lock (s_lock)
            {
                // Issue the callback for this new diagnostic listener.
                var allListenerObservable = s_allListenerObservable;
                if (allListenerObservable != null)
                    allListenerObservable.OnNewDiagnosticListener(this);

                // And add it to the list of all past listeners.  
                _next = s_allListeners;
                s_allListeners = this;
            }

            // Touch DiagnosticSourceEventSource.Logger so we ensure that the
            // DiagnosticSourceEventSource has been constructed (and thus is responsive to ETW requests to be enabled).
            GC.KeepAlive(DiagnosticSourceEventSource.Logger);
        }

        /// <summary>
        /// Clean up the NotificationListeners.   Notification listeners do NOT DIE ON THEIR OWN
        /// because they are in a global list (for discoverability).  You must dispose them explicitly. 
        /// Note that we do not do the Dispose(bool) pattern because we frankly don't want to support
        /// subclasses that have non-managed state.   
        /// </summary>
        virtual public void Dispose()
        {
            // Remove myself from the list of all listeners.  
            lock (s_lock)
            {
                if (_disposed)
                {
                    return;
                }
                _disposed = true;
                if (s_allListeners == this)
                    s_allListeners = s_allListeners._next;
                else
                {
                    var cur = s_allListeners;
                    while (cur != null)
                    {
                        if (cur._next == this)
                        {
                            cur._next = _next;
                            break;
                        }
                        cur = cur._next;
                    }
                }
                _next = null;
            }

            // Indicate completion to all subscribers.  
            DiagnosticSubscription subscriber = null;
            Interlocked.Exchange(ref subscriber, _subscriptions);
            while (subscriber != null)
            {
                subscriber.Observer.OnCompleted();
                subscriber = subscriber.Next;
            }
            // The code above also nulled out all subscriptions. 
        }

        /// <summary>
        /// When a DiagnosticListener is created it is given a name.   Return this.  
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Return the name for the ToString() to aid in debugging.  
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Name;
        }

        #region private

        // NotificationSource implementation

        /// <summary>
        /// Determines whether there are any registered subscribers
        /// </summary>
        /// <remarks> If there is an expensive setup for the notification,
        /// you may call IsEnabled() as the first and most efficient check before doing this setup. 
        /// Producers may optionally use this check before IsEnabled(string) in the most performance-critical parts of the system
        /// to ensure somebody listens to the DiagnosticListener at all.</remarks>
        public bool IsEnabled()
        {
            return _subscriptions != null;
        }

        /// <summary>
        /// Override abstract method
        /// </summary>
        public override bool IsEnabled(string name)
        {
            for (DiagnosticSubscription curSubscription = _subscriptions; curSubscription != null; curSubscription = curSubscription.Next)
            {
                if (curSubscription.IsEnabled1Arg == null || curSubscription.IsEnabled1Arg(name))
                    return true;
            }
            return false;
        }

        // NotificationSource implementation
        /// <summary>
        /// Override abstract method
        /// </summary>
        public override bool IsEnabled(string name, object arg1, object arg2 = null)
        {
            for (DiagnosticSubscription curSubscription = _subscriptions; curSubscription != null; curSubscription = curSubscription.Next)
            {
                if (curSubscription.IsEnabled3Arg == null || curSubscription.IsEnabled3Arg(name, arg1, arg2))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Override abstract method
        /// </summary>
        public override void Write(string name, object value)
        {
            for (DiagnosticSubscription curSubscription = _subscriptions; curSubscription != null; curSubscription = curSubscription.Next)
                curSubscription.Observer.OnNext(new KeyValuePair<string, object>(name, value));
        }

        // Note that Subscriptions are READ ONLY.   This means you never update any fields (even on removal!)
        private class DiagnosticSubscription : IDisposable
        {
            internal IObserver<KeyValuePair<string, object>> Observer;

            // IsEnabled1Arg and IsEnabled3Arg represent IsEnabled callbacks. 
            //    - IsEnabled1Arg invoked for DiagnosticSource.IsEnabled(string)
            //    - IsEnabled3Arg invoked for DiagnosticSource.IsEnabled(string, obj, obj)
            // Subscriber MUST set both IsEnabled1Arg and IsEnabled3Arg or none of them:
            //     when Predicate<string> is provided in DiagosticListener.Subscribe,
            //       - IsEnabled1Arg is set to predicate
            //       - IsEnabled3Arg falls back to predicate ignoring extra arguments.
            //     similarly, when Func<string, obj, obj, bool> is provided, 
            //     IsEnabled1Arg falls back to IsEnabled3Arg with null context
            // Thus, dispatching is very efficient when producer and consumer agree on number of IsEnabled arguments
            // Argument number mismatch between producer/consumer adds extra cost of adding or omitting context parameters 
            internal Predicate<string> IsEnabled1Arg;
            internal Func<string, object, object, bool> IsEnabled3Arg;

            internal DiagnosticListener Owner;          // The DiagnosticListener this is a subscription for.  
            internal DiagnosticSubscription Next;                // Linked list of subscribers

            public void Dispose()
            {
                // TO keep this lock free and easy to analyze, the linked list is READ ONLY.   Thus we copy
                for (;;)
                {
                    DiagnosticSubscription subscriptions = Owner._subscriptions;
                    DiagnosticSubscription newSubscriptions = Remove(subscriptions, this);    // Make a new list, with myself removed.  

                    // try to update, but if someone beat us to it, then retry.  
                    if (Interlocked.CompareExchange(ref Owner._subscriptions, newSubscriptions, subscriptions) == subscriptions)
                    {
#if DEBUG
                        var cur = newSubscriptions;
                        while (cur != null)
                        {
                            Debug.Assert(!(cur.Observer == Observer && cur.IsEnabled1Arg == IsEnabled1Arg && cur.IsEnabled3Arg == IsEnabled3Arg), "Did not remove subscription!");
                            cur = cur.Next;
                        }
#endif
                        break;
                    }
                }
            }

            // Create a new linked list where 'subscription has been removed from the linked list of 'subscriptions'. 
            private static DiagnosticSubscription Remove(DiagnosticSubscription subscriptions, DiagnosticSubscription subscription)
            {
                if (subscriptions == null)
                {
                    // May happen if the IDisposable returned from Subscribe is Dispose'd again
                    return null;
                }

                if (subscriptions.Observer == subscription.Observer && 
                    subscriptions.IsEnabled1Arg == subscription.IsEnabled1Arg &&
                    subscriptions.IsEnabled3Arg == subscription.IsEnabled3Arg)
                    return subscriptions.Next;
#if DEBUG
                // Delay a bit.  This makes it more likely that races will happen. 
                for (int i = 0; i < 100; i++)
                    GC.KeepAlive("");
#endif
                return new DiagnosticSubscription() { Observer = subscriptions.Observer, Owner = subscriptions.Owner, IsEnabled1Arg = subscriptions.IsEnabled1Arg, IsEnabled3Arg = subscriptions.IsEnabled3Arg, Next = Remove(subscriptions.Next, subscription) };
            }
        }

        #region AllListenerObservable 
        /// <summary>
        /// Logically AllListenerObservable has a very simple task.  It has a linked list of subscribers that want
        /// a callback when a new listener gets created.   When a new DiagnosticListener gets created it should call 
        /// OnNewDiagnosticListener so that AllListenerObservable can forward it on to all the subscribers.   
        /// </summary>
        private class AllListenerObservable : IObservable<DiagnosticListener>
        {
            public IDisposable Subscribe(IObserver<DiagnosticListener> observer)
            {
                lock (s_lock)
                {
                    // Call back for each existing listener on the new callback (catch-up).   
                    for (DiagnosticListener cur = s_allListeners; cur != null; cur = cur._next)
                        observer.OnNext(cur);

                    // Add the observer to the list of subscribers.   
                    _subscriptions = new AllListenerSubscription(this, observer, _subscriptions);
                    return _subscriptions;
                }
            }

            /// <summary>
            /// Called when a new DiagnosticListener gets created to tell anyone who subscribed that this happened.  
            /// </summary>
            /// <param name="diagnosticListener"></param>
            internal void OnNewDiagnosticListener(DiagnosticListener diagnosticListener)
            {
                Debug.Assert(Monitor.IsEntered(s_lock));     // We should only be called when we hold this lock

                // Simply send a callback to every subscriber that we have a new listener
                for (var cur = _subscriptions; cur != null; cur = cur.Next)
                    cur.Subscriber.OnNext(diagnosticListener);
            }

            #region private 
            /// <summary>
            /// Remove 'subscription from the list of subscriptions that the observable has.   Called when
            /// subscriptions are disposed.   Returns true if the subscription was removed.  
            /// </summary>
            private bool Remove(AllListenerSubscription subscription)
            {
                lock (s_lock)
                {
                    if (_subscriptions == subscription)
                    {
                        _subscriptions = subscription.Next;
                        return true;
                    }
                    else if (_subscriptions != null)
                    {
                        for (var cur = _subscriptions; cur.Next != null; cur = cur.Next)
                        {
                            if (cur.Next == subscription)
                            {
                                cur.Next = cur.Next.Next;
                                return true;
                            }
                        }
                    }

                    // Subscriber likely disposed multiple times
                    return false;
                }
            }

            /// <summary>
            /// One node in the linked list of subscriptions that AllListenerObservable keeps.   It is
            /// IDisposable, and when that is called it removes itself from the list.  
            /// </summary>
            internal class AllListenerSubscription : IDisposable
            {
                internal AllListenerSubscription(AllListenerObservable owner, IObserver<DiagnosticListener> subscriber, AllListenerSubscription next)
                {
                    this._owner = owner;
                    this.Subscriber = subscriber;
                    this.Next = next;
                }

                public void Dispose()
                {
                    if (_owner.Remove(this))
                    {
                        Subscriber.OnCompleted();           // Called outside of a lock
                    }
                }

                private readonly AllListenerObservable _owner;               // the list this is a member of.  
                internal readonly IObserver<DiagnosticListener> Subscriber;
                internal AllListenerSubscription Next;
            }

            private AllListenerSubscription _subscriptions;
            #endregion
        }
        #endregion

        private IDisposable SubscribeInternal(IObserver<KeyValuePair<string, object>> observer, Predicate<string> isEnabled1Arg, Func<string, object, object, bool> isEnabled3Arg)
        {
            // If we have been disposed, we silently ignore any subscriptions.  
            if (_disposed)
            {
                return new DiagnosticSubscription() { Owner = this };
            }
            DiagnosticSubscription newSubscription = new DiagnosticSubscription()
            {
                Observer = observer,
                IsEnabled1Arg = isEnabled1Arg,
                IsEnabled3Arg = isEnabled3Arg,
                Owner = this,
                Next = _subscriptions
            };

            while (Interlocked.CompareExchange(ref _subscriptions, newSubscription, newSubscription.Next) != newSubscription.Next)
                newSubscription.Next = _subscriptions;
            return newSubscription;
        }

        private volatile DiagnosticSubscription _subscriptions;
        private DiagnosticListener _next;               // We keep a linked list of all NotificationListeners (s_allListeners)
        private bool _disposed;                        // Has Dispose been called?

        private static DiagnosticListener s_allListeners;               // linked list of all instances of DiagnosticListeners.  
        private static AllListenerObservable s_allListenerObservable;   // to make callbacks to this object when listeners come into existence. 
        private static object s_lock = new object();                    // A lock for  
#if false
        private static readonly DiagnosticListener s_default = new DiagnosticListener("DiagnosticListener.DefaultListener");
#endif

        #endregion
    }
}
