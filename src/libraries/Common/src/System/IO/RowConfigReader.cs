// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;

namespace System.IO
{
    /// <summary>
    /// Helper for reading config files where each row is a key-value data pair.
    /// The input key-values must not have any whitespace within them.
    /// Keys are only matched if they begin a line, with no preceding whitespace.
    /// </summary>
    internal ref struct RowConfigReader
    {
        private readonly ReadOnlySpan<char> _buffer;
        private readonly StringComparison _comparisonKind;
        private int _currentIndex;

        /// <summary>
        /// Constructs a new RowConfigReader which reads from the given span.
        /// </summary>
        /// <param name="buffer">The span to parse through.</param>
        public RowConfigReader(ReadOnlySpan<char> buffer)
        {
            _buffer = buffer;
            _comparisonKind = StringComparison.Ordinal;
            _currentIndex = 0;
        }

        /// <summary>
        /// Constructs a new RowConfigReader which reads from the given span.
        /// </summary>
        /// <param name="buffer">The span to parse through.</param>
        /// <param name="comparisonKind">The comparison kind to use.</param>
        public RowConfigReader(ReadOnlySpan<char> buffer, StringComparison comparisonKind)
        {
            _buffer = buffer;
            _comparisonKind = comparisonKind;
            _currentIndex = 0;
        }

        /// <summary>
        /// Gets the next occurrence of the given key, from the current position of the reader,
        /// or throws if no occurrence of the key exists in the remainder of the span.
        /// </summary>
        public ReadOnlySpan<char> GetNextValue(ReadOnlySpan<char> key)
        {
            if (!TryGetNextValue(key, out ReadOnlySpan<char> value))
            {
                throw new InvalidOperationException($"Couldn't get next value with key {key.ToString()}");
            }
            else
            {
                return value;
            }
        }

        /// <summary>
        /// Tries to get the next occurrence of the given key from the current position of the reader.
        /// If successful, returns true and stores the result in 'value'. Otherwise, returns false.
        /// </summary>
        public bool TryGetNextValue(ReadOnlySpan<char> key, out ReadOnlySpan<char> value)
        {
            if (_currentIndex >= _buffer.Length)
            {
                value = default;
                return false;
            }

            // First, find the key, by repeatedly searching for occurrences.
            // We only match an occurrence if it starts a line, by itself, with no preceding whitespace.
            int keyIndex;
            if (!TryFindNextKeyOccurrence(key, _currentIndex, out keyIndex))
            {
                value = default;
                return false;
            }

            // Next, we will take the end of the line, and look backwards for the start of the value.
            // NOTE: This assumes that the "value" does not have any whitespace in it, nor is there any
            // after. This is the format of most "row-based" config files in /proc/net, etc.
            int afterKey = keyIndex + key.Length;

            int endOfLine = _buffer.Slice(afterKey).IndexOf(Environment.NewLine, _comparisonKind);
            if (endOfLine == -1)
            {
                // There may not be a newline after this key, if we've reached the end of the file.
                endOfLine = _buffer.Length;
            }
            else
            {
                endOfLine += afterKey; // Adjust for the slice offset
            }

            // Get the portion of the line from after the key to end of line
            ReadOnlySpan<char> afterKeySpan = _buffer.Slice(afterKey, endOfLine - afterKey);

            // Find the last whitespace in the span after the key
            int whitespaceBeforeValue = afterKeySpan.LastIndexOf('\t');
            if (whitespaceBeforeValue == -1)
            {
                whitespaceBeforeValue = afterKeySpan.LastIndexOf(' ');
            }

            if (whitespaceBeforeValue == -1)
            {
                // No whitespace found after key, which means no value
                value = default;
                return false;
            }

            // Value starts right after the whitespace
            ReadOnlySpan<char> valueSpan = afterKeySpan.Slice(whitespaceBeforeValue + 1).TrimEnd(" \t\r\n");

            if (valueSpan.IsEmpty)
            {
                value = default;
                return false;
            }

            value = valueSpan;
            _currentIndex = endOfLine + 1;
            return true;
        }

        private bool TryFindNextKeyOccurrence(ReadOnlySpan<char> key, int startIndex, out int keyIndex)
        {
            ReadOnlySpan<char> remaining = _buffer.Slice(startIndex);
            int offset = startIndex;

            // Loop until end of file is reached, or a match is found.
            while (true)
            {
                int foundIndex = remaining.IndexOf(key, _comparisonKind);
                if (foundIndex == -1)
                {
                    // Reached end of span with no match.
                    keyIndex = -1;
                    return false;
                }

                keyIndex = offset + foundIndex;

                // Check if the match is at the beginning of the buffer, or is preceded by a newline.
                if (keyIndex == 0 ||
                    (keyIndex >= Environment.NewLine.Length && _buffer.Slice(keyIndex - Environment.NewLine.Length, Environment.NewLine.Length).SequenceEqual(Environment.NewLine)))
                {
                    // Check if the match is followed by whitespace, meaning it is not part of a larger word.
                    if (HasFollowingWhitespace(keyIndex, key.Length))
                    {
                        return true;
                    }
                }

                // Move past this match and continue searching
                remaining = remaining.Slice(foundIndex + key.Length);
                offset += foundIndex + key.Length;
            }
        }

        private bool HasFollowingWhitespace(int keyIndex, int length)
        {
            return (keyIndex + length < _buffer.Length)
                && (_buffer[keyIndex + length] == ' ' || _buffer[keyIndex + length] == '\t');
        }

        /// <summary>
        /// Gets the next occurrence of the key in the span, and parses it as an Int32.
        /// Throws if the key is not found in the remainder of the span, or if the key
        /// cannot be successfully parsed into an Int32.
        /// </summary>
        /// <remarks>
        /// This is mainly provided as a helper because most Linux config/info files
        /// store integral data.
        /// </remarks>
        public int GetNextValueAsInt32(ReadOnlySpan<char> key)
        {
            ReadOnlySpan<char> value = GetNextValue(key);
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
            {
                return result;
            }
            else
            {
                throw new InvalidOperationException($"Unable to parse value {value.ToString()} of key {key.ToString()} as an Int32.");
            }
        }

        /// <summary>
        /// Gets the next occurrence of the key in the span, and parses it as an Int64.
        /// Throws if the key is not found in the remainder of the span, or if the key
        /// cannot be successfully parsed into an Int64.
        /// </summary>
        /// <remarks>
        /// This is mainly provided as a helper because most Linux config/info files
        /// store integral data.
        /// </remarks>
        public long GetNextValueAsInt64(ReadOnlySpan<char> key)
        {
            ReadOnlySpan<char> value = GetNextValue(key);
            if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long result))
            {
                return result;
            }
            else
            {
                throw new InvalidOperationException($"Unable to parse value {value.ToString()} of key {key.ToString()} as an Int64.");
            }
        }

        /// <summary>
        /// Reads the value of the first occurrence of the given key contained in the string given.
        /// </summary>
        /// <param name="data">The key-value row configuration string.</param>
        /// <param name="key">The key to find.</param>
        /// <returns>The value of the row containing the first occurrence of the key.</returns>
        public static string ReadFirstValueFromString(string data, string key)
        {
            return new RowConfigReader(data).GetNextValue(key).ToString();
        }
    }
}
