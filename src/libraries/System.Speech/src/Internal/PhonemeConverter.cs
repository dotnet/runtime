// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;

namespace System.Speech.Internal
{
    internal sealed class PhonemeConverter
    {
        #region Constructors

        private PhonemeConverter(PhoneMap phoneMap)
        {
            _phoneMap = phoneMap;
        }

        #endregion

        #region Internal methods

        /// <summary>
        /// Returns the cached version of the universal phone converter.
        /// </summary>
        internal static PhonemeConverter UpsConverter
        {
            get
            {
                return s_upsConverter;
            }
        }

        /// <summary>
        /// Convert a pronunciation string to code points
        /// </summary>
        internal static string ConvertPronToId(string pronunciation, int lcid)
        {
            PhonemeConverter phoneConv = UpsConverter;
            foreach (PhoneMap phoneMap in s_phoneMaps)
            {
                if (phoneMap._lcid == lcid)
                {
                    phoneConv = new PhonemeConverter(phoneMap);
                }
            }

            string phonemes = phoneConv.ConvertPronToId(pronunciation);
            if (string.IsNullOrEmpty(phonemes))
            {
                throw new FormatException(SR.Get(SRID.EmptyPronunciationString));
            }
            return phonemes;
        }

        /// <summary>
        /// Convert an internal phone string to Id code string
        /// The internal phones are space separated and may have a space
        /// at the end.
        /// </summary>
        internal string ConvertPronToId(string sPhone)    // Internal phone string
        {
            // remove the white spaces
            sPhone = sPhone.Trim(Helpers._achTrimChars);

            // Empty Phoneme string
            if (string.IsNullOrEmpty(sPhone))
            {
                return string.Empty;
            }

            int iPos = 0, iPosNext;
            int cLen = sPhone.Length;
            StringBuilder pidArray = new(cLen);
            PhoneId phoneIdRef = new();

            while (iPos < cLen)
            {
                iPosNext = sPhone.IndexOf(' ', iPos + 1);
                if (iPosNext < 0)
                {
                    iPosNext = cLen;
                }

                string sCur = sPhone.Substring(iPos, iPosNext - iPos);
                string sCurUpper = sCur.ToUpperInvariant();

                // Search for this phone
                phoneIdRef._phone = sCurUpper;
                int index = Array.BinarySearch<PhoneId>(_phoneMap._phoneIds, phoneIdRef, phoneIdRef);
                if (index >= 0)
                {
                    foreach (char ch in _phoneMap._phoneIds[index]._cp)
                    {
                        pidArray.Append(ch);
                    }
                }
                else
                {
                    // phoneme not found error out
                    throw new FormatException(SR.Get(SRID.InvalidPhoneme, sCur));
                }

                iPos = iPosNext;

                // skip over the spaces
                while (iPos < cLen && sPhone[iPos] == ' ')
                {
                    iPos++;
                }
            }

            return pidArray.ToString();
        } /* CSpPhoneConverter::PhoneToId */

        internal static void ValidateUpsIds(string ids)
        {
            ValidateUpsIds(ids.ToCharArray());
        }

