// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;

// TODO when we upgrade to C# V6 you can remove this.  
// warning CS0420: ‘P.x': a reference to a volatile field will not be treated as volatile
// This happens when you pass a _subcribers (a volatile field) to interlocked operations (which are byref). 
// This was fixed in C# V6.  
#pragma warning disable 0420

namespace System.Diagnostics.Tracing
{
    /// <summary>
    /// A TelemetryListener is something that forwards on events written with TelemetrySource.
    /// It is an IObservable (has Subscribe method), and it also has a Subscribe overload that
    /// lets you specify a 'IsEnabled' predicate that users of TelemetrySource will use for 
    /// 'quick checks'.   
    /// 
    /// The item in the stream is a KeyValuePair[string, object] where the string is the name
    /// of the telemetry item and the object is the payload (typically an anonymous type).  
    /// 
    /// There may be many TelemetryListeners in the system, but we encourage the use of
    /// The TelemetrySource.DefaultSource which goes to the TelemetryListener.DefaultListener.
    /// 
    /// If you need to see 'everything' you can subscribe to the 'AllListeners' event that
    /// will fire for every live TelemetryListener in the appdomain (past or present). 
    /// </summary>
    public class TelemetryListener : TelemetrySource, IObservable<KeyValuePair<string, object>>, IDisposable
    {
        /// <summary>
        /// This is the TelemetryListener that is used by default by the class library.   
        /// Generally you don't want to make your own but rather have everyone use this one, which
        /// ensures that everyone who wished to subscribe gets the callbacks.  
        /// The main reason not to us this one is that you WANT isolation from other 
        /// events in the system (e.g. multi-tenancy).  
        /// </summary>
        public static TelemetryListener DefaultListener { get { return s_default; } }

