// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Formatter to convert the named format items like {NamedformatItem} to <see cref="string.Format(IFormatProvider, string, object)"/> format.
    /// </summary>
    internal sealed class LogValuesFormatter
    {
        private const string NullValue = "(null)";
        private readonly List<string> _valueNames = new List<string>();
#if NET8_0_OR_GREATER
        private readonly CompositeFormat _format;
#else
        private readonly string _format;
#endif

        // NOTE: If this assembly ever builds for netcoreapp, the below code should change to:
        // - Be annotated as [SkipLocalsInit] to avoid zero'ing the stackalloc'd char span
        // - Format _valueNames.Count directly into a span

        public LogValuesFormatter(string format)
        {
            ThrowHelper.ThrowIfNull(format);

            OriginalFormat = format;

            var vsb = new ValueStringBuilder(stackalloc char[256]);
            int scanIndex = 0;
            int endIndex = format.Length;

            while (scanIndex < endIndex)
            {
                int openBraceIndex = FindBraceIndex(format, '{', scanIndex, endIndex);
                if (scanIndex == 0 && openBraceIndex == endIndex)
                {
                    // No holes found.
                    _format =
#if NET8_0_OR_GREATER
                        CompositeFormat.Parse(format);
#else
                        format;
#endif
                    return;
                }

                int closeBraceIndex = FindBraceIndex(format, '}', openBraceIndex, endIndex);

                if (closeBraceIndex == endIndex)
                {
                    vsb.Append(format.AsSpan(scanIndex, endIndex - scanIndex));
                    scanIndex = endIndex;
                }
                else
                {
                    // Format item syntax : { index[,alignment][ :formatString] }.
                    int formatDelimiterIndex = format.AsSpan(openBraceIndex, closeBraceIndex - openBraceIndex).IndexOfAny(',', ':');
                    formatDelimiterIndex = formatDelimiterIndex < 0 ? closeBraceIndex : formatDelimiterIndex + openBraceIndex;

                    vsb.Append(format.AsSpan(scanIndex, openBraceIndex - scanIndex + 1));
                    vsb.Append(_valueNames.Count.ToString());
                    _valueNames.Add(format.Substring(openBraceIndex + 1, formatDelimiterIndex - openBraceIndex - 1));
                    vsb.Append(format.AsSpan(formatDelimiterIndex, closeBraceIndex - formatDelimiterIndex + 1));

                    scanIndex = closeBraceIndex + 1;
                }
            }

            _format =
#if NET8_0_OR_GREATER
                CompositeFormat.Parse(vsb.ToString());
#else
                vsb.ToString();
#endif
        }

        public string OriginalFormat { get; private set; }
        public List<string> ValueNames => _valueNames;

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

        public string Format(object?[]? values)
        {
            object?[]? formattedValues = values;

            if (values != null)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    object formattedValue = FormatArgument(values[i]);
                    // If the formatted value is changed, we allocate and copy items to a new array to avoid mutating the array passed in to this method
                    if (!ReferenceEquals(formattedValue, values[i]))
                    {
                        formattedValues = new object[values.Length];
                        Array.Copy(values, formattedValues, i);
                        formattedValues[i++] = formattedValue;
                        for (; i < values.Length; i++)
                        {
                            formattedValues[i] = FormatArgument(values[i]);
                        }
                        break;
                    }
                }
            }

            return string.Format(CultureInfo.InvariantCulture, _format, formattedValues ?? Array.Empty<object>());
        }

        // NOTE: This method mutates the items in the array if needed to avoid extra allocations, and should only be used when caller expects this to happen
        internal string FormatWithOverwrite(object?[]? values)
        {
            if (values != null)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = FormatArgument(values[i]);
                }
            }

            return string.Format(CultureInfo.InvariantCulture, _format, values ?? Array.Empty<object>());
        }

        internal string Format()
        {
#if NET8_0_OR_GREATER
            return _format.Format;
#else
            return _format;
#endif
        }

