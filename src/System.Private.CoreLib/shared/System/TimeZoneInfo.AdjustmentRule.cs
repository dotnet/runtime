// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System
{
    public sealed partial class TimeZoneInfo
    {
        [Serializable]
        public sealed class AdjustmentRule : IEquatable<AdjustmentRule?>, ISerializable, IDeserializationCallback
        {
            private static readonly TimeSpan DaylightDeltaAdjustment = TimeSpan.FromHours(24.0);
            private static readonly TimeSpan MaxDaylightDelta = TimeSpan.FromHours(12.0);
            private readonly DateTime _dateStart;
            private readonly DateTime _dateEnd;
            private readonly TimeSpan _daylightDelta;
            private readonly TransitionTime _daylightTransitionStart;
            private readonly TransitionTime _daylightTransitionEnd;
            private readonly TimeSpan _baseUtcOffsetDelta;   // delta from the default Utc offset (utcOffset = defaultUtcOffset + _baseUtcOffsetDelta)
            private readonly bool _noDaylightTransitions;

            public DateTime DateStart => _dateStart;

            public DateTime DateEnd => _dateEnd;

            public TimeSpan DaylightDelta => _daylightDelta;

            public TransitionTime DaylightTransitionStart => _daylightTransitionStart;

            public TransitionTime DaylightTransitionEnd => _daylightTransitionEnd;

            internal TimeSpan BaseUtcOffsetDelta => _baseUtcOffsetDelta;

            /// <summary>
            /// Gets a value indicating that this AdjustmentRule fixes the time zone offset
            /// from DateStart to DateEnd without any daylight transitions in between.
            /// </summary>
            internal bool NoDaylightTransitions => _noDaylightTransitions;

            internal bool HasDaylightSaving =>
                DaylightDelta != TimeSpan.Zero ||
                (DaylightTransitionStart != default && DaylightTransitionStart.TimeOfDay != DateTime.MinValue) ||
                (DaylightTransitionEnd != default && DaylightTransitionEnd.TimeOfDay != DateTime.MinValue.AddMilliseconds(1));

            public bool Equals(AdjustmentRule? other) =>
                other != null &&
                _dateStart == other._dateStart &&
                _dateEnd == other._dateEnd &&
                _daylightDelta == other._daylightDelta &&
                _baseUtcOffsetDelta == other._baseUtcOffsetDelta &&
                _daylightTransitionEnd.Equals(other._daylightTransitionEnd) &&
                _daylightTransitionStart.Equals(other._daylightTransitionStart);

            public override int GetHashCode() => _dateStart.GetHashCode();

            private AdjustmentRule(
                DateTime dateStart,
                DateTime dateEnd,
                TimeSpan daylightDelta,
                TransitionTime daylightTransitionStart,
                TransitionTime daylightTransitionEnd,
                TimeSpan baseUtcOffsetDelta,
                bool noDaylightTransitions)
            {
                ValidateAdjustmentRule(dateStart, dateEnd, daylightDelta,
                       daylightTransitionStart, daylightTransitionEnd, noDaylightTransitions);

                _dateStart = dateStart;
                _dateEnd = dateEnd;
                _daylightDelta = daylightDelta;
                _daylightTransitionStart = daylightTransitionStart;
                _daylightTransitionEnd = daylightTransitionEnd;
                _baseUtcOffsetDelta = baseUtcOffsetDelta;
                _noDaylightTransitions = noDaylightTransitions;
            }

            public static AdjustmentRule CreateAdjustmentRule(
                DateTime dateStart,
                DateTime dateEnd,
                TimeSpan daylightDelta,
                TransitionTime daylightTransitionStart,
                TransitionTime daylightTransitionEnd)
            {
                return new AdjustmentRule(
                    dateStart,
                    dateEnd,
                    daylightDelta,
                    daylightTransitionStart,
                    daylightTransitionEnd,
                    baseUtcOffsetDelta: TimeSpan.Zero,
                    noDaylightTransitions: false);
            }

            internal static AdjustmentRule CreateAdjustmentRule(
                DateTime dateStart,
                DateTime dateEnd,
                TimeSpan daylightDelta,
                TransitionTime daylightTransitionStart,
                TransitionTime daylightTransitionEnd,
                TimeSpan baseUtcOffsetDelta,
                bool noDaylightTransitions)
            {
                AdjustDaylightDeltaToExpectedRange(ref daylightDelta, ref baseUtcOffsetDelta);
                return new AdjustmentRule(
                    dateStart,
                    dateEnd,
                    daylightDelta,
                    daylightTransitionStart,
                    daylightTransitionEnd,
                    baseUtcOffsetDelta,
                    noDaylightTransitions);
            }

            //
            // When Windows sets the daylight transition start Jan 1st at 12:00 AM, it means the year starts with the daylight saving on.
            // We have to special case this value and not adjust it when checking if any date is in the daylight saving period.
            //
            internal bool IsStartDateMarkerForBeginningOfYear() =>
                !NoDaylightTransitions &&
                DaylightTransitionStart.Month == 1 && DaylightTransitionStart.Day == 1 && DaylightTransitionStart.TimeOfDay.Hour == 0 &&
                DaylightTransitionStart.TimeOfDay.Minute == 0 && DaylightTransitionStart.TimeOfDay.Second == 0 &&
                _dateStart.Year == _dateEnd.Year;

            //
            // When Windows sets the daylight transition end Jan 1st at 12:00 AM, it means the year ends with the daylight saving on.
            // We have to special case this value and not adjust it when checking if any date is in the daylight saving period.
            //
            internal bool IsEndDateMarkerForEndOfYear() =>
                !NoDaylightTransitions &&
                DaylightTransitionEnd.Month == 1 && DaylightTransitionEnd.Day == 1 && DaylightTransitionEnd.TimeOfDay.Hour == 0 &&
                DaylightTransitionEnd.TimeOfDay.Minute == 0 && DaylightTransitionEnd.TimeOfDay.Second == 0 &&
                _dateStart.Year == _dateEnd.Year;

            /// <summary>
            /// Helper function that performs all of the validation checks for the factory methods and deserialization callback.
            /// </summary>
            private static void ValidateAdjustmentRule(
                DateTime dateStart,
                DateTime dateEnd,
                TimeSpan daylightDelta,
                TransitionTime daylightTransitionStart,
                TransitionTime daylightTransitionEnd,
                bool noDaylightTransitions)
            {
                if (dateStart.Kind != DateTimeKind.Unspecified && dateStart.Kind != DateTimeKind.Utc)
                {
                    throw new ArgumentException(SR.Argument_DateTimeKindMustBeUnspecifiedOrUtc, nameof(dateStart));
                }

                if (dateEnd.Kind != DateTimeKind.Unspecified && dateEnd.Kind != DateTimeKind.Utc)
                {
                    throw new ArgumentException(SR.Argument_DateTimeKindMustBeUnspecifiedOrUtc, nameof(dateEnd));
                }

                if (daylightTransitionStart.Equals(daylightTransitionEnd) && !noDaylightTransitions)
                {
                    throw new ArgumentException(SR.Argument_TransitionTimesAreIdentical, nameof(daylightTransitionEnd));
                }

                if (dateStart > dateEnd)
                {
                    throw new ArgumentException(SR.Argument_OutOfOrderDateTimes, nameof(dateStart));
                }

                // This cannot use UtcOffsetOutOfRange to account for the scenario where Samoa moved across the International Date Line,
                // which caused their current BaseUtcOffset to be +13. But on the other side of the line it was UTC-11 (+1 for daylight).
                // So when trying to describe DaylightDeltas for those times, the DaylightDelta needs
                // to be -23 (what it takes to go from UTC+13 to UTC-10)
                if (daylightDelta.TotalHours < -23.0 || daylightDelta.TotalHours > 14.0)
                {
                    throw new ArgumentOutOfRangeException(nameof(daylightDelta), daylightDelta, SR.ArgumentOutOfRange_UtcOffset);
                }

                if (daylightDelta.Ticks % TimeSpan.TicksPerMinute != 0)
                {
                    throw new ArgumentException(SR.Argument_TimeSpanHasSeconds, nameof(daylightDelta));
                }

                if (dateStart != DateTime.MinValue && dateStart.Kind == DateTimeKind.Unspecified && dateStart.TimeOfDay != TimeSpan.Zero)
                {
                    throw new ArgumentException(SR.Argument_DateTimeHasTimeOfDay, nameof(dateStart));
                }

                if (dateEnd != DateTime.MaxValue && dateEnd.Kind == DateTimeKind.Unspecified && dateEnd.TimeOfDay != TimeSpan.Zero)
                {
                    throw new ArgumentException(SR.Argument_DateTimeHasTimeOfDay, nameof(dateEnd));
                }
            }

            /// <summary>
            /// Ensures the daylight delta is within [-12, 12] hours
            /// </summary>>
            private static void AdjustDaylightDeltaToExpectedRange(ref TimeSpan daylightDelta, ref TimeSpan baseUtcOffsetDelta)
            {
                if (daylightDelta > MaxDaylightDelta)
                {
                    daylightDelta -= DaylightDeltaAdjustment;
                    baseUtcOffsetDelta += DaylightDeltaAdjustment;
                }
                else if (daylightDelta < -MaxDaylightDelta)
                {
                    daylightDelta += DaylightDeltaAdjustment;
                    baseUtcOffsetDelta -= DaylightDeltaAdjustment;
                }

                System.Diagnostics.Debug.Assert(daylightDelta <= MaxDaylightDelta && daylightDelta >= -MaxDaylightDelta,
                                                "DaylightDelta should not ever be more than 24h");
            }

            void IDeserializationCallback.OnDeserialization(object sender)
            {
                // OnDeserialization is called after each instance of this class is deserialized.
                // This callback method performs AdjustmentRule validation after being deserialized.

                try
                {
                    ValidateAdjustmentRule(_dateStart, _dateEnd, _daylightDelta,
                                           _daylightTransitionStart, _daylightTransitionEnd, _noDaylightTransitions);
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

                info.AddValue("DateStart", _dateStart); // Do not rename (binary serialization)
                info.AddValue("DateEnd", _dateEnd); // Do not rename (binary serialization)
                info.AddValue("DaylightDelta", _daylightDelta); // Do not rename (binary serialization)
                info.AddValue("DaylightTransitionStart", _daylightTransitionStart); // Do not rename (binary serialization)
                info.AddValue("DaylightTransitionEnd", _daylightTransitionEnd); // Do not rename (binary serialization)
                info.AddValue("BaseUtcOffsetDelta", _baseUtcOffsetDelta); // Do not rename (binary serialization)
                info.AddValue("NoDaylightTransitions", _noDaylightTransitions); // Do not rename (binary serialization)
            }

            private AdjustmentRule(SerializationInfo info, StreamingContext context)
            {
                if (info == null)
                {
                    throw new ArgumentNullException(nameof(info));
                }

                _dateStart = (DateTime)info.GetValue("DateStart", typeof(DateTime))!; // Do not rename (binary serialization)
                _dateEnd = (DateTime)info.GetValue("DateEnd", typeof(DateTime))!; // Do not rename (binary serialization)
                _daylightDelta = (TimeSpan)info.GetValue("DaylightDelta", typeof(TimeSpan))!; // Do not rename (binary serialization)
                _daylightTransitionStart = (TransitionTime)info.GetValue("DaylightTransitionStart", typeof(TransitionTime))!; // Do not rename (binary serialization)
                _daylightTransitionEnd = (TransitionTime)info.GetValue("DaylightTransitionEnd", typeof(TransitionTime))!; // Do not rename (binary serialization)

                object? o = info.GetValueNoThrow("BaseUtcOffsetDelta", typeof(TimeSpan)); // Do not rename (binary serialization)
                if (o != null)
                {
                    _baseUtcOffsetDelta = (TimeSpan)o;
                }

                o = info.GetValueNoThrow("NoDaylightTransitions", typeof(bool)); // Do not rename (binary serialization)
                if (o != null)
                {
                    _noDaylightTransitions = (bool)o;
                }
            }
        }
    }
}
