// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.Extensions.Logging
{
    internal class LogValuesMetadata : LogValuesFormatter
    {
        public LogValuesMetadata(string format, LogLevel level, EventId eventId, Attribute[]?[]? attributes) : base(format, attributes)
        {
            LogLevel = level;
            EventId = eventId;
        }

        public string FinalFormat => CompositeFormat.Format;
        public LogLevel LogLevel { get; }
        public EventId EventId { get; }
    }

    /// <summary>
    /// Formatter to convert the named format items like {NamedformatItem} to <see cref="string.Format(IFormatProvider, string, object)"/> format.
    /// </summary>
    internal class LogValuesFormatter
    {
        private const string NullValue = "(null)";
        private static readonly char[] FormatDelimiters = { ',', ':' };
        private readonly LogPropertyMetadata[]? _metadata;
        private readonly InternalCompositeFormat _format;

        // NOTE: If this assembly ever builds for netcoreapp, the below code should change to:
        // - Be annotated as [SkipLocalsInit] to avoid zero'ing the stackalloc'd char span
        // - Format _valueNames.Count directly into a span

        public LogValuesFormatter(string format, Attribute[]?[]? attributes = null)
        {
            ThrowHelper.ThrowIfNull(format);

            OriginalFormat = format;

            var vsb = new ValueStringBuilder(stackalloc char[256]);
            List<LogPropertyMetadata> metadata = new List<LogPropertyMetadata>();
            int scanIndex = 0;
            int endIndex = format.Length;

            while (scanIndex < endIndex)
            {
                int openBraceIndex = FindBraceIndex(format, '{', scanIndex, endIndex);
                if (scanIndex == 0 && openBraceIndex == endIndex)
                {
                    // No holes found.
                    _format = InternalCompositeFormat.Parse(format);
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
                    int formatDelimiterIndex = FindIndexOfAny(format, FormatDelimiters, openBraceIndex, closeBraceIndex);
                    int colonIndex = format.IndexOf(':', openBraceIndex, closeBraceIndex - openBraceIndex);

                    vsb.Append(format.AsSpan(scanIndex, openBraceIndex - scanIndex + 1));
                    vsb.Append(metadata.Count.ToString());
                    string propName = format.Substring(openBraceIndex + 1, formatDelimiterIndex - openBraceIndex - 1);
                    vsb.Append(format.AsSpan(formatDelimiterIndex, closeBraceIndex - formatDelimiterIndex + 1));
                    string? propFormat = null;
                    if (colonIndex != -1)
                    {
                        propFormat = format.Substring(colonIndex + 1, closeBraceIndex - colonIndex - 1);
                    }
                    Attribute[]? propAttributes = attributes != null && attributes.Length >= metadata.Count ? attributes[metadata.Count] : null;
                    metadata.Add(new LogPropertyMetadata(propName, propFormat, propAttributes));
                    scanIndex = closeBraceIndex + 1;
                }
            }

            _metadata = metadata.ToArray();
            _format = InternalCompositeFormat.Parse(vsb.ToString());
        }

        public string OriginalFormat { get; private set; }
        public InternalCompositeFormat CompositeFormat => _format;
        public int PropertyCount => _metadata != null ? _metadata.Length : 0;
        public string GetValueName(int index) => _metadata![index].Name;

        public LogPropertyMetadata GetPropertyMetadata(int index) => _metadata![index];

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

            return string.Format(CultureInfo.InvariantCulture, _format.Format, formattedValues ?? Array.Empty<object>());
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

            return string.Format(CultureInfo.InvariantCulture, _format.Format, values ?? Array.Empty<object>());
        }

        internal string Format()
        {
            return _format.Format;
        }
        internal string Format<TArg0>(TArg0 arg0)
        {
            string? arg0String = null;
            return
                !TryFormatArgumentIfNullOrEnumerable(arg0, ref arg0String) ?
                string.Format(CultureInfo.InvariantCulture, _format.Format, arg0) :
                string.Format(CultureInfo.InvariantCulture, _format.Format, arg0String);
        }

        internal string Format<TArg0, TArg1>(TArg0 arg0, TArg1 arg1)
        {
            string? arg0String = null, arg1String = null;
            return
                !TryFormatArgumentIfNullOrEnumerable(arg0, ref arg0String) &&
                !TryFormatArgumentIfNullOrEnumerable(arg1, ref arg1String) ?
                string.Format(CultureInfo.InvariantCulture, _format.Format, arg0, arg1) :
                string.Format(CultureInfo.InvariantCulture, _format.Format, (object?)arg0String ?? arg0, (object?)arg1String ?? arg1);
        }

        internal string Format<TArg0, TArg1, TArg2>(TArg0 arg0, TArg1 arg1, TArg2 arg2)
        {
            string? arg0String = null, arg1String = null, arg2String = null;
            return
                !TryFormatArgumentIfNullOrEnumerable(arg0, ref arg0String) &&
                !TryFormatArgumentIfNullOrEnumerable(arg1, ref arg1String) &&
                !TryFormatArgumentIfNullOrEnumerable(arg2, ref arg2String) ?
                string.Format(CultureInfo.InvariantCulture, _format.Format, arg0, arg1, arg2) :
                string.Format(CultureInfo.InvariantCulture, _format.Format, (object?)arg0String ?? arg0, (object?)arg1String ?? arg1, (object?)arg2String ?? arg2);
        }

        public KeyValuePair<string, object?> GetValue(object?[] values, int index)
        {
            if (index < 0 || index > PropertyCount)
            {
                throw new IndexOutOfRangeException(nameof(index));
            }

            if (PropertyCount > index)
            {
                return new KeyValuePair<string, object?>(_metadata![index].Name, values[index]);
            }

            return new KeyValuePair<string, object?>("{OriginalFormat}", OriginalFormat);
        }

        public IEnumerable<KeyValuePair<string, object?>> GetValues(object[] values)
        {
            var valueArray = new KeyValuePair<string, object?>[values.Length + 1];
            for (int index = 0; index != PropertyCount; ++index)
            {
                valueArray[index] = new KeyValuePair<string, object?>(_metadata![index].Name, values[index]);
            }

            valueArray[valueArray.Length - 1] = new KeyValuePair<string, object?>("{OriginalFormat}", OriginalFormat);
            return valueArray;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void AppendFormattedPropertyValue<T>(T value, ref BufferWriter<char> bufferWriter, int alignment, string? format)
        {
            if (value is string valueString)
            {
                if (alignment == 0)
                {
                    bufferWriter.Write(valueString.AsSpan());
                }
                else
                {
                    AppendFormattedPropertyValueAligned(valueString, ref bufferWriter, alignment, format);
                }
            }
            else
            {
                AppendFormattedPropertyValueNonString(value, ref bufferWriter, alignment, format);
            }
        }


        protected static void AppendFormattedPropertyValueNonString<T>(T value, ref BufferWriter<char> bufferWriter, int alignment, string? format)
        {
            if (alignment == 0)
            {
#if NET8_0_OR_GREATER
                if (value is ISpanFormattable)
                {
                    bufferWriter.EnsureSize(32);
                    if (((ISpanFormattable)value).TryFormat(bufferWriter.CurrentSpan, out int charsWritten, format, null))
                    {
                        bufferWriter.Advance(charsWritten);
                        return;
                    }
                }
#endif
                bufferWriter.Write(FormatPropertyValue(value, format).AsSpan());
            }
            else
            {
                AppendFormattedPropertyValueAligned(value, ref bufferWriter, alignment, format);
            }
        }

        protected static void AppendFormattedPropertyValueAligned<T>(T value, ref BufferWriter<char> bufferWriter, int alignment, string? format)
        {
            bool leftAlign = false;
            int paddingNeeded;
            Span<char> span;
            if (alignment < 0)
            {
                leftAlign = true;
                alignment = -alignment;
            }
#if NET8_0_OR_GREATER
            if (value is ISpanFormattable)
            {
                bufferWriter.EnsureSize(Math.Max(32, alignment));
                span = bufferWriter.CurrentSpan;
                if (((ISpanFormattable)value).TryFormat(span, out int charsWritten, format, CultureInfo.InvariantCulture))
                {
                    paddingNeeded = alignment - charsWritten;
                    if (paddingNeeded <= 0)
                    {
                        bufferWriter.Advance(charsWritten);
                        return;
                    }
                    if (leftAlign)
                    {
                        span.Slice(charsWritten, paddingNeeded).Fill(' ');
                    }
                    else
                    {
                        span.Slice(0, charsWritten).CopyTo(span.Slice(paddingNeeded));
                        span.Slice(0, paddingNeeded).Fill(' ');
                    }
                    bufferWriter.Advance(alignment);
                    return;
                }
            }
#endif

            string unpadded = FormatPropertyValue(value, format);
            paddingNeeded = alignment - unpadded.Length;
            bufferWriter.EnsureSize(Math.Max(unpadded.Length, alignment));
            span = bufferWriter.CurrentSpan;
            if (paddingNeeded <= 0)
            {
                bufferWriter.Write(unpadded.AsSpan());
                return;
            }

            if (leftAlign)
            {
                unpadded.AsSpan().CopyTo(span);
                span.Slice(unpadded.Length, paddingNeeded).Fill(' ');
            }
            else
            {
                span.Slice(0, paddingNeeded).Fill(' ');
                unpadded.AsSpan().CopyTo(span.Slice(paddingNeeded));
            }
            bufferWriter.Advance(alignment);

        }

        protected static string FormatPropertyValue<T>(T value, string? format)
        {
            string? s;
            if (value is IFormattable)
            {
                s = ((IFormattable)value).ToString(format, null);
            }
            else if (value is not string && value is IEnumerable enumerable)
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
                return vsb.ToString();
            }
            else
            {
                s = value?.ToString();
            }
            if (s == null)
            {
                return NullValue;
            }
            else
            {
                return s;
            }
        }

        private static object FormatArgument(object? value)
        {
            string? stringValue = null;
            return TryFormatArgumentIfNullOrEnumerable(value, ref stringValue) ? stringValue : value!;
        }

        private static bool TryFormatArgumentIfNullOrEnumerable(object? value, [NotNullWhen(true)] ref string? stringValue)
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
