// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace System.Diagnostics
{
    public partial class Activity
    {
        /// <summary>
        /// Gets or sets the current operation (Activity) for the current thread.  This flows 
        /// across async calls.
        /// </summary>
        public static Activity Current
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

#region private
        private static void SetCurrent(Activity activity)
        {
            s_current.Value = activity;
        }

        private static readonly AsyncLocal<Activity> s_current = new AsyncLocal<Activity>();
#endregion // private
    }
}
