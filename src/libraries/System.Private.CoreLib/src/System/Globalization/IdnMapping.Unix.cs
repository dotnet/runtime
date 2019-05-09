// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Globalization
{
    sealed partial class IdnMapping
    {
        private unsafe string GetAsciiCore(string unicodeString, char* unicode, int count)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(unicodeString != null && unicodeString.Length >= count);

            uint flags = Flags;
            CheckInvalidIdnCharacters(unicode, count, flags, nameof(unicode));

            const int StackallocThreshold = 512;
            // Each unicode character is represented by up to 3 ASCII chars
            // and the whole string is prefixed by "xn--" (length 4)
            int estimatedLength = (int)Math.Min(count * 3L + 4, StackallocThreshold);
            int actualLength;
            if (estimatedLength < StackallocThreshold)
            {
                char* outputStack = stackalloc char[estimatedLength];
                actualLength = Interop.Globalization.ToAscii(flags, unicode, count, outputStack, estimatedLength);
                if (actualLength > 0 && actualLength <= estimatedLength)
                {
                    return GetStringForOutput(unicodeString, unicode, count, outputStack, actualLength);
                }
            }
            else
            {
                actualLength = Interop.Globalization.ToAscii(flags, unicode, count, null, 0);
            }
            if (actualLength == 0)
            {
                throw new ArgumentException(SR.Argument_IdnIllegalName, nameof(unicode));
            }

            char[] outputHeap = new char[actualLength];
            fixed (char* pOutputHeap = &outputHeap[0])
            {
                actualLength = Interop.Globalization.ToAscii(flags, unicode, count, pOutputHeap, actualLength);
                if (actualLength == 0 || actualLength > outputHeap.Length)
                {
                    throw new ArgumentException(SR.Argument_IdnIllegalName, nameof(unicode));
                }
                return GetStringForOutput(unicodeString, unicode, count, pOutputHeap, actualLength);
            }
        }

        private unsafe string GetUnicodeCore(string asciiString, char* ascii, int count)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(asciiString != null && asciiString.Length >= count);

            uint flags = Flags;
            CheckInvalidIdnCharacters(ascii, count, flags, nameof(ascii));

            const int StackAllocThreshold = 512;
            if (count < StackAllocThreshold)
            {
                char* output = stackalloc char[count];
                return GetUnicodeCore(asciiString, ascii, count, flags, output, count, reattempt: true);
            }
            else
            {
                char[] output = new char[count];
                fixed (char* pOutput = &output[0])
                {
                    return GetUnicodeCore(asciiString, ascii, count, flags, pOutput, count, reattempt: true);
                }
            }
        }

        private unsafe string GetUnicodeCore(string asciiString, char* ascii, int count, uint flags, char* output, int outputLength, bool reattempt)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(asciiString != null && asciiString.Length >= count);

            int realLen = Interop.Globalization.ToUnicode(flags, ascii, count, output, outputLength);

            if (realLen == 0)
            {
                throw new ArgumentException(SR.Argument_IdnIllegalName, nameof(ascii));
            }
            else if (realLen <= outputLength)
            {
                return GetStringForOutput(asciiString, ascii, count, output, realLen);
            }
            else if (reattempt)
            {
                char[] newOutput = new char[realLen];
                fixed (char* pNewOutput = newOutput)
                {
                    return GetUnicodeCore(asciiString, ascii, count, flags, pNewOutput, realLen, reattempt: false);
                }
            }

            throw new ArgumentException(SR.Argument_IdnIllegalName, nameof(ascii));
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        private uint Flags
        {
            get
            {
                int flags =
                    (AllowUnassigned ? Interop.Globalization.AllowUnassigned : 0) |
                    (UseStd3AsciiRules ? Interop.Globalization.UseStd3AsciiRules : 0);
                return (uint)flags;
            }
        }

        /// <summary>
        /// ICU doesn't check for invalid characters unless the STD3 rules option
        /// is enabled.
        ///
        /// To match Windows behavior, we walk the string ourselves looking for these
        /// bad characters so we can continue to throw ArgumentException in these cases.
        /// </summary>
        private static unsafe void CheckInvalidIdnCharacters(char* s, int count, uint flags, string paramName)
        {
            if ((flags & Interop.Globalization.UseStd3AsciiRules) == 0)
            {
                for (int i = 0; i < count; i++)
                {
                    char c = s[i];

                    // These characters are prohibited regardless of the UseStd3AsciiRules property.
                    // See https://msdn.microsoft.com/en-us/library/system.globalization.idnmapping.usestd3asciirules(v=vs.110).aspx
                    if (c <= 0x1F || c == 0x7F)
                    {
                        throw new ArgumentException(SR.Argument_IdnIllegalName, paramName);
                    }
                }
            }
        }
    }
}
