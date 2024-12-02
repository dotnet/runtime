// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Xunit;

namespace System.Net.Test.Common
{
    internal class ActivityRecorder : IDisposable
    {
        private string _activitySourceName;
        private string _activityName;

        private readonly ActivityListener _listener;
        private List<Activity> _finishedActivities = new();

        private int _started;
        private int _stopped;

        public int Started => _started;
        public int Stopped => _stopped;

        public Predicate<Activity> Filter { get; set; } = _ => true;
        public bool VerifyParent { get; set; } = true;
        public Activity ExpectedParent { get; set; }

        public Activity LastStartedActivity { get; private set; }
        public Activity LastFinishedActivity { get; private set; }
        public IEnumerable<Activity> FinishedActivities => _finishedActivities;

        public ActivityRecorder(string activitySourceName, string activityName)
        {
            _activitySourceName = activitySourceName;
            _activityName = activityName;
            _listener = new ActivityListener
            {
                ShouldListenTo = (activitySource) => activitySource.Name == _activitySourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData,
                ActivityStarted = (activity) =>
                {
                    if (activity.OperationName == _activityName && Filter(activity))
                    {
                        if (VerifyParent)
                        {
                            Assert.Same(ExpectedParent, activity.Parent);
                        }

                        Interlocked.Increment(ref _started);

                        LastStartedActivity = activity;
                    }
                },
                ActivityStopped = (activity) =>
                {
                    if (activity.OperationName == _activityName && Filter(activity))
                    {
                        if (VerifyParent)
                        {
                            Assert.Same(ExpectedParent, activity.Parent);
                        }

                        Interlocked.Increment(ref _stopped);

                        lock (_finishedActivities)
                        {
                            LastFinishedActivity = activity;
                            _finishedActivities.Add(activity);
                        }
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

        public Activity VerifyActivityRecordedOnce()
        {
            VerifyActivityRecorded(1);
            return LastFinishedActivity;
        }
    }

    internal static class ActivityAssert
    {
        public static KeyValuePair<string, object> HasTag(Activity activity, string name)
        {
            KeyValuePair<string, object> tag = activity.TagObjects.SingleOrDefault(t => t.Key == name);
            if (tag.Key is null)
            {
                Assert.Fail($"The Activity tags should contain {name}.");
            }
            return tag;
        }

        public static void HasTag<T>(Activity activity, string name, T expectedValue)
        {
            KeyValuePair<string, object> tag = HasTag(activity, name);
            Assert.Equal(expectedValue, (T)tag.Value);
        }

        public static void HasTag<T>(Activity activity, string name, Func<T, bool> verifyValue)
        {
            T? value = (T?)activity.TagObjects.SingleOrDefault(t => t.Key == name).Value;
            Assert.False(value is null, $"The Activity tags should contain {name}.");
            Assert.True(verifyValue(value));
        }

        public static void HasNoTag(Activity activity, string name)
        {
            bool contains = activity.TagObjects.Any(t => t.Key == name);
            Assert.False(contains, $"The Activity tags should not contain {name}.");
        }

        public static void FinishedInOrder(Activity first, Activity second)
        {
            Assert.True(first.StartTimeUtc + first.Duration < second.StartTimeUtc + second.Duration, $"{first.OperationName} should stop before {second.OperationName}");
        }

        public static string CamelToSnake(string camel)
        {
            if (string.IsNullOrEmpty(camel)) return camel;
            StringBuilder bld = new();
            bld.Append(char.ToLower(camel[0]));
            for (int i = 1; i < camel.Length; i++)
            {
                char c = camel[i];
                if (char.IsUpper(c))
                {
                    bld.Append('_');
                }
                bld.Append(char.ToLower(c));
            }
            return bld.ToString();
        }
    }
}
