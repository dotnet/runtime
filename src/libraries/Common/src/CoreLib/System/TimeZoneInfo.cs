// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Serialization;
using System.Threading;

namespace System
{
    //
    // DateTime uses TimeZoneInfo under the hood for IsDaylightSavingTime, IsAmbiguousTime, and GetUtcOffset.
    // These TimeZoneInfo APIs can throw ArgumentException when an Invalid-Time is passed in.  To avoid this
    // unwanted behavior in DateTime public APIs, DateTime internally passes the
    // TimeZoneInfoOptions.NoThrowOnInvalidTime flag to internal TimeZoneInfo APIs.
    //
    // In the future we can consider exposing similar options on the public TimeZoneInfo APIs if there is enough
    // demand for this alternate behavior.
    //
    [Flags]
    internal enum TimeZoneInfoOptions
    {
        None = 1,
        NoThrowOnInvalidTime = 2
    };

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Core, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed partial class TimeZoneInfo : IEquatable<TimeZoneInfo?>, ISerializable, IDeserializationCallback
    {
        private enum TimeZoneInfoResult
        {
            Success = 0,
            TimeZoneNotFoundException = 1,
            InvalidTimeZoneException = 2,
            SecurityException = 3
        };

        private readonly string _id;
        private readonly string? _displayName;
        private readonly string? _standardDisplayName;
        private readonly string? _daylightDisplayName;
        private readonly TimeSpan _baseUtcOffset;
        private readonly bool _supportsDaylightSavingTime;
        private readonly AdjustmentRule[]? _adjustmentRules;

        // constants for TimeZoneInfo.Local and TimeZoneInfo.Utc
        private const string UtcId = "UTC";
        private const string LocalId = "Local";

        private static readonly TimeZoneInfo s_utcTimeZone = CreateCustomTimeZone(UtcId, TimeSpan.Zero, UtcId, UtcId);

        private static CachedData s_cachedData = new CachedData();

        //
        // All cached data are encapsulated in a helper class to allow consistent view even when the data are refreshed using ClearCachedData()
        //
        // For example, TimeZoneInfo.Local can be cleared by another thread calling TimeZoneInfo.ClearCachedData. Without the consistent snapshot,
        // there is a chance that the internal ConvertTime calls will throw since 'source' won't be reference equal to the new TimeZoneInfo.Local.
        //
        private sealed partial class CachedData
        {
            private volatile TimeZoneInfo? _localTimeZone;

            private TimeZoneInfo CreateLocal()
            {
                lock (this)
                {
                    TimeZoneInfo? timeZone = _localTimeZone;
                    if (timeZone == null)
                    {
                        timeZone = GetLocalTimeZone(this);

                        // this step is to break the reference equality
                        // between TimeZoneInfo.Local and a second time zone
                        // such as "Pacific Standard Time"
                        timeZone = new TimeZoneInfo(
                                            timeZone._id,
                                            timeZone._baseUtcOffset,
                                            timeZone._displayName,
                                            timeZone._standardDisplayName,
                                            timeZone._daylightDisplayName,
                                            timeZone._adjustmentRules,
                                            disableDaylightSavingTime: false);

                        _localTimeZone = timeZone;
                    }
                    return timeZone;
                }
            }

            public TimeZoneInfo Local
            {
                get
                {
                    TimeZoneInfo? timeZone = _localTimeZone;
                    if (timeZone == null)
                    {
                        timeZone = CreateLocal();
                    }
                    return timeZone;
                }
            }

            /// <summary>
            /// Helper function that returns the corresponding DateTimeKind for this TimeZoneInfo.
            /// </summary>
            public DateTimeKind GetCorrespondingKind(TimeZoneInfo? timeZone)
            {
                // We check reference equality to see if 'this' is the same as
                // TimeZoneInfo.Local or TimeZoneInfo.Utc.  This check is needed to
                // support setting the DateTime Kind property to 'Local' or
                // 'Utc' on the ConverTime(...) return value.
                //
                // Using reference equality instead of value equality was a
                // performance based design compromise.  The reference equality
                // has much greater performance, but it reduces the number of
                // returned DateTime's that can be properly set as 'Local' or 'Utc'.
                //
                // For example, the user could be converting to the TimeZoneInfo returned
                // by FindSystemTimeZoneById("Pacific Standard Time") and their local
                // machine may be in Pacific time.  If we used value equality to determine
                // the corresponding Kind then this conversion would be tagged as 'Local';
                // where as we are currently tagging the returned DateTime as 'Unspecified'
                // in this example.  Only when the user passes in TimeZoneInfo.Local or
                // TimeZoneInfo.Utc to the ConvertTime(...) methods will this check succeed.
                //
                return
                    ReferenceEquals(timeZone, s_utcTimeZone) ? DateTimeKind.Utc :
                    ReferenceEquals(timeZone, _localTimeZone) ? DateTimeKind.Local :
                    DateTimeKind.Unspecified;
            }

            public Dictionary<string, TimeZoneInfo>? _systemTimeZones;
            public ReadOnlyCollection<TimeZoneInfo>? _readOnlySystemTimeZones;
            public bool _allSystemTimeZonesRead;
        };

        // used by GetUtcOffsetFromUtc (DateTime.Now, DateTime.ToLocalTime) for max/min whole-day range checks
        private static readonly DateTime s_maxDateOnly = new DateTime(9999, 12, 31);
        private static readonly DateTime s_minDateOnly = new DateTime(1, 1, 2);

        public string Id => _id;

        public string DisplayName => _displayName ?? string.Empty;

        public string StandardName => _standardDisplayName ?? string.Empty;

        public string DaylightName => _daylightDisplayName ?? string.Empty;

        public TimeSpan BaseUtcOffset => _baseUtcOffset;

        public bool SupportsDaylightSavingTime => _supportsDaylightSavingTime;

        /// <summary>
        /// Returns an array of TimeSpan objects representing all of
        /// possible UTC offset values for this ambiguous time.
        /// </summary>
        public TimeSpan[] GetAmbiguousTimeOffsets(DateTimeOffset dateTimeOffset)
        {
            if (!SupportsDaylightSavingTime)
            {
                throw new ArgumentException(SR.Argument_DateTimeOffsetIsNotAmbiguous, nameof(dateTimeOffset));
            }

            DateTime adjustedTime = ConvertTime(dateTimeOffset, this).DateTime;

            bool isAmbiguous = false;
            int? ruleIndex;
            AdjustmentRule? rule = GetAdjustmentRuleForAmbiguousOffsets(adjustedTime, out ruleIndex);
            if (rule != null && rule.HasDaylightSaving)
            {
                DaylightTimeStruct daylightTime = GetDaylightTime(adjustedTime.Year, rule, ruleIndex);
                isAmbiguous = GetIsAmbiguousTime(adjustedTime, rule, daylightTime);
            }

            if (!isAmbiguous)
            {
                throw new ArgumentException(SR.Argument_DateTimeOffsetIsNotAmbiguous, nameof(dateTimeOffset));
            }

            // the passed in dateTime is ambiguous in this TimeZoneInfo instance
            TimeSpan[] timeSpans = new TimeSpan[2];

            TimeSpan actualUtcOffset = _baseUtcOffset + rule!.BaseUtcOffsetDelta;

            // the TimeSpan array must be sorted from least to greatest
            if (rule.DaylightDelta > TimeSpan.Zero)
            {
                timeSpans[0] = actualUtcOffset; // FUTURE:  + rule.StandardDelta;
                timeSpans[1] = actualUtcOffset + rule.DaylightDelta;
            }
            else
            {
                timeSpans[0] = actualUtcOffset + rule.DaylightDelta;
                timeSpans[1] = actualUtcOffset; // FUTURE: + rule.StandardDelta;
            }
            return timeSpans;
        }

        /// <summary>
        /// Returns an array of TimeSpan objects representing all of
        /// possible UTC offset values for this ambiguous time.
        /// </summary>
        public TimeSpan[] GetAmbiguousTimeOffsets(DateTime dateTime)
        {
            if (!SupportsDaylightSavingTime)
            {
                throw new ArgumentException(SR.Argument_DateTimeIsNotAmbiguous, nameof(dateTime));
            }

            DateTime adjustedTime;
            if (dateTime.Kind == DateTimeKind.Local)
            {
                CachedData cachedData = s_cachedData;
                adjustedTime = ConvertTime(dateTime, cachedData.Local, this, TimeZoneInfoOptions.None, cachedData);
            }
            else if (dateTime.Kind == DateTimeKind.Utc)
            {
                CachedData cachedData = s_cachedData;
                adjustedTime = ConvertTime(dateTime, s_utcTimeZone, this, TimeZoneInfoOptions.None, cachedData);
            }
            else
            {
                adjustedTime = dateTime;
            }

            bool isAmbiguous = false;
            int? ruleIndex;
            AdjustmentRule? rule = GetAdjustmentRuleForAmbiguousOffsets(adjustedTime, out ruleIndex);
            if (rule != null && rule.HasDaylightSaving)
            {
                DaylightTimeStruct daylightTime = GetDaylightTime(adjustedTime.Year, rule, ruleIndex);
                isAmbiguous = GetIsAmbiguousTime(adjustedTime, rule, daylightTime);
            }

            if (!isAmbiguous)
            {
                throw new ArgumentException(SR.Argument_DateTimeIsNotAmbiguous, nameof(dateTime));
            }

            // the passed in dateTime is ambiguous in this TimeZoneInfo instance
            TimeSpan[] timeSpans = new TimeSpan[2];
            TimeSpan actualUtcOffset = _baseUtcOffset + rule!.BaseUtcOffsetDelta;

            // the TimeSpan array must be sorted from least to greatest
            if (rule.DaylightDelta > TimeSpan.Zero)
            {
                timeSpans[0] = actualUtcOffset; // FUTURE:  + rule.StandardDelta;
                timeSpans[1] = actualUtcOffset + rule.DaylightDelta;
            }
            else
            {
                timeSpans[0] = actualUtcOffset + rule.DaylightDelta;
                timeSpans[1] = actualUtcOffset; // FUTURE: + rule.StandardDelta;
            }
            return timeSpans;
        }

        // note the time is already adjusted
        private AdjustmentRule? GetAdjustmentRuleForAmbiguousOffsets(DateTime adjustedTime, out int? ruleIndex)
        {
            AdjustmentRule? rule = GetAdjustmentRuleForTime(adjustedTime, out ruleIndex);
            if (rule != null && rule.NoDaylightTransitions && !rule.HasDaylightSaving)
            {
                // When using NoDaylightTransitions rules, each rule is only for one offset.
                // When looking for the Daylight savings rules, and we found the non-DST rule,
                // then we get the rule right before this rule.
                return GetPreviousAdjustmentRule(rule, ruleIndex);
            }

            return rule;
        }

        /// <summary>
        /// Gets the AdjustmentRule that is immediately preceding the specified rule.
        /// If the specified rule is the first AdjustmentRule, or it isn't in _adjustmentRules,
        /// then the specified rule is returned.
        /// </summary>
        private AdjustmentRule GetPreviousAdjustmentRule(AdjustmentRule rule, int? ruleIndex)
        {
            Debug.Assert(rule.NoDaylightTransitions, "GetPreviousAdjustmentRule should only be used with NoDaylightTransitions rules.");
            Debug.Assert(_adjustmentRules != null);

            if (ruleIndex.HasValue && 0 < ruleIndex.GetValueOrDefault() && ruleIndex.GetValueOrDefault() < _adjustmentRules.Length)
            {
                return _adjustmentRules[ruleIndex.GetValueOrDefault() - 1];
            }

            AdjustmentRule result = rule;
            for (int i = 1; i < _adjustmentRules.Length; i++)
            {
                // use ReferenceEquals here instead of AdjustmentRule.Equals because 
                // ReferenceEquals is much faster. This is safe because all the callers
                // of GetPreviousAdjustmentRule pass in a rule that was retrieved from
                // _adjustmentRules.  A different approach will be needed if this ever changes.
                if (ReferenceEquals(rule, _adjustmentRules[i]))
                {
                    result = _adjustmentRules[i - 1];
                    break;
                }
            }
            return result;
        }

        /// <summary>
        /// Returns the Universal Coordinated Time (UTC) Offset for the current TimeZoneInfo instance.
        /// </summary>
        public TimeSpan GetUtcOffset(DateTimeOffset dateTimeOffset) =>
            GetUtcOffsetFromUtc(dateTimeOffset.UtcDateTime, this);

        /// <summary>
        /// Returns the Universal Coordinated Time (UTC) Offset for the current TimeZoneInfo instance.
        /// </summary>
        public TimeSpan GetUtcOffset(DateTime dateTime) =>
            GetUtcOffset(dateTime, TimeZoneInfoOptions.NoThrowOnInvalidTime, s_cachedData);

        // Shortcut for TimeZoneInfo.Local.GetUtcOffset
        internal static TimeSpan GetLocalUtcOffset(DateTime dateTime, TimeZoneInfoOptions flags)
        {
            CachedData cachedData = s_cachedData;
            return cachedData.Local.GetUtcOffset(dateTime, flags, cachedData);
        }

        /// <summary>
        /// Returns the Universal Coordinated Time (UTC) Offset for the current TimeZoneInfo instance.
        /// </summary>
        internal TimeSpan GetUtcOffset(DateTime dateTime, TimeZoneInfoOptions flags) =>
            GetUtcOffset(dateTime, flags, s_cachedData);

        private TimeSpan GetUtcOffset(DateTime dateTime, TimeZoneInfoOptions flags, CachedData cachedData)
        {
            if (dateTime.Kind == DateTimeKind.Local)
            {
                if (cachedData.GetCorrespondingKind(this) != DateTimeKind.Local)
                {
                    //
                    // normal case of converting from Local to Utc and then getting the offset from the UTC DateTime
                    //
                    DateTime adjustedTime = ConvertTime(dateTime, cachedData.Local, s_utcTimeZone, flags);
                    return GetUtcOffsetFromUtc(adjustedTime, this);
                }

                //
                // Fall through for TimeZoneInfo.Local.GetUtcOffset(date)
                // to handle an edge case with Invalid-Times for DateTime formatting:
                //
                // Consider the invalid PST time "2007-03-11T02:00:00.0000000-08:00"
                //
                // By directly calling GetUtcOffset instead of converting to UTC and then calling GetUtcOffsetFromUtc
                // the correct invalid offset of "-08:00" is returned.  In the normal case of converting to UTC as an
                // interim-step, the invalid time is adjusted into a *valid* UTC time which causes a change in output:
                //
                // 1) invalid PST time "2007-03-11T02:00:00.0000000-08:00"
                // 2) converted to UTC "2007-03-11T10:00:00.0000000Z"
                // 3) offset returned  "2007-03-11T03:00:00.0000000-07:00"
                //
            }
            else if (dateTime.Kind == DateTimeKind.Utc)
            {
                if (cachedData.GetCorrespondingKind(this) == DateTimeKind.Utc)
                {
                    return _baseUtcOffset;
                }
                else
                {
                    //
                    // passing in a UTC dateTime to a non-UTC TimeZoneInfo instance is a
                    // special Loss-Less case.
                    //
                    return GetUtcOffsetFromUtc(dateTime, this);
                }
            }

            return GetUtcOffset(dateTime, this, flags);
        }

        /// <summary>
        /// Returns true if the time is during the ambiguous time period
        /// for the current TimeZoneInfo instance.
        /// </summary>
        public bool IsAmbiguousTime(DateTimeOffset dateTimeOffset)
        {
            if (!_supportsDaylightSavingTime)
            {
                return false;
            }

            DateTimeOffset adjustedTime = ConvertTime(dateTimeOffset, this);
            return IsAmbiguousTime(adjustedTime.DateTime);
        }

        /// <summary>
        /// Returns true if the time is during the ambiguous time period
        /// for the current TimeZoneInfo instance.
        /// </summary>
        public bool IsAmbiguousTime(DateTime dateTime) =>
            IsAmbiguousTime(dateTime, TimeZoneInfoOptions.NoThrowOnInvalidTime);

        /// <summary>
        /// Returns true if the time is during the ambiguous time period
        /// for the current TimeZoneInfo instance.
        /// </summary>
        internal bool IsAmbiguousTime(DateTime dateTime, TimeZoneInfoOptions flags)
        {
            if (!_supportsDaylightSavingTime)
            {
                return false;
            }

            CachedData cachedData = s_cachedData;
            DateTime adjustedTime =
                dateTime.Kind == DateTimeKind.Local ? ConvertTime(dateTime, cachedData.Local, this, flags, cachedData) :
                dateTime.Kind == DateTimeKind.Utc ? ConvertTime(dateTime, s_utcTimeZone, this, flags, cachedData) :
                dateTime;

            int? ruleIndex;
            AdjustmentRule? rule = GetAdjustmentRuleForTime(adjustedTime, out ruleIndex);
            if (rule != null && rule.HasDaylightSaving)
            {
                DaylightTimeStruct daylightTime = GetDaylightTime(adjustedTime.Year, rule, ruleIndex);
                return GetIsAmbiguousTime(adjustedTime, rule, daylightTime);
            }
            return false;
        }

        /// <summary>
        /// Returns true if the time is during Daylight Saving time for the current TimeZoneInfo instance.
        /// </summary>
        public bool IsDaylightSavingTime(DateTimeOffset dateTimeOffset)
        {
            bool isDaylightSavingTime;
            GetUtcOffsetFromUtc(dateTimeOffset.UtcDateTime, this, out isDaylightSavingTime);
            return isDaylightSavingTime;
        }

        /// <summary>
        /// Returns true if the time is during Daylight Saving time for the current TimeZoneInfo instance.
        /// </summary>
        public bool IsDaylightSavingTime(DateTime dateTime) =>
            IsDaylightSavingTime(dateTime, TimeZoneInfoOptions.NoThrowOnInvalidTime, s_cachedData);

        /// <summary>
        /// Returns true if the time is during Daylight Saving time for the current TimeZoneInfo instance.
        /// </summary>
        internal bool IsDaylightSavingTime(DateTime dateTime, TimeZoneInfoOptions flags) =>
            IsDaylightSavingTime(dateTime, flags, s_cachedData);

        private bool IsDaylightSavingTime(DateTime dateTime, TimeZoneInfoOptions flags, CachedData cachedData)
        {
            //
            //    dateTime.Kind is UTC, then time will be converted from UTC
            //        into current instance's timezone
            //    dateTime.Kind is Local, then time will be converted from Local
            //        into current instance's timezone
            //    dateTime.Kind is UnSpecified, then time is already in
            //        current instance's timezone
            //
            // Our DateTime handles ambiguous times, (one is in the daylight and
            // one is in standard.) If a new DateTime is constructed during ambiguous
            // time, it is defaulted to "Standard" (i.e. this will return false).
            // For Invalid times, we will return false

            if (!_supportsDaylightSavingTime || _adjustmentRules == null)
            {
                return false;
            }

            DateTime adjustedTime;
            //
            // handle any Local/Utc special cases...
            //
            if (dateTime.Kind == DateTimeKind.Local)
            {
                adjustedTime = ConvertTime(dateTime, cachedData.Local, this, flags, cachedData);
            }
            else if (dateTime.Kind == DateTimeKind.Utc)
            {
                if (cachedData.GetCorrespondingKind(this) == DateTimeKind.Utc)
                {
                    // simple always false case: TimeZoneInfo.Utc.IsDaylightSavingTime(dateTime, flags);
                    return false;
                }
                else
                {
                    //
                    // passing in a UTC dateTime to a non-UTC TimeZoneInfo instance is a
                    // special Loss-Less case.
                    //
                    bool isDaylightSavings;
                    GetUtcOffsetFromUtc(dateTime, this, out isDaylightSavings);
                    return isDaylightSavings;
                }
            }
            else
            {
                adjustedTime = dateTime;
            }

            //
            // handle the normal cases...
            //
            int? ruleIndex;
            AdjustmentRule? rule = GetAdjustmentRuleForTime(adjustedTime, out ruleIndex);
            if (rule != null && rule.HasDaylightSaving)
            {
                DaylightTimeStruct daylightTime = GetDaylightTime(adjustedTime.Year, rule, ruleIndex);
                return GetIsDaylightSavings(adjustedTime, rule, daylightTime, flags);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true when dateTime falls into a "hole in time".
        /// </summary>
        public bool IsInvalidTime(DateTime dateTime)
        {
            bool isInvalid = false;

            if ((dateTime.Kind == DateTimeKind.Unspecified) ||
                (dateTime.Kind == DateTimeKind.Local && s_cachedData.GetCorrespondingKind(this) == DateTimeKind.Local))
            {
                // only check Unspecified and (Local when this TimeZoneInfo instance is Local)
                int? ruleIndex;
                AdjustmentRule? rule = GetAdjustmentRuleForTime(dateTime, out ruleIndex);

                if (rule != null && rule.HasDaylightSaving)
                {
                    DaylightTimeStruct daylightTime = GetDaylightTime(dateTime.Year, rule, ruleIndex);
                    isInvalid = GetIsInvalidTime(dateTime, rule, daylightTime);
                }
                else
                {
                    isInvalid = false;
                }
            }

            return isInvalid;
        }

        /// <summary>
        /// Clears data from static members.
        /// </summary>
        public static void ClearCachedData()
        {
            // Clear a fresh instance of cached data
            s_cachedData = new CachedData();
        }

        /// <summary>
        /// Converts the value of a DateTime object from sourceTimeZone to destinationTimeZone.
        /// </summary>
        public static DateTimeOffset ConvertTimeBySystemTimeZoneId(DateTimeOffset dateTimeOffset, string destinationTimeZoneId) =>
            ConvertTime(dateTimeOffset, FindSystemTimeZoneById(destinationTimeZoneId));

        /// <summary>
        /// Converts the value of a DateTime object from sourceTimeZone to destinationTimeZone.
        /// </summary>
        public static DateTime ConvertTimeBySystemTimeZoneId(DateTime dateTime, string destinationTimeZoneId) =>
            ConvertTime(dateTime, FindSystemTimeZoneById(destinationTimeZoneId));

        /// <summary>
        /// Converts the value of a DateTime object from sourceTimeZone to destinationTimeZone.
        /// </summary>
        public static DateTime ConvertTimeBySystemTimeZoneId(DateTime dateTime, string sourceTimeZoneId, string destinationTimeZoneId)
        {
            if (dateTime.Kind == DateTimeKind.Local && string.Equals(sourceTimeZoneId, Local.Id, StringComparison.OrdinalIgnoreCase))
            {
                // TimeZoneInfo.Local can be cleared by another thread calling TimeZoneInfo.ClearCachedData.
                // Take snapshot of cached data to guarantee this method will not be impacted by the ClearCachedData call.
                // Without the snapshot, there is a chance that ConvertTime will throw since 'source' won't
                // be reference equal to the new TimeZoneInfo.Local
                //
                CachedData cachedData = s_cachedData;
                return ConvertTime(dateTime, cachedData.Local, FindSystemTimeZoneById(destinationTimeZoneId), TimeZoneInfoOptions.None, cachedData);
            }
            else if (dateTime.Kind == DateTimeKind.Utc && string.Equals(sourceTimeZoneId, Utc.Id, StringComparison.OrdinalIgnoreCase))
            {
                return ConvertTime(dateTime, s_utcTimeZone, FindSystemTimeZoneById(destinationTimeZoneId), TimeZoneInfoOptions.None, s_cachedData);
            }
            else
            {
                return ConvertTime(dateTime, FindSystemTimeZoneById(sourceTimeZoneId), FindSystemTimeZoneById(destinationTimeZoneId));
            }
        }

        /// <summary>
        /// Converts the value of the dateTime object from sourceTimeZone to destinationTimeZone
        /// </summary>
        public static DateTimeOffset ConvertTime(DateTimeOffset dateTimeOffset, TimeZoneInfo destinationTimeZone)
        {
            if (destinationTimeZone == null)
            {
                throw new ArgumentNullException(nameof(destinationTimeZone));
            }

            // calculate the destination time zone offset
            DateTime utcDateTime = dateTimeOffset.UtcDateTime;
            TimeSpan destinationOffset = GetUtcOffsetFromUtc(utcDateTime, destinationTimeZone);

            // check for overflow
            long ticks = utcDateTime.Ticks + destinationOffset.Ticks;

            return
                ticks > DateTimeOffset.MaxValue.Ticks ? DateTimeOffset.MaxValue :
                ticks < DateTimeOffset.MinValue.Ticks ? DateTimeOffset.MinValue :
                new DateTimeOffset(ticks, destinationOffset);
        }

        /// <summary>
        /// Converts the value of the dateTime object from sourceTimeZone to destinationTimeZone
        /// </summary>
        public static DateTime ConvertTime(DateTime dateTime, TimeZoneInfo destinationTimeZone)
        {
            if (destinationTimeZone == null)
            {
                throw new ArgumentNullException(nameof(destinationTimeZone));
            }

            // Special case to give a way clearing the cache without exposing ClearCachedData()
            if (dateTime.Ticks == 0)
            {
                ClearCachedData();
            }
            CachedData cachedData = s_cachedData;
            TimeZoneInfo sourceTimeZone = dateTime.Kind == DateTimeKind.Utc ? s_utcTimeZone : cachedData.Local;
            return ConvertTime(dateTime, sourceTimeZone, destinationTimeZone, TimeZoneInfoOptions.None, cachedData);
        }

        /// <summary>
        /// Converts the value of the dateTime object from sourceTimeZone to destinationTimeZone
        /// </summary>
        public static DateTime ConvertTime(DateTime dateTime, TimeZoneInfo sourceTimeZone, TimeZoneInfo destinationTimeZone) =>
            ConvertTime(dateTime, sourceTimeZone, destinationTimeZone, TimeZoneInfoOptions.None, s_cachedData);

        /// <summary>
        /// Converts the value of the dateTime object from sourceTimeZone to destinationTimeZone
        /// </summary>
        internal static DateTime ConvertTime(DateTime dateTime, TimeZoneInfo sourceTimeZone, TimeZoneInfo destinationTimeZone, TimeZoneInfoOptions flags) =>
            ConvertTime(dateTime, sourceTimeZone, destinationTimeZone, flags, s_cachedData);

        private static DateTime ConvertTime(DateTime dateTime, TimeZoneInfo sourceTimeZone, TimeZoneInfo destinationTimeZone, TimeZoneInfoOptions flags, CachedData cachedData)
        {
            if (sourceTimeZone == null)
            {
                throw new ArgumentNullException(nameof(sourceTimeZone));
            }

            if (destinationTimeZone == null)
            {
                throw new ArgumentNullException(nameof(destinationTimeZone));
            }

            DateTimeKind sourceKind = cachedData.GetCorrespondingKind(sourceTimeZone);
            if (((flags & TimeZoneInfoOptions.NoThrowOnInvalidTime) == 0) && (dateTime.Kind != DateTimeKind.Unspecified) && (dateTime.Kind != sourceKind))
            {
                throw new ArgumentException(SR.Argument_ConvertMismatch, nameof(sourceTimeZone));
            }

            //
            // check to see if the DateTime is in an invalid time range.  This check
            // requires the current AdjustmentRule and DaylightTime - which are also
            // needed to calculate 'sourceOffset' in the normal conversion case.
            // By calculating the 'sourceOffset' here we improve the
            // performance for the normal case at the expense of the 'ArgumentException'
            // case and Loss-less Local special cases.
            //
            int? sourceRuleIndex;
            AdjustmentRule? sourceRule = sourceTimeZone.GetAdjustmentRuleForTime(dateTime, out sourceRuleIndex);
            TimeSpan sourceOffset = sourceTimeZone.BaseUtcOffset;

            if (sourceRule != null)
            {
                sourceOffset = sourceOffset + sourceRule.BaseUtcOffsetDelta;
                if (sourceRule.HasDaylightSaving)
                {
                    bool sourceIsDaylightSavings = false;
                    DaylightTimeStruct sourceDaylightTime = sourceTimeZone.GetDaylightTime(dateTime.Year, sourceRule, sourceRuleIndex);

                    // 'dateTime' might be in an invalid time range since it is in an AdjustmentRule
                    // period that supports DST
                    if (((flags & TimeZoneInfoOptions.NoThrowOnInvalidTime) == 0) && GetIsInvalidTime(dateTime, sourceRule, sourceDaylightTime))
                    {
                        throw new ArgumentException(SR.Argument_DateTimeIsInvalid, nameof(dateTime));
                    }
                    sourceIsDaylightSavings = GetIsDaylightSavings(dateTime, sourceRule, sourceDaylightTime, flags);

                    // adjust the sourceOffset according to the Adjustment Rule / Daylight Saving Rule
                    sourceOffset += (sourceIsDaylightSavings ? sourceRule.DaylightDelta : TimeSpan.Zero /*FUTURE: sourceRule.StandardDelta*/);
                }
            }

            DateTimeKind targetKind = cachedData.GetCorrespondingKind(destinationTimeZone);

            // handle the special case of Loss-less Local->Local and UTC->UTC)
            if (dateTime.Kind != DateTimeKind.Unspecified && sourceKind != DateTimeKind.Unspecified && sourceKind == targetKind)
            {
                return dateTime;
            }

            long utcTicks = dateTime.Ticks - sourceOffset.Ticks;

            // handle the normal case by converting from 'source' to UTC and then to 'target'
            bool isAmbiguousLocalDst;
            DateTime targetConverted = ConvertUtcToTimeZone(utcTicks, destinationTimeZone, out isAmbiguousLocalDst);

            if (targetKind == DateTimeKind.Local)
            {
                // Because the ticks conversion between UTC and local is lossy, we need to capture whether the
                // time is in a repeated hour so that it can be passed to the DateTime constructor.
                return new DateTime(targetConverted.Ticks, DateTimeKind.Local, isAmbiguousLocalDst);
            }
            else
            {
                return new DateTime(targetConverted.Ticks, targetKind);
            }
        }

        /// <summary>
        /// Converts the value of a DateTime object from Coordinated Universal Time (UTC) to the destinationTimeZone.
        /// </summary>
        public static DateTime ConvertTimeFromUtc(DateTime dateTime, TimeZoneInfo destinationTimeZone) =>
            ConvertTime(dateTime, s_utcTimeZone, destinationTimeZone, TimeZoneInfoOptions.None, s_cachedData);

        /// <summary>
        /// Converts the value of a DateTime object to Coordinated Universal Time (UTC).
        /// </summary>
        public static DateTime ConvertTimeToUtc(DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Utc)
            {
                return dateTime;
            }
            CachedData cachedData = s_cachedData;
            return ConvertTime(dateTime, cachedData.Local, s_utcTimeZone, TimeZoneInfoOptions.None, cachedData);
        }

        /// <summary>
        /// Converts the value of a DateTime object to Coordinated Universal Time (UTC).
        /// </summary>
        internal static DateTime ConvertTimeToUtc(DateTime dateTime, TimeZoneInfoOptions flags)
        {
            if (dateTime.Kind == DateTimeKind.Utc)
            {
                return dateTime;
            }
            CachedData cachedData = s_cachedData;
            return ConvertTime(dateTime, cachedData.Local, s_utcTimeZone, flags, cachedData);
        }

        /// <summary>
        /// Converts the value of a DateTime object to Coordinated Universal Time (UTC).
        /// </summary>
        public static DateTime ConvertTimeToUtc(DateTime dateTime, TimeZoneInfo sourceTimeZone) =>
            ConvertTime(dateTime, sourceTimeZone, s_utcTimeZone, TimeZoneInfoOptions.None, s_cachedData);

        /// <summary>
        /// Returns value equality. Equals does not compare any localizable
        /// String objects (DisplayName, StandardName, DaylightName).
        /// </summary>
        public bool Equals(TimeZoneInfo? other) =>
            other != null &&
            string.Equals(_id, other._id, StringComparison.OrdinalIgnoreCase) &&
            HasSameRules(other);

        public override bool Equals(object? obj) => Equals(obj as TimeZoneInfo);

        public static TimeZoneInfo FromSerializedString(string source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (source.Length == 0)
            {
                throw new ArgumentException(SR.Format(SR.Argument_InvalidSerializedString, source), nameof(source));
            }

            return StringSerializer.GetDeserializedTimeZoneInfo(source);
        }

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(_id);

        /// <summary>
        /// Returns a <see cref="ReadOnlyCollection{TimeZoneInfo}"/> containing all valid TimeZone's
        /// from the local machine. The entries in the collection are sorted by
        /// <see cref="DisplayName"/>.
        /// This method does *not* throw TimeZoneNotFoundException or InvalidTimeZoneException.
        /// </summary>
        public static ReadOnlyCollection<TimeZoneInfo> GetSystemTimeZones()
        {
            CachedData cachedData = s_cachedData;

            lock (cachedData)
            {
                if (cachedData._readOnlySystemTimeZones == null)
                {
                    PopulateAllSystemTimeZones(cachedData);
                    cachedData._allSystemTimeZonesRead = true;

                    List<TimeZoneInfo> list;
                    if (cachedData._systemTimeZones != null)
                    {
                        // return a collection of the cached system time zones
                        list = new List<TimeZoneInfo>(cachedData._systemTimeZones.Values);
                    }
                    else
                    {
                        // return an empty collection
                        list = new List<TimeZoneInfo>();
                    }

                    // sort and copy the TimeZoneInfo's into a ReadOnlyCollection for the user
                    list.Sort((x, y) =>
                    {
                        // sort by BaseUtcOffset first and by DisplayName second - this is similar to the Windows Date/Time control panel
                        int comparison = x.BaseUtcOffset.CompareTo(y.BaseUtcOffset);
                        return comparison == 0 ? string.CompareOrdinal(x.DisplayName, y.DisplayName) : comparison;
                    });

                    cachedData._readOnlySystemTimeZones = new ReadOnlyCollection<TimeZoneInfo>(list);
                }
            }
            return cachedData._readOnlySystemTimeZones;
        }

        /// <summary>
        /// Value equality on the "adjustmentRules" array
        /// </summary>
        public bool HasSameRules(TimeZoneInfo other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            // check the utcOffset and supportsDaylightSavingTime members
            if (_baseUtcOffset != other._baseUtcOffset ||
                _supportsDaylightSavingTime != other._supportsDaylightSavingTime)
            {
                return false;
            }

            bool sameRules;
            AdjustmentRule[]? currentRules = _adjustmentRules;
            AdjustmentRule[]? otherRules = other._adjustmentRules;

            sameRules =
                (currentRules == null && otherRules == null) ||
                (currentRules != null && otherRules != null);

            if (!sameRules)
            {
                // AdjustmentRule array mismatch
                return false;
            }

            if (currentRules != null)
            {
                if (currentRules.Length != otherRules!.Length)
                {
                    // AdjustmentRule array length mismatch
                    return false;
                }

                for (int i = 0; i < currentRules.Length; i++)
                {
                    if (!(currentRules[i]).Equals(otherRules[i]))
                    {
                        // AdjustmentRule value-equality mismatch
                        return false;
                    }
                }
            }
            return sameRules;
        }

        /// <summary>
        /// Returns a TimeZoneInfo instance that represents the local time on the machine.
        /// Accessing this property may throw InvalidTimeZoneException or COMException
        /// if the machine is in an unstable or corrupt state.
        /// </summary>
        public static TimeZoneInfo Local => s_cachedData.Local;

        //
        // ToSerializedString -
        //
        // "TimeZoneInfo"           := TimeZoneInfo Data;[AdjustmentRule Data 1];...;[AdjustmentRule Data N]
        //
        // "TimeZoneInfo Data"      := <_id>;<_baseUtcOffset>;<_displayName>;
        //                          <_standardDisplayName>;<_daylightDispayName>;
        //
        // "AdjustmentRule Data" := <DateStart>;<DateEnd>;<DaylightDelta>;
        //                          [TransitionTime Data DST Start]
        //                          [TransitionTime Data DST End]
        //
        // "TransitionTime Data" += <DaylightStartTimeOfDat>;<Month>;<Week>;<DayOfWeek>;<Day>
        //
        public string ToSerializedString() => StringSerializer.GetSerializedString(this);

        /// <summary>
        /// Returns the <see cref="DisplayName"/>: "(GMT-08:00) Pacific Time (US &amp; Canada); Tijuana"
        /// </summary>
        public override string ToString() => DisplayName;

        /// <summary>
        /// Returns a TimeZoneInfo instance that represents Universal Coordinated Time (UTC)
        /// </summary>
        public static TimeZoneInfo Utc => s_utcTimeZone;

        private TimeZoneInfo(
                string id,
                TimeSpan baseUtcOffset,
                string? displayName,
                string? standardDisplayName,
                string? daylightDisplayName,
                AdjustmentRule[]? adjustmentRules,
                bool disableDaylightSavingTime)
        {
            bool adjustmentRulesSupportDst;
            ValidateTimeZoneInfo(id, baseUtcOffset, adjustmentRules, out adjustmentRulesSupportDst);

            _id = id;
            _baseUtcOffset = baseUtcOffset;
            _displayName = displayName;
            _standardDisplayName = standardDisplayName;
            _daylightDisplayName = disableDaylightSavingTime ? null : daylightDisplayName;
            _supportsDaylightSavingTime = adjustmentRulesSupportDst && !disableDaylightSavingTime;
            _adjustmentRules = adjustmentRules;
        }

        /// <summary>
        /// Returns a simple TimeZoneInfo instance that does not support Daylight Saving Time.
        /// </summary>
        public static TimeZoneInfo CreateCustomTimeZone(
            string id,
            TimeSpan baseUtcOffset,
            string? displayName,
            string? standardDisplayName)
        {
            return new TimeZoneInfo(
                id,
                baseUtcOffset,
                displayName,
                standardDisplayName,
                standardDisplayName,
                adjustmentRules: null,
                disableDaylightSavingTime: false);
        }

        /// <summary>
        /// Returns a TimeZoneInfo instance that may support Daylight Saving Time.
        /// </summary>
        public static TimeZoneInfo CreateCustomTimeZone(
            string id,
            TimeSpan baseUtcOffset,
            string? displayName,
            string? standardDisplayName,
            string? daylightDisplayName,
            AdjustmentRule[]? adjustmentRules)
        {
            return CreateCustomTimeZone(
                id,
                baseUtcOffset,
                displayName,
                standardDisplayName,
                daylightDisplayName,
                adjustmentRules,
                disableDaylightSavingTime: false);
        }

        /// <summary>
        /// Returns a TimeZoneInfo instance that may support Daylight Saving Time.
        /// </summary>
        public static TimeZoneInfo CreateCustomTimeZone(
            string id,
            TimeSpan baseUtcOffset,
            string? displayName,
            string? standardDisplayName,
            string? daylightDisplayName,
            AdjustmentRule[]? adjustmentRules,
            bool disableDaylightSavingTime)
        {
            if (!disableDaylightSavingTime && adjustmentRules?.Length > 0)
            {
                adjustmentRules = (AdjustmentRule[])adjustmentRules.Clone();
            }

            return new TimeZoneInfo(
                id,
                baseUtcOffset,
                displayName,
                standardDisplayName,
                daylightDisplayName,
                adjustmentRules,
                disableDaylightSavingTime);
        }

        void IDeserializationCallback.OnDeserialization(object sender)
        {
            try
            {
                bool adjustmentRulesSupportDst;
                ValidateTimeZoneInfo(_id, _baseUtcOffset, _adjustmentRules, out adjustmentRulesSupportDst);

                if (adjustmentRulesSupportDst != _supportsDaylightSavingTime)
                {
                    throw new SerializationException(SR.Format(SR.Serialization_CorruptField, "SupportsDaylightSavingTime"));
                }
            }
            catch (ArgumentException e)
            {
                throw new SerializationException(SR.Serialization_InvalidData, e);
            }
            catch (InvalidTimeZoneException e)
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

            info.AddValue("Id", _id); // Do not rename (binary serialization)
            info.AddValue("DisplayName", _displayName); // Do not rename (binary serialization)
            info.AddValue("StandardName", _standardDisplayName); // Do not rename (binary serialization)
            info.AddValue("DaylightName", _daylightDisplayName); // Do not rename (binary serialization)
            info.AddValue("BaseUtcOffset", _baseUtcOffset); // Do not rename (binary serialization)
            info.AddValue("AdjustmentRules", _adjustmentRules); // Do not rename (binary serialization)
            info.AddValue("SupportsDaylightSavingTime", _supportsDaylightSavingTime); // Do not rename (binary serialization)
        }

        private TimeZoneInfo(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            _id = (string)info.GetValue("Id", typeof(string))!; // Do not rename (binary serialization)
            _displayName = (string?)info.GetValue("DisplayName", typeof(string)); // Do not rename (binary serialization)
            _standardDisplayName = (string?)info.GetValue("StandardName", typeof(string)); // Do not rename (binary serialization)
            _daylightDisplayName = (string?)info.GetValue("DaylightName", typeof(string)); // Do not rename (binary serialization)
            _baseUtcOffset = (TimeSpan)info.GetValue("BaseUtcOffset", typeof(TimeSpan))!; // Do not rename (binary serialization)
            _adjustmentRules = (AdjustmentRule[]?)info.GetValue("AdjustmentRules", typeof(AdjustmentRule[])); // Do not rename (binary serialization)
            _supportsDaylightSavingTime = (bool)info.GetValue("SupportsDaylightSavingTime", typeof(bool))!; // Do not rename (binary serialization)
        }

        private AdjustmentRule? GetAdjustmentRuleForTime(DateTime dateTime, out int? ruleIndex)
        {
            AdjustmentRule? result = GetAdjustmentRuleForTime(dateTime, dateTimeisUtc: false, ruleIndex: out ruleIndex);
            Debug.Assert(result == null || ruleIndex.HasValue, "If an AdjustmentRule was found, ruleIndex should also be set.");

            return result;
        }
        
        private AdjustmentRule? GetAdjustmentRuleForTime(DateTime dateTime, bool dateTimeisUtc, out int? ruleIndex)
        {
            if (_adjustmentRules == null || _adjustmentRules.Length == 0)
            {
                ruleIndex = null;
                return null;
            }

            // Only check the whole-date portion of the dateTime for DateTimeKind.Unspecified rules -
            // This is because the AdjustmentRule DateStart & DateEnd are stored as
            // Date-only values {4/2/2006 - 10/28/2006} but actually represent the
            // time span {4/2/2006@00:00:00.00000 - 10/28/2006@23:59:59.99999}
            DateTime date = dateTimeisUtc ?
                (dateTime + BaseUtcOffset).Date :
                dateTime.Date;

            int low = 0;
            int high = _adjustmentRules.Length - 1;

            while (low <= high)
            {
                int median = low + ((high - low) >> 1);

                AdjustmentRule rule = _adjustmentRules[median];
                AdjustmentRule previousRule = median > 0 ? _adjustmentRules[median - 1] : rule;

                int compareResult = CompareAdjustmentRuleToDateTime(rule, previousRule, dateTime, date, dateTimeisUtc);
                if (compareResult == 0)
                {
                    ruleIndex = median;
                    return rule;
                }
                else if (compareResult < 0)
                {
                    low = median + 1;
                }
                else
                {
                    high = median - 1;
                }
            }

            ruleIndex = null;
            return null;
        }

        /// <summary>
        /// Determines if 'rule' is the correct AdjustmentRule for the given dateTime.
        /// </summary>
        /// <returns>
        /// A value less than zero if rule is for times before dateTime.
        /// Zero if rule is correct for dateTime.
        /// A value greater than zero if rule is for times after dateTime.
        /// </returns>
        private int CompareAdjustmentRuleToDateTime(AdjustmentRule rule, AdjustmentRule previousRule,
            DateTime dateTime, DateTime dateOnly, bool dateTimeisUtc)
        {
            bool isAfterStart;
            if (rule.DateStart.Kind == DateTimeKind.Utc)
            {
                DateTime dateTimeToCompare = dateTimeisUtc ?
                    dateTime :
                    // use the previous rule to compute the dateTimeToCompare, since the time daylight savings "switches"
                    // is based on the previous rule's offset
                    ConvertToUtc(dateTime, previousRule.DaylightDelta, previousRule.BaseUtcOffsetDelta);

                isAfterStart = dateTimeToCompare >= rule.DateStart;
            }
            else
            {
                // if the rule's DateStart is Unspecified, then use the whole-date portion
                isAfterStart = dateOnly >= rule.DateStart;
            }

            if (!isAfterStart)
            {
                return 1;
            }

            bool isBeforeEnd;
            if (rule.DateEnd.Kind == DateTimeKind.Utc)
            {
                DateTime dateTimeToCompare = dateTimeisUtc ?
                    dateTime :
                    ConvertToUtc(dateTime, rule.DaylightDelta, rule.BaseUtcOffsetDelta);

                isBeforeEnd = dateTimeToCompare <= rule.DateEnd;
            }
            else
            {
                // if the rule's DateEnd is Unspecified, then use the whole-date portion
                isBeforeEnd = dateOnly <= rule.DateEnd;
            }

            return isBeforeEnd ? 0 : -1;
        }

        /// <summary>
        /// Converts the dateTime to UTC using the specified deltas.
        /// </summary>
        private DateTime ConvertToUtc(DateTime dateTime, TimeSpan daylightDelta, TimeSpan baseUtcOffsetDelta) =>
            ConvertToFromUtc(dateTime, daylightDelta, baseUtcOffsetDelta, convertToUtc: true);

        /// <summary>
        /// Converts the dateTime from UTC using the specified deltas.
        /// </summary>
        private DateTime ConvertFromUtc(DateTime dateTime, TimeSpan daylightDelta, TimeSpan baseUtcOffsetDelta) =>
            ConvertToFromUtc(dateTime, daylightDelta, baseUtcOffsetDelta, convertToUtc: false);

        /// <summary>
        /// Converts the dateTime to or from UTC using the specified deltas.
        /// </summary>
        private DateTime ConvertToFromUtc(DateTime dateTime, TimeSpan daylightDelta, TimeSpan baseUtcOffsetDelta, bool convertToUtc)
        {
            TimeSpan offset = BaseUtcOffset + daylightDelta + baseUtcOffsetDelta;
            if (convertToUtc)
            {
                offset = offset.Negate();
            }

            long ticks = dateTime.Ticks + offset.Ticks;

            return
                ticks > DateTime.MaxValue.Ticks ? DateTime.MaxValue :
                ticks < DateTime.MinValue.Ticks ? DateTime.MinValue :
                new DateTime(ticks);
        }

        /// <summary>
        /// Helper function that converts a dateTime from UTC into the destinationTimeZone
        /// - Returns DateTime.MaxValue when the converted value is too large.
        /// - Returns DateTime.MinValue when the converted value is too small.
        /// </summary>
        private static DateTime ConvertUtcToTimeZone(long ticks, TimeZoneInfo destinationTimeZone, out bool isAmbiguousLocalDst)
        {
            // used to calculate the UTC offset in the destinationTimeZone
            DateTime utcConverted =
                ticks > DateTime.MaxValue.Ticks ? DateTime.MaxValue :
                ticks < DateTime.MinValue.Ticks ? DateTime.MinValue :
                new DateTime(ticks);

            // verify the time is between MinValue and MaxValue in the new time zone
            TimeSpan offset = GetUtcOffsetFromUtc(utcConverted, destinationTimeZone, out isAmbiguousLocalDst);
            ticks += offset.Ticks;

            return
                ticks > DateTime.MaxValue.Ticks ? DateTime.MaxValue :
                ticks < DateTime.MinValue.Ticks ? DateTime.MinValue :
                new DateTime(ticks);
        }

        /// <summary>
        /// Helper function that returns a DaylightTime from a year and AdjustmentRule.
        /// </summary>
        private DaylightTimeStruct GetDaylightTime(int year, AdjustmentRule rule, int? ruleIndex)
        {
            TimeSpan delta = rule.DaylightDelta;
            DateTime startTime;
            DateTime endTime;
            if (rule.NoDaylightTransitions)
            {
                // NoDaylightTransitions rules don't use DaylightTransition Start and End, instead
                // the DateStart and DateEnd are UTC times that represent when daylight savings time changes.
                // Convert the UTC times into adjusted time zone times.

                // use the previous rule to calculate the startTime, since the DST change happens w.r.t. the previous rule
                AdjustmentRule previousRule = GetPreviousAdjustmentRule(rule, ruleIndex);
                startTime = ConvertFromUtc(rule.DateStart, previousRule.DaylightDelta, previousRule.BaseUtcOffsetDelta);

                endTime = ConvertFromUtc(rule.DateEnd, rule.DaylightDelta, rule.BaseUtcOffsetDelta);
            }
            else
            {
                startTime = TransitionTimeToDateTime(year, rule.DaylightTransitionStart);
                endTime = TransitionTimeToDateTime(year, rule.DaylightTransitionEnd);
            }
            return new DaylightTimeStruct(startTime, endTime, delta);
        }

        /// <summary>
        /// Helper function that checks if a given dateTime is in Daylight Saving Time (DST).
        /// This function assumes the dateTime and AdjustmentRule are both in the same time zone.
        /// </summary>
        private static bool GetIsDaylightSavings(DateTime time, AdjustmentRule rule, DaylightTimeStruct daylightTime, TimeZoneInfoOptions flags)
        {
            if (rule == null)
            {
                return false;
            }

            DateTime startTime;
            DateTime endTime;

            if (time.Kind == DateTimeKind.Local)
            {
                // startTime and endTime represent the period from either the start of
                // DST to the end and ***includes*** the potentially overlapped times
                startTime = rule.IsStartDateMarkerForBeginningOfYear() ?
                    new DateTime(daylightTime.Start.Year, 1, 1, 0, 0, 0) :
                    daylightTime.Start + daylightTime.Delta;

                endTime = rule.IsEndDateMarkerForEndOfYear() ?
                    new DateTime(daylightTime.End.Year + 1, 1, 1, 0, 0, 0).AddTicks(-1) :
                    daylightTime.End;
            }
            else
            {
                // startTime and endTime represent the period from either the start of DST to the end and
                // ***does not include*** the potentially overlapped times
                //
                //         -=-=-=-=-=- Pacific Standard Time -=-=-=-=-=-=-
                //    April 2, 2006                            October 29, 2006
                // 2AM            3AM                        1AM              2AM
                // |      +1 hr     |                        |       -1 hr      |
                // | <invalid time> |                        | <ambiguous time> |
                //                  [========== DST ========>)
                //
                //        -=-=-=-=-=- Some Weird Time Zone -=-=-=-=-=-=-
                //    April 2, 2006                          October 29, 2006
                // 1AM              2AM                    2AM              3AM
                // |      -1 hr       |                      |       +1 hr      |
                // | <ambiguous time> |                      |  <invalid time>  |
                //                    [======== DST ========>)
                //
                bool invalidAtStart = rule.DaylightDelta > TimeSpan.Zero;

                startTime = rule.IsStartDateMarkerForBeginningOfYear() ?
                    new DateTime(daylightTime.Start.Year, 1, 1, 0, 0, 0) :
                    daylightTime.Start + (invalidAtStart ? rule.DaylightDelta : TimeSpan.Zero); /* FUTURE: - rule.StandardDelta; */

                endTime = rule.IsEndDateMarkerForEndOfYear() ?
                    new DateTime(daylightTime.End.Year + 1, 1, 1, 0, 0, 0).AddTicks(-1) :
                    daylightTime.End + (invalidAtStart ? -rule.DaylightDelta : TimeSpan.Zero);
            }

            bool isDst = CheckIsDst(startTime, time, endTime, false, rule);

            // If this date was previously converted from a UTC date and we were able to detect that the local
            // DateTime would be ambiguous, this data is stored in the DateTime to resolve this ambiguity.
            if (isDst && time.Kind == DateTimeKind.Local)
            {
                // For normal time zones, the ambiguous hour is the last hour of daylight saving when you wind the
                // clock back. It is theoretically possible to have a positive delta, (which would really be daylight
                // reduction time), where you would have to wind the clock back in the begnning.
                if (GetIsAmbiguousTime(time, rule, daylightTime))
                {
                    isDst = time.IsAmbiguousDaylightSavingTime();
                }
            }

            return isDst;
        }

        /// <summary>
        /// Gets the offset that should be used to calculate DST start times from a UTC time.
        /// </summary>
        private TimeSpan GetDaylightSavingsStartOffsetFromUtc(TimeSpan baseUtcOffset, AdjustmentRule rule, int? ruleIndex)
        {
            if (rule.NoDaylightTransitions)
            {
                // use the previous rule to calculate the startTime, since the DST change happens w.r.t. the previous rule
                AdjustmentRule previousRule = GetPreviousAdjustmentRule(rule, ruleIndex);
                return baseUtcOffset + previousRule.BaseUtcOffsetDelta + previousRule.DaylightDelta;
            }
            else
            {
                return baseUtcOffset + rule.BaseUtcOffsetDelta; /* FUTURE: + rule.StandardDelta; */
            }
        }

        /// <summary>
        /// Gets the offset that should be used to calculate DST end times from a UTC time.
        /// </summary>
        private TimeSpan GetDaylightSavingsEndOffsetFromUtc(TimeSpan baseUtcOffset, AdjustmentRule rule)
        {
            // NOTE: even NoDaylightTransitions rules use this logic since DST ends w.r.t. the current rule
            return baseUtcOffset + rule.BaseUtcOffsetDelta + rule.DaylightDelta; /* FUTURE: + rule.StandardDelta; */
        }

        /// <summary>
        /// Helper function that checks if a given dateTime is in Daylight Saving Time (DST).
        /// This function assumes the dateTime is in UTC and AdjustmentRule is in a different time zone.
        /// </summary>
        private static bool GetIsDaylightSavingsFromUtc(DateTime time, int year, TimeSpan utc, AdjustmentRule rule, int? ruleIndex, out bool isAmbiguousLocalDst, TimeZoneInfo zone)
        {
            isAmbiguousLocalDst = false;

            if (rule == null)
            {
                return false;
            }

            // Get the daylight changes for the year of the specified time.
            DaylightTimeStruct daylightTime = zone.GetDaylightTime(year, rule, ruleIndex);

            // The start and end times represent the range of universal times that are in DST for that year.
            // Within that there is an ambiguous hour, usually right at the end, but at the beginning in
            // the unusual case of a negative daylight savings delta.
            // We need to handle the case if the current rule has daylight saving end by the end of year. If so, we need to check if next year starts with daylight saving on
            // and get the actual daylight saving end time. Here is example for such case:
            //      Converting the UTC datetime "12/31/2011 8:00:00 PM" to "(UTC+03:00) Moscow, St. Petersburg, Volgograd (RTZ 2)" zone.
            //      In 2011 the daylight saving will go through the end of the year. If we use the end of 2011 as the daylight saving end,
            //      that will fail the conversion because the UTC time +4 hours (3 hours for the zone UTC offset and 1 hour for daylight saving) will move us to the next year "1/1/2012 12:00 AM",
            //      checking against the end of 2011 will tell we are not in daylight saving which is wrong and the conversion will be off by one hour.
            // Note we handle the similar case when rule year start with daylight saving and previous year end with daylight saving.

            bool ignoreYearAdjustment = false;
            TimeSpan dstStartOffset = zone.GetDaylightSavingsStartOffsetFromUtc(utc, rule, ruleIndex);
            DateTime startTime;
            if (rule.IsStartDateMarkerForBeginningOfYear() && daylightTime.Start.Year > DateTime.MinValue.Year)
            {
                int? previousYearRuleIndex;
                AdjustmentRule? previousYearRule = zone.GetAdjustmentRuleForTime(
                    new DateTime(daylightTime.Start.Year - 1, 12, 31),
                    out previousYearRuleIndex);
                if (previousYearRule != null && previousYearRule.IsEndDateMarkerForEndOfYear())
                {
                    DaylightTimeStruct previousDaylightTime = zone.GetDaylightTime(
                        daylightTime.Start.Year - 1, 
                        previousYearRule, 
                        previousYearRuleIndex);
                    startTime = previousDaylightTime.Start - utc - previousYearRule.BaseUtcOffsetDelta;
                    ignoreYearAdjustment = true;
                }
                else
                {
                    startTime = new DateTime(daylightTime.Start.Year, 1, 1, 0, 0, 0) - dstStartOffset;
                }
            }
            else
            {
                startTime = daylightTime.Start - dstStartOffset;
            }

            TimeSpan dstEndOffset = zone.GetDaylightSavingsEndOffsetFromUtc(utc, rule);
            DateTime endTime;
            if (rule.IsEndDateMarkerForEndOfYear() && daylightTime.End.Year < DateTime.MaxValue.Year)
            {
                int? nextYearRuleIndex;
                AdjustmentRule? nextYearRule = zone.GetAdjustmentRuleForTime(
                    new DateTime(daylightTime.End.Year + 1, 1, 1),
                    out nextYearRuleIndex);
                if (nextYearRule != null && nextYearRule.IsStartDateMarkerForBeginningOfYear())
                {
                    if (nextYearRule.IsEndDateMarkerForEndOfYear())
                    {
                        // next year end with daylight saving on too
                        endTime = new DateTime(daylightTime.End.Year + 1, 12, 31) - utc - nextYearRule.BaseUtcOffsetDelta - nextYearRule.DaylightDelta;
                    }
                    else
                    {
                        DaylightTimeStruct nextdaylightTime = zone.GetDaylightTime(
                            daylightTime.End.Year + 1, 
                            nextYearRule,
                            nextYearRuleIndex);
                        endTime = nextdaylightTime.End - utc - nextYearRule.BaseUtcOffsetDelta - nextYearRule.DaylightDelta;
                    }
                    ignoreYearAdjustment = true;
                }
                else
                {
                    endTime = new DateTime(daylightTime.End.Year + 1, 1, 1, 0, 0, 0).AddTicks(-1) - dstEndOffset;
                }
            }
            else
            {
                endTime = daylightTime.End - dstEndOffset;
            }

            DateTime ambiguousStart;
            DateTime ambiguousEnd;
            if (daylightTime.Delta.Ticks > 0)
            {
                ambiguousStart = endTime - daylightTime.Delta;
                ambiguousEnd = endTime;
            }
            else
            {
                ambiguousStart = startTime;
                ambiguousEnd = startTime - daylightTime.Delta;
            }

            bool isDst = CheckIsDst(startTime, time, endTime, ignoreYearAdjustment, rule);

            // See if the resulting local time becomes ambiguous. This must be captured here or the
            // DateTime will not be able to round-trip back to UTC accurately.
            if (isDst)
            {
                isAmbiguousLocalDst = (time >= ambiguousStart && time < ambiguousEnd);

                if (!isAmbiguousLocalDst && ambiguousStart.Year != ambiguousEnd.Year)
                {
                    // there exists an extreme corner case where the start or end period is on a year boundary and
                    // because of this the comparison above might have been performed for a year-early or a year-later
                    // than it should have been.
                    DateTime ambiguousStartModified;
                    DateTime ambiguousEndModified;
                    try
                    {
                        ambiguousStartModified = ambiguousStart.AddYears(1);
                        ambiguousEndModified = ambiguousEnd.AddYears(1);
                        isAmbiguousLocalDst = (time >= ambiguousStart && time < ambiguousEnd);
                    }
                    catch (ArgumentOutOfRangeException) { }

                    if (!isAmbiguousLocalDst)
                    {
                        try
                        {
                            ambiguousStartModified = ambiguousStart.AddYears(-1);
                            ambiguousEndModified = ambiguousEnd.AddYears(-1);
                            isAmbiguousLocalDst = (time >= ambiguousStart && time < ambiguousEnd);
                        }
                        catch (ArgumentOutOfRangeException) { }
                    }
                }
            }

            return isDst;
        }

        private static bool CheckIsDst(DateTime startTime, DateTime time, DateTime endTime, bool ignoreYearAdjustment, AdjustmentRule rule)
        {
            // NoDaylightTransitions AdjustmentRules should never get their year adjusted since they adjust the offset for the
            // entire time period - which may be for multiple years
            if (!ignoreYearAdjustment && !rule.NoDaylightTransitions)
            {
                int startTimeYear = startTime.Year;
                int endTimeYear = endTime.Year;

                if (startTimeYear != endTimeYear)
                {
                    endTime = endTime.AddYears(startTimeYear - endTimeYear);
                }

                int timeYear = time.Year;

                if (startTimeYear != timeYear)
                {
                    time = time.AddYears(startTimeYear - timeYear);
                }
            }

            if (startTime > endTime)
            {
                // In southern hemisphere, the daylight saving time starts later in the year, and ends in the beginning of next year.
                // Note, the summer in the southern hemisphere begins late in the year.
                return (time < endTime || time >= startTime);
            }
            else if (rule.NoDaylightTransitions)
            {
                // In NoDaylightTransitions AdjustmentRules, the startTime is always before the endTime,
                // and both the start and end times are inclusive
                return time >= startTime && time <= endTime;
            }
            else
            {
                // In northern hemisphere, the daylight saving time starts in the middle of the year.
                return time >= startTime && time < endTime;
            }
        }

        /// <summary>
        /// Returns true when the dateTime falls into an ambiguous time range.
        ///
        /// For example, in Pacific Standard Time on Sunday, October 29, 2006 time jumps from
        /// 2AM to 1AM.  This means the timeline on Sunday proceeds as follows:
        /// 12AM ... [1AM ... 1:59:59AM -> 1AM ... 1:59:59AM] 2AM ... 3AM ...
        ///
        /// In this example, any DateTime values that fall into the [1AM - 1:59:59AM] range
        /// are ambiguous; as it is unclear if these times are in Daylight Saving Time.
        /// </summary>
        private static bool GetIsAmbiguousTime(DateTime time, AdjustmentRule rule, DaylightTimeStruct daylightTime)
        {
            bool isAmbiguous = false;
            if (rule == null || rule.DaylightDelta == TimeSpan.Zero)
            {
                return isAmbiguous;
            }

            DateTime startAmbiguousTime;
            DateTime endAmbiguousTime;

            // if at DST start we transition forward in time then there is an ambiguous time range at the DST end
            if (rule.DaylightDelta > TimeSpan.Zero)
            {
                if (rule.IsEndDateMarkerForEndOfYear())
                { // year end with daylight on so there is no ambiguous time
                    return false;
                }
                startAmbiguousTime = daylightTime.End;
                endAmbiguousTime = daylightTime.End - rule.DaylightDelta; /* FUTURE: + rule.StandardDelta; */
            }
            else
            {
                if (rule.IsStartDateMarkerForBeginningOfYear())
                { // year start with daylight on so there is no ambiguous time
                    return false;
                }
                startAmbiguousTime = daylightTime.Start;
                endAmbiguousTime = daylightTime.Start + rule.DaylightDelta; /* FUTURE: - rule.StandardDelta; */
            }

            isAmbiguous = (time >= endAmbiguousTime && time < startAmbiguousTime);

            if (!isAmbiguous && startAmbiguousTime.Year != endAmbiguousTime.Year)
            {
                // there exists an extreme corner case where the start or end period is on a year boundary and
                // because of this the comparison above might have been performed for a year-early or a year-later
                // than it should have been.
                DateTime startModifiedAmbiguousTime;
                DateTime endModifiedAmbiguousTime;
                try
                {
                    startModifiedAmbiguousTime = startAmbiguousTime.AddYears(1);
                    endModifiedAmbiguousTime = endAmbiguousTime.AddYears(1);
                    isAmbiguous = (time >= endModifiedAmbiguousTime && time < startModifiedAmbiguousTime);
                }
                catch (ArgumentOutOfRangeException) { }

                if (!isAmbiguous)
                {
                    try
                    {
                        startModifiedAmbiguousTime = startAmbiguousTime.AddYears(-1);
                        endModifiedAmbiguousTime = endAmbiguousTime.AddYears(-1);
                        isAmbiguous = (time >= endModifiedAmbiguousTime && time < startModifiedAmbiguousTime);
                    }
                    catch (ArgumentOutOfRangeException) { }
                }
            }
            return isAmbiguous;
        }

        /// <summary>
        /// Helper function that checks if a given DateTime is in an invalid time ("time hole")
        /// A "time hole" occurs at a DST transition point when time jumps forward;
        /// For example, in Pacific Standard Time on Sunday, April 2, 2006 time jumps from
        /// 1:59:59.9999999 to 3AM.  The time range 2AM to 2:59:59.9999999AM is the "time hole".
        /// A "time hole" is not limited to only occurring at the start of DST, and may occur at
        /// the end of DST as well.
        /// </summary>
        private static bool GetIsInvalidTime(DateTime time, AdjustmentRule rule, DaylightTimeStruct daylightTime)
        {
            bool isInvalid = false;
            if (rule == null || rule.DaylightDelta == TimeSpan.Zero)
            {
                return isInvalid;
            }

            DateTime startInvalidTime;
            DateTime endInvalidTime;

            // if at DST start we transition forward in time then there is an ambiguous time range at the DST end
            if (rule.DaylightDelta < TimeSpan.Zero)
            {
                // if the year ends with daylight saving on then there cannot be any time-hole's in that year.
                if (rule.IsEndDateMarkerForEndOfYear())
                    return false;

                startInvalidTime = daylightTime.End;
                endInvalidTime = daylightTime.End - rule.DaylightDelta; /* FUTURE: + rule.StandardDelta; */
            }
            else
            {
                // if the year starts with daylight saving on then there cannot be any time-hole's in that year.
                if (rule.IsStartDateMarkerForBeginningOfYear())
                    return false;

                startInvalidTime = daylightTime.Start;
                endInvalidTime = daylightTime.Start + rule.DaylightDelta; /* FUTURE: - rule.StandardDelta; */
            }

            isInvalid = (time >= startInvalidTime && time < endInvalidTime);

            if (!isInvalid && startInvalidTime.Year != endInvalidTime.Year)
            {
                // there exists an extreme corner case where the start or end period is on a year boundary and
                // because of this the comparison above might have been performed for a year-early or a year-later
                // than it should have been.
                DateTime startModifiedInvalidTime;
                DateTime endModifiedInvalidTime;
                try
                {
                    startModifiedInvalidTime = startInvalidTime.AddYears(1);
                    endModifiedInvalidTime = endInvalidTime.AddYears(1);
                    isInvalid = (time >= startModifiedInvalidTime && time < endModifiedInvalidTime);
                }
                catch (ArgumentOutOfRangeException) { }

                if (!isInvalid)
                {
                    try
                    {
                        startModifiedInvalidTime = startInvalidTime.AddYears(-1);
                        endModifiedInvalidTime = endInvalidTime.AddYears(-1);
                        isInvalid = (time >= startModifiedInvalidTime && time < endModifiedInvalidTime);
                    }
                    catch (ArgumentOutOfRangeException) { }
                }
            }
            return isInvalid;
        }

        /// <summary>
        /// Helper function that calculates the UTC offset for a dateTime in a timeZone.
        /// This function assumes that the dateTime is already converted into the timeZone.
        /// </summary>
        private static TimeSpan GetUtcOffset(DateTime time, TimeZoneInfo zone, TimeZoneInfoOptions flags)
        {
            TimeSpan baseOffset = zone.BaseUtcOffset;
            int? ruleIndex;
            AdjustmentRule? rule = zone.GetAdjustmentRuleForTime(time, out ruleIndex);

            if (rule != null)
            {
                baseOffset = baseOffset + rule.BaseUtcOffsetDelta;
                if (rule.HasDaylightSaving)
                {
                    DaylightTimeStruct daylightTime = zone.GetDaylightTime(time.Year, rule, ruleIndex);
                    bool isDaylightSavings = GetIsDaylightSavings(time, rule, daylightTime, flags);
                    baseOffset += (isDaylightSavings ? rule.DaylightDelta : TimeSpan.Zero /* FUTURE: rule.StandardDelta */);
                }
            }

            return baseOffset;
        }

        /// <summary>
        /// Helper function that calculates the UTC offset for a UTC-dateTime in a timeZone.
        /// This function assumes that the dateTime is represented in UTC and has *not* already been converted into the timeZone.
        /// </summary>
        private static TimeSpan GetUtcOffsetFromUtc(DateTime time, TimeZoneInfo zone)
        {
            bool isDaylightSavings;
            return GetUtcOffsetFromUtc(time, zone, out isDaylightSavings);
        }

        /// <summary>
        /// Helper function that calculates the UTC offset for a UTC-dateTime in a timeZone.
        /// This function assumes that the dateTime is represented in UTC and has *not* already been converted into the timeZone.
        /// </summary>
        private static TimeSpan GetUtcOffsetFromUtc(DateTime time, TimeZoneInfo zone, out bool isDaylightSavings)
        {
            bool isAmbiguousLocalDst;
            return GetUtcOffsetFromUtc(time, zone, out isDaylightSavings, out isAmbiguousLocalDst);
        }

        /// <summary>
        /// Helper function that calculates the UTC offset for a UTC-dateTime in a timeZone.
        /// This function assumes that the dateTime is represented in UTC and has *not* already been converted into the timeZone.
        /// </summary>
        internal static TimeSpan GetUtcOffsetFromUtc(DateTime time, TimeZoneInfo zone, out bool isDaylightSavings, out bool isAmbiguousLocalDst)
        {
            isDaylightSavings = false;
            isAmbiguousLocalDst = false;
            TimeSpan baseOffset = zone.BaseUtcOffset;
            int year;
            int? ruleIndex;
            AdjustmentRule? rule;

            if (time > s_maxDateOnly)
            {
                rule = zone.GetAdjustmentRuleForTime(DateTime.MaxValue, out ruleIndex);
                year = 9999;
            }
            else if (time < s_minDateOnly)
            {
                rule = zone.GetAdjustmentRuleForTime(DateTime.MinValue, out ruleIndex);
                year = 1;
            }
            else
            {
                rule = zone.GetAdjustmentRuleForTime(time, dateTimeisUtc: true, ruleIndex: out ruleIndex);
                Debug.Assert(rule == null || ruleIndex.HasValue, 
                    "If GetAdjustmentRuleForTime returned an AdjustmentRule, ruleIndex should also be set.");

                // As we get the associated rule using the adjusted targetTime, we should use the adjusted year (targetTime.Year) too as after adding the baseOffset,
                // sometimes the year value can change if the input datetime was very close to the beginning or the end of the year. Examples of such cases:
                //      Libya Standard Time when used with the date 2011-12-31T23:59:59.9999999Z
                //      "W. Australia Standard Time" used with date 2005-12-31T23:59:00.0000000Z
                DateTime targetTime = time + baseOffset;
                year = targetTime.Year;
            }

            if (rule != null)
            {
                baseOffset = baseOffset + rule.BaseUtcOffsetDelta;
                if (rule.HasDaylightSaving)
                {
                    isDaylightSavings = GetIsDaylightSavingsFromUtc(time, year, zone._baseUtcOffset, rule, ruleIndex, out isAmbiguousLocalDst, zone);
                    baseOffset += (isDaylightSavings ? rule.DaylightDelta : TimeSpan.Zero /* FUTURE: rule.StandardDelta */);
                }
            }

            return baseOffset;
        }

        /// <summary>
        /// Helper function that converts a year and TransitionTime into a DateTime.
        /// </summary>
        internal static DateTime TransitionTimeToDateTime(int year, TransitionTime transitionTime)
        {
            DateTime value;
            DateTime timeOfDay = transitionTime.TimeOfDay;

            if (transitionTime.IsFixedDateRule)
            {
                // create a DateTime from the passed in year and the properties on the transitionTime

                // if the day is out of range for the month then use the last day of the month
                int day = DateTime.DaysInMonth(year, transitionTime.Month);

                value = new DateTime(year, transitionTime.Month, (day < transitionTime.Day) ? day : transitionTime.Day,
                            timeOfDay.Hour, timeOfDay.Minute, timeOfDay.Second, timeOfDay.Millisecond);
            }
            else
            {
                if (transitionTime.Week <= 4)
                {
                    //
                    // Get the (transitionTime.Week)th Sunday.
                    //
                    value = new DateTime(year, transitionTime.Month, 1,
                            timeOfDay.Hour, timeOfDay.Minute, timeOfDay.Second, timeOfDay.Millisecond);

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
                    value = new DateTime(year, transitionTime.Month, daysInMonth,
                            timeOfDay.Hour, timeOfDay.Minute, timeOfDay.Second, timeOfDay.Millisecond);

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
        /// Helper function for retrieving a TimeZoneInfo object by time_zone_name.
        ///
        /// This function may return null.
        ///
        /// assumes cachedData lock is taken
        /// </summary>
        private static TimeZoneInfoResult TryGetTimeZone(string id, bool dstDisabled, out TimeZoneInfo? value, out Exception? e, CachedData cachedData, bool alwaysFallbackToLocalMachine = false)
        {
            Debug.Assert(Monitor.IsEntered(cachedData));

            TimeZoneInfoResult result = TimeZoneInfoResult.Success;
            e = null;
            TimeZoneInfo? match = null;

            // check the cache
            if (cachedData._systemTimeZones != null)
            {
                if (cachedData._systemTimeZones.TryGetValue(id, out match))
                {
                    if (dstDisabled && match._supportsDaylightSavingTime)
                    {
                        // we found a cache hit but we want a time zone without DST and this one has DST data
                        value = CreateCustomTimeZone(match._id, match._baseUtcOffset, match._displayName, match._standardDisplayName);
                    }
                    else
                    {
                        value = new TimeZoneInfo(match._id, match._baseUtcOffset, match._displayName, match._standardDisplayName,
                                              match._daylightDisplayName, match._adjustmentRules, disableDaylightSavingTime: false);
                    }
                    return result;
                }
            }

            // Fall back to reading from the local machine when the cache is not fully populated.
            // On UNIX, there may be some tzfiles that aren't in the zones.tab file, and thus aren't returned from GetSystemTimeZones().
            // If a caller asks for one of these zones before calling GetSystemTimeZones(), the time zone is returned successfully. But if
            // GetSystemTimeZones() is called first, FindSystemTimeZoneById will throw TimeZoneNotFoundException, which is inconsistent.
            // To fix this, when 'alwaysFallbackToLocalMachine' is true, even if _allSystemTimeZonesRead is true, try reading the tzfile
            // from disk, but don't add the time zone to the list returned from GetSystemTimeZones(). These time zones will only be
            // available if asked for directly.
            if (!cachedData._allSystemTimeZonesRead || alwaysFallbackToLocalMachine)
            {
                result = TryGetTimeZoneFromLocalMachine(id, dstDisabled, out value, out e, cachedData);
            }
            else
            {
                result = TimeZoneInfoResult.TimeZoneNotFoundException;
                value = null;
            }

            return result;
        }

        private static TimeZoneInfoResult TryGetTimeZoneFromLocalMachine(string id, bool dstDisabled, out TimeZoneInfo? value, out Exception? e, CachedData cachedData)
        {
            TimeZoneInfoResult result;
            TimeZoneInfo? match;

            result = TryGetTimeZoneFromLocalMachine(id, out match, out e);

            if (result == TimeZoneInfoResult.Success)
            {
                if (cachedData._systemTimeZones == null)
                    cachedData._systemTimeZones = new Dictionary<string, TimeZoneInfo>(StringComparer.OrdinalIgnoreCase);

                cachedData._systemTimeZones.Add(id, match!); // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/26761

                if (dstDisabled && match!._supportsDaylightSavingTime) // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/26761
                {
                    // we found a cache hit but we want a time zone without DST and this one has DST data
                    value = CreateCustomTimeZone(match._id, match._baseUtcOffset, match._displayName, match._standardDisplayName);
                }
                else
                {
                    value = new TimeZoneInfo(match!._id, match._baseUtcOffset, match._displayName, match._standardDisplayName, // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/26761
                                          match._daylightDisplayName, match._adjustmentRules, disableDaylightSavingTime: false);
                }
            }
            else
            {
                value = null;
            }

            return result;
        }

        /// <summary>
        /// Helper function that performs all of the validation checks for the
        /// factory methods and deserialization callback.
        /// </summary>
        private static void ValidateTimeZoneInfo(string id, TimeSpan baseUtcOffset, AdjustmentRule[]? adjustmentRules, out bool adjustmentRulesSupportDst)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            if (id.Length == 0)
            {
                throw new ArgumentException(SR.Format(SR.Argument_InvalidId, id), nameof(id));
            }

            if (UtcOffsetOutOfRange(baseUtcOffset))
            {
                throw new ArgumentOutOfRangeException(nameof(baseUtcOffset), SR.ArgumentOutOfRange_UtcOffset);
            }

            if (baseUtcOffset.Ticks % TimeSpan.TicksPerMinute != 0)
            {
                throw new ArgumentException(SR.Argument_TimeSpanHasSeconds, nameof(baseUtcOffset));
            }

            adjustmentRulesSupportDst = false;

            //
            // "adjustmentRules" can either be null or a valid array of AdjustmentRule objects.
            // A valid array is one that does not contain any null elements and all elements
            // are sorted in chronological order
            //

            if (adjustmentRules != null && adjustmentRules.Length != 0)
            {
                adjustmentRulesSupportDst = true;
                AdjustmentRule? prev = null;
                AdjustmentRule? current = null;
                for (int i = 0; i < adjustmentRules.Length; i++)
                {
                    prev = current;
                    current = adjustmentRules[i];

                    if (current == null)
                    {
                        throw new InvalidTimeZoneException(SR.Argument_AdjustmentRulesNoNulls);
                    }

                    if (!IsValidAdjustmentRuleOffest(baseUtcOffset, current))
                    {
                        throw new InvalidTimeZoneException(SR.ArgumentOutOfRange_UtcOffsetAndDaylightDelta);
                    }

                    if (prev != null && current.DateStart <= prev.DateEnd)
                    {
                        // verify the rules are in chronological order and the DateStart/DateEnd do not overlap
                        throw new InvalidTimeZoneException(SR.Argument_AdjustmentRulesOutOfOrder);
                    }
                }
            }
        }

        private static readonly TimeSpan MaxOffset = TimeSpan.FromHours(14.0);
        private static readonly TimeSpan MinOffset = -MaxOffset;
        
        /// <summary>
        /// Helper function that validates the TimeSpan is within +/- 14.0 hours
        /// </summary>
        internal static bool UtcOffsetOutOfRange(TimeSpan offset) =>
            offset < MinOffset || offset > MaxOffset;

        private static TimeSpan GetUtcOffset(TimeSpan baseUtcOffset, AdjustmentRule adjustmentRule)
        {
            return baseUtcOffset
                + adjustmentRule.BaseUtcOffsetDelta
                + (adjustmentRule.HasDaylightSaving ? adjustmentRule.DaylightDelta : TimeSpan.Zero);
        }
        
        /// <summary>
        /// Helper function that performs adjustment rule validation
        /// </summary>
        private static bool IsValidAdjustmentRuleOffest(TimeSpan baseUtcOffset, AdjustmentRule adjustmentRule)
        {
            TimeSpan utcOffset = GetUtcOffset(baseUtcOffset, adjustmentRule);
            return !UtcOffsetOutOfRange(utcOffset);
        }

        /// <summary>
        /// Normalize adjustment rule offset so that it is within valid range
        /// This method should not be called at all but is here in case something changes in the future
        /// or if really old time zones are present on the OS (no combination is known at the moment)
        /// </summary>
        private static void NormalizeAdjustmentRuleOffset(TimeSpan baseUtcOffset, ref AdjustmentRule adjustmentRule)
        {
            // Certain time zones such as:
            //       Time Zone  start date  end date    offset
            // -----------------------------------------------------
            // America/Yakutat  0001-01-01  1867-10-18   14:41:00
            // America/Yakutat  1867-10-18  1900-08-20   14:41:00
            // America/Sitka    0001-01-01  1867-10-18   14:58:00
            // America/Sitka    1867-10-18  1900-08-20   14:58:00
            // Asia/Manila      0001-01-01  1844-12-31  -15:56:00
            // Pacific/Guam     0001-01-01  1845-01-01  -14:21:00
            // Pacific/Saipan   0001-01-01  1845-01-01  -14:21:00
            //
            // have larger offset than currently supported by framework.
            // If for whatever reason we find that time zone exceeding max
            // offset of 14h this function will truncate it to the max valid offset.
            // Updating max offset may cause problems with interacting with SQL server
            // which uses SQL DATETIMEOFFSET field type which was originally designed to be
            // bit-for-bit compatible with DateTimeOffset.

            TimeSpan utcOffset = GetUtcOffset(baseUtcOffset, adjustmentRule);

            // utc base offset delta increment
            TimeSpan adjustment = TimeSpan.Zero;

            if (utcOffset > MaxOffset)
            {
                adjustment = MaxOffset - utcOffset;
            }
            else if (utcOffset < MinOffset)
            {
                adjustment = MinOffset - utcOffset;
            }

            if (adjustment != TimeSpan.Zero)
            {
                adjustmentRule = AdjustmentRule.CreateAdjustmentRule(
                    adjustmentRule.DateStart,
                    adjustmentRule.DateEnd,
                    adjustmentRule.DaylightDelta,
                    adjustmentRule.DaylightTransitionStart,
                    adjustmentRule.DaylightTransitionEnd,
                    adjustmentRule.BaseUtcOffsetDelta + adjustment,
                    adjustmentRule.NoDaylightTransitions);
            }
        }
    }
}
