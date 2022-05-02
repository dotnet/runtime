// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.Versioning;
using System.Text;
using System.Xml;
using System.Xml.XPath;

namespace System.Diagnostics
{
    public class XmlWriterTraceListener : TextWriterTraceListener
    {
        private const string FixedHeader = "<E2ETraceEvent xmlns=\"http://schemas.microsoft.com/2004/06/E2ETraceEvent\"><System xmlns=\"http://schemas.microsoft.com/2004/06/windows/eventlog/system\">";

        private static volatile string? s_processName;
        private readonly string _machineName = Environment.MachineName;
        private StringBuilder? _strBldr;
        private XmlTextWriter? _xmlBlobWriter;

        public XmlWriterTraceListener(Stream stream)
            : base(stream)
        {
        }

        public XmlWriterTraceListener(Stream stream, string? name)
            : base(stream, name)
        {
        }

        public XmlWriterTraceListener(TextWriter writer)
            : base(writer)
        {
        }

        public XmlWriterTraceListener(TextWriter writer, string? name)
            : base(writer, name)
        {
        }

        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public XmlWriterTraceListener(string? filename)
            : base(filename)
        {
        }

        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public XmlWriterTraceListener(string? filename, string? name)
            : base(filename, name)
        {
        }

        public override void Write(string? message)
        {
            WriteLine(message);
        }

        public override void WriteLine(string? message)
        {
            TraceEvent(null, SR.TraceAsTraceSource, TraceEventType.Information, 0, message);
        }

        public override void Fail(string? message, string? detailMessage)
        {
            message ??= string.Empty;
            int length = detailMessage != null ? message.Length + 1 + detailMessage.Length : message.Length;
            TraceEvent(null, SR.TraceAsTraceSource, TraceEventType.Error, 0, string.Create(length, (message, detailMessage),
            (dst, v) =>
            {
                string prefix = v.message;
                prefix.CopyTo(dst);

                if (v.detailMessage != null)
                {
                    dst[prefix.Length] = ' ';

                    string detail = v.detailMessage;
                    detail.CopyTo(dst.Slice(prefix.Length + 1, detail.Length));
                }
            }));
        }

        public override void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string? format, params object?[]? args)
        {
            if (Filter != null && !Filter.ShouldTrace(eventCache, source, eventType, id, format, args, null, null))
                return;

            WriteHeader(source, eventType, id, eventCache);
            WriteEscaped(args != null && args.Length != 0 ? string.Format(CultureInfo.InvariantCulture, format!, args) : format);
            WriteFooter(eventCache);
        }

