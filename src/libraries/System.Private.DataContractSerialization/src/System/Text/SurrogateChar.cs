// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.Serialization; // Just for SR


namespace System.Text
{
    internal readonly struct SurrogateChar
    {
        private readonly char _lowChar;
        private readonly char _highChar;

        public const int MinValue = 0x10000;
        public const int MaxValue = MinValue + (1 << 20) - 1;

        private const char surHighMin = (char)0xd800;
        private const char surHighMax = (char)0xdbff;
        private const char surLowMin = (char)0xdc00;
        private const char surLowMax = (char)0xdfff;

        public SurrogateChar(int ch)
        {
            if (ch < MinValue || ch > MaxValue)
                throw new ArgumentException(SR.Format(SR.XmlInvalidSurrogate, ch.ToString("X", CultureInfo.InvariantCulture)), nameof(ch));

            const int mask = ((1 << 10) - 1);

            _lowChar = (char)(((ch - MinValue) & mask) + surLowMin);
            _highChar = (char)((((ch - MinValue) >> 10) & mask) + surHighMin);
        }

        public SurrogateChar(char lowChar, char highChar)
        {
            if (lowChar < surLowMin || lowChar > surLowMax)
                throw new ArgumentException(SR.Format(SR.XmlInvalidLowSurrogate, ((int)lowChar).ToString("X", CultureInfo.InvariantCulture)), nameof(lowChar));

            if (highChar < surHighMin || highChar > surHighMax)
                throw new ArgumentException(SR.Format(SR.XmlInvalidHighSurrogate, ((int)highChar).ToString("X", CultureInfo.InvariantCulture)), nameof(highChar));

            _lowChar = lowChar;
            _highChar = highChar;
        }

        public char LowChar { get { return _lowChar; } }
        public char HighChar { get { return _highChar; } }

        public int Char
        {
            get
            {
                return (_lowChar - surLowMin) | ((_highChar - surHighMin) << 10) + MinValue;
            }
        }
    }
}
