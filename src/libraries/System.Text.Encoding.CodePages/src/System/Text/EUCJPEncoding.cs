// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Globalization;

// EUCJPEncoding
//
// EUC-JP Encoding (51932)
//
// EUC-JP has the following code points:
//  00-7F            - ASCII
//  80-8D & 90-9F    - Control.  (Like Unicode, except for 8e and 8f)
//  A1-FE, A1-FE     - 2 byte JIS X 0208 range.
//  8E, A1-DF        - 2 byte half-width Katakana
//  8F, A1-FE, A1-FE - 3 byte JIX X 0212 range. WE DON'T USE JIS 0212!!!
//
// New thoughts:
//  Fixing windows 20932 code page so that all characters can be looked up there.
//
// Old thoughts:
// Windows NLS uses a special CP20932 for EUC-JP, but it is not used by mlang.  Windows
// Maps the 3 byte ranges to the 2 byte CP20932 by masking the 2nd byte with & 0x7F.
// MLang uses the native windows 932 code page, which is more reliable, however the code points
// don't line up as nicely as the 20932 code page, however it doesn't have JIS X 0212 support.
//
// So what we do is:
//  1.  For ASCII, leave it alone
//  2.  For half-width Katakana, use the leading byte and convert with 20936 code page.
//  3.  For JIS X 0208, Use the leading & trailing bytes with 20936 code page
//  4.  For JIS X 0212, Remove the lead byte, & 0xFF7F, and use the CP20936 table to convert.
//
// Regarding Normalization:
//  Forms KC & KD are precluded because of things like halfwidth Katakana that has compatibility mappings
//  Form D is precluded because of 0x00a8, which changes to space + dieresis.
//
// I think that IsAlwaysNormalized should probably return true for form C (but not certain)
//
// NOTE: We don't use JIS 0212 so we are basically a DBCS code page, we just have to modify
//       the 932 table we're basing this on.
//

using System;

namespace System.Text
{
    internal sealed class EUCJPEncoding : DBCSCodePageEncoding
    {
        // This pretends to be CP 932 as far as memory tables are concerned.
        public EUCJPEncoding() : base(51932, 932)
        {
        }

        // Clean up characters for EUC-JP code pages, etc.
        protected override bool CleanUpBytes(ref int bytes)
        {
            if (bytes >= 0x100)
            {
                // map extended char (0xfa40-0xfc4b) to a special range
                // (ported from mlang)
                if (bytes >= 0xfa40 && bytes <= 0xfc4b)
                {
                    if (bytes >= 0xfa40 && bytes <= 0xfa5b)
                    {
                        if (bytes <= 0xfa49)
                            bytes -= 0x0b51;
                        else if (bytes >= 0xfa4a && bytes <= 0xfa53)
                            bytes -= 0x072f6;
                        else if (bytes >= 0xfa54 && bytes <= 0xfa57)
                            bytes -= 0x0b5b;
                        else if (bytes == 0xfa58)
                            bytes = 0x878a;
                        else if (bytes == 0xfa59)
                            bytes = 0x8782;
                        else if (bytes == 0xfa5a)
                            bytes = 0x8784;
                        else if (bytes == 0xfa5b)
                            bytes = 0x879a;
                    }
                    else if (bytes >= 0xfa5c && bytes <= 0xfc4b)
                    {
                        byte tc = unchecked((byte)bytes);
                        if (tc < 0x5c)
                            bytes -= 0x0d5f;
                        else if (tc >= 0x80 && tc <= 0x9B)
                            bytes -= 0x0d1d;
                        else
                            bytes -= 0x0d1c;
                    }
                }

                // Convert 932 code page to 20932 like code page range
                // (also ported from mlang)
                byte bLead = unchecked((byte)(bytes >> 8));
                byte bTrail = unchecked((byte)bytes);

                bLead -= ((bLead > (byte)0x9f) ? (byte)0xb1 : (byte)0x71);
                bLead = (byte)((bLead << 1) + 1);
                if (bTrail > (byte)0x9e)
                {
                    bTrail -= (byte)0x7e;
                    bLead++;
                }
                else
                {
                    if (bTrail > (byte)0x7e)
                        bTrail--;
                    bTrail -= (byte)0x1f;
                }

                bytes = ((int)bLead) << 8 | (int)bTrail | 0x8080;

                // Don't step out of our allocated lead byte area.
                // All DBCS lead and trail bytes should be >= 0xa1 and <= 0xfe
                if ((bytes & 0xFF00) < 0xa100 || (bytes & 0xFF00) > 0xfe00 ||
                    (bytes & 0xFF) < 0xa1 || (bytes & 0xFF) > 0xfe)
                    return false;
                // WARNING: Our funky mapping allows illegal values, which we continue to use
                // for compatibility purposes.
            }
            else
            {
                // For 51932 1/2 Katakana gets a 0x8E lead byte
                // Adjust 1/2 Katakana
                if (bytes >= 0xa1 && bytes <= 0xdf)
                {
                    bytes |= 0x8E00;
                    return true;
                }

                // 0x81-0x9f and 0xe0-0xfc CP 932
                // 0x8e and 0xa1-0xfe      CP 20932 (we don't use 8e though)
                // b0-df is 1/2 Katakana
                // So 81-9f & e0-fc are 932 lead bytes, a1-fe are our lead bytes
                // so ignore everything above 0x80 except 0xa0 and 0xff
                if (bytes >= 0x81 && bytes != 0xa0 && bytes != 0xff)
                {
                    // We set different lead bytes later, so just return false
                    return false;
                }
            }

            return true;
        }

        protected override unsafe void CleanUpEndBytes(char* chars)
        {
            // Need to special case CP 51932
            // 0x81-0x9f and 0xe0-0xfc CP 932
            // 0x8e and 0xa1-0xfe      CP 20932
            // 0x10 and 0x21-0x9?       Us (remapping 932)
            // b0-df is 1/2 Katakana (trail byte)

            // A1-FE are DBCS code points
            for (int i = 0xA1; i <= 0xFE; i++)
                chars[i] = LEAD_BYTE_CHAR;

            // And 8E is lead byte for Katakana (already set)
            chars[0x8e] = LEAD_BYTE_CHAR;
        }
    }
}
