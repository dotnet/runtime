// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace System.Diagnostics
{
    /// <summary>
    /// DiagnosticSourceEventSource serves two purposes
    ///
    ///   1) It allows debuggers to inject code via Function evaluation. This is the purpose of the
    ///   BreakPointWithDebuggerFuncEval function in the 'OnEventCommand' method. Basically even in
    ///   release code, debuggers can place a breakpoint in this method and then trigger the
    ///   DiagnosticSourceEventSource via ETW. Thus from outside the process you can get a hook that
    ///   is guaranteed to happen BEFORE any DiagnosticSource events (if the process is just starting)
    ///   or as soon as possible afterward if it is on attach.
    ///
    ///   2) It provides a 'bridge' that allows DiagnosticSource messages to be forwarded to EventListers
    ///   or ETW. You can do this by enabling the Microsoft-Diagnostics-DiagnosticSource with the
    ///   'Events' keyword (for diagnostics purposes, you should also turn on the 'Messages' keyword.
    ///
    ///   This EventSource defines a EventSource argument called 'FilterAndPayloadSpecs' that defines
    ///   what DiagnosticSources to enable and what parts of the payload to serialize into the key-value
    ///   list that will be forwarded to the EventSource. If it is empty, values of properties of the
    ///   diagnostic source payload are dumped as strings (using ToString()) and forwarded to the EventSource.
    ///   For what people think of as serializable object strings, primitives this gives you want you want.
    ///   (the value of the property in string form) for what people think of as non-serializable objects
    ///   (e.g. HttpContext) the ToString() method is typically not defined, so you get the Object.ToString()
    ///   implementation that prints the type name. This is useful since this is the information you need
    ///   (the type of the property) to discover the field names so you can create a transform specification
    ///   that will pick off the properties you desire.
    ///
    ///   Once you have the particular values you desire, the implicit payload elements are typically not needed
    ///   anymore and you can prefix the Transform specification with a '-' which suppresses the implicit
    ///   transform (you only get the values of the properties you specifically ask for.
    ///
    ///   Logically a transform specification is simply a fetching specification X.Y.Z along with a name to give
    ///   it in the output (which defaults to the last name in the fetch specification).
    ///
    ///   The FilterAndPayloadSpecs is one long string with the following structures
    ///
    ///   * It is a newline separated list of FILTER_AND_PAYLOAD_SPEC
    ///   * a FILTER_AND_PAYLOAD_SPEC can be
    ///       * EVENT_NAME : TRANSFORM_SPECS
    ///       * EMPTY - turns on all sources with implicit payload elements.
    ///   * an EVENTNAME can be
    ///       * DIAGNOSTIC_SOURCE_NAME / DIAGNOSTIC_EVENT_NAME @ EVENT_SOURCE_EVENTNAME - give the name as well as the EventSource event to log it under.
    ///       * DIAGNOSTIC_SOURCE_NAME / DIAGNOSTIC_EVENT_NAME
    ///       * DIAGNOSTIC_SOURCE_NAME    - which wildcards every event in the Diagnostic source or
    ///       * EMPTY                     - which turns on all sources
    ///     Or it can be "[AS] ACTIVITY_SOURCE_NAME + ACTIVITY_NAME / ACTIVITY_EVENT_NAME - SAMPLING_RESULT"
    ///       * All parts are optional and can be empty string.
    ///       * ACTIVITY_SOURCE_NAME can be "*" to listen to all ActivitySources
    ///       * ACTIVITY_SOURCE_NAME can be empty string which will listen to ActivitySource that create Activities using "new Activity(...)"
    ///       * ACTIVITY_NAME is the activity operation name to filter with.
    ///       * ACTIVITY_EVENT_NAME either "Start" to listen to Activity Start event, or "Stop" to listen to Activity Stop event, or empty string to listen to both Start and Stop Activity events.
    ///       * SAMPLING_RESULT either "Propagate" to create the Activity with PropagationData, or "Record" to create the Activity with AllData, or empty string to create the Activity with AllDataAndRecorded
    ///   * TRANSFORM_SPEC is a semicolon separated list of TRANSFORM_SPEC, which can be
    ///       * - TRANSFORM_SPEC               - the '-' indicates that implicit payload elements should be suppressed
    ///       * VARIABLE_NAME = PROPERTY_SPEC  - indicates that a payload element 'VARIABLE_NAME' is created from PROPERTY_SPEC
    ///       * PROPERTY_SPEC                  - This is a shortcut where VARIABLE_NAME is the LAST property name
    ///   * a PROPERTY_SPEC is basically a list of names separated by '.'
    ///       * PROPERTY_NAME                  - fetches a property from the DiagnosticSource payload object
    ///       * PROPERTY_NAME . PROPERTY NAME  - fetches a sub-property of the object.
    ///
    ///       * *Activity                      - fetches Activity.Current
    ///       * *Enumerate                     - enumerates all the items in an IEnumerable, calls ToString() on them, and joins the
    ///                                          strings in a comma separated list.
    /// Example1:
    ///
    ///    "BridgeTestSource1/TestEvent1:cls_Point_X=cls.Point.X;cls_Point_Y=cls.Point.Y\r\n" +
    ///    "BridgeTestSource2/TestEvent2:-cls.Url"
    ///
    /// This indicates that two events should be turned on, The 'TestEvent1' event in BridgeTestSource1 and the
    /// 'TestEvent2' in BridgeTestSource2. In the first case, because the transform did not begin with a -
    /// any primitive type/string of 'TestEvent1's payload will be serialized into the output. In addition if
    /// there a property of the payload object called 'cls' which in turn has a property 'Point' which in turn
    /// has a property 'X' then that data is also put in the output with the name cls_Point_X. Similarly
    /// if cls.Point.Y exists, then that value will also be put in the output with the name cls_Point_Y.
    ///
    /// For the 'BridgeTestSource2/TestEvent2' event, because the - was specified NO implicit fields will be
    /// generated, but if there is a property call 'cls' which has a property 'Url' then that will be placed in
    /// the output with the name 'Url' (since that was the last property name used and no Variable= clause was
    /// specified.
    ///
    /// Example:
    ///
    ///     "BridgeTestSource1\r\n" +
    ///     "BridgeTestSource2"
    ///
    /// This will enable all events for the BridgeTestSource1 and BridgeTestSource2 sources. Any string/primitive
    /// properties of any of the events will be serialized into the output.
    ///
    /// Example:
    ///
    ///     ""
    ///
    /// This turns on all DiagnosticSources Any string/primitive properties of any of the events will be serialized
    /// into the output. This is not likely to be a good idea as it will be very verbose, but is useful to quickly
    /// discover what is available.
    ///
    /// Example:
    ///     "[AS]*"                      listen to all ActivitySources and all Activities events (Start/Stop). Activities will be created with AllDataAndRecorded sampling.
    ///     "[AS]"                       listen to default ActivitySource and Activities events (Start/Stop) while the Activity is created using "new Activity(...)". Such Activities will be created with AllDataAndRecorded sampling.
    ///     "[AS]MyLibrary/Start"        listen to `MyLibrary` ActivitySource and the 'Start' Activity event. The Activities will be created with AllDataAndRecorded sampling.
    ///     "[AS]MyLibrary/-Propagate"   listen to `MyLibrary` ActivitySource and the 'Start and Stop' Activity events. The Activities will be created with PropagationData sampling.
    ///     "[AS]MyLibrary/Stop-Record"  listen to `MyLibrary` ActivitySource and the 'Stop' Activity event. The Activities will be created with AllData sampling.
    ///     "[AS]*/-"                    listen to all ActivitySources and the Start and Stop Activity events. Activities will be created with AllDataAndRecorded sampling. this equivalent to "[AS]*" too.
    ///     "[AS]*+MyActivity"           listen to all activity sources when creating Activity with the operation name "MyActivity".
    ///
    /// * How data is logged in the EventSource
    ///
    /// By default all data from DiagnosticSources is logged to the DiagnosticEventSource event called 'Event'
    /// which has three fields
    ///
    ///     string SourceName,
    ///     string EventName,
    ///     IEnumerable[KeyValuePair[string, string]] Argument
    ///
    /// However to support start-stop activity tracking, there are six other events that can be used
    ///
    ///     Activity1Start
    ///     Activity1Stop
    ///     Activity2Start
    ///     Activity2Stop
    ///     RecursiveActivity1Start
    ///     RecursiveActivity1Stop
    ///
    /// By using the SourceName/EventName@EventSourceName syntax, you can force particular DiagnosticSource events to
    /// be logged with one of these EventSource events. This is useful because the events above have start-stop semantics
    /// which means that they create activity IDs that are attached to all logging messages between the start and
    /// the stop (see https://blogs.msdn.microsoft.com/vancem/2015/09/14/exploring-eventsource-activity-correlation-and-causation-features/)
    ///
    /// For example the specification
    ///
    ///     "MyDiagnosticSource/RequestStart@Activity1Start\r\n" +
    ///     "MyDiagnosticSource/RequestStop@Activity1Stop\r\n" +
    ///     "MyDiagnosticSource/SecurityStart@Activity2Start\r\n" +
    ///     "MyDiagnosticSource/SecurityStop@Activity2Stop\r\n"
    ///
    /// Defines that RequestStart will be logged with the EventSource Event Activity1Start (and the corresponding stop) which
    /// means that all events caused between these two markers will have an activity ID associated with this start event.
    /// Similarly SecurityStart is mapped to Activity2Start.
    ///
    /// Note you can map many DiagnosticSource events to the same EventSource Event (e.g. Activity1Start). As long as the
    /// activities don't nest, you can reuse the same event name (since the payloads have the DiagnosticSource name which can
    /// disambiguate). However if they nest you need to use another EventSource event because the rules of EventSource
    /// activities state that a start of the same event terminates any existing activity of the same name.
    ///
    /// As its name suggests RecursiveActivity1Start, is marked as recursive and thus can be used when the activity can nest with
    /// itself. This should not be a 'top most' activity because it is not 'self healing' (if you miss a stop, then the
    /// activity NEVER ends).
    ///
    /// See the DiagnosticSourceEventSourceBridgeTest.cs for more explicit examples of using this bridge.
    /// </summary>
    [EventSource(Name = "Microsoft-Diagnostics-DiagnosticSource")]
    // These suppressions can go away with https://github.com/mono/linker/issues/2175
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2113:ReflectionToRequiresUnreferencedCode",
        Justification = "In EventSource, EnsureDescriptorsInitialized's use of GetType preserves methods on Delegate and MulticastDelegate " +
                        "because the nested type OverrideEventProvider's base type EventProvider defines a delegate. " +
                        "This includes Delegate and MulticastDelegate methods which require unreferenced code, but " +
                        "EnsureDescriptorsInitialized does not access these members and is safe to call.")]
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2115:ReflectionToDynamicallyAccessedMembers",
        Justification = "In EventSource, EnsureDescriptorsInitialized's use of GetType preserves methods on Delegate and MulticastDelegate " +
                        "because the nested type OverrideEventProvider's base type EventProvider defines a delegate. " +
                        "This includes Delegate and MulticastDelegate methods which have dynamically accessed members requirements, but " +
                        "EnsureDescriptorsInitialized does not access these members and is safe to call.")]
    internal sealed class DiagnosticSourceEventSource : EventSource
    {
        public static DiagnosticSourceEventSource Log = new DiagnosticSourceEventSource();

        public static class Keywords
        {
            /// <summary>
            /// Indicates diagnostics messages from DiagnosticSourceEventSource should be included.
            /// </summary>
            public const EventKeywords Messages = (EventKeywords)0x1;
            /// <summary>
            /// Indicates that all events from all diagnostic sources should be forwarded to the EventSource using the 'Event' event.
            /// </summary>
            public const EventKeywords Events = (EventKeywords)0x2;

            // Some ETW logic does not support passing arguments to the EventProvider. To get around
            // this in common cases, we define some keywords that basically stand in for particular common arguments
            // That way at least the common cases can be used by everyone (and it also compresses things).
            // We start these keywords at 0x1000. See below for the values these keywords represent
            // Because we want all keywords on to still mean 'dump everything by default' we have another keyword
            // IgnoreShorcutKeywords which must be OFF in order for the shortcuts to work thus the all 1s keyword
            // still means what you expect.
            public const EventKeywords IgnoreShortCutKeywords = (EventKeywords)0x0800;
            public const EventKeywords AspNetCoreHosting = (EventKeywords)0x1000;
            public const EventKeywords EntityFrameworkCoreCommands = (EventKeywords)0x2000;
        };

        // Setting AspNetCoreHosting is like having this in the FilterAndPayloadSpecs string
        // It turns on basic hosting events.
        private readonly string AspNetCoreHostingKeywordValue =
            "Microsoft.AspNetCore/Microsoft.AspNetCore.Hosting.BeginRequest@Activity1Start:-" +
                "httpContext.Request.Method;" +
                "httpContext.Request.Host;" +
                "httpContext.Request.Path;" +
                "httpContext.Request.QueryString" +
            "\n" +
            "Microsoft.AspNetCore/Microsoft.AspNetCore.Hosting.EndRequest@Activity1Stop:-" +
                "httpContext.TraceIdentifier;" +
                "httpContext.Response.StatusCode";

        // Setting EntityFrameworkCoreCommands is like having this in the FilterAndPayloadSpecs string
        // It turns on basic SQL commands.
        private readonly string EntityFrameworkCoreCommandsKeywordValue =
            "Microsoft.EntityFrameworkCore/Microsoft.EntityFrameworkCore.BeforeExecuteCommand@Activity2Start:-" +
                "Command.Connection.DataSource;" +
                "Command.Connection.Database;" +
                "Command.CommandText" +
            "\n" +
            "Microsoft.EntityFrameworkCore/Microsoft.EntityFrameworkCore.AfterExecuteCommand@Activity2Stop:-";

        /// <summary>
        /// Used to send ad-hoc diagnostics to humans.
        /// </summary>
        [Event(1, Keywords = Keywords.Messages)]
        public void Message(string? Message)
        {
            WriteEvent(1, Message);
        }

        /// <summary>
        /// Events from DiagnosticSource can be forwarded to EventSource using this event.
        /// </summary>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Arguments parameter is trimmer safe")]
        [Event(2, Keywords = Keywords.Events)]
        private void Event(string SourceName, string EventName, IEnumerable<KeyValuePair<string, string?>>? Arguments)
        {
            WriteEvent(2, SourceName, EventName, Arguments);
        }

        /// <summary>
        /// This is only used on V4.5 systems that don't have the ability to log KeyValuePairs directly.
        /// It will eventually go away, but we should always reserve the ID for this.
        /// </summary>
        [Event(3, Keywords = Keywords.Events)]
        private void EventJson(string SourceName, string EventName, string ArgmentsJson)
        {
            WriteEvent(3, SourceName, EventName, ArgmentsJson);
        }

        /// <summary>
        /// Used to mark the beginning of an activity
        /// </summary>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Arguments parameter is trimmer safe")]
        [Event(4, Keywords = Keywords.Events)]
        private void Activity1Start(string SourceName, string EventName, IEnumerable<KeyValuePair<string, string?>> Arguments)
        {
            WriteEvent(4, SourceName, EventName, Arguments);
        }

        /// <summary>
        /// Used to mark the end of an activity
        /// </summary>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Arguments parameter is trimmer safe")]
        [Event(5, Keywords = Keywords.Events)]
        private void Activity1Stop(string SourceName, string EventName, IEnumerable<KeyValuePair<string, string?>> Arguments)
        {
            WriteEvent(5, SourceName, EventName, Arguments);
        }

        /// <summary>
        /// Used to mark the beginning of an activity
        /// </summary>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Arguments parameter is trimmer safe")]
        [Event(6, Keywords = Keywords.Events)]
        private void Activity2Start(string SourceName, string EventName, IEnumerable<KeyValuePair<string, string?>> Arguments)
        {
            WriteEvent(6, SourceName, EventName, Arguments);
        }

        /// <summary>
        /// Used to mark the end of an activity that can be recursive.
        /// </summary>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Arguments parameter is trimmer safe")]
        [Event(7, Keywords = Keywords.Events)]
        private void Activity2Stop(string SourceName, string EventName, IEnumerable<KeyValuePair<string, string?>> Arguments)
        {
            WriteEvent(7, SourceName, EventName, Arguments);
        }

        /// <summary>
        /// Used to mark the beginning of an activity
        /// </summary>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Arguments parameter is trimmer safe")]
        [Event(8, Keywords = Keywords.Events, ActivityOptions = EventActivityOptions.Recursive)]
        private void RecursiveActivity1Start(string SourceName, string EventName, IEnumerable<KeyValuePair<string, string?>> Arguments)
        {
            WriteEvent(8, SourceName, EventName, Arguments);
        }

        /// <summary>
        /// Used to mark the end of an activity that can be recursive.
        /// </summary>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Arguments parameter is trimmer safe")]
        [Event(9, Keywords = Keywords.Events, ActivityOptions = EventActivityOptions.Recursive)]
        private void RecursiveActivity1Stop(string SourceName, string EventName, IEnumerable<KeyValuePair<string, string?>> Arguments)
        {
            WriteEvent(9, SourceName, EventName, Arguments);
        }

        /// <summary>
        /// Fires when a new DiagnosticSource becomes available.
        /// </summary>
        /// <param name="SourceName"></param>
        [Event(10, Keywords = Keywords.Events)]
        private void NewDiagnosticListener(string SourceName)
        {
            WriteEvent(10, SourceName);
        }

        /// <summary>
        /// Fires when the Activity start.
        /// </summary>
        /// <param name="SourceName">The ActivitySource name</param>
        /// <param name="ActivityName">The Activity name</param>
        /// <param name="Arguments">Name and value pairs of the Activity properties</param>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Arguments parameter is trimmer safe")]
        [Event(11, Keywords = Keywords.Events, ActivityOptions = EventActivityOptions.Recursive)]
        private void ActivityStart(string SourceName, string ActivityName, IEnumerable<KeyValuePair<string, string?>> Arguments) =>
            WriteEvent(11, SourceName, ActivityName, Arguments);

        /// <summary>
        /// Fires when the Activity stop.
        /// </summary>
        /// <param name="SourceName">The ActivitySource name</param>
        /// <param name="ActivityName">The Activity name</param>
        /// <param name="Arguments">Name and value pairs of the Activity properties</param>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Arguments parameter is trimmer safe")]
        [Event(12, Keywords = Keywords.Events, ActivityOptions = EventActivityOptions.Recursive)]
        private void ActivityStop(string SourceName, string ActivityName, IEnumerable<KeyValuePair<string, string?>> Arguments) =>
            WriteEvent(12, SourceName, ActivityName, Arguments);

        #region private

        private DiagnosticSourceEventSource()
            // This constructor uses EventSourceSettings which is only available on V4.6 and above
            // Use the EventSourceSettings to turn on support for complex types, if available (v4.6 and above).
            : base(EventSourceSettings.EtwSelfDescribingEventFormat)
        {
        }

        /// <summary>
        /// Called when the EventSource gets a command from a EventListener or ETW.
        /// </summary>
        [NonEvent]
        protected override void OnEventCommand(EventCommandEventArgs command)
        {
            // On every command (which the debugger can force by turning on this EventSource with ETW)
            // call a function that the debugger can hook to do an arbitrary func evaluation.
            BreakPointWithDebuggerFuncEval();

            lock (this)
            {
                if ((command.Command == EventCommand.Update || command.Command == EventCommand.Enable) &&
                    IsEnabled(EventLevel.Informational, Keywords.Events))
                {
                    string? filterAndPayloadSpecs = null;
                    command.Arguments!.TryGetValue("FilterAndPayloadSpecs", out filterAndPayloadSpecs);

                    if (!IsEnabled(EventLevel.Informational, Keywords.IgnoreShortCutKeywords))
                    {
                        if (IsEnabled(EventLevel.Informational, Keywords.AspNetCoreHosting))
                            filterAndPayloadSpecs = NewLineSeparate(filterAndPayloadSpecs, AspNetCoreHostingKeywordValue);
                        if (IsEnabled(EventLevel.Informational, Keywords.EntityFrameworkCoreCommands))
                            filterAndPayloadSpecs = NewLineSeparate(filterAndPayloadSpecs, EntityFrameworkCoreCommandsKeywordValue);
                    }
                    FilterAndTransform.CreateFilterAndTransformList(ref _specs, filterAndPayloadSpecs, this);
                }
                else if (command.Command == EventCommand.Update || command.Command == EventCommand.Disable)
                {
                    FilterAndTransform.DestroyFilterAndTransformList(ref _specs, this);
                }
            }
        }

        // trivial helper to allow you to join two strings the first of which can be null.
        private static string NewLineSeparate(string? str1, string str2)
        {
            Debug.Assert(str2 != null);
            if (string.IsNullOrEmpty(str1))
                return str2;
            return str1 + "\n" + str2;
        }

        #region debugger hooks
        private volatile bool _false;       // A value that is always false but the compiler does not know this.

        /// <summary>
        /// A function which is fully interruptible even in release code so we can stop here and
        /// do function evaluation in the debugger. Thus this is just a place that is useful
        /// for the debugger to place a breakpoint where it can inject code with function evaluation
        /// </summary>
        [NonEvent, MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void BreakPointWithDebuggerFuncEval()
        {
            new object();   // This is only here because it helps old .NET Framework runtimes emit a GC safe point at the start of the method
            while (_false)
            {
                _false = false;
            }
        }
        #endregion


        [Flags]
        internal enum ActivityEvents
        {
            None          = 0x00,
            ActivityStart = 0x01,
            ActivityStop  = 0x02,
            All           = ActivityStart | ActivityStop,
        }

        #region EventSource hooks

        /// <summary>
        /// FilterAndTransform represents on transformation specification from a DiagnosticsSource
        /// to EventSource's 'Event' method. (e.g. MySource/MyEvent:out=prop1.prop2.prop3).
        /// Its main method is 'Morph' which takes a DiagnosticSource object and morphs it into
        /// a list of string,string key value pairs.
        ///
        /// This method also contains that static 'Create/Destroy FilterAndTransformList, which
        /// simply parse a series of transformation specifications.
        /// </summary>
        internal sealed class FilterAndTransform
        {
            /// <summary>
            /// Parses filterAndPayloadSpecs which is a list of lines each of which has the from
            ///
            ///    DiagnosticSourceName/EventName:PAYLOAD_SPEC
            ///
            /// where PAYLOADSPEC is a semicolon separated list of specifications of the form
            ///
            ///    OutputName=Prop1.Prop2.PropN
            ///
            /// Into linked list of FilterAndTransform that together forward events from the given
            /// DiagnosticSource's to 'eventSource'. Sets the 'specList' variable to this value
            /// (destroying anything that was there previously).
            ///
            /// By default any serializable properties of the payload object are also included
            /// in the output payload, however this feature and be tuned off by prefixing the
            /// PAYLOADSPEC with a '-'.
            /// </summary>
            public static void CreateFilterAndTransformList(ref FilterAndTransform? specList, string? filterAndPayloadSpecs, DiagnosticSourceEventSource eventSource)
            {
                DestroyFilterAndTransformList(ref specList, eventSource);        // Stop anything that was on before.
                filterAndPayloadSpecs ??= "";

                // Points just beyond the last point in the string that has yet to be parsed. Thus we start with the whole string.
                int endIdx = filterAndPayloadSpecs.Length;
                while (true)
                {
                    // Skip trailing whitespace.
                    while (0 < endIdx && char.IsWhiteSpace(filterAndPayloadSpecs[endIdx - 1]))
                        --endIdx;

                    int newlineIdx = filterAndPayloadSpecs.LastIndexOf('\n', endIdx - 1, endIdx);
                    int startIdx = 0;
                    if (0 <= newlineIdx)
                        startIdx = newlineIdx + 1;  // starts after the newline, or zero if we don't find one.

                    // Skip leading whitespace
                    while (startIdx < endIdx && char.IsWhiteSpace(filterAndPayloadSpecs[startIdx]))
                        startIdx++;

                    if (IsActivitySourceEntry(filterAndPayloadSpecs, startIdx, endIdx))
                    {
                        AddNewActivitySourceTransform(filterAndPayloadSpecs, startIdx, endIdx, eventSource);
                    }
                    else
                    {
                        specList = new FilterAndTransform(filterAndPayloadSpecs, startIdx, endIdx, eventSource, specList);
                    }

                    endIdx = newlineIdx;
                    if (endIdx < 0)
                        break;
                }

                if (eventSource._activitySourceSpecs != null)
                {
                    NormalizeActivitySourceSpecsList(eventSource);
                    CreateActivityListener(eventSource);
                }
            }

            /// <summary>
            /// This destroys (turns off) the FilterAndTransform stopping the forwarding started with CreateFilterAndTransformList
            /// </summary>
            /// <param name="specList"></param>
            /// <param name="eventSource"></param>
            public static void DestroyFilterAndTransformList(ref FilterAndTransform? specList, DiagnosticSourceEventSource eventSource)
            {
                eventSource._activityListener?.Dispose();
                eventSource._activityListener = null;
                eventSource._activitySourceSpecs = null; // nothing to dispose inside this list.

                var curSpec = specList;
                specList = null;            // Null out the list
                while (curSpec != null)     // Dispose everything in the list.
                {
                    curSpec.Dispose();
                    curSpec = curSpec.Next;
                }
            }

            /// <summary>
            /// Creates one FilterAndTransform specification from filterAndPayloadSpec starting at 'startIdx' and ending just before 'endIdx'.
            /// This FilterAndTransform will subscribe to DiagnosticSources specified by the specification and forward them to 'eventSource.
            /// For convenience, the 'Next' field is set to the 'next' parameter, so you can easily form linked lists.
            /// </summary>
            public FilterAndTransform(string filterAndPayloadSpec, int startIdx, int endIdx, DiagnosticSourceEventSource eventSource, FilterAndTransform? next)
            {
                Debug.Assert(filterAndPayloadSpec != null && startIdx >= 0 && startIdx <= endIdx && endIdx <= filterAndPayloadSpec.Length);
                Next = next;
                _eventSource = eventSource;

                string? listenerNameFilter = null;       // Means WildCard.
                string? eventNameFilter = null;          // Means WildCard.
                string? activityName = null;

                var startTransformIdx = startIdx;
                var endEventNameIdx = endIdx;
                var colonIdx = filterAndPayloadSpec.IndexOf(':', startIdx, endIdx - startIdx);
                if (0 <= colonIdx)
                {
                    endEventNameIdx = colonIdx;
                    startTransformIdx = colonIdx + 1;
                }

                // Parse the Source/Event name into listenerNameFilter and eventNameFilter
                var slashIdx = filterAndPayloadSpec.IndexOf('/', startIdx, endEventNameIdx - startIdx);
                if (0 <= slashIdx)
                {
                    listenerNameFilter = filterAndPayloadSpec.Substring(startIdx, slashIdx - startIdx);

                    var atIdx = filterAndPayloadSpec.IndexOf('@', slashIdx + 1, endEventNameIdx - slashIdx - 1);
                    if (0 <= atIdx)
                    {
                        activityName = filterAndPayloadSpec.Substring(atIdx + 1, endEventNameIdx - atIdx - 1);
                        eventNameFilter = filterAndPayloadSpec.Substring(slashIdx + 1, atIdx - slashIdx - 1);
                    }
                    else
                    {
                        eventNameFilter = filterAndPayloadSpec.Substring(slashIdx + 1, endEventNameIdx - slashIdx - 1);
                    }
                }
                else if (startIdx < endEventNameIdx)
                {
                    listenerNameFilter = filterAndPayloadSpec.Substring(startIdx, endEventNameIdx - startIdx);
                }

                _eventSource.Message("DiagnosticSource: Enabling '" + (listenerNameFilter ?? "*") + "/" + (eventNameFilter ?? "*") + "'");

                // If the transform spec begins with a - it means you don't want implicit transforms.
                if (startTransformIdx < endIdx && filterAndPayloadSpec[startTransformIdx] == '-')
                {
                    _eventSource.Message("DiagnosticSource: suppressing implicit transforms.");
                    _noImplicitTransforms = true;
                    startTransformIdx++;
                }

                // Parse all the explicit transforms, if present
                if (startTransformIdx < endIdx)
                {
                    while (true)
                    {
                        int specStartIdx = startTransformIdx;
                        int semiColonIdx = filterAndPayloadSpec.LastIndexOf(';', endIdx - 1, endIdx - startTransformIdx);
                        if (0 <= semiColonIdx)
                            specStartIdx = semiColonIdx + 1;

                        // Ignore empty specifications.
                        if (specStartIdx < endIdx)
                        {
                            if (_eventSource.IsEnabled(EventLevel.Informational, Keywords.Messages))
                                _eventSource.Message("DiagnosticSource: Parsing Explicit Transform '" + filterAndPayloadSpec.Substring(specStartIdx, endIdx - specStartIdx) + "'");

                            _explicitTransforms = new TransformSpec(filterAndPayloadSpec, specStartIdx, endIdx, _explicitTransforms);
                        }
                        if (startTransformIdx == specStartIdx)
                            break;
                        endIdx = semiColonIdx;
                    }
                }

                Action<string, string, IEnumerable<KeyValuePair<string, string?>>>? writeEvent = null;
                if (activityName != null && activityName.Contains("Activity"))
                {
                    writeEvent = activityName switch
                    {
                        nameof(Activity1Start) => _eventSource.Activity1Start,
                        nameof(Activity1Stop) => _eventSource.Activity1Stop,
                        nameof(Activity2Start) => _eventSource.Activity2Start,
                        nameof(Activity2Stop) => _eventSource.Activity2Stop,
                        nameof(RecursiveActivity1Start) => _eventSource.RecursiveActivity1Start,
                        nameof(RecursiveActivity1Stop) => _eventSource.RecursiveActivity1Stop,
                        _ => null
                    };

                    if (writeEvent == null)
                        _eventSource.Message("DiagnosticSource: Could not find Event to log Activity " + activityName);
                }

                writeEvent ??= _eventSource.Event;

                // Set up a subscription that watches for the given Diagnostic Sources and events which will call back
                // to the EventSource.
                _diagnosticsListenersSubscription = DiagnosticListener.AllListeners.Subscribe(new CallbackObserver<DiagnosticListener>(delegate (DiagnosticListener newListener)
                {
                    if (listenerNameFilter == null || listenerNameFilter == newListener.Name)
                    {
                        _eventSource.NewDiagnosticListener(newListener.Name);
                        Predicate<string>? eventNameFilterPredicate = null;
                        if (eventNameFilter != null)
                            eventNameFilterPredicate = (string eventName) => eventNameFilter == eventName;

                        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                            Justification = "DiagnosticSource.Write is marked with RequiresUnreferencedCode.")]
                        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2119",
                            Justification = "DAM on EventSource references this compiler-generated local function which calls a " +
                                            "method that requires unreferenced code. EventSource will not access this local function.")]
                        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
                            Justification = "DiagnosticSource.Write is marked with RequiresDynamicCode.")]
                        void OnEventWritten(KeyValuePair<string, object?> evnt)
                        {
                            // The filter given to the DiagnosticSource may not work if users don't is 'IsEnabled' as expected.
                            // Thus we look for any events that may have snuck through and filter them out before forwarding.
                            if (eventNameFilter != null && eventNameFilter != evnt.Key)
                                return;

                            var outputArgs = this.Morph(evnt.Value);
                            var eventName = evnt.Key;
                            writeEvent(newListener.Name, eventName, outputArgs);
                        }

                        var subscription = newListener.Subscribe(new CallbackObserver<KeyValuePair<string, object?>>(OnEventWritten), eventNameFilterPredicate);
                        _liveSubscriptions = new Subscriptions(subscription, _liveSubscriptions);
                    }
                }));
            }

            internal FilterAndTransform(string filterAndPayloadSpec, int endIdx, int colonIdx, string activitySourceName, string? activityName, ActivityEvents events, ActivitySamplingResult samplingResult, DiagnosticSourceEventSource eventSource)
            {
                _eventSource = eventSource;

                Next = _eventSource._activitySourceSpecs;
                _eventSource._activitySourceSpecs = this;

                SourceName = activitySourceName;
                ActivityName = activityName;
                Events = events;
                SamplingResult = samplingResult;

                if (colonIdx >= 0)
                {
                    int startTransformIdx = colonIdx + 1;

                    // If the transform spec begins with a - it means you don't want implicit transforms.
                    if (startTransformIdx < endIdx && filterAndPayloadSpec[startTransformIdx] == '-')
                    {
                        _eventSource.Message("DiagnosticSource: suppressing implicit transforms.");
                        _noImplicitTransforms = true;
                        startTransformIdx++;
                    }

                    // Parse all the explicit transforms, if present
                    if (startTransformIdx < endIdx)
                    {
                        while (true)
                        {
                            int specStartIdx = startTransformIdx;
                            int semiColonIdx = filterAndPayloadSpec.LastIndexOf(';', endIdx - 1, endIdx - startTransformIdx);
                            if (0 <= semiColonIdx)
                                specStartIdx = semiColonIdx + 1;

                            // Ignore empty specifications.
                            if (specStartIdx < endIdx)
                            {
                                if (_eventSource.IsEnabled(EventLevel.Informational, Keywords.Messages))
                                    _eventSource.Message("DiagnosticSource: Parsing Explicit Transform '" + filterAndPayloadSpec.Substring(specStartIdx, endIdx - specStartIdx) + "'");

                                _explicitTransforms = new TransformSpec(filterAndPayloadSpec, specStartIdx, endIdx, _explicitTransforms);
                            }
                            if (startTransformIdx == specStartIdx)
                                break;
                            endIdx = semiColonIdx;
                        }
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static bool IsActivitySourceEntry(string filterAndPayloadSpec, int startIdx, int endIdx) =>
                            filterAndPayloadSpec.AsSpan(startIdx, endIdx - startIdx).StartsWith(c_ActivitySourcePrefix.AsSpan(), StringComparison.Ordinal);

            internal static void AddNewActivitySourceTransform(string filterAndPayloadSpec, int startIdx, int endIdx, DiagnosticSourceEventSource eventSource)
            {
                Debug.Assert(endIdx - startIdx >= 4);
                Debug.Assert(IsActivitySourceEntry(filterAndPayloadSpec, startIdx, endIdx));

                ReadOnlySpan<char> eventName;
                ReadOnlySpan<char> activitySourceName;

                ActivityEvents supportedEvent = ActivityEvents.All; // Default events
                ActivitySamplingResult samplingResult = ActivitySamplingResult.AllDataAndRecorded; // Default sampling results

                int colonIdx = filterAndPayloadSpec.IndexOf(':', startIdx + c_ActivitySourcePrefix.Length, endIdx - startIdx - c_ActivitySourcePrefix.Length);

                ReadOnlySpan<char> entry = filterAndPayloadSpec.AsSpan(
                                                startIdx + c_ActivitySourcePrefix.Length,
                                                (colonIdx >= 0 ? colonIdx : endIdx) - startIdx - c_ActivitySourcePrefix.Length)
                                                .Trim();

                int eventNameIndex = entry.IndexOf('/');
                if (eventNameIndex >= 0)
                {
                    activitySourceName = entry.Slice(0, eventNameIndex).Trim();

                    ReadOnlySpan<char> suffixPart = entry.Slice(eventNameIndex + 1).Trim();
                    int samplingResultIndex = suffixPart.IndexOf('-');
                    if (samplingResultIndex >= 0)
                    {
                        // We have the format "[AS]SourceName/[EventName]-[SamplingResult]
                        eventName = suffixPart.Slice(0, samplingResultIndex).Trim();
                        suffixPart = suffixPart.Slice(samplingResultIndex + 1).Trim();

                        if (suffixPart.Length > 0)
                        {
                            if (suffixPart.Equals("Propagate".AsSpan(), StringComparison.OrdinalIgnoreCase))
                            {
                                samplingResult = ActivitySamplingResult.PropagationData;
                            }
                            else if (suffixPart.Equals("Record".AsSpan(), StringComparison.OrdinalIgnoreCase))
                            {
                                samplingResult = ActivitySamplingResult.AllData;
                            }
                            else
                            {
                                // Invalid format
                                return;
                            }
                        }
                    }
                    else
                    {
                        // We have the format "[AS]SourceName/[EventName]
                        eventName = suffixPart;
                    }

                    if (eventName.Length > 0)
                    {
                        if (eventName.Equals("Start".AsSpan(), StringComparison.OrdinalIgnoreCase))
                        {
                            supportedEvent = ActivityEvents.ActivityStart;
                        }
                        else if (eventName.Equals("Stop".AsSpan(), StringComparison.OrdinalIgnoreCase))
                        {
                            supportedEvent = ActivityEvents.ActivityStop;
                        }
                        else
                        {
                            // Invalid format
                            return;
                        }
                    }
                }
                else
                {
                    // We have the format "[AS]SourceName"
                    activitySourceName = entry;
                }

                string? activityName = null;
                int plusSignIndex = activitySourceName.IndexOf('+');
                if (plusSignIndex >= 0)
                {
                    activityName = activitySourceName.Slice(plusSignIndex + 1).Trim().ToString();
                    activitySourceName = activitySourceName.Slice(0, plusSignIndex).Trim();
                }

                new FilterAndTransform(filterAndPayloadSpec, endIdx, colonIdx, activitySourceName.ToString(), activityName, supportedEvent, samplingResult, eventSource);
            }

            // Check if we are interested to listen to such ActivitySource
            private static ActivitySamplingResult Sample(string activitySourceName, string activityName, DiagnosticSourceEventSource eventSource)
            {
                FilterAndTransform? list = eventSource._activitySourceSpecs;
                ActivitySamplingResult specificResult = ActivitySamplingResult.None;
                ActivitySamplingResult wildResult = ActivitySamplingResult.None;

                while (list != null)
                {
                    if (list.ActivityName == null || list.ActivityName == activityName)
                    {
                        if (activitySourceName == list.SourceName)
                        {
                                if (list.SamplingResult > specificResult)
                                {
                                    specificResult = list.SamplingResult;
                                }

                                if (specificResult >= ActivitySamplingResult.AllDataAndRecorded)
                                {
                                    return specificResult; // highest possible value
                                }
                                // We don't break here as we can have more than one entry with the same source name.
                            }
                        else if (list.SourceName == "*")
                        {
                            if (specificResult != ActivitySamplingResult.None)
                            {
                                // We reached the '*' nodes which means there is no more specific source names in the list.
                                // If we encountered any specific node before, then return that value.
                                return specificResult;
                            }

                            if (list.SamplingResult > wildResult)
                            {
                                wildResult = list.SamplingResult;
                            }
                        }
                    }
                    list = list.Next;
                }

                // We can return None in case there is no '*' nor any entry match the source name.
                return specificResult != ActivitySamplingResult.None ? specificResult : wildResult;
            }

            internal static void CreateActivityListener(DiagnosticSourceEventSource eventSource)
            {
                Debug.Assert(eventSource._activityListener == null);
                Debug.Assert(eventSource._activitySourceSpecs != null);

                eventSource._activityListener = new ActivityListener();

                eventSource._activityListener.SampleUsingParentId = (ref ActivityCreationOptions<string> activityOptions) => Sample(activityOptions.Source.Name, activityOptions.Name, eventSource);
                eventSource._activityListener.Sample = (ref ActivityCreationOptions<ActivityContext> activityOptions) => Sample(activityOptions.Source.Name, activityOptions.Name, eventSource);

                eventSource._activityListener.ShouldListenTo = (activitySource) =>
                {
                    FilterAndTransform? list = eventSource._activitySourceSpecs;
                    while (list != null)
                    {
                        if (activitySource.Name == list.SourceName || list.SourceName == "*")
                        {
                            return true;
                        }

                        list = list.Next;
                    }

                    return false;
                };

                eventSource._activityListener.ActivityStarted = activity => OnActivityStarted(eventSource, activity);

                eventSource._activityListener.ActivityStopped = activity => OnActivityStopped(eventSource, activity);

                ActivitySource.AddActivityListener(eventSource._activityListener);
            }

            [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(Activity))]
            [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(ActivityContext))]
            [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(ActivityEvent))]
            [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(ActivityLink))]
            [DynamicDependency(nameof(DateTime.Ticks), typeof(DateTime))]
            [DynamicDependency(nameof(TimeSpan.Ticks), typeof(TimeSpan))]
            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                Justification = "Activity's properties are being preserved with the DynamicDependencies on OnActivityStarted.")]
            [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
                            Justification = "Activity is a reference type and is safe in aot.")]
            private static void OnActivityStarted(DiagnosticSourceEventSource eventSource, Activity activity)
            {
                FilterAndTransform? list = eventSource._activitySourceSpecs;
                while (list != null)
                {
                    if ((list.Events & ActivityEvents.ActivityStart) != 0 &&
                        (activity.Source.Name == list.SourceName || list.SourceName == "*") &&
                        (list.ActivityName == null || list.ActivityName == activity.OperationName))
                    {
                        eventSource.ActivityStart(activity.Source.Name, activity.OperationName, list.Morph(activity));
                        return;
                    }

                    list = list.Next;
                }
            }

            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                Justification = "Activity's properties are being preserved with the DynamicDependencies on OnActivityStarted.")]
            [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
                            Justification = "Activity is a reference type and is safe with aot.")]
            private static void OnActivityStopped(DiagnosticSourceEventSource eventSource, Activity activity)
            {
                FilterAndTransform? list = eventSource._activitySourceSpecs;
                while (list != null)
                {
                    if ((list.Events & ActivityEvents.ActivityStop) != 0 &&
                        (activity.Source.Name == list.SourceName || list.SourceName == "*") &&
                        (list.ActivityName == null || list.ActivityName == activity.OperationName))
                    {
                        eventSource.ActivityStop(activity.Source.Name, activity.OperationName, list.Morph(activity));
                        return;
                    }

                    list = list.Next;
                }
            }

            // Move all wildcard nodes at the end of the list.
            // This will give more priority to the specific nodes over the wildcards.
            internal static void NormalizeActivitySourceSpecsList(DiagnosticSourceEventSource eventSource)
            {
                Debug.Assert(eventSource._activityListener == null);
                Debug.Assert(eventSource._activitySourceSpecs != null);

                FilterAndTransform? list = eventSource._activitySourceSpecs;

                FilterAndTransform? firstSpecificList = null;
                FilterAndTransform? lastSpecificList = null;

                FilterAndTransform? firstWildcardList = null;
                FilterAndTransform? lastWildcardList = null;

                while (list != null)
                {
                    if (list.SourceName == "*")
                    {
                        if (firstWildcardList == null)
                        {
                            firstWildcardList = lastWildcardList = list;
                        }
                        else
                        {
                            Debug.Assert(lastWildcardList != null);
                            lastWildcardList.Next = list;
                            lastWildcardList = list;
                        }
                    }
                    else
                    {
                        if (firstSpecificList == null)
                        {
                            firstSpecificList = lastSpecificList = list;
                        }
                        else
                        {
                            Debug.Assert(lastSpecificList != null);
                            lastSpecificList.Next = list;
                            lastSpecificList = list;
                        }
                    }

                    list = list.Next;
                }

                if (firstSpecificList == null || firstWildcardList == null)
                {
                    Debug.Assert(firstSpecificList != null || firstWildcardList != null);
                    return; // list shouldn't be chanaged.
                }

                Debug.Assert(lastWildcardList != null && lastSpecificList != null);

                lastSpecificList.Next = firstWildcardList;
                lastWildcardList.Next = null;

                eventSource._activitySourceSpecs = firstSpecificList;
            }

            private void Dispose()
            {
                if (_diagnosticsListenersSubscription != null)
                {
                    _diagnosticsListenersSubscription.Dispose();
                    _diagnosticsListenersSubscription = null;
                }

                if (_liveSubscriptions != null)
                {
                    Subscriptions? subscr = _liveSubscriptions;
                    _liveSubscriptions = null;
                    while (subscr != null)
                    {
                        subscr.Subscription.Dispose();
                        subscr = subscr.Next;
                    }
                }
            }

            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2112:ReflectionToRequiresUnreferencedCode",
                Justification = "In EventSource, EnsureDescriptorsInitialized's use of GetType preserves this method which " +
                                "requires unreferenced code, but EnsureDescriptorsInitialized does not access this member and is safe to call.")]
            [RequiresUnreferencedCode(DiagnosticSource.WriteRequiresUnreferencedCode)]
            [RequiresDynamicCode(DiagnosticSource.WriteRequiresDynamicCode)]
            public List<KeyValuePair<string, string?>> Morph(object? args)
            {
                // Transform the args into a bag of key-value strings.
                var outputArgs = new List<KeyValuePair<string, string?>>();
                if (args != null)
                {
                    if (!_noImplicitTransforms)
                    {
                        // given the type, fetch the implicit transforms for that type and put it in the implicitTransforms variable.
                        Type argType = args.GetType();
                        TransformSpec? implicitTransforms;

                        // First check the one-element cache _firstImplicitTransformsEntry
                        ImplicitTransformEntry? cacheEntry = _firstImplicitTransformsEntry;
                        if (cacheEntry != null && cacheEntry.Type == argType)
                        {
                            implicitTransforms = cacheEntry.Transforms;     // Yeah we hit the cache.
                        }
                        else if (cacheEntry == null)
                        {
                            // _firstImplicitTransformsEntry is empty, we should fill it.
                            // Note that it is OK that two threads may race and both call MakeImplicitTransforms on their own
                            // (that is we don't expect exactly once initialization of _firstImplicitTransformsEntry)
                            implicitTransforms = MakeImplicitTransforms(argType);
                            Interlocked.CompareExchange(ref _firstImplicitTransformsEntry,
                                new ImplicitTransformEntry() { Type = argType, Transforms = implicitTransforms }, null);
                        }
                        else
                        {
                            // This should only happen when you are wildcarding your events (reasonably rare).
                            // In that case you will probably need many types
                            // Note currently we don't limit the cache size, but it is limited by the number of
                            // distinct types of objects passed to DiagnosticSource.Write.
                            if (_implicitTransformsTable == null)
                            {
                                Interlocked.CompareExchange(ref _implicitTransformsTable,
                                    new ConcurrentDictionary<Type, TransformSpec?>(1, 8), null);
                            }
                            implicitTransforms = _implicitTransformsTable.GetOrAdd(argType, type => MakeImplicitTransformsWrapper(type));

                            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                                Justification = "The Morph method has RequiresUnreferencedCode, but the trimmer can't see through lamdba calls.")]
                            static TransformSpec? MakeImplicitTransformsWrapper(Type transformType) => MakeImplicitTransforms(transformType);
                        }

                        // implicitTransformas now fetched from cache or constructed, use it to Fetch all the implicit fields.
                        if (implicitTransforms != null)
                        {
                            for (TransformSpec? serializableArg = implicitTransforms; serializableArg != null; serializableArg = serializableArg.Next)
                                outputArgs.Add(serializableArg.Morph(args));
                        }
                    }

                    if (_explicitTransforms != null)
                    {
                        for (TransformSpec? explicitTransform = _explicitTransforms; explicitTransform != null; explicitTransform = explicitTransform.Next)
                        {
                            var keyValue = explicitTransform.Morph(args);
                            if (keyValue.Value != null)
                                outputArgs.Add(keyValue);
                        }
                    }
                }
                return outputArgs;
            }

            public FilterAndTransform? Next;

            // Specific ActivitySource Transforms information

            internal const string c_ActivitySourcePrefix = "[AS]";
            internal string? SourceName { get; set; }
            internal string? ActivityName { get; set; }
            internal DiagnosticSourceEventSource.ActivityEvents Events  { get; set; }
            internal ActivitySamplingResult SamplingResult { get; set; }

            #region private

            // Given a type generate all the implicit transforms for type (that is for every field
            // generate the spec that fetches it).
            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2112:ReflectionToRequiresUnreferencedCode",
                Justification = "In EventSource, EnsureDescriptorsInitialized's use of GetType preserves this method which " +
                                "requires unreferenced code, but EnsureDescriptorsInitialized does not access this member and is safe to call.")]
            [RequiresUnreferencedCode(DiagnosticSource.WriteRequiresUnreferencedCode)]
            private static TransformSpec? MakeImplicitTransforms(Type type)
            {
                TransformSpec? newSerializableArgs = null;
                TypeInfo curTypeInfo = type.GetTypeInfo();
                foreach (PropertyInfo property in curTypeInfo.GetProperties(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    // prevent TransformSpec from attempting to implicitly transform index properties
                    if (property.GetMethod == null || property.GetMethod!.GetParameters().Length > 0)
                        continue;
                    newSerializableArgs = new TransformSpec(property.Name, 0, property.Name.Length, newSerializableArgs);
                }
                return Reverse(newSerializableArgs);
            }

            // Reverses a linked list (of TransformSpecs) in place.
            private static TransformSpec? Reverse(TransformSpec? list)
            {
                TransformSpec? ret = null;
                while (list != null)
                {
                    var next = list.Next;
                    list.Next = ret;
                    ret = list;
                    list = next;
                }
                return ret;
            }

            private IDisposable? _diagnosticsListenersSubscription; // This is our subscription that listens for new Diagnostic source to appear.
            private Subscriptions? _liveSubscriptions;              // These are the subscriptions that we are currently forwarding to the EventSource.
            private readonly bool _noImplicitTransforms;                    // Listener can say they don't want implicit transforms.
            private ImplicitTransformEntry? _firstImplicitTransformsEntry; // The transform for _firstImplicitFieldsType
            private ConcurrentDictionary<Type, TransformSpec?>? _implicitTransformsTable; // If there is more than one object type for an implicit transform, they go here.
            private readonly TransformSpec? _explicitTransforms;             // payload to include because the user explicitly indicated how to fetch the field.
            private readonly DiagnosticSourceEventSource _eventSource;      // Where the data is written to.
            #endregion
        }

        // This olds one the implicit transform for one type of object.
        // We remember this type-transform pair in the _firstImplicitTransformsEntry cache.
        internal sealed class ImplicitTransformEntry
        {
            public Type? Type;
            public TransformSpec? Transforms;
        }

        /// <summary>
        /// Transform spec represents a string that describes how to extract a piece of data from
        /// the DiagnosticSource payload. An example string is OUTSTR=EVENT_VALUE.PROP1.PROP2.PROP3
        /// It has a Next field so they can be chained together in a linked list.
        /// </summary>
        internal sealed class TransformSpec
        {
            /// <summary>
            /// parse the strings 'spec' from startIdx to endIdx (points just beyond the last considered char)
            /// The syntax is ID1=ID2.ID3.ID4 .... Where ID1= is optional.
            /// </summary>
            public TransformSpec(string transformSpec, int startIdx, int endIdx, TransformSpec? next = null)
            {
                Debug.Assert(transformSpec != null && startIdx >= 0 && startIdx < endIdx && endIdx <= transformSpec.Length);
                Next = next;

                // Pick off the Var=
                int equalsIdx = transformSpec.IndexOf('=', startIdx, endIdx - startIdx);
                if (0 <= equalsIdx)
                {
                    _outputName = transformSpec.Substring(startIdx, equalsIdx - startIdx);
                    startIdx = equalsIdx + 1;
                }

                // Working from back to front, create a PropertySpec for each .ID in the string.
                while (startIdx < endIdx)
                {
                    int dotIdx = transformSpec.LastIndexOf('.', endIdx - 1, endIdx - startIdx);
                    int idIdx = startIdx;
                    if (0 <= dotIdx)
                        idIdx = dotIdx + 1;

                    string propertyName = transformSpec.Substring(idIdx, endIdx - idIdx);
                    _fetches = new PropertySpec(propertyName, _fetches);

                    // If the user did not explicitly set a name, it is the last one (first to be processed from the end).
                    _outputName ??= propertyName;

                    endIdx = dotIdx;    // This works even when LastIndexOf return -1.
                }
            }

            /// <summary>
            /// Given the DiagnosticSourcePayload 'obj', compute a key-value pair from it. For example
            /// if the spec is OUTSTR=EVENT_VALUE.PROP1.PROP2.PROP3 and the ultimate value of PROP3 is
            /// 10 then the return key value pair is  KeyValuePair("OUTSTR","10")
            /// </summary>
            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2112:ReflectionToRequiresUnreferencedCode",
                Justification = "In EventSource, EnsureDescriptorsInitialized's use of GetType preserves this method which " +
                                "requires unreferenced code, but EnsureDescriptorsInitialized does not access this member and is safe to call.")]
            [RequiresUnreferencedCode(DiagnosticSource.WriteRequiresUnreferencedCode)]
            [RequiresDynamicCode(DiagnosticSource.WriteRequiresDynamicCode)]
            public KeyValuePair<string, string?> Morph(object? obj)
            {
                for (PropertySpec? cur = _fetches; cur != null; cur = cur.Next)
                {
                    if (obj != null || cur.IsStatic)
                        obj = cur.Fetch(obj);
                }

                return new KeyValuePair<string, string?>(_outputName, obj?.ToString());
            }

            /// <summary>
            /// A public field that can be used to form a linked list.
            /// </summary>
            public TransformSpec? Next;

            #region private
            /// <summary>
            /// A PropertySpec represents information needed to fetch a property from
            /// and efficiently. Thus it represents a '.PROP' in a TransformSpec
            /// (and a transformSpec has a list of these).
            /// </summary>
            internal sealed class PropertySpec
            {
                private const string CurrentActivityPropertyName = "*Activity";
                private const string EnumeratePropertyName = "*Enumerate";

                /// <summary>
                /// Make a new PropertySpec for a property named 'propertyName'.
                /// For convenience you can set he 'next' field to form a linked
                /// list of PropertySpecs.
                /// </summary>
                public PropertySpec(string propertyName, PropertySpec? next)
                {
                    Next = next;
                    _propertyName = propertyName;

                    // detect well-known names that are static functions
                    if (_propertyName == CurrentActivityPropertyName)
                    {
                        IsStatic = true;
                    }
                }

                public bool IsStatic { get; private set; }

                /// <summary>
                /// Given an object fetch the property that this PropertySpec represents.
                /// obj may be null when IsStatic is true, otherwise it must be non-null.
                /// </summary>
                [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2112:ReflectionToRequiresUnreferencedCode",
                    Justification = "In EventSource, EnsureDescriptorsInitialized's use of GetType preserves this method which " +
                                    "requires unreferenced code, but EnsureDescriptorsInitialized does not access this member and is safe to call.")]
                [RequiresUnreferencedCode(DiagnosticSource.WriteRequiresUnreferencedCode)]
                [RequiresDynamicCode(DiagnosticSource.WriteRequiresDynamicCode)]
                public object? Fetch(object? obj)
                {
                    PropertyFetch? fetch = _fetchForExpectedType;
                    Debug.Assert(obj != null || IsStatic);
                    Type? objType = obj?.GetType();
                    if (fetch == null || fetch.Type != objType)
                    {
                        _fetchForExpectedType = fetch = PropertyFetch.FetcherForProperty(objType, _propertyName);
                    }
                    object? ret = null;
                    // Avoid the exception which can be thrown during accessing the object properties.
                    try { ret = fetch!.Fetch(obj); } catch (Exception e) { Log.Message($"Property {objType}.{_propertyName} threw the exception {e}"); }
                    return ret;
                }

                /// <summary>
                /// A public field that can be used to form a linked list.
                /// </summary>
                public PropertySpec? Next;

                #region private
                /// <summary>
                /// PropertyFetch is a helper class. It takes a PropertyInfo and then knows how
                /// to efficiently fetch that property from a .NET object (See Fetch method).
                /// It hides some slightly complex generic code.
                /// </summary>
                private class PropertyFetch
                {
                    public PropertyFetch(Type? type)
                    {
                        Type = type;
                    }

                    /// <summary>
                    /// The type of the object that the property is fetched from. For well-known static methods that
                    /// aren't actually property getters this will return null.
                    /// </summary>
                    internal Type? Type { get; }

                    /// <summary>
                    /// Create a property fetcher for a propertyName
                    /// </summary>
                    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2112:ReflectionToRequiresUnreferencedCode",
                        Justification = "In EventSource, EnsureDescriptorsInitialized's use of GetType preserves this method which " +
                                        "requires unreferenced code, but EnsureDescriptorsInitialized does not access this member and is safe to call.")]
                    [RequiresUnreferencedCode(DiagnosticSource.WriteRequiresUnreferencedCode)]
                    [RequiresDynamicCode(DiagnosticSource.WriteRequiresDynamicCode)]
                    public static PropertyFetch FetcherForProperty(Type? type, string propertyName)
                    {
                        if (propertyName == null)
                            return new PropertyFetch(type);     // returns null on any fetch.
                        if (propertyName == CurrentActivityPropertyName)
                        {
                            return new CurrentActivityPropertyFetch();
                        }

                        Debug.Assert(type != null, "Type should only be null for the well-known static fetchers already checked");
                        TypeInfo typeInfo = type.GetTypeInfo();
                        if (propertyName == EnumeratePropertyName)
                        {
                            // If there are multiple implementations of IEnumerable<T>, this arbitrarily uses the first one
                            foreach (Type iFaceType in typeInfo.GetInterfaces())
                            {
                                TypeInfo iFaceTypeInfo = iFaceType.GetTypeInfo();
                                if (!iFaceTypeInfo.IsGenericType ||
                                    iFaceTypeInfo.GetGenericTypeDefinition() != typeof(IEnumerable<>))
                                {
                                    continue;
                                }

                                Type elemType = iFaceTypeInfo.GetGenericArguments()[0];
                                Type instantiatedTypedPropertyFetcher = typeof(EnumeratePropertyFetch<>)
                                    .GetTypeInfo().MakeGenericType(elemType);
                                return (PropertyFetch)Activator.CreateInstance(instantiatedTypedPropertyFetcher, type)!;
                            }

                            // no implementation of IEnumerable<T> found, return a null fetcher
                            Log.Message($"*Enumerate applied to non-enumerable type {type}");
                            return new PropertyFetch(type);
                        }
                        else
                        {
                            PropertyInfo? propertyInfo = typeInfo.GetDeclaredProperty(propertyName);
                            if (propertyInfo == null)
                            {
                                foreach (PropertyInfo pi in typeInfo.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                                {
                                    if (pi.Name == propertyName)
                                    {
                                        propertyInfo = pi;
                                        break;
                                    }
                                }
                            }

                            if (propertyInfo == null)
                            {
                                Log.Message($"Property {propertyName} not found on {type}. Ensure the name is spelled correctly. If you published the application with PublishTrimmed=true, ensure the property was not trimmed away.");
                                return new PropertyFetch(type);
                            }
                            // Delegate creation below is incompatible with static properties.
                            else if (propertyInfo.GetMethod?.IsStatic == true || propertyInfo.SetMethod?.IsStatic == true)
                            {
                                Log.Message($"Property {propertyName} is static.");
                                return new PropertyFetch(type);
                            }
                            Type typedPropertyFetcher = typeInfo.IsValueType ?
                                typeof(ValueTypedFetchProperty<,>) : typeof(RefTypedFetchProperty<,>);
                            Type instantiatedTypedPropertyFetcher = typedPropertyFetcher.GetTypeInfo().MakeGenericType(
                                propertyInfo.DeclaringType!, propertyInfo.PropertyType);
                            return (PropertyFetch)Activator.CreateInstance(instantiatedTypedPropertyFetcher, type, propertyInfo)!;
                        }
                    }

                    /// <summary>
                    /// Given an object, fetch the property that this propertyFech represents.
                    /// </summary>
                    public virtual object? Fetch(object? obj) { return null; }

                    #region private

                    private sealed class RefTypedFetchProperty<TObject, TProperty> : PropertyFetch
                    {
                        public RefTypedFetchProperty(Type type, PropertyInfo property) : base(type)
                        {
                            Debug.Assert(typeof(TObject).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()));
                            _propertyFetch = (Func<TObject, TProperty>)property.GetMethod!.CreateDelegate(typeof(Func<TObject, TProperty>));
                        }
                        public override object? Fetch(object? obj)
                        {
                            Debug.Assert(obj is TObject);
                            return _propertyFetch((TObject)obj);
                        }
                        private readonly Func<TObject, TProperty> _propertyFetch;
                    }

                    private delegate TProperty StructFunc<TStruct, TProperty>(ref TStruct thisArg);

                    // Value types methods require that the first argument is passed by reference. This requires a different delegate signature
                    // from the reference type case.
                    private sealed class ValueTypedFetchProperty<TStruct, TProperty> : PropertyFetch
                    {
                        public ValueTypedFetchProperty(Type type, PropertyInfo property) : base(type)
                        {
                            Debug.Assert(typeof(TStruct) == type);
                            _propertyFetch = (StructFunc<TStruct, TProperty>)property.GetMethod!.CreateDelegate(typeof(StructFunc<TStruct, TProperty>));
                        }
                        public override object? Fetch(object? obj)
                        {
                            Debug.Assert(obj is TStruct);
                            // It is uncommon for property getters to mutate the struct, but if they do the change will be lost.
                            // We are calling the getter on an unboxed copy
                            TStruct structObj = (TStruct)obj;
                            return _propertyFetch(ref structObj);
                        }
                        private readonly StructFunc<TStruct, TProperty> _propertyFetch;
                    }

                    /// <summary>
                    /// A fetcher that returns the result of Activity.Current
                    /// </summary>
                    private sealed class CurrentActivityPropertyFetch : PropertyFetch
                    {
                        public CurrentActivityPropertyFetch() : base(null) { }
                        public override object? Fetch(object? obj)
                        {
                            return Activity.Current;
                        }
                    }

                    /// <summary>
                    /// A fetcher that enumerates and formats an IEnumerable
                    /// </summary>
                    private sealed class EnumeratePropertyFetch<ElementType> : PropertyFetch
                    {
                        public EnumeratePropertyFetch(Type type) : base(type) { }
                        public override object? Fetch(object? obj)
                        {
                            Debug.Assert(obj is IEnumerable<ElementType>);
                            return string.Join(",", (IEnumerable<ElementType>)obj);
                        }
                    }
                    #endregion
                }

                private readonly string _propertyName;
                private volatile PropertyFetch? _fetchForExpectedType;
                #endregion
            }

            private readonly string _outputName = null!;
            private readonly PropertySpec? _fetches;
            #endregion
        }

        /// <summary>
        /// CallbackObserver is an adapter class that creates an observer (which you can pass
        /// to IObservable.Subscribe), and calls the given callback every time the 'next'
        /// operation on the IObserver happens.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        internal sealed class CallbackObserver<T> : IObserver<T>
        {
            public CallbackObserver(Action<T> callback) { _callback = callback; }

            #region private
            public void OnCompleted() { }
            public void OnError(Exception error) { }
            public void OnNext(T value) { _callback(value); }

            private readonly Action<T> _callback;
            #endregion
        }

        // A linked list of IObservable subscriptions (which are IDisposable).
        // We use this to keep track of the DiagnosticSource subscriptions.
        // We use this linked list for thread atomicity
        internal sealed class Subscriptions
        {
            public Subscriptions(IDisposable subscription, Subscriptions? next)
            {
                Subscription = subscription;
                Next = next;
            }
            public IDisposable Subscription;
            public Subscriptions? Next;
        }

        #endregion

        private FilterAndTransform? _specs;                 // Transformation specifications that indicate which sources/events are forwarded.
        private FilterAndTransform? _activitySourceSpecs;   // ActivitySource Transformation specifications that indicate which sources/events are forwarded.
        private ActivityListener? _activityListener;
        #endregion
    }
}
