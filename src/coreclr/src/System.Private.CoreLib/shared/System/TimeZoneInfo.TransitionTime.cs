// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Runtime.Serialization;

namespace System
{
    public sealed partial class TimeZoneInfo
    {
        [Serializable]
        public readonly struct TransitionTime : IEquatable<TransitionTime>, ISerializable, IDeserializationCallback
        {
            private readonly DateTime _timeOfDay;
            private readonly byte _month;
            private readonly byte _week;
            private readonly byte _day;
            private readonly DayOfWeek _dayOfWeek;
            private readonly bool _isFixedDateRule;

            public DateTime TimeOfDay => _timeOfDay;

            public int Month => _month;

            public int Week => _week;

            public int Day => _day;

            public DayOfWeek DayOfWeek => _dayOfWeek;

            public bool IsFixedDateRule => _isFixedDateRule;

            public override bool Equals(object? obj) =>
                obj is TransitionTime && Equals((TransitionTime)obj);

            public static bool operator ==(TransitionTime t1, TransitionTime t2) => t1.Equals(t2);

            public static bool operator !=(TransitionTime t1, TransitionTime t2) => !t1.Equals(t2);

            public bool Equals(TransitionTime other) =>
                _isFixedDateRule == other._isFixedDateRule &&
                _timeOfDay == other._timeOfDay &&
                _month == other._month &&
                (other._isFixedDateRule ?
                    _day == other._day :
                    _week == other._week && _dayOfWeek == other._dayOfWeek);

            public override int GetHashCode() => (int)_month ^ (int)_week << 8;

            private TransitionTime(DateTime timeOfDay, int month, int week, int day, DayOfWeek dayOfWeek, bool isFixedDateRule)
            {
                ValidateTransitionTime(timeOfDay, month, week, day, dayOfWeek);

                _timeOfDay = timeOfDay;
                _month = (byte)month;
                _week = (byte)week;
                _day = (byte)day;
                _dayOfWeek = dayOfWeek;
                _isFixedDateRule = isFixedDateRule;
            }

            public static TransitionTime CreateFixedDateRule(DateTime timeOfDay, int month, int day) =>
                new TransitionTime(timeOfDay, month, 1, day, DayOfWeek.Sunday, isFixedDateRule: true);

            public static TransitionTime CreateFloatingDateRule(DateTime timeOfDay, int month, int week, DayOfWeek dayOfWeek) =>
                new TransitionTime(timeOfDay, month, week, 1, dayOfWeek, isFixedDateRule: false);

            /// <summary>
            /// Helper function that validates a TransitionTime instance.
            /// </summary>
            private static void ValidateTransitionTime(DateTime timeOfDay, int month, int week, int day, DayOfWeek dayOfWeek)
            {
                if (timeOfDay.Kind != DateTimeKind.Unspecified)
                {
                    throw new ArgumentException(SR.Argument_DateTimeKindMustBeUnspecified, nameof(timeOfDay));
                }

                // Month range 1-12
                if (month < 1 || month > 12)
                {
                    throw new ArgumentOutOfRangeException(nameof(month), SR.ArgumentOutOfRange_MonthParam);
                }

                // Day range 1-31
                if (day < 1 || day > 31)
                {
                    throw new ArgumentOutOfRangeException(nameof(day), SR.ArgumentOutOfRange_DayParam);
                }

                // Week range 1-5
                if (week < 1 || week > 5)
                {
                    throw new ArgumentOutOfRangeException(nameof(week), SR.ArgumentOutOfRange_Week);
                }

                // DayOfWeek range 0-6
                if ((int)dayOfWeek < 0 || (int)dayOfWeek > 6)
                {
                    throw new ArgumentOutOfRangeException(nameof(dayOfWeek), SR.ArgumentOutOfRange_DayOfWeek);
                }

                timeOfDay.GetDatePart(out int timeOfDayYear, out int timeOfDayMonth, out int timeOfDayDay);
                if (timeOfDayYear != 1 || timeOfDayMonth != 1 || timeOfDayDay != 1 || (timeOfDay.Ticks % TimeSpan.TicksPerMillisecond != 0))
                {
                    throw new ArgumentException(SR.Argument_DateTimeHasTicks, nameof(timeOfDay));
                }
            }

            void IDeserializationCallback.OnDeserialization(object sender)
            {
                // OnDeserialization is called after each instance of this class is deserialized.
                // This callback method performs TransitionTime validation after being deserialized.

                try
                {
                    ValidateTransitionTime(_timeOfDay, _month, _week, _day, _dayOfWeek);
                }
                catch (ArgumentException e)
                {
                    throw new SerializationException(SR.Serialization_InvalidData, e);
                }
            }

            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            {
                if (info == null)
                {
                    throw new ArgumentNullException(nameof(info));
                }

                info.AddValue("TimeOfDay", _timeOfDay); // Do not rename (binary serialization)
                info.AddValue("Month", _month); // Do not rename (binary serialization)
                info.AddValue("Week", _week); // Do not rename (binary serialization)
                info.AddValue("Day", _day); // Do not rename (binary serialization)
                info.AddValue("DayOfWeek", _dayOfWeek); // Do not rename (binary serialization)
                info.AddValue("IsFixedDateRule", _isFixedDateRule); // Do not rename (binary serialization)
            }

            private TransitionTime(SerializationInfo info, StreamingContext context)
            {
                if (info == null)
                {
                    throw new ArgumentNullException(nameof(info));
                }

                _timeOfDay = (DateTime)info.GetValue("TimeOfDay", typeof(DateTime))!; // Do not rename (binary serialization)
                _month = (byte)info.GetValue("Month", typeof(byte))!; // Do not rename (binary serialization)
                _week = (byte)info.GetValue("Week", typeof(byte))!; // Do not rename (binary serialization)
                _day = (byte)info.GetValue("Day", typeof(byte))!; // Do not rename (binary serialization)
                _dayOfWeek = (DayOfWeek)info.GetValue("DayOfWeek", typeof(DayOfWeek))!; // Do not rename (binary serialization)
                _isFixedDateRule = (bool)info.GetValue("IsFixedDateRule", typeof(bool))!; // Do not rename (binary serialization)
            }
        }
    }
}
