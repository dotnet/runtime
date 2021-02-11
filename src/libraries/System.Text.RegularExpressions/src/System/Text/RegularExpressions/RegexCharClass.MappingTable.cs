// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Text.RegularExpressions
{
    internal sealed partial class RegexCharClass
    {
        /**************************************************************************
            Let U be the set of Unicode character values and let L be the lowercase
            function, mapping from U to U. To perform case insensitive matching of
            character sets, we need to be able to map an interval I in U, say

                I = [chMin, chMax] = { ch : chMin <= ch <= chMax }

            to a set A such that A contains L(I) and A is contained in the union of
            I and L(I).

            The table below partitions U into intervals on which L is non-decreasing.
            Thus, for any interval J = [a, b] contained in one of these intervals,
            L(J) is contained in [L(a), L(b)].

            It is also true that for any such J, [L(a), L(b)] is contained in the
            union of J and L(J). This does not follow from L being non-decreasing on
            these intervals. It follows from the nature of the L on each interval.
            On each interval, L has one of the following forms:

                (1) L(ch) = constant            (LowercaseSet)
                (2) L(ch) = ch + offset         (LowercaseAdd)
                (3) L(ch) = ch | 1              (LowercaseBor)
                (4) L(ch) = ch + (ch & 1)       (LowercaseBad)

            It is easy to verify that for any of these forms [L(a), L(b)] is
            contained in the union of [a, b] and L([a, b]).
        ***************************************************************************/

        internal const int LowercaseSet = 0;    // Set to arg.
        internal const int LowercaseAdd = 1;    // Add arg.
        internal const int LowercaseBor = 2;    // Bitwise or with 1.
        internal const int LowercaseBad = 3;    // Bitwise and with 1 and add original.

        internal static readonly LowerCaseMapping[] s_lcTable = new LowerCaseMapping[]
        {
            new LowerCaseMapping('\u0041', '\u005A', LowercaseAdd, 32),
            new LowerCaseMapping('\u00C0', '\u00D6', LowercaseAdd, 32),
            new LowerCaseMapping('\u00D8', '\u00DE', LowercaseAdd, 32),
            new LowerCaseMapping('\u0100', '\u012E', LowercaseBor, 0),
            new LowerCaseMapping('\u0132', '\u0136', LowercaseBor, 0),
            new LowerCaseMapping('\u0139', '\u0147', LowercaseBad, 0),
            new LowerCaseMapping('\u014A', '\u0176', LowercaseBor, 0),
            new LowerCaseMapping('\u0178', '\u0178', LowercaseSet, 0x00FF),
            new LowerCaseMapping('\u0179', '\u017D', LowercaseBad, 0),
            new LowerCaseMapping('\u0181', '\u0181', LowercaseSet, 0x0253),
            new LowerCaseMapping('\u0182', '\u0184', LowercaseBor, 0),
            new LowerCaseMapping('\u0186', '\u0186', LowercaseSet, 0x0254),
            new LowerCaseMapping('\u0187', '\u0187', LowercaseSet, 0x0188),
            new LowerCaseMapping('\u0189', '\u018A', LowercaseAdd, 205),
            new LowerCaseMapping('\u018B', '\u018B', LowercaseSet, 0x018C),
            new LowerCaseMapping('\u018E', '\u018E', LowercaseSet, 0x01DD),
            new LowerCaseMapping('\u018F', '\u018F', LowercaseSet, 0x0259),
            new LowerCaseMapping('\u0190', '\u0190', LowercaseSet, 0x025B),
            new LowerCaseMapping('\u0191', '\u0191', LowercaseSet, 0x0192),
            new LowerCaseMapping('\u0193', '\u0193', LowercaseSet, 0x0260),
            new LowerCaseMapping('\u0194', '\u0194', LowercaseSet, 0x0263),
            new LowerCaseMapping('\u0196', '\u0196', LowercaseSet, 0x0269),
            new LowerCaseMapping('\u0197', '\u0197', LowercaseSet, 0x0268),
            new LowerCaseMapping('\u0198', '\u0198', LowercaseSet, 0x0199),
            new LowerCaseMapping('\u019C', '\u019C', LowercaseSet, 0x026F),
            new LowerCaseMapping('\u019D', '\u019D', LowercaseSet, 0x0272),
            new LowerCaseMapping('\u019F', '\u019F', LowercaseSet, 0x0275),
            new LowerCaseMapping('\u01A0', '\u01A4', LowercaseBor, 0),
            new LowerCaseMapping('\u01A7', '\u01A7', LowercaseSet, 0x01A8),
            new LowerCaseMapping('\u01A9', '\u01A9', LowercaseSet, 0x0283),
            new LowerCaseMapping('\u01AC', '\u01AC', LowercaseSet, 0x01AD),
            new LowerCaseMapping('\u01AE', '\u01AE', LowercaseSet, 0x0288),
            new LowerCaseMapping('\u01AF', '\u01AF', LowercaseSet, 0x01B0),
            new LowerCaseMapping('\u01B1', '\u01B2', LowercaseAdd, 217),
            new LowerCaseMapping('\u01B3', '\u01B5', LowercaseBad, 0),
            new LowerCaseMapping('\u01B7', '\u01B7', LowercaseSet, 0x0292),
            new LowerCaseMapping('\u01B8', '\u01B8', LowercaseSet, 0x01B9),
            new LowerCaseMapping('\u01BC', '\u01BC', LowercaseSet, 0x01BD),
            new LowerCaseMapping('\u01C4', '\u01C5', LowercaseSet, 0x01C6),
            new LowerCaseMapping('\u01C7', '\u01C8', LowercaseSet, 0x01C9),
            new LowerCaseMapping('\u01CA', '\u01CB', LowercaseSet, 0x01CC),
            new LowerCaseMapping('\u01CD', '\u01DB', LowercaseBad, 0),
            new LowerCaseMapping('\u01DE', '\u01EE', LowercaseBor, 0),
            new LowerCaseMapping('\u01F1', '\u01F2', LowercaseSet, 0x01F3),
            new LowerCaseMapping('\u01F4', '\u01F4', LowercaseSet, 0x01F5),
            new LowerCaseMapping('\u01FA', '\u0216', LowercaseBor, 0),
            new LowerCaseMapping('\u0386', '\u0386', LowercaseSet, 0x03AC),
            new LowerCaseMapping('\u0388', '\u038A', LowercaseAdd, 37),
            new LowerCaseMapping('\u038C', '\u038C', LowercaseSet, 0x03CC),
            new LowerCaseMapping('\u038E', '\u038F', LowercaseAdd, 63),
            new LowerCaseMapping('\u0391', '\u03A1', LowercaseAdd, 32),
            new LowerCaseMapping('\u03A3', '\u03AB', LowercaseAdd, 32),
            new LowerCaseMapping('\u03E2', '\u03EE', LowercaseBor, 0),
            new LowerCaseMapping('\u0401', '\u040F', LowercaseAdd, 80),
            new LowerCaseMapping('\u0410', '\u042F', LowercaseAdd, 32),
            new LowerCaseMapping('\u0460', '\u0480', LowercaseBor, 0),
            new LowerCaseMapping('\u0490', '\u04BE', LowercaseBor, 0),
            new LowerCaseMapping('\u04C1', '\u04C3', LowercaseBad, 0),
            new LowerCaseMapping('\u04C7', '\u04C7', LowercaseSet, 0x04C8),
            new LowerCaseMapping('\u04CB', '\u04CB', LowercaseSet, 0x04CC),
            new LowerCaseMapping('\u04D0', '\u04EA', LowercaseBor, 0),
            new LowerCaseMapping('\u04EE', '\u04F4', LowercaseBor, 0),
            new LowerCaseMapping('\u04F8', '\u04F8', LowercaseSet, 0x04F9),
            new LowerCaseMapping('\u0531', '\u0556', LowercaseAdd, 48),
            new LowerCaseMapping('\u10A0', '\u10C5', LowercaseAdd, 7264),
            new LowerCaseMapping('\u1E00', '\u1E95', LowercaseBor, 0),
            new LowerCaseMapping('\u1EA0', '\u1EF8', LowercaseBor, 0),
            new LowerCaseMapping('\u1F08', '\u1F0F', LowercaseAdd, -8),
            new LowerCaseMapping('\u1F18', '\u1F1D', LowercaseAdd, -8),
            new LowerCaseMapping('\u1F28', '\u1F2F', LowercaseAdd, -8),
            new LowerCaseMapping('\u1F38', '\u1F3F', LowercaseAdd, -8),
            new LowerCaseMapping('\u1F48', '\u1F4D', LowercaseAdd, -8),
            new LowerCaseMapping('\u1F59', '\u1F59', LowercaseSet, 0x1F51),
            new LowerCaseMapping('\u1F5B', '\u1F5B', LowercaseSet, 0x1F53),
            new LowerCaseMapping('\u1F5D', '\u1F5D', LowercaseSet, 0x1F55),
            new LowerCaseMapping('\u1F5F', '\u1F5F', LowercaseSet, 0x1F57),
            new LowerCaseMapping('\u1F68', '\u1F6F', LowercaseAdd, -8),
            new LowerCaseMapping('\u1F88', '\u1F8F', LowercaseAdd, -8),
            new LowerCaseMapping('\u1F98', '\u1F9F', LowercaseAdd, -8),
            new LowerCaseMapping('\u1FA8', '\u1FAF', LowercaseAdd, -8),
            new LowerCaseMapping('\u1FB8', '\u1FB9', LowercaseAdd, -8),
            new LowerCaseMapping('\u1FBA', '\u1FBB', LowercaseAdd, -74),
            new LowerCaseMapping('\u1FBC', '\u1FBC', LowercaseSet, 0x1FB3),
            new LowerCaseMapping('\u1FC8', '\u1FCB', LowercaseAdd, -86),
            new LowerCaseMapping('\u1FCC', '\u1FCC', LowercaseSet, 0x1FC3),
            new LowerCaseMapping('\u1FD8', '\u1FD9', LowercaseAdd, -8),
            new LowerCaseMapping('\u1FDA', '\u1FDB', LowercaseAdd, -100),
            new LowerCaseMapping('\u1FE8', '\u1FE9', LowercaseAdd, -8),
            new LowerCaseMapping('\u1FEA', '\u1FEB', LowercaseAdd, -112),
            new LowerCaseMapping('\u1FEC', '\u1FEC', LowercaseSet, 0x1FE5),
            new LowerCaseMapping('\u1FF8', '\u1FF9', LowercaseAdd, -128),
            new LowerCaseMapping('\u1FFA', '\u1FFB', LowercaseAdd, -126),
            new LowerCaseMapping('\u1FFC', '\u1FFC', LowercaseSet, 0x1FF3),
            new LowerCaseMapping('\u2160', '\u216F', LowercaseAdd, 16),
            new LowerCaseMapping('\u24B6', '\u24CF', LowercaseAdd, 26),
            new LowerCaseMapping('\uFF21', '\uFF3A', LowercaseAdd, 32),
        };

        /// <summary>
        /// Lower case mapping descriptor.
        /// </summary>
        internal readonly struct LowerCaseMapping
        {
            public readonly char ChMin;
            public readonly char ChMax;
            public readonly int LcOp;
            public readonly int Data;

            internal LowerCaseMapping(char chMin, char chMax, int lcOp, int data)
            {
                ChMin = chMin;
                ChMax = chMax;
                LcOp = lcOp;
                Data = data;
            }
        }
    }
}
