// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    /// <summary>
    /// Implements a <see cref="TimeClock"/> that tracks time in its internal state.
    /// This is primarily intended for use in unit tests, so that an artificial time can be provided
    /// to the code being tested rather than the actual system time.
    /// </summary>
    public sealed class VirtualClock : TimeClock
    {
        // This is the internal time value of the virtual clock.
        private DateTimeOffset? _value;

        // This is only used when constructing the virtual clock using a DateTime instead of a DateTimeOffset.
        // It is later converted to a DateTimeOffset when used in the GetCurrentUtcDateTimeOffsetImpl method.
        private readonly DateTime _initialDateTimeValue;

        // When set, this function controls how the internal time value advances after each value is retrieved.
        // Otherwise, the internal time value is fixed and does not advance.
        private readonly Func<DateTimeOffset, DateTimeOffset>? _advancementFunction;

        /// <summary>
        /// Constructs a virtual clock that always provides a fixed time value.
        /// </summary>
        /// <param name="timeValue">The time that the clock is set to.</param>
        /// <remarks>
        /// The <paramref name="timeValue"/> parameter will internally be converted to UTC using its <see cref="DateTimeOffset.Offset"/>.
        /// </remarks>
        public VirtualClock(DateTimeOffset timeValue)
        {
            _value = timeValue.ToUniversalTime();
        }

        /// <summary>
        /// Constructs a virtual clock that always provides a fixed time value.
        /// </summary>
        /// <param name="timeValue">The time that the clock is set to.</param>
        /// <remarks>
        /// The <paramref name="timeValue"/> parameter will internally be converted to UTC using its <see cref="DateTime.Kind"/>.
        /// Values with <see cref="DateTimeKind.Local"/> or <see cref="DateTimeKind.Unspecified"/> will be treated as local time,
        /// using the local time zone as given by <see cref="TimeZoneInfo.Local"/> or the current <see cref="TimeContext.LocalTimeZone"/>.
        /// </remarks>
        public VirtualClock(DateTime timeValue)
        {
            _initialDateTimeValue = timeValue;
        }

        /// <summary>
        /// Constructs a virtual clock that is initialized to a given value, and advances after each retrieval by a given amount.
        /// </summary>
        /// <param name="initialTimeValue">The time that the clock is initially set to.</param>
        /// <param name="advancementAmount">The amount of time that the clock will advance after the current time is retrieved.</param>
        /// <remarks>
        /// The <paramref name="initialTimeValue"/> parameter will internally be converted to UTC using its <see cref="DateTimeOffset.Offset"/>.
        /// </remarks>
        public VirtualClock(DateTimeOffset initialTimeValue, TimeSpan advancementAmount)
        {
            _value = initialTimeValue.ToUniversalTime();
            _advancementFunction = value => value.Add(advancementAmount);
        }

        /// <summary>
        /// Constructs a virtual clock that is initialized to a given value, and advances after each retrieval by a given amount.
        /// </summary>
        /// <param name="initialTimeValue">The time that the clock is initially set to.</param>
        /// <param name="advancementAmount">The amount of time that the clock will advance after the current time is retrieved.</param>
        /// <remarks>
        /// The <paramref name="initialTimeValue"/> parameter will internally be converted to UTC using its <see cref="DateTime.Kind"/>.
        /// Values with <see cref="DateTimeKind.Local"/> or <see cref="DateTimeKind.Unspecified"/> will be treated as local time,
        /// using the local time zone as given by <see cref="TimeZoneInfo.Local"/> or the current <see cref="TimeContext.LocalTimeZone"/>.
        /// </remarks>
        public VirtualClock(DateTime initialTimeValue, TimeSpan advancementAmount)
        {
            _initialDateTimeValue = initialTimeValue;
            _advancementFunction = value => value.Add(advancementAmount);
        }

        /// <summary>
        /// Constructs a virtual clock that is initialized to a given value, and advances after each retrieval according
        /// to a given function.
        /// </summary>
        /// <param name="initialTimeValue">The time that the clock is initially set to.</param>
        /// <param name="advancementFunction">
        /// A function that advances the clock after the current time is retrieved.
        /// The function's input parameter is the clock's current value, and the function should return the clock's new value.
        /// </param>
        /// <remarks>
        /// The <paramref name="initialTimeValue"/> parameter will internally be converted to UTC using its <see cref="DateTimeOffset.Offset"/>.
        /// </remarks>
        public VirtualClock(DateTimeOffset initialTimeValue, Func<DateTimeOffset, DateTimeOffset> advancementFunction)
        {
            _value = initialTimeValue.ToUniversalTime();
            _advancementFunction = advancementFunction;
        }

        /// <summary>
        /// Constructs a virtual clock that is initialized to a given value, and advances after each retrieval according
        /// to a given function.
        /// </summary>
        /// <param name="initialTimeValue">The time that the clock is initially set to.</param>
        /// <param name="advancementFunction">
        /// A function that advances the clock after the current time is retrieved.
        /// The function's input parameter is the clock's current value, and the function should return the clock's new value.
        /// </param>
        /// <remarks>
        /// The <paramref name="initialTimeValue"/> parameter will internally be converted to UTC using its <see cref="DateTime.Kind"/>.
        /// Values with <see cref="DateTimeKind.Local"/> or <see cref="DateTimeKind.Unspecified"/> will be treated as local time,
        /// using the local time zone as given by <see cref="TimeZoneInfo.Local"/> or the current <see cref="TimeContext.LocalTimeZone"/>.
        /// </remarks>
        public VirtualClock(DateTime initialTimeValue, Func<DateTimeOffset, DateTimeOffset> advancementFunction)
        {
            _initialDateTimeValue = initialTimeValue;
            _advancementFunction = advancementFunction;
        }

        protected override DateTimeOffset GetCurrentUtcDateTimeOffsetImpl()
        {
            // Note: When the clock is initialized using a DateTime, the conversion to DateTimeOffset is done here
            //       rather than in the constructor.  This allows for the local time zone to be changed (if desired)
            //       when using this clock within an ambient TimeContext, thus affecting the conversion behavior
            //       in the following line of code when the DateTime value has a Kind of Local or Unspecified.
            DateTimeOffset value = _value ?? new DateTimeOffset(_initialDateTimeValue);

            if (_advancementFunction != null)
            {
                DateTimeOffset newValue = _advancementFunction.Invoke(value);
                _value = newValue.ToUniversalTime();
            }

            return value;
        }
    }
}
