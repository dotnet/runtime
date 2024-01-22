// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Diagnostics
{
    /// <devdoc>
    ///    <para>Provides
    ///       the default output methods and behavior for tracing.</para>
    /// </devdoc>
    public class DefaultTraceListener : TraceListener
    {
        private bool _assertUIEnabled;
        private bool _settingsInitialized;
        private string? _logFileName;

        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.Diagnostics.DefaultTraceListener'/> class with
        ///    Default as its <see cref='System.Diagnostics.TraceListener.Name'/>.</para>
        /// </devdoc>
        public DefaultTraceListener()
            : base("Default")
        {
        }

        public bool AssertUiEnabled
        {
            get
            {
                if (!_settingsInitialized) InitializeSettings();
                return _assertUIEnabled;
            }
            set
            {
                if (!_settingsInitialized) InitializeSettings();
                _assertUIEnabled = value;
            }
        }

        public string? LogFileName
        {
            get
            {
                if (!_settingsInitialized) InitializeSettings();
                return _logFileName;
            }
            set
            {
                if (!_settingsInitialized) InitializeSettings();
                _logFileName = value;
            }
        }

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

        private void InitializeSettings()
        {
            // don't use the property setters here to avoid infinite recursion.
            _assertUIEnabled = DiagnosticsConfiguration.AssertUIEnabled;
            _logFileName = DiagnosticsConfiguration.LogFileName;
            _settingsInitialized = true;
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
            Write(message, useLogFile: true);
        }

        /// <devdoc>
        ///    <para>
        ///       Writes the output followed by a line terminator using <see cref="System.Diagnostics.Debug.Write(string)"/>.
        ///    </para>
        /// </devdoc>
        public override void WriteLine(string? message)
        {
            WriteLine(message, useLogFile: true);
        }

        private void WriteLine(string? message, bool useLogFile)
        {
            if (NeedIndent)
                WriteIndent();

            // The concat is done here to enable a single call to Write
            Write(message + Environment.NewLine, useLogFile);
            NeedIndent = true;
        }

        private void Write(string? message, bool useLogFile)
        {
            message ??= string.Empty;

            if (NeedIndent && message.Length != 0)
            {
                WriteIndent();
            }

            DebugProvider.WriteCore(message);

            if (useLogFile && !string.IsNullOrEmpty(LogFileName))
            {
                WriteToLogFile(message);
            }
        }

        private void WriteToLogFile(string message)
        {
            try
            {
                File.AppendAllText(LogFileName!, message);
            }
            catch (Exception e)
            {
                WriteLine(SR.Format(SR.ExceptionOccurred, LogFileName, e), useLogFile: false);
            }
        }
    }
}
