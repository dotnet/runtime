// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;

namespace System.Timers
{
    /// <summary>
    /// DescriptionAttribute marks a property, event, or extender with a
    /// description. Visual designers can display this description when referencing
    /// the member.
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public class TimersDescriptionAttribute : DescriptionAttribute
    {
        private bool _replaced;

        /// <summary>
        /// Constructs a new sys description.
        /// </summary>
        public TimersDescriptionAttribute(string description) : base(description) { }

        internal TimersDescriptionAttribute(TimersDescriptionStringId id) : base(GetResourceString(id)) { }

        private static string GetResourceString(TimersDescriptionStringId id)
        {
            switch (id)
            {
                case TimersDescriptionStringId.TimerAutoReset: return SR.TimerAutoReset;
                case TimersDescriptionStringId.TimerEnabled: return SR.TimerEnabled;
                case TimersDescriptionStringId.TimerInterval: return SR.TimerInterval;
                case TimersDescriptionStringId.TimerIntervalElapsed: return SR.TimerIntervalElapsed;
                case TimersDescriptionStringId.TimerSynchronizingObject: return SR.TimerSynchronizingObject;
                default:
                    Debug.Fail($"Unexpected resource {id}");
                    return "";
            }
        }

        /// <summary>
        /// Retrieves the description text.
        /// </summary>
        public override string Description
        {
            get
            {
                if (!_replaced)
                {
                    _replaced = true;

                    // We call string.Format here only to keep the original behavior which throws when having null description.
                    // That will keep the exception is thrown from same original place with the exact parameters.
                    DescriptionValue = string.Format(base.Description);
                }
                return base.Description;
            }
        }
    }

    internal enum TimersDescriptionStringId
    {
        TimerAutoReset,
        TimerEnabled,
        TimerInterval,
        TimerIntervalElapsed,
        TimerSynchronizingObject
    }
}
