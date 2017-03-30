// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#define DEBUG
#define TRACE
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Runtime.InteropServices;
using System.Globalization;

namespace System.Diagnostics
{
    /// <devdoc>
    ///    <para>Provides
    ///       the default output methods and behavior for tracing.</para>
    /// </devdoc>
    public class DefaultTraceListener : TraceListener
    {
        private const int InternalWriteSize = 16384;
        private bool _assertUIEnabled; 
        private bool _settingsInitialized;
        private string _logFileName;

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

        public string LogFileName 
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
        public override void Fail(string message)
        {
            Fail(message, null);
        }

        /// <devdoc>
        ///    <para>
        ///       Emits or displays messages and a stack trace for an assertion that
        ///       always fails.
        ///    </para>
        /// </devdoc>
        public override void Fail(string message, string detailMessage)
        {
            // UIAssert is not enabled.
            WriteAssert(String.Empty, message, detailMessage);
        }

         private void InitializeSettings() 
         {
            // don't use the property setters here to avoid infinite recursion.
            _assertUIEnabled = DiagnosticsConfiguration.AssertUIEnabled;
            _logFileName = DiagnosticsConfiguration.LogFileName;
            _settingsInitialized = true;
        }

        private void WriteAssert(string stackTrace, string message, string detailMessage)
        {
            string assertMessage = SR.DebugAssertBanner + Environment.NewLine
                                            + SR.DebugAssertShortMessage + Environment.NewLine
                                            + message + Environment.NewLine
                                            + SR.DebugAssertLongMessage + Environment.NewLine +
                                            detailMessage + Environment.NewLine
                                            + stackTrace;
            WriteLine(assertMessage);

            // In case the debugger is attached we break the debugger.
            if (Debugger.IsAttached)
                Debugger.Break();
        }

        /// <devdoc>
        ///    <para>
        ///       Writes the output using <see cref="System.Diagnostics.Debug.Write"/>.
        ///    </para>
        /// </devdoc>
        public override void Write(string message)
        {
            Write(message, useLogFile: true);
        }

        /// <devdoc>
        ///    <para>
        ///       Writes the output followed by a line terminator using <see cref="System.Diagnostics.Debug.Write"/>.
        ///    </para>
        /// </devdoc>
        public override void WriteLine(string message)
        {
            WriteLine(message, useLogFile: true);
        }

        private void WriteLine(string message, bool useLogFile)
        {
            if (NeedIndent) 
                WriteIndent();

            // The concat is done here to enable a single call to Write
            Write(message + Environment.NewLine, useLogFile); 
            NeedIndent = true;
        }

        private void Write(string message, bool useLogFile)
        {
            if (NeedIndent) 
                WriteIndent();

            // really huge messages mess up both VS and dbmon, so we chop it up into 
            // reasonable chunks if it's too big
            if (message == null || message.Length <= InternalWriteSize)
            {
                Debug.Write(message);
            }
            else
            {
                int offset;
                for (offset = 0; offset < message.Length - InternalWriteSize; offset += InternalWriteSize)
                {
                    Debug.Write(message.Substring(offset, InternalWriteSize));
                }
                Debug.Write(message.Substring(offset));
            }

            if (useLogFile && !string.IsNullOrEmpty(LogFileName))
                WriteToLogFile(message);
        }

        private void WriteToLogFile(string message)
        {
            try
            {
                File.AppendAllText(LogFileName, message);
            }
            catch (Exception e)
            {
                WriteLine(string.Format(SR.ExceptionOccurred, LogFileName, e.ToString()), useLogFile: false);
            }
        }
    }
}
