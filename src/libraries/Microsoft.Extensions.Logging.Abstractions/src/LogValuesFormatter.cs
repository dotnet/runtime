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
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Extensions.Logging
{
    internal class LogValuesMetadata : LogValuesFormatter
    {
        public LogValuesMetadata(string format, LogLevel level, EventId eventId, object[]?[]? metadata) : base(format, metadata)
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
        private readonly LogPropertyInfo[]? _metadata;
        private readonly InternalCompositeFormat _format;

        public LogValuesFormatter(string format, object[]?[]? metadata = null)
        {
            ThrowHelper.ThrowIfNull(format);

            OriginalFormat = format;
            _format = MessageFormatHelper.Parse(format, out _metadata);
            if(metadata != null && _metadata != null)
            {
                for (int i = 0; i < _metadata.Length; i++)
                {
                    _metadata[i].Metadata = metadata[i];
                }
            }
        }

        public string OriginalFormat { get; private set; }
        public InternalCompositeFormat CompositeFormat => _format;
        public int PropertyCount => _metadata != null ? _metadata.Length : 0;
        public string GetValueName(int index) => _metadata![index].Name;

        public LogPropertyInfo GetPropertyInfo(int index) => _metadata![index];

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
