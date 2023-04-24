// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Unicode;

namespace System.Globalization
{
    /// <summary>
    /// This Class defines behaviors specific to a writing system.
    /// A writing system is the collection of scripts and orthographic rules
    /// required to represent a language as text.
    /// </summary>
    public sealed partial class TextInfo : ICloneable, IDeserializationCallback
    {
        private enum Tristate : byte
        {
            NotInitialized = 0,
            False = 1,
            True = 2
        }

        private string? _listSeparator;
        private bool _isReadOnly;

        private readonly string _cultureName;
        private readonly CultureData _cultureData;

        private bool HasEmptyCultureName { get { return _cultureName.Length == 0; } }

        // // Name of the text info we're using (ie: _cultureData.TextInfoName)
        private readonly string _textInfoName;

        private Tristate _isAsciiCasingSameAsInvariant = Tristate.NotInitialized;

        // Invariant text info
        internal static readonly TextInfo Invariant = new TextInfo(CultureData.Invariant, readOnly: true) { _isAsciiCasingSameAsInvariant = Tristate.True };

        internal TextInfo(CultureData cultureData)
        {
            // This is our primary data source, we don't need most of the rest of this
            _cultureData = cultureData;
            _cultureName = _cultureData.CultureName;
            _textInfoName = _cultureData.TextInfoName;

            if (GlobalizationMode.UseNls)
            {
                _sortHandle = CompareInfo.NlsGetSortHandle(_textInfoName);
            }
        }

        private TextInfo(CultureData cultureData, bool readOnly)
            : this(cultureData)
        {
            SetReadOnlyState(readOnly);
        }

        void IDeserializationCallback.OnDeserialization(object? sender)
        {
            throw new PlatformNotSupportedException();
        }

        public int ANSICodePage => _cultureData.ANSICodePage;

        public int OEMCodePage => _cultureData.OEMCodePage;

        public int MacCodePage => _cultureData.MacCodePage;

        public int EBCDICCodePage => _cultureData.EBCDICCodePage;

        // Just use the LCID from our text info name
        public int LCID => CultureInfo.GetCultureInfo(_textInfoName).LCID;

        public string CultureName => _textInfoName;

        public bool IsReadOnly => _isReadOnly;

        public object Clone()
        {
            object o = MemberwiseClone();
            ((TextInfo)o).SetReadOnlyState(false);
            return o;
        }

        /// <summary>
        /// Create a cloned readonly instance or return the input one if it is
        /// readonly.
        /// </summary>
        public static TextInfo ReadOnly(TextInfo textInfo)
        {
            ArgumentNullException.ThrowIfNull(textInfo);

            if (textInfo.IsReadOnly)
            {
                return textInfo;
            }

            TextInfo clonedTextInfo = (TextInfo)(textInfo.MemberwiseClone());
            clonedTextInfo.SetReadOnlyState(true);
            return clonedTextInfo;
        }

        private void VerifyWritable()
        {
            if (_isReadOnly)
            {
                throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
            }
        }

        internal void SetReadOnlyState(bool readOnly)
        {
            _isReadOnly = readOnly;
        }

        /// <summary>
        /// Returns the string used to separate items in a list.
        /// </summary>
        public string ListSeparator
        {
            get => _listSeparator ??= _cultureData.ListSeparator;
            set
            {
                ArgumentNullException.ThrowIfNull(value);

                VerifyWritable();
                _listSeparator = value;
            }
        }

