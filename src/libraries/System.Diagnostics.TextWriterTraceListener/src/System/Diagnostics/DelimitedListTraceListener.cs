// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;

namespace System.Diagnostics
{
    public class DelimitedListTraceListener : TextWriterTraceListener
    {
        private const string DelimiterKey = "delimiter";

        private string _delimiter = ";";
        private string _secondaryDelim = ",";
        private bool _initializedDelim;

        public DelimitedListTraceListener(Stream stream) : base(stream)
        {
        }

        public DelimitedListTraceListener(Stream stream, string? name) : base(stream, name)
        {
        }

        public DelimitedListTraceListener(TextWriter writer) : base(writer)
        {
        }

        public DelimitedListTraceListener(TextWriter writer, string? name) : base(writer, name)
        {
        }

        public DelimitedListTraceListener(string? fileName) : base(fileName)
        {
        }

        public DelimitedListTraceListener(string? fileName, string? name) : base(fileName, name)
        {
        }

        public string Delimiter
        {
            get
            {
                lock (this)
                {
                    if (!_initializedDelim)
                    {
                        if (Attributes.ContainsKey(DelimiterKey))
                        {
                            string? delimiter = Attributes[DelimiterKey];
                            if (!string.IsNullOrEmpty(delimiter))
                            {
                                _delimiter = delimiter;
                            }
                        }
                        _initializedDelim = true;
                    }
                }
                return _delimiter;
            }
            set
            {
                ArgumentException.ThrowIfNullOrEmpty(value, nameof(Delimiter));

                lock (this)
                {
                    _delimiter = value;
                    _initializedDelim = true;
                }

                if (_delimiter == ",")
                    _secondaryDelim = ";";
                else
                    _secondaryDelim = ",";
            }
        }

        // base class method is protected internal but since its base class is in another assembly can't override it as protected internal because a CS0507
        // warning would be hitted.
        protected override string[] GetSupportedAttributes() => new string[] { DelimiterKey };

        public override void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string? format, params object?[]? args)
        {
            if (Filter != null && !Filter.ShouldTrace(eventCache, source, eventType, id, format, args, null, null))
                return;

            WriteHeader(source, eventType, id);

            if (args != null)
                WriteEscaped(string.Format(CultureInfo.InvariantCulture, format!, args));
            else
                WriteEscaped(format);
            Write(Delimiter); // Use get_Delimiter

            // one more delimiter for the data object
            Write(Delimiter); // Use get_Delimiter

            WriteFooter(eventCache);
        }

        public override void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, string? message)
        {
            if (Filter != null && !Filter.ShouldTrace(eventCache, source, eventType, id, message, null, null, null))
                return;

            WriteHeader(source, eventType, id);

            WriteEscaped(message);
            Write(Delimiter); // Use get_Delimiter

            // one more delimiter for the data object
            Write(Delimiter); // Use get_Delimiter

            WriteFooter(eventCache);
        }

        public override void TraceData(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, object? data)
        {
            if (Filter != null && !Filter.ShouldTrace(eventCache, source, eventType, id, null, null, data, null))
                return;

            WriteHeader(source, eventType, id);

            // first a delimiter for the message
            Write(Delimiter); // Use get_Delimiter

            WriteEscaped(data?.ToString());
            Write(Delimiter); // Use get_Delimiter

            WriteFooter(eventCache);
        }

        public override void TraceData(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, params object?[]? data)
        {
            if (Filter != null && !Filter.ShouldTrace(eventCache, source, eventType, id, null, null, null, data))
                return;

            WriteHeader(source, eventType, id);

            // first a delimiter for the message
            Write(Delimiter); // Use get_Delimiter

            if (data != null)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    if (i != 0)
                        Write(_secondaryDelim);
                    WriteEscaped(data[i]?.ToString());
                }
            }
            Write(Delimiter); // Use get_Delimiter

            WriteFooter(eventCache);
        }

        private void WriteHeader(string source, TraceEventType eventType, int id)
        {
            WriteEscaped(source);
            Write(Delimiter); // Use get_Delimiter

            Write(eventType.ToString());
            Write(Delimiter); // Use get_Delimiter

            Write(id.ToString(CultureInfo.InvariantCulture));
            Write(Delimiter); // Use get_Delimiter
        }

        private void WriteFooter(TraceEventCache? eventCache)
        {
            if (eventCache != null)
            {
                if (IsEnabled(TraceOptions.ProcessId))
                    Write(eventCache.ProcessId.ToString(CultureInfo.InvariantCulture));
                Write(Delimiter); // Use get_Delimiter

                if (IsEnabled(TraceOptions.LogicalOperationStack))
                    WriteStackEscaped(eventCache.LogicalOperationStack);
                Write(Delimiter); // Use get_Delimiter

                if (IsEnabled(TraceOptions.ThreadId))
                    WriteEscaped(eventCache.ThreadId);
                Write(Delimiter); // Use get_Delimiter

                if (IsEnabled(TraceOptions.DateTime))
                    WriteEscaped(eventCache.DateTime.ToString("o", CultureInfo.InvariantCulture));
                Write(Delimiter); // Use get_Delimiter

                if (IsEnabled(TraceOptions.Timestamp))
                    Write(eventCache.Timestamp.ToString(CultureInfo.InvariantCulture));
                Write(Delimiter); // Use get_Delimiter

                if (IsEnabled(TraceOptions.Callstack))
                    WriteEscaped(eventCache.Callstack);
            }
            else
            {
                for (int i = 0; i < 5; i++)
                    Write(Delimiter); // Use get_Delimiter
            }

            WriteLine("");
        }

        private void WriteEscaped(string? message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                StringBuilder sb = new StringBuilder("\"");
                EscapeMessage(message, sb);
                sb.Append('"');
                Write(sb.ToString());
            }
        }

        private void WriteStackEscaped(Stack stack)
        {
            StringBuilder sb = new StringBuilder("\"");
            bool first = true;
            foreach (object? obj in stack)
            {
                if (!first)
                {
                    sb.Append(", ");
                }
                else
                {
                    first = false;
                }

                string? operation = obj?.ToString();
                EscapeMessage(operation, sb);
            }

            sb.Append('"');
            Write(sb.ToString());
        }

        private static void EscapeMessage(string? message, StringBuilder sb)
        {
            if (!string.IsNullOrEmpty(message))
            {
                int index;
                int lastindex = 0;
                while ((index = message.IndexOf('"', lastindex)) != -1)
                {
                    sb.Append(message, lastindex, index - lastindex);
                    sb.Append("\"\"");
                    lastindex = index + 1;
                }

                sb.Append(message, lastindex, message.Length - lastindex);
            }
        }
    }
}
