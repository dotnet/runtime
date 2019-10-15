// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Web.Util
{
    internal static class UriUtil
    {
        private static readonly char[] s_queryFragmentSeparators = { '?', '#' };

        // Just extracts the query string and fragment from the input path by splitting on the separator characters.
        // Doesn't perform any validation as to whether the input represents a valid URL.
        // Concatenating the pieces back together will form the original input string.
        private static void ExtractQueryAndFragment(string input, out string path, out string queryAndFragment)
        {
            int queryFragmentSeparatorPos = input.IndexOfAny(s_queryFragmentSeparators);
            if (queryFragmentSeparatorPos != -1)
            {
                path = input.Substring(0, queryFragmentSeparatorPos);
                queryAndFragment = input.Substring(queryFragmentSeparatorPos);
            }
            else
            {
                // no query or fragment separator
                path = input;
                queryAndFragment = null;
            }
        }

        // Attempts to split a URI into its constituent pieces.
        // Even if this method returns true, one or more of the out parameters might contain a null or empty string, e.g. if there is no query / fragment.
        // Concatenating the pieces back together will form the original input string.
        internal static bool TrySplitUriForPathEncode(string input, out string schemeAndAuthority, out string path, out string queryAndFragment)
        {
            // Strip off ?query and #fragment if they exist, since we're not going to look at them
            string inputWithoutQueryFragment;
            ExtractQueryAndFragment(input, out inputWithoutQueryFragment, out queryAndFragment);

            // Use Uri class to parse the url into authority and path, use that to help decide
            // where to split the string. Do not rebuild the url from the Uri instance, as that
            // might have subtle changes from the original string (for example, see below about "://").
            Uri uri;
            if (Uri.TryCreate(inputWithoutQueryFragment, UriKind.Absolute, out uri))
            {
                string authority = uri.Authority; // e.g. "foo:81" in "http://foo:81/bar"
                if (!string.IsNullOrEmpty(authority))
                {
                    // don't make any assumptions about the scheme or the "://" part.
                    // For example, the "//" could be missing, or there could be "///" as in "file:///C:\foo.txt"
                    // To retain the same string as originally given, find the authority in the original url and include
                    // everything up to that.
                    int authorityIndex = inputWithoutQueryFragment.IndexOf(authority, StringComparison.OrdinalIgnoreCase);
                    if (authorityIndex != -1)
                    {
                        int schemeAndAuthorityLength = authorityIndex + authority.Length;
                        schemeAndAuthority = inputWithoutQueryFragment.Substring(0, schemeAndAuthorityLength);
                        path = inputWithoutQueryFragment.Substring(schemeAndAuthorityLength);
                        return true;
                    }
                }
            }

            // Not a safe URL
            schemeAndAuthority = null;
            path = null;
            queryAndFragment = null;
            return false;
        }
    }
}