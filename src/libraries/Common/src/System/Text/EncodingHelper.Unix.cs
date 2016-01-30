// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text
{
    internal static partial class EncodingHelper
    {
        /// <summary>Creates an encoding from the current environment.</summary>
        /// <returns>The encoding, or null if it could not be determined.</returns>
        internal static Encoding GetEncodingFromCharset()
        {
            string charset = GetCharset();
            if (charset != null)
            {
                try { return Encoding.GetEncoding(charset); }
                catch { }
            }
            return null;
        }

        /// <summary>Environment variables that should be checked, in order, for locale.</summary>
        /// <remarks>
        /// One of these environment variables should contain a string of a form consistent with
        /// the X/Open Portability Guide syntax:
        ///     language[territory][.charset][@modifier]
        /// We're interested in the charset, as it specifies the encoding used
        /// for the console.
        /// </remarks>
        private static readonly string[] s_localeEnvVars = { "LC_ALL", "LC_MESSAGES", "LANG" }; // this ordering codifies the lookup rules prescribed by POSIX

        /// <summary>Gets the current charset name from the environment.</summary>
        /// <returns>The charset name if found; otherwise, null.</returns>
        private static string GetCharset()
        {
            // Find the first of the locale environment variables that's set.
            string locale = null;
            foreach (string envVar in s_localeEnvVars)
            {
                locale = Environment.GetEnvironmentVariable(envVar);
                if (!string.IsNullOrWhiteSpace(locale)) break;
            }

            // If we found one, try to parse it.
            // The locale string is expected to be of a form that matches the
            // X/Open Portability Guide syntax: language[_territory][.charset][@modifier]
            if (locale != null)
            {
                // Does it contain the optional charset?
                int dotPos = locale.IndexOf('.');
                if (dotPos >= 0)
                {
                    dotPos++;
                    int atPos = locale.IndexOf('@', dotPos + 1);

                    // return the charset from the locale, stripping off everything else
                    string charset = atPos < dotPos ?
                        locale.Substring(dotPos) :                // no modifier
                        locale.Substring(dotPos, atPos - dotPos); // has modifier
                    return charset.ToLowerInvariant();
                }
            }

            // no charset found; the default will be used
            return null;
        }
    }
}
