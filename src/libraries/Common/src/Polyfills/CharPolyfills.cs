// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System;

internal static class CharPolyfills
{
    extension(char)
    {
        public static bool IsAsciiHexDigit(char c) =>
            c is >= '0' and <= '9' or >= 'A' and <= 'F' or >= 'a' and <= 'f';
    }
}
