// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text
{
    internal sealed partial class EncoderLatin1BestFitFallbackBuffer
    {
        // Best fit for ASCII, and since it works for ASCII, we use it for latin1 as well.
        private static ReadOnlySpan<char> ArrayCharBestFit =>
        [
// The first many are in case you wanted to use this for ASCIIEncoding, which we don't need to do any more.
//          (char)0x00a0, (char)0x0020,    // No-Break Space -> Space
//          (char)0x00a1, (char)0x0021,    // Inverted Exclamation Mark -> !
//          (char)0x00a2, (char)0x0063,    // Cent Sign -> c
//          (char)0x00a3, (char)0x003f,    // Pound Sign
//          (char)0x00a4, (char)0x0024,    // Currency Sign -> $
//          (char)0x00a5, (char)0x0059,    // Yen Sign -> Y
//          (char)0x00a6, (char)0x007c,    // Broken Bar -> |
//          (char)0x00a7, (char)0x003f,    // Section Sign
//          (char)0x00a8, (char)0x003f,    // Diaeresis
//          (char)0x00a9, (char)0x0043,    // Copyright Sign -> C
//          (char)0x00aa, (char)0x0061,    // Feminine Ordinal Indicator -> a
//          (char)0x00ab, (char)0x003c,    // Left-Pointing Double Angle Quotation Mark -> <
//          (char)0x00ac, (char)0x003f,    // Not Sign
//          (char)0x00ad, (char)0x002d,    // Soft Hyphen -> -
//          (char)0x00ae, (char)0x0052,    // Registered Sign -> R
//          (char)0x00af, (char)0x003f,    // Macron
//          (char)0x00b0, (char)0x003f,    // Degree Sign
//          (char)0x00b1, (char)0x003f,    // Plus-Minus Sign
//          (char)0x00b2, (char)0x0032,    // Superscript Two -> 2
//          (char)0x00b3, (char)0x0033,    // Superscript Three -> 3
//          (char)0x00b4, (char)0x003f,    // Acute Accent
//          (char)0x00b5, (char)0x003f,    // Micro Sign
//          (char)0x00b6, (char)0x003f,    // Pilcrow Sign
//          (char)0x00b7, (char)0x002e,    // Middle Dot -> .
//          (char)0x00b8, (char)0x002c,    // Cedilla -> ,
//          (char)0x00b9, (char)0x0031,    // Superscript One -> 1
//          (char)0x00ba, (char)0x006f,    // Masculine Ordinal Indicator -> o
//          (char)0x00bb, (char)0x003e,    // Right-Pointing Double Angle Quotation Mark -> >
//          (char)0x00bc, (char)0x003f,    // Vulgar Fraction One Quarter
//          (char)0x00bd, (char)0x003f,    // Vulgar Fraction One Half
//          (char)0x00be, (char)0x003f,    // Vulgar Fraction Three Quarters
//          (char)0x00bf, (char)0x003f,    // Inverted Question Mark
//          (char)0x00c0, (char)0x0041,    // Latin Capital Letter A With Grave -> A
//          (char)0x00c1, (char)0x0041,    // Latin Capital Letter A With Acute -> A
//          (char)0x00c2, (char)0x0041,    // Latin Capital Letter A With Circumflex -> A
//          (char)0x00c3, (char)0x0041,    // Latin Capital Letter A With Tilde -> A
//          (char)0x00c4, (char)0x0041,    // Latin Capital Letter A With Diaeresis -> A
//          (char)0x00c5, (char)0x0041,    // Latin Capital Letter A With Ring Above -> A
//          (char)0x00c6, (char)0x0041,    // Latin Capital Ligature Ae -> A
//          (char)0x00c7, (char)0x0043,    // Latin Capital Letter C With Cedilla -> C
//          (char)0x00c8, (char)0x0045,    // Latin Capital Letter E With Grave -> E
//          (char)0x00c9, (char)0x0045,    // Latin Capital Letter E With Acute -> E
//          (char)0x00ca, (char)0x0045,    // Latin Capital Letter E With Circumflex -> E
//          (char)0x00cb, (char)0x0045,    // Latin Capital Letter E With Diaeresis -> E
//          (char)0x00cc, (char)0x0049,    // Latin Capital Letter I With Grave -> I
//          (char)0x00cd, (char)0x0049,    // Latin Capital Letter I With Acute -> I
//          (char)0x00ce, (char)0x0049,    // Latin Capital Letter I With Circumflex -> I
//          (char)0x00cf, (char)0x0049,    // Latin Capital Letter I With Diaeresis -> I
//          (char)0x00d0, (char)0x0044,    // Latin Capital Letter Eth -> D
//          (char)0x00d1, (char)0x004e,    // Latin Capital Letter N With Tilde -> N
//          (char)0x00d2, (char)0x004f,    // Latin Capital Letter O With Grave -> O
//          (char)0x00d3, (char)0x004f,    // Latin Capital Letter O With Acute -> O
//          (char)0x00d4, (char)0x004f,    // Latin Capital Letter O With Circumflex -> O
//          (char)0x00d5, (char)0x004f,    // Latin Capital Letter O With Tilde -> O
//          (char)0x00d6, (char)0x004f,    // Latin Capital Letter O With Diaeresis -> O
//          (char)0x00d7, (char)0x003f,    // Multiplication Sign
//          (char)0x00d8, (char)0x004f,    // Latin Capital Letter O With Stroke -> O
//          (char)0x00d9, (char)0x0055,    // Latin Capital Letter U With Grave -> U
//          (char)0x00da, (char)0x0055,    // Latin Capital Letter U With Acute -> U
//          (char)0x00db, (char)0x0055,    // Latin Capital Letter U With Circumflex -> U
//          (char)0x00dc, (char)0x0055,    // Latin Capital Letter U With Diaeresis -> U
//          (char)0x00dd, (char)0x0059,    // Latin Capital Letter Y With Acute -> Y
//          (char)0x00de, (char)0x003f,    // Latin Capital Letter Thorn
//          (char)0x00df, (char)0x003f,    // Latin Small Letter Sharp S
//          (char)0x00e0, (char)0x0061,    // Latin Small Letter A With Grave -> a
//          (char)0x00e1, (char)0x0061,    // Latin Small Letter A With Acute -> a
//          (char)0x00e2, (char)0x0061,    // Latin Small Letter A With Circumflex -> a
//          (char)0x00e3, (char)0x0061,    // Latin Small Letter A With Tilde -> a
//          (char)0x00e4, (char)0x0061,    // Latin Small Letter A With Diaeresis -> a
//          (char)0x00e5, (char)0x0061,    // Latin Small Letter A With Ring Above -> a
//          (char)0x00e6, (char)0x0061,    // Latin Small Ligature Ae -> a
//          (char)0x00e7, (char)0x0063,    // Latin Small Letter C With Cedilla -> c
//          (char)0x00e8, (char)0x0065,    // Latin Small Letter E With Grave -> e
//          (char)0x00e9, (char)0x0065,    // Latin Small Letter E With Acute -> e
//          (char)0x00ea, (char)0x0065,    // Latin Small Letter E With Circumflex -> e
//          (char)0x00eb, (char)0x0065,    // Latin Small Letter E With Diaeresis -> e
//          (char)0x00ec, (char)0x0069,    // Latin Small Letter I With Grave -> i
//          (char)0x00ed, (char)0x0069,    // Latin Small Letter I With Acute -> i
//          (char)0x00ee, (char)0x0069,    // Latin Small Letter I With Circumflex -> i
//          (char)0x00ef, (char)0x0069,    // Latin Small Letter I With Diaeresis -> i
//          (char)0x00f0, (char)0x003f,    // Latin Small Letter Eth
//          (char)0x00f1, (char)0x006e,    // Latin Small Letter N With Tilde -> n
//          (char)0x00f2, (char)0x006f,    // Latin Small Letter O With Grave -> o
//          (char)0x00f3, (char)0x006f,    // Latin Small Letter O With Acute -> o
//          (char)0x00f4, (char)0x006f,    // Latin Small Letter O With Circumflex -> o
//          (char)0x00f5, (char)0x006f,    // Latin Small Letter O With Tilde -> o
//          (char)0x00f6, (char)0x006f,    // Latin Small Letter O With Diaeresis -> o
//          (char)0x00f7, (char)0x003f,    // Division Sign
//          (char)0x00f8, (char)0x006f,    // Latin Small Letter O With Stroke -> o
//          (char)0x00f9, (char)0x0075,    // Latin Small Letter U With Grave -> u
//          (char)0x00fa, (char)0x0075,    // Latin Small Letter U With Acute -> u
//          (char)0x00fb, (char)0x0075,    // Latin Small Letter U With Circumflex -> u
//          (char)0x00fc, (char)0x0075,    // Latin Small Letter U With Diaeresis -> u
//          (char)0x00fd, (char)0x0079,    // Latin Small Letter Y With Acute -> y
//          (char)0x00fe, (char)0x003f,    // Latin Small Letter Thorn
//          (char)0x00ff, (char)0x0079,    // Latin Small Letter Y With Diaeresis -> y
            (char)0x0100, (char)0x0041,    // Latin Capital Letter A With Macron -> A
            (char)0x0101, (char)0x0061,    // Latin Small Letter A With Macron -> a
            (char)0x0102, (char)0x0041,    // Latin Capital Letter A With Breve -> A
            (char)0x0103, (char)0x0061,    // Latin Small Letter A With Breve -> a
            (char)0x0104, (char)0x0041,    // Latin Capital Letter A With Ogonek -> A
            (char)0x0105, (char)0x0061,    // Latin Small Letter A With Ogonek -> a
            (char)0x0106, (char)0x0043,    // Latin Capital Letter C With Acute -> C
            (char)0x0107, (char)0x0063,    // Latin Small Letter C With Acute -> c
            (char)0x0108, (char)0x0043,    // Latin Capital Letter C With Circumflex -> C
            (char)0x0109, (char)0x0063,    // Latin Small Letter C With Circumflex -> c
            (char)0x010a, (char)0x0043,    // Latin Capital Letter C With Dot Above -> C
            (char)0x010b, (char)0x0063,    // Latin Small Letter C With Dot Above -> c
            (char)0x010c, (char)0x0043,    // Latin Capital Letter C With Caron -> C
            (char)0x010d, (char)0x0063,    // Latin Small Letter C With Caron -> c
            (char)0x010e, (char)0x0044,    // Latin Capital Letter D With Caron -> D
            (char)0x010f, (char)0x0064,    // Latin Small Letter D With Caron -> d
            (char)0x0110, (char)0x0044,    // Latin Capital Letter D With Stroke -> D
            (char)0x0111, (char)0x0064,    // Latin Small Letter D With Stroke -> d
            (char)0x0112, (char)0x0045,    // Latin Capital Letter E With Macron -> E
            (char)0x0113, (char)0x0065,    // Latin Small Letter E With Macron -> e
            (char)0x0114, (char)0x0045,    // Latin Capital Letter E With Breve -> E
            (char)0x0115, (char)0x0065,    // Latin Small Letter E With Breve -> e
            (char)0x0116, (char)0x0045,    // Latin Capital Letter E With Dot Above -> E
            (char)0x0117, (char)0x0065,    // Latin Small Letter E With Dot Above -> e
            (char)0x0118, (char)0x0045,    // Latin Capital Letter E With Ogonek -> E
            (char)0x0119, (char)0x0065,    // Latin Small Letter E With Ogonek -> e
            (char)0x011a, (char)0x0045,    // Latin Capital Letter E With Caron -> E
            (char)0x011b, (char)0x0065,    // Latin Small Letter E With Caron -> e
            (char)0x011c, (char)0x0047,    // Latin Capital Letter G With Circumflex -> G
            (char)0x011d, (char)0x0067,    // Latin Small Letter G With Circumflex -> g
            (char)0x011e, (char)0x0047,    // Latin Capital Letter G With Breve -> G
            (char)0x011f, (char)0x0067,    // Latin Small Letter G With Breve -> g
            (char)0x0120, (char)0x0047,    // Latin Capital Letter G With Dot Above -> G
            (char)0x0121, (char)0x0067,    // Latin Small Letter G With Dot Above -> g
            (char)0x0122, (char)0x0047,    // Latin Capital Letter G With Cedilla -> G
            (char)0x0123, (char)0x0067,    // Latin Small Letter G With Cedilla -> g
            (char)0x0124, (char)0x0048,    // Latin Capital Letter H With Circumflex -> H
            (char)0x0125, (char)0x0068,    // Latin Small Letter H With Circumflex -> h
            (char)0x0126, (char)0x0048,    // Latin Capital Letter H With Stroke -> H
            (char)0x0127, (char)0x0068,    // Latin Small Letter H With Stroke -> h
            (char)0x0128, (char)0x0049,    // Latin Capital Letter I With Tilde -> I
            (char)0x0129, (char)0x0069,    // Latin Small Letter I With Tilde -> i
            (char)0x012a, (char)0x0049,    // Latin Capital Letter I With Macron -> I
            (char)0x012b, (char)0x0069,    // Latin Small Letter I With Macron -> i
            (char)0x012c, (char)0x0049,    // Latin Capital Letter I With Breve -> I
            (char)0x012d, (char)0x0069,    // Latin Small Letter I With Breve -> i
            (char)0x012e, (char)0x0049,    // Latin Capital Letter I With Ogonek -> I
            (char)0x012f, (char)0x0069,    // Latin Small Letter I With Ogonek -> i
            (char)0x0130, (char)0x0049,    // Latin Capital Letter I With Dot Above -> I
            (char)0x0131, (char)0x0069,    // Latin Small Letter Dotless I -> i
            (char)0x0134, (char)0x004a,    // Latin Capital Letter J With Circumflex -> J
            (char)0x0135, (char)0x006a,    // Latin Small Letter J With Circumflex -> j
            (char)0x0136, (char)0x004b,    // Latin Capital Letter K With Cedilla -> K
            (char)0x0137, (char)0x006b,    // Latin Small Letter K With Cedilla -> k
            (char)0x0139, (char)0x004c,    // Latin Capital Letter L With Acute -> L
            (char)0x013a, (char)0x006c,    // Latin Small Letter L With Acute -> l
            (char)0x013b, (char)0x004c,    // Latin Capital Letter L With Cedilla -> L
            (char)0x013c, (char)0x006c,    // Latin Small Letter L With Cedilla -> l
            (char)0x013d, (char)0x004c,    // Latin Capital Letter L With Caron -> L
            (char)0x013e, (char)0x006c,    // Latin Small Letter L With Caron -> l
            (char)0x0141, (char)0x004c,    // Latin Capital Letter L With Stroke -> L
            (char)0x0142, (char)0x006c,    // Latin Small Letter L With Stroke -> l
            (char)0x0143, (char)0x004e,    // Latin Capital Letter N With Acute -> N
            (char)0x0144, (char)0x006e,    // Latin Small Letter N With Acute -> n
            (char)0x0145, (char)0x004e,    // Latin Capital Letter N With Cedilla -> N
            (char)0x0146, (char)0x006e,    // Latin Small Letter N With Cedilla -> n
            (char)0x0147, (char)0x004e,    // Latin Capital Letter N With Caron -> N
            (char)0x0148, (char)0x006e,    // Latin Small Letter N With Caron -> n
            (char)0x014c, (char)0x004f,    // Latin Capital Letter O With Macron -> O
            (char)0x014d, (char)0x006f,    // Latin Small Letter O With Macron -> o
            (char)0x014e, (char)0x004f,    // Latin Capital Letter O With Breve -> O
            (char)0x014f, (char)0x006f,    // Latin Small Letter O With Breve -> o
            (char)0x0150, (char)0x004f,    // Latin Capital Letter O With Double Acute -> O
            (char)0x0151, (char)0x006f,    // Latin Small Letter O With Double Acute -> o
            (char)0x0152, (char)0x004f,    // Latin Capital Ligature Oe -> O
            (char)0x0153, (char)0x006f,    // Latin Small Ligature Oe -> o
            (char)0x0154, (char)0x0052,    // Latin Capital Letter R With Acute -> R
            (char)0x0155, (char)0x0072,    // Latin Small Letter R With Acute -> r
            (char)0x0156, (char)0x0052,    // Latin Capital Letter R With Cedilla -> R
            (char)0x0157, (char)0x0072,    // Latin Small Letter R With Cedilla -> r
            (char)0x0158, (char)0x0052,    // Latin Capital Letter R With Caron -> R
            (char)0x0159, (char)0x0072,    // Latin Small Letter R With Caron -> r
            (char)0x015a, (char)0x0053,    // Latin Capital Letter S With Acute -> S
            (char)0x015b, (char)0x0073,    // Latin Small Letter S With Acute -> s
            (char)0x015c, (char)0x0053,    // Latin Capital Letter S With Circumflex -> S
            (char)0x015d, (char)0x0073,    // Latin Small Letter S With Circumflex -> s
            (char)0x015e, (char)0x0053,    // Latin Capital Letter S With Cedilla -> S
            (char)0x015f, (char)0x0073,    // Latin Small Letter S With Cedilla -> s
            (char)0x0160, (char)0x0053,    // Latin Capital Letter S With Caron -> S
            (char)0x0161, (char)0x0073,    // Latin Small Letter S With Caron -> s
            (char)0x0162, (char)0x0054,    // Latin Capital Letter T With Cedilla -> T
            (char)0x0163, (char)0x0074,    // Latin Small Letter T With Cedilla -> t
            (char)0x0164, (char)0x0054,    // Latin Capital Letter T With Caron -> T
            (char)0x0165, (char)0x0074,    // Latin Small Letter T With Caron -> t
            (char)0x0166, (char)0x0054,    // Latin Capital Letter T With Stroke -> T
            (char)0x0167, (char)0x0074,    // Latin Small Letter T With Stroke -> t
            (char)0x0168, (char)0x0055,    // Latin Capital Letter U With Tilde -> U
            (char)0x0169, (char)0x0075,    // Latin Small Letter U With Tilde -> u
            (char)0x016a, (char)0x0055,    // Latin Capital Letter U With Macron -> U
            (char)0x016b, (char)0x0075,    // Latin Small Letter U With Macron -> u
            (char)0x016c, (char)0x0055,    // Latin Capital Letter U With Breve -> U
            (char)0x016d, (char)0x0075,    // Latin Small Letter U With Breve -> u
            (char)0x016e, (char)0x0055,    // Latin Capital Letter U With Ring Above -> U
            (char)0x016f, (char)0x0075,    // Latin Small Letter U With Ring Above -> u
            (char)0x0170, (char)0x0055,    // Latin Capital Letter U With Double Acute -> U
            (char)0x0171, (char)0x0075,    // Latin Small Letter U With Double Acute -> u
            (char)0x0172, (char)0x0055,    // Latin Capital Letter U With Ogonek -> U
            (char)0x0173, (char)0x0075,    // Latin Small Letter U With Ogonek -> u
            (char)0x0174, (char)0x0057,    // Latin Capital Letter W With Circumflex -> W
            (char)0x0175, (char)0x0077,    // Latin Small Letter W With Circumflex -> w
            (char)0x0176, (char)0x0059,    // Latin Capital Letter Y With Circumflex -> Y
            (char)0x0177, (char)0x0079,    // Latin Small Letter Y With Circumflex -> y
            (char)0x0178, (char)0x0059,    // Latin Capital Letter Y With Diaeresis -> Y
            (char)0x0179, (char)0x005a,    // Latin Capital Letter Z With Acute -> Z
            (char)0x017a, (char)0x007a,    // Latin Small Letter Z With Acute -> z
            (char)0x017b, (char)0x005a,    // Latin Capital Letter Z With Dot Above -> Z
            (char)0x017c, (char)0x007a,    // Latin Small Letter Z With Dot Above -> z
            (char)0x017d, (char)0x005a,    // Latin Capital Letter Z With Caron -> Z
            (char)0x017e, (char)0x007a,    // Latin Small Letter Z With Caron -> z
            (char)0x0180, (char)0x0062,    // Latin Small Letter B With Stroke -> b
            (char)0x0189, (char)0x0044,    // Latin Capital Letter African D -> D
            (char)0x0191, (char)0x0046,    // Latin Capital Letter F With Hook -> F
            (char)0x0192, (char)0x0066,    // Latin Small Letter F With Hook -> f
            (char)0x0197, (char)0x0049,    // Latin Capital Letter I With Stroke -> I
            (char)0x019a, (char)0x006c,    // Latin Small Letter L With Bar -> l
            (char)0x019f, (char)0x004f,    // Latin Capital Letter O With Middle Tilde -> O
            (char)0x01a0, (char)0x004f,    // Latin Capital Letter O With Horn -> O
            (char)0x01a1, (char)0x006f,    // Latin Small Letter O With Horn -> o
            (char)0x01ab, (char)0x0074,    // Latin Small Letter T With Palatal Hook -> t
            (char)0x01ae, (char)0x0054,    // Latin Capital Letter T With Retroflex Hook -> T
            (char)0x01af, (char)0x0055,    // Latin Capital Letter U With Horn -> U
            (char)0x01b0, (char)0x0075,    // Latin Small Letter U With Horn -> u
            (char)0x01b6, (char)0x007a,    // Latin Small Letter Z With Stroke -> z
            (char)0x01cd, (char)0x0041,    // Latin Capital Letter A With Caron -> A
            (char)0x01ce, (char)0x0061,    // Latin Small Letter A With Caron -> a
            (char)0x01cf, (char)0x0049,    // Latin Capital Letter I With Caron -> I
            (char)0x01d0, (char)0x0069,    // Latin Small Letter I With Caron -> i
            (char)0x01d1, (char)0x004f,    // Latin Capital Letter O With Caron -> O
            (char)0x01d2, (char)0x006f,    // Latin Small Letter O With Caron -> o
            (char)0x01d3, (char)0x0055,    // Latin Capital Letter U With Caron -> U
            (char)0x01d4, (char)0x0075,    // Latin Small Letter U With Caron -> u
            (char)0x01d5, (char)0x0055,    // Latin Capital Letter U With Diaeresis And Macron -> U
            (char)0x01d6, (char)0x0075,    // Latin Small Letter U With Diaeresis And Macron -> u
            (char)0x01d7, (char)0x0055,    // Latin Capital Letter U With Diaeresis And Acute -> U
            (char)0x01d8, (char)0x0075,    // Latin Small Letter U With Diaeresis And Acute -> u
            (char)0x01d9, (char)0x0055,    // Latin Capital Letter U With Diaeresis And Caron -> U
            (char)0x01da, (char)0x0075,    // Latin Small Letter U With Diaeresis And Caron -> u
            (char)0x01db, (char)0x0055,    // Latin Capital Letter U With Diaeresis And Grave -> U
            (char)0x01dc, (char)0x0075,    // Latin Small Letter U With Diaeresis And Grave -> u
            (char)0x01de, (char)0x0041,    // Latin Capital Letter A With Diaeresis And Macron -> A
            (char)0x01df, (char)0x0061,    // Latin Small Letter A With Diaeresis And Macron -> a
            (char)0x01e4, (char)0x0047,    // Latin Capital Letter G With Stroke -> G
            (char)0x01e5, (char)0x0067,    // Latin Small Letter G With Stroke -> g
            (char)0x01e6, (char)0x0047,    // Latin Capital Letter G With Caron -> G
            (char)0x01e7, (char)0x0067,    // Latin Small Letter G With Caron -> g
            (char)0x01e8, (char)0x004b,    // Latin Capital Letter K With Caron -> K
            (char)0x01e9, (char)0x006b,    // Latin Small Letter K With Caron -> k
            (char)0x01ea, (char)0x004f,    // Latin Capital Letter O With Ogonek -> O
            (char)0x01eb, (char)0x006f,    // Latin Small Letter O With Ogonek -> o
            (char)0x01ec, (char)0x004f,    // Latin Capital Letter O With Ogonek And Macron -> O
            (char)0x01ed, (char)0x006f,    // Latin Small Letter O With Ogonek And Macron -> o
            (char)0x01f0, (char)0x006a,    // Latin Small Letter J With Caron -> j
            (char)0x0261, (char)0x0067,    // Latin Small Letter Script G -> g
            (char)0x02b9, (char)0x0027,    // Modifier Letter Prime -> '
            (char)0x02ba, (char)0x0022,    // Modifier Letter Double Prime -> "
            (char)0x02bc, (char)0x0027,    // Modifier Letter Apostrophe -> '
            (char)0x02c4, (char)0x005e,    // Modifier Letter Up Arrowhead -> ^
            (char)0x02c6, (char)0x005e,    // Modifier Letter Circumflex Accent -> ^
            (char)0x02c8, (char)0x0027,    // Modifier Letter Vertical Line -> '
            (char)0x02c9, (char)0x003f,    // Modifier Letter Macron
            (char)0x02ca, (char)0x003f,    // Modifier Letter Acute Accent
            (char)0x02cb, (char)0x0060,    // Modifier Letter Grave Accent -> `
            (char)0x02cd, (char)0x005f,    // Modifier Letter Low Macron -> _
            (char)0x02da, (char)0x003f,    // Ring Above
            (char)0x02dc, (char)0x007e,    // Small Tilde -> ~
            (char)0x0300, (char)0x0060,    // Combining Grave Accent -> `
            (char)0x0302, (char)0x005e,    // Combining Circumflex Accent -> ^
            (char)0x0303, (char)0x007e,    // Combining Tilde -> ~
            (char)0x030e, (char)0x0022,    // Combining Double Vertical Line Above -> "
            (char)0x0331, (char)0x005f,    // Combining Macron Below -> _
            (char)0x0332, (char)0x005f,    // Combining Low Line -> _
            (char)0x2000, (char)0x0020,    // En Quad
            (char)0x2001, (char)0x0020,    // Em Quad
            (char)0x2002, (char)0x0020,    // En Space
            (char)0x2003, (char)0x0020,    // Em Space
            (char)0x2004, (char)0x0020,    // Three-Per-Em Space
            (char)0x2005, (char)0x0020,    // Four-Per-Em Space
            (char)0x2006, (char)0x0020,    // Six-Per-Em Space
            (char)0x2010, (char)0x002d,    // Hyphen -> -
            (char)0x2011, (char)0x002d,    // Non-Breaking Hyphen -> -
            (char)0x2013, (char)0x002d,    // En Dash -> -
            (char)0x2014, (char)0x002d,    // Em Dash -> -
            (char)0x2018, (char)0x0027,    // Left Single Quotation Mark -> '
            (char)0x2019, (char)0x0027,    // Right Single Quotation Mark -> '
            (char)0x201a, (char)0x002c,    // Single Low-9 Quotation Mark -> ,
            (char)0x201c, (char)0x0022,    // Left Double Quotation Mark -> "
            (char)0x201d, (char)0x0022,    // Right Double Quotation Mark -> "
            (char)0x201e, (char)0x0022,    // Double Low-9 Quotation Mark -> "
            (char)0x2020, (char)0x003f,    // Dagger
            (char)0x2021, (char)0x003f,    // Double Dagger
            (char)0x2022, (char)0x002e,    // Bullet -> .
            (char)0x2026, (char)0x002e,    // Horizontal Ellipsis -> .
            (char)0x2030, (char)0x003f,    // Per Mille Sign
            (char)0x2032, (char)0x0027,    // Prime -> '
            (char)0x2035, (char)0x0060,    // Reversed Prime -> `
            (char)0x2039, (char)0x003c,    // Single Left-Pointing Angle Quotation Mark -> <
            (char)0x203a, (char)0x003e,    // Single Right-Pointing Angle Quotation Mark -> >
            (char)0x2122, (char)0x0054,    // Trade Mark Sign -> T
            (char)0xff01, (char)0x0021,    // Fullwidth Exclamation Mark -> !
            (char)0xff02, (char)0x0022,    // Fullwidth Quotation Mark -> "
            (char)0xff03, (char)0x0023,    // Fullwidth Number Sign -> #
            (char)0xff04, (char)0x0024,    // Fullwidth Dollar Sign -> $
            (char)0xff05, (char)0x0025,    // Fullwidth Percent Sign -> %
            (char)0xff06, (char)0x0026,    // Fullwidth Ampersand -> &
            (char)0xff07, (char)0x0027,    // Fullwidth Apostrophe -> '
            (char)0xff08, (char)0x0028,    // Fullwidth Left Parenthesis -> (
            (char)0xff09, (char)0x0029,    // Fullwidth Right Parenthesis -> )
            (char)0xff0a, (char)0x002a,    // Fullwidth Asterisk -> *
            (char)0xff0b, (char)0x002b,    // Fullwidth Plus Sign -> +
            (char)0xff0c, (char)0x002c,    // Fullwidth Comma -> ,
            (char)0xff0d, (char)0x002d,    // Fullwidth Hyphen-Minus -> -
            (char)0xff0e, (char)0x002e,    // Fullwidth Full Stop -> .
            (char)0xff0f, (char)0x002f,    // Fullwidth Solidus -> /
            (char)0xff10, (char)0x0030,    // Fullwidth Digit Zero -> 0
            (char)0xff11, (char)0x0031,    // Fullwidth Digit One -> 1
            (char)0xff12, (char)0x0032,    // Fullwidth Digit Two -> 2
            (char)0xff13, (char)0x0033,    // Fullwidth Digit Three -> 3
            (char)0xff14, (char)0x0034,    // Fullwidth Digit Four -> 4
            (char)0xff15, (char)0x0035,    // Fullwidth Digit Five -> 5
            (char)0xff16, (char)0x0036,    // Fullwidth Digit Six -> 6
            (char)0xff17, (char)0x0037,    // Fullwidth Digit Seven -> 7
            (char)0xff18, (char)0x0038,    // Fullwidth Digit Eight -> 8
            (char)0xff19, (char)0x0039,    // Fullwidth Digit Nine -> 9
            (char)0xff1a, (char)0x003a,    // Fullwidth Colon -> :
            (char)0xff1b, (char)0x003b,    // Fullwidth Semicolon -> ;
            (char)0xff1c, (char)0x003c,    // Fullwidth Less-Than Sign -> <
            (char)0xff1d, (char)0x003d,    // Fullwidth Equals Sign -> =
            (char)0xff1e, (char)0x003e,    // Fullwidth Greater-Than Sign -> >
            (char)0xff1f, (char)0x003f,    // Fullwidth Question Mark
            (char)0xff20, (char)0x0040,    // Fullwidth Commercial At -> @
            (char)0xff21, (char)0x0041,    // Fullwidth Latin Capital Letter A -> A
            (char)0xff22, (char)0x0042,    // Fullwidth Latin Capital Letter B -> B
            (char)0xff23, (char)0x0043,    // Fullwidth Latin Capital Letter C -> C
            (char)0xff24, (char)0x0044,    // Fullwidth Latin Capital Letter D -> D
            (char)0xff25, (char)0x0045,    // Fullwidth Latin Capital Letter E -> E
            (char)0xff26, (char)0x0046,    // Fullwidth Latin Capital Letter F -> F
            (char)0xff27, (char)0x0047,    // Fullwidth Latin Capital Letter G -> G
            (char)0xff28, (char)0x0048,    // Fullwidth Latin Capital Letter H -> H
            (char)0xff29, (char)0x0049,    // Fullwidth Latin Capital Letter I -> I
            (char)0xff2a, (char)0x004a,    // Fullwidth Latin Capital Letter J -> J
            (char)0xff2b, (char)0x004b,    // Fullwidth Latin Capital Letter K -> K
            (char)0xff2c, (char)0x004c,    // Fullwidth Latin Capital Letter L -> L
            (char)0xff2d, (char)0x004d,    // Fullwidth Latin Capital Letter M -> M
            (char)0xff2e, (char)0x004e,    // Fullwidth Latin Capital Letter N -> N
            (char)0xff2f, (char)0x004f,    // Fullwidth Latin Capital Letter O -> O
            (char)0xff30, (char)0x0050,    // Fullwidth Latin Capital Letter P -> P
            (char)0xff31, (char)0x0051,    // Fullwidth Latin Capital Letter Q -> Q
            (char)0xff32, (char)0x0052,    // Fullwidth Latin Capital Letter R -> R
            (char)0xff33, (char)0x0053,    // Fullwidth Latin Capital Letter S -> S
            (char)0xff34, (char)0x0054,    // Fullwidth Latin Capital Letter T -> T
            (char)0xff35, (char)0x0055,    // Fullwidth Latin Capital Letter U -> U
            (char)0xff36, (char)0x0056,    // Fullwidth Latin Capital Letter V -> V
            (char)0xff37, (char)0x0057,    // Fullwidth Latin Capital Letter W -> W
            (char)0xff38, (char)0x0058,    // Fullwidth Latin Capital Letter X -> X
            (char)0xff39, (char)0x0059,    // Fullwidth Latin Capital Letter Y -> Y
            (char)0xff3a, (char)0x005a,    // Fullwidth Latin Capital Letter Z -> Z
            (char)0xff3b, (char)0x005b,    // Fullwidth Left Square Bracket -> [
            (char)0xff3c, (char)0x005c,    // Fullwidth Reverse Solidus -> \
            (char)0xff3d, (char)0x005d,    // Fullwidth Right Square Bracket -> ]
            (char)0xff3e, (char)0x005e,    // Fullwidth Circumflex Accent -> ^
            (char)0xff3f, (char)0x005f,    // Fullwidth Low Line -> _
            (char)0xff40, (char)0x0060,    // Fullwidth Grave Accent -> `
            (char)0xff41, (char)0x0061,    // Fullwidth Latin Small Letter A -> a
            (char)0xff42, (char)0x0062,    // Fullwidth Latin Small Letter B -> b
            (char)0xff43, (char)0x0063,    // Fullwidth Latin Small Letter C -> c
            (char)0xff44, (char)0x0064,    // Fullwidth Latin Small Letter D -> d
            (char)0xff45, (char)0x0065,    // Fullwidth Latin Small Letter E -> e
            (char)0xff46, (char)0x0066,    // Fullwidth Latin Small Letter F -> f
            (char)0xff47, (char)0x0067,    // Fullwidth Latin Small Letter G -> g
            (char)0xff48, (char)0x0068,    // Fullwidth Latin Small Letter H -> h
            (char)0xff49, (char)0x0069,    // Fullwidth Latin Small Letter I -> i
            (char)0xff4a, (char)0x006a,    // Fullwidth Latin Small Letter J -> j
            (char)0xff4b, (char)0x006b,    // Fullwidth Latin Small Letter K -> k
            (char)0xff4c, (char)0x006c,    // Fullwidth Latin Small Letter L -> l
            (char)0xff4d, (char)0x006d,    // Fullwidth Latin Small Letter M -> m
            (char)0xff4e, (char)0x006e,    // Fullwidth Latin Small Letter N -> n
            (char)0xff4f, (char)0x006f,    // Fullwidth Latin Small Letter O -> o
            (char)0xff50, (char)0x0070,    // Fullwidth Latin Small Letter P -> p
            (char)0xff51, (char)0x0071,    // Fullwidth Latin Small Letter Q -> q
            (char)0xff52, (char)0x0072,    // Fullwidth Latin Small Letter R -> r
            (char)0xff53, (char)0x0073,    // Fullwidth Latin Small Letter S -> s
            (char)0xff54, (char)0x0074,    // Fullwidth Latin Small Letter T -> t
            (char)0xff55, (char)0x0075,    // Fullwidth Latin Small Letter U -> u
            (char)0xff56, (char)0x0076,    // Fullwidth Latin Small Letter V -> v
            (char)0xff57, (char)0x0077,    // Fullwidth Latin Small Letter W -> w
            (char)0xff58, (char)0x0078,    // Fullwidth Latin Small Letter X -> x
            (char)0xff59, (char)0x0079,    // Fullwidth Latin Small Letter Y -> y
            (char)0xff5a, (char)0x007a,    // Fullwidth Latin Small Letter Z -> z
            (char)0xff5b, (char)0x007b,    // Fullwidth Left Curly Bracket -> {
            (char)0xff5c, (char)0x007c,    // Fullwidth Vertical Line -> |
            (char)0xff5d, (char)0x007d,    // Fullwidth Right Curly Bracket -> }
            (char)0xff5e, (char)0x007e     // Fullwidth Tilde -> ~
        ];
    }
}