        public override void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, string? message)
        {
            if (Filter != null && !Filter.ShouldTrace(eventCache, source, eventType, id, message, null, null, null))
                return;

            WriteHeader(source, eventType, id, eventCache);
            WriteEscaped(message);
            WriteFooter(eventCache);
        }

        public override void TraceData(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, object? data)
        {
            if (Filter != null && !Filter.ShouldTrace(eventCache, source, eventType, id, null, null, data, null))
                return;

            WriteHeader(source, eventType, id, eventCache);

            InternalWrite("<TraceData>");
            if (data != null)
            {
                InternalWrite("<DataItem>");
                WriteData(data);
                InternalWrite("</DataItem>");
            }
            InternalWrite("</TraceData>");

            WriteFooter(eventCache);
        }

        public override void TraceData(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, params object?[]? data)
        {
            if (Filter != null && !Filter.ShouldTrace(eventCache, source, eventType, id, null, null, null, data))
                return;

            WriteHeader(source, eventType, id, eventCache);
            InternalWrite("<TraceData>");
            if (data != null)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    InternalWrite("<DataItem>");
                    if (data[i] != null)
                    {
                        WriteData(data[i]!);
                    }
                    InternalWrite("</DataItem>");
                }
            }
            InternalWrite("</TraceData>");

            WriteFooter(eventCache);
        }

        // Special case XPathNavigator dataitems to write out XML blob unescaped
        private void WriteData(object data)
        {
            if (!(data is XPathNavigator xmlBlob))
            {
                WriteEscaped(data.ToString());
            }
            else
            {
                if (_strBldr == null)
                {
                    _strBldr = new StringBuilder();
                    _xmlBlobWriter = new XmlTextWriter(new StringWriter(_strBldr, CultureInfo.CurrentCulture));
                }
                else
                {
                    _strBldr.Length = 0;
                }

                try
                {
                    // Rewind the blob to point to the root, this is needed to support multiple XMLTL in one TraceData call
                    xmlBlob.MoveToRoot();
                    _xmlBlobWriter!.WriteNode(xmlBlob, false);
                    InternalWrite(_strBldr);
                }
                catch (Exception)
                {
                    InternalWrite(data.ToString());
                }
            }
        }

        public override void Close()
        {
            base.Close();
            _xmlBlobWriter?.Close();
            _xmlBlobWriter = null;
            _strBldr = null;
        }

        public override void TraceTransfer(TraceEventCache? eventCache, string source, int id, string? message, Guid relatedActivityId)
        {
            if (Filter != null && !Filter.ShouldTrace(eventCache, source, TraceEventType.Transfer, id, message, null, null, null))
                return;

            WriteHeader(source, TraceEventType.Transfer, id, eventCache, relatedActivityId);
            WriteEscaped(message);
            WriteFooter(eventCache);
        }

        private void WriteHeader(string source, TraceEventType eventType, int id, TraceEventCache? eventCache, Guid relatedActivityId)
        {
            WriteStartHeader(source, eventType, id, eventCache);
            InternalWrite("\" RelatedActivityID=\"");
            InternalWrite(relatedActivityId);
            WriteEndHeader();
        }

        private void WriteHeader(string source, TraceEventType eventType, int id, TraceEventCache? eventCache)
        {
            WriteStartHeader(source, eventType, id, eventCache);
            WriteEndHeader();
        }

        private void WriteStartHeader(string source, TraceEventType eventType, int id, TraceEventCache? eventCache)
        {
            InternalWrite(FixedHeader);

            InternalWrite("<EventID>");
            InternalWrite((uint)id);
            InternalWrite("</EventID>");

            InternalWrite("<Type>3</Type>");

            InternalWrite("<SubType Name=\"");
            InternalWrite(eventType.ToString());
            InternalWrite("\">0</SubType>");

            InternalWrite("<Level>");
            InternalWrite(Math.Clamp((int)eventType, 0, 255));
            InternalWrite("</Level>");

            InternalWrite("<TimeCreated SystemTime=\"");
            InternalWrite(eventCache != null ? eventCache.DateTime : DateTime.Now);
            InternalWrite("\" />");

            InternalWrite("<Source Name=\"");
            WriteEscaped(source);
            InternalWrite("\" />");

            InternalWrite("<Correlation ActivityID=\"");
            InternalWrite(eventCache != null ? Trace.CorrelationManager.ActivityId : Guid.Empty);
        }

        [ResourceExposure(ResourceScope.None)]
        [ResourceConsumption(ResourceScope.Process, ResourceScope.Process)]
        private void WriteEndHeader()
        {
            string? processName = s_processName;
            if (processName is null)
            {
                if (OperatingSystem.IsBrowser()) // Process isn't supported on Browser
                {
                    s_processName = processName = string.Empty;
                }
                else
                {
                    using Process process = Process.GetCurrentProcess();
                    s_processName = processName = process.ProcessName;
                }
            }

            InternalWrite("\" />");

            InternalWrite("<Execution ProcessName=\"");
            InternalWrite(processName);
            InternalWrite("\" ProcessID=\"");
            InternalWrite((uint)Environment.ProcessId);
            InternalWrite("\" ThreadID=\"");
            InternalWrite((uint)Environment.CurrentManagedThreadId);
            InternalWrite("\" />");

            InternalWrite("<Channel/>");

            InternalWrite("<Computer>");
            InternalWrite(_machineName);
            InternalWrite("</Computer>");

            InternalWrite("</System>");

            InternalWrite("<ApplicationData>");
        }

        private void WriteFooter(TraceEventCache? eventCache)
        {
            if (eventCache != null)
            {
                bool writeLogicalOps = IsEnabled(TraceOptions.LogicalOperationStack);
                bool writeCallstack = IsEnabled(TraceOptions.Callstack);

                if (writeLogicalOps || writeCallstack)
                {
                    InternalWrite("<System.Diagnostics xmlns=\"http://schemas.microsoft.com/2004/08/System.Diagnostics\">");

                    if (writeLogicalOps)
                    {
                        InternalWrite("<LogicalOperationStack>");
                        foreach (object? correlationId in eventCache.LogicalOperationStack)
                        {
                            InternalWrite("<LogicalOperation>");
                            WriteEscaped(correlationId?.ToString());
                            InternalWrite("</LogicalOperation>");
                        }
                        InternalWrite("</LogicalOperationStack>");
                    }

                    InternalWrite("<Timestamp>");
                    InternalWrite(eventCache.Timestamp);
                    InternalWrite("</Timestamp>");

                    if (writeCallstack)
                    {
                        InternalWrite("<Callstack>");
                        WriteEscaped(eventCache.Callstack);
                        InternalWrite("</Callstack>");
                    }

                    InternalWrite("</System.Diagnostics>");
                }
            }

            InternalWrite("</ApplicationData></E2ETraceEvent>");
        }

        private void WriteEscaped(string? str)
        {
            if (string.IsNullOrEmpty(str))
                return;

            int lastIndex = 0;
            for (int i = 0; i < str.Length; i++)
            {
                switch (str[i])
                {
                    case '&':
                        InternalWrite(str.AsSpan(lastIndex, i - lastIndex));
                        InternalWrite("&amp;");
                        lastIndex = i + 1;
                        break;
                    case '<':
                        InternalWrite(str.AsSpan(lastIndex, i - lastIndex));
                        InternalWrite("&lt;");
                        lastIndex = i + 1;
                        break;
                    case '>':
                        InternalWrite(str.AsSpan(lastIndex, i - lastIndex));
                        InternalWrite("&gt;");
                        lastIndex = i + 1;
                        break;
                    case '"':
                        InternalWrite(str.AsSpan(lastIndex, i - lastIndex));
                        InternalWrite("&quot;");
                        lastIndex = i + 1;
                        break;
                    case '\'':
                        InternalWrite(str.AsSpan(lastIndex, i - lastIndex));
                        InternalWrite("&apos;");
                        lastIndex = i + 1;
                        break;
                    case (char)0xD:
                        InternalWrite(str.AsSpan(lastIndex, i - lastIndex));
                        InternalWrite("&#xD;");
                        lastIndex = i + 1;
                        break;
                    case (char)0xA:
                        InternalWrite(str.AsSpan(lastIndex, i - lastIndex));
                        InternalWrite("&#xA;");
                        lastIndex = i + 1;
                        break;
                }
            }
            InternalWrite(str.AsSpan(lastIndex, str.Length - lastIndex));
        }

        private void InternalWrite(string? message)
        {
            EnsureWriter();
            _writer?.Write(message);
        }

        private void InternalWrite(ReadOnlySpan<char> message)
        {
            EnsureWriter();
            _writer?.Write(message);
        }

        private void InternalWrite<T>(T message) where T : ISpanFormattable
        {
            Debug.Assert(typeof(T) == typeof(int) || typeof(T) == typeof(uint) || typeof(T) == typeof(long), "We only currently stackalloc enough space for these types.");

            EnsureWriter();
            if (_writer is TextWriter writer)
            {
                Span<char> span = stackalloc char[20]; // max length of longest formatted long with invariant culture
                message.TryFormat(span, out int charsWritten, format: default, provider: CultureInfo.InvariantCulture);
                Debug.Assert(charsWritten > 0);
                writer.Write(span.Slice(0, charsWritten));
            }
        }

        private void InternalWrite(Guid message)
        {
            EnsureWriter();
            if (_writer is TextWriter writer)
            {
                Span<char> span = stackalloc char[38]; // length of a Guid formatted as "B"
                message.TryFormat(span, out int charsWritten, format: "B");
                Debug.Assert(charsWritten == span.Length);
                writer.Write(span);
            }
        }

        private void InternalWrite(DateTime message)
        {
            EnsureWriter();
            if (_writer is TextWriter writer)
            {
                Span<char> span = stackalloc char[33]; // max length of a DateTime formatted as "o"
                message.TryFormat(span, out int charsWritten, format: "o");
                Debug.Assert(charsWritten > 0);
                writer.Write(span.Slice(0, charsWritten));
            }
        }

        private void InternalWrite(StringBuilder message)
        {
            EnsureWriter();
            if (_writer is TextWriter writer)
            {
                foreach (ReadOnlyMemory<char> chunk in message.GetChunks())
                {
                    writer.Write(chunk.Span);
                }
            }
        }
    }
}
