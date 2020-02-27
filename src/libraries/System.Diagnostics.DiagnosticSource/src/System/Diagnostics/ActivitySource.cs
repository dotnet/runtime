// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Collections.Generic;

namespace System.Diagnostics
{
    public sealed class ActivitySource : IDisposable
    {
        private static SynchronizedList<ActivitySource> s_activeSources = new SynchronizedList<ActivitySource>();
        private static SynchronizedList<ActivityListener> s_listeners = new SynchronizedList<ActivityListener>();
        private SynchronizedList<ActivityListener>? _listeners;

        private ActivitySource() { throw new InvalidOperationException(); }

        /// <summary>
        /// Construct an ActivitySource object with the input name
        /// </summary>
        /// <param name="name">The name of the ActivitySource object</param>
        public ActivitySource(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
            s_activeSources.Add(this);

            s_listeners.EnumWithAction(listener => {
                if (listener.EnableListening(name))
                {
                    AddActivityListener(listener);
                }
            });
        }

        /// <summary>
        /// Returns the ActivitySource name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Creates a new <see cref="Activity"/> object if there is any listener to the Activity creation event, returns null otherwise.
        /// </summary>
        /// <returns>The created <see cref="Activity"/> object or null if there is no any event listener.</returns>
        public Activity? StartActivity() => StartActivity(default, null, default);

        /// <summary>
        /// Creates a new <see cref="Activity"/> object if there is any listener to the Activity events, returns null otherwise.
        /// </summary>
        /// <param name="context">The <see cref="ActivityContext"/> object to initialize the created Activity object with.</param>
        /// <param name="links">The optional <see cref="ActivityLink"/> list to initialize the created Activity object with.</param>
        /// <param name="startTime">The optional start timestamp to set on the created Activity object.</param>
        /// <returns>The created <see cref="Activity"/> object or null if there is no any event listener.</returns>
        public Activity? StartActivity(ActivityContext context, IEnumerable<ActivityLink>? links = null, DateTimeOffset startTime = default)
        {
            // _listeners can get assigned to null in Dispose.
            SynchronizedList<ActivityListener>? listeners = _listeners;
            if (listeners == null || listeners.Count == 0)
            {
                return null;
            }

            Activity? activity = null;

            listeners.EnumWithAction(listener => {
                if (listener.ShouldCreateActivity(Name, context, links))
                {
                    if (activity == null)
                    {
                        activity = Activity.CreateAndStart(this, context, links, startTime);
                    }

                    listener.OnActivityStarted(activity);
                }
            });

            return activity;
        }

        /// <summary>
        /// Dispose the ActivitySource object and remove the current instance from the global list.
        /// </summary>
        public void Dispose()
        {
            s_activeSources.Remove(this);
            _listeners = null;
        }

        public static void AddListener(ActivityListener listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            s_listeners.AddIfNotExist(listener);

            s_activeSources.EnumWithAction(source => {
                if (listener.EnableListening(source.Name))
                {
                    source.AddActivityListener(listener);
                }
            });
        }

        internal static void DetachListener(ActivityListener listener)
        {
            if (s_listeners.Remove(listener))
            {
                s_activeSources.EnumWithAction(source => source.RemoveActivityListener(listener));
            }
        }

        private void AddActivityListener(ActivityListener listener)
        {
            if (_listeners == null)
            {
                Interlocked.CompareExchange(ref _listeners, new SynchronizedList<ActivityListener>(), null);
            }

            // _listeners can get assigned to null in Dispose.
            SynchronizedList<ActivityListener> listeners = _listeners;
            if (listeners != null)
            {
                listeners.AddIfNotExist(listener);
            }
        }

        private void RemoveActivityListener(ActivityListener listener)
        {
            Debug.Assert(listener != null);

            // _listeners can get assigned to null in Dispose.
            SynchronizedList<ActivityListener>? listeners = _listeners;
            if (listeners != null)
            {
                listeners.Remove(listener);
            }
        }

        internal void NotifyActivityStop(Activity activity)
        {
            Debug.Assert(activity != null);

            // _listeners can get assigned to null in Dispose.
            SynchronizedList<ActivityListener>? listeners = _listeners;
            if (listeners != null)
            {
                listeners.EnumWithAction(listener => listener.OnActivityStopped(activity));
            }
        }
    }


    // SynchronizedList<T> is a helper collection which ensure thread safety on the collection
    // and allow enumerating the collection items and execute some action on the enumerated item and can detect any change in the collection
    // during the enumeration which force restarting the enumeration again.
    // Causion: We can have teh action executed on the same item more than once which is ok in our scenarios.
    internal class SynchronizedList<T>
    {
        private List<T> _list;
        private uint _version;

        public SynchronizedList() => _list = new List<T>();

        public void Add(T item)
        {
            lock (_list)
            {
                _list.Add(item);
                _version++;
            }
        }

        public void AddIfNotExist(T item)
        {
            lock (_list)
            {
                if (!_list.Contains(item))
                {
                    _list.Add(item);
                    _version++;
                }
            }
        }

        public bool Remove(T item)
        {
            lock (_list)
            {
                if (_list.Remove(item))
                {
                    _version++;
                    return true;
                }
                return false;
            }
        }

        public int Count => _list.Count;

        public void EnumWithAction(Action<T> action)
        {
            uint version = _version;
            int index = 0;

            while (index < _list.Count)
            {
                T item;
                lock (_list)
                {
                    if (version != _version)
                    {
                        version = _version;
                        index = 0;
                        continue;
                    }

                    item = _list[index];
                    index++;
                }

                // Important to call the action outside the lock.
                // This is the whole point we are having this wrapper class.
                action(item);
            }
        }
    }
}
