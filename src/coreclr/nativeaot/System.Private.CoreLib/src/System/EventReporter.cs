// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Runtime;
using System.Text;
using System.Threading;

namespace System
{
    internal class EventReporter
    {
        private readonly RhFailFastReason _eventType;
        private readonly StringBuilder _description = new StringBuilder();
        private bool _bufferFull;

        public unsafe EventReporter(RhFailFastReason eventType)
        {
            _eventType = eventType;

            string? processPath = Environment.ProcessPath;

            _description.Append("Application: ");

            // If we were able to get an app name.
            if (processPath != null)
            {
                // If app name has a '\', consider the part after that; otherwise consider whole name.
                _description.AppendLine(Path.GetFileName(processPath));
            }
            else
            {
                _description.AppendLine("unknown");
            }

            _description.Append("CoreCLR Version: ");

            byte* utf8version = RuntimeImports.RhGetRuntimeVersion(out int cbLength);
            _description.AppendLine(new string((sbyte*)utf8version));

            switch (_eventType)
            {
                case RhFailFastReason.UnhandledException:
                case RhFailFastReason.UnhandledExceptionFromPInvoke:
                    _description.AppendLine("Description: The process was terminated due to an unhandled exception.");
                    break;
                case RhFailFastReason.EnvironmentFailFast:
                case RhFailFastReason.AssertionFailure:
                    _description.AppendLine("Description: The application requested process termination through System.Environment.FailFast.");
                    break;
                case RhFailFastReason.InternalError:
                    _description.AppendLine("Description: The process was terminated due to an internal error in the .NET Runtime ");
                    break;
                default:
                    Debug.Fail($"Unknown {nameof(RhFailFastReason)}");
                    break;
            }
        }

        public void AddDescription(string s)
        {
            Debug.Assert(_eventType is RhFailFastReason.UnhandledException
                or RhFailFastReason.EnvironmentFailFast or RhFailFastReason.AssertionFailure
                or RhFailFastReason.UnhandledExceptionFromPInvoke or RhFailFastReason.InternalError);
            if (_eventType is RhFailFastReason.EnvironmentFailFast or RhFailFastReason.AssertionFailure)
            {
                _description.Append("Message: ");
            }
            else if (_eventType == RhFailFastReason.UnhandledException)
            {
                _description.Append("Exception Info: ");
            }
            _description.AppendLine(s);
        }

        public void BeginStackTrace()
        {
            Debug.Assert(_eventType is RhFailFastReason.UnhandledException
                or RhFailFastReason.EnvironmentFailFast or RhFailFastReason.AssertionFailure
                or RhFailFastReason.UnhandledExceptionFromPInvoke);
            _description.AppendLine("Stack:");
        }

        public void AddStackTrace(string s)
        {
            // The (approx.) maximum size that EventLog appears to allow.
            //
            // An event entry comprises of string to be written and event header information.
            // The total permissible length of the string and event header is 32K.
            const int MAX_SIZE_EVENTLOG_ENTRY_STRING = 0x7C62; // decimal 31842

            // Continue to append to the buffer until we are full
            if (!_bufferFull)
            {
                _description.AppendLine(s);

                // Truncate the buffer if we have exceeded the limit based upon the OS we are on
                if (_description.Length > MAX_SIZE_EVENTLOG_ENTRY_STRING)
                {
                    // Load the truncation message
                    string truncate = "\nThe remainder of the message was truncated.\n";

                    int truncCount = truncate.Length;

                    // Go back "truncCount" characters from the end of the string.
                    int ext = MAX_SIZE_EVENTLOG_ENTRY_STRING - truncCount;

                    // Now look for a "\n" from the last position we got
                    for (; ext > 0 && _description[ext] != '\n'; ext--) ;

                    // Truncate the string till our current position and append
                    // the truncation message
                    _description.Length = ext;

                    _description.Append(truncate);

                    // Set the flag that we are full - no point appending more stack details
                    _bufferFull = true;
                }
            }
        }

        public void Report()
        {
            uint eventID;
            switch (_eventType)
            {
                case RhFailFastReason.UnhandledException:
                case RhFailFastReason.UnhandledExceptionFromPInvoke:
                    eventID = 1026;
                    break;
                case RhFailFastReason.EnvironmentFailFast:
                case RhFailFastReason.AssertionFailure:
                    eventID = 1025;
                    break;
                case RhFailFastReason.InternalError:
                    eventID = 1023;
                    break;
                default:
                    Debug.Fail("Invalid event type");
                    eventID = 1023;
                    break;
            }

            if (_description.Length > 0)
            {
                ClrReportEvent(".NET Runtime",
                       1 /* EVENTLOG_ERROR_TYPE */,
                       0,
                       eventID,
                       _description.ToString()
                       );
            }
        }

        private static unsafe void ClrReportEvent(string eventSource, short type, ushort category, uint eventId, string message)
        {
            IntPtr handle = Interop.Advapi32.RegisterEventSource(
                null, // uses local computer
                eventSource);

            if (handle == IntPtr.Zero)
                return;

            fixed (char* pMessage = message)
            {
                Interop.Advapi32.ReportEvent(handle, type, category, eventId, null, 1, 0, (nint)(&pMessage), null);
            }

            Interop.Advapi32.DeregisterEventSource(handle);
        }

        private static byte s_once;

        public static bool ShouldLogInEventLog
        {
            get
            {
                if (Interop.Kernel32.IsDebuggerPresent())
                    return false;

                if (s_once == 1 || Interlocked.Exchange(ref s_once, 1) == 1)
                    return false;

                return true;
            }
        }
    }
}
