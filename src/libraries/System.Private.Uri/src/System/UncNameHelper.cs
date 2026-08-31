// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    // The class designed as to keep minimal the working set of Uri class.
    // The idea is to stay with static helper methods and strings
    internal static class UncNameHelper
    {
        public const int MaximumInternetNameLength = 256;

        public static string ParseCanonicalName(string str, int start, int end, ref bool loopback)
        {
            return DomainNameHelper.ParseCanonicalName(str, start, end, ref loopback);
        }

        //
        // IsValid
        //
        //
        //   ATTN: This class has been re-designed as to conform to XP+ UNC hostname format
        //         It is now similar to DNS name but can contain Unicode characters as well
        //         This class will be removed and replaced by IDN specification later,
        //         but for now we violate URI RFC cause we never escape Unicode characters on the wire
        //         For the same reason we never unescape UNC host names since we never accept
        //         them in escaped format.
        //
        //
        //      Valid UNC server name chars:
        //          a Unicode Letter    (not allowed as the only in a segment)
        //          a Latin-1 digit
        //          '-'    45 0x2D
        //          '.'    46 0x2E    (only as a host domain delimiter)
        //          '_'    95 0x5F
        //
        //
        // Assumption is the caller will check on the resulting name length
        // Remarks:  MUST NOT be used unless all input indexes are verified and trusted.
        public static bool IsValid(ReadOnlySpan<char> name, bool notImplicitFile, out int nameLength)
        {
            nameLength = 0;

            // First segment could consist of only '_' or '-' but it cannot be all digits or empty
            bool validShortName = false;
            int i = 0;
            for (; i < name.Length; i++)
            {
                if (char.IsLetter(name[i]) || name[i] == '-' || name[i] == '_')
                {
                    validShortName = true;
                }
                else if (name[i] == '/' || name[i] == '\\' || (notImplicitFile && (name[i] == ':' || name[i] == '?' || name[i] == '#')))
                {
                    break;
                }
                else if (name[i] == '.')
                {
                    i++;
                    break;
                }
                else if (!char.IsAsciiDigit(name[i]))
                {
                    return false;
                }
            }

            if (!validShortName)
                return false;

            // Subsequent segments must start with a letter or a digit

            for (; (uint)i < (uint)name.Length; i++)
            {
                if (name[i] == '/' || name[i] == '\\' || (notImplicitFile && (name[i] == ':' || name[i] == '?' || name[i] == '#')))
                {
                    break;
                }
                else if (name[i] == '.')
                {
                    if (!validShortName || name[i - 1] == '.')
                        return false;

                    validShortName = false;
                }
                else if (name[i] == '-' || name[i] == '_')
                {
                    if (!validShortName)
                        return false;
                }
                else if (char.IsLetter(name[i]) || char.IsAsciiDigit(name[i]))
                {
                    validShortName = true;
                }
                else
                {
                    return false;
                }
            }

            if (!validShortName)
            {
                // last segment can end with the dot
                if ((uint)(i - 1) >= (uint)name.Length || name[i - 1] != '.')
                {
                    return false;
                }
            }

            // Caller must check that (nameLength <= MaximumInternetNameLength)
            nameLength = i;
            return true;
        }
    }
}
