// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace System.Net.Http.Headers
{
    /// <summary>Provides a collection of header string values.</summary>
    public readonly struct HeaderStringValues : IReadOnlyCollection<string>
    {
        /// <summary>The associated header.  This is used only for producing a string from <see cref="_value"/> when it's an array.</summary>
        private readonly HeaderDescriptor _header;
        /// <summary>A string or string array (or null if the instance is default).</summary>
        private readonly object _value;

        /// <summary>Initializes the instance.</summary>
        /// <param name="descriptor">The header descriptor associated with the header value.</param>
        /// <param name="value">The header value.</param>
        internal HeaderStringValues(HeaderDescriptor descriptor, string value)
        {
            _header = descriptor;
            _value = value;
        }

        /// <summary>Initializes the instance.</summary>
        /// <param name="descriptor">The header descriptor associated with the header values.</param>
        /// <param name="values">The header values.</param>
        internal HeaderStringValues(HeaderDescriptor descriptor, string[] values)
        {
            _header = descriptor;
            _value = values;
        }

        /// <summary>Gets the number of header values in the collection.</summary>
        public int Count => _value switch
        {
            string => 1,
            string[] values => values.Length,
            _ => 0
        };

        /// <summary>Gets a string containing all the headers in the collection.</summary>
        /// <returns></returns>
        public override string ToString() => _value switch
        {
            string value => value,
            string[] values => string.Join(_header.Parser is HttpHeaderParser parser && parser.SupportsMultipleValues ? parser.Separator : HttpHeaderParser.DefaultSeparator, values),
            _ => string.Empty,
        };

        /// <summary>Gets an enumerator for all of the strings in the collection.</summary>
        /// <returns></returns>
        public Enumerator GetEnumerator() => new Enumerator(_value);

        /// <inheritdoc/>
        IEnumerator<string> IEnumerable<string>.GetEnumerator() => GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>Enumerates the elements of a <see cref="HeaderStringValues"/>.</summary>
        public struct Enumerator : IEnumerator<string>
        {
            /// <summary>If this wraps a string[], that array. Otherwise, null.</summary>
            private readonly string[]? _values;
            /// <summary>The current string header value.  If this wraps a single string, that string.</summary>
            private string? _current;
            /// <summary>Current state of the iteration.</summary>
            private int _index;

            /// <summary>Initializes the enumerator with a string or string[].</summary>
            /// <param name="value">The string or string[] value, or null if this collection is empty.</param>
            internal Enumerator(object value)
            {
                if (value is string s)
                {
                    _values = null;
                    _current = s;
                }
                else
                {
                    _values = value as string[];
                    _current = null;
                }

                _index = 0;
            }

            /// <inheritdoc/>
            public bool MoveNext()
            {
                int index = _index;
                if (index < 0)
                {
                    return false;
                }

                string[]? values = _values;
                if (values != null)
                {
                    if ((uint)index < (uint)values.Length)
                    {
                        _index = index + 1;
                        _current = values[index];
                        return true;
                    }

                    _index = -1;
                    return false;
                }

                _index = -1;
                return _current != null;
            }

            /// <inheritdoc/>
            public string Current => _current!;

            /// <inheritdoc/>
            object IEnumerator.Current => Current;

            /// <inheritdoc/>
            public void Dispose() { }

            /// <inheritdoc/>
            void IEnumerator.Reset() => throw new NotSupportedException();
        }
    }
}
