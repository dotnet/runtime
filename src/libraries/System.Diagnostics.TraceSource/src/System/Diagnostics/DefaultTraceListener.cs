// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#define DEBUG
#define TRACE
using System.IO;

namespace System.Diagnostics
{
    /// <devdoc>
    ///    <para>Provides
    ///       the default output methods and behavior for tracing.</para>
    /// </devdoc>
    public class DefaultTraceListener : TraceListener
    {
        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.Diagnostics.DefaultTraceListener'/> class with
        ///    Default as its <see cref='System.Diagnostics.TraceListener.Name'/>.</para>
        /// </devdoc>
        public DefaultTraceListener()
            : base("Default")
        {
        }

        public bool AssertUiEnabled { get; set; } = true;

        public string? LogFileName { get; set; } = string.Empty;

        /// <devdoc>
        ///    <para>
        ///       Emits or displays a message
        ///       and a stack trace for an assertion that
        ///       always fails.
        ///    </para>
        /// </devdoc>
        public override void Fail(string? message)
        {
            Fail(message, null);
        }

        /// <devdoc>
        ///    <para>
        ///       Emits or displays messages and a stack trace for an assertion that
        ///       always fails.
        ///    </para>
        /// </devdoc>
        public override void Fail(string? message, string? detailMessage)
        {
            string stackTrace;
            try
            {
                stackTrace = new StackTrace(fNeedFileInfo: true).ToString();
            }
            catch
            {
                stackTrace = "";
            }
            WriteAssert(stackTrace, message, detailMessage);
            if (AssertUiEnabled)
            {
                DebugProvider.FailCore(stackTrace, message, detailMessage, "Assertion Failed");
            }
        }

        private void WriteAssert(string stackTrace, string? message, string? detailMessage)
        {
            WriteLine(SR.DebugAssertBanner + Environment.NewLine
                   + SR.DebugAssertShortMessage + Environment.NewLine
                   + message + Environment.NewLine
                   + SR.DebugAssertLongMessage + Environment.NewLine
                   + detailMessage + Environment.NewLine
                   + stackTrace);
        }

        /// <devdoc>
        ///    <para>
        ///       Writes the output using <see cref="System.Diagnostics.Debug.Write(string)"/>.
        ///    </para>
        /// </devdoc>
        public override void Write(string? message)
        {
            if (message == null || message.Length == 0)
            {
                DebugProvider.WriteCore(string.Empty);
            }
            else
            {
                if (NeedIndent)
                    WriteIndent();

                DebugProvider.WriteCore(message);

                if (LogFileName is { Length: > 0 } logFileName)
                    WriteToLogFile(message, logFileName);
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Writes the output followed by a line terminator using <see cref="System.Diagnostics.Debug.Write(string)"/>.
        ///    </para>
        /// </devdoc>
        public override void WriteLine(string? message)
        {
            if (NeedIndent)
                WriteIndent();

            // The concat is done here to enable a single call to Write
            message += Environment.NewLine;
            DebugProvider.WriteCore(message);
            NeedIndent = true;

            if (LogFileName is { Length: > 0 } logFileName)
                WriteToLogFile(message, logFileName);
        }

        private void WriteToLogFile(string message, string logFileName)
        {
            try
            {
                File.AppendAllText(logFileName, message);
            }
            catch (Exception e)
            {
                DebugProvider.WriteCore(SR.Format(SR.ExceptionOccurred, logFileName, e) + Environment.NewLine);
                NeedIndent = true;
            }
        }
    }
}
