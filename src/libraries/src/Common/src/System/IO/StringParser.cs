// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace System.IO
{
    /// <summary>
    /// Provides a string parser that may be used instead of String.Split 
    /// to avoid unnecessary string and array allocations.
    /// </summary>
    internal struct StringParser
    {
        /// <summary>The string being parsed.</summary>
        private readonly string _buffer;

        /// <summary>The separator character used to separate subcomponents of the larger string.</summary>
        private readonly char _separator;

        /// <summary>true if empty subcomponents should be skipped; false to treat them as valid entries.</summary>
        private readonly bool _skipEmpty;

        /// <summary>The starting index from which to parse the current entry.</summary>
        private int _startIndex;

        /// <summary>The ending index that represents the next index after the last character that's part of the current entry.</summary>
        private int _endIndex;

        /// <summary>Initialize the StringParser.</summary>
        /// <param name="buffer">The string to parse.</param>
        /// <param name="separator">The separator character used to separate subcomponents of <paramref name="buffer"/>.</param>
        /// <param name="skipEmpty">true if empty subcomponents should be skipped; false to treat them as valid entries.  Defaults to false.</param>
        public StringParser(string buffer, char separator, bool skipEmpty = false)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            _buffer = buffer;
            _separator = separator;
            _skipEmpty = skipEmpty;
            _startIndex = -1;
            _endIndex = -1;
        }

        /// <summary>Moves to the next component of the string.</summary>
        /// <returns>true if there is a next component to be parsed; otherwise, false.</returns>
        public bool MoveNext()
        {
            if (_buffer == null)
            {
                throw new InvalidOperationException();
            }

            while (true)
            {
                if (_endIndex >= _buffer.Length)
                {
                    _startIndex = _endIndex;
                    return false;
                }

                int nextSeparator = _buffer.IndexOf(_separator, _endIndex + 1);
                _startIndex = _endIndex + 1;
                _endIndex = nextSeparator >= 0 ? nextSeparator : _buffer.Length;

                if (!_skipEmpty || _endIndex > _startIndex + 1)
                {
                   return true;
                }
            }
        }

        /// <summary>
        /// Moves to the next component of the string.  If there isn't one, it throws an exception.
        /// </summary>
        public void MoveNextOrFail()
        {
            if (!MoveNext())
            {
                ThrowForInvalidData();
            }
        }

        /// <summary>
        /// Moves to the next component of the string and returns it as a string.
        /// </summary>
        /// <returns></returns>
        public string MoveAndExtractNext()
        {
            MoveNextOrFail();
            return _buffer.Substring(_startIndex, _endIndex - _startIndex);
        }

        /// <summary>
        /// Gets the current subcomponent of the string as a string.
        /// </summary>
        public string ExtractCurrent()
        {
            if (_buffer == null || _startIndex == -1)
            {
                throw new InvalidOperationException();
            }
            return _buffer.Substring(_startIndex, _endIndex - _startIndex);
        }

        /// <summary>Moves to the next component and parses it as an Int32.</summary>
        public int ParseNextInt32()
        {
            int result;
            if (!int.TryParse(MoveAndExtractNext(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
            {
                ThrowForInvalidData();
            }
            return result;
        }

        /// <summary>Moves to the next component and parses it as an Int64.</summary>
        public long ParseNextInt64()
        {
            long result;
            if (!long.TryParse(MoveAndExtractNext(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
            {
                ThrowForInvalidData();
            }
            return result;
        }

        /// <summary>Moves to the next component and parses it as a UInt32.</summary>
        public uint ParseNextUInt32()
        {
            uint result;
            if (!uint.TryParse(MoveAndExtractNext(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
            {
                ThrowForInvalidData();
            }
            return result;
        }

        /// <summary>Moves to the next component and parses it as a UInt64.</summary>
        public ulong ParseNextUInt64()
        {
            ulong result;
            if (!ulong.TryParse(MoveAndExtractNext(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
            {
                ThrowForInvalidData();
            }
            return result;
        }

        /// <summary>Moves to the next component and parses it as a Char.</summary>
        public char ParseNextChar()
        {
            char result;
            if (!char.TryParse(MoveAndExtractNext(), out result))
            {
                ThrowForInvalidData();
            }
            return result;
        }

        internal delegate T ParseRawFunc<T>(string buffer, ref int startIndex, ref int endIndex);

        /// <summary>
        /// Moves to the next component and hands the raw buffer and indexing data to a selector function
        /// that can validate and return the appropriate data from the component.
        /// </summary>
        internal T ParseRaw<T>(ParseRawFunc<T> selector)
        {
            MoveNextOrFail();
            return selector(_buffer, ref _startIndex, ref _endIndex);
        }

        /// <summary>Throws unconditionally for invalid data.</summary>
        private static void ThrowForInvalidData()
        {
            throw new InvalidDataException();
        }
    }
}