// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Diagnostics
{
    /// <summary>
    /// ActivityListener allows listening to the ActivitySource creation event
    /// and decides to enable listening to the activity objects created from the ActivitySource.
    /// </summary>
    public abstract class ActivityListener : IDisposable
    {
        /// <summary>
        /// EnableListening will be called with the <see cref="ActivitySource"/> object name to decide if interested to
        /// listen to this object events.
        /// </summary>
        /// <param name="activitySourceName">The name of the ActivitySource object to decide if need to listen to.</param>
        /// <returns>true if want to listen to ActivitySource object with the name activitySourceName.</returns>
        public virtual bool EnableListening(string activitySourceName) => true;

        /// <summary>
        /// ShouldCreateActivity allow deciding if should allow create the <see cref="Activity"/> object.
        /// The main scenario for this is when doing sampling and try to avoid creating <see cref="Activity"/> objects which is not going to be used.
        /// </summary>
        /// <param name="activitySourceName">The name of the <see cref="ActivitySource"/> object.</param>
        /// <param name="context">The <see cref="ActivityContext"/> object to get more information about the tracing context.</param>
        /// <param name="links">List of <see cref="ActivityLink"/> objects used with the tracing operation.</param>
        /// <returns>true if should create the <see cref="Activity"/> object.</returns>
        public virtual bool ShouldCreateActivity(string activitySourceName, ActivityContext context, IEnumerable<ActivityLink>? links) => true;

        /// <summary>
        /// OnActivityStarted will get called when an <see cref="Activity"/> object get created and started using an <see cref="ActivitySource"/> object
        /// which the current listener is listening to.
        /// </summary>
        public virtual void OnActivityStarted(Activity a) {}

        /// <summary>
        /// OnActivityStopped will get called when an <see cref="Activity"/> object get stopped.
        /// </summary>
        public virtual void OnActivityStopped(Activity a) {}

        /// <summary>
        /// Dispose this listener and detach it from any ActivitySource listening to.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            ActivitySource.DetachListener(this);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Allow the implementer of this class to do any cleanup before disposing.
        /// </summary>
        protected virtual void Dispose(bool disposing) {}
   }
}