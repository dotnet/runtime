// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics
{
    public abstract partial class DiagnosticSource
    {
        /// <summary>
        /// Starts an Activity and writes start event.
        ///
        /// Activity describes logical operation, its context and parent relation;
        /// Current activity flows through the operation processing.
        ///
        /// This method starts given Activity (maintains global Current Activity
        /// and Parent for the given activity) and notifies consumers  that new Activity
        /// was started. Consumers could access <see cref="Activity.Current"/>
        /// to add context and/or augment telemetry.
        ///
        /// Producers may pass additional details to the consumer in the payload.
        /// </summary>
        /// <param name="activity">Activity to be started</param>
        /// <param name="args">An object that represent the value being passed as a payload for the event.</param>
        /// <returns>Started Activity for convenient chaining</returns>
        /// <seealso cref="Activity"/>
        [RequiresUnreferencedCode(WriteRequiresUnreferencedCode)]
        [RequiresDynamicCode(WriteRequiresDynamicCode)]
        public Activity StartActivity(Activity activity, object? args)
        {
            activity.Start();
            Write(activity.OperationName + ".Start", args);
            return activity;
        }

        /// <summary>
        /// Stops given Activity: maintains global Current Activity and notifies consumers
        /// that Activity was stopped. Consumers could access <see cref="Activity.Current"/>
        /// to add context and/or augment telemetry.
        ///
        /// Producers may pass additional details to the consumer in the payload.
        /// </summary>
        /// <param name="activity">Activity to be stopped</param>
        /// <param name="args">An object that represent the value being passed as a payload for the event.</param>
        /// <seealso cref="Activity"/>
        [RequiresUnreferencedCode(WriteRequiresUnreferencedCode)]
        [RequiresDynamicCode(WriteRequiresDynamicCode)]
        public void StopActivity(Activity activity, object? args)
        {
            // Stop sets the end time if it was unset, but we want it set before we issue the write
            // so we do it now.
            if (activity.Duration == TimeSpan.Zero)
                activity.SetEndTime(Activity.GetUtcNow());
            Write(activity.OperationName + ".Stop", args);
            activity.Stop();    // Resets Activity.Current (we want this after the Write)
        }

        /// <summary>
        /// Optional: If an instrumentation site creating an new activity that was caused
        /// by something outside the process (e.g. an incoming HTTP request), then that site
        /// will want to make a new activity and transfer state from that incoming request
        /// to the activity.   To the extent possible this should be done by the instrumentation
        /// site (because it is a contract between Activity and the incoming request logic
        /// at the instrumentation site.   However the instrumentation site can't handle policy
        /// (for example if sampling is done exactly which requests should be sampled) For this
        /// the instrumentation site needs to call back out to the logging system and ask it to
        /// resolve policy (e.g. decide if the Activity's 'sampling' bit should be set)  This
        /// is what OnActivityImport is for.   It is given the activity as well as a payload
        /// object that represents the incoming request.   The DiagnosticSource's subscribers
        /// then have the opportunity to update this activity as desired.
        ///
        /// Note that this callout is rarely used at instrumentation sites (only those sites
        /// that are on the 'boundry' of the process), and the instrumentation site will implement
        /// some default policy (it sets the activity in SOME way), and so this method does not
        /// need to be overridden if that default policy is fine.   Thus this is call should
        /// be used rare (but often important) cases.
        ///
        /// Note that the type of 'payload' is typed as object here, but for any
        /// particular instrumentation site and the subscriber will know the type of
        /// the payload and thus cast it and decode it if it needs to.
        /// </summary>
        public virtual void OnActivityImport(Activity activity, object? payload) { }

        /// <summary>
        /// Optional: If an instrumentation site is at a location where activities leave the
        /// process (e.g. an outgoing HTTP request), then that site will want to transfer state
        /// from the activity to the outgoing request.    To the extent possible this should be
        /// done by the instrumentation site (because it is a contract between Activity and the
        /// outgoing request logic at the instrumentation site.   However the instrumentation site
        /// can't handle policy (for example whether activity information should be disabled,
        /// or written in a older format for compatibility reasons).   For this
        /// the instrumentation site needs to call back out to the logging system and ask it to
        /// resolve policy.  This is what OnActivityExport is for.   It is given the activity as
        /// well as a payloay object that represents the outgoing request.   The DiagnosticSource's
        /// subscriber then have the ability to update the outgoing request before it is sent.
        ///
        /// Note that this callout is rarely used at instrumentation sites (only those sites
        /// that are on an outgoing 'boundry' of the process).   Moreover typically the default
        /// policy that the instrumentation site performs (transfer all activity state in a
        /// particular outgoing convention), is likely to be fine.   This is only for cases
        /// where that is a problem.  Thus this is call should be used very rarely and is
        /// mostly here for symetry with OnActivityImport and future-proofing.
        ///
        /// Note that the type of 'payload' is typed as object here, but for any
        /// particular instrumentation site and the subscriber should know the type of
        /// the payload and thus cast it and decode it if it needs to.
        /// </summary>
        public virtual void OnActivityExport(Activity activity, object? payload) { }
    }

    public partial class DiagnosticListener
    {
        public override void OnActivityImport(Activity activity, object? payload)
        {
            for (DiagnosticSubscription? curSubscription = _subscriptions; curSubscription != null; curSubscription = curSubscription.Next)
                curSubscription.OnActivityImport?.Invoke(activity, payload);
        }

        public override void OnActivityExport(Activity activity, object? payload)
        {
            for (DiagnosticSubscription? curSubscription = _subscriptions; curSubscription != null; curSubscription = curSubscription.Next)
                curSubscription.OnActivityExport?.Invoke(activity, payload);
        }

        /// <summary>
        /// Add a subscriber (Observer).  If the isEnabled parameter is non-null indicates that some events are
        /// uninteresting can be skipped for efficiency.  You can also supply an 'onActivityImport' and 'onActivityExport'
        /// methods that should be called when providers are 'importing' or 'exporting' activities from outside the
        /// process (e.g. from Http Requests).   These are called right after importing (exporting) the activity and
        /// can be used to modify the activity (or outgoing request) to add policy.
        /// </summary>
        public virtual IDisposable Subscribe(IObserver<KeyValuePair<string, object?>> observer, Func<string, object?, object?, bool>? isEnabled,
            Action<Activity, object?>? onActivityImport = null, Action<Activity, object?>? onActivityExport = null)
        {
            return isEnabled == null ?
             SubscribeInternal(observer, null, null, onActivityImport, onActivityExport) :
             SubscribeInternal(observer, name => IsEnabled(name, null, null), isEnabled, onActivityImport, onActivityExport);
        }
    }
}
