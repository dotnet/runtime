// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;

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
    ///       * SAMPLING_RESULT either:
    ///         * "Propagate" to create the Activity with PropagationData
    ///         * "Record" to create the Activity with AllData
    ///         * "ParentRatioSampler([ratio])" to create the Activity based on OTel parent + TraceId ratio algorithm. [ratio] should be a value between 0.0 (0%) and 1.0 (100%).
    ///         * Empty string to create the Activity with AllDataAndRecorded
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
    internal sealed partial class DiagnosticSourceEventSource : EventSource
    {
        public static readonly DiagnosticSourceEventSource Log = new DiagnosticSourceEventSource();

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
            Justification = "Arguments parameter is preserved by DynamicDependency")]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(KeyValuePair<,>))]
        [Event(2, Keywords = Keywords.Events)]
        public void Event(string SourceName, string EventName, IEnumerable<KeyValuePair<string, string?>>? Arguments)
        {
            WriteEvent(2, SourceName, EventName, Arguments);
        }

        /// <summary>
        /// This is only used on V4.5 systems that don't have the ability to log KeyValuePairs directly.
        /// It will eventually go away, but we should always reserve the ID for this.
        /// </summary>
        [Event(3, Keywords = Keywords.Events)]
        public void EventJson(string SourceName, string EventName, string ArgmentsJson)
        {
            WriteEvent(3, SourceName, EventName, ArgmentsJson);
        }

        /// <summary>
        /// Used to mark the beginning of an activity
        /// </summary>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Arguments parameter is preserved by DynamicDependency")]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(KeyValuePair<,>))]
        [Event(4, Keywords = Keywords.Events)]
        public void Activity1Start(string SourceName, string EventName, IEnumerable<KeyValuePair<string, string?>> Arguments)
        {
            WriteEvent(4, SourceName, EventName, Arguments);
        }

        /// <summary>
        /// Used to mark the end of an activity
        /// </summary>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Arguments parameter is preserved by DynamicDependency")]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(KeyValuePair<,>))]
        [Event(5, Keywords = Keywords.Events)]
        public void Activity1Stop(string SourceName, string EventName, IEnumerable<KeyValuePair<string, string?>> Arguments)
        {
            WriteEvent(5, SourceName, EventName, Arguments);
        }

        /// <summary>
        /// Used to mark the beginning of an activity
        /// </summary>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Arguments parameter is preserved by DynamicDependency")]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(KeyValuePair<,>))]
        [Event(6, Keywords = Keywords.Events)]
        public void Activity2Start(string SourceName, string EventName, IEnumerable<KeyValuePair<string, string?>> Arguments)
        {
            WriteEvent(6, SourceName, EventName, Arguments);
        }

        /// <summary>
        /// Used to mark the end of an activity that can be recursive.
        /// </summary>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Arguments parameter is preserved by DynamicDependency")]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(KeyValuePair<,>))]
        [Event(7, Keywords = Keywords.Events)]
        public void Activity2Stop(string SourceName, string EventName, IEnumerable<KeyValuePair<string, string?>> Arguments)
        {
            WriteEvent(7, SourceName, EventName, Arguments);
        }

        /// <summary>
        /// Used to mark the beginning of an activity
        /// </summary>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Arguments parameter is preserved by DynamicDependency")]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(KeyValuePair<,>))]
        [Event(8, Keywords = Keywords.Events, ActivityOptions = EventActivityOptions.Recursive)]
        public void RecursiveActivity1Start(string SourceName, string EventName, IEnumerable<KeyValuePair<string, string?>> Arguments)
        {
            WriteEvent(8, SourceName, EventName, Arguments);
        }

        /// <summary>
        /// Used to mark the end of an activity that can be recursive.
        /// </summary>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Arguments parameter is preserved by DynamicDependency")]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(KeyValuePair<,>))]
        [Event(9, Keywords = Keywords.Events, ActivityOptions = EventActivityOptions.Recursive)]
        public void RecursiveActivity1Stop(string SourceName, string EventName, IEnumerable<KeyValuePair<string, string?>> Arguments)
        {
            WriteEvent(9, SourceName, EventName, Arguments);
        }

        /// <summary>
        /// Fires when a new DiagnosticSource becomes available.
        /// </summary>
        /// <param name="SourceName"></param>
        [Event(10, Keywords = Keywords.Events)]
        public void NewDiagnosticListener(string SourceName)
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
            Justification = "Arguments parameter is preserved by DynamicDependency")]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(KeyValuePair<,>))]
        [Event(11, Keywords = Keywords.Events, ActivityOptions = EventActivityOptions.Recursive)]
        public void ActivityStart(string SourceName, string ActivityName, IEnumerable<KeyValuePair<string, string?>> Arguments) =>
            WriteEvent(11, SourceName, ActivityName, Arguments);

        /// <summary>
        /// Fires when the Activity stop.
        /// </summary>
        /// <param name="SourceName">The ActivitySource name</param>
        /// <param name="ActivityName">The Activity name</param>
        /// <param name="Arguments">Name and value pairs of the Activity properties</param>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Arguments parameter is preserved by DynamicDependency")]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(KeyValuePair<,>))]
        [Event(12, Keywords = Keywords.Events, ActivityOptions = EventActivityOptions.Recursive)]
        public void ActivityStop(string SourceName, string ActivityName, IEnumerable<KeyValuePair<string, string?>> Arguments) =>
            WriteEvent(12, SourceName, ActivityName, Arguments);

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
                    _listener?.Dispose();
                    _listener = DiagnosticSourceEventSourceFilterAndTransform.ParseFilterAndPayloadSpecs(filterAndPayloadSpecs);
                }
                else if (command.Command == EventCommand.Update || command.Command == EventCommand.Disable)
                {
                    _listener?.Dispose();
                }
            }
        }

        #region private
        private DiagnosticSourceEventSource()
            // This constructor uses EventSourceSettings which is only available on V4.6 and above
            // Use the EventSourceSettings to turn on support for complex types, if available (v4.6 and above).
            : base(EventSourceSettings.EtwSelfDescribingEventFormat)
        {
        }

        // trivial helper to allow you to join two strings the first of which can be null.
        private static string NewLineSeparate(string? str1, string str2)
        {
            Debug.Assert(str2 != null);
            if (string.IsNullOrEmpty(str1))
                return str2;
            return str1 + "\n" + str2;
        }

        private IDisposable? _listener;
        #endregion

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
    }
}
