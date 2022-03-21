// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Logging.EventSource
{
    /// <summary>
    /// The LoggingEventSource is the bridge from all ILogger based logging to EventSource/EventListener logging.
    ///
    /// You turn this logging on by enabling the EventSource called
    ///
    ///      Microsoft-Extensions-Logging
    ///
    /// When you enabled the EventSource, the EventLevel you set is translated in the obvious way to the level
    /// associated with the ILogger (thus Debug = verbose, Informational = Informational ... Critical == Critical)
    ///
    /// This allows you to filter by event level in a straightforward way.
    ///
    /// For finer control you can specify a EventSource Argument called
    ///
    /// FilterSpecs
    ///
    /// The FilterSpecs argument is a semicolon separated list of specifications.   Where each specification is
    ///
    /// SPEC =                          // empty spec, same as *
    ///      | NAME                     // Just a name the level is the default level
    ///      | NAME : LEVEL             // specifies level for a particular logger (can have a * suffix).
    ///
    /// When "UseAppFilters" is specified in the FilterSpecs, it avoids disabling all categories which happens by default otherwise.
    ///
    /// Where Name is the name of a ILoggger (case matters), Name can have a * which acts as a wildcard
    /// AS A SUFFIX.   Thus Net* will match any loggers that start with the 'Net'.
    ///
    /// The LEVEL is a number or a LogLevel string. 0=Trace, 1=Debug, 2=Information, 3=Warning,  4=Error, Critical=5
    /// This specifies the level for the associated pattern.  If the number is not specified, (first form
    /// of the specification) it is the default level for the EventSource.
    ///
    /// First match is used if a particular name matches more than one pattern.
    ///
    /// In addition the level and FilterSpec argument, you can also set EventSource Keywords.  See the Keywords
    /// definition below, but basically you get to decide if you wish to have
    ///
    ///   * Keywords.Message - You get the event with the data in parsed form.
    ///   * Keywords.JsonMessage - you get an event with the data in parse form but as a JSON blob (not broken up by argument ...)
    ///   * Keywords.FormattedMessage - you get an event with the data formatted as a string
    ///
    /// It is expected that you will turn only one of these keywords on at a time, but you can turn them all on (and get
    /// the same data logged three different ways.
    ///
    /// Example Usage
    ///
    /// This example shows how to use an EventListener to get ILogging information
    ///
    /// class MyEventListener : EventListener {
    ///     protected override void OnEventSourceCreated(EventSource eventSource) {
    ///         if (eventSource.Name == "Microsoft-Extensions-Logging") {
    ///             // initialize a string, string dictionary of arguments to pass to the EventSource.
    ///             // Turn on loggers matching App* to Information, everything else (*) is the default level (which is EventLevel.Error)
    ///             var args = new Dictionary&lt;string, string&gt;() { { "FilterSpecs", "App*:Information;*" } };
    ///             // Set the default level (verbosity) to Error, and only ask for the formatted messages in this case.
    ///             EnableEvents(eventSource, EventLevel.Error, LoggingEventSource.Keywords.FormattedMessage, args);
    ///         }
    ///     }
    ///     protected override void OnEventWritten(EventWrittenEventArgs eventData) {
    ///         // Look for the formatted message event, which has the following argument layout (as defined in the LoggingEventSource.
    ///         // FormattedMessage(LogLevel Level, int FactoryID, string LoggerName, string EventId, string FormattedMessage);
    ///         if (eventData.EventName == "FormattedMessage")
    ///             Console.WriteLine("Logger {0}: {1}", eventData.Payload[2], eventData.Payload[4]);
    ///     }
    /// }
    /// </summary>
    [EventSource(Name = "Microsoft-Extensions-Logging")]
    public sealed class LoggingEventSource : System.Diagnostics.Tracing.EventSource
    {
        /// <summary>
        /// This is public from an EventSource consumer point of view, but since these defintions
        /// are not needed outside this class
        /// </summary>
        public static class Keywords
        {
            /// <summary>
            /// Meta events are events about the LoggingEventSource itself (that is they did not come from ILogger
            /// </summary>
            public const EventKeywords Meta = (EventKeywords)1;
            /// <summary>
            /// Turns on the 'Message' event when ILogger.Log() is called.   It gives the information in a programmatic (not formatted) way
            /// </summary>
            public const EventKeywords Message = (EventKeywords)2;
            /// <summary>
            /// Turns on the 'FormatMessage' event when ILogger.Log() is called.  It gives the formatted string version of the information.
            /// </summary>
            public const EventKeywords FormattedMessage = (EventKeywords)4;
            /// <summary>
            /// Turns on the 'MessageJson' event when ILogger.Log() is called.   It gives  JSON representation of the Arguments.
            /// </summary>
            public const EventKeywords JsonMessage = (EventKeywords)8;
        }

        /// <summary>
        ///  The one and only instance of the LoggingEventSource.
        /// </summary>
        internal static readonly LoggingEventSource Instance = new LoggingEventSource();

        // It's important to have _filterSpec initialization here rather than in ctor
        // base ctor might call OnEventCommand and set filter spec
        // having assignment in ctor would overwrite the value
        private LoggerFilterRule[] _filterSpec = Array.Empty<LoggerFilterRule>();
        private CancellationTokenSource? _cancellationTokenSource;
        private const string UseAppFilters = "UseAppFilters";
        private const string WriteEventCoreSuppressionJustification = "WriteEventCore is safe when eventData object is a primitive type which is in this case.";
        private const string WriteEventDynamicDependencySuppressionJustification = "DynamicDependency attribute will ensure that the required properties are not trimmed.";

        private LoggingEventSource() : base(EventSourceSettings.EtwSelfDescribingEventFormat)
        {
        }

        /// <summary>
        /// FormattedMessage() is called when ILogger.Log() is called. and the FormattedMessage keyword is active
        /// This only gives you the human readable formatted message.
        /// </summary>
        [Event(1, Keywords = Keywords.FormattedMessage, Level = EventLevel.LogAlways)]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = WriteEventCoreSuppressionJustification)]
        internal unsafe void FormattedMessage(LogLevel Level, int FactoryID, string LoggerName, int EventId, string? EventName, string FormattedMessage)
        {
            if (IsEnabled())
            {
                LoggerName ??= "";
                EventName ??= "";
                FormattedMessage ??= "";

                fixed (char* loggerName = LoggerName)
                fixed (char* eventName = EventName)
                fixed (char* formattedMessage = FormattedMessage)
                {
                    const int eventDataCount = 6;
                    EventData* eventData = stackalloc EventData[eventDataCount];

                    SetEventData(ref eventData[0], ref Level);
                    SetEventData(ref eventData[1], ref FactoryID);
                    SetEventData(ref eventData[2], ref LoggerName, loggerName);
                    SetEventData(ref eventData[3], ref EventId);
                    SetEventData(ref eventData[4], ref EventName, eventName);
                    SetEventData(ref eventData[5], ref FormattedMessage, formattedMessage);

                    WriteEventCore(1, eventDataCount, eventData);
                }
            }
        }

        /// <summary>
        /// Message() is called when ILogger.Log() is called. and the Message keyword is active
        /// This gives you the logged information in a programmatic format (arguments are key-value pairs)
        /// </summary>
        [Event(2, Keywords = Keywords.Message, Level = EventLevel.LogAlways)]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(KeyValuePair<string, string>))]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = WriteEventDynamicDependencySuppressionJustification)]
        internal void Message(LogLevel Level, int FactoryID, string LoggerName, int EventId, string? EventName, ExceptionInfo Exception, IEnumerable<KeyValuePair<string, string?>> Arguments)
        {
            if (IsEnabled())
            {
                WriteEvent(2, Level, FactoryID, LoggerName, EventId, EventName, Exception, Arguments);
            }
        }

        /// <summary>
        /// ActivityStart is called when ILogger.BeginScope() is called
        /// </summary>
        [Event(3, Keywords = Keywords.Message | Keywords.FormattedMessage, Level = EventLevel.LogAlways, ActivityOptions = EventActivityOptions.Recursive)]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(KeyValuePair<string, string>))]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = WriteEventDynamicDependencySuppressionJustification)]
        internal void ActivityStart(int ID, int FactoryID, string LoggerName, IEnumerable<KeyValuePair<string, string?>> Arguments)
        {
            if (IsEnabled())
            {
                WriteEvent(3, ID, FactoryID, LoggerName, Arguments);
            }
        }

        [Event(4, Keywords = Keywords.Message | Keywords.FormattedMessage, Level = EventLevel.LogAlways)]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = WriteEventCoreSuppressionJustification)]
        internal unsafe void ActivityStop(int ID, int FactoryID, string LoggerName)
        {
            if (IsEnabled())
            {
                LoggerName ??= "";

                fixed (char* loggerName = LoggerName)
                {
                    const int eventDataCount = 3;
                    EventData* eventData = stackalloc EventData[eventDataCount];

                    SetEventData(ref eventData[0], ref ID);
                    SetEventData(ref eventData[1], ref FactoryID);
                    SetEventData(ref eventData[2], ref LoggerName, loggerName);

                    WriteEventCore(4, eventDataCount, eventData);
                }
            }
        }

        [Event(5, Keywords = Keywords.JsonMessage, Level = EventLevel.LogAlways)]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = WriteEventCoreSuppressionJustification)]
        internal unsafe void MessageJson(LogLevel Level, int FactoryID, string LoggerName, int EventId, string? EventName, string ExceptionJson, string ArgumentsJson, string FormattedMessage)
        {
            if (IsEnabled())
            {
                LoggerName ??= "";
                EventName ??= "";
                ExceptionJson ??= "";
                ArgumentsJson ??= "";
                FormattedMessage ??= "";

                fixed (char* loggerName = LoggerName)
                fixed (char* eventName = EventName)
                fixed (char* exceptionJson = ExceptionJson)
                fixed (char* argumentsJson = ArgumentsJson)
                fixed (char* formattedMessage = FormattedMessage)
                {
                    const int eventDataCount = 8;
                    EventData* eventData = stackalloc EventData[eventDataCount];

                    SetEventData(ref eventData[0], ref Level);
                    SetEventData(ref eventData[1], ref FactoryID);
                    SetEventData(ref eventData[2], ref LoggerName, loggerName);
                    SetEventData(ref eventData[3], ref EventId);
                    SetEventData(ref eventData[4], ref EventName, eventName);
                    SetEventData(ref eventData[5], ref ExceptionJson, exceptionJson);
                    SetEventData(ref eventData[6], ref ArgumentsJson, argumentsJson);
                    SetEventData(ref eventData[7], ref FormattedMessage, formattedMessage);

                    WriteEventCore(5, eventDataCount, eventData);
                }
            }
        }

        [Event(6, Keywords = Keywords.JsonMessage | Keywords.FormattedMessage, Level = EventLevel.LogAlways, ActivityOptions = EventActivityOptions.Recursive)]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = WriteEventCoreSuppressionJustification)]
        internal unsafe void ActivityJsonStart(int ID, int FactoryID, string LoggerName, string ArgumentsJson)
        {
            if (IsEnabled())
            {
                LoggerName ??= "";
                ArgumentsJson ??= "";

                fixed (char* loggerName = LoggerName)
                fixed (char* argumentsJson = ArgumentsJson)
                {
                    const int eventDataCount = 4;
                    EventData* eventData = stackalloc EventData[eventDataCount];

                    SetEventData(ref eventData[0], ref ID);
                    SetEventData(ref eventData[1], ref FactoryID);
                    SetEventData(ref eventData[2], ref LoggerName, loggerName);
                    SetEventData(ref eventData[3], ref ArgumentsJson, argumentsJson);

                    WriteEventCore(6, eventDataCount, eventData);
                }
            }
        }

        [Event(7, Keywords = Keywords.JsonMessage | Keywords.FormattedMessage, Level = EventLevel.LogAlways)]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = WriteEventCoreSuppressionJustification)]
        internal unsafe void ActivityJsonStop(int ID, int FactoryID, string LoggerName)
        {
            if (IsEnabled())
            {
                LoggerName ??= "";

                fixed (char* loggerName = LoggerName)
                {
                    const int eventDataCount = 3;
                    EventData* eventData = stackalloc EventData[eventDataCount];

                    SetEventData(ref eventData[0], ref ID);
                    SetEventData(ref eventData[1], ref FactoryID);
                    SetEventData(ref eventData[2], ref LoggerName, loggerName);

                    WriteEventCore(7, eventDataCount, eventData);
                }
            }
        }

        /// <inheritdoc />
        protected override void OnEventCommand(EventCommandEventArgs command)
        {
            if (command.Command is EventCommand.Update or EventCommand.Enable)
            {
                if (!command.Arguments!.TryGetValue("FilterSpecs", out string? filterSpec))
                {
                    filterSpec = string.Empty; // This means turn on everything.
                }

                SetFilterSpec(filterSpec);
            }
            else if (command.Command == EventCommand.Disable)
            {
                SetFilterSpec(null); // This means disable everything.
            }
        }

        /// <summary>
        /// Set the filtering specification.  null means turn off all loggers.   Empty string is turn on all providers.
        /// </summary>
        /// <param name="filterSpec">The filter specification to set.</param>
        [NonEvent]
        private void SetFilterSpec(string? filterSpec)
        {
            _filterSpec = ParseFilterSpec(filterSpec, GetDefaultLevel());

            FireChangeToken();
        }

        [NonEvent]
        internal IChangeToken GetFilterChangeToken()
        {
            CancellationTokenSource cts = LazyInitializer.EnsureInitialized(ref _cancellationTokenSource, () => new CancellationTokenSource());
            return new CancellationChangeToken(cts.Token);
        }

        [NonEvent]
        private void FireChangeToken()
        {
            CancellationTokenSource? tcs = Interlocked.Exchange(ref _cancellationTokenSource, null);
            tcs?.Cancel();
        }

        /// <summary>
        /// Given a set of specifications  Pat1:Level1;Pat1;Level2 ... Where
        /// Pat is a string pattern (a logger Name with a optional trailing wildcard * char)
        /// and Level is a number 0 (Trace) through 5 (Critical).
        ///
        /// The :Level can be omitted (thus Pat1;Pat2 ...) in which case the level is 1 (Debug).
        ///
        /// A completely empty string act like * (all loggers set to Debug level).
        ///
        /// The first specification that 'loggers' Name matches is used.
        /// </summary>
        [NonEvent]
        private static LoggerFilterRule[] ParseFilterSpec(string? filterSpec, LogLevel defaultLevel)
        {
            if (filterSpec == string.Empty)
            {
                return new[] { new LoggerFilterRule(typeof(EventSourceLoggerProvider).FullName, null, defaultLevel, null) };
            }

            if (filterSpec == null)
            {
                // All event source loggers are disabled by default
                return new[] { new LoggerFilterRule(typeof(EventSourceLoggerProvider).FullName, null, LogLevel.None, null) };
            }

            var rules = new List<LoggerFilterRule>();
            int ruleStringsStartIndex = 0;
            string[] ruleStrings = filterSpec.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (ruleStrings.Length > 0 && ruleStrings[0].Equals(UseAppFilters, StringComparison.OrdinalIgnoreCase))
            {
                // Avoid adding default rule to disable event source loggers
                ruleStringsStartIndex = 1;
            }
            else
            {
                rules.Add(new LoggerFilterRule(typeof(EventSourceLoggerProvider).FullName, null, LogLevel.None, null));
            }

            for (int i = ruleStringsStartIndex; i < ruleStrings.Length; i++)
            {
                string rule = ruleStrings[i];
                LogLevel level = defaultLevel;
                string[] parts = rule.Split(new[] { ':' }, 2);
                string loggerName = parts[0];
                if (loggerName.Length == 0)
                {
                    continue;
                }

                if (loggerName[loggerName.Length - 1] == '*')
                {
                    loggerName = loggerName.Substring(0, loggerName.Length - 1);
                }

                if (parts.Length == 2)
                {
                    if (!TryParseLevel(defaultLevel, parts[1], out level))
                    {
                        continue;
                    }
                }

                rules.Add(new LoggerFilterRule(typeof(EventSourceLoggerProvider).FullName, loggerName, level, null));
            }

            return rules.ToArray();
        }

        /// <summary>
        /// Parses the level specification (which should look like :N where n is a  number 0 (Trace)
        /// through 5 (Critical).   It can also be an empty string (which means 1 (Debug) and ';' marks
        /// the end of the specification. This specification should start at spec[curPos]
        /// It returns the value in 'ret' and returns true if successful.  If false is returned ret is left unchanged.
        /// </summary>
        [NonEvent]
        private static bool TryParseLevel(LogLevel defaultLevel, string levelString, out LogLevel ret)
        {
            ret = defaultLevel;

            if (levelString.Length == 0)
            {
                // No :Num spec means Debug
                ret = defaultLevel;
                return true;
            }

            int level;
            switch (levelString)
            {
                case "Trace":
                    ret = LogLevel.Trace;
                    break;
                case "Debug":
                    ret = LogLevel.Debug;
                    break;
                case "Information":
                    ret = LogLevel.Information;
                    break;
                case "Warning":
                    ret = LogLevel.Warning;
                    break;
                case "Error":
                    ret = LogLevel.Error;
                    break;
                case "Critical":
                    ret = LogLevel.Critical;
                    break;
                default:
                    if (!int.TryParse(levelString, out level))
                    {
                        return false;
                    }
                    if (!(LogLevel.Trace <= (LogLevel)level && (LogLevel)level <= LogLevel.None))
                    {
                        return false;
                    }
                    ret = (LogLevel)level;
                    break;
            }
            return true;
        }

        [NonEvent]
        private LogLevel GetDefaultLevel()
        {
            EventKeywords allMessageKeywords = Keywords.Message | Keywords.FormattedMessage | Keywords.JsonMessage;

            if (IsEnabled(EventLevel.Verbose, allMessageKeywords))
            {
                return LogLevel.Debug;
            }

            if (IsEnabled(EventLevel.Informational, allMessageKeywords))
            {
                return LogLevel.Information;
            }

            if (IsEnabled(EventLevel.Warning, allMessageKeywords))
            {
                return LogLevel.Warning;
            }

            if (IsEnabled(EventLevel.Error, allMessageKeywords))
            {
                return LogLevel.Error;
            }

            return LogLevel.Critical;
        }

        [NonEvent]
        internal LoggerFilterRule[] GetFilterRules()
        {
            return _filterSpec;
        }

        [NonEvent]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void SetEventData<T>(ref EventData eventData, ref T value, void* pinnedString = null)
        {
            if (typeof(T) == typeof(string))
            {
                string str = (value as string)!;
#if DEBUG
                fixed (char* rePinnedString = str)
                {
                    Debug.Assert(pinnedString == rePinnedString);
                }
#endif

                eventData.DataPointer = (IntPtr)pinnedString;
                eventData.Size = checked((str.Length + 1) * sizeof(char)); // size is specified in bytes, including null wide char

            }
            else
            {
                eventData.DataPointer = (IntPtr)Unsafe.AsPointer(ref value);
                eventData.Size = Unsafe.SizeOf<T>();
            }
        }
    }
}
