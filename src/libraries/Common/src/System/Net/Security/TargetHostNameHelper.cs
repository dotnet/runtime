// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Globalization;

namespace System.Net.Security
{
    internal static class TargetHostNameHelper
    {
        private static readonly IdnMapping s_idnMapping = new IdnMapping();
        private static readonly SearchValues<char> s_safeDnsChars =
            SearchValues.Create("-.0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz");

        private static bool IsSafeDnsString(ReadOnlySpan<char> name) =>
            !name.ContainsAnyExcept(s_safeDnsChars);

        internal static string NormalizeHostName(string? targetHost)
        {
            if (string.IsNullOrEmpty(targetHost))
            {
                return string.Empty;
            }

            // RFC 6066 section 3 says to exclude trailing dot from fully qualified DNS hostname
            targetHost = targetHost.TrimEnd('.');

            try
            {
                return s_idnMapping.GetAscii(targetHost);
            }
            catch (ArgumentException) when (IsSafeDnsString(targetHost))
            {
                // Seems like name that does not conform to IDN but appears somewhat valid according to original DNS rfc.
            }

            return targetHost;
        }
    }
}
