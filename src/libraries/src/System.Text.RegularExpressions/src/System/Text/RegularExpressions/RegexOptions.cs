// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.RegularExpressions
{
    [Flags]
    public enum RegexOptions
    {
        None                    = 0x0000,
        IgnoreCase              = 0x0001, // "i"
        Multiline               = 0x0002, // "m"
        ExplicitCapture         = 0x0004, // "n"
        Compiled                = 0x0008, // "c"
        Singleline              = 0x0010, // "s"
        IgnorePatternWhitespace = 0x0020, // "x"
        RightToLeft             = 0x0040, // "r"

#if DEBUG
        Debug                   = 0x0080, // "d"
#endif

        ECMAScript              = 0x0100, // "e"
        CultureInvariant        = 0x0200,
    }
}
