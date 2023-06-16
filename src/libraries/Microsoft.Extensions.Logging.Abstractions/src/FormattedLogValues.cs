// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// LogValues to enable formatting options supported by <see cref="string.Format(IFormatProvider, string, object?)"/>.
    /// This also enables using {NamedformatItem} in the format string.
    /// </summary>
    internal readonly struct FormattedLogValues : IReadOnlyList<KeyValuePair<string, object?>>
    {
        internal const int MaxCachedFormatters = 1024;
        private const string NullFormat = "[null]";

        private static int s_count;
        private static readonly ConcurrentDictionary<string, FormattedLogValuesMetadata> s_formatters = new ConcurrentDictionary<string, FormattedLogValuesMetadata>();

        private readonly FormattedLogValuesMetadata? _metadata;
        private readonly object?[]? _values;
        private readonly string _originalMessage;

        // for testing purposes
        internal FormattedLogValuesMetadata? Metadata => _metadata;

        public FormattedLogValues(string? format, params object?[]? values)
        {
            if (values != null && values.Length != 0 && format != null)
            {
                if (s_count >= MaxCachedFormatters)
                {
                    if (!s_formatters.TryGetValue(format, out _metadata))
                    {
                        _metadata = new FormattedLogValuesMetadata(format);
                    }
                }
                else
                {
                    _metadata = s_formatters.GetOrAdd(format, f =>
                    {
                        Interlocked.Increment(ref s_count);
                        return new FormattedLogValuesMetadata(f);
                    });
                }
            }
            else
            {
                _metadata = null;
            }

            _originalMessage = format ?? NullFormat;
            _values = values;
        }

        public KeyValuePair<string, object?> this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                {
                    throw new IndexOutOfRangeException(nameof(index));
                }

                if (index == Count - 1)
                {
                    return new KeyValuePair<string, object?> ("{OriginalFormat}", _originalMessage);
                }

                return new KeyValuePair<string, object?>(_metadata!.GetPropertyInfo(index).Name, _values![index]);
            }
        }

        public int Count
        {
            get
            {
                if (_metadata == null)
                {
                    return 1;
                }

                return _metadata.PropertyCount + 1;
            }
        }

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            for (int i = 0; i < Count; ++i)
            {
                yield return this[i];
            }
        }

        public object?[]? Values => _values;

        public override string ToString()
        {
            if (_metadata == null)
            {
                return _originalMessage;
            }

            // this could be done a little more efficiently by caching CompositeFormat parsed earlier
            // creating a FormattingState and directly passing the values. It would avoid allocating
            // the delegate and the delegate closure.
            return _metadata.Formatter(this, null);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    internal sealed class FormattedLogValuesMetadata : ILogMetadata<FormattedLogValues>
    {
        private readonly LogPropertyInfo[] _propertyInfo;
        private Func<FormattedLogValues, Exception?, string>? _formatter;
        public FormattedLogValuesMetadata(string originalFormat)
        {
            OriginalFormat = originalFormat;
            MessageFormatHelper.Parse(originalFormat, out _propertyInfo);
        }

        public LogLevel LogLevel => throw new NotImplementedException();
        public EventId EventId => throw new NotImplementedException();
        public string OriginalFormat { get; }
        public int PropertyCount => _propertyInfo != null ? _propertyInfo.Length : 0;
        public LogPropertyInfo GetPropertyInfo(int index) => _propertyInfo[index];
        public VisitPropertyListAction<FormattedLogValues, TCookie> CreatePropertyListVisitor<TCookie>(IPropertyVisitorFactory<TCookie> propertyVisitorFactory)
        {
            VisitPropertyAction<object?, TCookie> visitProperty = propertyVisitorFactory.GetPropertyVisitor<object?>();
            return VisitProperties;

            void VisitProperties(ref FormattedLogValues flv, ref Span<byte> spanCookie, ref TCookie cookie)
            {
                object?[]? values = flv.Values;
                if(values != null)
                {
                    for(int i = 0; i < values.Length;i++)
                    {
                        visitProperty(i, values[i], ref spanCookie, ref cookie);
                    }
                }
            }
        }

        internal Func<FormattedLogValues, Exception?, string> Formatter
        {
            get
            {
                _formatter ??= this.CreateStringMessageFormatter();
                return _formatter;
            }
        }
    }
}