        internal static void ValidateUpsIds(char[] ids)
        {
            foreach (char id in ids)
            {
                if (Array.BinarySearch(s_updIds, id) < 0)
                {
                    throw new FormatException(SR.Get(SRID.InvalidPhoneme, id));
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Builds the Phoneme maps from the compressed form.
        /// </summary>
        private static PhoneMap[] DecompressPhoneMaps(PhoneMapCompressed[] pmComps)
        {
            PhoneMap[] phoneMaps = new PhoneMap[pmComps.Length];

            // Build the phoneme maps
            for (int i = 0; i < pmComps.Length; i++)
            {
                PhoneMapCompressed pmCompressed = pmComps[i];
                PhoneMap pm = phoneMaps[i] = new PhoneMap();
                pm._lcid = pmCompressed._lcid;
                pm._phoneIds = new PhoneId[pmCompressed._count];

                int posPhone = 0;
                int posCp = 0;
                for (int j = 0; j < pm._phoneIds.Length; j++)
                {
                    pm._phoneIds[j] = new PhoneId();
                    // Count the number of chars in the phoneme string
                    int lastPhone;
                    int multi_phones = 0;
                    for (lastPhone = posPhone; pmCompressed._phones[lastPhone] != 0; lastPhone++)
                    {
                        // All phoneme code points are assumed to be of length == 1
                        // if the length is greater, then a marker of -1 is set for each additional code points
                        if (pmCompressed._phones[lastPhone] == unchecked((byte)-1))
                        {
                            multi_phones++;
                        }
                    }

                    // Build the phoneme string
                    int strLen = lastPhone - posPhone - multi_phones;
                    char[] phone = new char[strLen];
                    for (int l = 0; l < strLen; l++)
                    {
                        phone[l] = (char)pmCompressed._phones[posPhone++];
                    }

                    // Update the index for the next phoneme string
                    posPhone += multi_phones + 1;

                    // Copy the code points for this phoneme
                    pm._phoneIds[j]._phone = new string(phone);
                    pm._phoneIds[j]._cp = new char[multi_phones + 1];
                    for (int l = 0; l < pm._phoneIds[j]._cp.Length; l++)
                    {
                        pm._phoneIds[j]._cp[l] = pmCompressed._cps[posCp++];
                    }
                }

                // Ensure that the table is built properly
                System.Diagnostics.Debug.Assert(posPhone == pmCompressed._phones.Length);
                System.Diagnostics.Debug.Assert(posCp == pmCompressed._cps.Length);
            }
            return phoneMaps;
        }

        // Do not delete generation of the phone conversion table from the registry entries

        #endregion

        #region Private Fields

        private PhoneMap _phoneMap;

        private static PhoneMapCompressed[] s_phoneMapsCompressed = new PhoneMapCompressed[]
        {
            new PhoneMapCompressed ( 0x0, 207, new byte [] {46, 0, 95, 33, 0, 95, 38, 0, 95, 44, 0, 95, 46, 0, 95, 63, 0, 95, 94, 0, 95, 124, 0, 95, 124, 124, 0, 95, 83, 0, 43, 0, 65, 0, 65, 65, 0, 65, 68, 86, 0, 65, 69, 0, 65, 69, 88, 0, 65, 72, 0, 65, 73, 255, 255, 0, 65, 78, 255, 0, 65, 79, 0, 65, 79, 69, 0, 65, 79, 88, 255, 255, 0, 65, 80, 73, 0, 65, 83, 80, 0, 65, 84, 82, 0, 65, 85, 255, 255, 0, 65, 88, 0, 65, 88, 82, 0, 66, 0, 66, 66, 0, 66, 72, 0, 66, 73, 77, 0, 66, 86, 65, 0, 66, 86, 68, 0, 67, 0, 67, 67, 255, 255, 0, 67, 67, 50, 0, 67, 67, 75, 0, 67, 69, 78, 0, 67, 72, 255, 255, 0, 67, 72, 50, 0, 67, 74, 0, 67, 84, 0, 67, 86, 68, 0, 68, 0, 68, 69, 78, 0, 68, 72, 0, 68, 73, 77, 0, 68, 82, 0, 68, 88, 0, 68, 88, 82, 0, 68, 90, 255, 255, 0, 68, 90, 50, 0, 69, 0, 69, 72, 0, 69, 72, 88, 255, 255, 0, 69, 73, 255, 255, 0, 69, 74, 67, 0, 69, 78, 255, 0, 69, 82, 0, 69, 82, 82, 0, 69, 83, 72, 0, 69, 84, 0, 69, 85, 0, 69, 88, 0, 69, 90, 72, 0, 70, 0, 71, 0, 71, 50, 0, 71, 65, 0, 71, 72, 0, 71, 73, 77, 0, 71, 76, 0, 71, 84, 0, 72, 0, 72, 71, 0, 72, 72, 0, 72, 76, 71, 0, 72, 90, 0, 73, 0, 73, 72, 0, 73, 88, 0, 73, 89, 88, 255, 255, 0, 74, 0, 74, 67, 255, 255, 0, 74, 67, 50, 0, 74, 68, 0, 74, 72, 255, 255, 0, 74, 72, 50, 0, 74, 73, 77, 0, 74, 74, 255, 255, 0, 75, 0, 76, 0, 76, 65, 66, 0, 76, 65, 77, 0, 76, 65, 82, 0, 76, 67, 75, 0, 76, 67, 75, 50, 0, 76, 71, 0, 76, 72, 0, 76, 74, 0, 76, 76, 65, 0, 76, 78, 71, 0, 76, 79, 87, 0, 76, 82, 0, 76, 82, 68, 0, 76, 83, 72, 0, 76, 84, 0, 77, 0, 77, 67, 78, 0, 77, 70, 0, 77, 82, 68, 0, 78, 0, 78, 65, 82, 0, 78, 65, 83, 0, 78, 67, 75, 0, 78, 67, 75, 50, 0, 78, 67, 75, 51, 0, 78, 71, 0, 78, 74, 0, 78, 82, 0, 78, 83, 82, 0, 78, 83, 89, 0, 79, 0, 79, 69, 0, 79, 69, 78, 255, 0, 79, 73, 255, 255, 0, 79, 78, 255, 0, 79, 85, 0, 79, 87, 88, 255, 255, 0, 79, 88, 0, 80, 0, 80, 65, 76, 0, 80, 67, 75, 0, 80, 70, 255, 255, 0, 80, 72, 0, 80, 72, 82, 0, 81, 0, 81, 68, 0, 81, 72, 0, 81, 73, 77, 0, 81, 78, 0, 81, 79, 77, 0, 81, 81, 0, 81, 84, 0, 82, 0, 82, 65, 0, 82, 65, 73, 0, 82, 69, 84, 0, 82, 72, 0, 82, 72, 79, 0, 82, 72, 90, 0, 82, 82, 0, 82, 84, 69, 0, 82, 84, 82, 0, 83, 0, 83, 49, 0, 83, 50, 0, 83, 67, 0, 83, 72, 0, 83, 72, 67, 0, 83, 72, 88, 0, 83, 82, 0, 83, 89, 76, 0, 84, 0, 84, 45, 0, 84, 43, 0, 84, 61, 0, 84, 49, 0, 84, 50, 0, 84, 51, 0, 84, 52, 0, 84, 53, 0, 84, 67, 75, 0, 84, 67, 75, 50, 0, 84, 72, 0, 84, 82, 0, 84, 83, 255, 255, 0, 84, 83, 50, 0, 84, 83, 82, 255, 255, 0, 85, 0, 85, 72, 0, 85, 82, 0, 85, 85, 0, 85, 87, 88, 255, 255, 0, 85, 89, 88, 255, 255, 0, 86, 0, 86, 65, 0, 86, 67, 68, 0, 86, 69, 76, 0, 86, 76, 83, 0, 86, 80, 72, 0, 86, 83, 76, 0, 87, 0, 87, 72, 0, 87, 74, 0, 88, 0, 88, 83, 72, 0, 88, 83, 84, 0, 89, 0, 89, 72, 0, 89, 88, 0, 90, 0, 90, 67, 0, 90, 72, 0, 90, 72, 74, 0, 90, 82, 0}, new char [] {(char) 46, (char) 1, (char) 2, (char) 3, (char) 8600, (char) 8599, (char) 8255, (char) 124, (char) 8214, (char) 4, (char) 865, (char) 97, (char) 593, (char) 799, (char) 230, (char) 592, (char) 652, (char) 97, (char) 865, (char) 105, (char) 97, (char) 771, (char) 596, (char) 630, (char) 596, (char) 865, (char) 601, (char) 826, (char) 688, (char) 792, (char) 97, (char) 865, (char) 650, (char) 601, (char) 602, (char) 98, (char) 665, (char) 946, (char) 595, (char) 689, (char) 804, (char) 231, (char) 1856, (char) 865, (char) 597, (char) 680, (char) 450, (char) 776, (char) 116, (char) 865, (char) 643, (char) 679, (char) 669, (char) 99, (char) 816, (char) 100, (char) 810, (char) 240, (char) 599, (char) 598, (char) 638, (char) 637, (char) 100, (char) 865, (char) 122, (char) 675, (char) 101, (char) 603, (char) 603, (char) 865, (char) 601, (char) 101, (char) 865, (char) 105, (char) 700, (char) 101, (char) 771, (char) 604, (char) 605, (char) 668, (char) 673, (char) 248, (char) 600, (char) 674, (char) 102, (char) 103, (char) 609, (char) 624, (char) 611, (char) 608, (char) 671, (char) 660, (char) 104, (char) 661, (char) 295, (char) 721, (char) 614, (char) 105, (char) 618, (char) 616, (char) 105, (char) 865, (char) 601, (char) 106, (char) 100, (char) 865, (char) 657, (char) 677, (char) 607, (char) 100, (char) 865, (char) 658, (char) 676, (char) 644, (char) 106, (char) 865, (char) 106, (char) 107, (char) 108, (char) 695, (char) 827, (char) 737, (char) 449, (char) 662, (char) 619, (char) 622, (char) 654, (char) 828, (char) 720, (char) 798, (char) 621, (char) 796, (char) 620, (char) 634, (char) 109, (char) 829, (char) 625, (char) 825, (char) 110, (char) 794, (char) 771, (char) 33, (char) 451, (char) 663, (char) 331, (char) 626, (char) 627, (char) 8319, (char) 815, (char) 111, (char) 339, (char) 339, (char) 771, (char) 596, (char) 865, (char) 105, (char) 111, (char) 771, (char) 612, (char) 111, (char) 865, (char) 601, (char) 629, (char) 112, (char) 690, (char) 664, (char) 112, (char) 865, (char) 102, (char) 632, (char) 740, (char) 594, (char) 610, (char) 967, (char) 667, (char) 628, (char) 672, (char) 640, (char) 113, (char) 635, (char) 633, (char) 797, (char) 817, (char) 641, (char) 734, (char) 692, (char) 114, (char) 800, (char) 793, (char) 115, (char) 712, (char) 716, (char) 597, (char) 643, (char) 646, (char) 615, (char) 642, (char) 809, (char) 116, (char) 8595, (char) 8593, (char) 8594, (char) 783, (char) 768, (char) 772, (char) 769, (char) 779, (char) 448, (char) 647, (char) 952, (char) 648, (char) 116, (char) 865, (char) 115, (char) 678, (char) 116, (char) 865, (char) 642, (char) 117, (char) 650, (char) 606, (char) 623, (char) 117, (char) 865, (char) 601, (char) 121, (char) 865, (char) 601, (char) 118, (char) 651, (char) 812, (char) 736, (char) 778, (char) 820, (char) 805, (char) 119, (char) 653, (char) 613, (char) 120, (char) 728, (char) 774, (char) 121, (char) 655, (char) 649, (char) 122, (char) 657, (char) 658, (char) 659, (char) 656}),
            new PhoneMapCompressed ( 0x404, 52, new byte [] {48, 48, 50, 49, 0, 48, 48, 50, 54, 0, 48, 48, 50, 65, 0, 48, 48, 50, 66, 0, 48, 48, 50, 67, 0, 48, 48, 50, 68, 0, 48, 48, 50, 69, 0, 48, 48, 51, 70, 0, 48, 48, 53, 70, 0, 48, 50, 67, 55, 0, 48, 50, 67, 57, 0, 48, 50, 67, 65, 0, 48, 50, 67, 66, 0, 48, 50, 68, 57, 0, 51, 48, 48, 48, 0, 51, 49, 48, 53, 0, 51, 49, 48, 54, 0, 51, 49, 48, 55, 0, 51, 49, 48, 56, 0, 51, 49, 48, 57, 0, 51, 49, 48, 65, 0, 51, 49, 48, 66, 0, 51, 49, 48, 67, 0, 51, 49, 48, 68, 0, 51, 49, 48, 69, 0, 51, 49, 48, 70, 0, 51, 49, 49, 48, 0, 51, 49, 49, 49, 0, 51, 49, 49, 50, 0, 51, 49, 49, 51, 0, 51, 49, 49, 52, 0, 51, 49, 49, 53, 0, 51, 49, 49, 54, 0, 51, 49, 49, 55, 0, 51, 49, 49, 56, 0, 51, 49, 49, 57, 0, 51, 49, 49, 65, 0, 51, 49, 49, 66, 0, 51, 49, 49, 67, 0, 51, 49, 49, 68, 0, 51, 49, 49, 69, 0, 51, 49, 49, 70, 0, 51, 49, 50, 48, 0, 51, 49, 50, 49, 0, 51, 49, 50, 50, 0, 51, 49, 50, 51, 0, 51, 49, 50, 52, 0, 51, 49, 50, 53, 0, 51, 49, 50, 54, 0, 51, 49, 50, 55, 0, 51, 49, 50, 56, 0, 51, 49, 50, 57, 0}, new char [] {(char) 33, (char) 38, (char) 42, (char) 43, (char) 44, (char) 45, (char) 46, (char) 63, (char) 95, (char) 711, (char) 713, (char) 714, (char) 715, (char) 729, (char) 12288, (char) 12549, (char) 12550, (char) 12551, (char) 12552, (char) 12553, (char) 12554, (char) 12555, (char) 12556, (char) 12557, (char) 12558, (char) 12559, (char) 12560, (char) 12561, (char) 12562, (char) 12563, (char) 12564, (char) 12565, (char) 12566, (char) 12567, (char) 12568, (char) 12569, (char) 12570, (char) 12571, (char) 12572, (char) 12573, (char) 12574, (char) 12575, (char) 12576, (char) 12577, (char) 12578, (char) 12579, (char) 12580, (char) 12581, (char) 12582, (char) 12583, (char) 12584, (char) 12585}),
            new PhoneMapCompressed ( 0x407, 53, new byte [] {45, 0, 33, 0, 38, 0, 44, 0, 46, 0, 58, 0, 63, 0, 94, 0, 95, 0, 126, 0, 49, 0, 50, 0, 65, 0, 65, 87, 0, 65, 88, 0, 65, 89, 0, 66, 0, 67, 72, 0, 68, 0, 69, 72, 0, 69, 85, 0, 69, 89, 0, 70, 0, 71, 0, 72, 0, 73, 72, 0, 73, 89, 0, 74, 72, 0, 75, 0, 76, 0, 77, 0, 78, 0, 78, 71, 0, 79, 69, 0, 79, 72, 0, 79, 87, 0, 79, 89, 0, 80, 0, 80, 70, 0, 82, 0, 83, 0, 83, 72, 0, 84, 0, 84, 83, 0, 85, 69, 0, 85, 72, 0, 85, 87, 0, 85, 89, 0, 86, 0, 88, 0, 89, 0, 90, 0, 90, 72, 0}, new char [] {(char) 1, (char) 2, (char) 3, (char) 4, (char) 5, (char) 12, (char) 6, (char) 8, (char) 7, (char) 11, (char) 9, (char) 10, (char) 13, (char) 14, (char) 15, (char) 16, (char) 17, (char) 19, (char) 18, (char) 20, (char) 21, (char) 22, (char) 23, (char) 24, (char) 25, (char) 26, (char) 27, (char) 28, (char) 29, (char) 30, (char) 31, (char) 32, (char) 33, (char) 34, (char) 35, (char) 36, (char) 37, (char) 38, (char) 39, (char) 40, (char) 41, (char) 42, (char) 43, (char) 44, (char) 45, (char) 46, (char) 47, (char) 48, (char) 49, (char) 50, (char) 51, (char) 52, (char) 53}),
            new PhoneMapCompressed ( 0x409, 49, new byte [] {45, 0, 33, 0, 38, 0, 44, 0, 46, 0, 63, 0, 95, 0, 49, 0, 50, 0, 65, 65, 0, 65, 69, 0, 65, 72, 0, 65, 79, 0, 65, 87, 0, 65, 88, 0, 65, 89, 0, 66, 0, 67, 72, 0, 68, 0, 68, 72, 0, 69, 72, 0, 69, 82, 0, 69, 89, 0, 70, 0, 71, 0, 72, 0, 73, 72, 0, 73, 89, 0, 74, 72, 0, 75, 0, 76, 0, 77, 0, 78, 0, 78, 71, 0, 79, 87, 0, 79, 89, 0, 80, 0, 82, 0, 83, 0, 83, 72, 0, 84, 0, 84, 72, 0, 85, 72, 0, 85, 87, 0, 86, 0, 87, 0, 89, 0, 90, 0, 90, 72, 0}, new char [] {(char) 1, (char) 2, (char) 3, (char) 4, (char) 5, (char) 6, (char) 7, (char) 8, (char) 9, (char) 10, (char) 11, (char) 12, (char) 13, (char) 14, (char) 15, (char) 16, (char) 17, (char) 18, (char) 19, (char) 20, (char) 21, (char) 22, (char) 23, (char) 24, (char) 25, (char) 26, (char) 27, (char) 28, (char) 29, (char) 30, (char) 31, (char) 32, (char) 33, (char) 34, (char) 35, (char) 36, (char) 37, (char) 38, (char) 39, (char) 40, (char) 41, (char) 42, (char) 43, (char) 44, (char) 45, (char) 46, (char) 47, (char) 48, (char) 49}),
            new PhoneMapCompressed ( 0x40A, 35, new byte [] {45, 0, 33, 0, 38, 0, 44, 0, 46, 0, 63, 0, 95, 0, 49, 0, 50, 0, 65, 0, 66, 0, 67, 72, 0, 68, 0, 69, 0, 70, 0, 71, 0, 73, 0, 74, 0, 74, 74, 0, 75, 0, 76, 0, 76, 76, 0, 77, 0, 78, 0, 78, 74, 0, 79, 0, 80, 0, 82, 0, 82, 82, 0, 83, 0, 84, 0, 84, 72, 0, 85, 0, 87, 0, 88, 0}, new char [] {(char) 1, (char) 2, (char) 3, (char) 4, (char) 5, (char) 6, (char) 7, (char) 8, (char) 9, (char) 10, (char) 18, (char) 21, (char) 16, (char) 11, (char) 23, (char) 20, (char) 12, (char) 33, (char) 22, (char) 19, (char) 29, (char) 30, (char) 26, (char) 27, (char) 28, (char) 13, (char) 17, (char) 31, (char) 32, (char) 24, (char) 15, (char) 35, (char) 14, (char) 34, (char) 25}),
            new PhoneMapCompressed ( 0x40C, 42, new byte [] {45, 0, 33, 0, 38, 0, 44, 0, 46, 0, 63, 0, 95, 0, 126, 0, 49, 0, 65, 0, 65, 65, 0, 65, 88, 0, 66, 0, 68, 0, 69, 72, 0, 69, 85, 0, 69, 89, 0, 70, 0, 71, 0, 72, 89, 0, 73, 89, 0, 75, 0, 76, 0, 77, 0, 78, 0, 78, 71, 0, 78, 74, 0, 79, 69, 0, 79, 72, 0, 79, 87, 0, 80, 0, 82, 0, 83, 0, 83, 72, 0, 84, 0, 85, 87, 0, 85, 89, 0, 86, 0, 87, 0, 89, 0, 90, 0, 90, 72, 0}, new char [] {(char) 1, (char) 2, (char) 3, (char) 4, (char) 5, (char) 6, (char) 7, (char) 9, (char) 8, (char) 11, (char) 10, (char) 13, (char) 14, (char) 15, (char) 16, (char) 30, (char) 17, (char) 18, (char) 19, (char) 20, (char) 22, (char) 23, (char) 24, (char) 25, (char) 26, (char) 27, (char) 28, (char) 29, (char) 12, (char) 31, (char) 32, (char) 33, (char) 34, (char) 35, (char) 36, (char) 37, (char) 21, (char) 38, (char) 39, (char) 40, (char) 41, (char) 42}),
            new PhoneMapCompressed ( 0x411, 102, new byte [] {48, 48, 50, 49, 0, 48, 48, 50, 55, 0, 48, 48, 50, 66, 0, 48, 48, 50, 69, 0, 48, 48, 51, 70, 0, 48, 48, 53, 70, 0, 48, 48, 55, 67, 0, 51, 48, 57, 67, 0, 51, 48, 65, 49, 0, 51, 48, 65, 50, 0, 51, 48, 65, 51, 0, 51, 48, 65, 52, 0, 51, 48, 65, 53, 0, 51, 48, 65, 54, 0, 51, 48, 65, 55, 0, 51, 48, 65, 56, 0, 51, 48, 65, 57, 0, 51, 48, 65, 65, 0, 51, 48, 65, 66, 0, 51, 48, 65, 67, 0, 51, 48, 65, 68, 0, 51, 48, 65, 69, 0, 51, 48, 65, 70, 0, 51, 48, 66, 48, 0, 51, 48, 66, 49, 0, 51, 48, 66, 50, 0, 51, 48, 66, 51, 0, 51, 48, 66, 52, 0, 51, 48, 66, 53, 0, 51, 48, 66, 54, 0, 51, 48, 66, 55, 0, 51, 48, 66, 56, 0, 51, 48, 66, 57, 0, 51, 48, 66, 65, 0, 51, 48, 66, 66, 0, 51, 48, 66, 67, 0, 51, 48, 66, 68, 0, 51, 48, 66, 69, 0, 51, 48, 66, 70, 0, 51, 48, 67, 48, 0, 51, 48, 67, 49, 0, 51, 48, 67, 50, 0, 51, 48, 67, 51, 0, 51, 48, 67, 52, 0, 51, 48, 67, 53, 0, 51, 48, 67, 54, 0, 51, 48, 67, 55, 0, 51, 48, 67, 56, 0, 51, 48, 67, 57, 0, 51, 48, 67, 65, 0, 51, 48, 67, 66, 0, 51, 48, 67, 67, 0, 51, 48, 67, 68, 0, 51, 48, 67, 69, 0, 51, 48, 67, 70, 0, 51, 48, 68, 48, 0, 51, 48, 68, 49, 0, 51, 48, 68, 50, 0, 51, 48, 68, 51, 0, 51, 48, 68, 52, 0, 51, 48, 68, 53, 0, 51, 48, 68, 54, 0, 51, 48, 68, 55, 0, 51, 48, 68, 56, 0, 51, 48, 68, 57, 0, 51, 48, 68, 65, 0, 51, 48, 68, 66, 0, 51, 48, 68, 67, 0, 51, 48, 68, 68, 0, 51, 48, 68, 69, 0, 51, 48, 68, 70, 0, 51, 48, 69, 48, 0, 51, 48, 69, 49, 0, 51, 48, 69, 50, 0, 51, 48, 69, 51, 0, 51, 48, 69, 52, 0, 51, 48, 69, 53, 0, 51, 48, 69, 54, 0, 51, 48, 69, 55, 0, 51, 48, 69, 56, 0, 51, 48, 69, 57, 0, 51, 48, 69, 65, 0, 51, 48, 69, 66, 0, 51, 48, 69, 67, 0, 51, 48, 69, 68, 0, 51, 48, 69, 69, 0, 51, 48, 69, 70, 0, 51, 48, 70, 48, 0, 51, 48, 70, 49, 0, 51, 48, 70, 50, 0, 51, 48, 70, 51, 0, 51, 48, 70, 52, 0, 51, 48, 70, 53, 0, 51, 48, 70, 54, 0, 51, 48, 70, 55, 0, 51, 48, 70, 56, 0, 51, 48, 70, 57, 0, 51, 48, 70, 65, 0, 51, 48, 70, 66, 0, 51, 48, 70, 67, 0, 51, 48, 70, 68, 0, 51, 48, 70, 69, 0}, new char [] {(char) 33, (char) 39, (char) 43, (char) 46, (char) 63, (char) 95, (char) 124, (char) 12444, (char) 12449, (char) 12450, (char) 12451, (char) 12452, (char) 12453, (char) 12454, (char) 12455, (char) 12456, (char) 12457, (char) 12458, (char) 12459, (char) 12460, (char) 12461, (char) 12462, (char) 12463, (char) 12464, (char) 12465, (char) 12466, (char) 12467, (char) 12468, (char) 12469, (char) 12470, (char) 12471, (char) 12472, (char) 12473, (char) 12474, (char) 12475, (char) 12476, (char) 12477, (char) 12478, (char) 12479, (char) 12480, (char) 12481, (char) 12482, (char) 12483, (char) 12484, (char) 12485, (char) 12486, (char) 12487, (char) 12488, (char) 12489, (char) 12490, (char) 12491, (char) 12492, (char) 12493, (char) 12494, (char) 12495, (char) 12496, (char) 12497, (char) 12498, (char) 12499, (char) 12500, (char) 12501, (char) 12502, (char) 12503, (char) 12504, (char) 12505, (char) 12506, (char) 12507, (char) 12508, (char) 12509, (char) 12510, (char) 12511, (char) 12512, (char) 12513, (char) 12514, (char) 12515, (char) 12516, (char) 12517, (char) 12518, (char) 12519, (char) 12520, (char) 12521, (char) 12522, (char) 12523, (char) 12524, (char) 12525, (char) 12526, (char) 12527, (char) 12528, (char) 12529, (char) 12530, (char) 12531, (char) 12532, (char) 12533, (char) 12534, (char) 12535, (char) 12536, (char) 12537, (char) 12538, (char) 12539, (char) 12540, (char) 12541, (char) 12542}),
            new PhoneMapCompressed ( 0x804, 422, new byte [] {45, 0, 33, 0, 38, 0, 42, 0, 44, 0, 46, 0, 63, 0, 95, 0, 43, 0, 49, 0, 50, 0, 51, 0, 52, 0, 53, 0, 65, 0, 65, 73, 0, 65, 78, 0, 65, 78, 71, 0, 65, 79, 0, 66, 65, 0, 66, 65, 73, 0, 66, 65, 78, 0, 66, 65, 78, 71, 0, 66, 65, 79, 0, 66, 69, 73, 0, 66, 69, 78, 0, 66, 69, 78, 71, 0, 66, 73, 0, 66, 73, 65, 78, 0, 66, 73, 65, 79, 0, 66, 73, 69, 0, 66, 73, 78, 0, 66, 73, 78, 71, 0, 66, 79, 0, 66, 85, 0, 67, 65, 0, 67, 65, 73, 0, 67, 65, 78, 0, 67, 65, 78, 71, 0, 67, 65, 79, 0, 67, 69, 0, 67, 69, 78, 0, 67, 69, 78, 71, 0, 67, 72, 65, 0, 67, 72, 65, 73, 0, 67, 72, 65, 78, 0, 67, 72, 65, 78, 71, 0, 67, 72, 65, 79, 0, 67, 72, 69, 0, 67, 72, 69, 78, 0, 67, 72, 69, 78, 71, 0, 67, 72, 73, 0, 67, 72, 79, 78, 71, 0, 67, 72, 79, 85, 0, 67, 72, 85, 0, 67, 72, 85, 65, 73, 0, 67, 72, 85, 65, 78, 0, 67, 72, 85, 65, 78, 71, 0, 67, 72, 85, 73, 0, 67, 72, 85, 78, 0, 67, 72, 85, 79, 0, 67, 73, 0, 67, 79, 78, 71, 0, 67, 79, 85, 0, 67, 85, 0, 67, 85, 65, 78, 0, 67, 85, 73, 0, 67, 85, 78, 0, 67, 85, 79, 0, 68, 65, 0, 68, 65, 73, 0, 68, 65, 78, 0, 68, 65, 78, 71, 0, 68, 65, 79, 0, 68, 69, 0, 68, 69, 73, 0, 68, 69, 78, 0, 68, 69, 78, 71, 0, 68, 73, 0, 68, 73, 65, 0, 68, 73, 65, 78, 0, 68, 73, 65, 79, 0, 68, 73, 69, 0, 68, 73, 78, 71, 0, 68, 73, 85, 0, 68, 79, 78, 71, 0, 68, 79, 85, 0, 68, 85, 0, 68, 85, 65, 78, 0, 68, 85, 73, 0, 68, 85, 78, 0, 68, 85, 79, 0, 69, 0, 69, 73, 0, 69, 78, 0, 69, 82, 0, 70, 65, 0, 70, 65, 78, 0, 70, 65, 78, 71, 0, 70, 69, 73, 0, 70, 69, 78, 0, 70, 69, 78, 71, 0, 70, 79, 0, 70, 79, 85, 0, 70, 85, 0, 71, 65, 0, 71, 65, 73, 0, 71, 65, 78, 0, 71, 65, 78, 71, 0, 71, 65, 79, 0, 71, 69, 0, 71, 69, 73, 0, 71, 69, 78, 0, 71, 69, 78, 71, 0, 71, 79, 78, 71, 0, 71, 79, 85, 0, 71, 85, 0, 71, 85, 65, 0, 71, 85, 65, 73, 0, 71, 85, 65, 78, 0, 71, 85, 65, 78, 71, 0, 71, 85, 73, 0, 71, 85, 78, 0, 71, 85, 79, 0, 72, 65, 0, 72, 65, 73, 0, 72, 65, 78, 0, 72, 65, 78, 71, 0, 72, 65, 79, 0, 72, 69, 0, 72, 69, 73, 0, 72, 69, 78, 0, 72, 69, 78, 71, 0, 72, 79, 78, 71, 0, 72, 79, 85, 0, 72, 85, 0, 72, 85, 65, 0, 72, 85, 65, 73, 0, 72, 85, 65, 78, 0, 72, 85, 65, 78, 71, 0, 72, 85, 73, 0, 72, 85, 78, 0, 72, 85, 79, 0, 74, 73, 0, 74, 73, 65, 0, 74, 73, 65, 78, 0, 74, 73, 65, 78, 71, 0, 74, 73, 65, 79, 0, 74, 73, 69, 0, 74, 73, 78, 0, 74, 73, 78, 71, 0, 74, 73, 79, 78, 71, 0, 74, 73, 85, 0, 74, 85, 0, 74, 85, 65, 78, 0, 74, 85, 69, 0, 74, 85, 78, 0, 75, 65, 0, 75, 65, 73, 0, 75, 65, 78, 0, 75, 65, 78, 71, 0, 75, 65, 79, 0, 75, 69, 0, 75, 69, 73, 0, 75, 69, 78, 0, 75, 69, 78, 71, 0, 75, 79, 78, 71, 0, 75, 79, 85, 0, 75, 85, 0, 75, 85, 65, 0, 75, 85, 65, 73, 0, 75, 85, 65, 78, 0, 75, 85, 65, 78, 71, 0, 75, 85, 73, 0, 75, 85, 78, 0, 75, 85, 79, 0, 76, 65, 0, 76, 65, 73, 0, 76, 65, 78, 0, 76, 65, 78, 71, 0, 76, 65, 79, 0, 76, 69, 0, 76, 69, 73, 0, 76, 69, 78, 71, 0, 76, 73, 0, 76, 73, 65, 0, 76, 73, 65, 78, 0, 76, 73, 65, 78, 71, 0, 76, 73, 65, 79, 0, 76, 73, 69, 0, 76, 73, 78, 0, 76, 73, 78, 71, 0, 76, 73, 85, 0, 76, 79, 0, 76, 79, 78, 71, 0, 76, 79, 85, 0, 76, 85, 0, 76, 85, 65, 78, 0, 76, 85, 69, 0, 76, 85, 78, 0, 76, 85, 79, 0, 76, 86, 0, 77, 65, 0, 77, 65, 73, 0, 77, 65, 78, 0, 77, 65, 78, 71, 0, 77, 65, 79, 0, 77, 69, 0, 77, 69, 73, 0, 77, 69, 78, 0, 77, 69, 78, 71, 0, 77, 73, 0, 77, 73, 65, 78, 0, 77, 73, 65, 79, 0, 77, 73, 69, 0, 77, 73, 78, 0, 77, 73, 78, 71, 0, 77, 73, 85, 0, 77, 79, 0, 77, 79, 85, 0, 77, 85, 0, 78, 65, 0, 78, 65, 73, 0, 78, 65, 78, 0, 78, 65, 78, 71, 0, 78, 65, 79, 0, 78, 69, 0, 78, 69, 73, 0, 78, 69, 78, 0, 78, 69, 78, 71, 0, 78, 73, 0, 78, 73, 65, 78, 0, 78, 73, 65, 78, 71, 0, 78, 73, 65, 79, 0, 78, 73, 69, 0, 78, 73, 78, 0, 78, 73, 78, 71, 0, 78, 73, 85, 0, 78, 79, 78, 71, 0, 78, 79, 85, 0, 78, 85, 0, 78, 85, 65, 78, 0, 78, 85, 69, 0, 78, 85, 79, 0, 78, 86, 0, 79, 0, 79, 85, 0, 80, 65, 0, 80, 65, 73, 0, 80, 65, 78, 0, 80, 65, 78, 71, 0, 80, 65, 79, 0, 80, 69, 73, 0, 80, 69, 78, 0, 80, 69, 78, 71, 0, 80, 73, 0, 80, 73, 65, 78, 0, 80, 73, 65, 79, 0, 80, 73, 69, 0, 80, 73, 78, 0, 80, 73, 78, 71, 0, 80, 79, 0, 80, 79, 85, 0, 80, 85, 0, 81, 73, 0, 81, 73, 65, 0, 81, 73, 65, 78, 0, 81, 73, 65, 78, 71, 0, 81, 73, 65, 79, 0, 81, 73, 69, 0, 81, 73, 78, 0, 81, 73, 78, 71, 0, 81, 73, 79, 78, 71, 0, 81, 73, 85, 0, 81, 85, 0, 81, 85, 65, 78, 0, 81, 85, 69, 0, 81, 85, 78, 0, 82, 65, 78, 0, 82, 65, 78, 71, 0, 82, 65, 79, 0, 82, 69, 0, 82, 69, 78, 0, 82, 69, 78, 71, 0, 82, 73, 0, 82, 79, 78, 71, 0, 82, 79, 85, 0, 82, 85, 0, 82, 85, 65, 78, 0, 82, 85, 73, 0, 82, 85, 78, 0, 82, 85, 79, 0, 83, 65, 0, 83, 65, 73, 0, 83, 65, 78, 0, 83, 65, 78, 71, 0, 83, 65, 79, 0, 83, 69, 0, 83, 69, 78, 0, 83, 69, 78, 71, 0, 83, 72, 65, 0, 83, 72, 65, 73, 0, 83, 72, 65, 78, 0, 83, 72, 65, 78, 71, 0, 83, 72, 65, 79, 0, 83, 72, 69, 0, 83, 72, 69, 73, 0, 83, 72, 69, 78, 0, 83, 72, 69, 78, 71, 0, 83, 72, 73, 0, 83, 72, 79, 85, 0, 83, 72, 85, 0, 83, 72, 85, 65, 0, 83, 72, 85, 65, 73, 0, 83, 72, 85, 65, 78, 0, 83, 72, 85, 65, 78, 71, 0, 83, 72, 85, 73, 0, 83, 72, 85, 78, 0, 83, 72, 85, 79, 0, 83, 73, 0, 83, 79, 78, 71, 0, 83, 79, 85, 0, 83, 85, 0, 83, 85, 65, 78, 0, 83, 85, 73, 0, 83, 85, 78, 0, 83, 85, 79, 0, 84, 65, 0, 84, 65, 73, 0, 84, 65, 78, 0, 84, 65, 78, 71, 0, 84, 65, 79, 0, 84, 69, 0, 84, 69, 73, 0, 84, 69, 78, 71, 0, 84, 73, 0, 84, 73, 65, 78, 0, 84, 73, 65, 79, 0, 84, 73, 69, 0, 84, 73, 78, 71, 0, 84, 79, 78, 71, 0, 84, 79, 85, 0, 84, 85, 0, 84, 85, 65, 78, 0, 84, 85, 73, 0, 84, 85, 78, 0, 84, 85, 79, 0, 87, 65, 0, 87, 65, 73, 0, 87, 65, 78, 0, 87, 65, 78, 71, 0, 87, 69, 73, 0, 87, 69, 78, 0, 87, 69, 78, 71, 0, 87, 79, 0, 87, 85, 0, 88, 73, 0, 88, 73, 65, 0, 88, 73, 65, 78, 0, 88, 73, 65, 78, 71, 0, 88, 73, 65, 79, 0, 88, 73, 69, 0, 88, 73, 78, 0, 88, 73, 78, 71, 0, 88, 73, 79, 78, 71, 0, 88, 73, 85, 0, 88, 85, 0, 88, 85, 65, 78, 0, 88, 85, 69, 0, 88, 85, 78, 0, 89, 65, 0, 89, 65, 78, 0, 89, 65, 78, 71, 0, 89, 65, 79, 0, 89, 69, 0, 89, 73, 0, 89, 73, 78, 0, 89, 73, 78, 71, 0, 89, 79, 0, 89, 79, 78, 71, 0, 89, 79, 85, 0, 89, 85, 0, 89, 85, 65, 78, 0, 89, 85, 69, 0, 89, 85, 78, 0, 90, 65, 0, 90, 65, 73, 0, 90, 65, 78, 0, 90, 65, 78, 71, 0, 90, 65, 79, 0, 90, 69, 0, 90, 69, 73, 0, 90, 69, 78, 0, 90, 69, 78, 71, 0, 90, 72, 65, 0, 90, 72, 65, 73, 0, 90, 72, 65, 78, 0, 90, 72, 65, 78, 71, 0, 90, 72, 65, 79, 0, 90, 72, 69, 0, 90, 72, 69, 73, 0, 90, 72, 69, 78, 0, 90, 72, 69, 78, 71, 0, 90, 72, 73, 0, 90, 72, 79, 78, 71, 0, 90, 72, 79, 85, 0, 90, 72, 85, 0, 90, 72, 85, 65, 0, 90, 72, 85, 65, 73, 0, 90, 72, 85, 65, 78, 0, 90, 72, 85, 65, 78, 71, 0, 90, 72, 85, 73, 0, 90, 72, 85, 78, 0, 90, 72, 85, 79, 0, 90, 73, 0, 90, 79, 78, 71, 0, 90, 79, 85, 0, 90, 85, 0, 90, 85, 65, 78, 0, 90, 85, 73, 0, 90, 85, 78, 0, 90, 85, 79, 0}, new char [] {(char) 1, (char) 2, (char) 3, (char) 9, (char) 4, (char) 5, (char) 6, (char) 7, (char) 8, (char) 10, (char) 11, (char) 12, (char) 13, (char) 14, (char) 15, (char) 16, (char) 17, (char) 18, (char) 19, (char) 20, (char) 21, (char) 22, (char) 23, (char) 24, (char) 25, (char) 26, (char) 27, (char) 28, (char) 29, (char) 30, (char) 31, (char) 32, (char) 33, (char) 34, (char) 35, (char) 36, (char) 37, (char) 38, (char) 39, (char) 40, (char) 41, (char) 42, (char) 43, (char) 44, (char) 45, (char) 46, (char) 47, (char) 48, (char) 49, (char) 50, (char) 51, (char) 52, (char) 53, (char) 54, (char) 55, (char) 56, (char) 57, (char) 58, (char) 59, (char) 60, (char) 61, (char) 62, (char) 63, (char) 64, (char) 65, (char) 66, (char) 67, (char) 68, (char) 69, (char) 70, (char) 71, (char) 72, (char) 73, (char) 74, (char) 75, (char) 76, (char) 77, (char) 78, (char) 79, (char) 80, (char) 81, (char) 82, (char) 83, (char) 84, (char) 85, (char) 86, (char) 87, (char) 88, (char) 89, (char) 90, (char) 91, (char) 92, (char) 93, (char) 94, (char) 95, (char) 96, (char) 97, (char) 98, (char) 99, (char) 100, (char) 101, (char) 102, (char) 103, (char) 104, (char) 105, (char) 106, (char) 107, (char) 108, (char) 109, (char) 110, (char) 111, (char) 112, (char) 113, (char) 114, (char) 115, (char) 116, (char) 117, (char) 118, (char) 119, (char) 120, (char) 121, (char) 122, (char) 123, (char) 124, (char) 125, (char) 126, (char) 127, (char) 128, (char) 129, (char) 130, (char) 131, (char) 132, (char) 133, (char) 134, (char) 135, (char) 136, (char) 137, (char) 138, (char) 139, (char) 140, (char) 141, (char) 142, (char) 143, (char) 144, (char) 145, (char) 146, (char) 147, (char) 148, (char) 149, (char) 150, (char) 151, (char) 152, (char) 153, (char) 154, (char) 155, (char) 156, (char) 157, (char) 158, (char) 159, (char) 160, (char) 161, (char) 162, (char) 163, (char) 164, (char) 165, (char) 166, (char) 167, (char) 168, (char) 169, (char) 170, (char) 171, (char) 172, (char) 173, (char) 174, (char) 175, (char) 176, (char) 177, (char) 178, (char) 179, (char) 180, (char) 181, (char) 182, (char) 183, (char) 184, (char) 185, (char) 186, (char) 187, (char) 188, (char) 189, (char) 190, (char) 191, (char) 192, (char) 193, (char) 194, (char) 195, (char) 196, (char) 197, (char) 198, (char) 199, (char) 200, (char) 201, (char) 202, (char) 203, (char) 204, (char) 205, (char) 206, (char) 207, (char) 208, (char) 209, (char) 210, (char) 211, (char) 212, (char) 213, (char) 214, (char) 215, (char) 216, (char) 217, (char) 218, (char) 219, (char) 220, (char) 221, (char) 222, (char) 223, (char) 224, (char) 225, (char) 226, (char) 227, (char) 228, (char) 229, (char) 230, (char) 231, (char) 232, (char) 233, (char) 234, (char) 235, (char) 236, (char) 237, (char) 238, (char) 239, (char) 240, (char) 241, (char) 242, (char) 243, (char) 244, (char) 245, (char) 246, (char) 247, (char) 248, (char) 249, (char) 250, (char) 251, (char) 252, (char) 253, (char) 254, (char) 255, (char) 256, (char) 257, (char) 258, (char) 259, (char) 260, (char) 261, (char) 262, (char) 263, (char) 264, (char) 265, (char) 266, (char) 267, (char) 268, (char) 269, (char) 270, (char) 271, (char) 272, (char) 273, (char) 274, (char) 275, (char) 276, (char) 277, (char) 278, (char) 279, (char) 280, (char) 281, (char) 282, (char) 283, (char) 284, (char) 285, (char) 286, (char) 287, (char) 288, (char) 289, (char) 290, (char) 291, (char) 292, (char) 293, (char) 294, (char) 295, (char) 296, (char) 297, (char) 298, (char) 299, (char) 300, (char) 301, (char) 302, (char) 303, (char) 304, (char) 305, (char) 306, (char) 307, (char) 308, (char) 309, (char) 310, (char) 311, (char) 312, (char) 313, (char) 314, (char) 315, (char) 316, (char) 317, (char) 318, (char) 319, (char) 320, (char) 321, (char) 322, (char) 323, (char) 324, (char) 325, (char) 326, (char) 327, (char) 328, (char) 329, (char) 330, (char) 331, (char) 332, (char) 333, (char) 334, (char) 335, (char) 336, (char) 337, (char) 338, (char) 339, (char) 340, (char) 341, (char) 342, (char) 343, (char) 344, (char) 345, (char) 346, (char) 347, (char) 348, (char) 349, (char) 350, (char) 351, (char) 352, (char) 353, (char) 354, (char) 355, (char) 356, (char) 357, (char) 358, (char) 359, (char) 360, (char) 361, (char) 362, (char) 363, (char) 364, (char) 365, (char) 366, (char) 367, (char) 368, (char) 369, (char) 370, (char) 371, (char) 372, (char) 373, (char) 374, (char) 375, (char) 376, (char) 377, (char) 378, (char) 379, (char) 380, (char) 381, (char) 382, (char) 383, (char) 384, (char) 385, (char) 386, (char) 387, (char) 388, (char) 389, (char) 390, (char) 391, (char) 392, (char) 393, (char) 394, (char) 395, (char) 396, (char) 397, (char) 398, (char) 399, (char) 400, (char) 401, (char) 402, (char) 403, (char) 404, (char) 405, (char) 406, (char) 407, (char) 408, (char) 409, (char) 410, (char) 411, (char) 412, (char) 413, (char) 414, (char) 415, (char) 416, (char) 417, (char) 418, (char) 419, (char) 420, (char) 421, (char) 422}),
         };

        private static readonly PhoneMap[] s_phoneMaps = DecompressPhoneMaps(s_phoneMapsCompressed);

        private static char[] s_updIds = new char[] { (char)1, (char)2, (char)3, (char)4, (char)33, (char)46, (char)97, (char)98, (char)99, (char)100, (char)101, (char)102, (char)103, (char)104, (char)105, (char)106, (char)107, (char)108, (char)109, (char)110, (char)111, (char)112, (char)113, (char)114, (char)115, (char)116, (char)117, (char)118, (char)119, (char)120, (char)121, (char)122, (char)124, (char)230, (char)231, (char)240, (char)248, (char)295, (char)331, (char)339, (char)448, (char)449, (char)450, (char)451, (char)592, (char)593, (char)594, (char)595, (char)596, (char)597, (char)598, (char)599, (char)600, (char)601, (char)602, (char)603, (char)604, (char)605, (char)606, (char)607, (char)608, (char)609, (char)610, (char)611, (char)612, (char)613, (char)614, (char)615, (char)616, (char)618, (char)619, (char)620, (char)621, (char)622, (char)623, (char)624, (char)625, (char)626, (char)627, (char)628, (char)629, (char)630, (char)632, (char)633, (char)634, (char)635, (char)637, (char)638, (char)640, (char)641, (char)642, (char)643, (char)644, (char)646, (char)647, (char)648, (char)649, (char)650, (char)651, (char)652, (char)653, (char)654, (char)655, (char)656, (char)657, (char)658, (char)659, (char)660, (char)661, (char)662, (char)663, (char)664, (char)665, (char)667, (char)668, (char)669, (char)671, (char)672, (char)673, (char)674, (char)675, (char)676, (char)677, (char)678, (char)679, (char)680, (char)688, (char)689, (char)690, (char)692, (char)695, (char)700, (char)712, (char)716, (char)720, (char)721, (char)728, (char)734, (char)736, (char)737, (char)740, (char)768, (char)769, (char)771, (char)772, (char)774, (char)776, (char)778, (char)779, (char)783, (char)792, (char)793, (char)794, (char)796, (char)797, (char)798, (char)799, (char)800, (char)804, (char)805, (char)809, (char)810, (char)812, (char)815, (char)816, (char)817, (char)820, (char)825, (char)826, (char)827, (char)828, (char)829, (char)865, (char)946, (char)952, (char)967, (char)1856, (char)8214, (char)8255, (char)8319, (char)8593, (char)8594, (char)8595, (char)8599, (char)8600 };

        private static readonly PhonemeConverter s_upsConverter = new(s_phoneMaps[0]);

        #endregion

        #region Private Types

        private sealed class PhoneMap
        {
            internal PhoneMap() { }

            internal int _lcid;
            internal PhoneId[] _phoneIds;
        }

        private sealed class PhoneId : IComparer<PhoneId>
        {
            internal PhoneId() { }

            internal string _phone;
            internal char[] _cp;

            int IComparer<PhoneId>.Compare(PhoneId x, PhoneId y)
            {
                return string.Compare(x._phone, y._phone, StringComparison.CurrentCulture);
            }
        }

        /// <summary>
        /// Compressed version for the phone map so that the size for the pronunciation table is small in the dll.
        /// A single large arrays of bytes (ASCII) is used to store the 'pron' string. Each string is zero terminated.
        /// A single large array of char is used to store the code point for the 'pron' string. Each binary array for the pron by default
        /// has a length of 1 character. If the length is greater than 1, then the 'pron' string is appended with -1 values, one per extra code
        /// point.
        /// </summary>
        private sealed class PhoneMapCompressed
        {
            internal PhoneMapCompressed() { }

            internal PhoneMapCompressed(int lcid, int count, byte[] phoneIds, char[] cps)
            {
                _lcid = lcid;
                _count = count;
                _phones = phoneIds;
                _cps = cps;
            }

            // Language Id
            internal int _lcid;

            // Number of phonemes
            internal int _count;

            // Array of zero terminated ASCII strings
            internal byte[] _phones;

            // Array of code points for the 'pron'. By default each code point for a 'pron' is 1 char long, unless the 'pron' string is prepended with -1
            internal char[] _cps;
        }

        #endregion
    }
}