        /// <summary>
        /// Converts the character or string to lower case.  Certain locales
        /// have different casing semantics from the file systems in Win32.
        /// </summary>
        public char ToLower(char c)
        {
            if (GlobalizationMode.Invariant)
            {
                return InvariantModeCasing.ToLower(c);
            }

            if (UnicodeUtility.IsAsciiCodePoint(c) && IsAsciiCasingSameAsInvariant)
            {
                return ToLowerAsciiInvariant(c);
            }

            return ChangeCase(c, toUpper: false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static char ToLowerInvariant(char c)
        {
            if (UnicodeUtility.IsAsciiCodePoint(c))
            {
                return ToLowerAsciiInvariant(c);
            }

            if (GlobalizationMode.Invariant)
            {
                return InvariantModeCasing.ToLower(c);
            }

            return Invariant.ChangeCase(c, toUpper: false);
        }

        public string ToLower(string str)
        {
            ArgumentNullException.ThrowIfNull(str);

            if (GlobalizationMode.Invariant)
            {
                return InvariantModeCasing.ToLower(str);
            }

            return ChangeCaseCommon<ToLowerConversion>(str);
        }

        private unsafe char ChangeCase(char c, bool toUpper)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            char dst = default;
            ChangeCaseCore(&c, 1, &dst, 1, toUpper);
            return dst;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ChangeCaseToLower(ReadOnlySpan<char> source, Span<char> destination)
        {
            Debug.Assert(destination.Length >= source.Length);
            ChangeCaseCommon<ToLowerConversion>(source, destination);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ChangeCaseToUpper(ReadOnlySpan<char> source, Span<char> destination)
        {
            Debug.Assert(destination.Length >= source.Length);
            ChangeCaseCommon<ToUpperConversion>(source, destination);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void ChangeCaseCommon<TConversion>(ReadOnlySpan<char> source, Span<char> destination) where TConversion : struct
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(typeof(TConversion) == typeof(ToUpperConversion) || typeof(TConversion) == typeof(ToLowerConversion));

            if (source.IsEmpty)
            {
                return;
            }

            bool toUpper = typeof(TConversion) == typeof(ToUpperConversion); // JIT will treat this as a constant in release builds
            int charsConsumed = 0;

            if (IsAsciiCasingSameAsInvariant)
            {
                OperationStatus operationStatus = toUpper
                    ? Ascii.ToUpper(source, destination, out charsConsumed)
                    : Ascii.ToLower(source, destination, out charsConsumed);

                if (operationStatus != OperationStatus.InvalidData)
                {
                    Debug.Assert(operationStatus == OperationStatus.Done);
                    return;
                }
            }

            fixed (char* pSource = &MemoryMarshal.GetReference(source))
            fixed (char* pDestination = &MemoryMarshal.GetReference(destination))
            {
                ChangeCaseCore(pSource + charsConsumed, source.Length - charsConsumed, pDestination + charsConsumed, destination.Length - charsConsumed, toUpper);
            }
        }

        private unsafe string ChangeCaseCommon<TConversion>(string source) where TConversion : struct
        {
            Debug.Assert(typeof(TConversion) == typeof(ToUpperConversion) || typeof(TConversion) == typeof(ToLowerConversion));
            bool toUpper = typeof(TConversion) == typeof(ToUpperConversion); // JIT will treat this as a constant in release builds

            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(source != null);

            // If the string is empty, we're done.
            if (source.Length == 0)
            {
                return string.Empty;
            }

            fixed (char* pSource = source)
            {
                nuint currIdx = 0; // in chars

                // If this culture's casing for ASCII is the same as invariant, try to take
                // a fast path that'll work in managed code and ASCII rather than calling out
                // to the OS for culture-aware casing.
                if (IsAsciiCasingSameAsInvariant)
                {
                    // Read 2 chars (one 32-bit integer) at a time

                    if (source.Length >= 2)
                    {
                        nuint lastIndexWhereCanReadTwoChars = (uint)source.Length - 2;
                        do
                        {
                            // See the comments in ChangeCaseCommon<TConversion>(ROS<char>, Span<char>) for a full explanation of the below code.

                            uint tempValue = Unsafe.ReadUnaligned<uint>(pSource + currIdx);
                            if (!Utf16Utility.AllCharsInUInt32AreAscii(tempValue))
                            {
                                goto NotAscii;
                            }
                            if ((toUpper) ? Utf16Utility.UInt32ContainsAnyLowercaseAsciiChar(tempValue) : Utf16Utility.UInt32ContainsAnyUppercaseAsciiChar(tempValue))
                            {
                                goto AsciiMustChangeCase;
                            }

                            currIdx += 2;
                        } while (currIdx <= lastIndexWhereCanReadTwoChars);
                    }

                    // If there's a single character left to convert, do it now.
                    if ((source.Length & 1) != 0)
                    {
                        uint tempValue = pSource[currIdx];
                        if (tempValue > 0x7Fu)
                        {
                            goto NotAscii;
                        }
                        if ((toUpper) ? ((tempValue - 'a') <= (uint)('z' - 'a')) : ((tempValue - 'A') <= (uint)('Z' - 'A')))
                        {
                            goto AsciiMustChangeCase;
                        }
                    }

                    // We got through all characters without finding anything that needed to change - done!
                    return source;

                AsciiMustChangeCase:
                    {
                        // We reached ASCII data that requires a case change.
                        // This will necessarily allocate a new string, but let's try to stay within the managed (non-localization tables)
                        // conversion code path if we can.

                        string result = string.FastAllocateString(source.Length); // changing case uses simple folding: doesn't change UTF-16 code unit count

                        // copy existing known-good data into the result
                        Span<char> resultSpan = new Span<char>(ref result.GetRawStringData(), result.Length);
                        source.AsSpan(0, (int)currIdx).CopyTo(resultSpan);

                        // and re-run the fast span-based logic over the remainder of the data
                        ChangeCaseCommon<TConversion>(source.AsSpan((int)currIdx), resultSpan.Slice((int)currIdx));
                        return result;
                    }
                }

            NotAscii:
                {
                    // We reached non-ASCII data *or* the requested culture doesn't map ASCII data the same way as the invariant culture.
                    // In either case we need to fall back to the localization tables.

                    string result = string.FastAllocateString(source.Length); // changing case uses simple folding: doesn't change UTF-16 code unit count

                    if (currIdx > 0)
                    {
                        // copy existing known-good data into the result
                        Span<char> resultSpan = new Span<char>(ref result.GetRawStringData(), result.Length);
                        source.AsSpan(0, (int)currIdx).CopyTo(resultSpan);
                    }

                    // and run the culture-aware logic over the remainder of the data
                    fixed (char* pResult = result)
                    {
                        ChangeCaseCore(pSource + currIdx, source.Length - (int)currIdx, pResult + currIdx, result.Length - (int)currIdx, toUpper);
                    }
                    return result;
                }
            }
        }

        internal static unsafe string ToLowerAsciiInvariant(string s)
        {
            if (s.Length == 0)
            {
                return string.Empty;
            }

            int i = s.AsSpan().IndexOfAnyInRange('A', 'Z');
            if (i < 0)
            {
                return s;
            }

            fixed (char* pSource = s)
            {
                string result = string.FastAllocateString(s.Length);
                fixed (char* pResult = result)
                {
                    s.AsSpan(0, i).CopyTo(new Span<char>(pResult, result.Length));

                    pResult[i] = (char)(pSource[i] | 0x20);
                    i++;

                    while (i < s.Length)
                    {
                        pResult[i] = ToLowerAsciiInvariant(pSource[i]);
                        i++;
                    }
                }

                return result;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static char ToLowerAsciiInvariant(char c)
        {
            if (char.IsAsciiLetterUpper(c))
            {
                // on x86, extending BYTE -> DWORD is more efficient than WORD -> DWORD
                c = (char)(byte)(c | 0x20);
            }
            return c;
        }

        /// <summary>
        /// Converts the character or string to upper case.  Certain locales
        /// have different casing semantics from the file systems in Win32.
        /// </summary>
        public char ToUpper(char c)
        {
            if (GlobalizationMode.Invariant)
            {
                return InvariantModeCasing.ToUpper(c);
            }

            if (UnicodeUtility.IsAsciiCodePoint(c) && IsAsciiCasingSameAsInvariant)
            {
                return ToUpperAsciiInvariant(c);
            }

            return ChangeCase(c, toUpper: true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static char ToUpperInvariant(char c)
        {
            if (UnicodeUtility.IsAsciiCodePoint(c))
            {
                return ToUpperAsciiInvariant(c);
            }

            if (GlobalizationMode.Invariant)
            {
                return InvariantModeCasing.ToUpper(c);
            }

            return Invariant.ChangeCase(c, toUpper: true);
        }

        public string ToUpper(string str)
        {
            ArgumentNullException.ThrowIfNull(str);

            if (GlobalizationMode.Invariant)
            {
                return InvariantModeCasing.ToUpper(str);
            }

            return ChangeCaseCommon<ToUpperConversion>(str);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static char ToUpperAsciiInvariant(char c)
        {
            if (char.IsAsciiLetterLower(c))
            {
                c = (char)(c & 0x5F); // = low 7 bits of ~0x20
            }
            return c;
        }

        private bool IsAsciiCasingSameAsInvariant
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_isAsciiCasingSameAsInvariant == Tristate.NotInitialized)
                {
                    PopulateIsAsciiCasingSameAsInvariant();
                }

                Debug.Assert(_isAsciiCasingSameAsInvariant == Tristate.True || _isAsciiCasingSameAsInvariant == Tristate.False);
                return _isAsciiCasingSameAsInvariant == Tristate.True;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void PopulateIsAsciiCasingSameAsInvariant()
        {
            bool compareResult = CultureInfo.GetCultureInfo(_textInfoName).CompareInfo.Compare("abcdefghijklmnopqrstuvwxyz", "ABCDEFGHIJKLMNOPQRSTUVWXYZ", CompareOptions.IgnoreCase) == 0;
            _isAsciiCasingSameAsInvariant = (compareResult) ? Tristate.True : Tristate.False;
        }

        /// <summary>
        /// Returns true if the dominant direction of text and UI such as the
        /// relative position of buttons and scroll bars
        /// </summary>
        public bool IsRightToLeft => _cultureData.IsRightToLeft;

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is TextInfo otherTextInfo
                && CultureName.Equals(otherTextInfo.CultureName);
        }

        public override int GetHashCode() => CultureName.GetHashCode();

        public override string ToString()
        {
            return "TextInfo - " + _cultureData.CultureName;
        }

        /// <summary>
        /// Titlecasing refers to a casing practice wherein the first letter of a word is an uppercase letter
        /// and the rest of the letters are lowercase.  The choice of which words to titlecase in headings
        /// and titles is dependent on language and local conventions.  For example, "The Merry Wives of Windor"
        /// is the appropriate titlecasing of that play's name in English, with the word "of" not titlecased.
        /// In German, however, the title is "Die lustigen Weiber von Windsor," and both "lustigen" and "von"
        /// are not titlecased.  In French even fewer words are titlecased: "Les joyeuses commeres de Windsor."
        ///
        /// Moreover, the determination of what actually constitutes a word is language dependent, and this can
        /// influence which letter or letters of a "word" are uppercased when titlecasing strings.  For example
        /// "l'arbre" is considered two words in French, whereas "can't" is considered one word in English.
        /// </summary>
        public unsafe string ToTitleCase(string str)
        {
            ArgumentNullException.ThrowIfNull(str);

            if (str.Length == 0)
            {
                return str;
            }

            StringBuilder result = new StringBuilder();
            string? lowercaseData = null;
            // Store if the current culture is Dutch (special case)
            bool isDutchCulture = CultureName.StartsWith("nl-", StringComparison.OrdinalIgnoreCase);

            for (int i = 0; i < str.Length; i++)
            {
                UnicodeCategory charType = CharUnicodeInfo.GetUnicodeCategoryInternal(str, i, out int charLen);
                if (char.CheckLetter(charType))
                {
                    // Special case to check for Dutch specific titlecasing with "IJ" characters
                    // at the beginning of a word
                    if (isDutchCulture && i < str.Length - 1 && (str[i] == 'i' || str[i] == 'I') && (str[i + 1] == 'j' || str[i + 1] == 'J'))
                    {
                        result.Append("IJ");
                        i += 2;
                    }
                    else
                    {
                        // Do the titlecasing for the first character of the word.
                        i = AddTitlecaseLetter(ref result, ref str, i, charLen) + 1;
                    }

                    // Convert the characters until the end of the this word
                    // to lowercase.
                    int lowercaseStart = i;

                    // Use hasLowerCase flag to prevent from lowercasing acronyms (like "URT", "USA", etc)
                    // This is in line with Word 2000 behavior of titlecasing.
                    bool hasLowerCase = (charType == UnicodeCategory.LowercaseLetter);

                    // Use a loop to find all of the other letters following this letter.
                    while (i < str.Length)
                    {
                        charType = CharUnicodeInfo.GetUnicodeCategoryInternal(str, i, out charLen);
                        if (IsLetterCategory(charType))
                        {
                            if (charType == UnicodeCategory.LowercaseLetter)
                            {
                                hasLowerCase = true;
                            }
                            i += charLen;
                        }
                        else if (str[i] == '\'')
                        {
                            i++;
                            if (hasLowerCase)
                            {
                                lowercaseData ??= ToLower(str);
                                result.Append(lowercaseData, lowercaseStart, i - lowercaseStart);
                            }
                            else
                            {
                                result.Append(str, lowercaseStart, i - lowercaseStart);
                            }
                            lowercaseStart = i;
                            hasLowerCase = true;
                        }
                        else if (!IsWordSeparator(charType))
                        {
                            // This category is considered to be part of the word.
                            // This is any category that is marked as false in wordSeparator array.
                            i += charLen;
                        }
                        else
                        {
                            // A word separator. Break out of the loop.
                            break;
                        }
                    }

                    int count = i - lowercaseStart;

                    if (count > 0)
                    {
                        if (hasLowerCase)
                        {
                            lowercaseData ??= ToLower(str);
                            result.Append(lowercaseData, lowercaseStart, count);
                        }
                        else
                        {
                            result.Append(str, lowercaseStart, count);
                        }
                    }

                    if (i < str.Length)
                    {
                        // not a letter, just append it
                        i = AddNonLetter(ref result, ref str, i, charLen);
                    }
                }
                else
                {
                    // not a letter, just append it
                    i = AddNonLetter(ref result, ref str, i, charLen);
                }
            }
            return result.ToString();
        }

        private static int AddNonLetter(ref StringBuilder result, ref string input, int inputIndex, int charLen)
        {
            Debug.Assert(charLen == 1 || charLen == 2, "[TextInfo.AddNonLetter] CharUnicodeInfo.InternalGetUnicodeCategory returned an unexpected charLen!");
            if (charLen == 2)
            {
                // Surrogate pair
                result.Append(input[inputIndex++]);
                result.Append(input[inputIndex]);
            }
            else
            {
                result.Append(input[inputIndex]);
            }
            return inputIndex;
        }

        private int AddTitlecaseLetter(ref StringBuilder result, ref string input, int inputIndex, int charLen)
        {
            Debug.Assert(charLen == 1 || charLen == 2, "[TextInfo.AddTitlecaseLetter] CharUnicodeInfo.InternalGetUnicodeCategory returned an unexpected charLen!");

            if (charLen == 2)
            {
                // for surrogate pairs do a ToUpper operation on the substring
                ReadOnlySpan<char> src = input.AsSpan(inputIndex, 2);
                if (GlobalizationMode.Invariant)
                {
                    SurrogateCasing.ToUpper(src[0], src[1], out char h, out char l);
                    result.Append(h);
                    result.Append(l);
                }
                else
                {
                    Span<char> dst = stackalloc char[2];
                    ChangeCaseToUpper(src, dst);
                    result.Append(dst);
                }
                inputIndex++;
            }
            else
            {
                switch (input[inputIndex])
                {
                    // For AppCompat, the Titlecase Case Mapping data from NDP 2.0 is used below.
                    case (char)0x01C4:  // DZ with Caron -> Dz with Caron
                    case (char)0x01C5:  // Dz with Caron -> Dz with Caron
                    case (char)0x01C6:  // dz with Caron -> Dz with Caron
                        result.Append((char)0x01C5);
                        break;
                    case (char)0x01C7:  // LJ -> Lj
                    case (char)0x01C8:  // Lj -> Lj
                    case (char)0x01C9:  // lj -> Lj
                        result.Append((char)0x01C8);
                        break;
                    case (char)0x01CA:  // NJ -> Nj
                    case (char)0x01CB:  // Nj -> Nj
                    case (char)0x01CC:  // nj -> Nj
                        result.Append((char)0x01CB);
                        break;
                    case (char)0x01F1:  // DZ -> Dz
                    case (char)0x01F2:  // Dz -> Dz
                    case (char)0x01F3:  // dz -> Dz
                        result.Append((char)0x01F2);
                        break;
                    default:
                        result.Append(GlobalizationMode.Invariant ? InvariantModeCasing.ToUpper(input[inputIndex]) : ToUpper(input[inputIndex]));
                        break;
                }
            }
            return inputIndex;
        }

        private unsafe void ChangeCaseCore(char* src, int srcLen, char* dstBuffer, int dstBufferCapacity, bool bToUpper)
        {
            if (GlobalizationMode.UseNls)
            {
                NlsChangeCase(src, srcLen, dstBuffer, dstBufferCapacity, bToUpper);
                return;
            }
#if TARGET_BROWSER
            if (GlobalizationMode.Hybrid)
            {
                JsChangeCase(src, srcLen, dstBuffer, dstBufferCapacity, bToUpper);
                return;
            }
#endif
            IcuChangeCase(src, srcLen, dstBuffer, dstBufferCapacity, bToUpper);
        }

        // Used in ToTitleCase():
        // When we find a starting letter, the following array decides if a category should be
        // considered as word separator or not.
        private const int c_wordSeparatorMask =
            /* false */ (0 <<  0) | // UppercaseLetter = 0,
            /* false */ (0 <<  1) | // LowercaseLetter = 1,
            /* false */ (0 <<  2) | // TitlecaseLetter = 2,
            /* false */ (0 <<  3) | // ModifierLetter = 3,
            /* false */ (0 <<  4) | // OtherLetter = 4,
            /* false */ (0 <<  5) | // NonSpacingMark = 5,
            /* false */ (0 <<  6) | // SpacingCombiningMark = 6,
            /* false */ (0 <<  7) | // EnclosingMark = 7,
            /* false */ (0 <<  8) | // DecimalDigitNumber = 8,
            /* false */ (0 <<  9) | // LetterNumber = 9,
            /* false */ (0 << 10) | // OtherNumber = 10,
            /* true  */ (1 << 11) | // SpaceSeparator = 11,
            /* true  */ (1 << 12) | // LineSeparator = 12,
            /* true  */ (1 << 13) | // ParagraphSeparator = 13,
            /* true  */ (1 << 14) | // Control = 14,
            /* true  */ (1 << 15) | // Format = 15,
            /* false */ (0 << 16) | // Surrogate = 16,
            /* false */ (0 << 17) | // PrivateUse = 17,
            /* true  */ (1 << 18) | // ConnectorPunctuation = 18,
            /* true  */ (1 << 19) | // DashPunctuation = 19,
            /* true  */ (1 << 20) | // OpenPunctuation = 20,
            /* true  */ (1 << 21) | // ClosePunctuation = 21,
            /* true  */ (1 << 22) | // InitialQuotePunctuation = 22,
            /* true  */ (1 << 23) | // FinalQuotePunctuation = 23,
            /* true  */ (1 << 24) | // OtherPunctuation = 24,
            /* true  */ (1 << 25) | // MathSymbol = 25,
            /* true  */ (1 << 26) | // CurrencySymbol = 26,
            /* true  */ (1 << 27) | // ModifierSymbol = 27,
            /* true  */ (1 << 28) | // OtherSymbol = 28,
            /* false */ (0 << 29);  // OtherNotAssigned = 29;

        private static bool IsWordSeparator(UnicodeCategory category)
        {
            return (c_wordSeparatorMask & (1 << (int)category)) != 0;
        }

        private static bool IsLetterCategory(UnicodeCategory uc)
        {
            return uc == UnicodeCategory.UppercaseLetter
                 || uc == UnicodeCategory.LowercaseLetter
                 || uc == UnicodeCategory.TitlecaseLetter
                 || uc == UnicodeCategory.ModifierLetter
                 || uc == UnicodeCategory.OtherLetter;
        }

        // A dummy struct that is used for 'ToUpper' in generic parameters
        private readonly struct ToUpperConversion { }

        // A dummy struct that is used for 'ToLower' in generic parameters
        private readonly struct ToLowerConversion { }
    }
}
