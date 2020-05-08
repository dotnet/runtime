// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Collections.Generic;

namespace System.Diagnostics
{
    public sealed class ActivitySource : IDisposable
    {
        private static readonly SynchronizedList<ActivitySource> s_activeSources = new SynchronizedList<ActivitySource>();
        private static readonly SynchronizedList<ActivityListener> s_allListeners = new SynchronizedList<ActivityListener>();
        private SynchronizedList<ActivityListener>? _listeners;

        /// <summary>
        /// Construct an ActivitySource object with the input name
        /// </summary>
        /// <param name="name">The name of the ActivitySource object</param>
        /// <param name="version">The version of the component publishing the tracing info.</param>
        public ActivitySource(string name, string? version = "")
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
            Version = version;

            s_activeSources.Add(this);

            if (s_allListeners.Count > 0)
            {
                s_allListeners.EnumWithAction(listener => {
                    var shouldListenTo = listener.ShouldListenTo;
                    if (shouldListenTo != null && shouldListenTo(this))
                    {
                        AddListener(listener);
                    }
                });
            }
        }

        /// <summary>
        /// Returns the ActivitySource name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Returns the ActivitySource version.
        /// </summary>
        public string? Version { get; }

        /// <summary>
        /// Check if there is any listeners for this ActivitySource.
        /// This property can be helpful to tell if there is no listener, then no need to create Activity object
        /// and avoid creating the objects needed to create Activity (e.g. ActivityContext)
        /// Example of that is http scenario which can avoid reading the context data from the wire.
        /// </summary>
        public bool HasListeners()
        {
            SynchronizedList<ActivityListener>? listeners = _listeners;
            return listeners != null && listeners.Count > 0;
        }