        /// <summary>
        /// When you subscribe to this you get callbacks for all NotificationListeners in the appdomain
        /// as well as those that occurred in the past, and all future Listeners created in the future. 
        /// </summary>
        public static IObservable<TelemetryListener> AllListeners
        {
            get
            {
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
        virtual public IDisposable Subscribe(IObserver<KeyValuePair<string, object>> observer, Predicate<string> isEnabled)
        {
            // If we have been disposed, we silently ignore any subscriptions.  
            if (_disposed)
            {
                return new TelemetrySubscription() { Owner = this };
            }
            TelemetrySubscription newSubscription = new TelemetrySubscription() { Observer = observer, IsEnabled = isEnabled, Owner = this, Next = _subscriptions };
            while (Interlocked.CompareExchange(ref _subscriptions, newSubscription, newSubscription.Next) != newSubscription.Next)
                newSubscription.Next = _subscriptions;
            return newSubscription;
        }
        /// <summary>
        /// Same as other Subscribe overload where the predicate is assumed to always return true.  
        /// </summary>
        public IDisposable Subscribe(IObserver<KeyValuePair<string, object>> observer)
        {
            return Subscribe(observer, null);
        }

        /// <summary>
        /// Make a new TelemetryListener, it is a NotificationSource, which means the returned result can be used to 
        /// log notifications, but it also has a Subscribe method so notifications can be forwarded
        /// arbitrarily.  Thus its job is to forward things from the producer to all the listeners
        /// (multi-casting).    Generally you should not be making your own TelemetryListener but use the
        /// TelemetryListener.Default, so that notifications are as 'public' as possible.  
        /// </summary>
        public TelemetryListener(string name)
        {
            Name = name;
            // To avoid allocating an explicit lock object I lock the Default TelemetryListener.   However there is a 
            // chicken and egg problem because I need to call this code to initialize TelemetryListener.DefaultListener.      
            var lockObj = DefaultListener;
            if (lockObj == null)
            {
                lockObj = this;
                Debug.Assert(this.Name == "TelemetryListener.DefaultListener");
            }

            // Insert myself into the list of all Listeners.   
            lock (lockObj)
            {
                // Issue the callback for this new telemetry listener.
                var allListenerObservable = s_allListenerObservable;
                if (allListenerObservable != null)
                    allListenerObservable.OnNewTelemetryListener(this);

                // And add it to the list of all past listeners.  
                _next = s_allListeners;
                s_allListeners = this;
            }
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
            lock (DefaultListener)
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
            TelemetrySubscription subscriber = null;
            Interlocked.Exchange(ref subscriber, _subscriptions);
            while (subscriber != null)
            {
                subscriber.Observer.OnCompleted();
                subscriber = subscriber.Next;
            }
            // The code above also nulled out all subscriptions. 
        }

        /// <summary>
        /// When a TelemetryListener is created it is given a name.   Return this.  
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
        /// Override abstract method
        /// </summary>
        public override bool IsEnabled(string telemetryName)
        {
            for (TelemetrySubscription curSubscription = _subscriptions; curSubscription != null; curSubscription = curSubscription.Next)
            {
                if (curSubscription.IsEnabled == null || curSubscription.IsEnabled(telemetryName))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Override abstract method
        /// </summary>
        public override void WriteTelemetry(string telemetryName, object arguments)
        {
            for (TelemetrySubscription curSubscription = _subscriptions; curSubscription != null; curSubscription = curSubscription.Next)
                curSubscription.Observer.OnNext(new KeyValuePair<string, object>(telemetryName, arguments));
        }

        // Note that Subscriptions are READ ONLY.   This means you never update any fields (even on removal!)
        private class TelemetrySubscription : IDisposable
        {
            internal IObserver<KeyValuePair<string, object>> Observer;
            internal Predicate<string> IsEnabled;
            internal TelemetryListener Owner;          // The TelemetryListener this is a subscription for.  
            internal TelemetrySubscription Next;                // Linked list of subscribers

            public void Dispose()
            {
                // TO keep this lock free and easy to analyze, the linked list is READ ONLY.   Thus we copy
                for (;;)
                {
                    TelemetrySubscription subscriptions = Owner._subscriptions;
                    TelemetrySubscription newSubscriptions = Remove(subscriptions, this);    // Make a new list, with myself removed.  

                    // try to update, but if someone beat us to it, then retry.  
                    if (Interlocked.CompareExchange(ref Owner._subscriptions, newSubscriptions, subscriptions) == subscriptions)
                    {
#if DEBUG
                        var cur = newSubscriptions;
                        while (cur != null)
                        {
                            Debug.Assert(!(cur.Observer == Observer && cur.IsEnabled == IsEnabled), "Did not remove subscription!");
                            cur = cur.Next;
                        }
#endif
                        break;
                    }
                }
            }

            // Create a new linked list where 'subscription has been removed from the linked list of 'subscriptions'. 
            private static TelemetrySubscription Remove(TelemetrySubscription subscriptions, TelemetrySubscription subscription)
            {
                if (subscriptions == null)
                {
                    // May happen if the IDisposable returned from Subscribe is Dispose'd again
                    return null;
                }

                if (subscriptions.Observer == subscription.Observer && subscriptions.IsEnabled == subscription.IsEnabled)
                    return subscriptions.Next;
#if DEBUG
                // Delay a bit.  This makes it more likely that races will happen. 
                for (int i = 0; i < 100; i++)
                    GC.KeepAlive("");
#endif 
                return new TelemetrySubscription() { Observer = subscriptions.Observer, Owner = subscriptions.Owner, IsEnabled = subscriptions.IsEnabled, Next = Remove(subscriptions.Next, subscription) };
            }
        }

        #region AllListenerObservable 
        /// <summary>
        /// Logically AllListenerObservable has a very simple task.  It has a linked list of subscribers that want
        /// a callback when a new listener gets created.   When a new TelemetryListener gets created it should call 
        /// OnNewTelemetryListener so that AllListenerObservable can forward it on to all the subscribers.   
        /// </summary>
        private class AllListenerObservable : IObservable<TelemetryListener>
        {
            public IDisposable Subscribe(IObserver<TelemetryListener> observer)
            {
                lock (DefaultListener)
                {
                    // Call back for each existing listener on the new callback (catch-up).   
                    for (TelemetryListener cur = s_allListeners; cur != null; cur = cur._next)
                        observer.OnNext(cur);

                    // Add the observer to the list of subscribers.   
                    _subscriptions = new AllListenerSubscription(this, observer, _subscriptions);
                    return _subscriptions;
                }
            }

            /// <summary>
            /// Called when a new TelemetryListener gets created to tell anyone who subscribed that this happened.  
            /// </summary>
            /// <param name="telemetryListener"></param>
            internal void OnNewTelemetryListener(TelemetryListener telemetryListener)
            {
                Debug.Assert(Monitor.IsEntered(DefaultListener));     // We should only be called when we hold this lock

                // Simply send a callback to every subscriber that we have a new listener
                for (var cur = _subscriptions; cur != null; cur = cur.Next)
                    cur.Subscriber.OnNext(telemetryListener);
            }

            #region private 
            /// <summary>
            /// Remove 'subscription from the list of subscriptions that the observable has.   Called when
            /// subscriptions are disposed.   Returns true if the subscription was removed.  
            /// </summary>
            private bool Remove(AllListenerSubscription subscription)
            {
                lock (DefaultListener)
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
                internal AllListenerSubscription(AllListenerObservable owner, IObserver<TelemetryListener> subscriber, AllListenerSubscription next)
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
                internal readonly IObserver<TelemetryListener> Subscriber;
                internal AllListenerSubscription Next;
            }

            private AllListenerSubscription _subscriptions;
            #endregion 
        }
        #endregion 

        private volatile TelemetrySubscription _subscriptions;
        private TelemetryListener _next;               // We keep a linked list of all NotificationListeners (s_allListeners)
        private bool _disposed;                        // Has Dispose been called?

        private static TelemetryListener s_allListeners;                // linked list of all instances of TelemetryListeners.  
        private static AllListenerObservable s_allListenerObservable;   // to make callbacks to this object when listeners come into existence.  
        private static readonly TelemetryListener s_default = new TelemetryListener("TelemetryListener.DefaultListener");

        #endregion
    }
}
