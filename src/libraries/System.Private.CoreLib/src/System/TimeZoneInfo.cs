// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security;
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
    }

    [Serializable]
    [TypeForwardedFrom("System.Core, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed partial class TimeZoneInfo : IEquatable<TimeZoneInfo?>, ISerializable, IDeserializationCallback
    {
        private enum TimeZoneInfoResult
        {
            Success = 0,
            TimeZoneNotFoundException = 1,
            InvalidTimeZoneException = 2,
            SecurityException = 3
        }

        private const int MaxKeyLength = 255;

        private readonly string _id;
        private string? _displayName;
        private string? _standardDisplayName;
        private string? _daylightDisplayName;
        private readonly TimeSpan _baseUtcOffset;
        private readonly bool _supportsDaylightSavingTime;
        private readonly AdjustmentRule[]? _adjustmentRules;
        // As we support IANA and Windows IDs, it is possible we create equivalent zone objects which differ only in the IDs.
        private List<TimeZoneInfo>? _equivalentZones;

        // constants for TimeZoneInfo.Local and TimeZoneInfo.Utc
        private const string UtcId = "UTC";
        private const string LocalId = "Local";

        private static readonly TimeZoneInfo s_utcTimeZone = CreateUtcTimeZone();
        private static CachedData s_cachedData = new CachedData();

        [FeatureSwitchDefinition("System.TimeZoneInfo.Invariant")]
        internal static bool Invariant { get; } = AppContextConfigHelper.GetBooleanConfig("System.TimeZoneInfo.Invariant", "DOTNET_SYSTEM_TIMEZONE_INVARIANT");

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
                                            timeZone.DisplayName,
                                            timeZone.StandardName,
                                            timeZone.DaylightName,
                                            timeZone._adjustmentRules,
                                            disableDaylightSavingTime: false,
                                            timeZone.HasIanaId);

                        _localTimeZone = timeZone;
                    }
                    return timeZone;
                }
            }

            public TimeZoneInfo Local => _localTimeZone ?? CreateLocal();

            /// <summary>
            /// Helper function that returns the corresponding DateTimeKind for this TimeZoneInfo.
            /// </summary>
            public DateTimeKind GetCorrespondingKind(TimeZoneInfo? timeZone)
            {
                // We check reference equality to see if 'this' is the same as
                // TimeZoneInfo.Local or TimeZoneInfo.Utc.  This check is needed to
                // support setting the DateTime Kind property to 'Local' or
                // 'Utc' on the ConvertTime(...) return value.
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

            internal long GetLocalDateTimeNowTicks(DateTime utcNow, out bool isAmbiguous)
            {
                long utcNowTicks = utcNow.Ticks;
                DateTimeNowCache? dateTimeNowCache = _dateTimeNowCache;

                if (dateTimeNowCache is null || utcNowTicks >= dateTimeNowCache._nextUtcNowTransitionTicks)
                {
                    // GetNextNowTransition always create a new instance
                    dateTimeNowCache = Local.GetNextNowTransition(utcNow);
                    _dateTimeNowCache = dateTimeNowCache; // volatile write, safe publication
                }

                long localTicks = SafeCreateDateTimeFromTicks(dateTimeNowCache._nowUtcOffsetTicks + utcNowTicks).Ticks;
                isAmbiguous = localTicks >= dateTimeNowCache._dtsAmbiguousOffsetStart && localTicks <= dateTimeNowCache._dtsAmbiguousOffsetEnd;
                return localTicks;
            }

            public Dictionary<string, TimeZoneInfo>? _systemTimeZones;
            public ReadOnlyCollection<TimeZoneInfo>? _readOnlySystemTimeZones;
            public ReadOnlyCollection<TimeZoneInfo>? _readOnlyUnsortedSystemTimeZones;
            public Dictionary<string, TimeZoneInfo>? _timeZonesUsingAlternativeIds;
            public bool _allSystemTimeZonesRead;
            public volatile DateTimeNowCache? _dateTimeNowCache;
        }

        public string Id => _id;

        /// <summary>
        /// Returns true if this TimeZoneInfo object has an IANA ID.
        /// </summary>
        public bool HasIanaId { get; }

        public string DisplayName
        {
            get
            {
                if (_displayName == null)
                    Interlocked.CompareExchange(ref _displayName, PopulateDisplayName(), null);

                return _displayName ?? string.Empty;
            }
        }

        public string StandardName
        {
            get
            {
                if (_standardDisplayName == null)
                    Interlocked.CompareExchange(ref _standardDisplayName, PopulateStandardDisplayName(), null);

                return _standardDisplayName ?? string.Empty;
            }
        }

        public string DaylightName
        {
            get
            {
                if (_daylightDisplayName == null)
                    Interlocked.CompareExchange(ref _daylightDisplayName, PopulateDaylightDisplayName(), null);

                return _daylightDisplayName ?? string.Empty;
            }
        }

        public TimeSpan BaseUtcOffset => _baseUtcOffset;

        public bool SupportsDaylightSavingTime => _supportsDaylightSavingTime;

        /// <summary>
        /// Returns an array of TimeSpan objects representing all of
        /// the possible UTC offset values for this ambiguous time.
        /// </summary>
        public TimeSpan[] GetAmbiguousTimeOffsets(DateTimeOffset dateTimeOffset)
        {
            Span<TimeSpan> offsets = stackalloc TimeSpan[2];

            if (!SupportsDaylightSavingTime || !IsAmbiguousLocalTime(ConvertTime(dateTimeOffset, this).DateTime, offsets))
            {
                throw new ArgumentException(SR.Argument_DateTimeOffsetIsNotAmbiguous, nameof(dateTimeOffset));
            }

            return offsets.ToArray();
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

            Span<TimeSpan> offsets = stackalloc TimeSpan[2];
            if (!IsAmbiguousLocalTime(adjustedTime, offsets))
            {
                throw new ArgumentException(SR.Argument_DateTimeIsNotAmbiguous, nameof(dateTime));
            }

            return offsets.ToArray();
        }

        /// <summary>
        /// Returns the Universal Coordinated Time (UTC) Offset for the current TimeZoneInfo instance.
        /// </summary>
        public TimeSpan GetUtcOffset(DateTimeOffset dateTimeOffset) =>
            GetOffsetForUtcDate(dateTimeOffset.UtcDateTime, out _);

        /// <summary>
        /// Returns the Universal Coordinated Time (UTC) Offset for the current TimeZoneInfo instance.
        /// </summary>
        public TimeSpan GetUtcOffset(DateTime dateTime) =>
            GetUtcOffset(dateTime, TimeZoneInfoOptions.NoThrowOnInvalidTime, s_cachedData);

        // Shortcut for TimeZoneInfo.Local.GetUtcOffset, it is called from DateTime and DateTimeOffset types.
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

            return TryGetUtcOffset(dateTime, out TimeSpan offset) ? offset : _baseUtcOffset;
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

            return IsAmbiguousLocalTime(adjustedTime);
        }

        /// <summary>
        /// Returns true if the time is during Daylight Saving time for the current TimeZoneInfo instance.
        /// </summary>
        public bool IsDaylightSavingTime(DateTimeOffset dateTimeOffset)
        {
            GetUtcOffsetFromUtc(dateTimeOffset.UtcDateTime, this, out bool isDaylightSavingTime);
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
                    GetUtcOffsetFromUtc(dateTime, this, out bool isDaylightSavings);
                    return isDaylightSavings;
                }
            }
            else
            {
                adjustedTime = dateTime;
            }

            return IsDaylightSavingOn(adjustedTime);
        }

        /// <summary>
        /// Returns true when dateTime falls into a "hole in time".
        /// </summary>
        public bool IsInvalidTime(DateTime dateTime)
            => (dateTime.Kind == DateTimeKind.Unspecified) || (dateTime.Kind == DateTimeKind.Local && s_cachedData.GetCorrespondingKind(this) == DateTimeKind.Local) ?
                IsInvalidLocalTime(dateTime) : false;

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
        /// Helper function for retrieving a <see cref="TimeZoneInfo"/> object by time zone name.
        /// This function wraps the logic necessary to keep the private
        /// SystemTimeZones cache in working order.
        ///
        /// This function will either return a valid <see cref="TimeZoneInfo"/> instance or
        /// it will throw <see cref="InvalidTimeZoneException"/> / <see cref="TimeZoneNotFoundException"/> /
        /// <see cref="SecurityException"/>
        /// </summary>
        /// <param name="id">Time zone name.</param>
        /// <returns>Valid <see cref="TimeZoneInfo"/> instance.</returns>
        public static TimeZoneInfo FindSystemTimeZoneById(string id)
        {
            TimeZoneInfo? value;
            Exception? e;

            TimeZoneInfoResult result = TryFindSystemTimeZoneById(id, out value, out e);
            switch (result)
            {
                case TimeZoneInfoResult.Success:
                    return value!;
                case TimeZoneInfoResult.InvalidTimeZoneException:
                    Debug.Assert(e is InvalidTimeZoneException,
                        "TryGetTimeZone must create an InvalidTimeZoneException when it returns TimeZoneInfoResult.InvalidTimeZoneException");
                    throw e;
                case TimeZoneInfoResult.SecurityException:
                    throw new SecurityException(SR.Format(SR.Security_CannotReadFileData, id), e);
                default:
                    throw new TimeZoneNotFoundException(SR.Format(SR.TimeZoneNotFound_MissingData, id), e);
            }
        }

        /// <summary>
        /// Helper function for retrieving a <see cref="TimeZoneInfo"/> object by time zone name.
        /// This function wraps the logic necessary to keep the private
        /// SystemTimeZones cache in working order.
        ///
        /// This function will either return <c>true</c> and a valid <see cref="TimeZoneInfo"/>
        /// instance or return <c>false</c> and <c>null</c>.
        /// </summary>
        /// <param name="id">Time zone name.</param>
        /// <param name="timeZoneInfo">A valid retrieved <see cref="TimeZoneInfo"/> or <c>null</c>.</param>
        /// <returns><c>true</c> if the <see cref="TimeZoneInfo"/> object was successfully retrieved, <c>false</c> otherwise.</returns>
        public static bool TryFindSystemTimeZoneById(string id, [NotNullWhenAttribute(true)] out TimeZoneInfo? timeZoneInfo)
            => TryFindSystemTimeZoneById(id, out timeZoneInfo, out _) == TimeZoneInfoResult.Success;

        /// <summary>
        /// Helper function for retrieving a TimeZoneInfo object by time_zone_name.
        /// This function wraps the logic necessary to keep the private
        /// SystemTimeZones cache in working order.
        ///
        /// This function will either return:
        /// <c>TimeZoneInfoResult.Success</c> and a valid <see cref="TimeZoneInfo"/>instance and <c>null</c> Exception or
        /// <c>TimeZoneInfoResult.TimeZoneNotFoundException</c> and <c>null</c> <see cref="TimeZoneInfo"/> and Exception (can be null) or
        /// other <c>TimeZoneInfoResult</c> and <c>null</c> <see cref="TimeZoneInfo"/> and valid Exception.
        /// </summary>
        private static TimeZoneInfoResult TryFindSystemTimeZoneById(string id, out TimeZoneInfo? timeZone, out Exception? e)
        {
            // Special case for Utc as it will not exist in the dictionary with the rest
            // of the system time zones.  There is no need to do this check for Local.Id
            // since Local is a real time zone that exists in the dictionary cache
            if (string.Equals(id, UtcId, StringComparison.OrdinalIgnoreCase))
            {
                timeZone = Utc;
                e = default;
                return TimeZoneInfoResult.Success;
            }

            ArgumentNullException.ThrowIfNull(id);
            if (id.Length == 0 || id.Length > MaxKeyLength || id.Contains('\0'))
            {
                timeZone = default;
                e = default;
                return TimeZoneInfoResult.TimeZoneNotFoundException;
            }

            CachedData cachedData = s_cachedData;

            lock (cachedData)
            {
                return TryGetTimeZone(id, out timeZone, out e, cachedData);
            }
        }

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
            ArgumentNullException.ThrowIfNull(destinationTimeZone);

            // calculate the destination time zone offset
            DateTime utcDateTime = dateTimeOffset.UtcDateTime;
            TimeSpan destinationOffset = destinationTimeZone.GetOffsetForUtcDate(utcDateTime, out _);

            // check for overflow
            long ticks = utcDateTime.Ticks + destinationOffset.Ticks;

            return
                ticks > DateTime.MaxTicks ? DateTimeOffset.MaxValue :
                ticks < DateTime.MinTicks ? DateTimeOffset.MinValue :
                new DateTimeOffset(ticks, destinationOffset);
        }

        /// <summary>
        /// Converts the value of the dateTime object from sourceTimeZone to destinationTimeZone
        /// </summary>
        public static DateTime ConvertTime(DateTime dateTime, TimeZoneInfo destinationTimeZone)
        {
            ArgumentNullException.ThrowIfNull(destinationTimeZone);

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
            ArgumentNullException.ThrowIfNull(sourceTimeZone);
            ArgumentNullException.ThrowIfNull(destinationTimeZone);

            DateTimeKind sourceKind = cachedData.GetCorrespondingKind(sourceTimeZone);
            if (((flags & TimeZoneInfoOptions.NoThrowOnInvalidTime) == 0) && (dateTime.Kind != DateTimeKind.Unspecified) && (dateTime.Kind != sourceKind))
            {
                throw new ArgumentException(SR.Argument_ConvertMismatch, nameof(sourceTimeZone));
            }

            bool isInvalidTime = !sourceTimeZone.TryLocalToUtc(dateTime, out DateTime utcDateTime);

            if (((flags & TimeZoneInfoOptions.NoThrowOnInvalidTime) == 0) && isInvalidTime)
            {
                throw new ArgumentException(SR.Argument_DateTimeIsInvalid, nameof(dateTime));
            }

            if (isInvalidTime)
            {
                // This is not logical to do but we are keeping it for app compatibility reason.
                // We get here if the dateTime is invalid in the source time zone.
                utcDateTime = new DateTime(dateTime.Ticks + sourceTimeZone.BaseUtcOffset.Ticks, DateTimeKind.Utc);
            }

            DateTimeKind targetKind = cachedData.GetCorrespondingKind(destinationTimeZone);

            // handle the special case of Loss-less Local->Local and UTC->UTC)
            if (dateTime.Kind != DateTimeKind.Unspecified && sourceKind != DateTimeKind.Unspecified && sourceKind == targetKind)
            {
                return dateTime;
            }

            DateTime targetConverted = destinationTimeZone.UtcToLocal(utcDateTime, out _);

            if (targetKind == DateTimeKind.Local)
            {
                // Because the ticks conversion between UTC and local is lossy, we need to capture whether the
                // time is in a repeated hour so that it can be passed to the DateTime constructor.
                return new DateTime(targetConverted.Ticks, DateTimeKind.Local, destinationTimeZone.IsAmbiguousLocalTime(targetConverted));
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
            Debug.Assert(dateTime.Kind != DateTimeKind.Utc);
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
        public bool Equals([NotNullWhen(true)] TimeZoneInfo? other) =>
            other != null &&
            string.Equals(_id, other._id, StringComparison.OrdinalIgnoreCase) &&
            HasSameRules(other);

        public override bool Equals([NotNullWhen(true)] object? obj) => Equals(obj as TimeZoneInfo);

        public static TimeZoneInfo FromSerializedString(string source)
        {
            ArgumentNullException.ThrowIfNull(source);

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
        public static ReadOnlyCollection<TimeZoneInfo> GetSystemTimeZones() => GetSystemTimeZones(skipSorting: false);

        /// <summary>
        /// Returns a <see cref="ReadOnlyCollection{TimeZoneInfo}"/> containing all valid TimeZone's from the local machine.
        /// This method does *not* throw TimeZoneNotFoundException or InvalidTimeZoneException.
        /// </summary>
        /// <param name="skipSorting">If true, The collection returned may not necessarily be sorted.</param>
        /// <remarks>By setting the skipSorting parameter to true, the method will attempt to avoid sorting the returned collection.
        /// This option can be beneficial when the caller does not require a sorted list and aims to enhance the performance. </remarks>
        public static ReadOnlyCollection<TimeZoneInfo> GetSystemTimeZones(bool skipSorting)
        {
            CachedData cachedData = s_cachedData;

            lock (cachedData)
            {
                if ((skipSorting ? cachedData._readOnlyUnsortedSystemTimeZones : cachedData._readOnlySystemTimeZones) is null)
                {
                    if (!cachedData._allSystemTimeZonesRead)
                    {
                        PopulateAllSystemTimeZones(cachedData);
                        cachedData._allSystemTimeZonesRead = true;
                    }

                    if (cachedData._systemTimeZones != null)
                    {
                        // return a collection of the cached system time zones
                        TimeZoneInfo[] array = new TimeZoneInfo[cachedData._systemTimeZones.Count];
                        cachedData._systemTimeZones.Values.CopyTo(array, 0);

                        if (!skipSorting)
                        {
                            // sort and copy the TimeZoneInfo's into a ReadOnlyCollection for the user
                            Array.Sort(array, static (x, y) =>
                            {
                                // sort by BaseUtcOffset first and by DisplayName second - this is similar to the Windows Date/Time control panel
                                int comparison = x.BaseUtcOffset.CompareTo(y.BaseUtcOffset);
                                return comparison == 0 ? string.CompareOrdinal(x.DisplayName, y.DisplayName) : comparison;
                            });

                            // Always reset _readOnlyUnsortedSystemTimeZones even if it was initialized before. This prevents the need to maintain two separate cache lists in memory
                            // and guarantees that if _readOnlySystemTimeZones is initialized, _readOnlyUnsortedSystemTimeZones is also initialized.
                            cachedData._readOnlySystemTimeZones = cachedData._readOnlyUnsortedSystemTimeZones = new ReadOnlyCollection<TimeZoneInfo>(array);
                        }
                        else
                        {
                            cachedData._readOnlyUnsortedSystemTimeZones = new ReadOnlyCollection<TimeZoneInfo>(array);
                        }
                    }
                    else
                    {
                        cachedData._readOnlySystemTimeZones = cachedData._readOnlyUnsortedSystemTimeZones = ReadOnlyCollection<TimeZoneInfo>.Empty;
                    }
                }
            }

            return skipSorting ? cachedData._readOnlyUnsortedSystemTimeZones! : cachedData._readOnlySystemTimeZones!;
        }

        /// <summary>
        /// Value equality on the "adjustmentRules" array
        /// </summary>
        public bool HasSameRules(TimeZoneInfo other)
        {
            ArgumentNullException.ThrowIfNull(other);

            // check the utcOffset and supportsDaylightSavingTime members
            if (_baseUtcOffset != other._baseUtcOffset ||
                _supportsDaylightSavingTime != other._supportsDaylightSavingTime)
            {
                return false;
            }

            AdjustmentRule[]? currentRules = _adjustmentRules;
            AdjustmentRule[]? otherRules = other._adjustmentRules;

            if (currentRules is null && otherRules is null)
            {
                return true;
            }

            return currentRules.AsSpan().SequenceEqual(otherRules);
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
        //                          <_standardDisplayName>;<_daylightDisplayName>;
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
                bool disableDaylightSavingTime,
                bool hasIanaId = false)
        {
            ValidateTimeZoneInfo(id, baseUtcOffset, adjustmentRules, out bool adjustmentRulesSupportDst);

            _id = id;
            _baseUtcOffset = baseUtcOffset;
            _displayName = displayName;
            _standardDisplayName = standardDisplayName;
            _daylightDisplayName = disableDaylightSavingTime ? null : daylightDisplayName;
            _supportsDaylightSavingTime = adjustmentRulesSupportDst && !disableDaylightSavingTime;
            _adjustmentRules = adjustmentRules;

            HasIanaId = hasIanaId || _id.Equals(UtcId, StringComparison.OrdinalIgnoreCase);
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
            bool hasIanaId = TryConvertIanaIdToWindowsId(id, allocate: false, out _);

            standardDisplayName ??= string.Empty;

            return new TimeZoneInfo(
                id,
                baseUtcOffset,
                displayName ?? string.Empty,
                standardDisplayName,
                standardDisplayName,
                adjustmentRules: null,
                disableDaylightSavingTime: false,
                hasIanaId);
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

            bool hasIanaId = TryConvertIanaIdToWindowsId(id, allocate: false, out _);

            return new TimeZoneInfo(
                id,
                baseUtcOffset,
                displayName ?? string.Empty,
                standardDisplayName ?? string.Empty,
                daylightDisplayName ?? string.Empty,
                adjustmentRules,
                disableDaylightSavingTime,
                hasIanaId);
        }

        /// <summary>
        /// Tries to convert an IANA time zone ID to a Windows ID.
        /// </summary>
        /// <param name="ianaId">The IANA time zone ID.</param>
        /// <param name="windowsId">String object holding the Windows ID which resulted from the IANA ID conversion.</param>
        /// <returns>True if the ID conversion succeeded, false otherwise.</returns>
        public static bool TryConvertIanaIdToWindowsId(string ianaId, [NotNullWhen(true)] out string? windowsId) => TryConvertIanaIdToWindowsId(ianaId, allocate: true, out windowsId);

        /// <summary>
        /// Tries to convert a Windows time zone ID to an IANA ID.
        /// </summary>
        /// <param name="windowsId">The Windows time zone ID.</param>
        /// <param name="ianaId">String object holding the IANA ID which resulted from the Windows ID conversion.</param>
        /// <returns>True if the ID conversion succeeded, false otherwise.</returns>
        public static bool TryConvertWindowsIdToIanaId(string windowsId, [NotNullWhen(true)] out string? ianaId) => TryConvertWindowsIdToIanaId(windowsId, region: null, allocate: true, out ianaId);

        /// <summary>
        /// Tries to convert a Windows time zone ID to an IANA ID.
        /// </summary>
        /// <param name="windowsId">The Windows time zone ID.</param>
        /// <param name="region">The ISO 3166 code for the country/region.</param>
        /// <param name="ianaId">String object holding the IANA ID which resulted from the Windows ID conversion.</param>
        /// <returns>True if the ID conversion succeeded, false otherwise.</returns>
        public static bool TryConvertWindowsIdToIanaId(string windowsId, string? region, [NotNullWhen(true)] out string? ianaId) => TryConvertWindowsIdToIanaId(windowsId, region, allocate: true, out ianaId);

        void IDeserializationCallback.OnDeserialization(object? sender)
        {
            try
            {
                ValidateTimeZoneInfo(_id, _baseUtcOffset, _adjustmentRules, out bool adjustmentRulesSupportDst);

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
            ArgumentNullException.ThrowIfNull(info);

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
            ArgumentNullException.ThrowIfNull(info);

            _id = (string)info.GetValue("Id", typeof(string))!; // Do not rename (binary serialization)
            _displayName = (string?)info.GetValue("DisplayName", typeof(string)); // Do not rename (binary serialization)
            _standardDisplayName = (string?)info.GetValue("StandardName", typeof(string)); // Do not rename (binary serialization)
            _daylightDisplayName = (string?)info.GetValue("DaylightName", typeof(string)); // Do not rename (binary serialization)
            _baseUtcOffset = (TimeSpan)info.GetValue("BaseUtcOffset", typeof(TimeSpan))!; // Do not rename (binary serialization)
            _adjustmentRules = (AdjustmentRule[]?)info.GetValue("AdjustmentRules", typeof(AdjustmentRule[])); // Do not rename (binary serialization)
            _supportsDaylightSavingTime = (bool)info.GetValue("SupportsDaylightSavingTime", typeof(bool))!; // Do not rename (binary serialization)
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
            DateTime dateTime, DateTime dateOnly, bool dateTimeIsUtc)
        {
            bool isAfterStart;
            if (rule.DateStart.Kind == DateTimeKind.Utc)
            {
                DateTime dateTimeToCompare = dateTimeIsUtc ?
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
                DateTime dateTimeToCompare = dateTimeIsUtc ?
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
                ticks > DateTime.MaxTicks ? DateTime.MaxValue :
                ticks < DateTime.MinTicks ? DateTime.MinValue :
                new DateTime(ticks);
        }

        /// <summary>
        /// Helper function that calculates the UTC offset for a UTC-dateTime in a timeZone.
        /// This function assumes that the dateTime is represented in UTC and has *not* already been converted into the timeZone.
        /// </summary>
        private static TimeSpan GetUtcOffsetFromUtc(DateTime time, TimeZoneInfo zone) =>
            GetUtcOffsetFromUtc(time, zone, out _);

        /// <summary>
        /// Helper function that calculates the UTC offset for a UTC-dateTime in a timeZone.
        /// This function assumes that the dateTime is represented in UTC and has *not* already been converted into the timeZone.
        /// </summary>
        private static TimeSpan GetUtcOffsetFromUtc(DateTime time, TimeZoneInfo zone, out bool isDaylightSavings) =>
            GetUtcOffsetFromUtc(time, zone, out isDaylightSavings, out _);

        /// <summary>
        /// Helper function that calculates the UTC offset for a UTC-dateTime in a timeZone.
        /// This function assumes that the dateTime is represented in UTC and has *not* already been converted into the timeZone.
        /// </summary>
        internal static TimeSpan GetUtcOffsetFromUtc(DateTime time, TimeZoneInfo zone, out bool isDaylightSavings, out bool isAmbiguousLocalDst)
            => zone.GetOffsetForUtcDate(time, out isDaylightSavings, out isAmbiguousLocalDst);

        /// <summary>
        /// Helper function for retrieving a TimeZoneInfo object by time_zone_name.
        ///
        /// This function may return null.
        ///
        /// assumes cachedData lock is taken
        /// </summary>
        private static TimeZoneInfoResult TryGetTimeZone(string id, bool dstDisabled, out TimeZoneInfo? value, out Exception? e, CachedData cachedData, bool alwaysFallbackToLocalMachine = false)
        {
            TimeZoneInfoResult result = TryGetTimeZoneUsingId(id, dstDisabled, out value, out e, cachedData, alwaysFallbackToLocalMachine);
            if (result != TimeZoneInfoResult.Success)
            {
                string? alternativeId = GetAlternativeId(id, out bool idIsIana);
                if (alternativeId != null)
                {
                    result = TryGetTimeZoneUsingId(alternativeId, dstDisabled, out value, out e, cachedData, alwaysFallbackToLocalMachine);
                    if (result == TimeZoneInfoResult.Success)
                    {
                        TimeZoneInfo? zone = null;
                        if (value!._equivalentZones == null)
                        {
                            zone = new TimeZoneInfo(id, value._baseUtcOffset, value._displayName, value._standardDisplayName,
                                                    value._daylightDisplayName, value._adjustmentRules, dstDisabled && value._supportsDaylightSavingTime, idIsIana);
                            value._equivalentZones = new List<TimeZoneInfo>();
                            lock (value._equivalentZones)
                            {
                                value._equivalentZones.Add(zone);
                            }
                        }
                        else
                        {
                            foreach (TimeZoneInfo tzi in value._equivalentZones)
                            {
                                if (tzi.Id == id)
                                {
                                    zone = tzi;
                                    break;
                                }
                            }
                            if (zone == null)
                            {
                                zone = new TimeZoneInfo(id, value._baseUtcOffset, value._displayName, value._standardDisplayName,
                                                        value._daylightDisplayName, value._adjustmentRules, dstDisabled && value._supportsDaylightSavingTime, idIsIana);
                                lock (value._equivalentZones)
                                {
                                    value._equivalentZones.Add(zone);
                                }
                            }
                        }

                        cachedData._timeZonesUsingAlternativeIds ??= new Dictionary<string, TimeZoneInfo>(StringComparer.OrdinalIgnoreCase);
                        cachedData._timeZonesUsingAlternativeIds[id] = zone;

                        Debug.Assert(zone != null);
                        value = zone;
                    }
                }
            }

            return result;
        }

        private static TimeZoneInfoResult TryGetTimeZoneUsingId(string id, bool dstDisabled, out TimeZoneInfo? value, out Exception? e, CachedData cachedData, bool alwaysFallbackToLocalMachine)
        {
            Debug.Assert(Monitor.IsEntered(cachedData));

            TimeZoneInfoResult result = TimeZoneInfoResult.Success;
            e = null;

            if (Invariant && !cachedData._allSystemTimeZonesRead)
            {
                PopulateAllSystemTimeZones(cachedData);
                cachedData._allSystemTimeZonesRead = true;
            }

            // check the cache
            if (cachedData._systemTimeZones != null)
            {
                if (cachedData._systemTimeZones.TryGetValue(id, out value))
                {
                    if (dstDisabled && value._supportsDaylightSavingTime)
                    {
                        // we found a cache hit but we want a time zone without DST and this one has DST data
                        value = CreateCustomTimeZone(value._id, value._baseUtcOffset, value._displayName, value._standardDisplayName);
                    }

                    return result;
                }
            }

            if (Invariant)
            {
                value = null;
                return TimeZoneInfoResult.TimeZoneNotFoundException;
            }

            if (cachedData._timeZonesUsingAlternativeIds != null)
            {
                if (cachedData._timeZonesUsingAlternativeIds.TryGetValue(id, out value))
                {
                    if (dstDisabled && value._supportsDaylightSavingTime)
                    {
                        // we found a cache hit but we want a time zone without DST and this one has DST data
                        value = CreateCustomTimeZone(value._id, value._baseUtcOffset, value._displayName, value._standardDisplayName);
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
            Debug.Assert(!Invariant);

            TimeZoneInfoResult result;

            result = TryGetTimeZoneFromLocalMachine(id, out value, out e);

            if (result == TimeZoneInfoResult.Success)
            {
                cachedData._systemTimeZones ??= new Dictionary<string, TimeZoneInfo>(StringComparer.OrdinalIgnoreCase)
                {
                    { UtcId, s_utcTimeZone }
                };

                // Avoid using multiple Utc objects to ensure consistency and correctness as we have some code
                // uses reference equality with the Utc object.
                if (!id.Equals(UtcId, StringComparison.OrdinalIgnoreCase))
                {
                    cachedData._systemTimeZones.Add(id, value!);
                }

                if (dstDisabled && value!._supportsDaylightSavingTime)
                {
                    // we found a cache hit but we want a time zone without DST and this one has DST data
                    value = CreateCustomTimeZone(value._id, value._baseUtcOffset, value._displayName, value._standardDisplayName);
                }
            }

            return result;
        }

        /// <summary>
        /// Helper function that performs all of the validation checks for the
        /// factory methods and deserialization callback.
        /// </summary>
        private static void ValidateTimeZoneInfo(string id, TimeSpan baseUtcOffset, AdjustmentRule[]? adjustmentRules, out bool adjustmentRulesSupportDst)
        {
            ArgumentException.ThrowIfNullOrEmpty(id);

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
                AdjustmentRule? current = null;
                for (int i = 0; i < adjustmentRules.Length; i++)
                {
                    AdjustmentRule? prev = current;
                    current = adjustmentRules[i];

                    if (current == null)
                    {
                        throw new InvalidTimeZoneException(SR.Argument_AdjustmentRulesNoNulls);
                    }

                    if (!IsValidAdjustmentRuleOffset(baseUtcOffset, current))
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

        private static TimeSpan MaxOffset => TimeSpan.FromHours(14);
        private static TimeSpan MinOffset => TimeSpan.FromHours(-14);

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
        private static bool IsValidAdjustmentRuleOffset(TimeSpan baseUtcOffset, AdjustmentRule adjustmentRule)
        {
            TimeSpan utcOffset = GetUtcOffset(baseUtcOffset, adjustmentRule);
            return !UtcOffsetOutOfRange(utcOffset);
        }

        // Helper function to create the static UTC time zone instance
        private static TimeZoneInfo CreateUtcTimeZone()
        {
            string standardDisplayName = GetUtcStandardDisplayName();
            string displayName = GetUtcFullDisplayName(UtcId, standardDisplayName);
            return CreateCustomTimeZone(UtcId, TimeSpan.Zero, displayName, standardDisplayName);
        }
    }
}
