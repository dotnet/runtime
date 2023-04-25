// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Net.Security
{
    internal static class TargetHostNameHelper
    {
        private static readonly IdnMapping s_idnMapping = new IdnMapping();
        private static readonly IndexOfAnyValues<char> s_safeDnsChars =
            IndexOfAnyValues.Create("-.0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz");

        private static bool IsSafeDnsString(ReadOnlySpan<char> name) =>
            name.IndexOfAnyExcept(s_safeDnsChars) < 0;

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
                // Seems like name that does not confrom to IDN but apers somewhat valid according to original DNS rfc.
            }

            return targetHost;
        }

        // Simplified version of IPAddressParser.Parse to avoid allocations and dependencies.
        // It purposely ignores scopeId as we don't really use so we do not need to map it to actual interface id.
        internal static unsafe bool IsValidAddress(string? hostname)
        {
            if (string.IsNullOrEmpty(hostname))
            {
                return false;
            }

            ReadOnlySpan<char> ipSpan = hostname.AsSpan();

            int end = ipSpan.Length;

            if (ipSpan.Contains(':'))
            {
                // The address is parsed as IPv6 if and only if it contains a colon. This is valid because
                // we don't support/parse a port specification at the end of an IPv4 address.
                Span<ushort> numbers = stackalloc ushort[IPAddressParserStatics.IPv6AddressShorts];

                fixed (char* ipStringPtr = &MemoryMarshal.GetReference(ipSpan))
                {
                    return IPv6AddressHelper.IsValidStrict(ipStringPtr, 0, ref end);
                }
            }
            else if (char.IsDigit(ipSpan[0]))
            {
                long tmpAddr;

                fixed (char* ipStringPtr = &MemoryMarshal.GetReference(ipSpan))
                {
                    tmpAddr = IPv4AddressHelper.ParseNonCanonical(ipStringPtr, 0, ref end, notImplicitFile: true);
                }

                if (tmpAddr != IPv4AddressHelper.Invalid && end == ipSpan.Length)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
