// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using Xunit;

namespace System.Net.Test.Common
{
    internal class ActivityRecorder : IDisposable
    {
        private string _activitySourceName;
        private string _activityName;

        private readonly ActivityListener _listener;

        public Predicate<Activity> Filter { get; set; } = _ => true;
        public bool VerifyParent { get; set; } = true;
        public Activity ExpectedParent { get; set; }

        public int Started { get; private set; }
        public int Stopped { get; private set; }
        public Activity LastStartedActivity { get; private set; }
        public Activity LastFinishedActivity { get; private set; }

        public ActivityRecorder(string activitySourceName, string activityName)
        {
            _activitySourceName = activitySourceName;
            _activityName = activityName;
            _listener = new ActivityListener
            {
                ShouldListenTo = (activitySource) => activitySource.Name == _activitySourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData,
                ActivityStarted = (activity) => {
                    if (activity.OperationName == _activityName && Filter(activity))
                    {
                        if (VerifyParent)
                        {
                            Assert.Same(ExpectedParent, activity.Parent);
                        }

                        Started++;
                        LastStartedActivity = activity;
                    }
                },
                ActivityStopped = (activity) => {
                    if (activity.OperationName == _activityName && Filter(activity))
                    {
                        if (VerifyParent)
                        {
                            Assert.Same(ExpectedParent, activity.Parent);
                        }

                        Stopped++;
                        LastFinishedActivity = activity;
                    }
                }
            };

            ActivitySource.AddActivityListener(_listener);   
        }

        public void Dispose() => _listener.Dispose();

        public void VerifyActivityRecorded(int times)
        {
            Assert.Equal(times, Started);
            Assert.Equal(times, Stopped);
        }
    }
}
