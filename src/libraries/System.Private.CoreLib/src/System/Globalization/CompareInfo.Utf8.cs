// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace System.Globalization
{
    public partial class CompareInfo
    {
        /// <summary>
        /// Determines whether a UTF-8 string starts with a specific prefix.
        /// </summary>
        /// <param name="source">The UTF-8 string to search within.</param>
        /// <param name="prefix">The prefix to attempt to match at the start of <paramref name="source"/>.</param>
        /// <param name="options">The <see cref="CompareOptions"/> to use during the match.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="prefix"/> occurs at the start of <paramref name="source"/>;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="options"/> contains an unsupported combination of flags.
        /// </exception>
        internal bool IsPrefixUtf8(ReadOnlySpan<byte> source, ReadOnlySpan<byte> prefix, CompareOptions options = CompareOptions.None)
        {
            // The empty UTF-8 string is trivially a prefix of every other string. For compat with
            // earlier versions of the Framework we'll early-exit here before validating the
            // 'options' argument.

            if (prefix.IsEmpty)
            {
                return true;
            }

            if ((options & ValidIndexMaskOffFlags) == 0)
            {
                // Common case: caller is attempting to perform a linguistic search.
                // Pass the flags down to NLS or ICU unless we're running in invariant
                // mode, at which point we normalize the flags to Ordinal[IgnoreCase].

                if (!GlobalizationMode.Invariant)
                {
                    return StartsWithCoreUtf8(source, prefix, options);
                }

                if ((options & CompareOptions.IgnoreCase) == 0)
                {
                    return source.StartsWith(prefix);
                }

                return source.StartsWithOrdinalIgnoreCaseUtf8(prefix);
            }
            else
            {
                // Less common case: caller is attempting to perform non-linguistic comparison,
                // or an invalid combination of flags was supplied.

                if (options == CompareOptions.Ordinal)
                {
                    return source.StartsWith(prefix);
                }

                if (options == CompareOptions.OrdinalIgnoreCase)
                {
                    return source.StartsWithOrdinalIgnoreCaseUtf8(prefix);
                }

                ThrowCompareOptionsCheckFailed(options);

                return false; // make the compiler happy;
            }
        }

        private unsafe bool StartsWithCoreUtf8(ReadOnlySpan<byte> source, ReadOnlySpan<byte> prefix, CompareOptions options)
        {
            // NLS/ICU doesn't provide native UTF-8 support so we need to convert to UTF-16 and compare that way

            ReadOnlySpan<char> sourceUtf16 = Encoding.Unicode.GetString(source);
            ReadOnlySpan<char> prefixUtf16 = Encoding.Unicode.GetString(prefix);

            return StartsWithCore(sourceUtf16, prefixUtf16, options, matchLengthPtr: null);
        }
    }
}
