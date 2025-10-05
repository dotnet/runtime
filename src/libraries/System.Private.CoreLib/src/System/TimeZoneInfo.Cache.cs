// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file implements caching of per-year time zone transitions to improve performance
// when converting between UTC and local time. It also includes helper methods for using
// the cached data in time conversions.
//
// To cache transitions for a year, we must ensure all transitions for that year are
// included—covering the full range of UTC and local times from
// "year/01/01 12:00:00 AM" through "year/12/31 11:59:59 PM".
//
// Transitions are derived from adjustment rules, which can be either Linux-based or
// Windows-based:
//
// ------------------
// Linux-based Rules:
// ------------------
//   - Store exact transition times using rule.DateStart and rule.DateEnd.
//   - Transition times are stored in UTC.
//   - Start and end are both inclusive:
//       * start → first tick of the transition
//       * end   → last tick of the transition
//   - DaylightTransitionStart/DaylightTransitionEnd are not used.
//   - Identified by rule.NoDaylightTransitions = true.
//
// --------------------
// Windows-based Rules:
// --------------------
//   - Store transition times using DaylightTransitionStart and DaylightTransitionEnd.
//   - Transition times are stored in local time.
//   - rule.DateStart/DateEnd define which years the rule applies to, not exact transition times.
//   - Transition start is inclusive; transition end is exclusive.
//   - Identified by rule.NoDaylightTransitions = false.
//   - Can represent fixed or floating transitions (e.g., "First Sunday in March at 2:00 AM").
//   - Can define one or two transitions:
//       * One transition if start < end.
//       * Two transitions if start > end. In this case, the year begins and ends in
//         daylight saving time, with standard time occurring in the middle.
//         This pattern is common in southern hemisphere countries and occasionally
//         seen in places like Morocco.
//   - Special cases:
//       * Start = 1/1/1 12:00 AM → year starts with DST enabled.
//       * End   = 1/1/1 12:00 AM → year ends with DST enabled.
//
// -----------------
// Additional Notes:
// -----------------
//   - Rules may define BaseUtcOffsetDelta ≠ 0, meaning the base UTC offset changes and
//     must be accounted for.
//   - On Linux, both Linux-based and Windows-based rules may appear. Windows typically
//     uses only Windows-based rules.
//   - Year transitions are cached using TimeTransition records, always stored in UTC.
//   - Caching starts with the rule matching the target year’s transitions.
//     Transitions from the previous year may also be cached to handle year boundaries.
//     Subsequent transitions are cached if they occur in the same year
//     or if local/UTC transition times overlap the next rule.
//     On Linux, it is common to cache multiple years of transitions to ensure full coverage.
//   - The code is designed to handle consecutive rules, regardless of type.
//     It supports any mix of Windows- or Linux-based rules, fixed or floating rules,
//     northern or southern hemisphere rules, and rules that start or end with daylight saving.

