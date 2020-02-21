// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Diagnostics
{
    public sealed class ActivitySource : IDisposable
    {
        private static object s_syncObject = new object();
        private static List<ActivitySource> s_activeSources = new List<ActivitySource>();
        private ActivitySource() { throw new InvalidOperationException(); }

        /// <summary>
        /// Event to subscribe to get the notification when any ActivitySource object get created or released.
        /// </summary>
        public static event EventHandler<ActivitySourceEventArgs>? OperationEvent;

        /// <summary>
        /// Event to subscribe to get the notification when any Activity object get created or disposed.
        /// </summary>
        public event EventHandler<ActivitySourceEventArgs>? ActivityEvent;

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

            lock (s_syncObject)
            {
                Name = name;
                s_activeSources.Add(this);
            }

            EventHandler<ActivitySourceEventArgs>? handlers = OperationEvent;
            if (handlers != null)
            {
                handlers(this, ActivitySourceEventArgs.s_sourceCreated);
            }
        }

        /// <summary>
        /// Returns the list of all created ActivitySource objects.
        /// </summary>
        public static IEnumerable<ActivitySource> ActiveList
        {
            get
            {
                lock (s_syncObject)
                {
                    return s_activeSources.ToArray();
                }
            }
        }

        /// <summary>
        /// Returns the ActivitySource name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Creates a new <see cref="Activity"/> object if there is any listener to the Activity creation event, returns null otherwise.
        /// </summary>
        /// <returns>The created <see cref="Activity"/> object or null if there is no any event listener.</returns>
        public Activity? CreateActivity()
        {
            if (ActivityEvent == null)
            {
                return null;
            }

            return Activity.CreateAndStart(this, default, null, default);
        }

        /// <summary>
        /// Creates a new <see cref="Activity"/> object if there is any listener to the Activity events, returns null otherwise.
        /// </summary>
        /// <param name="context">The <see cref="ActivityContext"/> object to initialize the created Activity object with.</param>
        /// <param name="links">The optional <see cref="ActivityLink"/> list to initialize the created Activity object with.</param>
        /// <param name="startTime">The optional start timestamp to set on the created Activity object.</param>
        /// <returns>The created <see cref="Activity"/> object or null if there is no any event listener.</returns>
        public Activity? CreateActivity(ActivityContext context, IEnumerable<ActivityLink>? links = null, DateTimeOffset startTime = default)
        {
            if (ActivityEvent == null)
            {
                return null;
            }

            return Activity.CreateAndStart(this, context, links, startTime);
        }

        /// <summary>
        /// Dispose the ActivitySource object and remove the current instance from the global list.
        /// </summary>
        public void Dispose()
        {
            lock (s_syncObject)
            {
                if (s_activeSources.Remove(this))
                {
                    ActivityEvent = null;
                }
            }
        }

        internal void RaiseActivityEvent(Activity activity, ActivitySourceEventArgs eventArgs)
        {
            EventHandler<ActivitySourceEventArgs>? handlers = ActivityEvent;
            if (handlers != null)
            {
                handlers(activity, eventArgs);
            }
        }
    }
}
