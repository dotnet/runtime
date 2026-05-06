// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Web.Util
{
    internal static class UriUtil
    {
        // Attempts to split a URI into its constituent pieces.
        // Even if this method returns true, one or more of the out parameters might contain a null or empty string, e.g. if there is no query / fragment.
        // Concatenating the pieces back together will form the original input string.
        internal static bool TrySplitUriForPathEncode(string input, out ReadOnlySpan<char> schemeAndAuthority, [NotNullWhen(true)] out string? path, out ReadOnlySpan<char> queryAndFragment)
        {
            // Strip off ?query and #fragment if they exist, since we're not going to look at them
            int queryFragmentSeparatorPos = input.AsSpan().IndexOfAny('?', '#'); // query fragment separators
            string inputWithoutQueryFragment;
            if (queryFragmentSeparatorPos >= 0)
            {
                inputWithoutQueryFragment = input.Substring(0, queryFragmentSeparatorPos);
                queryAndFragment = input.AsSpan(queryFragmentSeparatorPos);
            }
            else
            {
                // no query or fragment separator
                inputWithoutQueryFragment = input;
                queryAndFragment = ReadOnlySpan<char>.Empty;
            }

            // Use Uri class to parse the url into authority and path, use that to help decide
            // where to split the string. Do not rebuild the url from the Uri instance, as that
            // might have subtle changes from the original string (for example, see below about "://").
            if (Uri.TryCreate(inputWithoutQueryFragment, UriKind.Absolute, out Uri? uri))
            {
                string authority = uri.Authority; // e.g. "foo:81" in "http://foo:81/bar"
                if (!string.IsNullOrEmpty(authority))
                {
                    // don't make any assumptions about the scheme or the "://" part.
                    // For example, the "//" could be missing, or there could be "///" as in "file:///C:\foo.txt"
                    // To retain the same string as originally given, find the authority in the original url and include
                    // everything up to that.
                    int authorityIndex = inputWithoutQueryFragment.IndexOf(authority, StringComparison.OrdinalIgnoreCase);
                    if (authorityIndex >= 0)
                    {
                        int schemeAndAuthorityLength = authorityIndex + authority.Length;
                        schemeAndAuthority = input.AsSpan(0, schemeAndAuthorityLength);
                        path = inputWithoutQueryFragment.Substring(schemeAndAuthorityLength);
                        return true;
                    }
                }
            }

            // Not a safe URL
            schemeAndAuthority = ReadOnlySpan<char>.Empty;
            path = null;
            queryAndFragment = ReadOnlySpan<char>.Empty;
            return false;
        }
    }
}
