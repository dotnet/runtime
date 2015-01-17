// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !FEATURE_TRACING
using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Microsoft.Composition.Diagnostics
{
    internal sealed class DebuggerTraceWriter : TraceWriter
    {
        private static readonly string s_sourceName = "System.Composition";

        public override bool CanWriteInformation
        {
            get { return false; }
        }

        public override bool CanWriteWarning
        {
            get { return false; /* Debugger.IsLogging(); */ }
        }

        public override bool CanWriteError
        {
            get { return false; /* return Debugger.IsLogging(); */ }
        }

        public override void WriteInformation(CompositionTraceId traceId, string format, params object[] arguments)
        {
            WriteEvent(TraceEventType.Information, traceId, format, arguments);
        }

        public override void WriteWarning(CompositionTraceId traceId, string format, params object[] arguments)
        {
            WriteEvent(TraceEventType.Warning, traceId, format, arguments);
        }

        public override void WriteError(CompositionTraceId traceId, string format, params object[] arguments)
        {
            WriteEvent(TraceEventType.Error, traceId, format, arguments);
        }

        private static void WriteEvent(TraceEventType eventType, CompositionTraceId traceId, string format, params object[] arguments)
        {
            if (true /* !Debugger.IsLogging() */)
            {
                return;
            }
            //            string logMessage = CreateLogMessage(eventType, traceId, format, arguments);
            /*Debugger.Log(0, null, logMessage);*/
        }

        internal static string CreateLogMessage(TraceEventType eventType, CompositionTraceId traceId, string format, params object[] arguments)
        {
            StringBuilder messageBuilder = new StringBuilder();

            // Format taken from TraceListener.TraceEvent in full framework
            messageBuilder.AppendFormat(CultureInfo.InvariantCulture, "{0} {1}: {2} : ",
                s_sourceName, eventType.ToString(), (int)traceId);

            if (arguments == null)
            {
                messageBuilder.Append(format);
            }
            else
            {
                messageBuilder.AppendFormat(CultureInfo.InvariantCulture, format, arguments);
            }

            messageBuilder.AppendLine();

            return messageBuilder.ToString();
        }

        // Copied from TraceEventType in full framework
        internal enum TraceEventType
        {
            Error = 2,
            Warning = 4,
            Information = 8,
        }
    }
}

#endif //!FEATURE_TRACING