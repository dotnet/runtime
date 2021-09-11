// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Globalization
{
    // Corresponds to the "strong" categories from https://www.unicode.org/reports/tr44/#Bidi_Class_Values.
    // For our purposes, each code point is strongly left-to-right ("L"), strongly right-to-left ("R", "AL"),
    // or other (all remaining code points). This is only used internally by IDN processing, and since our
    // IDN processing logic only cares about "strong" values we don't carry the rest of the data.
    internal enum StrongBidiCategory
    {
        Other = 0x00,
        StrongLeftToRight = 0x20,
        StrongRightToLeft = 0x40,
    }
}
