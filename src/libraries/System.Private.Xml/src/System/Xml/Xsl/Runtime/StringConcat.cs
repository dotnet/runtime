// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace System.Xml.Xsl.Runtime
{
    /// <summary>
    /// Efficiently concatenates strings when the number of string is not known beforehand, and
    /// yet it is common for only one string to be concatenated.  StringBuilder is not good for
    /// this purpose, since it *always* allocates objects, even if only one string is appended.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct StringConcat
    {
        private string? _s1, _s2, _s3, _s4;
        private string? _delimiter;
        private List<string?>? _strList;
        private int _idxStr;

        /// <summary>
        /// Clear the result string.
        /// </summary>
        public void Clear()
        {
            _idxStr = 0;
            _delimiter = null;
        }

        /// <summary>
        /// Gets or sets the string that delimits concatenated strings.
        /// </summary>
        public string? Delimiter
        {
            get { return _delimiter; }
            set { _delimiter = value; }
        }

        /// <summary>
        /// Return the number of concatenated strings, including delimiters.
        /// </summary>
        internal int Count
        {
            get { return _idxStr; }
        }

        /// <summary>
        /// Concatenate a new string to the result.
        /// </summary>
        public void Concat(string value)
        {
            Debug.Assert(value != null);

            if (_delimiter != null && _idxStr != 0)
            {
                // Add delimiter
                ConcatNoDelimiter(_delimiter);
            }

            ConcatNoDelimiter(value);
        }

        /// <summary>
        /// Get the result string.
        /// </summary>
        public string GetResult() =>
            _idxStr switch
            {
                0 => string.Empty,
                1 => _s1 ?? string.Empty,
                2 => string.Concat(_s1, _s2),
                3 => string.Concat(_s1, _s2, _s3),
                4 => string.Concat(_s1, _s2, _s3, _s4),
                _ => string.Concat(_strList!.ToArray()),
            };

        /// <summary>
        /// Concatenate a new string to the result without adding a delimiter.
        /// </summary>
        internal void ConcatNoDelimiter(string? s)
        {
            switch (_idxStr)
            {
                case 0: _s1 = s; break;
                case 1: _s2 = s; break;
                case 2: _s3 = s; break;
                case 3: _s4 = s; break;
                case 4:
                    // Calling Clear() is expensive, allocate a new List instead
                    int capacity = (_strList == null) ? 8 : _strList.Count;
                    List<string?> strList = _strList = new List<string?>(capacity);
                    strList.Add(_s1);
                    strList.Add(_s2);
                    strList.Add(_s3);
                    strList.Add(_s4);
                    goto default;
                default:
                    _strList!.Add(s);
                    break;
            }

            _idxStr++;
        }
    }
}
