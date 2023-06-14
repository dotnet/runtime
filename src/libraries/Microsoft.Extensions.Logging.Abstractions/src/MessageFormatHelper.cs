// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Extensions.Logging
{

    /// <summary>
    /// Helper routines for parsing the ILogger message format. This is the same as composite format
    /// except holes are referenced by name instead of number.
    /// </summary>
    internal static class MessageFormatHelper
    {
        private static readonly char[] FormatDelimiters = { ',', ':' };

        internal static InternalCompositeFormat Parse(string messageFormat, out LogPropertyInfo[] properties)
        {
            var vsb = new ValueStringBuilder(stackalloc char[256]);
            List<LogPropertyInfo> propertyList = new List<LogPropertyInfo>();
            int scanIndex = 0;
            int endIndex = messageFormat.Length;

            while (scanIndex < endIndex)
            {
                int openBraceIndex = FindBraceIndex(messageFormat, '{', scanIndex, endIndex);
                if (scanIndex == 0 && openBraceIndex == endIndex)
                {
                    // No holes found.
                    properties = Array.Empty<LogPropertyInfo>();
                    return InternalCompositeFormat.Parse(messageFormat);
                }

                int closeBraceIndex = FindBraceIndex(messageFormat, '}', openBraceIndex, endIndex);

                if (closeBraceIndex == endIndex)
                {
                    vsb.Append(messageFormat.AsSpan(scanIndex, endIndex - scanIndex));
                    scanIndex = endIndex;
                }
                else
                {
                    // Format item syntax : { index[,alignment][ :formatString] }.
                    int formatDelimiterIndex = FindIndexOfAny(messageFormat, FormatDelimiters, openBraceIndex, closeBraceIndex);
                    int colonIndex = messageFormat.IndexOf(':', openBraceIndex, closeBraceIndex - openBraceIndex);

                    vsb.Append(messageFormat.AsSpan(scanIndex, openBraceIndex - scanIndex + 1));
                    vsb.Append(propertyList.Count.ToString());
                    string propName = messageFormat.Substring(openBraceIndex + 1, formatDelimiterIndex - openBraceIndex - 1);
                    propertyList.Add(new LogPropertyInfo(propName));
                    vsb.Append(messageFormat.AsSpan(formatDelimiterIndex, closeBraceIndex - formatDelimiterIndex + 1));
                    scanIndex = closeBraceIndex + 1;
                }
            }

            properties = propertyList.ToArray();
            return InternalCompositeFormat.Parse(vsb.ToString());
        }

        internal static InternalCompositeFormat Parse(string messageFormat)
        {
            var vsb = new ValueStringBuilder(stackalloc char[256]);
            int propertyCount = 0;
            int scanIndex = 0;
            int endIndex = messageFormat.Length;

            while (scanIndex < endIndex)
            {
                int openBraceIndex = FindBraceIndex(messageFormat, '{', scanIndex, endIndex);
                if (scanIndex == 0 && openBraceIndex == endIndex)
                {
                    // No holes found.
                    return InternalCompositeFormat.Parse(messageFormat);
                }

                int closeBraceIndex = FindBraceIndex(messageFormat, '}', openBraceIndex, endIndex);

                if (closeBraceIndex == endIndex)
                {
                    vsb.Append(messageFormat.AsSpan(scanIndex, endIndex - scanIndex));
                    scanIndex = endIndex;
                }
                else
                {
                    // Format item syntax : { index[,alignment][ :formatString] }.
                    int formatDelimiterIndex = FindIndexOfAny(messageFormat, FormatDelimiters, openBraceIndex, closeBraceIndex);
                    int colonIndex = messageFormat.IndexOf(':', openBraceIndex, closeBraceIndex - openBraceIndex);

                    vsb.Append(messageFormat.AsSpan(scanIndex, openBraceIndex - scanIndex + 1));
                    vsb.Append(propertyCount.ToString());
                    propertyCount++;
                    string propName = messageFormat.Substring(openBraceIndex + 1, formatDelimiterIndex - openBraceIndex - 1);
                    vsb.Append(messageFormat.AsSpan(formatDelimiterIndex, closeBraceIndex - formatDelimiterIndex + 1));
                    scanIndex = closeBraceIndex + 1;
                }
            }

            return InternalCompositeFormat.Parse(vsb.ToString());
        }

        private static int FindBraceIndex(string format, char brace, int startIndex, int endIndex)
        {
            // Example: {{prefix{{{Argument}}}suffix}}.
            int braceIndex = endIndex;
            int scanIndex = startIndex;
            int braceOccurrenceCount = 0;

            while (scanIndex < endIndex)
            {
                if (braceOccurrenceCount > 0 && format[scanIndex] != brace)
                {
                    if (braceOccurrenceCount % 2 == 0)
                    {
                        // Even number of '{' or '}' found. Proceed search with next occurrence of '{' or '}'.
                        braceOccurrenceCount = 0;
                        braceIndex = endIndex;
                    }
                    else
                    {
                        // An unescaped '{' or '}' found.
                        break;
                    }
                }
                else if (format[scanIndex] == brace)
                {
                    if (brace == '}')
                    {
                        if (braceOccurrenceCount == 0)
                        {
                            // For '}' pick the first occurrence.
                            braceIndex = scanIndex;
                        }
                    }
                    else
                    {
                        // For '{' pick the last occurrence.
                        braceIndex = scanIndex;
                    }

                    braceOccurrenceCount++;
                }

                scanIndex++;
            }

            return braceIndex;
        }

        private static int FindIndexOfAny(string format, char[] chars, int startIndex, int endIndex)
        {
            int findIndex = format.IndexOfAny(chars, startIndex, endIndex - startIndex);
            return findIndex == -1 ? endIndex : findIndex;
        }
    }
}
