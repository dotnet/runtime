// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace System.Composition.Diagnostics
{
    internal sealed class DebuggerTraceWriter : TraceWriter
    {
        private static readonly string s_sourceName = typeof(DebuggerTraceWriter).Assembly.GetName().Name!;

        public override bool CanWriteInformation
        {
            get { return false; }
        }

        public override bool CanWriteWarning
        {
            get { return Debugger.IsLogging(); }
        }

        public override bool CanWriteError
        {
            get { return Debugger.IsLogging(); }
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
            if (!Debugger.IsLogging())
            {
                return;
            }

            string logMessage = CreateLogMessage(eventType, traceId, format, arguments);
            Debugger.Log(0, null, logMessage);
        }

        private static string CreateLogMessage(TraceEventType eventType, CompositionTraceId traceId, string format, params object[] arguments)
        {
            StringBuilder messageBuilder = new StringBuilder();

            // Format taken from TraceListener.TraceEvent in .NET Framework
            messageBuilder.Append($"{s_sourceName} {eventType}: {(int)traceId} : ");

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

        public enum TraceEventType
        {
            Error = 2,
            Warning = 4,
            Information = 8,
        }
    }
}