        /// <summary>
        /// Creates a new <see cref="Activity"/> object if there is any listener to the Activity, returns null otherwise.
        /// </summary>
        /// <param name="name">The operation name of the Activity</param>
        /// <param name="kind">The <see cref="ActivityKind"/></param>
        /// <returns>The created <see cref="Activity"/> object or null if there is no any event listener.</returns>
        public Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal)
            => StartActivity(name, kind, default, null, null, null, default);

        /// <summary>
        /// Creates a new <see cref="Activity"/> object if there is any listener to the Activity events, returns null otherwise.
        /// </summary>
        /// <param name="name">The operation name of the Activity.</param>
        /// <param name="kind">The <see cref="ActivityKind"/></param>
        /// <param name="parentContext">The parent <see cref="ActivityContext"/> object to initialize the created Activity object with.</param>
        /// <param name="tags">The optional tags list to initialize the created Activity object with.</param>
        /// <param name="links">The optional <see cref="ActivityLink"/> list to initialize the created Activity object with.</param>
        /// <param name="startTime">The optional start timestamp to set on the created Activity object.</param>
        /// <returns>The created <see cref="Activity"/> object or null if there is no any listener.</returns>
        public Activity? StartActivity(string name, ActivityKind kind, ActivityContext parentContext, IEnumerable<KeyValuePair<string, string?>>? tags = null, IEnumerable<ActivityLink>? links = null, DateTimeOffset startTime = default)
            => StartActivity(name, kind, parentContext, null, tags, links, startTime);

        /// <summary>
        /// Creates a new <see cref="Activity"/> object if there is any listener to the Activity events, returns null otherwise.
        /// </summary>
        /// <param name="name">The operation name of the Activity.</param>
        /// <param name="kind">The <see cref="ActivityKind"/></param>
        /// <param name="parentId">The parent Id to initialize the created Activity object with.</param>
        /// <param name="tags">The optional tags list to initialize the created Activity object with.</param>
        /// <param name="links">The optional <see cref="ActivityLink"/> list to initialize the created Activity object with.</param>
        /// <param name="startTime">The optional start timestamp to set on the created Activity object.</param>
        /// <returns>The created <see cref="Activity"/> object or null if there is no any listener.</returns>
        public Activity? StartActivity(string name, ActivityKind kind, string parentId, IEnumerable<KeyValuePair<string, string?>>? tags = null, IEnumerable<ActivityLink>? links = null, DateTimeOffset startTime = default)
            => StartActivity(name, kind, default, parentId, tags, links, startTime);

        private Activity? StartActivity(string name, ActivityKind kind, ActivityContext context, string? parentId, IEnumerable<KeyValuePair<string, string?>>? tags, IEnumerable<ActivityLink>? links, DateTimeOffset startTime)
        {
            // _listeners can get assigned to null in Dispose.
            SynchronizedList<ActivityListener>? listeners = _listeners;
            if (listeners == null || listeners.Count == 0)
            {
                return null;
            }

            Activity? activity = null;

            ActivityDataRequest dateRequest = ActivityDataRequest.None;

            if (parentId != null)
            {
                listeners.EnumWithFunc(listener => {
                    var getRequestedDataUsingParentId = listener.GetRequestedDataUsingParentId;
                    if (getRequestedDataUsingParentId != null)
                    {
                        ActivityCreationOptions<string> aco = new ActivityCreationOptions<string>(this, name, parentId, kind, tags, links);
                        ActivityDataRequest dr = getRequestedDataUsingParentId(ref aco);
                        if (dr > dateRequest)
                        {
                            dateRequest = dr;
                        }

                        // Stop the enumeration if we get the max value RecordingAndSampling.
                        return dateRequest != ActivityDataRequest.AllDataAndRecorded;
                    }
                    return true;
                });
            }
            else
            {
                listeners.EnumWithFunc(listener => {
                    var getRequestedDataUsingContext = listener.GetRequestedDataUsingContext;
                    if (getRequestedDataUsingContext != null)
                    {
                        ActivityCreationOptions<ActivityContext> aco = new ActivityCreationOptions<ActivityContext>(this, name, context, kind, tags, links);
                        ActivityDataRequest dr = getRequestedDataUsingContext(ref aco);
                        if (dr > dateRequest)
                        {
                            dateRequest = dr;
                        }

                        // Stop the enumeration if we get the max value RecordingAndSampling.
                        return dateRequest != ActivityDataRequest.AllDataAndRecorded;
                    }
                    return true;
                });
            }

            if (dateRequest != ActivityDataRequest.None)
            {
                activity = Activity.CreateAndStart(this, name, kind, parentId, context, tags, links, startTime, dateRequest);
                listeners.EnumWithAction(listener => {
                    var activityStarted = listener.ActivityStarted;
                    if (activityStarted != null)
                    {
                        activityStarted(activity);
                    }
                });
            }

            return activity;
        }

        /// <summary>
        /// Dispose the ActivitySource object and remove the current instance from the global list. empty the listeners list too.
        /// </summary>
        public void Dispose()
        {
            _listeners = null;
            s_activeSources.Remove(this);
        }

        /// <summary>
        /// Add a listener to the <see cref="Activity"/> starting and stopping events.
        /// </summary>
        /// <param name="listener"> The <see cref="ActivityListener"/> object to use for listeneing to the <see cref="Activity"/> events.</param>
        public static void AddActivityListener(ActivityListener listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            if (s_allListeners.AddIfNotExist(listener))
            {
                s_activeSources.EnumWithAction(source => {
                    var shouldListenTo = listener.ShouldListenTo;
                    if (shouldListenTo != null && shouldListenTo(source))
                    {
                        source.AddListener(listener);
                    }
                });
            }
        }

        internal void AddListener(ActivityListener listener)
        {
            if (_listeners == null)
            {
                Interlocked.CompareExchange(ref _listeners, new SynchronizedList<ActivityListener>(), null);
            }

            _listeners.AddIfNotExist(listener);
        }

        internal static void DetachListener(ActivityListener listener)
        {
            s_allListeners.Remove(listener);

            s_activeSources.EnumWithAction(source => {
                var listeners = source._listeners;
                listeners?.Remove(listener);
            });
        }

        internal void NotifyActivityStart(Activity activity)
        {
            Debug.Assert(activity != null);

            // _listeners can get assigned to null in Dispose.
            SynchronizedList<ActivityListener>? listeners = _listeners;
            if (listeners != null &&  listeners.Count > 0)
            {
                listeners.EnumWithAction(listener => listener.ActivityStarted?.Invoke(activity));
            }
        }

        internal void NotifyActivityStop(Activity activity)
        {
            Debug.Assert(activity != null);

            // _listeners can get assigned to null in Dispose.
            SynchronizedList<ActivityListener>? listeners = _listeners;
            if (listeners != null &&  listeners.Count > 0)
            {
                listeners.EnumWithAction(listener => {
                    listeners.EnumWithAction(listener => listener.ActivityStopped?.Invoke(activity));
                });
            }
        }
    }

    // SynchronizedList<T> is a helper collection which ensure thread safety on the collection
    // and allow enumerating the collection items and execute some action on the enumerated item and can detect any change in the collection
    // during the enumeration which force restarting the enumeration again.
    // Caution: We can have the action executed on the same item more than once which is ok in our scenarios.
    internal class SynchronizedList<T>
    {
        private readonly List<T> _list;
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

        public bool AddIfNotExist(T item)
        {
            lock (_list)
            {
                if (!_list.Contains(item))
                {
                    _list.Add(item);
                    _version++;
                    return true;
                }
                return false;
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

        public void EnumWithFunc(Func<T, bool> func)
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

                // Important to call the func outside the lock.
                // This is the whole point we are having this wrapper class.
                if (!func(item))
                {
                    break;
                }
            }
        }

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
