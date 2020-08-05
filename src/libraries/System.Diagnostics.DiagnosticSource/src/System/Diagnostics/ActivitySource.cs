// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
                s_allListeners.EnumWithAction((listener, source) =>
                {
                    Func<ActivitySource, bool>? shouldListenTo = listener.ShouldListenTo;
                    if (shouldListenTo != null)
                    {
                        var activitySource = (ActivitySource)source;
                        if (shouldListenTo(activitySource))
                        {
                            activitySource.AddListener(listener);
                        }
                    }
                }, this);
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
        public Activity? StartActivity(string name, ActivityKind kind, ActivityContext parentContext, IEnumerable<KeyValuePair<string, object?>>? tags = null, IEnumerable<ActivityLink>? links = null, DateTimeOffset startTime = default)
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
        public Activity? StartActivity(string name, ActivityKind kind, string parentId, IEnumerable<KeyValuePair<string, object?>>? tags = null, IEnumerable<ActivityLink>? links = null, DateTimeOffset startTime = default)
            => StartActivity(name, kind, default, parentId, tags, links, startTime);

        private Activity? StartActivity(string name, ActivityKind kind, ActivityContext context, string? parentId, IEnumerable<KeyValuePair<string, object?>>? tags, IEnumerable<ActivityLink>? links, DateTimeOffset startTime)
        {
            // _listeners can get assigned to null in Dispose.
            SynchronizedList<ActivityListener>? listeners = _listeners;
            if (listeners == null || listeners.Count == 0)
            {
                return null;
            }

            Activity? activity = null;

            ActivityDataRequest dataRequest = ActivityDataRequest.None;
            bool? useContext = default;
            ActivityCreationOptions<ActivityContext> optionsWithContext = default;

            if (parentId != null)
            {
                var aco = new ActivityCreationOptions<string>(this, name, parentId, kind, tags, links);
                listeners.EnumWithFunc((ActivityListener listener, ref ActivityCreationOptions<string> data, ref ActivityDataRequest request, ref bool? canUseContext, ref ActivityCreationOptions<ActivityContext> dataWithContext) => {
                    GetRequestedData<string>? getRequestedDataUsingParentId = listener.GetRequestedDataUsingParentId;
                    if (getRequestedDataUsingParentId != null)
                    {
                        ActivityDataRequest dr = getRequestedDataUsingParentId(ref data);
                        if (dr > request)
                        {
                            request = dr;
                        }

                        // Stop the enumeration if we get the max value RecordingAndSampling.
                        return request != ActivityDataRequest.AllDataAndRecorded;
                    }
                    else
                    {
                        // In case we have a parent Id and the listener not providing the GetRequestedDataUsingParentId, we'll try to find out if the following conditions are true:
                        //   - The listener is providing the GetRequestedDataUsingContext callback
                        //   - Can convert the parent Id to a Context
                        // Then we can call the listener GetRequestedDataUsingContext callback with the constructed context.
                        GetRequestedData<ActivityContext>? getRequestedDataUsingContext = listener.GetRequestedDataUsingContext;
                        if (getRequestedDataUsingContext != null)
                        {
                            if (!canUseContext.HasValue)
                            {
                                canUseContext = Activity.TryConvertIdToContext(parentId, traceState: null, out ActivityContext ctx);
                                if (canUseContext.Value)
                                {
                                    dataWithContext = new ActivityCreationOptions<ActivityContext>(data.Source, data.Name, ctx, data.Kind, data.Tags, data.Links);
                                }
                            }

                            if (canUseContext.Value)
                            {
                                ActivityDataRequest dr = getRequestedDataUsingContext(ref dataWithContext);
                                if (dr > request)
                                {
                                    request = dr;
                                }
                                // Stop the enumeration if we get the max value RecordingAndSampling.
                                return request != ActivityDataRequest.AllDataAndRecorded;
                            }
                        }
                    }
                    return true;
                }, ref aco, ref dataRequest, ref useContext, ref optionsWithContext);
            }
            else
            {
                ActivityContext initializedContext = context == default && Activity.Current != null ? Activity.Current.Context : context;
                optionsWithContext = new ActivityCreationOptions<ActivityContext>(this, name, initializedContext, kind, tags, links);
                listeners.EnumWithFunc((ActivityListener listener, ref ActivityCreationOptions<ActivityContext> data, ref ActivityDataRequest request, ref bool? canUseContext, ref ActivityCreationOptions<ActivityContext> dataWithContext) => {
                    GetRequestedData<ActivityContext>? getRequestedDataUsingContext = listener.GetRequestedDataUsingContext;
                    if (getRequestedDataUsingContext != null)
                    {
                        if (listener.AutoGenerateRootContextTraceId && !canUseContext.HasValue && data.Parent == default)
                        {
                            ActivityContext ctx = new ActivityContext(ActivityTraceId.CreateRandom(), default, default);
                            dataWithContext = new ActivityCreationOptions<ActivityContext>(data.Source, data.Name, ctx, data.Kind, data.Tags, data.Links);
                            canUseContext = true;
                        }
                        ActivityDataRequest dr = getRequestedDataUsingContext(ref data);
                        if (dr > request)
                        {
                            request = dr;
                        }

                        // Stop the enumeration if we get the max value RecordingAndSampling.
                        return request != ActivityDataRequest.AllDataAndRecorded;
                    }
                    return true;
                }, ref optionsWithContext, ref dataRequest, ref useContext, ref optionsWithContext);
            }

            if (dataRequest != ActivityDataRequest.None)
            {
                if (useContext.HasValue && useContext.Value)
                {
                    context = optionsWithContext.Parent;
                    parentId = null;
                }

                activity = Activity.CreateAndStart(this, name, kind, parentId, context, tags, links, startTime, dataRequest);
                listeners.EnumWithAction((listener, obj) => listener.ActivityStarted?.Invoke((Activity) obj), activity);
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
                s_activeSources.EnumWithAction((source, obj) => {
                    var shouldListenTo = ((ActivityListener)obj).ShouldListenTo;
                    if (shouldListenTo != null && shouldListenTo(source))
                    {
                        source.AddListener((ActivityListener)obj);
                    }
                }, listener);
            }
        }

        internal delegate bool Function<T, TParent>(T item, ref ActivityCreationOptions<TParent> data, ref ActivityDataRequest dataRequest, ref bool? ctxInitialized, ref ActivityCreationOptions<ActivityContext> dataWithContext);

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
            s_activeSources.EnumWithAction((source, obj) => source._listeners?.Remove((ActivityListener) obj), listener);
        }

        internal void NotifyActivityStart(Activity activity)
        {
            Debug.Assert(activity != null);

            // _listeners can get assigned to null in Dispose.
            SynchronizedList<ActivityListener>? listeners = _listeners;
            if (listeners != null &&  listeners.Count > 0)
            {
                listeners.EnumWithAction((listener, obj) => listener.ActivityStarted?.Invoke((Activity) obj), activity);
            }
        }

        internal void NotifyActivityStop(Activity activity)
        {
            Debug.Assert(activity != null);

            // _listeners can get assigned to null in Dispose.
            SynchronizedList<ActivityListener>? listeners = _listeners;
            if (listeners != null &&  listeners.Count > 0)
            {
                listeners.EnumWithAction((listener, obj) => listener.ActivityStopped?.Invoke((Activity) obj), activity);
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

        public void EnumWithFunc<TParent>(ActivitySource.Function<T, TParent> func, ref ActivityCreationOptions<TParent> data, ref ActivityDataRequest dataRequest, ref bool? ctxInitialized, ref ActivityCreationOptions<ActivityContext> dataWithContext)
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
                if (!func(item, ref data, ref dataRequest, ref ctxInitialized, ref dataWithContext))
                {
                    break;
                }
            }
        }

        public void EnumWithAction(Action<T, object> action, object arg)
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
                action(item, arg);
            }
        }

    }
}
