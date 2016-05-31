// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.IO
{
    /// <summary> 
    /// Helper for reading config files where each row is a key-value data pair.
    /// The input key-values must not have any whitespace within them.
    /// Keys are only matched if they begin a line, with no preceding whitespace.
    /// </summary>
    internal struct RowConfigReader
    {
        private readonly string _buffer;
        private readonly StringComparison _comparisonKind;
        private int _currentIndex;

        /// <summary>
        /// Constructs a new RowConfigReader which reads from the given string.
        /// <param name="buffer">The string to parse through.</param>
        /// </summary>
        public RowConfigReader(string buffer)
        {
            _buffer = buffer;
            _comparisonKind = StringComparison.Ordinal;
            _currentIndex = 0;
        }

        /// <summary>
        /// Constructs a new RowConfigReader which reads from the given string.
        /// <param name="buffer">The string to parse through.</param>
        /// <param name="comparisonKind">The comparison kind to use.</param>
        /// </summary>
        public RowConfigReader(string buffer, StringComparison comparisonKind)
        {
            _buffer = buffer;
            _comparisonKind = comparisonKind;
            _currentIndex = 0;
        }

        /// <summary>
        /// Gets the next occurrence of the given key, from the current position of the reader,
        /// or throws if no occurrence of the key exists in the remainder of the string.
        /// </summary>
        public string GetNextValue(string key)
        {
            string value;
            if (!TryGetNextValue(key, out value))
            {
                throw new InvalidOperationException("Couldn't get next value with key " + key);
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
        public bool TryGetNextValue(string key, out string value)
        {
            Debug.Assert(_buffer != null);
            if (_currentIndex >= _buffer.Length)
            {
                value = null;
                return false;
            }

            // First, find the key, by repeatedly searching for occurrences.
            // We only match an occurrence if it starts a line, by itself, with no preceding whitespace.
            int keyIndex;
            if (!TryFindNextKeyOccurrence(key, _currentIndex, out keyIndex))
            {
                value = null;
                return false;
            }

            // Next, we will take the end of the line, and look backwards for the start of the value.
            // NOTE: This assumes that the "value" does not have any whitespace in it, nor is there any
            // after. This is the format of most "row-based" config files in /proc/net, etc.
            int afterKey = keyIndex + key.Length;

            int endOfValue;
            int endOfLine = _buffer.IndexOf(Environment.NewLine, afterKey, _comparisonKind);
            if (endOfLine == -1)
            {
                // There may not be a newline after this key, if we've reached the end of the file.
                endOfLine = _buffer.Length - 1;
                endOfValue = endOfLine;
            }
            else
            {
                endOfValue = endOfLine - 1;
            }

            int lineLength = endOfLine - keyIndex; // keyIndex is the start of the line.
            int whitespaceBeforeValue = _buffer.LastIndexOf('\t', endOfLine, lineLength);
            if (whitespaceBeforeValue == -1)
            {
                whitespaceBeforeValue = _buffer.LastIndexOf(' ', endOfLine, lineLength); // try space as well
            }

            int valueIndex = whitespaceBeforeValue + 1; // Get the first character after the whitespace.
            int valueLength = endOfValue - whitespaceBeforeValue;
            if (valueIndex <= keyIndex || valueIndex == -1 || valueLength == 0)
            {
                // No value found after the key.
                value = null;
                return false;
            }

            value = _buffer.Substring(valueIndex, valueLength); // Grab the whole value string.

            _currentIndex = endOfLine + 1;
            return true;
        }

        private bool TryFindNextKeyOccurrence(string key, int startIndex, out int keyIndex)
        {
            // Loop until end of file is reached, or a match is found.
            while (true)
            {
                keyIndex = _buffer.IndexOf(key, startIndex, _comparisonKind);
                if (keyIndex == -1)
                {
                    // Reached end of string with no match.
                    return false;
                }
                // Check If the match is at the beginning of the string, or is preceded by a newline.
                else if (keyIndex == 0
                    || (keyIndex >= Environment.NewLine.Length && _buffer.Substring(keyIndex - Environment.NewLine.Length, Environment.NewLine.Length) == Environment.NewLine))
                {
                    // Check if the match is followed by whitespace, meaning it is not part of a larger word.
                    if (HasFollowingWhitespace(keyIndex, key.Length))
                    {
                        return true;
                    }
                }

                startIndex = startIndex + key.Length;
            }
        }

        private bool HasFollowingWhitespace(int keyIndex, int length)
        {
            return (keyIndex + length < _buffer.Length)
                && (_buffer[keyIndex + length] == ' ' || _buffer[keyIndex + length] == '\t');
        }

        /// <summary>
        /// Gets the next occurrence of the key in the string, and parses it as an Int32.
        /// Throws if the key is not found in the remainder of the string, or if the key
        /// cannot be successfully parsed into an Int32.
        /// </summary>
        /// <remarks>
        /// This is mainly provided as a helper because most Linux config/info files
        /// store integral data.
        /// </remarks>
        public int GetNextValueAsInt32(string key)
        {
            // PERF: We don't need to allocate a new string here, we can parse an Int32 "in-place" in the existing string.
            string value = GetNextValue(key);
            int result;
            if (int.TryParse(value, out result))
            {
                return result;
            }
            else
            {
                throw new InvalidOperationException("Unable to parse value " + value + " of key " + key + " as an Int32.");
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
            return new RowConfigReader(data).GetNextValue(key);
        }
    }
}
