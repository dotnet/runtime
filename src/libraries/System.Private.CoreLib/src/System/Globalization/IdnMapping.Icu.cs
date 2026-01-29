// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Globalization
{
    public sealed partial class IdnMapping
    {
        private string IcuGetAsciiCore(string unicodeString, int index, int count)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);

            ReadOnlySpan<char> unicode = unicodeString.AsSpan(index, count);
            uint flags = IcuFlags;
            CheckInvalidIdnCharacters(unicode, flags, nameof(unicode));

            const int StackallocThreshold = 512;
            // Each unicode character is represented by up to 3 ASCII chars
            // and the whole string is prefixed by "xn--" (length 4)
            int estimatedLength = (int)Math.Min(checked(count * 3L + 4), StackallocThreshold);
            int actualLength;
            if ((uint)estimatedLength < StackallocThreshold)
            {
                Span<char> outputStack = stackalloc char[estimatedLength];
                actualLength = Interop.Globalization.ToAscii(flags, unicode, count, outputStack, estimatedLength);
                if (actualLength > 0 && actualLength <= estimatedLength)
                {
                    return GetStringForOutput(unicodeString, unicode, outputStack.Slice(0, actualLength));
                }
            }
            else
            {
                actualLength = Interop.Globalization.ToAscii(flags, unicode, count, Span<char>.Empty, 0);
            }
            if (actualLength == 0)
            {
                throw new ArgumentException(SR.Argument_IdnIllegalName, nameof(unicode));
            }

            char[] outputHeap = new char[actualLength];
            actualLength = Interop.Globalization.ToAscii(flags, unicode, count, outputHeap, actualLength);
            if (actualLength == 0 || actualLength > outputHeap.Length)
            {
                throw new ArgumentException(SR.Argument_IdnIllegalName, nameof(unicode));
            }

            return GetStringForOutput(unicodeString, unicode, outputHeap.AsSpan(0, actualLength));
        }

        private bool IcuTryGetAsciiCore(ReadOnlySpan<char> unicode, Span<char> destination, out int charsWritten)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);

            uint flags = IcuFlags;
            CheckInvalidIdnCharacters(unicode, flags, nameof(unicode));

            int actualLength = Interop.Globalization.ToAscii(flags, unicode, unicode.Length, destination, destination.Length);

            if (actualLength <= destination.Length)
            {
                if (actualLength == 0)
                {
                    throw new ArgumentException(SR.Argument_IdnIllegalName, nameof(unicode));
                }

                charsWritten = actualLength;
                return true;
            }

            charsWritten = 0;
            return false;
        }

        private string IcuGetUnicodeCore(string asciiString, int index, int count)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);

            ReadOnlySpan<char> ascii = asciiString.AsSpan(index, count);
            uint flags = IcuFlags;
            CheckInvalidIdnCharacters(ascii, flags, nameof(ascii));

            const int StackAllocThreshold = 512;
            if ((uint)count < StackAllocThreshold)
            {
                Span<char> output = stackalloc char[count];
                return IcuGetUnicodeCore(asciiString, ascii, flags, output, reattempt: true);
            }
            else
            {
                char[] output = new char[count];
                return IcuGetUnicodeCore(asciiString, ascii, flags, output, reattempt: true);
            }
        }

        private static string IcuGetUnicodeCore(string asciiString, ReadOnlySpan<char> ascii, uint flags, Span<char> output, bool reattempt)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);

            int realLen = Interop.Globalization.ToUnicode(flags, ascii, ascii.Length, output, output.Length);

            if (realLen == 0)
            {
                throw new ArgumentException(SR.Argument_IdnIllegalName, nameof(ascii));
            }
            else if (realLen <= output.Length)
            {
                return GetStringForOutput(asciiString, ascii, output.Slice(0, realLen));
            }
            else if (reattempt)
            {
                char[] newOutput = new char[realLen];
                return IcuGetUnicodeCore(asciiString, ascii, flags, newOutput, reattempt: false);
            }

            throw new ArgumentException(SR.Argument_IdnIllegalName, nameof(ascii));
        }

        private bool IcuTryGetUnicodeCore(ReadOnlySpan<char> ascii, Span<char> destination, out int charsWritten)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);

            uint flags = IcuFlags;
            CheckInvalidIdnCharacters(ascii, flags, nameof(ascii));

            int actualLength = Interop.Globalization.ToUnicode(flags, ascii, ascii.Length, destination, destination.Length);

            if (actualLength <= destination.Length)
            {
                if (actualLength == 0)
                {
                    throw new ArgumentException(SR.Argument_IdnIllegalName, nameof(ascii));
                }

                charsWritten = actualLength;
                return true;
            }

            charsWritten = 0;
            return false;
        }

        private uint IcuFlags
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
        private static void CheckInvalidIdnCharacters(ReadOnlySpan<char> s, uint flags, string paramName)
        {
            if ((flags & Interop.Globalization.UseStd3AsciiRules) == 0)
            {
                for (int i = 0; i < s.Length; i++)
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
