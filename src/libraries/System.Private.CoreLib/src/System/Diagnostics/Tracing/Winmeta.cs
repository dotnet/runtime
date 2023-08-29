// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics.Tracing
{
    /// <summary>
    /// Contains an event level that is defined in an event provider. The level signifies the severity of the event.
    /// Custom values must be in the range from 16 through 255.
    /// </summary>
    public enum EventLevel
    {
        /// <summary>
        /// Log always
        /// </summary>
        LogAlways = 0,
        /// <summary>
        /// Only critical errors
        /// </summary>
        Critical,
        /// <summary>
        /// All errors, including previous levels
        /// </summary>
        Error,
        /// <summary>
        /// All warnings, including previous levels
        /// </summary>
        Warning,
        /// <summary>
        /// All informational events, including previous levels
        /// </summary>
        Informational,
        /// <summary>
        /// All events, including previous levels
        /// </summary>
        Verbose
    }

    /// <summary>
    /// Contains an event task that is defined in an event provider. The task identifies a portion of an application or a component that publishes an event. A task is a 16-bit value with 16 top values reserved.
    /// Custom values must be in the range from 1 through 65534.
    /// </summary>
    public enum EventTask
    {
        /// <summary>
        /// Undefined task
        /// </summary>
        None = 0
    }

    /// <summary>
    /// Contains an event opcode that is defined in an event provider. An opcode defines a numeric value that identifies the activity or a point within an activity that the application was performing when it raised the event.
    /// Custom values must be in the range from 11 through 239.
    /// </summary>
    public enum EventOpcode
    {
        /// <summary>
        /// An informational event
        /// </summary>
        Info = 0,
        /// <summary>
        /// An activity start event
        /// </summary>
        Start,
        /// <summary>
        /// An activity end event
        /// </summary>
        Stop,
        /// <summary>
        /// A trace collection start event
        /// </summary>
        DataCollectionStart,
        /// <summary>
        /// A trace collection end event
        /// </summary>
        DataCollectionStop,
        /// <summary>
        /// An extensional event
        /// </summary>
        Extension,
        /// <summary>
        /// A reply event
        /// </summary>
        Reply,
        /// <summary>
        /// An event representing the activity resuming from the suspension
        /// </summary>
        Resume,
        /// <summary>
        /// An event representing the activity is suspended, pending another activity's completion
        /// </summary>
        Suspend,
        /// <summary>
        /// An event representing the activity is transferred to another component, and can continue to work
        /// </summary>
        Send,
        /// <summary>
        /// An event representing receiving an activity transfer from another component
        /// </summary>
        Receive = 240
    }

    /// <summary>
    /// Specifies the event log channel for the event.
    /// </summary>
    public enum EventChannel : byte
    {
        /// <summary>
        /// No channel
        /// </summary>
        None = 0,
        // Channels 1 - 15 are reserved...
        /// <summary>The admin channel</summary>
        Admin = 16,
        /// <summary>The operational channel</summary>
        Operational = 17,
        /// <summary>The analytic channel</summary>
        Analytic = 18,
        /// <summary>The debug channel</summary>
        Debug = 19,
    }

    /// <summary>
    /// Defines the standard keywords that apply to events.
    /// </summary>
    [Flags]
    public enum EventKeywords : long
    {
        /// <summary>
        /// No events.
        /// </summary>
        None = 0x0,
        /// <summary>
        /// All Events
        /// </summary>
        All = ~0,
        /// <summary>
        /// Telemetry events
        /// </summary>
        MicrosoftTelemetry = 0x02000000000000,
        /// <summary>
        /// WDI context events
        /// </summary>
        WdiContext = 0x02000000000000,
        /// <summary>
        /// WDI diagnostic events
        /// </summary>
        WdiDiagnostic = 0x04000000000000,
        /// <summary>
        /// SQM events
        /// </summary>
        Sqm = 0x08000000000000,
        /// <summary>
        /// Failed security audits
        /// </summary>
        AuditFailure = 0x10000000000000,
        /// <summary>
        /// Successful security audits
        /// </summary>
        AuditSuccess = 0x20000000000000,
        /// <summary>
        /// Transfer events where the related Activity ID is a computed value and not a GUID
        /// N.B. The correct value for this field is 0x40000000000000.
        /// </summary>
        CorrelationHint = 0x10000000000000,
        /// <summary>
        /// Events raised using classic eventlog API
        /// </summary>
        EventLogClassic = 0x80000000000000
    }
}
