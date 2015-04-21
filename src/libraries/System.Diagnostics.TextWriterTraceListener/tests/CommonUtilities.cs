// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.IO;
using System.Text;

namespace System.Diagnostics.TextWriterTraceListenerTests
{
    internal static class CommonUtilities
    {
        private const string DefaultDelimiter = ";";

        internal static void DeleteFile(string fileName)
        {
            if (File.Exists(fileName))
                File.Delete(fileName);
        }

        internal static string ExpectedTraceEventOutput(TraceFilter filter, TraceEventCache cache, string source, TraceEventType eventType, int id, string format, object[] args)
        {
            if (filter != null && !filter.ShouldTrace(cache, source, eventType, id, format, args, null, null))
                return string.Empty;

            var builder = new StringBuilder();
            builder.AppendHeader(source, eventType, id);
            builder.Append(EscapedString(args != null ? string.Format(format, args) : format));
            builder.Append(DefaultDelimiter);
            builder.Append(DefaultDelimiter);
            builder.AppendTraceEventCache(cache);
            return builder.AppendLine().ToString();
        }

        internal static string ExpectedTraceDataOutput(TraceFilter filter, TraceEventCache cache, string source, TraceEventType eventType, int id, object data)
        {
            if (filter != null && !filter.ShouldTrace(cache, source, eventType, id, null, null, data, null))
                return string.Empty;

            var builder = new StringBuilder();
            builder.AppendHeader(source, eventType, id);
            builder.Append(DefaultDelimiter);
            builder.Append(EscapedString(data.ToString()));
            builder.Append(DefaultDelimiter);
            builder.AppendTraceEventCache(cache);
            return builder.AppendLine().ToString();
        }

        internal static string ExpectedTraceDataOutput(string delimiter, TraceFilter filter, TraceEventCache cache, string source, TraceEventType eventType, int id, object[] data)
        {
            if (filter != null && !filter.ShouldTrace(cache, source, eventType, id, null, null, data, null))
                return string.Empty;

            string secondDelimiter = delimiter == "," ? DefaultDelimiter : ",";
            var builder = new StringBuilder();
            builder.AppendHeader(source, eventType, id, delimiter);
            builder.Append(delimiter);
            if (data != null)
            {
                for (int i = 0; i < data.Length; ++i)
                {
                    if (i != 0)
                        builder.Append(secondDelimiter);
                    builder.Append(EscapedString(data[i].ToString()));
                }
            }
            builder.Append(delimiter);
            builder.AppendTraceEventCache(cache, delimiter);

            return builder.AppendLine().ToString();
        }

        private static void AppendHeader(this StringBuilder builder, string source, TraceEventType eventType, int id, string delimiter = DefaultDelimiter)
        {
            builder.Append(EscapedString(source));
            builder.Append(delimiter);
            builder.Append(eventType.ToString());
            builder.Append(delimiter);
            builder.Append(id.ToString(CultureInfo.InvariantCulture));
            builder.Append(delimiter);
        }

        private static void AppendTraceEventCache(this StringBuilder builder, TraceEventCache cache, string delimiter = DefaultDelimiter)
        {
            if (cache != null)
            {
                builder.Append(cache.ProcessId);
                builder.Append(delimiter);
                builder.Append(EscapedString(cache.ThreadId));
                builder.Append(delimiter);
                builder.Append(EscapedString(cache.DateTime.ToString("o", CultureInfo.InvariantCulture)));
                builder.Append(delimiter);
                builder.Append(cache.Timestamp.ToString(CultureInfo.InvariantCulture));
                builder.Append(delimiter);
            }
            else
            {
                for (int i = 0; i < 5; ++i)
                    builder.Append(delimiter);
            }
        }

        private static string EscapedString(string str)
        {
            int index;
            int lastindex = 0;
            StringBuilder sb = new StringBuilder("\"");
            while ((index = str.IndexOf('"', lastindex)) != -1)
            {
                sb.Append(str, lastindex, index - lastindex);
                sb.Append("\"\"");
                lastindex = index + 1;
            }

            sb.Append(str, lastindex, str.Length - lastindex);
            sb.Append("\"");
            return sb.ToString();
        }
    }
}
