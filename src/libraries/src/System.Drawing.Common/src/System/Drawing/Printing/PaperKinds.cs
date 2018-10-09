// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Printing
{
    /// <summary>
    /// Specifies the standard paper sizes.
    /// </summary>
    public enum PaperKind
    {
        /// <summary>
        /// The paper size is defined by the user.
        /// </summary>
        Custom = 0,

        // I got this information from two places: MSDN's writeup of DEVMODE 
        // (https://docs.microsoft.com/en-us/windows/desktop/api/wingdi/ns-wingdi-_devicemodea), 
        // and the raw C++ header file (wingdi.h).  Beyond that, your guess
        // is as good as mine as to what these members mean.

        /// <summary>
        /// Letter paper (8.5 in. by 11 in.).
        /// </summary>
        Letter = SafeNativeMethods.DMPAPER_LETTER,
        /// <summary>
        /// Legal paper (8.5 in. by 14 in.).
        /// </summary>
        Legal = SafeNativeMethods.DMPAPER_LEGAL,
        /// <summary>
        /// A4 paper (210 mm by 297 mm).
        /// </summary>
        A4 = SafeNativeMethods.DMPAPER_A4,
        /// <summary>
        /// C paper (17 in. by 22 in.).
        /// </summary>
        CSheet = SafeNativeMethods.DMPAPER_CSHEET,
        /// <summary>
        /// D paper (22 in. by 34 in.).
        /// </summary>
        DSheet = SafeNativeMethods.DMPAPER_DSHEET,
        /// <summary>
        /// E paper (34 in. by 44 in.).
        /// </summary>
        ESheet = SafeNativeMethods.DMPAPER_ESHEET,
        /// <summary>
        /// Letter small paper (8.5 in. by 11 in.).
        /// </summary>
        LetterSmall = SafeNativeMethods.DMPAPER_LETTERSMALL,
        /// <summary>
        /// Tabloid paper (11 in. by 17 in.).
        /// </summary>
        Tabloid = SafeNativeMethods.DMPAPER_TABLOID,
        /// <summary>
        /// Ledger paper (17 in. by 11 in.).
        /// </summary>
        Ledger = SafeNativeMethods.DMPAPER_LEDGER,
        /// <summary>
        /// Statement paper (5.5 in. by 8.5 in.).
        /// </summary>
        Statement = SafeNativeMethods.DMPAPER_STATEMENT,
        /// <summary>
        /// Executive paper (7.25 in. by 10.5 in.).
        /// </summary>
        Executive = SafeNativeMethods.DMPAPER_EXECUTIVE,
        /// <summary>
        /// A3 paper (297 mm by 420 mm).
        /// </summary>
        A3 = SafeNativeMethods.DMPAPER_A3,
        /// <summary>
        /// A4 small paper (210 mm by 297 mm).
        /// </summary>
        A4Small = SafeNativeMethods.DMPAPER_A4SMALL,
        /// <summary>
        /// A5 paper (148 mm by 210 mm).
        /// </summary>
        A5 = SafeNativeMethods.DMPAPER_A5,
        /// <summary>
        /// B4 paper (250 mm by 353 mm).
        /// </summary>
        B4 = SafeNativeMethods.DMPAPER_B4,
        /// <summary>
        /// B5 paper (176 mm by 250 mm).
        /// </summary>
        B5 = SafeNativeMethods.DMPAPER_B5,
        /// <summary>
        /// Folio paper (8.5 in. by 13 in.).
        /// </summary>
        Folio = SafeNativeMethods.DMPAPER_FOLIO,
        /// <summary>
        /// Quarto paper (215 mm by 275 mm).
        /// </summary>
        Quarto = SafeNativeMethods.DMPAPER_QUARTO,
        /// <summary>
        /// 10-by-14-inch paper.
        /// </summary>
        Standard10x14 = SafeNativeMethods.DMPAPER_10X14,
        /// <summary>
        /// 11-by-17-inch paper.
        /// </summary>
        Standard11x17 = SafeNativeMethods.DMPAPER_11X17,
        /// <summary>
        /// Note paper (8.5 in. by 11 in.).
        /// </summary>
        Note = SafeNativeMethods.DMPAPER_NOTE,
        /// <summary>
        /// #9 envelope (3.875 in. by 8.875 in.).
        /// </summary>
        Number9Envelope = SafeNativeMethods.DMPAPER_ENV_9,
        /// <summary>
        /// #10 envelope (4.125 in. by 9.5 in.).
        /// </summary>
        Number10Envelope = SafeNativeMethods.DMPAPER_ENV_10,
        /// <summary>
        /// #11 envelope (4.5 in. by 10.375 in.).
        /// </summary>
        Number11Envelope = SafeNativeMethods.DMPAPER_ENV_11,
        /// <summary>
        /// #12 envelope (4.75 in. by 11 in.).
        /// </summary>
        Number12Envelope = SafeNativeMethods.DMPAPER_ENV_12,
        /// <summary>
        /// #14 envelope (5 in. by 11.5 in.).
        /// </summary>
        Number14Envelope = SafeNativeMethods.DMPAPER_ENV_14,
        /// <summary>
        /// DL envelope (110 mm by 220 mm).
        /// </summary>
        DLEnvelope = SafeNativeMethods.DMPAPER_ENV_DL,
        /// <summary>
        /// C5 envelope (162 mm by 229 mm).
        /// </summary>
        C5Envelope = SafeNativeMethods.DMPAPER_ENV_C5,
        /// <summary>
        /// C3 envelope (324 mm by 458 mm).
        /// </summary>
        C3Envelope = SafeNativeMethods.DMPAPER_ENV_C3,
        /// <summary>
        /// C4 envelope (229 mm by 324 mm).
        /// </summary>
        C4Envelope = SafeNativeMethods.DMPAPER_ENV_C4,
        /// <summary>
        /// C6 envelope (114 mm by 162 mm).
        /// </summary>
        C6Envelope = SafeNativeMethods.DMPAPER_ENV_C6,
        /// <summary>
        /// C65 envelope (114 mm by 229 mm).
        /// </summary>
        C65Envelope = SafeNativeMethods.DMPAPER_ENV_C65,
        /// <summary>
        /// B4 envelope (250 mm by 353 mm).
        /// </summary>
        B4Envelope = SafeNativeMethods.DMPAPER_ENV_B4,
        /// <summary>
        /// B5 envelope (176 mm by 250 mm).
        /// </summary>
        B5Envelope = SafeNativeMethods.DMPAPER_ENV_B5,
        /// <summary>
        /// B6 envelope (176 mm by 125 mm).
        /// </summary>
        B6Envelope = SafeNativeMethods.DMPAPER_ENV_B6,
        /// <summary>
        /// Italy envelope (110 mm by 230 mm).
        /// </summary>
        ItalyEnvelope = SafeNativeMethods.DMPAPER_ENV_ITALY,
        /// <summary>
        /// Monarch envelope (3.875 in. by 7.5 in.).
        /// </summary>
        MonarchEnvelope = SafeNativeMethods.DMPAPER_ENV_MONARCH,
        /// <summary>
        /// 6 3/4 envelope (3.625 in. by 6.5 in.).
        /// </summary>
        PersonalEnvelope = SafeNativeMethods.DMPAPER_ENV_PERSONAL,
        /// <summary>
        /// US standard fanfold (14.875 in. by 11 in.).
        /// </summary>
        USStandardFanfold = SafeNativeMethods.DMPAPER_FANFOLD_US,
        /// <summary>
        /// German standard fanfold (8.5 in. by 12 in.).
        /// </summary>
        GermanStandardFanfold = SafeNativeMethods.DMPAPER_FANFOLD_STD_GERMAN,
        /// <summary>
        /// German legal fanfold (8.5 in. by 13 in.).
        /// </summary>
        GermanLegalFanfold = SafeNativeMethods.DMPAPER_FANFOLD_LGL_GERMAN,
        /// <summary>
        /// ISO B4 (250 mm by 353 mm).
        /// </summary>
        IsoB4 = SafeNativeMethods.DMPAPER_ISO_B4,
        /// <summary>
        /// Japanese postcard (100 mm by 148 mm).
        /// </summary>
        JapanesePostcard = SafeNativeMethods.DMPAPER_JAPANESE_POSTCARD,
        /// <summary>
        /// 9-by-11-inch paper.
        /// </summary>
        Standard9x11 = SafeNativeMethods.DMPAPER_9X11,
        /// <summary>
        /// 10-by-11-inch paper.
        /// </summary>
        Standard10x11 = SafeNativeMethods.DMPAPER_10X11,
        /// <summary>
        /// 15-by-11-inch paper.
        /// </summary>
        Standard15x11 = SafeNativeMethods.DMPAPER_15X11,
        /// <summary>
        /// Invite envelope (220 mm by 220 mm).
        /// </summary>
        InviteEnvelope = SafeNativeMethods.DMPAPER_ENV_INVITE,
        /// <summary>
        /// Letter extra paper (9.275 in. by 12 in.).
        /// This value is specific to the PostScript driver and is used only by Linotronic printers in order to conserve paper.
        /// </summary>
        LetterExtra = SafeNativeMethods.DMPAPER_LETTER_EXTRA,
        /// <summary>
        /// Legal extra paper (9.275 in. by 15 in.).
        /// This value is specific to the PostScript driver and is used only by Linotronic printers in order to conserve paper.
        /// </summary>
        LegalExtra = SafeNativeMethods.DMPAPER_LEGAL_EXTRA,
        /// <summary>
        /// Tabloid extra paper (11.69 in. by 18 in.).
        /// This value is specific to the PostScript driver and is used only by Linotronic printers in order to conserve paper.
        /// </summary>
        TabloidExtra = SafeNativeMethods.DMPAPER_TABLOID_EXTRA,
        /// <summary>
        /// A4 extra paper (236 mm by 322 mm).
        /// This value is specific to the PostScript driver and is used only by Linotronic printers in order to conserve paper.
        /// </summary>
        A4Extra = SafeNativeMethods.DMPAPER_A4_EXTRA,
        /// <summary>
        /// Letter transverse paper (8.275 in. by 11 in.).
        /// </summary>
        LetterTransverse = SafeNativeMethods.DMPAPER_LETTER_TRANSVERSE,
        /// <summary>
        /// A4 transverse paper (210 mm by 297 mm).
        /// </summary>
        A4Transverse = SafeNativeMethods.DMPAPER_A4_TRANSVERSE,
        /// <summary>
        /// Letter extra transverse paper (9.275 in. by 12 in.).
        /// </summary>
        LetterExtraTransverse = SafeNativeMethods.DMPAPER_LETTER_EXTRA_TRANSVERSE,
        /// <summary>
        /// SuperA/SuperA/A4 paper (227 mm by 356 mm).
        /// </summary>
        APlus = SafeNativeMethods.DMPAPER_A_PLUS,
        /// <summary>
        /// SuperB/SuperB/A3 paper (305 mm by 487 mm).
        /// </summary>
        BPlus = SafeNativeMethods.DMPAPER_B_PLUS,
        /// <summary>
        /// Letter plus paper (8.5 in. by 12.69 in.).
        /// </summary>
        LetterPlus = SafeNativeMethods.DMPAPER_LETTER_PLUS,
        /// <summary>
        /// A4 plus paper (210 mm by 330 mm).
        /// </summary>
        A4Plus = SafeNativeMethods.DMPAPER_A4_PLUS,
        /// <summary>
        /// A5 transverse paper (148 mm by 210 mm).
        /// </summary>
        A5Transverse = SafeNativeMethods.DMPAPER_A5_TRANSVERSE,
        /// <summary>
        /// JIS B5 transverse paper (182 mm by 257 mm).
        /// </summary>
        B5Transverse = SafeNativeMethods.DMPAPER_B5_TRANSVERSE,
        /// <summary>
        /// A3 extra paper (322 mm by 445 mm).
        /// </summary>
        A3Extra = SafeNativeMethods.DMPAPER_A3_EXTRA,
        /// <summary>
        /// A5 extra paper (174 mm by 235 mm).
        /// </summary>
        A5Extra = SafeNativeMethods.DMPAPER_A5_EXTRA,
        /// <summary>
        /// ISO B5 extra paper (201 mm by 276 mm).
        /// </summary>
        B5Extra = SafeNativeMethods.DMPAPER_B5_EXTRA,
        /// <summary>
        /// A2 paper (420 mm by 594 mm).
        /// </summary>
        A2 = SafeNativeMethods.DMPAPER_A2,
        /// <summary>
        /// A3 transverse paper (297 mm by 420 mm).
        /// </summary>
        A3Transverse = SafeNativeMethods.DMPAPER_A3_TRANSVERSE,
        /// <summary>
        /// A3 extra transverse paper (322 mm by 445 mm).
        /// </summary>
        A3ExtraTransverse = SafeNativeMethods.DMPAPER_A3_EXTRA_TRANSVERSE,
        /// <summary>
        /// Japanese double postcard (200 mm by 148mm).
        /// </summary>
        JapaneseDoublePostcard = SafeNativeMethods.DMPAPER_DBL_JAPANESE_POSTCARD,
        /// <summary>
        /// A6 paper (105 mm by 148 mm).
        /// </summary>
        A6 = SafeNativeMethods.DMPAPER_A6,
        /// <summary>
        /// Japanese Kaku #2 envelope.
        /// </summary>
        JapaneseEnvelopeKakuNumber2 = SafeNativeMethods.DMPAPER_JENV_KAKU2,
        /// <summary>
        /// Japanese Kaku #3 envelope.
        /// </summary>
        JapaneseEnvelopeKakuNumber3 = SafeNativeMethods.DMPAPER_JENV_KAKU3,
        /// <summary>
        /// Japanese Chou #3 envelope.
        /// </summary>
        JapaneseEnvelopeChouNumber3 = SafeNativeMethods.DMPAPER_JENV_CHOU3,
        /// <summary>
        /// Japanese Chou #4 envelope.
        /// </summary>
        JapaneseEnvelopeChouNumber4 = SafeNativeMethods.DMPAPER_JENV_CHOU4,
        /// <summary>
        /// Letter rotated paper (11 in. by 8.5 in.).
        /// </summary>
        LetterRotated = SafeNativeMethods.DMPAPER_LETTER_ROTATED,
        /// <summary>
        /// A3 rotated paper (420mm by 297 mm).
        /// </summary>
        A3Rotated = SafeNativeMethods.DMPAPER_A3_ROTATED,
        /// <summary>
        /// A4 rotated paper (297 mm by 210 mm).
        /// </summary>
        A4Rotated = SafeNativeMethods.DMPAPER_A4_ROTATED,
        /// <summary>
        /// A5 rotated paper (210 mm by 148 mm).
        /// </summary>
        A5Rotated = SafeNativeMethods.DMPAPER_A5_ROTATED,
        /// <summary>
        /// JIS B4 rotated paper (364 mm by 257 mm).
        /// </summary>
        B4JisRotated = SafeNativeMethods.DMPAPER_B4_JIS_ROTATED,
        /// <summary>
        /// JIS B5 rotated paper (257 mm by 182 mm).
        /// </summary>
        B5JisRotated = SafeNativeMethods.DMPAPER_B5_JIS_ROTATED,
        /// <summary>
        /// Japanese rotated postcard (148 mm by 100 mm).
        /// </summary>
        JapanesePostcardRotated = SafeNativeMethods.DMPAPER_JAPANESE_POSTCARD_ROTATED,
        /// <summary>
        /// Japanese rotated double postcard (148 mm by 200 mm).
        /// </summary>
        JapaneseDoublePostcardRotated = SafeNativeMethods.DMPAPER_DBL_JAPANESE_POSTCARD_ROTATED,
        /// <summary>
        /// A6 rotated paper (148 mm by 105 mm).
        /// </summary>
        A6Rotated = SafeNativeMethods.DMPAPER_A6_ROTATED,
        /// <summary>
        /// Japanese rotated Kaku #2 envelope.
        /// </summary>
        JapaneseEnvelopeKakuNumber2Rotated = SafeNativeMethods.DMPAPER_JENV_KAKU2_ROTATED,
        /// <summary>
        /// Japanese rotated Kaku #3 envelope.
        /// </summary>
        JapaneseEnvelopeKakuNumber3Rotated = SafeNativeMethods.DMPAPER_JENV_KAKU3_ROTATED,
        /// <summary>
        /// Japanese rotated Chou #3 envelope.
        /// </summary>
        JapaneseEnvelopeChouNumber3Rotated = SafeNativeMethods.DMPAPER_JENV_CHOU3_ROTATED,
        /// <summary>
        /// Japanese rotated Chou #4 envelope.
        /// </summary>
        JapaneseEnvelopeChouNumber4Rotated = SafeNativeMethods.DMPAPER_JENV_CHOU4_ROTATED,
        /// <summary>
        /// JIS B6 paper (128 mm by 182 mm).
        /// </summary>
        B6Jis = SafeNativeMethods.DMPAPER_B6_JIS,
        /// <summary>
        /// JIS B6 rotated paper (182 mm by 128 mm).
        /// </summary>
        B6JisRotated = SafeNativeMethods.DMPAPER_B6_JIS_ROTATED,
        /// <summary>
        /// 12-by-11-inch paper.
        /// </summary>
        Standard12x11 = SafeNativeMethods.DMPAPER_12X11,
        /// <summary>
        /// Japanese You #4 envelope.
        /// </summary>
        JapaneseEnvelopeYouNumber4 = SafeNativeMethods.DMPAPER_JENV_YOU4,
        /// <summary>
        /// Japanese You #4 rotated envelope.
        /// </summary>
        JapaneseEnvelopeYouNumber4Rotated = SafeNativeMethods.DMPAPER_JENV_YOU4_ROTATED,
        /// <summary>
        /// PRC 16K paper (146 mm by 215 mm).
        /// </summary>
        Prc16K = SafeNativeMethods.DMPAPER_P16K,
        /// <summary>
        /// PRC 32K paper (97 mm by 151 mm).
        /// </summary>
        Prc32K = SafeNativeMethods.DMPAPER_P32K,
        /// <summary>
        /// PRC 32K big paper (97 mm by 151 mm).
        /// </summary>
        Prc32KBig = SafeNativeMethods.DMPAPER_P32KBIG,
        /// <summary>
        /// PRC #1 envelope (102 mm by 165 mm).
        /// </summary>
        PrcEnvelopeNumber1 = SafeNativeMethods.DMPAPER_PENV_1,
        /// <summary>
        /// PRC #2 envelope (102 mm by 176 mm).
        /// </summary>
        PrcEnvelopeNumber2 = SafeNativeMethods.DMPAPER_PENV_2,
        /// <summary>
        /// PRC #3 envelope (125 mm by 176 mm).
        /// </summary>
        PrcEnvelopeNumber3 = SafeNativeMethods.DMPAPER_PENV_3,
        /// <summary>
        /// PRC #4 envelope (110 mm by 208 mm).
        /// </summary>
        PrcEnvelopeNumber4 = SafeNativeMethods.DMPAPER_PENV_4,
        /// <summary>
        /// PRC #5 envelope (110 mm by 220 mm).
        /// </summary>
        PrcEnvelopeNumber5 = SafeNativeMethods.DMPAPER_PENV_5,
        /// <summary>
        /// PRC #6 envelope (120 mm by 230 mm).
        /// </summary>
        PrcEnvelopeNumber6 = SafeNativeMethods.DMPAPER_PENV_6,
        /// <summary>
        /// PRC #7 envelope (160 mm by 230 mm).
        /// </summary>
        PrcEnvelopeNumber7 = SafeNativeMethods.DMPAPER_PENV_7,
        /// <summary>
        /// PRC #8 envelope (120 mm by 309 mm).
        /// </summary>
        PrcEnvelopeNumber8 = SafeNativeMethods.DMPAPER_PENV_8,
        /// <summary>
        /// PRC #9 envelope (229 mm by 324 mm).
        /// </summary>
        PrcEnvelopeNumber9 = SafeNativeMethods.DMPAPER_PENV_9,
        /// <summary>
        /// PRC #10 envelope (324 mm by 458 mm).
        /// </summary>
        PrcEnvelopeNumber10 = SafeNativeMethods.DMPAPER_PENV_10,
        /// <summary>
        /// PRC 16K rotated paper (146 mm by 215 mm).
        /// </summary>
        Prc16KRotated = SafeNativeMethods.DMPAPER_P16K_ROTATED,
        /// <summary>
        /// PRC 32K rotated paper (97 mm by 151 mm).
        /// </summary>
        Prc32KRotated = SafeNativeMethods.DMPAPER_P32K_ROTATED,
        /// <summary>
        /// PRC 32K big rotated paper (97 mm by 151 mm).
        /// </summary>
        Prc32KBigRotated = SafeNativeMethods.DMPAPER_P32KBIG_ROTATED,
        /// <summary>
        /// PRC #1 rotated envelope (165 mm by 102 mm).
        /// </summary>
        PrcEnvelopeNumber1Rotated = SafeNativeMethods.DMPAPER_PENV_1_ROTATED,
        /// <summary>
        /// PRC #2 rotated envelope (176 mm by 102 mm).
        /// </summary>
        PrcEnvelopeNumber2Rotated = SafeNativeMethods.DMPAPER_PENV_2_ROTATED,
        /// <summary>
        /// PRC #3 rotated envelope (176 mm by 125 mm).
        /// </summary>
        PrcEnvelopeNumber3Rotated = SafeNativeMethods.DMPAPER_PENV_3_ROTATED,
        /// <summary>
        /// PRC #4 rotated envelope (208 mm by 110 mm).
        /// </summary>
        PrcEnvelopeNumber4Rotated = SafeNativeMethods.DMPAPER_PENV_4_ROTATED,
        /// <summary>
        /// PRC #5 rotated envelope (220 mm by 110 mm).
        /// </summary>
        PrcEnvelopeNumber5Rotated = SafeNativeMethods.DMPAPER_PENV_5_ROTATED,
        /// <summary>
        /// PRC #6 rotated envelope (230 mm by 120 mm).
        /// </summary>
        PrcEnvelopeNumber6Rotated = SafeNativeMethods.DMPAPER_PENV_6_ROTATED,
        /// <summary>
        /// PRC #7 rotated envelope (230 mm by 160 mm).
        /// </summary>
        PrcEnvelopeNumber7Rotated = SafeNativeMethods.DMPAPER_PENV_7_ROTATED,
        /// <summary>
        /// PRC #8 rotated envelope (309 mm by 120 mm).
        /// </summary>
        PrcEnvelopeNumber8Rotated = SafeNativeMethods.DMPAPER_PENV_8_ROTATED,
        /// <summary>
        /// PRC #9 rotated envelope (324 mm by 229 mm).
        /// </summary>
        PrcEnvelopeNumber9Rotated = SafeNativeMethods.DMPAPER_PENV_9_ROTATED,
        /// <summary>
        /// PRC #10 rotated envelope (458 mm by 324 mm).
        /// </summary>
        PrcEnvelopeNumber10Rotated = SafeNativeMethods.DMPAPER_PENV_10_ROTATED,
    }
}
