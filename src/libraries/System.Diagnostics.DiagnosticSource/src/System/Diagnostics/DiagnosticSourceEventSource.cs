// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Diagnostics
{
    /// <summary>
    /// DiagnosticSourceEventSource serves two purposes 
    /// 
    ///   1) It allows debuggers to inject code via Function evaluation.  This is the purpose of the
    ///   BreakPointWithDebuggerFuncEval function in the 'OnEventCommand' method.   Basically even in
    ///   release code, debuggers can place a breakpoint in this method and then trigger the
    ///   DiagnosticSourceEventSource via ETW.  Thus from outside the process you can get a hook that
    ///   is guaranteed to happen BEFORE any DiangosticSource events (if the process is just starting)
    ///   or as soon as possible afterward if it is on attach.
    ///   
    ///   2) It provides a 'bridge' that allows DiagnosticSource messages to be forwarded to EventListers
    ///   or ETW.    You can do this by enabling the Microsoft-Diagnostics-DiagnosticSource with the
    ///   'Events' keyword (for diagnostics purposes, you should also turn on the 'Messages' keyword.  
    ///   
    ///   This EventSource defines a EventSource argument called 'FilterAndPayloadSpecs' that defines
    ///   what DiagnsoticSources to enable and what parts of the payload to serialize into the key-value
    ///   list that will be forwarded to the EventSource.    If it is empty, values of properties of the 
    ///   diagnostic source payload are dumped as strings (using ToString()) and forwarded to the EventSource.  
    ///   For what people think of as serializable object strings, primitives this gives you want you want. 
    ///   (the value of the property in string form) for what people think of as non-serializable objects 
    ///   (e.g. HttpContext) the ToString() method is typically not defined, so you get the Object.ToString() 
    ///   implementation that prints the type name.  This is useful since this is the information you need 
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
    ///       * DIAGNOSTIC_SOURCE_NAME / DIAGNOSTC_EVENT_NAME @ EVENT_SOURCE_EVENTNAME  - give the name as well as the EventSource event to log it under.  
    ///       * DIAGNOSTIC_SOURCE_NAME / DIAGNOSTC_EVENT_NAME   
    ///       * DIAGNOSTIC_SOURCE_NAME    - which wildcards every event in the Diagnostic source or 
    ///       * EMPTY                     - which turns on all sources
    ///   * TRANSFORM_SPEC is a semicolon separated list of TRANSFORM_SPEC, which can be 
    ///       * - TRANSFORM_SPEC               - the '-' indicates that implicit payload elements should be suppressed 
    ///       * VARIABLE_NAME = PROPERTY_SPEC  - indicates that a payload element 'VARIABLE_NAME' is created from PROPERTY_SPEC
    ///       * PROPERTY_SPEC                  - This is a shortcut where VARIABLE_NAME is the LAST property name
    ///   * a PROPERTY_SPEC is basically a list of names separated by '.'  
    ///       * PROPERTY_NAME                  - fetches a property from the DiagnosticSource payload object
    ///       * PROPERTY_NAME . PROPERTY NAME  - fetches a sub-property of the object. 
    /// 
    /// Example1:
    /// 
    ///    "BridgeTestSource1/TestEvent1:cls_Point_X=cls.Point.X;cls_Point_Y=cls.Point.Y\r\n" + 
    ///    "BridgeTestSource2/TestEvent2:-cls.Url"
    ///   
    /// This indicates that two events should be turned on, The 'TestEvent1' event in BridgeTestSource1 and the
    /// 'TestEvent2' in BridgeTestSource2.   In the first case, because the transform did not begin with a - 
    /// any primitive type/string of 'TestEvent1's payload will be serialized into the output.  In addition if
    /// there a property of the payload object called 'cls' which in turn has a property 'Point' which in turn
    /// has a property 'X' then that data is also put in the output with the name cls_Point_X.   Similarly 
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
    /// This will enable all events for the BridgeTestSource1 and BridgeTestSource2 sources.   Any string/primitive 
    /// properties of any of the events will be serialized into the output.  
    /// 
    /// Example:
    /// 
    ///     ""
    ///     
    /// This turns on all DiagnosticSources Any string/primitive properties of any of the events will be serialized 
    /// into the output.   This is not likely to be a good idea as it will be very verbose, but is useful to quickly
    /// discover what is available.
    /// 
    /// 
    /// * How data is logged in the EventSource 
    /// 
    /// By default all data from DiagnosticSources is logged to the DiagnosticEventSouce event called 'Event' 
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
    /// be logged with one of these EventSource events.   This is useful because the events above have start-stop semantics
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
    /// Note you can map many DiangosticSource events to the same EventSource Event (e.g. Activity1Start).  As long as the
    /// activities don't nest, you can reuse the same event name (since the payloads have the DiagnosticSource name which can
    /// disambiguate).   However if they nest you need to use another EventSource event because the rules of EventSource 
    /// activities state that a start of the same event terminates any existing activity of the same name.   
    /// 
    /// As its name suggests RecursiveActivity1Start, is marked as recursive and thus can be used when the activity can nest with 
    /// itself.   This should not be a 'top most' activity because it is not 'self healing' (if you miss a stop, then the
    /// activity NEVER ends).   
    /// 
    /// See the DiagnosticSourceEventSourceBridgeTest.cs for more explicit examples of using this bridge.  
    /// </summary>
    [EventSource(Name = "Microsoft-Diagnostics-DiagnosticSource")]
    internal class DiagnosticSourceEventSource : EventSource
    {
        public static DiagnosticSourceEventSource Logger = new DiagnosticSourceEventSource();

        public class Keywords
        {
            /// <summary>
            /// Indicates diagnostics messages from DiagnosticSourceEventSource should be included. 
            /// </summary>
            public const EventKeywords Messages = (EventKeywords)0x1;
            /// <summary>
            /// Indicates that all events from all diagnostic sources should be forwarded to the EventSource using the 'Event' event.  
            /// </summary>
            public const EventKeywords Events = (EventKeywords)0x2;

            // Some ETW logic does not support passing arguments to the EventProvider.   To get around
            // this in common cases, we define some keywords that basically stand in for particular common argumnents
            // That way at least the common cases can be used by everyone (and it also compresses things).
            // We start these keywords at 0x1000.   See below for the values these keywords represent
            // Because we want all keywords on to still mean 'dump everything by default' we have another keyword
            // IgnoreShorcutKeywords which must be OFF in order for the shortcuts to work thus the all 1s keyword
            // still means what you expect.   
            public const EventKeywords IgnoreShortCutKeywords = (EventKeywords)0x0800;
            public const EventKeywords AspNetCoreHosting = (EventKeywords)0x1000;
            public const EventKeywords EntityFrameworkCoreCommands = (EventKeywords)0x2000;
        };

        // Setting AspNetCoreHosting is like having this in the FilterAndPayloadSpecs string
        // It turns on basic hostig events. 
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
        public void Message(string Message)
        {
            WriteEvent(1, Message);
        }

#if !NO_EVENTSOURCE_COMPLEX_TYPE_SUPPORT
        /// <summary>
        /// Events from DiagnosticSource can be forwarded to EventSource using this event.  
        /// </summary>
        [Event(2, Keywords = Keywords.Events)]
        private void Event(string SourceName, string EventName, IEnumerable<KeyValuePair<string, string>> Arguments)
        {
            WriteEvent(2, SourceName, EventName, Arguments);
        }
#endif 
        /// <summary>
        /// This is only used on V4.5 systems that don't have the ability to log KeyValuePairs directly.
        /// It will eventually go away, but we should always reserve the ID for this.    
        /// </summary>
        [Event(3, Keywords = Keywords.Events)]
        private void EventJson(string SourceName, string EventName, string ArgmentsJson)
        {
            WriteEvent(3, SourceName, EventName, ArgmentsJson);
        }

#if !NO_EVENTSOURCE_COMPLEX_TYPE_SUPPORT
        /// <summary>
        /// Used to mark the beginning of an activity 
        /// </summary>
        [Event(4, Keywords = Keywords.Events)]
        private void Activity1Start(string SourceName, string EventName, IEnumerable<KeyValuePair<string, string>> Arguments)
        {
            WriteEvent(4, SourceName, EventName, Arguments);
        }

        /// <summary>
        /// Used to mark the end of an activity 
        /// </summary>
        [Event(5, Keywords = Keywords.Events)]
        private void Activity1Stop(string SourceName, string EventName, IEnumerable<KeyValuePair<string, string>> Arguments)
        {
            WriteEvent(5, SourceName, EventName, Arguments);
        }

        /// <summary>
        /// Used to mark the beginning of an activity 
        /// </summary>
        [Event(6, Keywords = Keywords.Events)]
        private void Activity2Start(string SourceName, string EventName, IEnumerable<KeyValuePair<string, string>> Arguments)
        {
            WriteEvent(6, SourceName, EventName, Arguments);
        }

        /// <summary>
        /// Used to mark the end of an activity that can be recursive.  
        /// </summary>
        [Event(7, Keywords = Keywords.Events)]
        private void Activity2Stop(string SourceName, string EventName, IEnumerable<KeyValuePair<string, string>> Arguments)
        {
            WriteEvent(7, SourceName, EventName, Arguments);
        }

        /// <summary>
        /// Used to mark the beginning of an activity 
        /// </summary>
        [Event(8, Keywords = Keywords.Events, ActivityOptions = EventActivityOptions.Recursive)]
        private void RecursiveActivity1Start(string SourceName, string EventName, IEnumerable<KeyValuePair<string, string>> Arguments)
        {
            WriteEvent(8, SourceName, EventName, Arguments);
        }

        /// <summary>
        /// Used to mark the end of an activity that can be recursive.  
        /// </summary>
        [Event(9, Keywords = Keywords.Events, ActivityOptions = EventActivityOptions.Recursive)]
        private void RecursiveActivity1Stop(string SourceName, string EventName, IEnumerable<KeyValuePair<string, string>> Arguments)
        {
            WriteEvent(9, SourceName, EventName, Arguments);
        }
#endif

        /// <summary>
        /// Fires when a new DiagnosticSource becomes available.   
        /// </summary>
        /// <param name="SourceName"></param>
        [Event(10, Keywords = Keywords.Events)]
        private void NewDiagnosticListener(string SourceName)
        {
            WriteEvent(10, SourceName);
        }

        #region private

#if NO_EVENTSOURCE_COMPLEX_TYPE_SUPPORT
        /// <summary>
        /// Converts a keyvalue bag to JSON.  Only used on V4.5 EventSources.  
        /// </summary>
        private static string ToJson(IEnumerable<KeyValuePair<string, string>> keyValues)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("{");
            bool first = true;
            foreach (var keyValue in keyValues)
            {
                if (!first)
                    sb.Append(',').AppendLine();
                first = false;

                sb.Append('"').Append(keyValue.Key).Append("\":\"");

                // Write out the value characters, escaping things as needed.  
                foreach(var c in keyValue.Value)
                {
                    if (Char.IsControl(c))
                    {
                        if (c == '\n')
                            sb.Append("\\n");
                        else if (c == '\r')
                            sb.Append("\\r");
                        else
                            sb.Append("\\u").Append(((int)c).ToString("x").PadLeft(4, '0'));
                    }
                    else 
                    {
                        if (c == '"' || c == '\\')
                            sb.Append('\\');
                        sb.Append(c);
                    }
                }
                sb.Append('"');     // Close the string.  
            }
            sb.AppendLine().AppendLine("}");
            return sb.ToString();
        }
#endif

#if !NO_EVENTSOURCE_COMPLEX_TYPE_SUPPORT
        /// <summary>
        /// This constructor uses EventSourceSettings which is only available on V4.6 and above
        /// systems.   We use the EventSourceSettings to turn on support for complex types. 
        /// </summary>
        private DiagnosticSourceEventSource() : base(EventSourceSettings.EtwSelfDescribingEventFormat) { }
#endif

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
                    string filterAndPayloadSpecs;
                    command.Arguments.TryGetValue("FilterAndPayloadSpecs", out filterAndPayloadSpecs);

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
                    FilterAndTransform.DestroyFilterAndTransformList(ref _specs);
                }
            }
        }

        // trivial helper to allow you to join two strings the first of which can be null.  
        private static string NewLineSeparate(string str1, string str2)
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
        /// do function evaluation in the debugger.   Thus this is just a place that is useful
        /// for the debugger to place a breakpoint where it can inject code with function evaluation
        /// </summary>
        [NonEvent, MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void BreakPointWithDebuggerFuncEval()
        {
            new object();   // This is only here because it helps old desktop runtimes emit a GC safe point at the start of the method
            while (_false)
            {
                _false = false;
            }
        }
        #endregion

        #region EventSource hooks 

        /// <summary>
        /// FilterAndTransform represents on transformation specification from a DiagnosticsSource
        /// to EventSource's 'Event' method.    (e.g.  MySource/MyEvent:out=prop1.prop2.prop3).
        /// Its main method is 'Morph' which takes a DiagnosticSource object and morphs it into
        /// a list of string,string key value pairs.   
        /// 
        /// This method also contains that static 'Create/Destroy FilterAndTransformList, which
        /// simply parse a series of transformation specifications.  
        /// </summary>
        internal class FilterAndTransform
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
            /// DiagnosticSource's to 'eventSource'.   Sets the 'specList' variable to this value
            /// (destroying anything that was there previously).  
            /// 
            /// By default any serializable properties of the payload object are also included
            /// in the output payload, however this feature and be tuned off by prefixing the
            /// PAYLOADSPEC with a '-'.   
            /// </summary>
            public static void CreateFilterAndTransformList(ref FilterAndTransform specList, string filterAndPayloadSpecs, DiagnosticSourceEventSource eventSource)
            {
                DestroyFilterAndTransformList(ref specList);        // Stop anything that was on before. 
                if (filterAndPayloadSpecs == null)
                    filterAndPayloadSpecs = "";

                // Points just beyond the last point in the string that has yet to be parsed.   Thus we start with the whole string.  
                int endIdx = filterAndPayloadSpecs.Length;
                for (;;)
                {
                    // Skip trailing whitespace.
                    while (0 < endIdx && Char.IsWhiteSpace(filterAndPayloadSpecs[endIdx - 1]))
                        --endIdx;

                    int newlineIdx = filterAndPayloadSpecs.LastIndexOf('\n', endIdx - 1, endIdx);
                    int startIdx = 0;
                    if (0 <= newlineIdx)
                        startIdx = newlineIdx + 1;  // starts after the newline, or zero if we don't find one.   

                    // Skip leading whitespace
                    while (startIdx < endIdx && Char.IsWhiteSpace(filterAndPayloadSpecs[startIdx]))
                        startIdx++;

                    specList = new FilterAndTransform(filterAndPayloadSpecs, startIdx, endIdx, eventSource, specList);
                    endIdx = newlineIdx;
                    if (endIdx < 0)
                        break;
                }
            }

            /// <summary>
            /// This destroys (turns off) the FilterAndTransform stopping the forwarding started with CreateFilterAndTransformList
            /// </summary>
            /// <param name="specList"></param>
            public static void DestroyFilterAndTransformList(ref FilterAndTransform specList)
            {
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
            public FilterAndTransform(string filterAndPayloadSpec, int startIdx, int endIdx, DiagnosticSourceEventSource eventSource, FilterAndTransform next)
            {
#if DEBUG
                string spec = filterAndPayloadSpec.Substring(startIdx, endIdx - startIdx);
#endif 
                Next = next;
                _eventSource = eventSource;

                string listenerNameFilter = null;       // Means WildCard. 
                string eventNameFilter = null;          // Means WildCard.
                string activityName = null;

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
                    for (;;)
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

                Action<string, string, IEnumerable<KeyValuePair<string, string>>> writeEvent = null;
                if (activityName != null && activityName.Contains("Activity"))
                {
                    MethodInfo writeEventMethodInfo = typeof(DiagnosticSourceEventSource).GetTypeInfo().GetDeclaredMethod(activityName);
                    if (writeEventMethodInfo != null)
                    {
                        // This looks up the activityName (which needs to be a name of an event on DiagnosticSourceEventSource
                        // like Activity1Start and returns that method).   This allows us to have a number of them and this code
                        // just works.  
                        try
                        {
                            writeEvent = (Action<string, string, IEnumerable<KeyValuePair<string, string>>>)
                                writeEventMethodInfo.CreateDelegate(typeof(Action<string, string, IEnumerable<KeyValuePair<string, string>>>), _eventSource);
                        }
                        catch (Exception) { }
                    }
                    if (writeEvent == null)
                        _eventSource.Message("DiagnosticSource: Could not find Event to log Activity " + activityName);
                }

                if (writeEvent == null)
                {
#if !NO_EVENTSOURCE_COMPLEX_TYPE_SUPPORT
                    writeEvent = _eventSource.Event;
#else
                    writeEvent = delegate (string sourceName, string eventName, IEnumerable<KeyValuePair<string, string>> arguments)
                    {
                        _eventSource.EventJson(sourceName, eventName, ToJson(arguments));
                    };
#endif
                }

                // Set up a subscription that watches for the given Diagnostic Sources and events which will call back
                // to the EventSource.   
                _diagnosticsListenersSubscription = DiagnosticListener.AllListeners.Subscribe(new CallbackObserver<DiagnosticListener>(delegate (DiagnosticListener newListener)
                {
                    if (listenerNameFilter == null || listenerNameFilter == newListener.Name)
                    {
                        _eventSource.NewDiagnosticListener(newListener.Name);
                        Predicate<string> eventNameFilterPredicate = null;
                        if (eventNameFilter != null)
                            eventNameFilterPredicate = (string eventName) => eventNameFilter == eventName;

                        var subscription = newListener.Subscribe(new CallbackObserver<KeyValuePair<string, object>>(delegate (KeyValuePair<string, object> evnt)
                        {
                            // The filter given to the DiagnosticSource may not work if users don't is 'IsEnabled' as expected.
                            // Thus we look for any events that may have snuck through and filter them out before forwarding.  
                            if (eventNameFilter != null && eventNameFilter != evnt.Key)
                                return;

                            var outputArgs = this.Morph(evnt.Value);
                            var eventName = evnt.Key;
                            writeEvent(newListener.Name, eventName, outputArgs);
                        }), eventNameFilterPredicate);
                        _liveSubscriptions = new Subscriptions(subscription, _liveSubscriptions);
                    }
                }));
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
                    var subscr = _liveSubscriptions;
                    _liveSubscriptions = null;
                    while (subscr != null)
                    {
                        subscr.Subscription.Dispose();
                        subscr = subscr.Next;
                    }
                }
            }

            public List<KeyValuePair<string, string>> Morph(object args)
            {
                // Transform the args into a bag of key-value strings.  
                var outputArgs = new List<KeyValuePair<string, string>>();
                if (args != null)
                {
                    if (!_noImplicitTransforms)
                    {
                        Type argType = args.GetType();
                        if (_expectedArgType != argType)
                        {
                            // Figure out the default properties to send on to EventSource.  These are all string or primitive properties.  
                            _implicitTransforms = null;
                            TransformSpec newSerializableArgs = null;
                            TypeInfo curTypeInfo = argType.GetTypeInfo();
                            foreach (var property in curTypeInfo.DeclaredProperties)
                            {
                                var propertyType = property.PropertyType;
                                newSerializableArgs = new TransformSpec(property.Name, 0, property.Name.Length, newSerializableArgs);
                            }
                            _expectedArgType = argType;
                            _implicitTransforms = Reverse(newSerializableArgs);
                        }

                        // Fetch all the fields that are already serializable
                        if (_implicitTransforms != null)
                        {
                            for (var serializableArg = _implicitTransforms; serializableArg != null; serializableArg = serializableArg.Next)
                                outputArgs.Add(serializableArg.Morph(args));
                        }
                    }

                    if (_explicitTransforms != null)
                    {
                        for (var explicitTransform = _explicitTransforms; explicitTransform != null; explicitTransform = explicitTransform.Next)
                        {
                            var keyValue = explicitTransform.Morph(args);
                            if (keyValue.Value != null)
                                outputArgs.Add(keyValue);
                        }
                    }
                }
                return outputArgs;
            }

            public FilterAndTransform Next;

            #region private
            // Reverses a linked list (of TransformSpecs) in place.    
            private static TransformSpec Reverse(TransformSpec list)
            {
                TransformSpec ret = null;
                while (list != null)
                {
                    var next = list.Next;
                    list.Next = ret;
                    ret = list;
                    list = next;
                }
                return ret;
            }

            private IDisposable _diagnosticsListenersSubscription; // This is our subscription that listens for new Diagnostic source to appear. 
            private Subscriptions _liveSubscriptions;              // These are the subscriptions that we are currently forwarding to the EventSource.
            private bool _noImplicitTransforms;                    // Listener can say they don't want implicit transforms.  
            private Type _expectedArgType;                         // This is the type where 'implicitTransforms is built for'
            private TransformSpec _implicitTransforms;             // payload to include because the DiagnosticSource's object fields are already serializable 
            private TransformSpec _explicitTransforms;             // payload to include because the user explicitly indicated how to fetch the field.  
            private DiagnosticSourceEventSource _eventSource;      // Where the data is written to.  
            #endregion
        }

        /// <summary>
        /// Transform spec represents a string that describes how to extract a piece of data from
        /// the DiagnosticSource payload.   An example string is OUTSTR=EVENT_VALUE.PROP1.PROP2.PROP3
        /// It has a Next field so they can be chained together in a linked list.  
        /// </summary>
        internal class TransformSpec
        {
            /// <summary>
            /// parse the strings 'spec' from startIdx to endIdx (points just beyond the last considered char)
            /// The syntax is ID1=ID2.ID3.ID4 ....   Where ID1= is optional.    
            /// </summary>
            public TransformSpec(string transformSpec, int startIdx, int endIdx, TransformSpec next = null)
            {
                Debug.Assert(transformSpec != null && startIdx < endIdx);
#if DEBUG
                string spec = transformSpec.Substring(startIdx, endIdx - startIdx);
#endif
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

                    string propertName = transformSpec.Substring(idIdx, endIdx - idIdx);
                    _fetches = new PropertySpec(propertName, _fetches);

                    // If the user did not explicitly set a name, it is the last one (first to be processed from the end).  
                    if (_outputName == null)
                        _outputName = propertName;

                    endIdx = dotIdx;    // This works even when LastIndexOf return -1.  
                }
            }

            /// <summary>
            /// Given the DiagnosticSourcePayload 'obj', compute a key-value pair from it.  For example 
            /// if the spec is OUTSTR=EVENT_VALUE.PROP1.PROP2.PROP3 and the ultimate value of PROP3 is
            /// 10 then the return key value pair is  KeyValuePair("OUTSTR","10") 
            /// </summary>
            public KeyValuePair<string, string> Morph(object obj)
            {
                for (PropertySpec cur = _fetches; cur != null; cur = cur.Next)
                {
                    if (obj != null)
                        obj = cur.Fetch(obj);
                }

                return new KeyValuePair<string, string>(_outputName, obj?.ToString());
            }

            /// <summary>
            /// A public field that can be used to form a linked list.   
            /// </summary>
            public TransformSpec Next;

            #region private 
            /// <summary>
            /// A PropertySpec represents information needed to fetch a property from 
            /// and efficiently.   Thus it represents a '.PROP' in a TransformSpec
            /// (and a transformSpec has a list of these).  
            /// </summary>
            internal class PropertySpec
            {
                /// <summary>
                /// Make a new PropertySpec for a property named 'propertyName'. 
                /// For convenience you can set he 'next' field to form a linked
                /// list of PropertySpecs. 
                /// </summary>
                public PropertySpec(string propertyName, PropertySpec next = null)
                {
                    Next = next;
                    _propertyName = propertyName;
                }

                /// <summary>
                /// Given an object fetch the property that this PropertySpec represents.  
                /// </summary>
                public object Fetch(object obj)
                {
                    Type objType = obj.GetType();
                    if (objType != _expectedType)
                    {
                        var typeInfo = objType.GetTypeInfo();
                        _fetchForExpectedType = PropertyFetch.FetcherForProperty(typeInfo.GetDeclaredProperty(_propertyName));
                        _expectedType = objType;
                    }
                    return _fetchForExpectedType.Fetch(obj);
                }

                /// <summary>
                /// A public field that can be used to form a linked list.   
                /// </summary>
                public PropertySpec Next;

                #region private
                /// <summary>
                /// PropertyFetch is a helper class.  It takes a PropertyInfo and then knows how
                /// to efficiently fetch that property from a .NET object (See Fetch method).  
                /// It hides some slightly complex generic code.  
                /// </summary>
                class PropertyFetch
                {
                    /// <summary>
                    /// Create a property fetcher from a .NET Reflection PropertyInfo class that
                    /// represents a property of a particular type.  
                    /// </summary>
                    public static PropertyFetch FetcherForProperty(PropertyInfo propertyInfo)
                    {
                        if (propertyInfo == null)
                            return new PropertyFetch();     // returns null on any fetch.

                        var typedPropertyFetcher = typeof(TypedFetchProperty<,>);
                        var instantiatedTypedPropertyFetcher = typedPropertyFetcher.GetTypeInfo().MakeGenericType(
                            propertyInfo.DeclaringType, propertyInfo.PropertyType);
                        return (PropertyFetch)Activator.CreateInstance(instantiatedTypedPropertyFetcher, propertyInfo);
                    }

                    /// <summary>
                    /// Given an object, fetch the property that this propertyFech represents. 
                    /// </summary>
                    public virtual object Fetch(object obj) { return null; }

                    #region private 

                    private class TypedFetchProperty<TObject, TProperty> : PropertyFetch
                    {
                        public TypedFetchProperty(PropertyInfo property)
                        {
                            _propertyFetch = (Func<TObject, TProperty>)property.GetMethod.CreateDelegate(typeof(Func<TObject, TProperty>));
                        }
                        public override object Fetch(object obj)
                        {
                            return _propertyFetch((TObject)obj);
                        }
                        private readonly Func<TObject, TProperty> _propertyFetch;
                    }
                    #endregion
                }

                private string _propertyName;
                private Type _expectedType;
                private PropertyFetch _fetchForExpectedType;
                #endregion
            }

            private string _outputName;
            private PropertySpec _fetches;
            #endregion
        }

        /// <summary>
        /// CallbackObserver is an adapter class that creates an observer (which you can pass
        /// to IObservable.Subscribe), and calls the given callback every time the 'next' 
        /// operation on the IObserver happens. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        internal class CallbackObserver<T> : IObserver<T>
        {
            public CallbackObserver(Action<T> callback) { _callback = callback; }

            #region private 
            public void OnCompleted() { }
            public void OnError(Exception error) { }
            public void OnNext(T value) { _callback(value); }

            private Action<T> _callback;
            #endregion
        }

        // A linked list of IObservable subscriptions (which are IDisposable).  
        // We use this to keep track of the DiagnosticSource subscriptions.  
        // We use this linked list for thread atomicity 
        internal class Subscriptions
        {
            public Subscriptions(IDisposable subscription, Subscriptions next)
            {
                Subscription = subscription;
                Next = next;
            }
            public IDisposable Subscription;
            public Subscriptions Next;
        }

        #endregion

        private FilterAndTransform _specs;      // Transformation specifications that indicate which sources/events are forwarded.  
        #endregion
    }
}
