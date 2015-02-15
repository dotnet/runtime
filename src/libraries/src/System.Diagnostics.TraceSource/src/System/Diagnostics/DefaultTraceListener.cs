// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        private const int internalWriteSize = 16384;


        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.Diagnostics.DefaultTraceListener'/> class with 
        ///    Default as its <see cref='System.Diagnostics.TraceListener.Name'/>.</para>
        /// </devdoc>
        public DefaultTraceListener()
            : base("Default")
        {
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
            if (NeedIndent) WriteIndent();

            // really huge messages mess up both VS and dbmon, so we chop it up into 
            // reasonable chunks if it's too big
            if (message == null || message.Length <= internalWriteSize)
            {
                Debug.Write(message);
            }
            else
            {
                int offset;
                for (offset = 0; offset < message.Length - internalWriteSize; offset += internalWriteSize)
                {
                    Debug.Write(message.Substring(offset, internalWriteSize));
                }
                Debug.Write(message.Substring(offset));
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Writes the output followed by a line terminator using <see cref="System.Diagnostics.Debug.Write"/>.
        ///    </para>
        /// </devdoc>
        public override void WriteLine(string message)
        {
            if (NeedIndent) WriteIndent();
            // I do the concat here to make sure it goes as one call to the output.
            // we would save a stringbuilder operation by calling Write twice.
            Write(message + Environment.NewLine);
            NeedIndent = true;
        }
    }
}
