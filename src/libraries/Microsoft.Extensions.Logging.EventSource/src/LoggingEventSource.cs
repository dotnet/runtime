// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Logging.EventSource
{
    /// <summary>
    /// The LoggingEventSource is the bridge form all ILogger based logging to EventSource/EventListener logging.
    ///
    /// You turn this logging on by enabling the EvenSource called
    ///
    ///      Microsoft-Extensions-Logging
    ///
    /// When you enabled the EventSource, the EventLevel you set is translated in the obvious way to the level
    /// associated with the ILogger (thus Debug = verbose, Informational = Informational ... Critical == Critical)
    ///
    /// This allows you to filter by event level in a straighforward way.
    ///
    /// For finer control you can specify a EventSource Argument called
    ///
    /// FilterSpecs
    ///
    /// The FilterSpecs argument is a semicolon separated list of specifications.   Where each specification is
    ///
    /// SPEC =                          // empty spec, same as *
    ///      | NAME                     // Just a name the level is the default level
    ///      | NAME : LEVEL            // specifies level for a particular logger (can have a * suffix).
    ///
    /// Where Name is the name of a ILoggger (case matters), Name can have a * which acts as a wildcard
    /// AS A SUFFIX.   Thus Net* will match any loggers that start with the 'Net'.
    ///
    /// The LEVEL is a number or a LogLevel string. 0=Trace, 1=Debug, 2=Information, 3=Warning,  4=Error, Critical=5
    /// This speicifies the level for the associated pattern.  If the number is not specified, (first form
    /// of the specification) it is the default level for the EventSource.
    ///
    /// First match is used if a partciular name matches more than one pattern.
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
    internal class LoggingEventSource : System.Diagnostics.Tracing.EventSource
    {
        /// <summary>
        /// This is public from an EventSource consumer point of view, but since these defintions
        /// are not needed outside this class
        /// </summary>
        public class Keywords
        {
            /// <summary>
            /// Meta events are evnets about the LoggingEventSource itself (that is they did not come from ILogger
            /// </summary>
            public const EventKeywords Meta = (EventKeywords)1;
            /// <summary>
            /// Turns on the 'Message' event when ILogger.Log() is called.   It gives the information in a programatic (not formatted) way
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

        private LoggerFilterRule[] _filterSpec;
        private CancellationTokenSource _cancellationTokenSource;

        private LoggingEventSource() : base(EventSourceSettings.EtwSelfDescribingEventFormat)
        {
            _filterSpec = new LoggerFilterRule[0];
        }

        /// <summary>
        /// FormattedMessage() is called when ILogger.Log() is called. and the FormattedMessage keyword is active
        /// This only gives you the human reasable formatted message.
        /// </summary>
        [Event(1, Keywords = Keywords.FormattedMessage, Level = EventLevel.LogAlways)]
        internal void FormattedMessage(LogLevel Level, int FactoryID, string LoggerName, string EventId, string FormattedMessage)
        {
            WriteEvent(1, Level, FactoryID, LoggerName, EventId, FormattedMessage);
        }

        /// <summary>
        /// Message() is called when ILogger.Log() is called. and the Message keyword is active
        /// This gives you the logged information in a programatic format (arguments are key-value pairs)
        /// </summary>
        [Event(2, Keywords = Keywords.Message, Level = EventLevel.LogAlways)]
        internal void Message(LogLevel Level, int FactoryID, string LoggerName, string EventId, ExceptionInfo Exception, IEnumerable<KeyValuePair<string, string>> Arguments)
        {
            WriteEvent(2, Level, FactoryID, LoggerName, EventId, Exception, Arguments);
        }

        /// <summary>
        /// ActivityStart is called when ILogger.BeginScope() is called
        /// </summary>
        [Event(3, Keywords = Keywords.Message | Keywords.FormattedMessage, Level = EventLevel.LogAlways, ActivityOptions = EventActivityOptions.Recursive)]
        internal void ActivityStart(int ID, int FactoryID, string LoggerName, IEnumerable<KeyValuePair<string, string>> Arguments)
        {
            WriteEvent(3, ID, FactoryID, LoggerName, Arguments);
        }

        [Event(4, Keywords = Keywords.Message | Keywords.FormattedMessage, Level = EventLevel.LogAlways)]
        internal void ActivityStop(int ID, int FactoryID, string LoggerName)
        {
            WriteEvent(4, ID, FactoryID, LoggerName);
        }

        [Event(5, Keywords = Keywords.JsonMessage, Level = EventLevel.LogAlways)]
        internal void MessageJson(LogLevel Level, int FactoryID, string LoggerName, string EventId, string ExceptionJson, string ArgumentsJson)
        {
            WriteEvent(5, Level, FactoryID, LoggerName, EventId, ExceptionJson, ArgumentsJson);
        }

        [Event(6, Keywords = Keywords.JsonMessage | Keywords.FormattedMessage, Level = EventLevel.LogAlways, ActivityOptions = EventActivityOptions.Recursive)]
        internal void ActivityJsonStart(int ID, int FactoryID, string LoggerName, string ArgumentsJson)
        {
            WriteEvent(6, ID, FactoryID, LoggerName, ArgumentsJson);
        }

        [Event(7, Keywords = Keywords.JsonMessage | Keywords.FormattedMessage, Level = EventLevel.LogAlways)]
        internal void ActivityJsonStop(int ID, int FactoryID, string LoggerName)
        {
            WriteEvent(7, ID, FactoryID, LoggerName);
        }

        /// <inheritdoc />
        protected override void OnEventCommand(EventCommandEventArgs command)
        {
            if (command.Command == EventCommand.Update || command.Command == EventCommand.Enable)
            {
                if (!command.Arguments.TryGetValue("FilterSpecs", out var filterSpec))
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
        /// Set the filtering specifcation.  null means turn off all loggers.   Empty string is turn on all providers.
        /// </summary>
        /// <param name="filterSpec"></param>
        [NonEvent]
        private void SetFilterSpec(string filterSpec)
        {
            _filterSpec = ParseFilterSpec(filterSpec, GetDefaultLevel());

            FireChangeToken();
        }

        [NonEvent]
        internal IChangeToken GetFilterChangeToken()
        {
            var cts = LazyInitializer.EnsureInitialized(ref _cancellationTokenSource, () => new CancellationTokenSource());
            return new CancellationChangeToken(cts.Token);
        }

        [NonEvent]
        private void FireChangeToken()
        {
            var tcs = Interlocked.Exchange(ref _cancellationTokenSource, null);
            tcs.Cancel();
        }

         /// <summary>
        /// Given a set of specifications  Pat1:Level1;Pat1;Level2 ... Where
        /// Pat is a string pattern (a logger Name with a optional trailing wildcard * char)
        /// and Level is a number 0 (Trace) through 5 (Critical).
        ///
        /// The :Level can be omitted (thus Pat1;Pat2 ...) in which case the level is 1 (Debug).
        ///
        /// A completely emtry sting act like * (all loggers set to Debug level).
        ///
        /// The first specification that 'loggers' Name matches is used.
        /// </summary>
        [NonEvent]
        public static LoggerFilterRule[] ParseFilterSpec(string filterSpec, LogLevel defaultLevel)
        {
            if (filterSpec == string.Empty)
            {
                return new [] { new LoggerFilterRule(typeof(EventSourceLoggerProvider).FullName, null, defaultLevel, null) };
            }

            var rules = new List<LoggerFilterRule>();

            // All event source loggers are disabled by default
            rules.Add(new LoggerFilterRule(typeof(EventSourceLoggerProvider).FullName, null, LogLevel.None, null));

            if (filterSpec != null)
            {
                var ruleStrings = filterSpec.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var rule in ruleStrings)
                {
                    var level = defaultLevel;
                    var parts = rule.Split(new[] { ':' }, 2);
                    var loggerName = parts[0];
                    if (loggerName.Length == 0)
                    {
                        continue;
                    }

                    if (loggerName[loggerName.Length-1] == '*')
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
            }

            return rules.ToArray();
        }

        /// <summary>
        /// Parses the level specification (which should look like :N where n is a  number 0 (Trace)
        /// through 5 (Critical).   It can also be an empty string (which means 1 (Debug) and ';' marks
        /// the end of the specifcation This specification should start at spec[curPos]
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
            var allMessageKeywords = Keywords.Message | Keywords.FormattedMessage | Keywords.JsonMessage;

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
        public LoggerFilterRule[] GetFilterRules()
        {
            return _filterSpec;
        }
    }
}