#if NET8_0_OR_GREATER
        internal string Format<TArg0>(TArg0 arg0)
        {
            object? arg0String = null;
            return
                !TryFormatArgumentIfNullOrEnumerable(arg0, ref arg0String) ?
                string.Format(CultureInfo.InvariantCulture, _format, arg0) :
                string.Format(CultureInfo.InvariantCulture, _format, arg0String);
        }

        internal string Format<TArg0, TArg1>(TArg0 arg0, TArg1 arg1)
        {
            object? arg0String = null, arg1String = null;
            return
                !TryFormatArgumentIfNullOrEnumerable(arg0, ref arg0String) &&
                !TryFormatArgumentIfNullOrEnumerable(arg1, ref arg1String) ?
                string.Format(CultureInfo.InvariantCulture, _format, arg0, arg1) :
                string.Format(CultureInfo.InvariantCulture, _format, arg0String ?? arg0, arg1String ?? arg1);
        }

        internal string Format<TArg0, TArg1, TArg2>(TArg0 arg0, TArg1 arg1, TArg2 arg2)
        {
            object? arg0String = null, arg1String = null, arg2String = null;
            return
                !TryFormatArgumentIfNullOrEnumerable(arg0, ref arg0String) &&
                !TryFormatArgumentIfNullOrEnumerable(arg1, ref arg1String) &&
                !TryFormatArgumentIfNullOrEnumerable(arg2, ref arg2String) ?
                string.Format(CultureInfo.InvariantCulture, _format, arg0, arg1, arg2) :
                string.Format(CultureInfo.InvariantCulture, _format, arg0String ?? arg0, arg1String ?? arg1, arg2String ?? arg2);
        }
#else
        internal string Format(object? arg0) =>
            string.Format(CultureInfo.InvariantCulture, _format, FormatArgument(arg0));

        internal string Format(object? arg0, object? arg1) =>
            string.Format(CultureInfo.InvariantCulture, _format, FormatArgument(arg0), FormatArgument(arg1));

        internal string Format(object? arg0, object? arg1, object? arg2) =>
            string.Format(CultureInfo.InvariantCulture, _format, FormatArgument(arg0), FormatArgument(arg1), FormatArgument(arg2));
#endif

        public KeyValuePair<string, object?> GetValue(object?[] values, int index)
        {
            if (index < 0 || index > _valueNames.Count)
            {
                throw new IndexOutOfRangeException(nameof(index));
            }

            if (_valueNames.Count > index)
            {
                return new KeyValuePair<string, object?>(_valueNames[index], values[index]);
            }

            return new KeyValuePair<string, object?>("{OriginalFormat}", OriginalFormat);
        }

        public IEnumerable<KeyValuePair<string, object?>> GetValues(object[] values)
        {
            var valueArray = new KeyValuePair<string, object?>[values.Length + 1];
            for (int index = 0; index != _valueNames.Count; ++index)
            {
                valueArray[index] = new KeyValuePair<string, object?>(_valueNames[index], values[index]);
            }

            valueArray[valueArray.Length - 1] = new KeyValuePair<string, object?>("{OriginalFormat}", OriginalFormat);
            return valueArray;
        }

        private static object FormatArgument(object? value)
        {
            object? stringValue = null;
            return TryFormatArgumentIfNullOrEnumerable(value, ref stringValue) ? stringValue : value!;
        }

        private static bool TryFormatArgumentIfNullOrEnumerable<T>(T? value, [NotNullWhen(true)] ref object? stringValue)
        {
            if (value == null)
            {
                stringValue = NullValue;
                return true;
            }

            // if the value implements IEnumerable but isn't itself a string, build a comma separated string.
            if (value is not string && value is IEnumerable enumerable)
            {
                var vsb = new ValueStringBuilder(stackalloc char[256]);
                bool first = true;
                foreach (object? e in enumerable)
                {
                    if (!first)
                    {
                        vsb.Append(", ");
                    }

                    vsb.Append(e != null ? e.ToString() : NullValue);
                    first = false;
                }
                stringValue = vsb.ToString();
                return true;
            }

            return false;
        }
    }
}
