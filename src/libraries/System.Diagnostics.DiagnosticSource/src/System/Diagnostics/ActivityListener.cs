// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;

namespace System.Diagnostics
{
    /// <summary>
    /// Define the callback that can be used in <see cref="ActivityListener"/> to allow deciding to create the Activity objects and with what data state.
    /// </summary>
    public delegate ActivitySamplingResult SampleActivity<T>(ref ActivityCreationOptions<T> options);

    /// <summary>
    /// Define the callback to be used in <see cref="ActivityListener"/> to receive notifications when exceptions are added to the <see cref="Activity"/>.
    /// </summary>
    public delegate void ExceptionRecorder(Activity activity, Exception exception, ref TagList tags);

    /// <summary>
    /// ActivityListener allows listening to the start and stop Activity events and give the opportunity to decide creating the Activity for sampling scenarios.
    /// </summary>
    public sealed class ActivityListener : IDisposable
    {
        private bool _disposed;

        /// <summary>
        /// Construct a new <see cref="ActivityListener"/> object to start listening to the <see cref="Activity"/> events.
        /// </summary>
        public ActivityListener()
        {
        }

        /// <summary>
        /// Set or get the callback used to listen to the <see cref="Activity"/> start event.
        /// </summary>
        public Action<Activity>? ActivityStarted { get; set; }

        /// <summary>
        /// Set or get the callback used to listen to the <see cref="Activity"/> stop event.
        /// </summary>
        public Action<Activity>? ActivityStopped { get; set; }

        /// <summary>
        /// Set or get the callback used to listen to <see cref="Activity"/> events when exceptions are added.
        /// </summary>
        public ExceptionRecorder? ExceptionRecorder { get; set; }

        /// <summary>
        /// Set or get the callback used to decide if want to listen to <see cref="Activity"/> objects events which created using <see cref="ActivitySource"/> object.
        /// </summary>
        public Func<ActivitySource, bool>? ShouldListenTo { get; set; }

        /// <summary>
        /// Set or get the callback used to decide allowing creating <see cref="Activity"/> objects with specific data state.
        /// </summary>
        public SampleActivity<string>? SampleUsingParentId { get; set; }

        /// <summary>
        /// Set or get the callback used to decide allowing creating <see cref="Activity"/> objects with specific data state.
        /// </summary>
        public SampleActivity<ActivityContext>? Sample { get; set; }

        internal bool IsDisposed => Volatile.Read(ref _disposed);

        /// <summary>
        /// Re-evaluates <see cref="ShouldListenTo"/> against every registered <see cref="ActivitySource"/>, attaching this
        /// listener to sources that now match and detaching from sources that no longer match. Call this after mutating
        /// <see cref="ShouldListenTo"/> or any state captured by its callback (for example, when configuration changes
        /// alter the rules used by the predicate). If the listener has not yet been registered, it is registered as part
        /// of the refresh; calling this on a disposed listener has no effect, including when the disposal races with
        /// the refresh.
        /// </summary>
        /// <exception cref="Exception">If <see cref="ShouldListenTo"/> throws while evaluating exactly one source, that
        /// exception is rethrown unchanged after the refresh completes for every other source. If it throws for more
        /// than one source, the throws are wrapped in an <see cref="AggregateException"/>. Sources whose evaluation
        /// threw are left in their previous attachment state; sources whose evaluation succeeded are updated.</exception>
        public void RefreshSources()
        {
            if (Volatile.Read(ref _disposed))
            {
                return;
            }

            ActivitySource.ResetSourceFilters(this);
        }

        /// <summary>
        /// Dispose will unregister this <see cref="ActivityListener"/> object from listening to <see cref="Activity"/> events.
        /// </summary>
        public void Dispose()
        {
            if (Volatile.Read(ref _disposed))
            {
                return;
            }

            // The flag must be published before the cleanup walks so that a concurrent
            // RefreshSources, AddActivityListener, or ActivitySource ctor observes IsDisposed
            // via its post-commit recheck and undoes any attachments it raced into place.
            Volatile.Write(ref _disposed, true);
            ActivitySource.DetachListener(this);
        }
    }
}
