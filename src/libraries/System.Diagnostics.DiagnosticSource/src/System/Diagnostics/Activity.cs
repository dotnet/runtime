// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace System.Diagnostics
{
    /// <summary>
    /// Activity represents operation with context to be used for logging.
    /// Activity has operation name, Id, start time and duration, tags and baggage.
    ///
    /// Current activity can be accessed with static AsyncLocal variable Activity.Current.
    ///
    /// Activities should be created with constructor, configured as necessary
    /// and then started with Activity.Start method which maintains parent-child
    /// relationships for the activities and sets Activity.Current.
    ///
    /// When activity is finished, it should be stopped with static Activity.Stop method.
    ///
    /// No methods on Activity allow exceptions to escape as a response to bad inputs.
    /// They are thrown and caught (that allows Debuggers and Monitors to see the error)
    /// but the exception is suppressed, and the operation does something reasonable (typically
    /// doing nothing).
    /// </summary>
    public partial class Activity : IDisposable
    {
#pragma warning disable CA1825 // Array.Empty<T>() doesn't exist in all configurations
        private static readonly IEnumerable<KeyValuePair<string, string?>> s_emptyBaggageTags = new KeyValuePair<string, string?>[0];
        private static readonly IEnumerable<KeyValuePair<string, object?>> s_emptyTagObjects = new KeyValuePair<string, object?>[0];
        private static readonly IEnumerable<ActivityLink> s_emptyLinks = new ActivityLink[0];
        private static readonly IEnumerable<ActivityEvent> s_emptyEvents = new ActivityEvent[0];
#pragma warning restore CA1825
        private static readonly ActivitySource s_defaultSource = new ActivitySource(string.Empty);

        private const byte ActivityTraceFlagsIsSet = 0b_1_0000000; // Internal flag to indicate if flags have been set
        private const int RequestIdMaxLength = 1024;

        // Used to generate an ID it represents the machine and process we are in.
        private static readonly string s_uniqSuffix = "-" + GetRandomNumber().ToString("x") + ".";

        // A unique number inside the appdomain, randomized between appdomains.
        // Int gives enough randomization and keeps hex-encoded s_currentRootId 8 chars long for most applications
        private static long s_currentRootId = (uint)GetRandomNumber();
        private static ActivityIdFormat s_defaultIdFormat;
        /// <summary>
        /// Normally if the ParentID is defined, the format of that is used to determine the
        /// format used by the Activity. However if ForceDefaultFormat is set to true, the
        /// ID format will always be the DefaultIdFormat even if the ParentID is define and is
        /// a different format.
        /// </summary>
        public static bool ForceDefaultIdFormat { get; set; }

        private string? _traceState;
        private State _state;
        private int _currentChildId;  // A unique number for all children of this activity.

        // State associated with ID.
        private string? _id;
        private string? _rootId;
        // State associated with ParentId.
        private string? _parentId;

        // W3C formats
        private string? _parentSpanId;
        private string? _traceId;
        private string? _spanId;

        private byte _w3CIdFlags;

        private TagsLinkedList? _tags;
        private LinkedList<KeyValuePair<string, string?>>? _baggage;
        private LinkedList<ActivityLink>? _links;
        private LinkedList<ActivityEvent>? _events;
        private ConcurrentDictionary<string, object>? _customProperties;
        private string? _displayName;

        /// <summary>
        /// Gets the relationship between the Activity, its parents, and its children in a Trace.
        /// </summary>
        public ActivityKind Kind { get; private set; } = ActivityKind.Internal;

        /// <summary>
        /// An operation name is a COARSEST name that is useful grouping/filtering.
        /// The name is typically a compile-time constant.   Names of Rest APIs are
        /// reasonable, but arguments (e.g. specific accounts etc), should not be in
        /// the name but rather in the tags.
        /// </summary>
        public string OperationName { get; }

        /// <summary>Gets or sets the display name of the Activity</summary>
        /// <remarks>
        /// DisplayName is intended to be used in a user interface and need not be the same as OperationName.
        /// </remarks>
        public string DisplayName
        {
            get => _displayName ?? OperationName;
            set => _displayName = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>Get the ActivitySource object associated with this Activity.</summary>
        /// <remarks>
        /// All Activities created from public constructors will have a singleton source where the source name is an empty string.
        /// Otherwise, the source will hold the object that created the Activity through ActivitySource.StartActivity.
        /// </remarks>
        public ActivitySource Source { get; private set; }

        /// <summary>
        /// If the Activity that created this activity is from the same process you can get
        /// that Activity with Parent.  However, this can be null if the Activity has no
        /// parent (a root activity) or if the Parent is from outside the process.
        /// </summary>
        /// <seealso cref="ParentId"/>
        public Activity? Parent { get; private set; }

        /// <summary>
        /// If the Activity has ended (<see cref="Stop"/> or <see cref="SetEndTime"/> was called) then this is the delta
        /// between <see cref="StartTimeUtc"/> and end.   If Activity is not ended and <see cref="SetEndTime"/> was not called then this is
        /// <see cref="TimeSpan.Zero"/>.
        /// </summary>
        public TimeSpan Duration { get; private set; }

        /// <summary>
        /// The time that operation started.  It will typically be initialized when <see cref="Start"/>
        /// is called, but you can set at any time via <see cref="SetStartTime(DateTime)"/>.
        /// </summary>
        public DateTime StartTimeUtc { get; private set; }

        /// <summary>
        /// This is an ID that is specific to a particular request.   Filtering
        /// to a particular ID insures that you get only one request that matches.
        /// Id has a hierarchical structure: '|root-id.id1_id2.id3_' Id is generated when
        /// <see cref="Start"/> is called by appending suffix to Parent.Id
        /// or ParentId; Activity has no Id until it started
        /// <para/>
        /// See <see href="https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/ActivityUserGuide.md#id-format"/> for more details
        /// </summary>
        /// <example>
        /// Id looks like '|a000b421-5d183ab6.1.8e2d4c28_1.':<para />
        ///  - '|a000b421-5d183ab6.' - Id of the first, top-most, Activity created<para />
        ///  - '|a000b421-5d183ab6.1.' - Id of a child activity. It was started in the same process as the first activity and ends with '.'<para />
        ///  - '|a000b421-5d183ab6.1.8e2d4c28_' - Id of the grand child activity. It was started in another process and ends with '_'<para />
        /// 'a000b421-5d183ab6' is a <see cref="RootId"/> for the first Activity and all its children
        /// </example>
        public string? Id
        {
#if ALLOW_PARTIALLY_TRUSTED_CALLERS
        [System.Security.SecuritySafeCriticalAttribute]
#endif
            get
            {
                // if we represented it as a traceId-spanId, convert it to a string.
                // We can do this concatenation with a stackalloced Span<char> if we actually used Id a lot.
                if (_id == null && _spanId != null)
                {
                    // Convert flags to binary.
                    Span<char> flagsChars = stackalloc char[2];
                    HexConverter.ToCharsBuffer((byte)((~ActivityTraceFlagsIsSet) & _w3CIdFlags), flagsChars, 0, HexConverter.Casing.Lower);
                    string id = "00-" + _traceId + "-" + _spanId + "-" + flagsChars.ToString();

                    Interlocked.CompareExchange(ref _id, id, null);

                }
                return _id;
            }
        }

        /// <summary>
        /// If the parent for this activity comes from outside the process, the activity
        /// does not have a Parent Activity but MAY have a ParentId (which was deserialized from
        /// from the parent).   This accessor fetches the parent ID if it exists at all.
        /// Note this can be null if this is a root Activity (it has no parent)
        /// <para/>
        /// See <see href="https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/ActivityUserGuide.md#id-format"/> for more details
        /// </summary>
        public string? ParentId
        {
            get
            {
                // if we represented it as a traceId-spanId, convert it to a string.
                if (_parentId == null)
                {
                    if (_parentSpanId != null)
                    {
                        string parentId = "00-" + _traceId + "-" + _parentSpanId + "-00";
                        Interlocked.CompareExchange(ref _parentId, parentId, null);
                    }
                    else if (Parent != null)
                    {
                        Interlocked.CompareExchange(ref _parentId, Parent.Id, null);
                    }
                }

                return _parentId;
            }
        }

        /// <summary>
        /// Root Id is substring from Activity.Id (or ParentId) between '|' (or beginning) and first '.'.
        /// Filtering by root Id allows to find all Activities involved in operation processing.
        /// RootId may be null if Activity has neither ParentId nor Id.
        /// See <see href="https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/ActivityUserGuide.md#id-format"/> for more details
        /// </summary>
        public string? RootId
        {
            get
            {
                //we expect RootId to be requested at any time after activity is created,
                //possibly even before it was started for sampling or logging purposes
                //Presumably, it will be called by logging systems for every log record, so we cache it.
                if (_rootId == null)
                {
                    string? rootId = null;
                    if (Id != null)
                    {
                        rootId = GetRootId(Id);
                    }
                    else if (ParentId != null)
                    {
                        rootId = GetRootId(ParentId);
                    }

                    if (rootId != null)
                    {
                        Interlocked.CompareExchange(ref _rootId, rootId, null);
                    }
                }

                return _rootId;
            }
        }

        /// <summary>
        /// Tags are string-string key-value pairs that represent information that will
        /// be logged along with the Activity to the logging system. This information
        /// however is NOT passed on to the children of this activity.
        /// </summary>
        /// <seealso cref="Baggage"/>
        public IEnumerable<KeyValuePair<string, string?>> Tags
        {
            get => _tags?.EnumerateStringValues() ?? s_emptyBaggageTags;
        }

        /// <summary>
        /// List of the tags which represent information that will be logged along with the Activity to the logging system.
        /// This information however is NOT passed on to the children of this activity.
        /// </summary>
        public IEnumerable<KeyValuePair<string, object?>> TagObjects
        {
#if ALLOW_PARTIALLY_TRUSTED_CALLERS
        [System.Security.SecuritySafeCriticalAttribute]
#endif
            get => _tags ?? s_emptyTagObjects;
        }

        /// <summary>
        /// Events is the list of all <see cref="ActivityEvent" /> objects attached to this Activity object.
        /// If there is not any <see cref="ActivityEvent" /> object attached to the Activity object, Events will return empty list.
        /// </summary>
        public IEnumerable<ActivityEvent> Events
        {
            get => _events ?? s_emptyEvents;
        }

        /// <summary>
        /// Links is the list of all <see cref="ActivityLink" /> objects attached to this Activity object.
        /// If there is no any <see cref="ActivityLink" /> object attached to the Activity object, Links will return empty list.
        /// </summary>
        public IEnumerable<ActivityLink> Links
        {
            get => _links ?? s_emptyLinks;
        }

        /// <summary>
        /// Baggage is string-string key-value pairs that represent information that will
        /// be passed along to children of this activity.   Baggage is serialized
        /// when requests leave the process (along with the ID).   Typically Baggage is
        /// used to do fine-grained control over logging of the activity and any children.
        /// In general, if you are not using the data at runtime, you should be using Tags
        /// instead.
        /// </summary>
        public IEnumerable<KeyValuePair<string, string?>> Baggage
        {
            get
            {
                for (Activity? activity = this; activity != null; activity = activity.Parent)
                {
                    if (activity._baggage != null)
                    {
                        return Iterate(activity);
                    }
                }

                return s_emptyBaggageTags;

                static IEnumerable<KeyValuePair<string, string?>> Iterate(Activity? activity)
                {
                    Debug.Assert(activity != null);
                    do
                    {
                        if (activity._baggage != null)
                        {
                            for (LinkedListNode<KeyValuePair<string, string?>>? current = activity._baggage.First; current != null; current = current.Next)
                            {
                                yield return current.Value;
                            }
                        }

                        activity = activity.Parent;
                    } while (activity != null);
                }
            }
        }

        /// <summary>
        /// Returns the value of the key-value pair added to the activity with <see cref="AddBaggage(string, string)"/>.
        /// Returns null if that key does not exist.
        /// </summary>
        public string? GetBaggageItem(string key)
        {
            foreach (KeyValuePair<string, string?> keyValue in Baggage)
                if (key == keyValue.Key)
                    return keyValue.Value;
            return null;
        }

        /* Constructors  Builder methods */

        /// <summary>
        /// Note that Activity has a 'builder' pattern, where you call the constructor, a number of 'Set*' and 'Add*' APIs and then
        /// call <see cref="Start"/> to build the activity.  You MUST call <see cref="Start"/> before using it.
        /// </summary>
        /// <param name="operationName">Operation's name <see cref="OperationName"/></param>
        public Activity(string operationName)
        {
            Source = s_defaultSource;
            // Allow data by default in the constructor to keep the compatability.
            IsAllDataRequested = true;

            if (string.IsNullOrEmpty(operationName))
            {
                NotifyError(new ArgumentException(SR.OperationNameInvalid));
            }

            OperationName = operationName;
        }

        /// <summary>
        /// Update the Activity to have a tag with an additional 'key' and value 'value'.
        /// This shows up in the <see cref="Tags"/>  enumeration. It is meant for information that
        /// is useful to log but not needed for runtime control (for the latter, <see cref="Baggage"/>)
        /// </summary>
        /// <returns>'this' for convenient chaining</returns>
        /// <param name="key">The tag key name</param>
        /// <param name="value">The tag value mapped to the input key</param>
        public Activity AddTag(string key, string? value) => AddTag(key, (object?) value);

        /// <summary>
        /// Update the Activity to have a tag with an additional 'key' and value 'value'.
        /// This shows up in the <see cref="TagObjects"/> enumeration. It is meant for information that
        /// is useful to log but not needed for runtime control (for the latter, <see cref="Baggage"/>)
        /// </summary>
        /// <returns>'this' for convenient chaining</returns>
        /// <param name="key">The tag key name</param>
        /// <param name="value">The tag value mapped to the input key</param>
        public Activity AddTag(string key, object? value)
        {
            KeyValuePair<string, object?> kvp = new KeyValuePair<string, object?>(key, value);

            if (_tags != null || Interlocked.CompareExchange(ref _tags, new TagsLinkedList(kvp), null) != null)
            {
                _tags.Add(kvp);
            }

            return this;
        }

        /// <summary>
        /// Add or update the Activity tag with the input key and value.
        /// If the input value is null
        ///     - if the collection has any tag with the same key, then this tag will get removed from the collection.
        ///     - otherwise, nothing will happen and the collection will not change.
        /// If the input value is not null
        ///     - if the collection has any tag with the same key, then the value mapped to this key will get updated with the new input value.
        ///     - otherwise, the key and value will get added as a new tag to the collection.
        /// </summary>
        /// <param name="key">The tag key name</param>
        /// <param name="value">The tag value mapped to the input key</param>
        /// <returns>'this' for convenient chaining</returns>
        public Activity SetTag(string key, object value)
        {
            KeyValuePair<string, object?> kvp = new KeyValuePair<string, object?>(key, value);

            if (_tags != null || Interlocked.CompareExchange(ref _tags, new TagsLinkedList(kvp, set: true), null) != null)
            {
                _tags.Set(kvp);
            }

            return this;
        }

        /// <summary>
        /// Add <see cref="ActivityEvent" /> object to the <see cref="Events" /> list.
        /// </summary>
        /// <param name="e"> object of <see cref="ActivityEvent"/> to add to the attached events list.</param>
        /// <returns>'this' for convenient chaining</returns>
        public Activity AddEvent(ActivityEvent e)
        {
            if (_events != null || Interlocked.CompareExchange(ref _events, new LinkedList<ActivityEvent>(e), null) != null)
            {
                _events.Add(e);
            }

            return this;
        }

        /// <summary>
        /// Update the Activity to have baggage with an additional 'key' and value 'value'.
        /// This shows up in the <see cref="Baggage"/> enumeration as well as the <see cref="GetBaggageItem(string)"/>
        /// method.
        /// Baggage is meant for information that is needed for runtime control.   For information
        /// that is simply useful to show up in the log with the activity use <see cref="Tags"/>.
        /// Returns 'this' for convenient chaining.
        /// </summary>
        /// <returns>'this' for convenient chaining</returns>
        public Activity AddBaggage(string key, string? value)
        {
            KeyValuePair<string, string?> kvp = new KeyValuePair<string, string?>(key, value);

            if (_baggage != null || Interlocked.CompareExchange(ref _baggage, new LinkedList<KeyValuePair<string, string?>>(kvp), null) != null)
            {
                _baggage.Add(kvp);
            }

            return this;
        }

        /// <summary>
        /// Updates the Activity To indicate that the activity with ID <paramref name="parentId"/>
        /// caused this activity.   This is intended to be used only at 'boundary'
        /// scenarios where an activity from another process logically started
        /// this activity. The Parent ID shows up the Tags (as well as the ParentID
        /// property), and can be used to reconstruct the causal tree.
        /// Returns 'this' for convenient chaining.
        /// </summary>
        /// <param name="parentId">The id of the parent operation.</param>
        public Activity SetParentId(string parentId)
        {
            if (Parent != null)
            {
                NotifyError(new InvalidOperationException(SR.SetParentIdOnActivityWithParent));
            }
            else if (ParentId != null || _parentSpanId != null)
            {
                NotifyError(new InvalidOperationException(SR.ParentIdAlreadySet));
            }
            else if (string.IsNullOrEmpty(parentId))
            {
                NotifyError(new ArgumentException(SR.ParentIdInvalid));
            }
            else
            {
                _parentId = parentId;
            }
            return this;
        }

        /// <summary>
        /// Set the parent ID using the W3C convention using a TraceId and a SpanId. This
        /// constructor has the advantage that no string manipulation is needed to set the ID.
        /// </summary>
        public Activity SetParentId(ActivityTraceId traceId, ActivitySpanId spanId, ActivityTraceFlags activityTraceFlags = ActivityTraceFlags.None)
        {
            if (Parent != null)
            {
                NotifyError(new InvalidOperationException(SR.SetParentIdOnActivityWithParent));
            }
            else if (ParentId != null || _parentSpanId != null)
            {
                NotifyError(new InvalidOperationException(SR.ParentIdAlreadySet));
            }
            else
            {
                _traceId = traceId.ToHexString();     // The child will share the parent's traceId.
                _parentSpanId = spanId.ToHexString();
                ActivityTraceFlags = activityTraceFlags;
            }
            return this;
        }

        /// <summary>
        /// Update the Activity to set start time
        /// </summary>
        /// <param name="startTimeUtc">Activity start time in UTC (Greenwich Mean Time)</param>
        /// <returns>'this' for convenient chaining</returns>
        public Activity SetStartTime(DateTime startTimeUtc)
        {
            if (startTimeUtc.Kind != DateTimeKind.Utc)
            {
                NotifyError(new InvalidOperationException(SR.StartTimeNotUtc));
            }
            else
            {
                StartTimeUtc = startTimeUtc;
            }
            return this;
        }

        /// <summary>
        /// Update the Activity to set <see cref="Duration"/>
        /// as a difference between <see cref="StartTimeUtc"/>
        /// and <paramref name="endTimeUtc"/>.
        /// </summary>
        /// <param name="endTimeUtc">Activity stop time in UTC (Greenwich Mean Time)</param>
        /// <returns>'this' for convenient chaining</returns>
        public Activity SetEndTime(DateTime endTimeUtc)
        {
            if (endTimeUtc.Kind != DateTimeKind.Utc)
            {
                NotifyError(new InvalidOperationException(SR.EndTimeNotUtc));
            }
            else
            {
                Duration = endTimeUtc - StartTimeUtc;
                if (Duration.Ticks <= 0)
                    Duration = new TimeSpan(1); // We want Duration of 0 to mean  'EndTime not set)
            }
            return this;
        }

        /// <summary>
        /// Get the context of the activity. Context becomes valid only if the activity has been started.
        /// otherwise will default context.
        /// </summary>
        public ActivityContext Context => new ActivityContext(TraceId, SpanId, ActivityTraceFlags, TraceStateString);

        /// <summary>
        /// Starts activity
        /// <list type="bullet">
        /// <item>Sets <see cref="Parent"/> to hold <see cref="Current"/>.</item>
        /// <item>Sets <see cref="Current"/> to this activity.</item>
        /// <item>If <see cref="StartTimeUtc"/> was not set previously, sets it to <see cref="DateTime.UtcNow"/>.</item>
        /// <item>Generates a unique <see cref="Id"/> for this activity.</item>
        /// </list>
        /// Use <see cref="DiagnosticSource.StartActivity(Activity, object)"/> to start activity and write start event.
        /// </summary>
        /// <seealso cref="DiagnosticSource.StartActivity(Activity, object)"/>
        /// <seealso cref="SetStartTime(DateTime)"/>
        public Activity Start()
        {
            // Has the ID already been set (have we called Start()).
            if (_id != null || _spanId != null)
            {
                NotifyError(new InvalidOperationException(SR.ActivityStartAlreadyStarted));
            }
            else
            {
                if (_parentId == null && _parentSpanId is null)
                {
                    Activity? parent = Current;
                    if (parent != null)
                    {
                        // The parent change should not form a loop.   We are actually guaranteed this because
                        // 1. Un-started activities can't be 'Current' (thus can't be 'parent'), we throw if you try.
                        // 2. All started activities have a finite parent change (by inductive reasoning).
                        Parent = parent;
                    }
                }

                if (StartTimeUtc == default)
                    StartTimeUtc = GetUtcNow();

                if (IdFormat == ActivityIdFormat.Unknown)
                {
                    // Figure out what format to use.
                    IdFormat =
                        ForceDefaultIdFormat ? DefaultIdFormat :
                        Parent != null ? Parent.IdFormat :
                        _parentSpanId != null ? ActivityIdFormat.W3C :
                        _parentId == null ? DefaultIdFormat :
                        IsW3CId(_parentId) ? ActivityIdFormat.W3C :
                        ActivityIdFormat.Hierarchical;
                }

                // Generate the ID in the appropriate format.
                if (IdFormat == ActivityIdFormat.W3C)
                    GenerateW3CId();
                else
                    _id = GenerateHierarchicalId();

                SetCurrent(this);

                Source.NotifyActivityStart(this);
            }
            return this;
        }

        /// <summary>
        /// Stops activity: sets <see cref="Current"/> to <see cref="Parent"/>.
        /// If end time was not set previously, sets <see cref="Duration"/> as a difference between <see cref="DateTime.UtcNow"/> and <see cref="StartTimeUtc"/>
        /// Use <see cref="DiagnosticSource.StopActivity(Activity, object)"/>  to stop activity and write stop event.
        /// </summary>
        /// <seealso cref="DiagnosticSource.StopActivity(Activity, object)"/>
        /// <seealso cref="SetEndTime(DateTime)"/>
        public void Stop()
        {
            if (Id == null)
            {
                NotifyError(new InvalidOperationException(SR.ActivityNotStarted));
                return;
            }

            if (!IsFinished)
            {
                IsFinished = true;

                if (Duration == TimeSpan.Zero)
                {
                    SetEndTime(GetUtcNow());
                }

                Source.NotifyActivityStop(this);

                SetCurrent(Parent);
            }
        }

        /* W3C support functionality (see https://w3c.github.io/trace-context) */

        /// <summary>
        /// Holds the W3C 'tracestate' header as a string.
        ///
        /// Tracestate is intended to carry information supplemental to trace identity contained
        /// in traceparent. List of key value pairs carried by tracestate convey information
        /// about request position in multiple distributed tracing graphs. It is typically used
        /// by distributed tracing systems and should not be used as a general purpose baggage
        /// as this use may break correlation of a distributed trace.
        ///
        /// Logically it is just a kind of baggage (if flows just like baggage), but because
        /// it is expected to be special cased (it has its own HTTP header), it is more
        /// convenient/efficient if it is not lumped in with other baggage.
        /// </summary>
        public string? TraceStateString
        {
            get
            {
                for (Activity? activity = this; activity != null; activity = activity.Parent)
                {
                    string? val = activity._traceState;
                    if (val != null)
                        return val;
                }
                return null;
            }
            set
            {
                _traceState = value;
            }
        }

        /// <summary>
        /// If the Activity has the W3C format, this returns the ID for the SPAN part of the Id.
        /// Otherwise it returns a zero SpanId.
        /// </summary>
        public ActivitySpanId SpanId
        {
#if ALLOW_PARTIALLY_TRUSTED_CALLERS
            [System.Security.SecuritySafeCriticalAttribute]
#endif
            get
            {
                if (_spanId is null)
                {
                    if (_id != null && IdFormat == ActivityIdFormat.W3C)
                    {
                        ActivitySpanId activitySpanId = ActivitySpanId.CreateFromString(_id.AsSpan(36, 16));
                        string spanId = activitySpanId.ToHexString();

                        Interlocked.CompareExchange(ref _spanId, spanId, null);
                    }
                }
                return new ActivitySpanId(_spanId);
            }
        }

        /// <summary>
        /// If the Activity has the W3C format, this returns the ID for the TraceId part of the Id.
        /// Otherwise it returns a zero TraceId.
        /// </summary>
        public ActivityTraceId TraceId
        {
            get
            {
                if (_traceId is null)
                {
                    TrySetTraceIdFromParent();
                }

                return new ActivityTraceId(_traceId);
            }
        }

        /// <summary>
        /// True if the W3CIdFlags.Recorded flag is set.
        /// </summary>
        public bool Recorded { get => (ActivityTraceFlags & ActivityTraceFlags.Recorded) != 0; }

        /// <summary>
        /// Indicate if the this Activity object should be populated with all the propagation info and also all other
        /// properties such as Links, Tags, and Events.
        /// </summary>
        public bool IsAllDataRequested { get; set;}

        /// <summary>
        /// Return the flags (defined by the W3C ID specification) associated with the activity.
        /// </summary>
        public ActivityTraceFlags ActivityTraceFlags
        {
            get
            {
                if (!W3CIdFlagsSet)
                {
                    TrySetTraceFlagsFromParent();
                }
                return (ActivityTraceFlags)((~ActivityTraceFlagsIsSet) & _w3CIdFlags);
            }
            set
            {
                _w3CIdFlags = (byte)(ActivityTraceFlagsIsSet | (byte)value);
            }
        }

        /// <summary>
        /// If the parent Activity ID has the W3C format, this returns the ID for the SpanId part of the ParentId.
        /// Otherwise it returns a zero SpanId.
        /// </summary>
        public ActivitySpanId ParentSpanId
        {
#if ALLOW_PARTIALLY_TRUSTED_CALLERS
            [System.Security.SecuritySafeCriticalAttribute]
#endif
            get
            {
                if (_parentSpanId is null)
                {
                    string? parentSpanId = null;
                    if (_parentId != null && IsW3CId(_parentId))
                    {
                        try
                        {
                            parentSpanId = ActivitySpanId.CreateFromString(_parentId.AsSpan(36, 16)).ToHexString();
                        }
                        catch { }
                    }
                    else if (Parent != null && Parent.IdFormat == ActivityIdFormat.W3C)
                    {
                        parentSpanId = Parent.SpanId.ToHexString();
                    }

                    if (parentSpanId != null)
                    {
                        Interlocked.CompareExchange(ref _parentSpanId, parentSpanId, null);
                    }
                }
                return new ActivitySpanId(_parentSpanId);
            }
        }

        /* static state (configuration) */
        /// <summary>
        /// Activity tries to use the same format for IDs as its parent.
        /// However if the activity has no parent, it has to do something.
        /// This determines the default format we use.
        /// </summary>
        public static ActivityIdFormat DefaultIdFormat
        {
            get
            {
                if (s_defaultIdFormat == ActivityIdFormat.Unknown)
                {
#if W3C_DEFAULT_ID_FORMAT
                    s_defaultIdFormat = LocalAppContextSwitches.DefaultActivityIdFormatIsHierarchial ? ActivityIdFormat.Hierarchical : ActivityIdFormat.W3C;
#else
                    s_defaultIdFormat = ActivityIdFormat.Hierarchical;
#endif // W3C_DEFAULT_ID_FORMAT
                }
                return s_defaultIdFormat;
            }
            set
            {
                if (!(ActivityIdFormat.Hierarchical <= value && value <= ActivityIdFormat.W3C))
                    throw new ArgumentException(SR.ActivityIdFormatInvalid);
                s_defaultIdFormat = value;
            }
        }

        /// <summary>
        /// Sets IdFormat on the Activity before it is started.  It takes precedence over
        /// Parent.IdFormat, ParentId format, DefaultIdFormat and ForceDefaultIdFormat.
        /// </summary>
        public Activity SetIdFormat(ActivityIdFormat format)
        {
            if (_id != null || _spanId != null)
            {
                NotifyError(new InvalidOperationException(SR.SetFormatOnStartedActivity));
            }
            else
            {
                IdFormat = format;
            }
            return this;
        }

        /// <summary>
        /// Returns true if 'id' has the format of a WC3 id see https://w3c.github.io/trace-context
        /// </summary>
        private static bool IsW3CId(string id)
        {
            // A W3CId is
            //  * 2 hex chars Version (ff is invalid)
            //  * 1 char - char
            //  * 32 hex chars traceId
            //  * 1 char - char
            //  * 16 hex chars spanId
            //  * 1 char - char
            //  * 2 hex chars flags
            //  = 55 chars (see https://w3c.github.io/trace-context)
            // The version (00-fe) is used to indicate that this is a WC3 ID.
            return id.Length == 55 &&
                   ('0' <= id[0] && id[0] <= '9' || 'a' <= id[0] && id[0] <= 'f') &&
                   ('0' <= id[1] && id[1] <= '9' || 'a' <= id[1] && id[1] <= 'e');
        }

#if ALLOW_PARTIALLY_TRUSTED_CALLERS
        [System.Security.SecuritySafeCriticalAttribute]
#endif
        internal static bool TryConvertIdToContext(string traceParent, string? traceState, out ActivityContext context)
        {
            context = default;
            if (!IsW3CId(traceParent))
            {
                return false;
            }

            ReadOnlySpan<char> traceIdSpan = traceParent.AsSpan(3,  32);
            ReadOnlySpan<char> spanIdSpan  = traceParent.AsSpan(36, 16);

            if (!ActivityTraceId.IsLowerCaseHexAndNotAllZeros(traceIdSpan) || !ActivityTraceId.IsLowerCaseHexAndNotAllZeros(spanIdSpan) ||
                !HexConverter.IsHexLowerChar(traceParent[53]) || !HexConverter.IsHexLowerChar(traceParent[54]))
            {
                return false;
            }

            context = new ActivityContext(
                            new ActivityTraceId(traceIdSpan.ToString()),
                            new ActivitySpanId(spanIdSpan.ToString()),
                            (ActivityTraceFlags) ActivityTraceId.HexByteFromChars(traceParent[53], traceParent[54]),
                            traceState);

            return true;
        }

        /// <summary>
        /// Dispose will stop the Activity if it is already started and notify any event listeners. Nothing will happen otherwise.
        /// </summary>
        public void Dispose()
        {
            if (!IsFinished)
            {
                Stop();
            }

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {

        }

        /// <summary>
        /// SetCustomProperty allow attaching any custom object to this Activity object.
        /// If the property name was previously associated with other object, SetCustomProperty will update to use the new propert value instead.
        /// </summary>
        /// <param name="propertyName"> The name to associate the value with.<see cref="OperationName"/></param>
        /// <param name="propertyValue">The object to attach and map to the property name.</param>
        public void SetCustomProperty(string propertyName, object? propertyValue)
        {
            if (_customProperties == null)
            {
                Interlocked.CompareExchange(ref _customProperties, new ConcurrentDictionary<string, object>(), null);
            }

            if (propertyValue == null)
            {
                _customProperties.TryRemove(propertyName, out object _);
            }
            else
            {
                _customProperties[propertyName] = propertyValue!;
            }
        }

        /// <summary>
        /// GetCustomProperty retrieve previously attached object mapped to the property name.
        /// </summary>
        /// <param name="propertyName"> The name to get the associated object with.</param>
        /// <returns>The object mapped to the property name. Or null if there is no mapping previously done with this property name.</returns>
        public object? GetCustomProperty(string propertyName)
        {
            // We don't check null name here as the dictionary is performing this check anyway.

            if (_customProperties == null)
            {
                return null;
            }

            return _customProperties.TryGetValue(propertyName, out object? o) ? o! : null;
        }

        internal static Activity CreateAndStart(ActivitySource source, string name, ActivityKind kind, string? parentId, ActivityContext parentContext,
                                                IEnumerable<KeyValuePair<string, object?>>? tags, IEnumerable<ActivityLink>? links,
                                                DateTimeOffset startTime, ActivityTagsCollection? samplerTags, ActivitySamplingResult request)
        {
            Activity activity = new Activity(name);

            activity.Source = source;
            activity.Kind = kind;

            if (parentId != null)
            {
                activity._parentId = parentId;
            }
            else if (parentContext != default)
            {
                activity._traceId = parentContext.TraceId.ToString();
                activity._parentSpanId = parentContext.SpanId.ToString();
                activity.ActivityTraceFlags = parentContext.TraceFlags;
                activity._traceState = parentContext.TraceState;
            }
            else
            {
                Activity? parent = Current;
                if (parent != null)
                {
                    // The parent change should not form a loop. We are actually guaranteed this because
                    // 1. Un-started activities can't be 'Current' (thus can't be 'parent'), we throw if you try.
                    // 2. All started activities have a finite parent change (by inductive reasoning).
                    activity.Parent = parent;
                }
            }

            activity.IdFormat =
                ForceDefaultIdFormat ? DefaultIdFormat :
                activity.Parent != null ? activity.Parent.IdFormat :
                activity._parentSpanId != null ? ActivityIdFormat.W3C :
                activity._parentId == null ? DefaultIdFormat :
                IsW3CId(activity._parentId) ? ActivityIdFormat.W3C :
                ActivityIdFormat.Hierarchical;

            if (activity.IdFormat == ActivityIdFormat.W3C)
                activity.GenerateW3CId();
            else
                activity._id = activity.GenerateHierarchicalId();

            if (links != null)
            {
                using (IEnumerator<ActivityLink> enumerator = links.GetEnumerator())
                {
                    if (enumerator.MoveNext())
                    {
                        activity._links = new LinkedList<ActivityLink>(enumerator);
                    }
                }
            }

            if (tags != null)
            {
                using (IEnumerator<KeyValuePair<string, object?>> enumerator = tags.GetEnumerator())
                {
                    if (enumerator.MoveNext())
                    {
                        activity._tags = new TagsLinkedList(enumerator);
                    }
                }
            }

            if (samplerTags != null)
            {
                if (activity._tags == null)
                {
                    activity._tags = new TagsLinkedList(samplerTags!);
                }
                else
                {
                    activity._tags.Add(samplerTags!);
                }
            }

            activity.StartTimeUtc = startTime == default ? DateTime.UtcNow : startTime.UtcDateTime;

            activity.IsAllDataRequested = request == ActivitySamplingResult.AllData || request == ActivitySamplingResult.AllDataAndRecorded;

            if (request == ActivitySamplingResult.AllDataAndRecorded)
            {
                activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
            }

            SetCurrent(activity);

            return activity;
        }

        /// <summary>
        /// Set the ID (lazily, avoiding strings if possible) to a W3C ID (using the
        /// traceId from the parent if possible
        /// </summary>
        private void GenerateW3CId()
        {
            // Called from .Start()

            // Get the TraceId from the parent or make a new one.
            if (_traceId is null)
            {
                if (!TrySetTraceIdFromParent())
                {
                    _traceId = ActivityTraceId.CreateRandom().ToHexString();
                }
            }

            if (!W3CIdFlagsSet)
            {
                TrySetTraceFlagsFromParent();
            }

            // Create a new SpanID.

            _spanId = ActivitySpanId.CreateRandom().ToHexString();
        }

        private static void NotifyError(Exception exception)
        {
            // Throw and catch the exception.  This lets it be seen by the debugger
            // ETW, and other monitoring tools.   However we immediately swallow the
            // exception.   We may wish in the future to allow users to hook this
            // in other useful ways but for now we simply swallow the exceptions.
            try
            {
                throw exception;
            }
            catch { }
        }

        /// <summary>
        /// Returns a new ID using the Hierarchical Id
        /// </summary>
        private string GenerateHierarchicalId()
        {
            // Called from .Start()
            string ret;
            if (Parent != null)
            {
                // Normal start within the process
                Debug.Assert(!string.IsNullOrEmpty(Parent.Id));
                ret = AppendSuffix(Parent.Id, Interlocked.Increment(ref Parent._currentChildId).ToString(), '.');
            }
            else if (ParentId != null)
            {
                // Start from outside the process (e.g. incoming HTTP)
                Debug.Assert(ParentId.Length != 0);

                //sanitize external RequestId as it may not be hierarchical.
                //we cannot update ParentId, we must let it be logged exactly as it was passed.
                string parentId = ParentId[0] == '|' ? ParentId : '|' + ParentId;

                char lastChar = parentId[parentId.Length - 1];
                if (lastChar != '.' && lastChar != '_')
                {
                    parentId += '.';
                }

                ret = AppendSuffix(parentId, Interlocked.Increment(ref s_currentRootId).ToString("x"), '_');
            }
            else
            {
                // A Root Activity (no parent).
                ret = GenerateRootId();
            }
            // Useful place to place a conditional breakpoint.
            return ret;
        }

        private string GetRootId(string id)
        {
            // If this is a W3C ID it has the format Version2-TraceId32-SpanId16-Flags2
            // and the root ID is the TraceId.
            if (IdFormat == ActivityIdFormat.W3C)
                return id.Substring(3, 32);

            //id MAY start with '|' and contain '.'. We return substring between them
            //ParentId MAY NOT have hierarchical structure and we don't know if initially rootId was started with '|',
            //so we must NOT include first '|' to allow mixed hierarchical and non-hierarchical request id scenarios
            int rootEnd = id.IndexOf('.');
            if (rootEnd < 0)
                rootEnd = id.Length;
            int rootStart = id[0] == '|' ? 1 : 0;
            return id.Substring(rootStart, rootEnd - rootStart);
        }

        private string AppendSuffix(string parentId, string suffix, char delimiter)
        {
#if DEBUG
            suffix = OperationName.Replace('.', '-') + "-" + suffix;
#endif
            if (parentId.Length + suffix.Length < RequestIdMaxLength)
                return parentId + suffix + delimiter;

            //Id overflow:
            //find position in RequestId to trim
            int trimPosition = RequestIdMaxLength - 9; // overflow suffix + delimiter length is 9
            while (trimPosition > 1)
            {
                if (parentId[trimPosition - 1] == '.' || parentId[trimPosition - 1] == '_')
                    break;
                trimPosition--;
            }

            //ParentId is not valid Request-Id, let's generate proper one.
            if (trimPosition == 1)
                return GenerateRootId();

            //generate overflow suffix
            string overflowSuffix = ((int)GetRandomNumber()).ToString("x8");
            return parentId.Substring(0, trimPosition) + overflowSuffix + '#';
        }
#if ALLOW_PARTIALLY_TRUSTED_CALLERS
        [System.Security.SecuritySafeCriticalAttribute]
#endif
        private static unsafe long GetRandomNumber()
        {
            // Use the first 8 bytes of the GUID as a random number.
            Guid g = Guid.NewGuid();
            return *((long*)&g);
        }

        private static bool ValidateSetCurrent(Activity? activity)
        {
            bool canSet = activity == null || (activity.Id != null && !activity.IsFinished);
            if (!canSet)
            {
                NotifyError(new InvalidOperationException(SR.ActivityNotRunning));
            }

            return canSet;
        }

#if ALLOW_PARTIALLY_TRUSTED_CALLERS
        [System.Security.SecuritySafeCriticalAttribute]
#endif
        private bool TrySetTraceIdFromParent()
        {
            Debug.Assert(_traceId is null);

            if (Parent != null && Parent.IdFormat == ActivityIdFormat.W3C)
            {
                _traceId = Parent.TraceId.ToHexString();
            }
            else if (_parentId != null && IsW3CId(_parentId))
            {
                try
                {
                    _traceId = ActivityTraceId.CreateFromString(_parentId.AsSpan(3, 32)).ToHexString();
                }
                catch
                {
                }
            }

            return _traceId != null;
        }

#if ALLOW_PARTIALLY_TRUSTED_CALLERS
        [System.Security.SecuritySafeCriticalAttribute]
#endif
        private void TrySetTraceFlagsFromParent()
        {
            Debug.Assert(!W3CIdFlagsSet);

            if (!W3CIdFlagsSet)
            {
                if (Parent != null)
                {
                    ActivityTraceFlags = Parent.ActivityTraceFlags;
                }
                else if (_parentId != null && IsW3CId(_parentId))
                {
                    if (HexConverter.IsHexLowerChar(_parentId[53]) && HexConverter.IsHexLowerChar(_parentId[54]))
                    {
                        _w3CIdFlags = (byte)(ActivityTraceId.HexByteFromChars(_parentId[53], _parentId[54]) | ActivityTraceFlagsIsSet);
                    }
                    else
                    {
                        _w3CIdFlags = ActivityTraceFlagsIsSet;
                    }
                }
            }
        }

        private bool W3CIdFlagsSet
        {
            get => (_w3CIdFlags & ActivityTraceFlagsIsSet) != 0;
        }

        private bool IsFinished
        {
            get => (_state & State.IsFinished) != 0;
            set
            {
                if (value)
                {
                    _state |= State.IsFinished;
                }
                else
                {
                    _state &= ~State.IsFinished;
                }
            }
        }

        /// <summary>
        /// Returns the format for the ID.
        /// </summary>
        public ActivityIdFormat IdFormat
        {
            get => (ActivityIdFormat)(_state & State.FormatFlags);
            private set => _state = (_state & ~State.FormatFlags) | (State)((byte)value & (byte)State.FormatFlags);
        }

        private partial class LinkedListNode<T>
        {
            public LinkedListNode(T value) => Value = value;
            public T Value;
            public LinkedListNode<T>? Next;
        }

        // We are not using the public LinkedList<T> because we need to ensure thread safety operation on the list.
        private class LinkedList<T> : IEnumerable<T>
        {
            private LinkedListNode<T> _first;
            private LinkedListNode<T> _last;

            public LinkedList(T firstValue) => _last = _first = new LinkedListNode<T>(firstValue);

            public LinkedList(IEnumerator<T> e)
            {
                _last = _first = new LinkedListNode<T>(e.Current);

                while (e.MoveNext())
                {
                    _last.Next = new LinkedListNode<T>(e.Current);
                    _last = _last.Next;
                }
            }

            public LinkedListNode<T> First => _first;

            public void Add(T value)
            {
                LinkedListNode<T> newNode = new LinkedListNode<T>(value);

                lock (_first)
                {
                    _last.Next = newNode;
                    _last = newNode;
                }
            }

            public Enumerator<T> GetEnumerator() => new Enumerator<T>(_first);
            IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator<T>(_first);
            IEnumerator IEnumerable.GetEnumerator() => new Enumerator<T>(_first);
        }

        private class TagsLinkedList : IEnumerable<KeyValuePair<string, object?>>
        {
            private LinkedListNode<KeyValuePair<string, object?>>? _first;
            private LinkedListNode<KeyValuePair<string, object?>>? _last;

            public TagsLinkedList(KeyValuePair<string, object?> firstValue, bool set = false) => _last = _first = ((set && firstValue.Value == null) ? null : new LinkedListNode<KeyValuePair<string, object?>>(firstValue));

            public TagsLinkedList(IEnumerator<KeyValuePair<string, object?>> e)
            {
                _last = _first = new LinkedListNode<KeyValuePair<string, object?>>(e.Current);

                while (e.MoveNext())
                {
                    _last.Next = new LinkedListNode<KeyValuePair<string, object?>>(e.Current);
                    _last = _last.Next;
                }
            }

            public TagsLinkedList(IEnumerable<KeyValuePair<string, object?>> list) => Add(list);

            // Add doesn't take the lock because it is called from the Activity creation before sharing the activity object to the caller.
            public void Add(IEnumerable<KeyValuePair<string, object?>> list)
            {
                IEnumerator<KeyValuePair<string, object?>> e = list.GetEnumerator();
                if (!e.MoveNext())
                {
                    return;
                }

                if (_first == null)
                {
                    _last = _first = new LinkedListNode<KeyValuePair<string, object?>>(e.Current);
                }
                else
                {
                    _last!.Next = new LinkedListNode<KeyValuePair<string, object?>>(e.Current);
                    _last = _last.Next;
                }

                while (e.MoveNext())
                {
                    _last.Next = new LinkedListNode<KeyValuePair<string, object?>>(e.Current);
                    _last = _last.Next;
                }
            }

            public void Add(KeyValuePair<string, object?> value)
            {
                LinkedListNode<KeyValuePair<string, object?>> newNode = new LinkedListNode<KeyValuePair<string, object?>>(value);

                lock (this)
                {
                    if (_first == null)
                    {
                        _first = _last = newNode;
                        return;
                    }

                    Debug.Assert(_last != null);

                    _last!.Next = newNode;
                    _last = newNode;
                }
            }

            public void Remove(string key)
            {
                if (_first == null)
                {
                    return;
                }

                lock (this)
                {
                    if (_first.Value.Key == key)
                    {
                        _first = _first.Next;
                        return;
                    }

                    LinkedListNode<KeyValuePair<string, object?>> previous = _first;

                    while (previous.Next != null)
                    {
                        if (previous.Next.Value.Key == key)
                        {
                            previous.Next = previous.Next.Next;
                            return;
                        }
                        previous = previous.Next;
                    }
                }
            }

            public void Set(KeyValuePair<string, object?> value)
            {
                if (value.Value == null)
                {
                    Remove(value.Key);
                    return;
                }

                lock (this)
                {
                    LinkedListNode<KeyValuePair<string, object?>>? current = _first;
                    while (current != null)
                    {
                        if (current.Value.Key == value.Key)
                        {
                            current.Value = value;
                            return;
                        }

                        current = current.Next;
                    }

                    LinkedListNode<KeyValuePair<string, object?>> newNode = new LinkedListNode<KeyValuePair<string, object?>>(value);
                    if (_first == null)
                    {
                        _first = _last = newNode;
                        return;
                    }

                    Debug.Assert(_last != null);

                    _last!.Next = newNode;
                    _last = newNode;
                }
            }

            public Enumerator<KeyValuePair<string, object?>> GetEnumerator() => new Enumerator<KeyValuePair<string, object?>>(_first);
            IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator() => new Enumerator<KeyValuePair<string, object?>>(_first);
            IEnumerator IEnumerable.GetEnumerator() => new Enumerator<KeyValuePair<string, object?>>(_first);

            public IEnumerable<KeyValuePair<string, string?>> EnumerateStringValues()
            {
                LinkedListNode<KeyValuePair<string, object?>>? current = _first;

                while (current != null)
                {
                    if (current.Value.Value is string || current.Value.Value == null)
                    {
                        yield return new KeyValuePair<string, string?>(current.Value.Key, (string?)current.Value.Value);
                    }

                    current = current.Next;
                };
            }
        }

        private struct Enumerator<T> : IEnumerator<T>
        {
            private readonly LinkedListNode<T>? _head;
            private LinkedListNode<T>? _nextNode;
            [AllowNull, MaybeNull] private T _currentItem;

            public Enumerator(LinkedListNode<T>? head)
            {
                _nextNode = _head = head;
                _currentItem = default;
            }

            public T Current => _currentItem!;

            object? IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (_nextNode == null)
                {
                    _currentItem = default;
                    return false;
                }

                _currentItem = _nextNode.Value;
                _nextNode = _nextNode.Next;
                return true;
            }

            public void Reset() => _nextNode = _head;

            public void Dispose()
            {
            }
        }

        [Flags]
        private enum State : byte
        {
            None = 0,

            FormatUnknown = 0b_0_00000_00,
            FormatHierarchical = 0b_0_00000_01,
            FormatW3C = 0b_0_00000_10,
            FormatFlags = 0b_0_00000_11,

            IsFinished = 0b_1_00000_00,
        }
    }

    /// <summary>
    /// These flags are defined by the W3C standard along with the ID for the activity.
    /// </summary>
    [Flags]
    public enum ActivityTraceFlags
    {
        None = 0b_0_0000000,
        Recorded = 0b_0_0000001, // The Activity (or more likely its parents) has been marked as useful to record
    }

    /// <summary>
    /// The possibilities for the format of the ID
    /// </summary>
    public enum ActivityIdFormat
    {
        Unknown = 0,      // ID format is not known.
        Hierarchical = 1, //|XXXX.XX.X_X ... see https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/ActivityUserGuide.md#id-format
        W3C = 2,          // 00-XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX-XXXXXXXXXXXXXXXX-XX see https://w3c.github.io/trace-context/
    };

    /// <summary>
    /// A TraceId is the format the W3C standard requires for its ID for the entire trace.
    /// It represents 16 binary bytes of information, typically displayed as 32 characters
    /// of Hexadecimal.  A TraceId is a STRUCT, and does contain the 16 bytes of binary information
    /// so there is value in passing it by reference.   It does know how to convert to and
    /// from its Hexadecimal string representation, tries to avoid changing formats until
    /// it has to, and caches the string representation after it was created.
    /// It is mostly useful as an exchange type.
    /// </summary>
#if ALLOW_PARTIALLY_TRUSTED_CALLERS
        [System.Security.SecuritySafeCriticalAttribute]
#endif
    public readonly struct ActivityTraceId : IEquatable<ActivityTraceId>
    {
        private readonly string? _hexString;

        internal ActivityTraceId(string? hexString) => _hexString = hexString;

        /// <summary>
        /// Create a new TraceId with at random number in it (very likely to be unique)
        /// </summary>
        public static ActivityTraceId CreateRandom()
        {
            Span<byte> span = stackalloc byte[sizeof(ulong) * 2];
            SetToRandomBytes(span);
            return CreateFromBytes(span);
        }
        public static ActivityTraceId CreateFromBytes(ReadOnlySpan<byte> idData)
        {
            if (idData.Length != 16)
                throw new ArgumentOutOfRangeException(nameof(idData));

            return new ActivityTraceId(HexConverter.ToString(idData, HexConverter.Casing.Lower));
        }
        public static ActivityTraceId CreateFromUtf8String(ReadOnlySpan<byte> idData) => new ActivityTraceId(idData);

        public static ActivityTraceId CreateFromString(ReadOnlySpan<char> idData)
        {
            if (idData.Length != 32 || !ActivityTraceId.IsLowerCaseHexAndNotAllZeros(idData))
                throw new ArgumentOutOfRangeException(nameof(idData));

            return new ActivityTraceId(idData.ToString());
        }

        /// <summary>
        /// Returns the TraceId as a 32 character hexadecimal string.
        /// </summary>
        public string ToHexString()
        {
            return _hexString ?? "00000000000000000000000000000000";
        }

        /// <summary>
        /// Returns the TraceId as a 32 character hexadecimal string.
        /// </summary>
        public override string ToString() => ToHexString();

        public static bool operator ==(ActivityTraceId traceId1, ActivityTraceId traceId2)
        {
            return traceId1._hexString == traceId2._hexString;
        }
        public static bool operator !=(ActivityTraceId traceId1, ActivityTraceId traceId2)
        {
            return traceId1._hexString != traceId2._hexString;
        }
        public bool Equals(ActivityTraceId traceId)
        {
            return _hexString == traceId._hexString;
        }
        public override bool Equals(object? obj)
        {
            if (obj is ActivityTraceId traceId)
                return _hexString == traceId._hexString;
            return false;
        }
        public override int GetHashCode()
        {
            return ToHexString().GetHashCode();
        }

        /// <summary>
        /// This is exposed as CreateFromUtf8String, but we are modifying fields, so the code needs to be in a constructor.
        /// </summary>
        /// <param name="idData"></param>
        private ActivityTraceId(ReadOnlySpan<byte> idData)
        {
            if (idData.Length != 32)
                throw new ArgumentOutOfRangeException(nameof(idData));

            Span<ulong> span = stackalloc ulong[2];

            if (!Utf8Parser.TryParse(idData.Slice(0, 16), out span[0], out _, 'x'))
            {
                // Invalid Id, use random https://github.com/dotnet/runtime/issues/29859
                _hexString = CreateRandom()._hexString;
                return;
            }

            if (!Utf8Parser.TryParse(idData.Slice(16, 16), out span[1], out _, 'x'))
            {
                // Invalid Id, use random https://github.com/dotnet/runtime/issues/29859
                _hexString = CreateRandom()._hexString;
                return;
            }

            if (BitConverter.IsLittleEndian)
            {
                span[0] = BinaryPrimitives.ReverseEndianness(span[0]);
                span[1] = BinaryPrimitives.ReverseEndianness(span[1]);
            }

            _hexString = HexConverter.ToString(MemoryMarshal.AsBytes(span), HexConverter.Casing.Lower);
        }

        /// <summary>
        /// Copy the bytes of the TraceId (16 total) into the 'destination' span.
        /// </summary>
        public void CopyTo(Span<byte> destination)
        {
            ActivityTraceId.SetSpanFromHexChars(ToHexString().AsSpan(), destination);
        }

        /// <summary>
        /// Sets the bytes in 'outBytes' to be random values.   outBytes.Length must be less than or equal to 16
        /// </summary>
        /// <param name="outBytes"></param>
        internal static unsafe void SetToRandomBytes(Span<byte> outBytes)
        {
            Debug.Assert(outBytes.Length <= sizeof(Guid));     // Guid is 16 bytes, and so is TraceId
            Guid guid = Guid.NewGuid();
            ReadOnlySpan<byte> guidBytes = new ReadOnlySpan<byte>(&guid, sizeof(Guid));
            guidBytes.Slice(0, outBytes.Length).CopyTo(outBytes);
        }

        /// <summary>
        /// Converts 'idData' which is assumed to be HEX Unicode characters to binary
        /// puts it in 'outBytes'
        /// </summary>
        internal static void SetSpanFromHexChars(ReadOnlySpan<char> charData, Span<byte> outBytes)
        {
            Debug.Assert(outBytes.Length * 2 == charData.Length);
            for (int i = 0; i < outBytes.Length; i++)
                outBytes[i] = HexByteFromChars(charData[i * 2], charData[i * 2 + 1]);
        }
        internal static byte HexByteFromChars(char char1, char char2)
        {
            int hi = HexConverter.FromLowerChar(char1);
            int lo = HexConverter.FromLowerChar(char2);
            if ((hi | lo) == 0xFF)
            {
                throw new ArgumentOutOfRangeException("idData");
            }

            return (byte)((hi << 4) | lo);
        }

        internal static bool IsLowerCaseHexAndNotAllZeros(ReadOnlySpan<char> idData)
        {
            // Verify lower-case hex and not all zeros https://w3c.github.io/trace-context/#field-value
            bool isNonZero = false;
            int i = 0;
            for (; i < idData.Length; i++)
            {
                char c = idData[i];
                if (!HexConverter.IsHexLowerChar(c))
                {
                    return false;
                }

                if (c != '0')
                {
                    isNonZero = true;
                }
            }

            return isNonZero;
        }
    }

    /// <summary>
    /// A SpanId is the format the W3C standard requires for its ID for a single span in a trace.
    /// It represents 8 binary bytes of information, typically displayed as 16 characters
    /// of Hexadecimal.  A SpanId is a STRUCT, and does contain the 8 bytes of binary information
    /// so there is value in passing it by reference.  It does know how to convert to and
    /// from its Hexadecimal string representation, tries to avoid changing formats until
    /// it has to, and caches the string representation after it was created.
    /// It is mostly useful as an exchange type.
    /// </summary>
#if ALLOW_PARTIALLY_TRUSTED_CALLERS
        [System.Security.SecuritySafeCriticalAttribute]
#endif
    public readonly struct ActivitySpanId : IEquatable<ActivitySpanId>
    {
        private readonly string? _hexString;

        internal ActivitySpanId(string? hexString) => _hexString = hexString;

        /// <summary>
        /// Create a new SpanId with at random number in it (very likely to be unique)
        /// </summary>
        public static unsafe ActivitySpanId CreateRandom()
        {
            ulong id;
            ActivityTraceId.SetToRandomBytes(new Span<byte>(&id, sizeof(ulong)));
            return new ActivitySpanId(HexConverter.ToString(new ReadOnlySpan<byte>(&id, sizeof(ulong)), HexConverter.Casing.Lower));
        }
        public static ActivitySpanId CreateFromBytes(ReadOnlySpan<byte> idData)
        {
            if (idData.Length != 8)
                throw new ArgumentOutOfRangeException(nameof(idData));

            return new ActivitySpanId(HexConverter.ToString(idData, HexConverter.Casing.Lower));
        }
        public static ActivitySpanId CreateFromUtf8String(ReadOnlySpan<byte> idData) => new ActivitySpanId(idData);

        public static ActivitySpanId CreateFromString(ReadOnlySpan<char> idData)
        {
            if (idData.Length != 16 || !ActivityTraceId.IsLowerCaseHexAndNotAllZeros(idData))
                throw new ArgumentOutOfRangeException(nameof(idData));

            return new ActivitySpanId(idData.ToString());
        }

        /// <summary>
        /// Returns the SpanId as a 16 character hexadecimal string.
        /// </summary>
        /// <returns></returns>
        public string ToHexString()
        {
            return _hexString ?? "0000000000000000";
        }

        /// <summary>
        /// Returns SpanId as a hex string.
        /// </summary>
        public override string ToString() => ToHexString();

        public static bool operator ==(ActivitySpanId spanId1, ActivitySpanId spandId2)
        {
            return spanId1._hexString == spandId2._hexString;
        }
        public static bool operator !=(ActivitySpanId spanId1, ActivitySpanId spandId2)
        {
            return spanId1._hexString != spandId2._hexString;
        }
        public bool Equals(ActivitySpanId spanId)
        {
            return _hexString == spanId._hexString;
        }
        public override bool Equals(object? obj)
        {
            if (obj is ActivitySpanId spanId)
                return _hexString == spanId._hexString;
            return false;
        }
        public override int GetHashCode()
        {
            return ToHexString().GetHashCode();
        }

        private unsafe ActivitySpanId(ReadOnlySpan<byte> idData)
        {
            if (idData.Length != 16)
            {
                throw new ArgumentOutOfRangeException(nameof(idData));
            }

            if (!Utf8Parser.TryParse(idData, out ulong id, out _, 'x'))
            {
                // Invalid Id, use random https://github.com/dotnet/runtime/issues/29859
                _hexString = CreateRandom()._hexString;
                return;
            }

            if (BitConverter.IsLittleEndian)
            {
                id = BinaryPrimitives.ReverseEndianness(id);
            }

            _hexString = HexConverter.ToString(new ReadOnlySpan<byte>(&id, sizeof(ulong)), HexConverter.Casing.Lower);
        }

        /// <summary>
        /// Copy the bytes of the TraceId (8 bytes total) into the 'destination' span.
        /// </summary>
        public void CopyTo(Span<byte> destination)
        {
            ActivityTraceId.SetSpanFromHexChars(ToHexString().AsSpan(), destination);
        }
    }
}