using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System
{
    public sealed partial class TimeZoneInfo
    {
        /// <summary>
        /// Gets the UTC offset for the specified UTC time, along with whether it is ambiguous.
        /// Used by DateTime.Now to get the local time ticks and whether it is ambiguous.
        /// </summary>
        internal static long GetLocalDateTimeNowTicks(DateTime utcNow, out bool isAmbiguous) => s_cachedData.GetLocalDateTimeNowTicks(utcNow, out isAmbiguous);

        /// <summary>
        /// Gets the next transition for the specified UTC time and cache the result for the subsequent calls.
        /// </summary>
        /// <param name="utcNow">The current UTC time.</param>
        /// <returns>A DateTimeNowCache containing details about the next transition after utcNow.</returns>
        /// <remarks>
        /// This method calculates the next time zone transition after the specified UTC time.
        /// The created DateTimeNowCache object will be used when calling DateTime.Now for better performance.
        /// </remarks>
        private DateTimeNowCache GetNextNowTransition(DateTime utcNow)
        {
            var dateTimeNowCache = new DateTimeNowCache();

            if (!_supportsDaylightSavingTime || _adjustmentRules is null || _adjustmentRules.Length == 0)
            {
                // no daylight transitions for this time zone.
                dateTimeNowCache._nextUtcNowTransitionTicks = DateTime.MaxTicks;
                dateTimeNowCache._nowUtcOffsetTicks = _baseUtcOffset.Ticks;
                dateTimeNowCache._dtsAmbiguousOffsetStart = 0;
                dateTimeNowCache._dtsAmbiguousOffsetEnd = 0;

                return dateTimeNowCache;
            }

            if (TryGetTransitionsForYear(utcNow.Year, out (int index, int count) transitionInfo))
            {
                TimeTransition[] transitions = _yearsTransitions;
                int boundary = transitionInfo.index + transitionInfo.count;
                Debug.Assert(boundary <= _yearsTransitionsCount && transitions is not null);
                long utcNowTicks = utcNow.Ticks;

                // Find the next transition after utcNow
                for (int i = transitionInfo.index; i < boundary; i++)
                {
                    TimeTransition transition = transitions[i];

                    if (utcNowTicks >= transition.DateStart.Ticks && utcNowTicks <= transition.DateEnd.Ticks)
                    {
                        // Found transition
                        dateTimeNowCache._nextUtcNowTransitionTicks = transition.DateEnd.Ticks + 1;
                        dateTimeNowCache._nowUtcOffsetTicks = _baseUtcOffset.Ticks + transition.Offset.Ticks;

                        if (transition.DaylightSavingOn)
                        {
                            // Ambiguous time in daylight saving time period, find the start and end of the ambiguous period in the current transition if it exists

                            DateTime localTransitionStart = SafeCreateDateTimeFromTicks(transition.DateStart.Ticks + transition.Offset.Ticks);
                            DateTime localTransitionEnd = SafeCreateDateTimeFromTicks(transition.DateEnd.Ticks + transition.Offset.Ticks);

                            // check Ambiguous Overlap with the next transition
                            if (i < boundary - 1)
                            {
                                // If the next transition is also in daylight saving time, extend the ambiguous period to include it
                                TimeTransition nextTransition = transitions[i + 1];

                                DateTime nextLocalTransitionStart = SafeCreateDateTimeFromTicks(nextTransition.DateStart.Ticks + nextTransition.Offset.Ticks);
                                DateTime nextLocalTransitionEnd = SafeCreateDateTimeFromTicks(nextTransition.DateEnd.Ticks + nextTransition.Offset.Ticks);

                                DateTime overlapStart = localTransitionStart > nextLocalTransitionStart ? localTransitionStart : nextLocalTransitionStart;
                                DateTime overlapEnd = localTransitionEnd < nextLocalTransitionEnd ? localTransitionEnd : nextLocalTransitionEnd;

                                if (overlapStart <= overlapEnd)
                                {
                                    dateTimeNowCache._dtsAmbiguousOffsetStart = overlapStart.Ticks;
                                    dateTimeNowCache._dtsAmbiguousOffsetEnd = overlapEnd.Ticks;

                                    return dateTimeNowCache;
                                }
                            }

                            // check transition with previous transition
                            if (i > transitionInfo.index)
                            {
                                // If the previous transition is also in daylight saving time, extend the ambiguous period to include it
                                TimeTransition prevTransition = transitions[i - 1];

                                DateTime prevLocalTransitionStart = SafeCreateDateTimeFromTicks(prevTransition.DateStart.Ticks + prevTransition.Offset.Ticks);
                                DateTime prevLocalTransitionEnd = SafeCreateDateTimeFromTicks(prevTransition.DateEnd.Ticks + prevTransition.Offset.Ticks);

                                DateTime overlapStart = localTransitionStart > prevLocalTransitionStart ? localTransitionStart : prevLocalTransitionStart;
                                DateTime overlapEnd = localTransitionEnd < prevLocalTransitionEnd ? localTransitionEnd : prevLocalTransitionEnd;

                                if (overlapStart <= overlapEnd)
                                {
                                    dateTimeNowCache._dtsAmbiguousOffsetStart = overlapStart.Ticks;
                                    dateTimeNowCache._dtsAmbiguousOffsetEnd = overlapEnd.Ticks;

                                    return dateTimeNowCache;
                                }
                            }
                        }

                        // No ambiguous period found
                        dateTimeNowCache._dtsAmbiguousOffsetStart = 0;
                        dateTimeNowCache._dtsAmbiguousOffsetEnd = 0;

                        return dateTimeNowCache;
                    }
                }
            }

            // no transition for this Year
            dateTimeNowCache._nextUtcNowTransitionTicks = utcNow.Year >= MaxYear ? DateTime.MaxTicks : new DateTime(utcNow.Year + 1, 1, 1).Ticks; // check again next year
            dateTimeNowCache._nowUtcOffsetTicks = _baseUtcOffset.Ticks;
            dateTimeNowCache._dtsAmbiguousOffsetStart = 0;
            dateTimeNowCache._dtsAmbiguousOffsetEnd = 0;

            return dateTimeNowCache;
        }

        /// <summary>
        /// Determines whether the specified local date and time is invalid in the current time zone.
        /// An invalid local time is a time that does not exist due to a daylight saving time transition.
        /// </summary>
        /// <param name="localDateTime">The local date and time to check.</param>
        /// <returns>True if the specified local date and time is invalid; otherwise, false.</returns>
        private bool IsInvalidLocalTime(DateTime localDateTime) => !TryGetUtcOffset(localDateTime, out _);

        /// <summary>
        /// Determines whether the specified local date and time is ambiguous in the current time zone.
        /// An ambiguous local time is a time that occurs twice due to a daylight saving time transition.
        /// </summary>
        /// <param name="localDateTime">The local date and time to check.</param>
        /// <param name="offsets">A span to fill with the possible UTC offsets.</param>
        /// <returns>True if the specified local date and time is ambiguous; otherwise, false.</returns>
        private bool IsAmbiguousLocalTime(DateTime localDateTime, Span<TimeSpan> offsets = default)
        {
            if (!TryGetTransitionsForYear(localDateTime.Year, out (int index, int count) transitionInfo))
            {
                return false;
            }

            TimeTransition[] transitions = _yearsTransitions;
            int boundary = transitionInfo.index + transitionInfo.count;

            Debug.Assert(boundary <= _yearsTransitionsCount && transitions is not null);

            int encountered = 0;
            long ticks = localDateTime.Ticks - _baseUtcOffset.Ticks;

            for (int i = transitionInfo.index; i < boundary; i++)
            {
                TimeTransition transition = transitions[i];
                long t = ticks - transition.Offset.Ticks;

                if (t >= transition.DateStart.Ticks && t <= transition.DateEnd.Ticks)
                {
                    if (encountered < offsets.Length)
                    {
                        offsets[encountered] = transition.Offset + _baseUtcOffset;
                    }

                    encountered++;
                }
            }

            if (offsets.Length >= 2 && offsets[0] > offsets[1])
            {
                // Keep app compatibility returning offsets sorted from least to greatest
                TimeSpan offset = offsets[0];
                offsets[0] = offsets[1];
                offsets[1] = offset;
            }

            return encountered > 1; // More than one transition means the local time is ambiguous
        }

        private bool IsDaylightSavingOn(DateTime localDateTime)
        {
            if (!TryGetTransitionsForYear(localDateTime.Year, out (int index, int count) transitionInfo))
            {
                return false;
            }

            TimeTransition[] transitions = _yearsTransitions;
            int boundary = transitionInfo.index + transitionInfo.count;

            Debug.Assert(boundary <= _yearsTransitionsCount && transitions is not null);

            long ticks = localDateTime.Ticks - _baseUtcOffset.Ticks;

            for (int i = transitionInfo.index; i < boundary; i++)
            {
                TimeTransition transition = transitions[i];
                long t = ticks - transition.Offset.Ticks;

                if (t >= transition.DateStart.Ticks && t <= transition.DateEnd.Ticks)
                {
                    bool ret = transition.DaylightSavingOn;

                    if (i + 1 < boundary && transition.DaylightSavingOn)
                    {
                        transition = transitions[i + 1];
                        t = ticks - transition.Offset.Ticks;

                        // Ambiguous time in daylight saving time period, prefer reporting the standard time status
                        if (t >= transition.DateStart.Ticks && t <= transition.DateEnd.Ticks)
                        {
                            return false;
                        }
                    }

                    return ret;
                }
            }

            return false;
        }

        /// <summary>
        /// Tries to convert a local date and time to Coordinated Universal Time (UTC).
        /// </summary>
        /// <param name="localDateTime">The local date and time to convert.</param>
        /// <param name="utcDateTime">When this method returns, contains the UTC date and time if the conversion succeeded.</param>
        /// <returns>True if the conversion was successful; otherwise, false.</returns>
        /// <remarks>
        /// This method attempts to convert a local time to UTC. It returns false if the local time is invalid,
        /// such as during a daylight saving time transition when the local time does not exist.
        /// </remarks>
        private bool TryLocalToUtc(DateTime localDateTime, out DateTime utcDateTime)
        {
            if (TryGetUtcOffset(localDateTime, out TimeSpan offset))
            {
                long ticks = localDateTime.Ticks - offset.Ticks;
                utcDateTime = SafeCreateDateTimeFromTicks(ticks, DateTimeKind.Utc);
                return true;
            }

            utcDateTime = default;
            return false;
        }

        /// <summary>
        /// Gets the UTC offset for a given UTC date and time, along with whether it is in daylight saving time.
        /// </summary>
        /// <param name="utcDateTime">The UTC date and time.</param>
        /// <param name="isDaylightSavingTime">When this method returns, indicates whether the time is in daylight saving time.</param>
        /// <returns>The UTC offset for the specified UTC date and time.</returns>
        private TimeSpan GetOffsetForUtcDate(DateTime utcDateTime, out bool isDaylightSavingTime)
        {
            if (TryGetTransitionsForYear(utcDateTime.Year, out (int index, int count) transitionInfo))
            {
                TimeTransition[] transitions = _yearsTransitions;
                int boundary = transitionInfo.index + transitionInfo.count;

                Debug.Assert(boundary <= _yearsTransitionsCount && transitions is not null);

                for (int i = transitionInfo.index; i < boundary; i++)
                {
                    TimeTransition transition = transitions[i];
                    if (utcDateTime >= transition.DateStart && utcDateTime <= transition.DateEnd)
                    {
                        isDaylightSavingTime = transition.DaylightSavingOn;
                        return _baseUtcOffset + transition.Offset;
                    }
                }
            }

            // no transitions found
            isDaylightSavingTime = false;
            return _baseUtcOffset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static DateTime SafeCreateDateTimeFromTicks(long ticks, DateTimeKind kind = DateTimeKind.Unspecified)
            => (ulong)ticks <= DateTime.MaxTicks ? new DateTime(ticks, kind) : (ticks < 0 ? DateTime.MinValue : DateTime.MaxValue);

        /// <summary>
        /// Gets the UTC offset for a given UTC date and time, along with whether it is in daylight saving time and if it is ambiguous.
        /// </summary>
        /// <param name="utcDateTime">The UTC date and time.</param>
        /// <param name="isDaylightSavingTime">When this method returns, indicates whether the time is in daylight saving time.</param>
        /// <param name="isAmbiguous">When this method returns, indicates whether the time is ambiguous.</param>
        /// <returns>The UTC offset for the specified UTC date and time.</returns>
        private TimeSpan GetOffsetForUtcDate(DateTime utcDateTime, out bool isDaylightSavingTime, out bool isAmbiguous)
        {
            TimeSpan offset = GetOffsetForUtcDate(utcDateTime, out isDaylightSavingTime);
            DateTime localTime = SafeCreateDateTimeFromTicks(utcDateTime.Ticks + offset.Ticks);

            // If the UTC time is not in daylight saving time, it is not considered ambiguous
            isAmbiguous = isDaylightSavingTime && IsAmbiguousLocalTime(localTime);
            return offset;
        }

        /// <summary>
        /// Converts a Coordinated Universal Time (UTC) to a local date and time.
        /// </summary>
        /// <param name="utcDateTime">The UTC date and time to convert.</param>
        /// <param name="isDaylightSavingTime">When this method returns, indicates whether the resulting local time is in daylight saving time.</param>
        /// <returns>The local date and time.</returns>
        private DateTime UtcToLocal(DateTime utcDateTime, out bool isDaylightSavingTime)
        {
            long ticks = utcDateTime.Ticks + GetOffsetForUtcDate(utcDateTime, out isDaylightSavingTime).Ticks;
            return SafeCreateDateTimeFromTicks(ticks);
        }

        /// <summary>
        /// Tries to get the UTC offset for a given local date and time.
        /// </summary>
        /// <param name="localDateTime">The local date and time.</param>
        /// <param name="offset">When this method returns, contains the UTC offset if the operation succeeded.</param>
        /// <returns>True if the UTC offset was successfully retrieved; otherwise, false.</returns>
        /// <remarks>
        /// It is possible to have invalid local times that do not map to a UTC offset.
        /// This can occur during transitions such as daylight saving time changes.
        /// </remarks>
        private bool TryGetUtcOffset(DateTime localDateTime, out TimeSpan offset)
        {
            if (!TryGetTransitionsForYear(localDateTime.Year, out (int index, int count) transitionInfo))
            {
                offset = _baseUtcOffset;
                return true;
            }

            TimeTransition[] transitions = _yearsTransitions;
            int boundary = transitionInfo.index + transitionInfo.count;

            Debug.Assert(boundary <= _yearsTransitionsCount && transitions is not null);

            for (int i = transitionInfo.index; i < boundary; i++)
            {
                TimeTransition transition = transitions[i];
                offset = _baseUtcOffset + transition.Offset;
                long localTicks = SafeCreateDateTimeFromTicks(localDateTime.Ticks - offset.Ticks).Ticks;
                if (localTicks >= transition.DateStart.Ticks && localTicks <= transition.DateEnd.Ticks) // Start and End dates in the transitions are inclusive
                {
                    // To keep the app compatibility, we need to prefer the standard offset over the DST offset except if the time has local kind and not created with KindLocalAmbiguousDst
                    if (i + 1 < boundary && transition.DaylightSavingOn && (localDateTime.Kind != DateTimeKind.Local || !localDateTime.IsAmbiguousDaylightSavingTime()))
                    {
                        transition = transitions[i + 1];
                        TimeSpan nextOffset = _baseUtcOffset + transition.Offset;
                        localTicks = SafeCreateDateTimeFromTicks(localDateTime.Ticks - nextOffset.Ticks).Ticks;
                        if (localTicks >= transition.DateStart.Ticks && localTicks <= transition.DateEnd.Ticks)
                        {
                            offset = nextOffset;
                        }
                    }

                    // Found the transition that applies to this local time
                    return true;
                }
            }

            offset = _baseUtcOffset; // No applicable transition found, use base offset
            return false;
        }

        /// <summary>
        /// Tries to get the cached transitions for a specific year.
        /// </summary>
        /// <param name="year">The year to get transitions for.</param>
        /// <param name="transitionInfo">A tuple containing the index and count of transitions.</param>
        /// <returns>True if transitions are found; otherwise, false.</returns>
        private bool TryGetTransitionsForYear(int year, out (int index, int count) transitionInfo)
        {
            if (_supportsDaylightSavingTime && _adjustmentRules is AdjustmentRule[] { Length: > 0 })
            {
                if (_transitionCache.TryGetValue(year, out int transitionData))
                {
                    if (transitionData == 0)
                    {
                        transitionInfo = (0, 0);
                        return false;
                    }

                    transitionInfo = (transitionData & 0xFFFF, transitionData >> 16);
                    return true;
                }

                transitionInfo = CacheTransitionsForYear(year);
                return transitionInfo != (0, 0);
            }

            transitionInfo = (0, 0);
            return false;
        }

        /// <summary>
        /// Finds the index of the adjustment rule that applies to the specified year.
        /// </summary>
        /// <param name="year">The year to find the adjustment rule for.</param>
        /// <returns>The index of the adjustment rule if found; otherwise, -1.</returns>
        private int FindRuleForYear(int year)
        {
            Debug.Assert(_adjustmentRules is not null && _adjustmentRules.Length > 0);

            int ruleIndex = -1;

            int top = 0;
            int bottom = _adjustmentRules.Length - 1;

            while (top <= bottom)
            {
                int mid = (top + bottom) / 2;
                var rule = _adjustmentRules[mid];

                if (year < rule.DateStart.Year)
                {
                    bottom = mid - 1;
                }
                else if (year > rule.DateEnd.Year)
                {
                    top = mid + 1;
                }
                else
                {
                    // Ensure both Utc and Local transitions are covered
                    while (mid > 0 &&
                            (_adjustmentRules[mid - 1].DateEnd.Year >= year ||
                            (_adjustmentRules[mid - 1].DateEnd + (_baseUtcOffset + _adjustmentRules[mid - 1].BaseUtcOffsetDelta + _adjustmentRules[mid - 1].DaylightDelta)).Year >= year))
                    {
                        mid--;
                    }

                    return mid;
                }
            }

            return ruleIndex;
        }

        /// <summary>
        /// Grows the given pool array to accommodate the required capacity.
        /// </summary>
        /// <typeparam name="T">The type of elements in the array.</typeparam>
        /// <param name="arrayPoolArray">The array to grow.</param>
        /// <param name="requiredCapacity">The required capacity.</param>
        private static void ArrayPoolGrow<T>(ref T[] arrayPoolArray, int requiredCapacity)
        {
            T[] tmp = ArrayPool<T>.Shared.Rent(Math.Max(arrayPoolArray.Length * 2, requiredCapacity));
            arrayPoolArray.CopyTo(tmp.AsSpan());
            ArrayPool<T>.Shared.Return(arrayPoolArray);
            arrayPoolArray = tmp;
        }

        /// <summary>
        /// Adds a transition to the transitions array, growing the array if necessary.
        /// </summary>
        /// <param name="transitions">The array of transitions.</param>
        /// <param name="count">The current count of transitions.</param>
        /// <param name="transition">The transition to add.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddTransition(ref TimeTransition[] transitions, ref int count, TimeTransition transition)
        {
            if (count > 0 && transitions[count - 1].Offset == transition.Offset && transitions[count - 1].DaylightSavingOn == transition.DaylightSavingOn)
            {
                // If the last transition has the same offset and daylight saving status, we can merge them
                transitions[count - 1] = transitions[count - 1] with { DateEnd = transition.DateEnd };
                return;
            }

            if (count >= transitions.Length)
            {
                ArrayPoolGrow(ref transitions, count * 2);
            }

            transitions[count++] = transition;
        }

        /// <summary>
        /// Converts the start transition time of the adjustment rule to a DateTime for the specified year.
        /// </summary>
        /// <param name="year">The year for which to calculate the transition time.</param>
        /// <param name="rule">The adjustment rule containing the transition information.</param>
        /// <returns>A DateTime representing the start transition time.</returns>
        private static DateTime StartTransitionTimeToDateTime(int year, AdjustmentRule rule)
            => rule.IsStartDateMarkerForBeginningOfYear() ? // Windows special case when the year transition starts at the beginning of the year
                new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Unspecified) :
                TransitionTimeToDateTime(year, rule.DaylightTransitionStart);

        /// <summary>
        /// Converts the end transition time of the adjustment rule to a DateTime for the specified year.
        /// </summary>
        /// <param name="year">The year for which to calculate the transition time.</param>
        /// <param name="rule">The adjustment rule containing the transition information.</param>
        /// <returns>A DateTime representing the end transition time.</returns>
        private static DateTime EndTransitionTimeToDateTime(int year, AdjustmentRule rule)
            => rule.IsEndDateMarkerForEndOfYear() ? // Windows special case when the year transition ends at the end of the year
                year >= MaxYear ? DateTime.MaxValue : new DateTime(year + 1, 1, 1, 0, 0, 0, DateTimeKind.Unspecified) :
                TransitionTimeToDateTime(year, rule.DaylightTransitionEnd);

        /// <summary>
        /// Helper function that converts a year and TransitionTime into a DateTime.
        /// </summary>
        internal static DateTime TransitionTimeToDateTime(int year, TransitionTime transitionTime)
        {
            DateTime value;
            TimeSpan timeOfDay = transitionTime.TimeOfDay.TimeOfDay;

            if (transitionTime.IsFixedDateRule)
            {
                // create a DateTime from the passed in year and the properties on the transitionTime

                int day = transitionTime.Day;
                // if the day is out of range for the month then use the last day of the month
                if (day > 28)
                {
                    int daysInMonth = DateTime.DaysInMonth(year, transitionTime.Month);
                    if (day > daysInMonth)
                    {
                        day = daysInMonth;
                    }
                }

                value = new DateTime(year, transitionTime.Month, day) + timeOfDay;
            }
            else
            {
                if (transitionTime.Week <= 4)
                {
                    //
                    // Get the (transitionTime.Week)th Sunday.
                    //
                    value = new DateTime(year, transitionTime.Month, 1) + timeOfDay;

                    int dayOfWeek = (int)value.DayOfWeek;
                    int delta = (int)transitionTime.DayOfWeek - dayOfWeek;
                    if (delta < 0)
                    {
                        delta += 7;
                    }
                    delta += 7 * (transitionTime.Week - 1);

                    if (delta > 0)
                    {
                        value = value.AddDays(delta);
                    }
                }
                else
                {
                    //
                    // If TransitionWeek is greater than 4, we will get the last week.
                    //
                    int daysInMonth = DateTime.DaysInMonth(year, transitionTime.Month);
                    value = new DateTime(year, transitionTime.Month, daysInMonth) + timeOfDay;

                    // This is the day of week for the last day of the month.
                    int dayOfWeek = (int)value.DayOfWeek;
                    int delta = dayOfWeek - (int)transitionTime.DayOfWeek;
                    if (delta < 0)
                    {
                        delta += 7;
                    }

                    if (delta > 0)
                    {
                        value = value.AddDays(-delta);
                    }
                }
            }
            return value;
        }

        /// <summary>
        /// Caches the time zone transitions for a specific year.
        /// </summary>
        /// <param name="year">The year for which to cache transitions.</param>
        /// <returns>A tuple containing the index and count of cached transitions.</returns>
        private (int index, int count) CacheTransitionsForYear(int year)
        {
            Debug.Assert(_adjustmentRules is not null && _adjustmentRules.Length > 0);

            int ruleIndex = FindRuleForYear(year);
            if (ruleIndex < 0)
            {
                // No rule found for the specified year

                _transitionCache[year] = 0;
                return (0, 0);
            }

            Debug.Assert(ruleIndex < _adjustmentRules.Length);

            const int InitialTransitionsCount = 20; // enough to cover all cases
            int transitionCount = 0;
            TimeTransition[] allTransitions = ArrayPool<TimeTransition>.Shared.Rent(InitialTransitionsCount);

            AdjustmentRule rule = _adjustmentRules[ruleIndex];
            if (rule.NoDaylightTransitions)
            {
                //
                // Linux rule, date start and end are stored in UTC
                //

                long localTicks = _baseUtcOffset.Ticks + rule.DateStart.Ticks + rule.BaseUtcOffsetDelta.Ticks;
                DateTime startOfYear = new DateTime(year, 1, 1);

                if (rule.DateStart > startOfYear || localTicks > startOfYear.Ticks) // Ensure local start of the year is covered
                {
                    //
                    // Handle transition from previous rule to current rule
                    //

                    AdjustmentRule? previousRule = ruleIndex > 0 ? _adjustmentRules[ruleIndex - 1] : null;

                    if (previousRule is not null && previousRule.DateEnd.Year >= year - 1)
                    {
                        if (previousRule.NoDaylightTransitions)
                        {
                            //
                            // Previous rule is Linux style with Utc start and end dates
                            //

                            DateTime previousYearTransitionEnd = SafeCreateDateTimeFromTicks(previousRule.DateEnd.Ticks + 1); // UTC coordinate
                            if (previousYearTransitionEnd.Ticks <= rule.DateStart.Ticks - 1)
                            {
                                // Gap between the last year transition end and current year transition start. no daylight
                                AddTransition(ref allTransitions, ref transitionCount,
                                    new TimeTransition(previousYearTransitionEnd, SafeCreateDateTimeFromTicks(rule.DateStart.Ticks - 1), previousRule.BaseUtcOffsetDelta, false));
                            }
                            else
                            {
                                // previous year transition end at the current year transition start, then include last year transition
                                AddTransition(ref allTransitions, ref transitionCount,
                                    new TimeTransition(previousRule.DateStart, previousRule.DateEnd, GetRuleFullUtcOffset(previousRule), previousRule.HasDaylightSaving));
                            }
                        }
                        else
                        {
                            //
                            // Previous rule is Windows style with local start and end dates
                            //

                            DateTime previousYearStart = StartTransitionTimeToDateTime(year - 1, previousRule); // Local coordinate
                            DateTime previousYearEnd = EndTransitionTimeToDateTime(year - 1, previousRule); // Local coordinate

                            if (previousYearStart < previousYearEnd)
                            {
                                //
                                // Previous year has one transition period
                                //

                                // Get previous rule end in UTC coordinates
                                DateTime previousYearEndUtc = GetUtcDateTimeFromLocalTicks(previousYearEnd.Ticks, previousRule, includeDaylightDelta: true, tickAdjustment: -1);

                                if (previousYearEndUtc.Ticks < rule.DateStart.Ticks - 1)
                                {
                                    // Gap between the last year transition end and current year transition start. no daylight
                                    AddTransition(ref allTransitions, ref transitionCount,
                                        new TimeTransition(previousYearEndUtc, SafeCreateDateTimeFromTicks(rule.DateStart.Ticks - 1), previousRule.BaseUtcOffsetDelta, false));
                                }
                                else
                                {
                                    // previous year transition end at the current year transition start, then include last year transition
                                    AddTransition(ref allTransitions, ref transitionCount,
                                        new TimeTransition(
                                                GetUtcDateTimeFromLocalTicks(previousYearStart.Ticks, previousRule),
                                                SafeCreateDateTimeFromTicks(rule.DateStart.Ticks - 1),
                                                GetRuleFullUtcOffset(previousRule),
                                                previousRule.HasDaylightSaving));
                                }
                            }
                            else // previousYearStart > previousYearEnd
                            {
                                //
                                // Previous year has two transition periods. One start from the year beginning while the second go through the end of the year
                                //

                                long previousEndOfYearUtcTicks = startOfYear.Ticks - GetTransitionUtcOffsetTicks(previousRule, includeDaylightDelta: true) - 1;
                                DateTime previousEndOfYearUtc = SafeCreateDateTimeFromTicks(previousEndOfYearUtcTicks, DateTimeKind.Utc);

                                if (previousEndOfYearUtc.Ticks < rule.DateStart.Ticks - 1)
                                {
                                    // Gap between the end of the last year and current year transition start. no daylight
                                    AddTransition(ref allTransitions, ref transitionCount,
                                        new TimeTransition(SafeCreateDateTimeFromTicks(previousEndOfYearUtc.Ticks + 1), SafeCreateDateTimeFromTicks(rule.DateStart.Ticks - 1), rule.BaseUtcOffsetDelta, false));
                                }

                                // The daylight start should be around the end of the year and go through the end
                                DateTime previousYearStartUtc = GetUtcDateTimeFromLocalTicks(previousYearStart.Ticks, previousRule); // daylight offset is not counted in this start

                                AddTransition(ref allTransitions, ref transitionCount,
                                    new TimeTransition(previousYearStartUtc, previousEndOfYearUtc, GetRuleFullUtcOffset(previousRule), previousRule.HasDaylightSaving));
                            }
                        }
                    }
                    else
                    {
                        // no rule is found
                        DateTime transitionStart = new DateTime(year > 1 ? year - 1 : 1, 1, 1);
                        transitionStart = SafeCreateDateTimeFromTicks(transitionStart.Ticks - _baseUtcOffset.Ticks);

                        if (transitionStart < rule.DateStart)
                        {
                            AddTransition(ref allTransitions, ref transitionCount,
                                new TimeTransition(transitionStart, SafeCreateDateTimeFromTicks(rule.DateStart.Ticks - 1), TimeSpan.Zero, false));
                        }
                    }
                }

                //
                // Add the current rule's transition
                //

                AddTransition(ref allTransitions, ref transitionCount,
                    new TimeTransition(rule.DateStart, rule.DateEnd, GetRuleFullUtcOffset(rule), rule.HasDaylightSaving));

                //
                // Ensure covering the whole year by adding transitions if necessary
                //

                DateTime endOfCurrentYear = year < MaxYear ? new DateTime(year + 1, 1, 1).AddTicks(-1) : DateTime.MaxValue;

                while (allTransitions[transitionCount - 1].DateEnd < endOfCurrentYear || // UTC end date still not reached the end of the year
                        allTransitions[transitionCount - 1].DateEnd.Ticks + allTransitions[transitionCount - 1].Offset.Ticks + _baseUtcOffset.Ticks < endOfCurrentYear.Ticks) // local end date still not reached the end of the year
                {
                    ruleIndex++;
                    if (ruleIndex < _adjustmentRules.Length && _adjustmentRules[ruleIndex].DateStart.Year <= allTransitions[transitionCount - 1].DateEnd.Year) // ensure the year is included in the rule
                    {
                        AdjustmentRule? nextRule = _adjustmentRules[ruleIndex];
                        if (nextRule.NoDaylightTransitions)
                        {
                            //
                            // Next year Rule is Linux style with Utc start and end dates
                            //

                            if (allTransitions[transitionCount - 1].DateEnd.Ticks < nextRule.DateStart.Ticks - 1)
                            {
                                AddTransition(ref allTransitions, ref transitionCount,
                                    new TimeTransition(allTransitions[transitionCount - 1].DateEnd.AddTicks(1), nextRule.DateStart.AddTicks(-1), nextRule.BaseUtcOffsetDelta, false));
                            }

                            AddTransition(ref allTransitions, ref transitionCount,
                                new TimeTransition(nextRule.DateStart, nextRule.DateEnd, GetRuleFullUtcOffset(nextRule), nextRule.HasDaylightSaving));
                        }
                        else
                        {
                            //
                            // Next Rule is Windows style with local start and end dates
                            //

                            int nextYear = Math.Min(year + 1, nextRule.DateStart.Year);
                            DateTime nextYearStart = StartTransitionTimeToDateTime(nextYear, nextRule); // in local time
                            DateTime nextYearEnd = EndTransitionTimeToDateTime(nextYear, nextRule);     // in local time

                            if (nextYearStart < nextYearEnd)
                            {
                                //
                                // Next year has one transition
                                //

                                // Get next rule start in UTC coordinates
                                DateTime nextYearStartUtc = GetUtcDateTimeFromLocalTicks(nextYearStart.Ticks, nextRule);
                                if (allTransitions[transitionCount - 1].DateEnd.Ticks < nextYearStartUtc.Ticks - 1)
                                {
                                    // Fill the transition gap between the previous year transition end and next year transition start
                                    AddTransition(ref allTransitions, ref transitionCount,
                                        new TimeTransition(allTransitions[transitionCount - 1].DateEnd.AddTicks(1), nextYearStartUtc.AddTicks(-1), nextRule.BaseUtcOffsetDelta, false));
                                }

                                // Add next year transition
                                AddTransition(ref allTransitions, ref transitionCount,
                                    new TimeTransition(
                                            nextYearStartUtc,
                                            GetUtcDateTimeFromLocalTicks(nextYearEnd.Ticks, nextRule, includeDaylightDelta: true, tickAdjustment: -1),
                                            GetRuleFullUtcOffset(nextRule),
                                            nextRule.HasDaylightSaving));
                            }
                            else // nextYearStart > nextYearEnd
                            {
                                //
                                // Next year has two transitions, the first start at the beginning of the year and the second ends at the end of the year
                                //

                                // The daylight should be starting from the beginning in the year and then go till next year transition end date.
                                DateTime nextYearEndUtc = GetUtcDateTimeFromLocalTicks(nextYearEnd.Ticks, nextRule, includeDaylightDelta: true, tickAdjustment: -1);
                                DateTime nextBeginningOfYearUtc = GetUtcDateTimeFromLocalTicks(new DateTime(year + 1, 1, 1).Ticks, nextRule, includeDaylightDelta: true);

                                if (allTransitions[transitionCount - 1].DateEnd.Ticks < nextBeginningOfYearUtc.Ticks - 1)
                                {
                                    // Fill the gap between the previous year transition end and next year transition start
                                    AddTransition(ref allTransitions, ref transitionCount,
                                        // Use previous year rule, daylight included as the year started with the daylight
                                        new TimeTransition(allTransitions[transitionCount - 1].DateEnd.AddTicks(1), nextBeginningOfYearUtc.AddTicks(-1), GetRuleFullUtcOffset(rule), rule.HasDaylightSaving));
                                }

                                if (allTransitions[transitionCount - 1].DateEnd < nextYearEndUtc)
                                {
                                    AddTransition(ref allTransitions, ref transitionCount,
                                        new TimeTransition(
                                                allTransitions[transitionCount - 1].DateEnd.AddTicks(1),
                                                nextYearEndUtc,
                                                GetRuleFullUtcOffset(nextRule),
                                                nextRule.HasDaylightSaving));
                                }
                            }

                            if (allTransitions[transitionCount - 1].DateEnd < endOfCurrentYear)
                            {
                                // Ensure covering up to and including the end of the year in local and Utc time
                                if (nextRule.BaseUtcOffsetDelta.Ticks > 0)
                                {
                                    endOfCurrentYear = SafeCreateDateTimeFromTicks(endOfCurrentYear.Ticks + nextRule.BaseUtcOffsetDelta.Ticks);
                                }

                                AddTransition(ref allTransitions, ref transitionCount,
                                    new TimeTransition(allTransitions[transitionCount - 1].DateEnd.AddTicks(1), endOfCurrentYear, nextRule.BaseUtcOffsetDelta, false));
                            }

                            break; // We know Windows Rules cover the whole year
                        }
                    }
                    else
                    {
                        //
                        // There is no rule for next year
                        //

                        DateTime endOfNextYear = year + 1 < MaxYear ? new DateTime(year + 2, 1, 1).AddTicks(-1) : DateTime.MaxValue;

                        if (allTransitions[transitionCount - 1].DateEnd < endOfNextYear)
                        {
                            AddTransition(ref allTransitions, ref transitionCount,
                                new TimeTransition(allTransitions[transitionCount - 1].DateEnd.AddTicks(1), endOfNextYear, TimeSpan.Zero, false));
                        }

                        // The whole next year should be covered now
                        break;
                    }
                }
            }
            else
            {
                //
                // Windows style rule, dates stored as local time
                //

                DateTime localStart = StartTransitionTimeToDateTime(year, rule); // Local time
                DateTime localEnd = EndTransitionTimeToDateTime(year, rule); // Local time

                bool startWithDaylightOn = rule.IsStartDateMarkerForBeginningOfYear();

                DateTime utcStart = GetUtcDateTimeFromLocalTicks(localStart.Ticks, rule, includeDaylightDelta: startWithDaylightOn, kind: DateTimeKind.Utc);
                DateTime utcEnd = GetUtcDateTimeFromLocalTicks(localEnd.Ticks, rule, includeDaylightDelta: true, tickAdjustment: -1, kind: DateTimeKind.Utc);

                long startOfCurrentYearUtcTicks = new DateTime(year, 1, 1).Ticks - GetTransitionUtcOffsetTicks(rule, includeDaylightDelta: startWithDaylightOn || localStart > localEnd);
                DateTime startOfCurrentYearUtc = SafeCreateDateTimeFromTicks(startOfCurrentYearUtcTicks, DateTimeKind.Utc);
                DateTime currentYearStart = new DateTime(year, 1, 1);

                if (localStart < localEnd)
                {
                    //
                    // This year has one transition
                    //

                    if (utcStart > currentYearStart || localStart > currentYearStart) // check either the year start in Utc or local are covering the beginning of the year
                    {
                        //
                        // cover the beginning of the current year using the previous year transitions
                        //

                        AdjustmentRule? previousRule =
                            rule.DateStart.Year < year ?
                                rule :
                                ruleIndex > 0 &&
                                _adjustmentRules[ruleIndex - 1].DateStart.Year <= year - 1 &&
                                _adjustmentRules[ruleIndex - 1].DateEnd.Year >= year - 1 ?
                                    _adjustmentRules[ruleIndex - 1] : null;

                        if (previousRule is not null)
                        {
                            if (previousRule.NoDaylightTransitions)
                            {
                                //
                                // Previous rule is Linux style with Utc start and end dates
                                //

                                DateTime previousYearTransitionEnd = SafeCreateDateTimeFromTicks(previousRule.DateEnd.Ticks + 1); // UTC coordinate
                                if (previousYearTransitionEnd.Ticks <= startOfCurrentYearUtc.Ticks - 1)
                                {
                                    // Gap between the last year transition end and current year transition start. no daylight
                                    AddTransition(ref allTransitions, ref transitionCount,
                                        new TimeTransition(previousYearTransitionEnd, SafeCreateDateTimeFromTicks(startOfCurrentYearUtc.Ticks - 1), previousRule.BaseUtcOffsetDelta, false));
                                }
                                else
                                {
                                    AddTransition(ref allTransitions, ref transitionCount,
                                        new TimeTransition(previousRule.DateStart, previousRule.DateEnd, GetRuleFullUtcOffset(previousRule), previousRule.HasDaylightSaving));

                                }
                            }
                            else
                            {
                                //
                                // Previous rule is Windows style with local start and end dates
                                //

                                DateTime previousYearStart = StartTransitionTimeToDateTime(year - 1, previousRule); // Local time
                                DateTime previousYearEnd = EndTransitionTimeToDateTime(year - 1, previousRule); // Local time

                                if (previousYearStart < previousYearEnd)
                                {
                                    //
                                    // Previous year has one transition period
                                    //

                                    // Get previous rule end in UTC coordinates
                                    DateTime previousYearEndUtc = GetUtcDateTimeFromLocalTicks(previousYearEnd.Ticks, previousRule, includeDaylightDelta: true);

                                    // previous year transition end at the current year transition start, then include last year transition
                                    AddTransition(ref allTransitions, ref transitionCount,
                                        new TimeTransition(
                                                GetUtcDateTimeFromLocalTicks(previousYearStart.Ticks, previousRule),
                                                SafeCreateDateTimeFromTicks(previousYearEndUtc.Ticks - 1),
                                                GetRuleFullUtcOffset(previousRule),
                                                previousRule.HasDaylightSaving));

                                    if (previousYearEndUtc.Ticks < startOfCurrentYearUtc.Ticks - 1)
                                    {
                                        // Gap between the last year transition end and current year transition start. no daylight
                                        AddTransition(ref allTransitions, ref transitionCount,
                                            new TimeTransition(previousYearEndUtc, startOfCurrentYearUtc.AddTicks(-1), previousRule.BaseUtcOffsetDelta, false));
                                    }
                                }
                                else // previousYearStart > previousYearEnd
                                {
                                    //
                                    // Previous year has two transition periods. One start from the year beginning while the second go through the end of the year
                                    //

                                    // The daylight start should be around the end of the year and go through the end
                                    DateTime previousYearStartUtc = GetUtcDateTimeFromLocalTicks(previousYearStart.Ticks, previousRule); // daylight offset is not counted in this start
                                    AddTransition(ref allTransitions, ref transitionCount,
                                        new TimeTransition(
                                            previousYearStartUtc,
                                            SafeCreateDateTimeFromTicks(startOfCurrentYearUtc.Ticks - 1),
                                            GetRuleFullUtcOffset(previousRule),
                                            previousRule.HasDaylightSaving));
                                }
                            }
                        }
                        else
                        {
                            // No rule found for the previous year
                            DateTime previousYearStart = new DateTime(year > 1 ? year - 1 : 1, 1, 1); // real Utc year start

                            if (previousYearStart < startOfCurrentYearUtc)
                            {
                                AddTransition(ref allTransitions, ref transitionCount,
                                    new TimeTransition(previousYearStart, startOfCurrentYearUtc.AddTicks(-1), TimeSpan.Zero, false));
                            }
                        }

                        if (startOfCurrentYearUtc.Ticks < utcStart.Ticks - 1)
                        {
                            AddTransition(ref allTransitions, ref transitionCount,
                                new TimeTransition(startOfCurrentYearUtc, utcStart.AddTicks(-1), rule.BaseUtcOffsetDelta, false));
                        }
                    }

                    //
                    // Now add the current rule
                    //

                    AddTransition(ref allTransitions, ref transitionCount,
                        new TimeTransition(utcStart, utcEnd, GetRuleFullUtcOffset(rule), rule.HasDaylightSaving));

                    //
                    // Check if we still need to cover the rest of the year
                    //

                    DateTime endOfCurrentYear = year < MaxYear ? new DateTime(year + 1, 1, 1).AddTicks(-1) : DateTime.MaxValue;
                    long endOfCurrentYearUtcTicks = endOfCurrentYear.Ticks - GetTransitionUtcOffsetTicks(rule, includeDaylightDelta: rule.IsEndDateMarkerForEndOfYear());
                    DateTime endOfCurrentYearUtc = SafeCreateDateTimeFromTicks(endOfCurrentYearUtcTicks, DateTimeKind.Utc);
                    if (utcEnd < endOfCurrentYearUtc)
                    {
                        AddTransition(ref allTransitions, ref transitionCount,
                            new TimeTransition(utcEnd.AddTicks(1), endOfCurrentYearUtc, rule.BaseUtcOffsetDelta, false));
                    }

                    // Check if end of the current year Utc time cover the whole year, we are sure the whole local year is already covered
                    if (endOfCurrentYearUtc < endOfCurrentYear)
                    {
                        AdjustmentRule? nextRule = (rule.DateStart.Year <= year + 1 &&
                            rule.DateEnd.Year >= year + 1) ?
                            rule : ruleIndex < _adjustmentRules.Length - 1 &&
                                _adjustmentRules[ruleIndex + 1].DateStart.Year <= year + 1 &&
                                _adjustmentRules[ruleIndex + 1].DateEnd.Year >= year + 1 ?
                                _adjustmentRules[ruleIndex + 1] : null;

                        if (nextRule is not null)
                        {
                            if (nextRule.NoDaylightTransitions)
                            {
                                //
                                // Linux style rule, it is unlikely case we get a Linux style rule after Windows style rule but we handle it anyway just in case in the future things can change
                                //

                                if (endOfCurrentYearUtc.Ticks < nextRule.DateStart.Ticks - 1)
                                {
                                    AddTransition(ref allTransitions, ref transitionCount,
                                        new TimeTransition(endOfCurrentYearUtc.AddTicks(1), nextRule.DateStart.AddTicks(-1), nextRule.BaseUtcOffsetDelta, false));
                                }

                                AddTransition(ref allTransitions, ref transitionCount,
                                    new TimeTransition(nextRule.DateStart, nextRule.DateEnd, GetRuleFullUtcOffset(nextRule), nextRule.HasDaylightSaving));
                            }
                            else
                            {
                                //
                                // Next rule is Windows style with local start and end dates
                                //

                                DateTime nextYearStart = StartTransitionTimeToDateTime(year + 1, nextRule); // Local time
                                DateTime nextYearEnd = EndTransitionTimeToDateTime(year + 1, nextRule); // Local time

                                DateTime nextYearStartUtc = GetUtcDateTimeFromLocalTicks(nextYearStart.Ticks, nextRule, includeDaylightDelta: nextRule.IsStartDateMarkerForBeginningOfYear());
                                DateTime nextYearEndUtc = GetUtcDateTimeFromLocalTicks(nextYearEnd.Ticks, nextRule, includeDaylightDelta: true, tickAdjustment: -1);

                                if (nextYearStart < nextYearEnd)
                                {
                                    //
                                    // One transition only in this next year
                                    //

                                    if (endOfCurrentYearUtc.Ticks < nextYearStartUtc.Ticks - 1)
                                    {
                                        AddTransition(ref allTransitions, ref transitionCount,
                                            new TimeTransition(endOfCurrentYearUtc.AddTicks(1), nextYearStartUtc.AddTicks(-1), nextRule.BaseUtcOffsetDelta, false));
                                    }

                                    AddTransition(ref allTransitions, ref transitionCount,
                                        new TimeTransition(nextYearStartUtc, nextYearEndUtc, GetRuleFullUtcOffset(nextRule), nextRule.HasDaylightSaving));
                                }
                                else // nextYearStart > nextYearEnd
                                {
                                    //
                                    // Next year has two transitions, the first starts at the beginning of the year, and the second ends at the end of the year
                                    //

                                    if (endOfCurrentYearUtc < nextYearEndUtc)
                                    {
                                        AddTransition(ref allTransitions, ref transitionCount,
                                            new TimeTransition(endOfCurrentYearUtc.AddTicks(1), nextYearEndUtc, GetRuleFullUtcOffset(nextRule), nextRule.HasDaylightSaving));
                                    }
                                }
                            }
                        }
                        else
                        {
                            // no rule for next year
                            // endOfCurrentYearUtc < endOfCurrentYear
                            AddTransition(ref allTransitions, ref transitionCount,
                                new TimeTransition(endOfCurrentYearUtc.AddTicks(1), endOfCurrentYear, TimeSpan.Zero, false));
                        }
                    }
                }
                else // localStart > localEnd
                {
                    //
                    // This rule has two transitions, the first starts at the beginning of the year, and the second ends at the end of the year
                    //

                    // Check if we need to cover any previous transitions to ensure covering the whole year.
                    // We know the local time is covered, we need to check only the Utc time
                    if (startOfCurrentYearUtc > currentYearStart)
                    {
                        // need to cover the previous year transitions
                        AdjustmentRule? previousRule = rule.DateStart.Year < year ?
                                                                rule :
                                                                ruleIndex > 0 &&
                                                                _adjustmentRules[ruleIndex - 1].DateStart.Year <= year - 1 &&
                                                                _adjustmentRules[ruleIndex - 1].DateEnd.Year >= year - 1 ?
                                                                _adjustmentRules[ruleIndex - 1] : null;
                        if (previousRule is not null)
                        {
                            if (previousRule.NoDaylightTransitions)
                            {
                                //
                                // Previous year has Linux style rule
                                //

                                if (startOfCurrentYearUtc.Ticks > previousRule.DateEnd.Ticks - 1)
                                {
                                    AddTransition(ref allTransitions, ref transitionCount,
                                        new TimeTransition(previousRule.DateEnd.AddTicks(1), startOfCurrentYearUtc.AddTicks(-1), rule.BaseUtcOffsetDelta, false));
                                }

                                AddTransition(ref allTransitions, ref transitionCount,
                                    new TimeTransition(previousRule.DateStart, previousRule.DateEnd, GetRuleFullUtcOffset(previousRule), previousRule.HasDaylightSaving));
                            }
                            else
                            {
                                //
                                // Previous year has Windows style rule
                                //

                                DateTime previousYearStart = StartTransitionTimeToDateTime(year - 1, previousRule); // Local time
                                DateTime previousYearEnd = EndTransitionTimeToDateTime(year - 1, previousRule); // Local time
                                DateTime previousYearStartUtc = GetUtcDateTimeFromLocalTicks(previousYearStart.Ticks, previousRule, includeDaylightDelta: previousRule.IsStartDateMarkerForBeginningOfYear());

                                if (previousYearStart < previousYearEnd)
                                {
                                    //
                                    // Previous year has one transition
                                    //

                                    DateTime previousYearEndUtc = GetUtcDateTimeFromLocalTicks(previousYearEnd.Ticks, previousRule, includeDaylightDelta: true, tickAdjustment: -1);

                                    if (previousYearEndUtc.Ticks < startOfCurrentYearUtc.Ticks - 1)
                                    {
                                        AddTransition(ref allTransitions, ref transitionCount,
                                            new TimeTransition(previousYearEndUtc.AddTicks(1), startOfCurrentYearUtc.AddTicks(-1), previousRule.BaseUtcOffsetDelta, false));
                                    }
                                    else
                                    {

                                        AddTransition(ref allTransitions, ref transitionCount,
                                            new TimeTransition(previousYearStartUtc, previousYearEndUtc, GetRuleFullUtcOffset(previousRule), previousRule.HasDaylightSaving));
                                    }
                                }
                                else // previousYearStart > previousYearEnd
                                {
                                    //
                                    // Previous year has two transitions
                                    //

                                    if (previousYearStartUtc < startOfCurrentYearUtc)
                                    {
                                        // The previous year ends with the daylight transition start
                                        AddTransition(ref allTransitions, ref transitionCount,
                                            new TimeTransition(
                                                    previousYearStartUtc,
                                                    startOfCurrentYearUtc.AddTicks(-1),
                                                    GetRuleFullUtcOffset(previousRule),
                                                    previousRule.HasDaylightSaving));
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (currentYearStart < startOfCurrentYearUtc)
                            {
                                // No previous year rule found, handle accordingly
                                AddTransition(ref allTransitions, ref transitionCount,
                                    new TimeTransition(currentYearStart, startOfCurrentYearUtc.AddTicks(-1), TimeSpan.Zero, false));
                            }
                        }
                    }

                    //
                    // Now add current year transitions
                    //

                    DateTime endOfCurrentYear = year < MaxYear ? new DateTime(year + 1, 1, 1).AddTicks(-1) : DateTime.MaxValue;
                    long endOfCurrentYearUtcTicks = endOfCurrentYear.Ticks - GetTransitionUtcOffsetTicks(rule, includeDaylightDelta: true);
                    DateTime endOfCurrentYearUtc = SafeCreateDateTimeFromTicks(endOfCurrentYearUtcTicks, DateTimeKind.Utc);

                    AddTransition(ref allTransitions, ref transitionCount,
                        new TimeTransition(startOfCurrentYearUtc, utcEnd, GetRuleFullUtcOffset(rule), rule.HasDaylightSaving));

                    AddTransition(ref allTransitions, ref transitionCount,
                        new TimeTransition(SafeCreateDateTimeFromTicks(utcEnd.Ticks + 1), SafeCreateDateTimeFromTicks(utcStart.Ticks - 1), rule.BaseUtcOffsetDelta, false));

                    AddTransition(ref allTransitions, ref transitionCount,
                        new TimeTransition(utcStart, endOfCurrentYearUtc, GetRuleFullUtcOffset(rule), rule.HasDaylightSaving));

                    //
                    // Check if we need to add more transitions to cover the the end of the year in Utc time. We already know we are covering the end of the year in local time.
                    //

                    if (endOfCurrentYearUtc < endOfCurrentYear)
                    {
                        //
                        // Cover the transition in the next year in Utc time.
                        //

                        AdjustmentRule? nextRule = rule.DateEnd.Year > year ? rule :
                                                        ruleIndex < _adjustmentRules.Length - 1 && _adjustmentRules[ruleIndex + 1].DateStart.Year <= year + 1 && _adjustmentRules[ruleIndex + 1].DateEnd.Year >= year + 1 ?
                                                            _adjustmentRules[ruleIndex + 1] :
                                                            null;

                        if (nextRule is not null)
                        {
                            if (nextRule.NoDaylightTransitions)
                            {
                                //
                                // Next year rule is Linux style rule. It is unlikely to get Linux style rules after Windows style rule, but handle gracefully.
                                //

                                if (endOfCurrentYearUtc.Ticks < nextRule.DateStart.Ticks)
                                {
                                    AddTransition(ref allTransitions, ref transitionCount,
                                        new TimeTransition(endOfCurrentYearUtc.AddTicks(1), nextRule.DateStart.AddTicks(-1), nextRule.BaseUtcOffsetDelta, false)); // No daylight transitions
                                }
                                else
                                {
                                    if (endOfCurrentYearUtc < nextRule.DateEnd)
                                    {
                                        AddTransition(ref allTransitions, ref transitionCount,
                                            new TimeTransition(
                                                    endOfCurrentYearUtc.AddTicks(1),
                                                    nextRule.DateEnd,
                                                    GetRuleFullUtcOffset(nextRule),
                                                    nextRule.HasDaylightSaving));
                                    }
                                }
                            }
                            else
                            {
                                //
                                // Next year rule is Windows style rule.
                                //

                                DateTime nextYearStart = StartTransitionTimeToDateTime(year + 1, nextRule); // in local time
                                DateTime nextYearEnd = EndTransitionTimeToDateTime(year + 1, nextRule);     // in local time

                                DateTime nextYearStartUtc = GetUtcDateTimeFromLocalTicks(nextYearStart.Ticks, nextRule, includeDaylightDelta: nextRule.IsStartDateMarkerForBeginningOfYear());
                                DateTime nextYearEndUtc = GetUtcDateTimeFromLocalTicks(nextYearEnd.Ticks, nextRule, includeDaylightDelta: true, tickAdjustment: -1);

                                if (nextYearStart < nextYearEnd)
                                {
                                    //
                                    // Next year rule has one transition
                                    //
                                    if (endOfCurrentYearUtc.Ticks < nextYearStartUtc.Ticks - 1)
                                    {
                                        AddTransition(ref allTransitions, ref transitionCount,
                                            new TimeTransition(endOfCurrentYearUtc.AddTicks(1), nextYearStartUtc.AddTicks(-1), nextRule.BaseUtcOffsetDelta, false)); // No daylight transitions
                                    }

                                    AddTransition(ref allTransitions, ref transitionCount,
                                        new TimeTransition(nextYearStartUtc, nextYearEndUtc, GetRuleFullUtcOffset(nextRule), nextRule.HasDaylightSaving));
                                }
                                else
                                {
                                    //
                                    // Next year rule has two transitions, the first starts at the beginning of the year, while the second ends at the end of the year
                                    //

                                    if (endOfCurrentYearUtc < nextYearEndUtc)
                                    {
                                        AddTransition(ref allTransitions, ref transitionCount,
                                            new TimeTransition(endOfCurrentYearUtc.AddTicks(1), nextYearEndUtc, GetRuleFullUtcOffset(nextRule), nextRule.HasDaylightSaving));
                                    }
                                }
                            }
                        }
                        else
                        {
                            // No rule is found
                            // endOfCurrentYearUtc < endOfCurrentYear
                            AddTransition(ref allTransitions, ref transitionCount,
                                new TimeTransition(endOfCurrentYearUtc.AddTicks(1), endOfCurrentYear, TimeSpan.Zero, false));
                        }
                    }
                }
            }

            (int index, int count) result = CacheTransitions(allTransitions, transitionCount, year);
            ArrayPool<TimeTransition>.Shared.Return(allTransitions);
            return result;
        }

        /// <summary>
        /// Caches the given time transitions for a specific year.
        /// </summary>
        /// <param name="allTransitions">An array of all time transitions.</param>
        /// <param name="count">The number of valid transitions in the array.</param>
        /// <param name="year">The year for which the transitions are cached.</param>
        /// <returns>A tuple containing the index and count of cached transitions.</returns>
        private (int index, int count) CacheTransitions(TimeTransition[] allTransitions, int count, int year)
        {
            Debug.Assert(count > 0 && allTransitions is not null && count < allTransitions.Length);

            lock (_transitionCache)
            {
                // We update _yearsTransitions and _yearsTransitionsCount under lock to ensure thread-safety.

                if (count + _yearsTransitionsCount > _yearsTransitions.Length)
                {
                    Array.Resize(ref _yearsTransitions, Math.Max(_yearsTransitions.Length * 2, count + _yearsTransitionsCount));
                }

                Array.Copy(allTransitions, 0, _yearsTransitions, _yearsTransitionsCount, count);
                int index = _yearsTransitionsCount;
                _yearsTransitionsCount += count;

                _transitionCache[year] = index | (count << 16);

                return (index, count);
            }
        }

        /// <summary>
        /// Calculates the full UTC transition offset ticks for a given adjustment rule.
        /// </summary>
        /// <param name="rule">The adjustment rule.</param>
        /// <param name="includeDaylightDelta">Whether to include the daylight saving delta.</param>
        /// <returns>The transition UTC offset ticks.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private long GetTransitionUtcOffsetTicks(AdjustmentRule rule, bool includeDaylightDelta = false) =>
            _baseUtcOffset.Ticks + rule.BaseUtcOffsetDelta.Ticks + (includeDaylightDelta ? rule.DaylightDelta.Ticks : 0);

        /// <summary>
        /// Converts local time to UTC by subtracting the combined UTC offset, with an additional tick adjustment.
        /// </summary>
        /// <param name="localTicks">The local time ticks.</param>
        /// <param name="rule">The adjustment rule.</param>
        /// <param name="includeDaylightDelta">Whether to include the daylight saving delta.</param>
        /// <param name="tickAdjustment">Additional ticks to add/subtract (e.g., -1 for end times).</param>
        /// <param name="kind">The DateTimeKind for the result.</param>
        /// <returns>A DateTime in UTC.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private DateTime GetUtcDateTimeFromLocalTicks(long localTicks, AdjustmentRule rule, bool includeDaylightDelta = false, long tickAdjustment = 0, DateTimeKind kind = DateTimeKind.Utc) =>
            SafeCreateDateTimeFromTicks(localTicks - GetTransitionUtcOffsetTicks(rule, includeDaylightDelta) + tickAdjustment, kind);

        /// <summary>
        /// Calculates the full UTC offset for a given adjustment rule, including daylight saving time if applicable.
        /// </summary>
        /// <param name="rule">The adjustment rule.</param>
        /// <returns>The combined offset including daylight saving time if the rule has it.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TimeSpan GetRuleFullUtcOffset(AdjustmentRule rule) =>
            rule.BaseUtcOffsetDelta + (rule.HasDaylightSaving ? rule.DaylightDelta : TimeSpan.Zero);

        // _transitionCache maps a year to int value. the low 16 bits store the index of the first transition for that year in _yearsTransitions.
        // the high 16 bits store the number of transitions for that year. We use concurrent dictionary for thread-safe access.
        private readonly ConcurrentDictionary<int, int> _transitionCache = new ConcurrentDictionary<int, int>();

        // _yearsTransitions stores all transitions for all cached years.
        // When accessing _yearsTransitions, store it in a local variable as it may be replaced by another thread.
        // _yearsTransitions can grow but never shrink. This guarantees indexes returned from _transitionCache are always valid.
        private TimeTransition[] _yearsTransitions = new TimeTransition[10]; // start with 10 transitions and grow as needed
        private int _yearsTransitionsCount;
        private const int MaxYear = 9999;

        private record struct TimeTransition(
            DateTime DateStart,     // UTC
            DateTime DateEnd,       // UTC
            TimeSpan Offset,        // Usually rule.BaseUtcOffsetDelta + rule.DaylightDelta
            bool DaylightSavingOn   // Indicates if daylight saving is active during this transition
        );

        private sealed class DateTimeNowCache
        {
            public long _nextUtcNowTransitionTicks; // ticks when the next transition starts
            public long _nowUtcOffsetTicks;         // offset in ticks to add to the UtcNow to get the local time
            public long _dtsAmbiguousOffsetStart;   // if the current transition is DST, the start of the ambiguous time range
            public long _dtsAmbiguousOffsetEnd;     // if the current transition is DST, the end of the ambiguous time range
        }
    }
}
