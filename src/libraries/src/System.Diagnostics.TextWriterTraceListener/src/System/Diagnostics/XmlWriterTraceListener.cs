// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
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

        private readonly string _machineName = Environment.MachineName;
        private StringBuilder _strBldr = null;
        private XmlTextWriter _xmlBlobWriter = null;

        public XmlWriterTraceListener(Stream stream)
            : base(stream)
        {
        }

        public XmlWriterTraceListener(Stream stream, string name)
            : base(stream, name)
        {
        }

        public XmlWriterTraceListener(TextWriter writer)
            : base(writer)
        {
        }

        public XmlWriterTraceListener(TextWriter writer, string name)
            : base(writer, name)
        {
        }

        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public XmlWriterTraceListener(string filename)
            : base(filename)
        {
        }

        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        public XmlWriterTraceListener(string filename, string name)
            : base(filename, name)
        {
        }

        public override void Write(string message)
        {
            WriteLine(message);
        }

        public override void WriteLine(string message)
        {
            TraceEvent(null, SR.TraceAsTraceSource, TraceEventType.Information, 0, message);
        }

        public override void Fail(string message, string detailMessage)
        {
            int length = detailMessage != null ? message.Length + 1 + detailMessage.Length : message.Length;
            TraceEvent(null, SR.TraceAsTraceSource, TraceEventType.Error, 0, string.Create(length, (message, detailMessage),
            (dst, v) =>
            {
                ReadOnlySpan<char> prefix = v.message;
                prefix.CopyTo(dst);

                if (v.detailMessage != null)
                {
                    dst[prefix.Length] = ' ';

                    ReadOnlySpan<char> detail = v.detailMessage;
                    detail.CopyTo(dst.Slice(prefix.Length + 1, detail.Length));
                }
            }));
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
        {
            if (Filter != null && !Filter.ShouldTrace(eventCache, source, eventType, id, format, args, null, null))
                return;

            WriteHeader(source, eventType, id, eventCache);
            WriteEscaped(args != null && args.Length != 0 ? string.Format(CultureInfo.InvariantCulture, format, args) : format);
            WriteFooter(eventCache);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            if (Filter != null && !Filter.ShouldTrace(eventCache, source, eventType, id, message, null, null, null))
                return;

            WriteHeader(source, eventType, id, eventCache);
            WriteEscaped(message);
            WriteFooter(eventCache);
        }

        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, object data)
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

        public override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, params object[] data)
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
                        WriteData(data[i]);
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
                    _xmlBlobWriter.WriteNode(xmlBlob, false);
                    InternalWrite(_strBldr.ToString());
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

        public override void TraceTransfer(TraceEventCache eventCache, string source, int id, string message, Guid relatedActivityId)
        {
            if (Filter != null && !Filter.ShouldTrace(eventCache, source, TraceEventType.Transfer, id, message, null, null, null))
                return;

            WriteHeader(source, TraceEventType.Transfer, id, eventCache, relatedActivityId);
            WriteEscaped(message);
            WriteFooter(eventCache);
        }

        private void WriteHeader(string source, TraceEventType eventType, int id, TraceEventCache eventCache, Guid relatedActivityId)
        {
            WriteStartHeader(source, eventType, id, eventCache);
            InternalWrite("\" RelatedActivityID=\"");
            InternalWrite(relatedActivityId.ToString("B"));
            WriteEndHeader();
        }

        private void WriteHeader(string source, TraceEventType eventType, int id, TraceEventCache eventCache)
        {
            WriteStartHeader(source, eventType, id, eventCache);
            WriteEndHeader();
        }

        private void WriteStartHeader(string source, TraceEventType eventType, int id, TraceEventCache eventCache)
        {
            InternalWrite(FixedHeader);

            InternalWrite("<EventID>");
            InternalWrite(((uint)id).ToString(CultureInfo.InvariantCulture));
            InternalWrite("</EventID>");

            InternalWrite("<Type>3</Type>");

            InternalWrite("<SubType Name=\"");
            InternalWrite(eventType.ToString());
            InternalWrite("\">0</SubType>");

            InternalWrite("<Level>");
            int sev = (int)eventType;
            if (sev > 255)
                sev = 255;
            if (sev < 0)
                sev = 0;
            InternalWrite(sev.ToString(CultureInfo.InvariantCulture));
            InternalWrite("</Level>");

            InternalWrite("<TimeCreated SystemTime=\"");
            if (eventCache != null)
                InternalWrite(eventCache.DateTime.ToString("o", CultureInfo.InvariantCulture));
            else
                InternalWrite(DateTime.Now.ToString("o", CultureInfo.InvariantCulture));
            InternalWrite("\" />");

            InternalWrite("<Source Name=\"");
            WriteEscaped(source);
            InternalWrite("\" />");

            InternalWrite("<Correlation ActivityID=\"");
            if (eventCache != null)
                InternalWrite(Trace.CorrelationManager.ActivityId.ToString("B"));
            else
                InternalWrite(Guid.Empty.ToString("B"));
        }

        [ResourceExposure(ResourceScope.None)]
        [ResourceConsumption(ResourceScope.Process, ResourceScope.Process)]
        private void WriteEndHeader()
        {
            InternalWrite("\" />");

            InternalWrite("<Execution ProcessName=\"");
            InternalWrite(TraceListenerHelpers.GetProcessName());
            InternalWrite("\" ProcessID=\"");
            InternalWrite(((uint)TraceListenerHelpers.GetProcessId()).ToString(CultureInfo.InvariantCulture));
            InternalWrite("\" ThreadID=\"");
            WriteEscaped(TraceListenerHelpers.GetThreadId().ToString(CultureInfo.InvariantCulture));
            InternalWrite("\" />");

            InternalWrite("<Channel/>");

            InternalWrite("<Computer>");
            InternalWrite(_machineName);
            InternalWrite("</Computer>");

            InternalWrite("</System>");

            InternalWrite("<ApplicationData>");
        }

        private void WriteFooter(TraceEventCache eventCache)
        {
            bool writeLogicalOps = IsEnabled(TraceOptions.LogicalOperationStack);
            bool writeCallstack = IsEnabled(TraceOptions.Callstack);

            if (eventCache != null && (writeLogicalOps || writeCallstack))
            {
                InternalWrite("<System.Diagnostics xmlns=\"http://schemas.microsoft.com/2004/08/System.Diagnostics\">");

                if (writeLogicalOps)
                {
                    InternalWrite("<LogicalOperationStack>");

                    Stack s = eventCache.LogicalOperationStack;

                    foreach (object correlationId in s)
                    {
                        InternalWrite("<LogicalOperation>");
                        WriteEscaped(correlationId.ToString());
                        InternalWrite("</LogicalOperation>");
                    }
                    InternalWrite("</LogicalOperationStack>");
                }

                InternalWrite("<Timestamp>");
                InternalWrite(eventCache.Timestamp.ToString(CultureInfo.InvariantCulture));
                InternalWrite("</Timestamp>");

                if (writeCallstack)
                {
                    InternalWrite("<Callstack>");
                    WriteEscaped(eventCache.Callstack);
                    InternalWrite("</Callstack>");
                }

                InternalWrite("</System.Diagnostics>");
            }

            InternalWrite("</ApplicationData></E2ETraceEvent>");
        }

        private void WriteEscaped(string str)
        {
            if (string.IsNullOrEmpty(str))
                return;

            int lastIndex = 0;
            for (int i = 0; i < str.Length; i++)
            {
                switch (str[i])
                {
                    case '&':
                        InternalWrite(str.Substring(lastIndex, i - lastIndex));
                        InternalWrite("&amp;");
                        lastIndex = i + 1;
                        break;
                    case '<':
                        InternalWrite(str.Substring(lastIndex, i - lastIndex));
                        InternalWrite("&lt;");
                        lastIndex = i + 1;
                        break;
                    case '>':
                        InternalWrite(str.Substring(lastIndex, i - lastIndex));
                        InternalWrite("&gt;");
                        lastIndex = i + 1;
                        break;
                    case '"':
                        InternalWrite(str.Substring(lastIndex, i - lastIndex));
                        InternalWrite("&quot;");
                        lastIndex = i + 1;
                        break;
                    case '\'':
                        InternalWrite(str.Substring(lastIndex, i - lastIndex));
                        InternalWrite("&apos;");
                        lastIndex = i + 1;
                        break;
                    case (char)0xD:
                        InternalWrite(str.Substring(lastIndex, i - lastIndex));
                        InternalWrite("&#xD;");
                        lastIndex = i + 1;
                        break;
                    case (char)0xA:
                        InternalWrite(str.Substring(lastIndex, i - lastIndex));
                        InternalWrite("&#xA;");
                        lastIndex = i + 1;
                        break;
                }
            }
            InternalWrite(str.Substring(lastIndex, str.Length - lastIndex));
        }

        private void InternalWrite(string message)
        {
            EnsureWriter();

            if (_writer != null)
                _writer.Write(message);
        }
    }
}
