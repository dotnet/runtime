// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Capture is just a location/length pair that indicates the
// location of a regular expression match. A single regexp
// search may return multiple Capture within each capturing
// RegexGroup.

namespace System.Text.RegularExpressions
{
    /// <summary>
    /// Represents the results from a single subexpression capture. The object represents
    /// one substring for a single successful capture.</para>
    /// </summary>
    public class Capture
    {
        internal String _text;
        internal int _index;
        internal int _length;

        internal Capture(String text, int i, int l)
        {
            _text = text;
            _index = i;
            _length = l;
        }

        /*
         * The index of the beginning of the matched capture
         */
        /// <summary>
        /// Returns the position in the original string where the first character of
        /// captured substring was found.
        /// </summary>
        public int Index
        {
            get
            {
                return _index;
            }
        }

        /*
         * The length of the matched capture
         */
        /// <summary>
        /// Returns the length of the captured substring.
        /// </summary>
        public int Length
        {
            get
            {
                return _length;
            }
        }

        /// <summary>
        /// Returns the value of this Regex Capture.
        /// </summary>
        public string Value
        {
            get
            {
                return _text.Substring(_index, _length);
            }
        }

        /*
         * The capture as a string
         */
        /// <summary>
        /// Returns the substring that was matched.
        /// </summary>
        override public String ToString()
        {
            return Value;
        }

        /*
         * The original string
         */
        internal String GetOriginalString()
        {
            return _text;
        }

        /*
         * The substring to the left of the capture
         */
        internal String GetLeftSubstring()
        {
            return _text.Substring(0, _index);
        }

        /*
         * The substring to the right of the capture
         */
        internal String GetRightSubstring()
        {
            return _text.Substring(_index + _length, _text.Length - _index - _length);
        }
#if DBG
        internal virtual String Description()
        {
            StringBuilder Sb = new StringBuilder();

            Sb.Append("(I = ");
            Sb.Append(_index);
            Sb.Append(", L = ");
            Sb.Append(_length);
            Sb.Append("): ");
            Sb.Append(_text, _index, _length);

            return Sb.ToString();
        }
#endif
    }
}
