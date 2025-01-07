// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

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
        private static readonly ConcurrentDictionary<string, LogValuesFormatter> s_formatters = new ConcurrentDictionary<string, LogValuesFormatter>();

        private readonly LogValuesFormatter? _formatter;
        private readonly object?[]? _values;
        private readonly string _originalMessage;

        // for testing purposes
        internal LogValuesFormatter? Formatter => _formatter;

        public FormattedLogValues(string? format, params object?[]? values)
        {
            if (values != null && values.Length != 0 && format != null)
            {
                if (s_count >= MaxCachedFormatters)
                {
                    if (!s_formatters.TryGetValue(format, out _formatter))
                    {
                        _formatter = new LogValuesFormatter(format);
                    }
                }
                else
                {
                    _formatter = s_formatters.GetOrAdd(format, f =>
                    {
                        Interlocked.Increment(ref s_count);
                        return new LogValuesFormatter(f);
                    });
                }
            }
            else
            {
                _formatter = null;
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
                    return new KeyValuePair<string, object?>("{OriginalFormat}", _originalMessage);
                }

                return _formatter!.GetValue(_values!, index);
            }
        }

        public int Count
        {
            get
            {
                if (_formatter == null)
                {
                    return 1;
                }

                return _formatter.ValueNames.Count + 1;
            }
        }

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            for (int i = 0; i < Count; ++i)
            {
                yield return this[i];
            }
        }

        public override string ToString()
        {
            if (_formatter == null)
            {
                return _originalMessage;
            }

            return _formatter.Format(_values);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
