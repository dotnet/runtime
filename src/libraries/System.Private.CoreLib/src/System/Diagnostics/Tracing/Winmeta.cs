// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
**
** Purpose:
** Contains eventing constants defined by the Windows
** environment.
**
============================================================*/

namespace System.Diagnostics.Tracing
{
    /// <summary>
    /// WindowsEventLevel. Custom values must be in the range from 16 through 255
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
    /// WindowsEventTask. Custom values must be in the range from 1 through 65534
    /// </summary>
    public enum EventTask
    {
        /// <summary>
        /// Undefined task
        /// </summary>
        None = 0
    }
    /// <summary>
    /// EventOpcode. Custom values must be in the range from 11 through 239
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

    // Added for CLR V4
    /// <summary>
    /// EventChannel. Custom values must be in the range from 16 through 255. Currently only predefined values allowed.
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
    /// EventOpcode
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
