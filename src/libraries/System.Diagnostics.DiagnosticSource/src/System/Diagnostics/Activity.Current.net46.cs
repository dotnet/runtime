// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace System.Diagnostics
{
    public partial class Activity
    {
        private static readonly AsyncLocal<Activity?> s_current = new AsyncLocal<Activity?>();

        /// <summary>
        /// Gets or sets the current operation (Activity) for the current thread.  This flows
        /// across async calls.
        /// </summary>
        public static Activity? Current
        {
            get { return s_current.Value; }
            set
            {
                if (ValidateSetCurrent(value))
                {
                    SetCurrent(value);
                }
            }
        }

        private static void SetCurrent(Activity? activity)
        {
            EventHandler<ActivityChangedEventArgs>? handler = CurrentChanged;
            if (handler is null)
            {
                s_current.Value = activity;
            }
            else
            {
                Activity? previous = s_current.Value;
                s_current.Value = activity;
                handler.Invoke(null, new ActivityChangedEventArgs(previous, activity));
            }
        }
    }
}
