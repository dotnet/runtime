// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace ILAssembler
{
    internal static class NameHelpers
    {
        public static string GetPrivateScopeMetadataName(string name, bool isMethod)
        {
            const int TokenLength = 8;
            const string PrivateScopeMarker = "$PST";
            int markerIndex = name.Length - PrivateScopeMarker.Length - TokenLength;
            if (markerIndex < 0)
            {
                return name;
            }

            ReadOnlySpan<char> token = name.AsSpan(markerIndex + PrivateScopeMarker.Length);
            if (!name.AsSpan(markerIndex, PrivateScopeMarker.Length).SequenceEqual(PrivateScopeMarker)
                || !token.StartsWith(isMethod ? "06" : "04")
                || !IsHexToken(token))
            {
                return name;
            }

            return name.Substring(0, markerIndex);

            static bool IsHexToken(ReadOnlySpan<char> token)
            {
                foreach (char c in token)
                {
                    if (!char.IsAsciiHexDigit(c))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public static (string Namespace, string Name) SplitDottedNameToNamespaceAndName(string dottedName)
        {
            int lastDotIndex = dottedName.LastIndexOf('.');

            if (lastDotIndex > 0 && dottedName[lastDotIndex - 1] == '.')
            {
                // Handle cases like "a.b..ctor".
                lastDotIndex -= 1;
            }

            // A dot at position 0 is part of the name (e.g., ".GlobalStruct"), not a namespace separator
            if (lastDotIndex <= 0)
            {
                return (string.Empty, dottedName);
            }

            return (
                dottedName.Substring(0, lastDotIndex),
                dottedName.Substring(lastDotIndex + 1));
        }
    }
}
