// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

namespace ILLink.Shared
{
    /// <summary>
    /// Helper class for parsing DebuggerDisplayAttribute strings to extract member references.
    /// This is shared between the ILLinker and the Roslyn analyzer to ensure consistent behavior.
    /// </summary>
    public static class DebuggerDisplayAttributeHelper
    {
        /// <summary>
        /// Regex pattern to match member references in DebuggerDisplay strings.
        /// Matches expressions like {MemberName} or {MemberName,nq}
        /// </summary>
        private static readonly Regex s_debuggerDisplayAttributeValueRegex = new Regex("{[^{}]+}", RegexOptions.Compiled);

        /// <summary>
        /// Regex pattern to detect the ",nq" suffix which asks the expression evaluator
        /// to remove quotes when displaying the final value.
        /// </summary>
        private static readonly Regex s_containsNqSuffixRegex = new Regex(@".+,\s*nq", RegexOptions.Compiled);

        /// <summary>
        /// Parses a DebuggerDisplay format string and returns member names referenced in it.
        /// Returns null if the string cannot be fully understood (to avoid false positives).
        /// </summary>
        /// <param name="displayString">The DebuggerDisplay format string to parse</param>
        /// <param name="memberNames">Output list of member names found</param>
        /// <returns>True if the string was fully understood, false if there were ambiguous references</returns>
        public static bool TryParseMemberReferences(string? displayString, out List<string> memberNames)
        {
            memberNames = new List<string>();

            if (string.IsNullOrEmpty(displayString))
                return true;

            foreach (Match match in s_debuggerDisplayAttributeValueRegex.Matches(displayString))
            {
                // Remove '{' and '}'
                string realMatch = match.Value.Substring(1, match.Value.Length - 2);

                // Remove ",nq" suffix if present
                // The regex check above ensures there's a comma, so LastIndexOf will not return -1
                if (s_containsNqSuffixRegex.IsMatch(realMatch))
                {
                    realMatch = realMatch.Substring(0, realMatch.LastIndexOf(','));
                }

                if (realMatch.EndsWith("()"))
                {
                    // It's a method call
                    string methodName = realMatch.Substring(0, realMatch.Length - 2);

                    // It's a call to a method on some member. Handling this scenario robustly
                    // would be complicated and a decent bit of work.
                    //
                    // We could implement support for this at some point, but for now it's important
                    // to make sure at least we don't crash trying to find some method on the current
                    // type when it exists on some other type
                    if (methodName.Contains("."))
                        return false; // Cannot fully understand this reference

                    memberNames.Add(methodName);
                }
                else
                {
                    // It's a field or property reference
                    memberNames.Add(realMatch);
                }
            }

            return true;
        }
    }
}
