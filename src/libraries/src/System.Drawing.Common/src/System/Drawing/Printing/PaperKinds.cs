// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Printing
{
    /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Specifies the standard paper sizes.
    ///    </para>
    /// </devdoc>
    [Serializable]
    public enum PaperKind
    {
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.Custom"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The paper size is defined by the user.
        ///    </para>
        /// </devdoc>
        Custom = 0,

        // I got this information from two places: MSDN's writeup of DEVMODE 
        // (http://msdn.microsoft.com/library/psdk/gdi/prntspol_8nle.htm), 
        // and the raw C++ header file (wingdi.h).  Beyond that, your guess
        // is as good as mine as to what these members mean.


        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.Letter"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Letter paper (8.5 in.
        ///       by 11 in.).
        ///    </para>
        /// </devdoc>
        Letter = SafeNativeMethods.DMPAPER_LETTER,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.Legal"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Legal paper (8.5 in.
        ///       by 14
        ///       in.).
        ///    </para>
        /// </devdoc>
        Legal = SafeNativeMethods.DMPAPER_LEGAL,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.A4"]/*' />
        /// <devdoc>
        ///    <para>
        ///       A4 paper (210
        ///       mm by 297
        ///       mm).
        ///    </para>
        /// </devdoc>
        A4 = SafeNativeMethods.DMPAPER_A4,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.CSheet"]/*' />
        /// <devdoc>
        ///    C paper (17 in. by 22 in.).
        /// </devdoc>
        CSheet = SafeNativeMethods.DMPAPER_CSHEET,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.DSheet"]/*' />
        /// <devdoc>
        ///    <para>
        ///       D paper (22
        ///       in. by 34 in.).
        ///    </para>
        /// </devdoc>
        DSheet = SafeNativeMethods.DMPAPER_DSHEET,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.ESheet"]/*' />
        /// <devdoc>
        ///    <para>
        ///       E paper (34
        ///       in. by 44 in.).
        ///    </para>
        /// </devdoc>
        ESheet = SafeNativeMethods.DMPAPER_ESHEET,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.LetterSmall"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Letter small paper (8.5 in. by 11 in.).
        ///    </para>
        /// </devdoc>
        LetterSmall = SafeNativeMethods.DMPAPER_LETTERSMALL,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.Tabloid"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Tabloid paper (11
        ///       in. by 17 in.).
        ///    </para>
        /// </devdoc>
        Tabloid = SafeNativeMethods.DMPAPER_TABLOID,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.Ledger"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Ledger paper (17
        ///       in. by 11 in.).
        ///    </para>
        /// </devdoc>
        Ledger = SafeNativeMethods.DMPAPER_LEDGER,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.Statement"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Statement paper (5.5
        ///       in. by 8.5 in.).
        ///    </para>
        /// </devdoc>
        Statement = SafeNativeMethods.DMPAPER_STATEMENT,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.Executive"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Executive paper (7.25
        ///       in. by 10.5
        ///       in.).
        ///    </para>
        /// </devdoc>
        Executive = SafeNativeMethods.DMPAPER_EXECUTIVE,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.A3"]/*' />
        /// <devdoc>
        ///    <para>
        ///       A3 paper
        ///       (297 mm by 420 mm).
        ///    </para>
        /// </devdoc>
        A3 = SafeNativeMethods.DMPAPER_A3,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.A4Small"]/*' />
        /// <devdoc>
        ///    <para>
        ///       A4 small paper
        ///       (210 mm by 297 mm).
        ///    </para>
        /// </devdoc>
        A4Small = SafeNativeMethods.DMPAPER_A4SMALL,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.A5"]/*' />
        /// <devdoc>
        ///    <para>
        ///       A5 paper (148
        ///       mm by 210
        ///       mm).
        ///    </para>
        /// </devdoc>
        A5 = SafeNativeMethods.DMPAPER_A5,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.B4"]/*' />
        /// <devdoc>
        ///    <para>
        ///       B4 paper (250 mm by 353 mm).
        ///    </para>
        /// </devdoc>
        B4 = SafeNativeMethods.DMPAPER_B4,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.B5"]/*' />
        /// <devdoc>
        ///    <para>
        ///       B5 paper (176
        ///       mm by 250 mm).
        ///    </para>
        /// </devdoc>
        B5 = SafeNativeMethods.DMPAPER_B5,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.Folio"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Folio paper (8.5
        ///       in. by 13 in.).
        ///    </para>
        /// </devdoc>
        Folio = SafeNativeMethods.DMPAPER_FOLIO,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.Quarto"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Quarto paper (215
        ///       mm by 275 mm).
        ///    </para>
        /// </devdoc>
        Quarto = SafeNativeMethods.DMPAPER_QUARTO,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.Standard10x14"]/*' />
        /// <devdoc>
        ///    <para>
        ///       10-by-14-inch paper.
        ///       
        ///    </para>
        /// </devdoc>
        Standard10x14 = SafeNativeMethods.DMPAPER_10X14,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.Standard11x17"]/*' />
        /// <devdoc>
        ///    <para>
        ///       11-by-17-inch paper.
        ///       
        ///    </para>
        /// </devdoc>
        Standard11x17 = SafeNativeMethods.DMPAPER_11X17,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.Note"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Note paper (8.5 in.
        ///       by 11 in.).
        ///    </para>
        /// </devdoc>
        Note = SafeNativeMethods.DMPAPER_NOTE,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.Number9Envelope"]/*' />
        /// <devdoc>
        ///    <para>
        ///       #9 envelope (3.875
        ///       in. by 8.875 in.).
        ///    </para>
        /// </devdoc>
        Number9Envelope = SafeNativeMethods.DMPAPER_ENV_9,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.Number10Envelope"]/*' />
        /// <devdoc>
        ///    <para>
        ///       #10 envelope
        ///       (4.125 in. by 9.5 in.).
        ///    </para>
        /// </devdoc>
        Number10Envelope = SafeNativeMethods.DMPAPER_ENV_10,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.Number11Envelope"]/*' />
        /// <devdoc>
        ///    <para>
        ///       #11 envelope (4.5
        ///       in. by 10.375 in.).
        ///    </para>
        /// </devdoc>
        Number11Envelope = SafeNativeMethods.DMPAPER_ENV_11,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.Number12Envelope"]/*' />
        /// <devdoc>
        ///    <para>
        ///       #12 envelope (4.75
        ///       in. by 11 in.).
        ///    </para>
        /// </devdoc>
        Number12Envelope = SafeNativeMethods.DMPAPER_ENV_12,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.Number14Envelope"]/*' />
        /// <devdoc>
        ///    <para>
        ///       #14 envelope (5 in. by 11.5 in.).
        ///    </para>
        /// </devdoc>
        Number14Envelope = SafeNativeMethods.DMPAPER_ENV_14,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.DLEnvelope"]/*' />
        /// <devdoc>
        ///    <para>
        ///       DL envelope
        ///       (110 mm by 220 mm).
        ///    </para>
        /// </devdoc>
        DLEnvelope = SafeNativeMethods.DMPAPER_ENV_DL,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.C5Envelope"]/*' />
        /// <devdoc>
        ///    <para>
        ///       C5 envelope
        ///       (162 mm by 229 mm).
        ///    </para>
        /// </devdoc>
        C5Envelope = SafeNativeMethods.DMPAPER_ENV_C5,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.C3Envelope"]/*' />
        /// <devdoc>
        ///    C3 envelope (324 mm by 458 mm).
        /// </devdoc>
        C3Envelope = SafeNativeMethods.DMPAPER_ENV_C3,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.C4Envelope"]/*' />
        /// <devdoc>
        ///    <para>
        ///       C4 envelope (229
        ///       mm by 324 mm).
        ///    </para>
        /// </devdoc>
        C4Envelope = SafeNativeMethods.DMPAPER_ENV_C4,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.C6Envelope"]/*' />
        /// <devdoc>
        ///    C6 envelope (114 mm by 162 mm).
        /// </devdoc>
        C6Envelope = SafeNativeMethods.DMPAPER_ENV_C6,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.C65Envelope"]/*' />
        /// <devdoc>
        ///    <para>
        ///       C65 envelope (114
        ///       mm by 229 mm).
        ///    </para>
        /// </devdoc>
        C65Envelope = SafeNativeMethods.DMPAPER_ENV_C65,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.B4Envelope"]/*' />
        /// <devdoc>
        ///    <para>
        ///       B4 envelope (250 mm by 353 mm).
        ///    </para>
        /// </devdoc>
        B4Envelope = SafeNativeMethods.DMPAPER_ENV_B4,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.B5Envelope"]/*' />
        /// <devdoc>
        ///    <para>
        ///       B5 envelope (176
        ///       mm by 250 mm).
        ///    </para>
        /// </devdoc>
        B5Envelope = SafeNativeMethods.DMPAPER_ENV_B5,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.B6Envelope"]/*' />
        /// <devdoc>
        ///    <para>
        ///       B6 envelope (176
        ///       mm by 125 mm).
        ///    </para>
        /// </devdoc>
        B6Envelope = SafeNativeMethods.DMPAPER_ENV_B6,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.ItalyEnvelope"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Italy envelope (110 mm by 230 mm).
        ///    </para>
        /// </devdoc>
        ItalyEnvelope = SafeNativeMethods.DMPAPER_ENV_ITALY,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.MonarchEnvelope"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Monarch envelope (3.875
        ///       in. by 7.5 in.).
        ///    </para>
        /// </devdoc>
        MonarchEnvelope = SafeNativeMethods.DMPAPER_ENV_MONARCH,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.PersonalEnvelope"]/*' />
        /// <devdoc>
        ///    <para>
        ///       6 3/4 envelope
        ///       (3.625 in. by 6.5 in.).
        ///    </para>
        /// </devdoc>
        PersonalEnvelope = SafeNativeMethods.DMPAPER_ENV_PERSONAL,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.USStandardFanfold"]/*' />
        /// <devdoc>
        ///    <para>
        ///       US standard
        ///       fanfold (14.875 in. by 11 in.).
        ///    </para>
        /// </devdoc>
        USStandardFanfold = SafeNativeMethods.DMPAPER_FANFOLD_US,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.GermanStandardFanfold"]/*' />
        /// <devdoc>
        ///    <para>
        ///       German standard fanfold
        ///       (8.5 in. by 12 in.).
        ///    </para>
        /// </devdoc>
        GermanStandardFanfold = SafeNativeMethods.DMPAPER_FANFOLD_STD_GERMAN,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.GermanLegalFanfold"]/*' />
        /// <devdoc>
        ///    <para>
        ///       German legal fanfold
        ///       (8.5 in. by 13 in.).
        ///    </para>
        /// </devdoc>
        GermanLegalFanfold = SafeNativeMethods.DMPAPER_FANFOLD_LGL_GERMAN,

        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.IsoB4"]/*' />
        /// <devdoc>
        ///    <para>
        ///       ISO B4 (250 mm by 353 mm).
        ///    </para>
        /// </devdoc>
        IsoB4 = SafeNativeMethods.DMPAPER_ISO_B4,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.JapanesePostcard"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Japanese postcard (100 mm by 148 mm).
        ///    </para>
        /// </devdoc>
        JapanesePostcard = SafeNativeMethods.DMPAPER_JAPANESE_POSTCARD,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.Standard9x11"]/*' />
        /// <devdoc>
        ///    <para>
        ///       9-by-11-inch
        ///       paper.
        ///       
        ///    </para>
        /// </devdoc>
        Standard9x11 = SafeNativeMethods.DMPAPER_9X11,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.Standard10x11"]/*' />
        /// <devdoc>
        ///    <para>
        ///       10-by-11-inch paper.
        ///       
        ///    </para>
        /// </devdoc>
        Standard10x11 = SafeNativeMethods.DMPAPER_10X11,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.Standard15x11"]/*' />
        /// <devdoc>
        ///    <para>
        ///       15-by-11-inch paper.
        ///       
        ///    </para>
        /// </devdoc>
        Standard15x11 = SafeNativeMethods.DMPAPER_15X11,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.InviteEnvelope"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Invite envelope (220
        ///       mm by 220 mm).
        ///    </para>
        /// </devdoc>
        InviteEnvelope = SafeNativeMethods.DMPAPER_ENV_INVITE,
        //= SafeNativeMethods.DMPAPER_RESERVED_48,
        //= SafeNativeMethods.DMPAPER_RESERVED_49,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.LetterExtra"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Letter extra paper
        ///       (9.275 in. by
        ///       12 in.). This value is specific to the PostScript driver and is used only
        ///       by Linotronic printers in order to conserve paper.
        ///    </para>
        /// </devdoc>
        LetterExtra = SafeNativeMethods.DMPAPER_LETTER_EXTRA,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.LegalExtra"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Legal extra
        ///       paper (9.275 in.
        ///       by 15 in.). This value is specific to the PostScript driver, and is used
        ///       only by Linotronic printers in order to conserve paper.
        ///    </para>
        /// </devdoc>
        LegalExtra = SafeNativeMethods.DMPAPER_LEGAL_EXTRA,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.TabloidExtra"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Tabloid extra paper
        ///       (11.69 in. by 18 in.). This
        ///       value is specific to the PostScript driver and is used only by Linotronic printers in order to conserve paper.
        ///    </para>
        /// </devdoc>
        TabloidExtra = SafeNativeMethods.DMPAPER_TABLOID_EXTRA,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.A4Extra"]/*' />
        /// <devdoc>
        ///    <para>
        ///       A4 extra
        ///       paper
        ///       (236 mm by 322 mm). This value is specific to the PostScript driver and is used only
        ///       by Linotronic printers to help save paper.
        ///    </para>
        /// </devdoc>
        A4Extra = SafeNativeMethods.DMPAPER_A4_EXTRA,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.LetterTransverse"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Letter transverse paper
        ///       (8.275 in. by 11 in.).
        ///    </para>
        /// </devdoc>
        LetterTransverse = SafeNativeMethods.DMPAPER_LETTER_TRANSVERSE,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.A4Transverse"]/*' />
        /// <devdoc>
        ///    <para>
        ///       A4 transverse paper
        ///       (210 mm by 297 mm).
        ///    </para>
        /// </devdoc>
        A4Transverse = SafeNativeMethods.DMPAPER_A4_TRANSVERSE,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.LetterExtraTransverse"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Letter extra transverse
        ///       paper (9.275 in. by 12
        ///       in.).
        ///    </para>
        /// </devdoc>
        LetterExtraTransverse = SafeNativeMethods.DMPAPER_LETTER_EXTRA_TRANSVERSE,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.APlus"]/*' />
        /// <devdoc>
        ///    <para>
        ///       SuperA/SuperA/A4 paper (227
        ///       mm by 356 mm).
        ///    </para>
        /// </devdoc>
        APlus = SafeNativeMethods.DMPAPER_A_PLUS,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.BPlus"]/*' />
        /// <devdoc>
        ///    <para>
        ///       SuperB/SuperB/A3 paper (305
        ///       mm by 487 mm).
        ///    </para>
        /// </devdoc>
        BPlus = SafeNativeMethods.DMPAPER_B_PLUS,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.LetterPlus"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Letter plus paper
        ///       (8.5 in. by 12.69 in.).
        ///    </para>
        /// </devdoc>
        LetterPlus = SafeNativeMethods.DMPAPER_LETTER_PLUS,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.A4Plus"]/*' />
        /// <devdoc>
        ///    <para>
        ///       A4 plus paper
        ///       (210 mm by 330 mm).
        ///    </para>
        /// </devdoc>
        A4Plus = SafeNativeMethods.DMPAPER_A4_PLUS,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.A5Transverse"]/*' />
        /// <devdoc>
        ///    <para>
        ///       A5 transverse paper
        ///       (148 mm by 210
        ///       mm).
        ///    </para>
        /// </devdoc>
        A5Transverse = SafeNativeMethods.DMPAPER_A5_TRANSVERSE,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.B5Transverse"]/*' />
        /// <devdoc>
        ///    <para>
        ///       JIS B5 transverse
        ///       paper (182 mm by 257 mm).
        ///    </para>
        /// </devdoc>
        B5Transverse = SafeNativeMethods.DMPAPER_B5_TRANSVERSE,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.A3Extra"]/*' />
        /// <devdoc>
        ///    <para>
        ///       A3 extra paper
        ///       (322 mm by 445 mm).
        ///    </para>
        /// </devdoc>
        A3Extra = SafeNativeMethods.DMPAPER_A3_EXTRA,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.A5Extra"]/*' />
        /// <devdoc>
        ///    <para>
        ///       A5 extra paper
        ///       (174 mm by 235 mm).
        ///    </para>
        /// </devdoc>
        A5Extra = SafeNativeMethods.DMPAPER_A5_EXTRA,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.B5Extra"]/*' />
        /// <devdoc>
        ///    <para>
        ///       ISO B5 extra
        ///       paper (201 mm by 276 mm).
        ///    </para>
        /// </devdoc>
        B5Extra = SafeNativeMethods.DMPAPER_B5_EXTRA,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.A2"]/*' />
        /// <devdoc>
        ///    <para>
        ///       A2 paper
        ///       (420
        ///       mm by 594 mm).
        ///    </para>
        /// </devdoc>
        A2 = SafeNativeMethods.DMPAPER_A2,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.A3Transverse"]/*' />
        /// <devdoc>
        ///    <para>
        ///       A3 transverse paper
        ///       (297 mm by 420 mm).
        ///    </para>
        /// </devdoc>
        A3Transverse = SafeNativeMethods.DMPAPER_A3_TRANSVERSE,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.A3ExtraTransverse"]/*' />
        /// <devdoc>
        ///    <para>
        ///       A3 extra transverse
        ///       paper (322 mm by 445 mm).
        ///    </para>
        /// </devdoc>
        A3ExtraTransverse = SafeNativeMethods.DMPAPER_A3_EXTRA_TRANSVERSE,
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.JapaneseDoublePostcard"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Japanese double postcard
        ///       (200 mm by 148
        ///       mm). Requires Windows
        ///       98, Windows NT 4.0, or later.
        ///    </para>
        /// </devdoc>
        JapaneseDoublePostcard = SafeNativeMethods.DMPAPER_DBL_JAPANESE_POSTCARD, /* Japanese Double Postcard 200 x 148 mm */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.A6"]/*' />
        /// <devdoc>
        ///    <para>
        ///       A6 paper (105
        ///       mm by 148 mm). Requires
        ///       Windows 98,
        ///       Windows
        ///       NT 4.0, or later.
        ///    </para>
        /// </devdoc>
        A6 = SafeNativeMethods.DMPAPER_A6,  /* A6 105 x 148 mm                 */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.JapaneseEnvelopeKakuNumber2"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Japanese Kaku #2 envelope. Requires Windows
        ///       98, Windows NT 4.0, or later.
        ///    </para>
        /// </devdoc>
        JapaneseEnvelopeKakuNumber2 = SafeNativeMethods.DMPAPER_JENV_KAKU2,  /* Japanese Envelope Kaku #2       */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.JapaneseEnvelopeKakuNumber3"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Japanese Kaku #3 envelope. Requires Windows 98, Windows NT 4.0, or later.
        ///    </para>
        /// </devdoc>
        JapaneseEnvelopeKakuNumber3 = SafeNativeMethods.DMPAPER_JENV_KAKU3,  /* Japanese Envelope Kaku #3       */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.JapaneseEnvelopeChouNumber3"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Japanese Chou #3 envelope. Requires Windows
        ///       98, Windows NT 4.0, or later.
        ///    </para>
        /// </devdoc>
        JapaneseEnvelopeChouNumber3 = SafeNativeMethods.DMPAPER_JENV_CHOU3,  /* Japanese Envelope Chou #3       */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.JapaneseEnvelopeChouNumber4"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Japanese Chou #4 envelope. Requires Windows
        ///       98, Windows NT 4.0, or later.
        ///    </para>
        /// </devdoc>
        JapaneseEnvelopeChouNumber4 = SafeNativeMethods.DMPAPER_JENV_CHOU4,  /* Japanese Envelope Chou #4       */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.LetterRotated"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Letter rotated paper (11
        ///       in. by
        ///       8.5 in.).
        ///    </para>
        /// </devdoc>
        LetterRotated = SafeNativeMethods.DMPAPER_LETTER_ROTATED,  /* Letter Rotated 11 x 8 1/2 11 in */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.A3Rotated"]/*' />
        /// <devdoc>
        ///    <para>
        ///       A3
        ///       rotated paper (420mm by 297 mm).
        ///    </para>
        /// </devdoc>
        A3Rotated = SafeNativeMethods.DMPAPER_A3_ROTATED,  /* A3 Rotated 420 x 297 mm         */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.A4Rotated"]/*' />
        /// <devdoc>
        ///    <para>
        ///       A4 rotated paper
        ///       (297 mm by 210 mm).
        ///       Requires Windows
        ///       98, Windows NT 4.0, or later.
        ///    </para>
        /// </devdoc>
        A4Rotated = SafeNativeMethods.DMPAPER_A4_ROTATED,  /* A4 Rotated 297 x 210 mm         */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.A5Rotated"]/*' />
        /// <devdoc>
        ///    <para>
        ///       A5 rotated paper
        ///       (210 mm by 148 mm).
        ///       Requires Windows
        ///       98, Windows NT 4.0, or later.
        ///    </para>
        /// </devdoc>
        A5Rotated = SafeNativeMethods.DMPAPER_A5_ROTATED,  /* A5 Rotated 210 x 148 mm         */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.B4JisRotated"]/*' />
        /// <devdoc>
        ///    <para>
        ///       JIS B4 rotated
        ///       paper (364 mm by 257
        ///       mm). Requires Windows
        ///       98, Windows NT 4.0, or later.
        ///    </para>
        /// </devdoc>
        B4JisRotated = SafeNativeMethods.DMPAPER_B4_JIS_ROTATED,  /* B4 (JIS) Rotated 364 x 257 mm   */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.B5JisRotated"]/*' />
        /// <devdoc>
        ///    <para>
        ///       JIS B5 rotated
        ///       paper (257 mm by 182
        ///       mm). Requires Windows
        ///       98, Windows NT 4.0, or later.
        ///    </para>
        /// </devdoc>
        B5JisRotated = SafeNativeMethods.DMPAPER_B5_JIS_ROTATED,  /* B5 (JIS) Rotated 257 x 182 mm   */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.JapanesePostcardRotated"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Japanese rotated postcard
        ///       (148 mm by 100
        ///       mm). Requires Windows
        ///       98,
        ///       Windows NT 4.0, or later.
        ///    </para>
        /// </devdoc>
        JapanesePostcardRotated = SafeNativeMethods.DMPAPER_JAPANESE_POSTCARD_ROTATED, /* Japanese Postcard Rotated 148 x 100 mm */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.JapaneseDoublePostcardRotated"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Japanese rotated double
        ///       postcard (148 mm by
        ///       200 mm). Requires
        ///       Windows 98, Windows NT 4.0, or later.
        ///    </para>
        /// </devdoc>
        JapaneseDoublePostcardRotated = SafeNativeMethods.DMPAPER_DBL_JAPANESE_POSTCARD_ROTATED, /* Double Japanese Postcard Rotated 148 x 200 mm */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.A6Rotated"]/*' />
        /// <devdoc>
        ///    <para>
        ///       A6
        ///       rotated paper
        ///       (148 mm by 105 mm).
        ///       Requires Windows
        ///       98, Windows NT 4.0, or later.
        ///    </para>
        /// </devdoc>
        A6Rotated = SafeNativeMethods.DMPAPER_A6_ROTATED,  /* A6 Rotated 148 x 105 mm         */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.JapaneseEnvelopeKakuNumber2Rotated"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Japanese rotated Kaku #2 envelope. Requires
        ///       Windows 98, Windows NT 4.0, or later.
        ///    </para>
        /// </devdoc>
        JapaneseEnvelopeKakuNumber2Rotated = SafeNativeMethods.DMPAPER_JENV_KAKU2_ROTATED,  /* Japanese Envelope Kaku #2 Rotated */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.JapaneseEnvelopeKakuNumber3Rotated"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Japanese rotated Kaku #3 envelope. Requires
        ///       Windows 98, Windows NT 4.0, or later.
        ///    </para>
        /// </devdoc>
        JapaneseEnvelopeKakuNumber3Rotated = SafeNativeMethods.DMPAPER_JENV_KAKU3_ROTATED,  /* Japanese Envelope Kaku #3 Rotated */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.JapaneseEnvelopeChouNumber3Rotated"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Japanese rotated Chou #3 envelope. Requires
        ///       Windows 98, Windows NT 4.0, or later.
        ///    </para>
        /// </devdoc>
        JapaneseEnvelopeChouNumber3Rotated = SafeNativeMethods.DMPAPER_JENV_CHOU3_ROTATED,  /* Japanese Envelope Chou #3 Rotated */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.JapaneseEnvelopeChouNumber4Rotated"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Japanese rotated Chou #4 envelope. Requires
        ///       Windows 98, Windows NT 4.0, or later.
        ///    </para>
        /// </devdoc>
        JapaneseEnvelopeChouNumber4Rotated = SafeNativeMethods.DMPAPER_JENV_CHOU4_ROTATED,  /* Japanese Envelope Chou #4 Rotated */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.B6Jis"]/*' />
        /// <devdoc>
        ///    <para>
        ///       JIS B6 paper
        ///       (128 mm by 182 mm).
        ///       Requires Windows 98,
        ///       Windows NT 4.0, or later.
        ///    </para>
        /// </devdoc>
        B6Jis = SafeNativeMethods.DMPAPER_B6_JIS,  /* B6 (JIS) 128 x 182 mm           */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.B6JisRotated"]/*' />
        /// <devdoc>
        ///    <para>
        ///       JIS B6
        ///       rotated paper (182 mm by 128
        ///       mm). Requires Windows
        ///       98, Windows NT 4.0, or later.
        ///    </para>
        /// </devdoc>
        B6JisRotated = SafeNativeMethods.DMPAPER_B6_JIS_ROTATED,  /* B6 (JIS) Rotated 182 x 128 mm   */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.Standard12x11"]/*' />
        /// <devdoc>
        ///    <para>
        ///       12-by-11-inch paper. Requires Windows 98,
        ///       Windows
        ///       NT 4.0, or later.
        ///    </para>
        /// </devdoc>
        Standard12x11 = SafeNativeMethods.DMPAPER_12X11,  /* 12 x 11 in                      */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.JapaneseEnvelopeYouNumber4"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Japanese You #4 envelope. Requires Windows
        ///       98, Windows NT 4.0, or later.
        ///    </para>
        /// </devdoc>
        JapaneseEnvelopeYouNumber4 = SafeNativeMethods.DMPAPER_JENV_YOU4,  /* Japanese Envelope You #4        */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.JapaneseEnvelopeYouNumber4Rotated"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Japanese You #4 rotated envelope. Requires
        ///       Windows 98, Windows NT 4.0, or later.
        ///    </para>
        /// </devdoc>
        JapaneseEnvelopeYouNumber4Rotated = SafeNativeMethods.DMPAPER_JENV_YOU4_ROTATED,  /* Japanese Envelope You #4 Rotated*/
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.Prc16K"]/*' />
        /// <devdoc>
        ///    <para>
        ///       PRC 16K paper (146 mm
        ///       by 215
        ///       mm). Requires Windows
        ///       98, Windows NT 4.0,
        ///       or later.
        ///    </para>
        /// </devdoc>
        Prc16K = SafeNativeMethods.DMPAPER_P16K,  /* PRC 16K 146 x 215 mm            */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.Prc32K"]/*' />
        /// <devdoc>
        ///    <para>
        ///       PRC 32K paper (97 mm
        ///       by 151
        ///       mm). Requires Windows 98, Windows
        ///       NT 4.0, or later.
        ///    </para>
        /// </devdoc>
        Prc32K = SafeNativeMethods.DMPAPER_P32K,  /* PRC 32K 97 x 151 mm             */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.Prc32KBig"]/*' />
        /// <devdoc>
        ///    <para>
        ///       PRC 32K big paper (97
        ///       mm by
        ///       151 mm). Requires Windows 98, Windows
        ///       NT 4.0, or later.
        ///    </para>
        /// </devdoc>
        Prc32KBig = SafeNativeMethods.DMPAPER_P32KBIG,  /* PRC 32K(Big) 97 x 151 mm        */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.PrcEnvelopeNumber1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       PRC #1 envelope (102 mm
        ///       by 165
        ///       mm). Requires Windows 98, Windows NT 4.0, or later.
        ///    </para>
        /// </devdoc>
        PrcEnvelopeNumber1 = SafeNativeMethods.DMPAPER_PENV_1,  /* PRC Envelope #1 102 x 165 mm    */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.PrcEnvelopeNumber2"]/*' />
        /// <devdoc>
        ///    <para>
        ///       PRC #2 envelope (102 mm
        ///       by 176
        ///       mm). Requires Windows 98, Windows NT 4.0, or later.
        ///    </para>
        /// </devdoc>
        PrcEnvelopeNumber2 = SafeNativeMethods.DMPAPER_PENV_2,  /* PRC Envelope #2 102 x 176 mm    */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.PrcEnvelopeNumber3"]/*' />
        /// <devdoc>
        ///    <para>
        ///       PRC #3 envelope (125 mm
        ///       by 176
        ///       mm). Requires Windows 98, Windows NT 4.0, or later.
        ///    </para>
        /// </devdoc>
        PrcEnvelopeNumber3 = SafeNativeMethods.DMPAPER_PENV_3,  /* PRC Envelope #3 125 x 176 mm    */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.PrcEnvelopeNumber4"]/*' />
        /// <devdoc>
        ///    <para>
        ///       PRC #4 envelope (110 mm
        ///       by 208
        ///       mm). Requires Windows 98, Windows NT 4.0, or later.
        ///    </para>
        /// </devdoc>
        PrcEnvelopeNumber4 = SafeNativeMethods.DMPAPER_PENV_4,  /* PRC Envelope #4 110 x 208 mm    */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.PrcEnvelopeNumber5"]/*' />
        /// <devdoc>
        ///    <para>
        ///       PRC #5 envelope (110 mm by 220 mm). Requires Windows 98, Windows NT 4.0, or later.
        ///    </para>
        /// </devdoc>
        PrcEnvelopeNumber5 = SafeNativeMethods.DMPAPER_PENV_5, /* PRC Envelope #5 110 x 220 mm    */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.PrcEnvelopeNumber6"]/*' />
        /// <devdoc>
        ///    <para>
        ///       PRC #6 envelope (120 mm by 230 mm). Requires Windows 98, Windows NT 4.0, or later.
        ///    </para>
        /// </devdoc>
        PrcEnvelopeNumber6 = SafeNativeMethods.DMPAPER_PENV_6, /* PRC Envelope #6 120 x 230 mm    */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.PrcEnvelopeNumber7"]/*' />
        /// <devdoc>
        ///    <para>
        ///       PRC #7 envelope (160 mm
        ///       by 230
        ///       mm). Requires Windows 98, Windows NT 4.0, or later.
        ///    </para>
        /// </devdoc>
        PrcEnvelopeNumber7 = SafeNativeMethods.DMPAPER_PENV_7, /* PRC Envelope #7 160 x 230 mm    */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.PrcEnvelopeNumber8"]/*' />
        /// <devdoc>
        ///    <para>
        ///       PRC #8 envelope (120 mm
        ///       by 309
        ///       mm). Requires Windows 98, Windows NT 4.0, or later.
        ///    </para>
        /// </devdoc>
        PrcEnvelopeNumber8 = SafeNativeMethods.DMPAPER_PENV_8, /* PRC Envelope #8 120 x 309 mm    */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.PrcEnvelopeNumber9"]/*' />
        /// <devdoc>
        ///    <para>
        ///       PRC #9 envelope (229 mm by 324 mm). Requires Windows 98, Windows NT 4.0, or later.
        ///    </para>
        /// </devdoc>
        PrcEnvelopeNumber9 = SafeNativeMethods.DMPAPER_PENV_9, /* PRC Envelope #9 229 x 324 mm    */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.PrcEnvelopeNumber10"]/*' />
        /// <devdoc>
        ///    <para>
        ///       PRC #10 envelope (324 mm
        ///       by 458
        ///       mm). Requires Windows 98, Windows NT 4.0, or
        ///       later.
        ///    </para>
        /// </devdoc>
        PrcEnvelopeNumber10 = SafeNativeMethods.DMPAPER_PENV_10, /* PRC Envelope #10 324 x 458 mm   */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.Prc16KRotated"]/*' />
        /// <devdoc>
        ///    <para>
        ///       PRC 16K rotated paper (146 mm by 215 mm). Requires Windows 98, Windows NT 4.0, or later.
        ///    </para>
        /// </devdoc>
        Prc16KRotated = SafeNativeMethods.DMPAPER_P16K_ROTATED, /* PRC 16K Rotated                 */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.Prc32KRotated"]/*' />
        /// <devdoc>
        ///    <para>
        ///       PRC 32K rotated paper (97 mm by 151
        ///       mm). Requires Windows 98, Windows NT 4.0, or later.
        ///    </para>
        /// </devdoc>
        Prc32KRotated = SafeNativeMethods.DMPAPER_P32K_ROTATED, /* PRC 32K Rotated                 */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.Prc32KBigRotated"]/*' />
        /// <devdoc>
        ///    <para>
        ///       PRC 32K big rotated paper (97 mm by 151 mm). Requires Windows 98, Windows NT 4.0, or later.
        ///    </para>
        /// </devdoc>
        Prc32KBigRotated = SafeNativeMethods.DMPAPER_P32KBIG_ROTATED, /* PRC 32K(Big) Rotated            */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.PrcEnvelopeNumber1Rotated"]/*' />
        /// <devdoc>
        ///    <para>
        ///       PRC #1 rotated envelope (165 mm by 102 mm). Requires Windows 98, Windows NT 4.0, or later.
        ///    </para>
        /// </devdoc>
        PrcEnvelopeNumber1Rotated = SafeNativeMethods.DMPAPER_PENV_1_ROTATED, /* PRC Envelope #1 Rotated 165 x 102 mm */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.PrcEnvelopeNumber2Rotated"]/*' />
        /// <devdoc>
        ///    <para>
        ///       PRC #2 rotated envelope
        ///       (176 mm by
        ///       102 mm). Requires Windows 98, Windows NT 4.0, or
        ///       later.
        ///    </para>
        /// </devdoc>
        PrcEnvelopeNumber2Rotated = SafeNativeMethods.DMPAPER_PENV_2_ROTATED, /* PRC Envelope #2 Rotated 176 x 102 mm */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.PrcEnvelopeNumber3Rotated"]/*' />
        /// <devdoc>
        ///    <para>
        ///       PRC #3 rotated envelope
        ///       (176 mm by
        ///       125 mm). Requires Windows 98, Windows NT 4.0, or
        ///       later.
        ///    </para>
        /// </devdoc>
        PrcEnvelopeNumber3Rotated = SafeNativeMethods.DMPAPER_PENV_3_ROTATED, /* PRC Envelope #3 Rotated 176 x 125 mm */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.PrcEnvelopeNumber4Rotated"]/*' />
        /// <devdoc>
        ///    <para>
        ///       PRC #4 rotated envelope (208 mm by 110 mm). Requires Windows 98, Windows NT 4.0, or later.
        ///    </para>
        /// </devdoc>
        PrcEnvelopeNumber4Rotated = SafeNativeMethods.DMPAPER_PENV_4_ROTATED, /* PRC Envelope #4 Rotated 208 x 110 mm */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.PrcEnvelopeNumber5Rotated"]/*' />
        /// <devdoc>
        ///    <para>
        ///       PRC #5 rotated envelope (220 mm by 110 mm). Requires Windows 98, Windows NT 4.0, or
        ///       later.
        ///    </para>
        /// </devdoc>
        PrcEnvelopeNumber5Rotated = SafeNativeMethods.DMPAPER_PENV_5_ROTATED, /* PRC Envelope #5 Rotated 220 x 110 mm */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.PrcEnvelopeNumber6Rotated"]/*' />
        /// <devdoc>
        ///    <para>
        ///       PRC #6 rotated envelope (230 mm by 120 mm). Requires Windows 98, Windows NT 4.0, or later.
        ///    </para>
        /// </devdoc>
        PrcEnvelopeNumber6Rotated = SafeNativeMethods.DMPAPER_PENV_6_ROTATED, /* PRC Envelope #6 Rotated 230 x 120 mm */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.PrcEnvelopeNumber7Rotated"]/*' />
        /// <devdoc>
        ///    <para>
        ///       PRC #7 rotated envelope (230 mm by 160 mm). Requires Windows 98, Windows NT 4.0, or later.
        ///    </para>
        /// </devdoc>
        PrcEnvelopeNumber7Rotated = SafeNativeMethods.DMPAPER_PENV_7_ROTATED, /* PRC Envelope #7 Rotated 230 x 160 mm */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.PrcEnvelopeNumber8Rotated"]/*' />
        /// <devdoc>
        ///    <para>
        ///       PRC #8 rotated
        ///       envelope (309 mm
        ///       by 120
        ///       mm). Requires Windows 98, Windows NT 4.0, or later.
        ///    </para>
        /// </devdoc>
        PrcEnvelopeNumber8Rotated = SafeNativeMethods.DMPAPER_PENV_8_ROTATED, /* PRC Envelope #8 Rotated 309 x 120 mm */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.PrcEnvelopeNumber9Rotated"]/*' />
        /// <devdoc>
        ///    <para>
        ///       PRC #9 rotated envelope (324 mm by 229 mm). Requires Windows 98, Windows NT 4.0, or later.
        ///    </para>
        /// </devdoc>
        PrcEnvelopeNumber9Rotated = SafeNativeMethods.DMPAPER_PENV_9_ROTATED, /* PRC Envelope #9 Rotated 324 x 229 mm */
        /// <include file='doc\PaperKinds.uex' path='docs/doc[@for="PaperKind.PrcEnvelopeNumber10Rotated"]/*' />
        /// <devdoc>
        ///    <para>
        ///       PRC #10 rotated envelope (458 mm by 324 mm). Requires Windows 98, Windows NT 4.0, or later.
        ///    </para>
        /// </devdoc>
        PrcEnvelopeNumber10Rotated = SafeNativeMethods.DMPAPER_PENV_10_ROTATED, /* PRC Envelope #10 Rotated 458 x 324 mm */

        // Other useful values: SafeNativeMethods.DMPAPER_LAST, SafeNativeMethods.DMPAPER_USER
    }
}
