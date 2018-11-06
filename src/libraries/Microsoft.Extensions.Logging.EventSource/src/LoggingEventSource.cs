// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System.Collections.Generic;
using System.Diagnostics.Tracing;

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

        internal static readonly LogLevel LoggingDisabled = LogLevel.None + 1;

        private readonly object _providerLock = new object();
        private string _filterSpec;
        private EventSourceLoggerProvider _loggingProviders;
        private bool _checkLevel;

        internal EventSourceLoggerProvider CreateLoggerProvider()
        {
            lock (_providerLock)
            {
                var newLoggerProvider = new EventSourceLoggerProvider(this, _loggingProviders);
                _loggingProviders = newLoggerProvider;

                // If the EventSource has already been turned on.  set the filters.
                if (_filterSpec != null)
                {
                    newLoggerProvider.SetFilterSpec(_filterSpec);
                }

                return newLoggerProvider;
            }
        }

        private LoggingEventSource() : base(EventSourceSettings.EtwSelfDescribingEventFormat) { }

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
            lock (_providerLock)
            {
                if (command.Command == EventCommand.Update || command.Command == EventCommand.Enable)
                {
                    string filterSpec;
                    if (!command.Arguments.TryGetValue("FilterSpecs", out filterSpec))
                    {
                        filterSpec = string.Empty; // This means turn on everything.
                    }

                    SetFilterSpec(filterSpec);
                }
                else if (command.Command == EventCommand.Update || command.Command == EventCommand.Disable)
                {
                    SetFilterSpec(null); // This means disable everything.
                }
            }
        }

        /// <summary>
        /// Set the filtering specifcation.  null means turn off all loggers.   Empty string is turn on all providers.
        /// </summary>
        /// <param name="filterSpec"></param>
        [NonEvent]
        private void SetFilterSpec(string filterSpec)
        {
            _filterSpec = filterSpec;

            // In .NET 4.5.2 the internal EventSource level hasn't been correctly set
            // when this callback is invoked. To still have the logger behave correctly
            // in .NET 4.5.2 we delay checking the level until the logger is used the first
            // time after this callback.
            _checkLevel = true;
        }

        [NonEvent]
        internal void ApplyFilterSpec()
        {
            lock (_providerLock)
            {
                if (_checkLevel)
                {
                    for (var cur = _loggingProviders; cur != null; cur = cur.Next)
                    {
                        cur.SetFilterSpec(_filterSpec);
                    }
                    _checkLevel = false;
                }
            }
        }
    }
}