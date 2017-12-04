// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Globalization
{
    internal enum BidiCategory
    {
        LeftToRight = 0,
        LeftToRightEmbedding = 1,
        LeftToRightOverride = 2,
        RightToLeft = 3,
        RightToLeftArabic = 4,
        RightToLeftEmbedding = 5,
        RightToLeftOverride = 6,
        PopDirectionalFormat = 7,
        EuropeanNumber = 8,
        EuropeanNumberSeparator = 9,
        EuropeanNumberTerminator = 10,
        ArabicNumber = 11,
        CommonNumberSeparator = 12,
        NonSpacingMark = 13,
        BoundaryNeutral = 14,
        ParagraphSeparator = 15,
        SegmentSeparator = 16,
        Whitespace = 17,
        OtherNeutrals = 18,
        LeftToRightIsolate = 19,
        RightToLeftIsolate = 20,
        FirstStrongIsolate = 21,
        PopDirectionIsolate = 22,
    }
}
