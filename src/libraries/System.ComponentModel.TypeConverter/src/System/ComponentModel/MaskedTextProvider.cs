// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Versioning;
using System.Text;

namespace System.ComponentModel
{
    /// <summary>
    /// Provides functionality for formatting a test string against a mask string.
    /// MaskedTextProvider is stateful, it keeps information about the input characters so
    /// multiple call to Add/Remove will work in the same buffer.
    /// Most of the operations are performed on a virtual string containing the input characters as opposed
    /// to the test string itself, since mask literals cannot be modified (i.e: replacing on a literal position
    /// will actually replace on the nearest edit position forward).
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] // needed by Clone() to preserve derived ctors
    public class MaskedTextProvider : ICloneable
    {
        ///
        /// Some concept definitions:
        ///
        /// 'mask'             : A string representing the mask associated with an instance of this class.
        /// 'test string'      : A string representing the user's text formatted as specified by the mask.
        /// 'virtual text'     : The characters entered by the user to be converted into the 'test string'.
        ///              no buffer exists to hold them since they're stored in the test string but
        ///              we keep an array with their position in the test string for fast access.
        /// 'text indexer'     : An array which values point to 'edit char' positions in the test string and
        ///              indexes correspond to the position in the user's text.
        /// 'char descriptor'  : A structure describing a char constraints as specified in the mask plus some
        ///              other info.
        /// 'string descriptor': An array of char descriptor objects describing the chars in the 'test string',
        ///              the indexes of this array represent the position of the chars in the string.


        /// <summary>
        /// Char case conversion type used when '&gt;' (subsequent chars to upper case) or '&lt;' (subsequent chars to lower case)
        /// are specified in the mask.
        /// </summary>
        private enum CaseConversion
        {
            None,
            ToLower,
            ToUpper
        }

        /// <summary>
        /// Type of the characters in the test string according to the mask language.
        /// </summary>
        [Flags]
        private enum CharType
        {
            EditOptional = 0x01, // editable char  ('#', '9', 'A', 'a', etc) optional.
            EditRequired = 0x02, // editable char  ('#', '9', 'A', 'a', etc) required.
            Separator = 0x04, // separator char ('.', ',', ':', '$').
            Literal = 0x08, // literal char   ('\\', '-', etc)
            Modifier = 0x10  // char modifier  ('>', '<')
        }

        /// <summary>
        /// This structure describes some constraints and properties of a character in the test string, as specified
        /// in the mask.
        /// </summary>
        private sealed class CharDescriptor
        {
            // The position the character holds in the mask string. Required for testing the character against the mask.
            public int MaskPosition;

            // The char case conversion specified in the mask. Required for formatting the string when requested.
            public CaseConversion CaseConversion;

            // The char type according to the mask language indentifiers. (Separator, Editable char...).
            // Required for validating the input char.
            public CharType CharType;

            // Specifies whether the editable char has been assigned a value. Meaningful to edit chars only.
            public bool IsAssigned;

            // constructors.
            public CharDescriptor(int maskPos, CharType charType)
            {
                MaskPosition = maskPos;
                CharType = charType;
            }

            public override string ToString()
            {
                return string.Format(
                                        CultureInfo.InvariantCulture,
                                        "MaskPosition[{0}] <CaseConversion.{1}><CharType.{2}><IsAssigned: {3}",
                                        MaskPosition,
                                        CaseConversion,
                                        CharType,
                                        IsAssigned
                                     );
            }
        }

        //// class data.

        private const char SPACE_CHAR = ' ';
        private const char DEFAULT_PROMPT_CHAR = '_';
        private const char NULL_PASSWORD_CHAR = '\0';
        private const bool DEFAULT_ALLOW_PROMPT = true;
        private const int  INVALID_INDEX = -1;
        private const byte EDIT_ANY = 0;
        private const byte EDIT_UNASSIGNED = 1;
        private const byte EDIT_ASSIGNED = 2;
        private const bool FORWARD = true;
        private const bool BACKWARD = false;

        // Bit masks for bool properties.
        private static readonly int s_ASCII_ONLY = BitVector32.CreateMask();
        private static readonly int s_ALLOW_PROMPT_AS_INPUT = BitVector32.CreateMask(s_ASCII_ONLY);
        private static readonly int s_INCLUDE_PROMPT = BitVector32.CreateMask(s_ALLOW_PROMPT_AS_INPUT);
        private static readonly int s_INCLUDE_LITERALS = BitVector32.CreateMask(s_INCLUDE_PROMPT);
        private static readonly int s_RESET_ON_PROMPT = BitVector32.CreateMask(s_INCLUDE_LITERALS);
        private static readonly int s_RESET_ON_LITERALS = BitVector32.CreateMask(s_RESET_ON_PROMPT);
        private static readonly int s_SKIP_SPACE = BitVector32.CreateMask(s_RESET_ON_LITERALS);

        // Type cached to speed up cloning of this object.
        private static readonly Type s_maskTextProviderType = typeof(MaskedTextProvider);

        //// Instance data.

        // Bit vector to represent bool variables.
        private BitVector32 _flagState;

        // Used to obtained localized placeholder chars (date separator for instance).

        // the formatted string.
        private StringBuilder _testString;

        // the number of assigned edit chars.

        // the number of assigned required edit chars.
        private int _requiredCharCount;

        // the number of required edit positions in the test string.
        private int _requiredEditChars;

        // the number of optional edit positions in the test string.
        private int _optionalEditChars;

        // Properties backend fields (see corresponding property for info).
        private char _passwordChar;
        private char _promptChar;

        // We maintain an array (string descriptor table) of CharDescriptor elements describing the characters in the
        // test string, as specified in the mask. It allows us to access character information in constant time since
        // we don't have to traverse the mask or test string whenever we need that information.
        private List<CharDescriptor> _stringDescriptor;

        ////// Construction API

        /// <summary>
        /// Creates a MaskedTextProvider object from the specified mask.
        /// </summary>
        public MaskedTextProvider(string mask)
            : this(mask, null, DEFAULT_ALLOW_PROMPT, DEFAULT_PROMPT_CHAR, NULL_PASSWORD_CHAR, false)
        {
        }

        /// <summary>
        /// Creates a MaskedTextProvider object from the specified mask.
        /// 'restrictToAscii' specifies whether the input characters should be restricted to ASCII characters only.
        /// </summary>
        public MaskedTextProvider(string mask, bool restrictToAscii)
            : this(mask, null, DEFAULT_ALLOW_PROMPT, DEFAULT_PROMPT_CHAR, NULL_PASSWORD_CHAR, restrictToAscii)
        {
        }

        /// <summary>
        /// Creates a MaskedTextProvider object from the specified mask.
        /// 'culture' is used to set the separator characters to the corresponding locale character; if null, the current
        ///      culture is used.
        /// </summary>
        public MaskedTextProvider(string mask, CultureInfo? culture)
            : this(mask, culture, DEFAULT_ALLOW_PROMPT, DEFAULT_PROMPT_CHAR, NULL_PASSWORD_CHAR, false)
        {
        }

        /// <summary>
        /// Creates a MaskedTextProvider object from the specified mask.
        /// 'culture' is used to set the separator characters to the corresponding locale character; if null, the current
        ///      culture is used.
        /// 'restrictToAscii' specifies whether the input characters should be restricted to ASCII characters only.
        /// </summary>
        public MaskedTextProvider(string mask, CultureInfo? culture, bool restrictToAscii)
            : this(mask, culture, DEFAULT_ALLOW_PROMPT, DEFAULT_PROMPT_CHAR, NULL_PASSWORD_CHAR, restrictToAscii)
        {
        }

        /// <summary>
        /// Creates a MaskedTextProvider object from the specified mask .
        /// 'passwordChar' specifies the character to be used in the password string.
        /// 'allowPromptAsInput' specifies whether the prompt character should be accepted as a valid input or not.
        /// </summary>
        public MaskedTextProvider(string mask, char passwordChar, bool allowPromptAsInput)
            : this(mask, null, allowPromptAsInput, DEFAULT_PROMPT_CHAR, passwordChar, false)
        {
        }

        /// <summary>
        /// Creates a MaskedTextProvider object from the specified mask .
        /// 'passwordChar' specifies the character to be used in the password string.
        /// 'allowPromptAsInput' specifies whether the prompt character should be accepted as a valid input or not.
        /// </summary>
        public MaskedTextProvider(string mask, CultureInfo? culture, char passwordChar, bool allowPromptAsInput)
            : this(mask, culture, allowPromptAsInput, DEFAULT_PROMPT_CHAR, passwordChar, false)
        {
        }

        /// <summary>
        /// Creates a MaskedTextProvider object from the specified mask.
        /// 'culture' is used to set the separator characters to the corresponding locale character; if null, the current
        ///      culture is used.
        /// 'allowPromptAsInput' specifies whether the prompt character should be accepted as a valid input or not.
        /// 'promptChar' specifies the character to be used for the prompt.
        /// 'passwordChar' specifies the character to be used in the password string.
        /// 'restrictToAscii' specifies whether the input characters should be restricted to ASCII characters only.
        /// </summary>
        public MaskedTextProvider(string mask, CultureInfo? culture, bool allowPromptAsInput, char promptChar, char passwordChar, bool restrictToAscii)
        {
            if (string.IsNullOrEmpty(mask))
            {
                throw new ArgumentException(SR.MaskedTextProviderMaskNullOrEmpty, nameof(mask));
            }

            foreach (char c in mask)
            {
                if (!IsPrintableChar(c))
                {
                    throw new ArgumentException(SR.MaskedTextProviderMaskInvalidChar);
                }
            }

            if (culture == null)
            {
                culture = CultureInfo.CurrentCulture;
            }

            _flagState = default;

            // read only property-backend fields.

            Mask = mask;
            _promptChar = promptChar;
            _passwordChar = passwordChar;

            //Neutral cultures cannot be queried for culture-specific information.
            if (culture.IsNeutralCulture)
            {
                // find the first specific (non-neutral) culture that contains country/region specific info.
                foreach (CultureInfo tempCulture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
                {
                    if (culture.Equals(tempCulture.Parent))
                    {
                        Culture = tempCulture;
                        break;
                    }
                }

                // Last resort use invariant culture.
                if (Culture == null)
                {
                    Culture = CultureInfo.InvariantCulture;
                }
            }
            else
            {
                Culture = culture;
            }

            if (!Culture.IsReadOnly)
            {
                Culture = CultureInfo.ReadOnly(Culture);
            }

            _flagState[s_ALLOW_PROMPT_AS_INPUT] = allowPromptAsInput;
            _flagState[s_ASCII_ONLY] = restrictToAscii;

            // set default values for read/write properties.

            _flagState[s_INCLUDE_PROMPT] = false;
            _flagState[s_INCLUDE_LITERALS] = true;
            _flagState[s_RESET_ON_PROMPT] = true;
            _flagState[s_SKIP_SPACE] = true;
            _flagState[s_RESET_ON_LITERALS] = true;

            Initialize();
        }

        /// <summary>
        /// Initializes the test string according to the mask and populates the character descriptor table
        /// (stringDescriptor).
        /// </summary>
        [MemberNotNull(nameof(_testString))]
        [MemberNotNull(nameof(_stringDescriptor))]
        private void Initialize()
        {
            _testString = new StringBuilder();
            _stringDescriptor = new List<CharDescriptor>();

            CaseConversion caseConversion = CaseConversion.None; // The conversion specified in the mask.
            bool escapedChar = false;            // indicates the current char is to be escaped.
            int testPosition = 0;                // the position of the char in the test string.
            CharType charType = CharType.Literal; // the mask language char type.
            char ch;                              // the char under test.
            string locSymbol = string.Empty;     // the locale symbol corresponding to a separator in the mask.
                                                 // in some cultures a symbol is represented with more than one
                                                 // char, for instance '$' for en-JA is '$J'.

            //
            // Traverse the mask to generate the test string and the string descriptor table so we don't have
            // to traverse those strings anymore.
            //
            for (int maskPos = 0; maskPos < Mask.Length; maskPos++)
            {
                ch = Mask[maskPos];
                if (!escapedChar)   // if false treat the char as literal.
                {
                    switch (ch)
                    {
                        //
                        // Mask language placeholders.
                        // set the corresponding localized char to be added to the test string.
                        //
                        case '.':   // decimal separator.
                            locSymbol = Culture.NumberFormat.NumberDecimalSeparator;
                            charType = CharType.Separator;
                            break;

                        case ',':   // thousands separator.
                            locSymbol = Culture.NumberFormat.NumberGroupSeparator;
                            charType = CharType.Separator;
                            break;

                        case ':':   // time separator.
                            locSymbol = Culture.DateTimeFormat.TimeSeparator;
                            charType = CharType.Separator;
                            break;

                        case '/':   // date separator.
                            locSymbol = Culture.DateTimeFormat.DateSeparator;
                            charType = CharType.Separator;
                            break;

                        case '$':   // currency symbol.
                            locSymbol = Culture.NumberFormat.CurrencySymbol;
                            charType = CharType.Separator;
                            break;

                        //
                        // Mask language modifiers.
                        // StringDescriptor won't have an entry for modifiers, the modified character
                        // descriptor contains an entry for case conversion that is set accordingly.
                        // Just set the appropriate flag for the characters that follow and continue.
                        //
                        case '<':   // convert all chars that follow to lowercase.
                            caseConversion = CaseConversion.ToLower;
                            continue;

                        case '>':   // convert all chars that follow to uppercase.
                            caseConversion = CaseConversion.ToUpper;
                            continue;

                        case '|':   // no convertion performed on the chars that follow.
                            caseConversion = CaseConversion.None;
                            continue;

                        case '\\':   // escape char - next will be a literal.
                            escapedChar = true;
                            charType = CharType.Literal;
                            continue;

                        //
                        // Mask language edit identifiers (#, 9, &, C, A, a, ?).
                        // Populate a CharDescriptor structure desrcribing the editable char corresponding to this
                        // identifier.
                        //
                        case '0':   // digit required.
                        case 'L':   // letter required.
                        case '&':   // any character required.
                        case 'A':   // alphanumeric (letter or digit) required.
                            _requiredEditChars++;
                            ch = _promptChar;                     // replace edit identifier with prompt.
                            charType = CharType.EditRequired;         // set char as editable.
                            break;

                        case '?':   // letter optional (space OK).
                        case '9':   // digit optional (space OK).
                        case '#':   // digit or plus/minus sign optional (space OK).
                        case 'C':   // any character optional (space OK).
                        case 'a':   // alphanumeric (letter or digit) optional.
                            _optionalEditChars++;
                            ch = _promptChar;                     // replace edit identifier with prompt.
                            charType = CharType.EditOptional;         // set char as editable.
                            break;

                        //
                        // Literals just break so they're added to the test string.
                        //
                        default:
                            charType = CharType.Literal;
                            break;
                    }
                }
                else
                {
                    escapedChar = false; // reset flag since the escaped char is now going to be added to the test string.
                }

                // Populate a character descriptor for the current character (or loc symbol).
                CharDescriptor chDex = new CharDescriptor(maskPos, charType);

                if (IsEditPosition(chDex))
                {
                    chDex.CaseConversion = caseConversion;
                }

                // Now let's add the character to the string description table.
                // For code clarity we treat all characters as localizable symbols (can have multi-char representation).

                if (charType != CharType.Separator)
                {
                    locSymbol = ch.ToString();
                }

                foreach (char chVal in locSymbol)
                {
                    _testString.Append(chVal);
                    _stringDescriptor.Add(chDex);
                    testPosition++;
                }
            }

            //
            // Trim test string to needed size.
            //
            _testString.Capacity = _testString.Length;
        }


        ////// Properties


        /// <summary>
        /// Specifies whether the prompt character should be treated as a valid input character or not.
        /// </summary>
        public bool AllowPromptAsInput => _flagState[s_ALLOW_PROMPT_AS_INPUT];

        /// <summary>
        /// Retrieves the number of editable characters that have been set.
        /// </summary>
        public int AssignedEditPositionCount { get; private set; }

        /// <summary>
        /// Retrieves the number of editable characters that have been set.
        /// </summary>
        public int AvailableEditPositionCount => EditPositionCount - AssignedEditPositionCount;

        /// <summary>
        /// Creates a 'clean' (no text assigned) MaskedTextProvider instance with the same property values as the
        /// current instance.
        /// Derived classes can override this method and call base.Clone to get proper cloning semantics but must
        /// implement the full-parameter constructor (passing parameters to the base constructor as well).
        /// </summary>
        [UnsupportedOSPlatform("browser")]
        public object Clone()
        {
            MaskedTextProvider clonedProvider;
            Type providerType = GetType();

            if (providerType == s_maskTextProviderType)
            {
                clonedProvider = new MaskedTextProvider(
                                                        Mask,
                                                        Culture,
                                                        AllowPromptAsInput,
                                                        PromptChar,
                                                        PasswordChar,
                                                        AsciiOnly);
            }
            else // A derived Type instance used.
            {
                object[] parameters = new object[]
                {
                    Mask,
                    Culture,
                    AllowPromptAsInput,
                    PromptChar,
                    PasswordChar,
                    AsciiOnly
                };

                clonedProvider = (Activator.CreateInstance(providerType, parameters) as MaskedTextProvider)!;
            }

            clonedProvider.ResetOnPrompt = false;
            clonedProvider.ResetOnSpace = false;
            clonedProvider.SkipLiterals = false;

            for (int position = 0; position < _testString.Length; position++)
            {
                CharDescriptor chDex = _stringDescriptor[position];

                if (IsEditPosition(chDex) && chDex.IsAssigned)
                {
                    clonedProvider.Replace(_testString[position], position);
                }
            }

            clonedProvider.ResetOnPrompt = ResetOnPrompt;
            clonedProvider.ResetOnSpace = ResetOnSpace;
            clonedProvider.SkipLiterals = SkipLiterals;
            clonedProvider.IncludeLiterals = IncludeLiterals;
            clonedProvider.IncludePrompt = IncludePrompt;

            return clonedProvider;
        }

        /// <summary>
        /// The culture that determines the value of the localizable mask language separators and placeholders.
        /// </summary>
        public CultureInfo Culture { get; }

        /// <summary>
        /// The system password char.
        /// </summary>
        /// <remarks>
        /// ComCtl32.dll V6 (WindowsXP) provides a nice black circle but we don't want to attempt to simulate it
        /// here to avoid hard coding values. MaskedTextBox picks up the right value at run time from comctl32.
        /// </remarks>
        public static char DefaultPasswordChar => '*';

        /// <summary>
        /// The number of editable positions in the test string.
        /// </summary>
        public int EditPositionCount => _optionalEditChars + _requiredEditChars;

        /// <summary>
        /// Returns a new IEnumerator object containing the editable positions in the test string.
        /// </summary>
        public System.Collections.IEnumerator EditPositions
        {
            get
            {
                List<int> editPositions = new List<int>();
                int position = 0;

                foreach (CharDescriptor chDex in _stringDescriptor)
                {
                    if (IsEditPosition(chDex))
                    {
                        editPositions.Add(position);
                    }

                    position++;
                }

                return ((System.Collections.IList)editPositions).GetEnumerator();
            }
        }

        /// <summary>
        /// Specifies whether the formatted string should include literals.
        /// </summary>
        public bool IncludeLiterals
        {
            get
            {
                return _flagState[s_INCLUDE_LITERALS];
            }
            set
            {
                _flagState[s_INCLUDE_LITERALS] = value;
            }
        }

        /// <summary>
        /// Specifies whether or not the prompt character should be included in the formatted text when there are
        /// character slots available in the mask.
        /// </summary>
        public bool IncludePrompt
        {
            get
            {
                return _flagState[s_INCLUDE_PROMPT];
            }
            set
            {
                _flagState[s_INCLUDE_PROMPT] = value;
            }
        }

        /// <summary>
        /// Specifies whether only ASCII characters are accepted as valid input.
        /// </summary>
        public bool AsciiOnly => _flagState[s_ASCII_ONLY];

        /// <summary>
        /// Specifies whether the user text is to be rendered as password characters.
        /// </summary>
        public bool IsPassword
        {
            get
            {
                return _passwordChar != '\0';
            }

            set
            {
                if (IsPassword != value)
                {
                    _passwordChar = value ? DefaultPasswordChar : NULL_PASSWORD_CHAR;
                }
            }
        }

        /// <summary>
        /// A negative value representing an index outside the test string.
        /// </summary>
        public static int InvalidIndex => INVALID_INDEX;

        /// <summary>
        /// The last edit position (relative to the origin not to time) in the test string where
        /// an input character has been placed. If no position has been assigned, InvalidIndex is returned.
        /// </summary>
        public int LastAssignedPosition => FindAssignedEditPositionFrom(_testString.Length - 1, BACKWARD);

        /// <summary>
        /// Specifies the length of the test string.
        /// </summary>
        public int Length => _testString.Length;

        /// <summary>
        /// The mask to be applied to the test string.
        /// </summary>
        public string Mask { get; }

        /// <summary>
        /// Specifies whether all required inputs have been provided into the mask successfully.
        /// </summary>
        public bool MaskCompleted
        {
            get
            {
                Debug.Assert(AssignedEditPositionCount >= 0, "Invalid count of assigned chars.");
                return _requiredCharCount == _requiredEditChars;
            }
        }

        /// <summary>
        /// Specifies whether all inputs (required and optional) have been provided into the mask successfully.
        /// </summary>
        public bool MaskFull
        {
            get
            {
                Debug.Assert(AssignedEditPositionCount >= 0, "Invalid count of assigned chars.");
                return AssignedEditPositionCount == EditPositionCount;
            }
        }

        /// <summary>
        /// Specifies the character to be used in the formatted string in place of editable characters.
        /// Use the null character '\0' to reset this property.
        /// </summary>
        public char PasswordChar
        {
            get
            {
                return _passwordChar;
            }

            set
            {
                if (value == _promptChar)
                {
                    // Prompt and password chars must be different.
                    throw new InvalidOperationException(SR.MaskedTextProviderPasswordAndPromptCharError);
                }

                if (!IsValidPasswordChar(value) && (value != NULL_PASSWORD_CHAR))
                {
                    // Same message as in SR.MaskedTextBoxInvalidCharError.
                    throw new ArgumentException(SR.MaskedTextProviderInvalidCharError);
                }

                if (value != _passwordChar)
                {
                    _passwordChar = value;
                }
            }
        }

        /// <summary>
        /// Specifies the prompt character to be used in the formatted string for unsupplied characters.
        /// </summary>
        public char PromptChar
        {
            get
            {
                return _promptChar;
            }

            set
            {
                if (value == _passwordChar)
                {
                    // Prompt and password chars must be different.
                    throw new InvalidOperationException(SR.MaskedTextProviderPasswordAndPromptCharError);
                }

                if (!IsPrintableChar(value))
                {
                    // Same message as in SR.MaskedTextBoxInvalidCharError.
                    throw new ArgumentException(SR.MaskedTextProviderInvalidCharError);
                }

                if (value != _promptChar)
                {
                    _promptChar = value;

                    for (int position = 0; position < _testString.Length; position++)
                    {
                        CharDescriptor chDex = _stringDescriptor[position];

                        if (IsEditPosition(position) && !chDex.IsAssigned)
                        {
                            _testString[position] = _promptChar;
                        }
                    }
                }
            }
        }



        /// <summary>
        /// Specifies whether to reset and skip the current position if editable, when the input character has
        /// the same value as the prompt.
        ///
        /// This is useful when assigning text that was saved including the prompt; in this case
        /// we don't want to take the prompt character as valid input but don't want to fail the test either.
        /// </summary>
        public bool ResetOnPrompt
        {
            get
            {
                return _flagState[s_RESET_ON_PROMPT];
            }
            set
            {
                _flagState[s_RESET_ON_PROMPT] = value;
            }
        }

        /// <summary>
        /// Specifies whether to reset and skip the current position if editable, when the input is the space character.
        ///
        /// This is useful when assigning text that was saved excluding the prompt (prompt replaced with spaces);
        /// in this case we don't want to take the space but instead, reset the position (or just skip it) so the
        /// next input character gets positioned correctly.
        /// </summary>
        public bool ResetOnSpace
        {
            get
            {
                return _flagState[s_SKIP_SPACE];
            }
            set
            {
                _flagState[s_SKIP_SPACE] = value;
            }
        }


        /// <summary>
        /// Specifies whether to skip the current position if non-editable and the input character has the same
        /// value as the literal at that position.
        ///
        /// This is useful for round-tripping the text when saved with literals; when assigned back we don't want
        /// to treat literals as input.
        /// </summary>
        public bool SkipLiterals
        {
            get
            {
                return _flagState[s_RESET_ON_LITERALS];
            }
            set
            {
                _flagState[s_RESET_ON_LITERALS] = value;
            }
        }

        /// <summary>
        /// Indexer.
        /// </summary>
        public char this[int index]
        {
            get
            {
                if (index < 0 || index >= _testString.Length)
                {
                    throw new IndexOutOfRangeException(index.ToString(CultureInfo.CurrentCulture));
                }

                return _testString[index];
            }
        }

        ////// Methods

        /// <summary>
        /// Attempts to add the specified charactert to the last unoccupied positions in the test string (append text to
        /// the virtual string).
        /// Returns true on success, false otherwise.
        /// </summary>
        public bool Add(char input)
        {
            int dummyVar;
            MaskedTextResultHint dummyVar2;
            return Add(input, out dummyVar, out dummyVar2);
        }

        /// <summary>
        /// Attempts to add the specified charactert to the last unoccupied positions in the test string (append text to
        /// the virtual string).
        /// On exit the testPosition contains last position where the primary operation was actually performed if successful,
        /// otherwise the first position that made the test fail. This position is relative to the test string.
        /// The MaskedTextResultHint out param gives a hint about the operation result reason.
        /// Returns true on success, false otherwise.
        /// </summary>
        public bool Add(char input, out int testPosition, out MaskedTextResultHint resultHint)
        {
            int lastAssignedPos = LastAssignedPosition;

            if (lastAssignedPos == _testString.Length - 1)    // at the last edit char position.
            {
                testPosition = _testString.Length;
                resultHint = MaskedTextResultHint.UnavailableEditPosition;
                return false;
            }

            // Get position after last assigned position.
            testPosition = lastAssignedPos + 1;
            testPosition = FindEditPositionFrom(testPosition, FORWARD);

            if (testPosition == INVALID_INDEX)
            {
                resultHint = MaskedTextResultHint.UnavailableEditPosition;
                testPosition = _testString.Length;
                return false;
            }

            if (!TestSetChar(input, testPosition, out resultHint))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Attempts to add the characters in the specified string to the last unoccupied positions in the test string
        /// (append text to the virtual string).
        /// Returns true on success, false otherwise.
        /// </summary>
        public bool Add(string input)
        {
            int dummyVar;
            MaskedTextResultHint dummyVar2;
            return Add(input, out dummyVar, out dummyVar2);
        }

        /// <summary>
        /// Attempts to add the characters in the specified string to the last unoccupied positions in the test string
        /// (append text to the virtual string).
        /// On exit the testPosition contains last position where the primary operation was actually performed if successful,
        /// otherwise the first position that made the test fail. This position is relative to the test string.
        /// The MaskedTextResultHint out param gives a hint about the operation result reason.
        /// Returns true on success, false otherwise.
        /// </summary>
        public bool Add(string input, out int testPosition, out MaskedTextResultHint resultHint)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            testPosition = LastAssignedPosition + 1;

            if (input.Length == 0) // nothing to add.
            {
                // Get position where the test would be performed.
                resultHint = MaskedTextResultHint.NoEffect;
                return true;
            }

            return TestSetString(input, testPosition, out testPosition, out resultHint);
        }

        /// <summary>
        /// Resets the state of the test string edit chars. (Remove all characters from the virtual string).
        /// </summary>
        public void Clear()
        {
            MaskedTextResultHint dummyHint;
            Clear(out dummyHint);
        }

        /// <summary>
        /// Resets the state of the test string edit chars. (Remove all characters from the virtual string).
        /// The MaskedTextResultHint out param gives more information about the operation result.
        /// </summary>
        public void Clear(out MaskedTextResultHint resultHint)
        {
            if (AssignedEditPositionCount == 0)
            {
                resultHint = MaskedTextResultHint.NoEffect;
                return;
            }

            resultHint = MaskedTextResultHint.Success;

            for (int position = 0; position < _testString.Length; position++)
            {
                ResetChar(position);
            }
        }

        /// <summary>
        /// Gets the position of the first edit char in the test string, the search starts from the specified
        /// position included.
        /// Returns InvalidIndex if it doesn't find one.
        /// </summary>
        public int FindAssignedEditPositionFrom(int position, bool direction)
        {
            if (AssignedEditPositionCount == 0)
            {
                return INVALID_INDEX;
            }

            int startPosition;
            int endPosition;

            if (direction == FORWARD)
            {
                startPosition = position;
                endPosition = _testString.Length - 1;
            }
            else
            {
                startPosition = 0;
                endPosition = position;
            }

            return FindAssignedEditPositionInRange(startPosition, endPosition, direction);
        }

        /// <summary>
        /// Gets the position of the first edit char in the test string in the specified range, the search starts from
        /// the specified  position included.
        /// Returns InvalidIndex if it doesn't find one.
        /// </summary>
        public int FindAssignedEditPositionInRange(int startPosition, int endPosition, bool direction)
        {
            if (AssignedEditPositionCount == 0)
            {
                return INVALID_INDEX;
            }

            return FindEditPositionInRange(startPosition, endPosition, direction, EDIT_ASSIGNED);
        }

        /// <summary>
        /// Gets the position of the first assigned edit char in the test string, the search starts from the specified
        /// position included and in the direction specified (true == forward). The positions are relative to the test
        /// string.
        /// Returns InvalidIndex if it doesn't find one.
        /// </summary>
        public int FindEditPositionFrom(int position, bool direction)
        {
            int startPosition;
            int endPosition;

            if (direction == FORWARD)
            {
                startPosition = position;
                endPosition = _testString.Length - 1;
            }
            else
            {
                startPosition = 0;
                endPosition = position;
            }

            return FindEditPositionInRange(startPosition, endPosition, direction);
        }

        /// <summary>
        /// Gets the position of the first assigned edit char in the test string; the search is performed in the specified
        /// positions range and in the specified direction.
        /// The positions are relative to the test string.
        /// Returns InvalidIndex if it doesn't find one.
        /// </summary>
        public int FindEditPositionInRange(int startPosition, int endPosition, bool direction)
        {
            CharType editCharFlags = CharType.EditOptional | CharType.EditRequired;
            return FindPositionInRange(startPosition, endPosition, direction, editCharFlags);
        }

        /// <summary>
        /// Gets the position of the first edit char in the test string in the specified range, according to the
        /// assignedRequired parameter; if true, it gets the first assigned position otherwise the first unassigned one.
        /// The search starts from the specified position included.
        /// Returns InvalidIndex if it doesn't find one.
        /// </summary>
        private int FindEditPositionInRange(int startPosition, int endPosition, bool direction, byte assignedStatus)
        {
            // out of range position is handled in FindEditPositionFrom method.
            int testPosition;

            do
            {
                testPosition = FindEditPositionInRange(startPosition, endPosition, direction);

                if (testPosition == INVALID_INDEX)  // didn't find any.
                {
                    break;
                }

                CharDescriptor chDex = _stringDescriptor[testPosition];

                switch (assignedStatus)
                {
                    case EDIT_UNASSIGNED:
                        if (!chDex.IsAssigned)
                        {
                            return testPosition;
                        }
                        break;

                    case EDIT_ASSIGNED:
                        if (chDex.IsAssigned)
                        {
                            return testPosition;
                        }
                        break;

                    default: // don't care
                        return testPosition;
                }

                if (direction == FORWARD)
                {
                    startPosition++;
                }
                else
                {
                    endPosition--;
                }
            }
            while (startPosition <= endPosition);

            return INVALID_INDEX;
        }

        /// <summary>
        /// Gets the position of the first non edit position in the test string; the search is performed from the specified
        /// position and in the specified direction.
        /// The positions are relative to the test string.
        /// Returns InvalidIndex if it doesn't find one.
        /// </summary>
        public int FindNonEditPositionFrom(int position, bool direction)
        {
            int startPosition;
            int endPosition;

            if (direction == FORWARD)
            {
                startPosition = position;
                endPosition = _testString.Length - 1;
            }
            else
            {
                startPosition = 0;
                endPosition = position;
            }

            return FindNonEditPositionInRange(startPosition, endPosition, direction);
        }

        /// <summary>
        /// Gets the position of the first non edit position in the test string; the search is performed in the specified
        /// positions range and in the specified direction.
        /// The positions are relative to the test string.
        /// Returns InvalidIndex if it doesn't find one.
        /// </summary>
        public int FindNonEditPositionInRange(int startPosition, int endPosition, bool direction)
        {
            CharType literalCharFlags = CharType.Literal | CharType.Separator;
            return FindPositionInRange(startPosition, endPosition, direction, literalCharFlags);
        }

        /// <summary>
        /// Finds a position in the test string according to the needed position type (needEditPos).
        /// The positions are relative to the test string.
        /// Returns InvalidIndex if it doesn't find one.
        /// </summary>
        private int FindPositionInRange(int startPosition, int endPosition, bool direction, CharType charTypeFlags)
        {
            if (startPosition < 0)
            {
                startPosition = 0;
            }

            if (endPosition >= _testString.Length)
            {
                endPosition = _testString.Length - 1;
            }

            if (startPosition > endPosition)
            {
                return INVALID_INDEX;
            }

            // Iterate through the test string until we find an edit char position.
            int testPosition;

            while (startPosition <= endPosition)
            {
                testPosition = (direction == FORWARD) ? startPosition++ : endPosition--;

                CharDescriptor chDex = _stringDescriptor[testPosition];

                if ((chDex.CharType & charTypeFlags) == chDex.CharType)
                {
                    return testPosition;
                }
            }

            return INVALID_INDEX;
        }

        /// <summary>
        /// Gets the position of the first edit char in the test string, the search starts from the specified
        /// position included.
        /// Returns InvalidIndex if it doesn't find one.
        /// </summary>
        public int FindUnassignedEditPositionFrom(int position, bool direction)
        {
            int startPosition;
            int endPosition;

            if (direction == FORWARD)
            {
                startPosition = position;
                endPosition = _testString.Length - 1;
            }
            else
            {
                startPosition = 0;
                endPosition = position;
            }

            return FindEditPositionInRange(startPosition, endPosition, direction, EDIT_UNASSIGNED);
        }

        /// <summary>
        /// Gets the position of the first edit char in the test string in the specified range; the search starts
        /// from the specified position included.
        /// Returns InvalidIndex if it doesn't find one.
        /// </summary>
        public int FindUnassignedEditPositionInRange(int startPosition, int endPosition, bool direction)
        {
            int position;

            while (true)
            {
                position = FindEditPositionInRange(startPosition, endPosition, direction, EDIT_ANY);

                if (position == INVALID_INDEX)
                {
                    return INVALID_INDEX;
                }

                CharDescriptor chDex = _stringDescriptor[position];

                if (!chDex.IsAssigned)
                {
                    return position;
                }

                if (direction == FORWARD)
                {
                    startPosition++;
                }
                else
                {
                    endPosition--;
                }
            }
        }

        /// <summary>
        /// Specifies whether the specified MaskedTextResultHint denotes success or not.
        /// </summary>
        public static bool GetOperationResultFromHint(MaskedTextResultHint hint)
        {
            return ((int)hint) > 0;
        }

        /// <summary>
        /// Attempts to insert the specified character at the specified position in the test string.
        /// (Insert character in the virtual string).
        /// Returns true on success, false otherwise.
        /// </summary>
        public bool InsertAt(char input, int position)
        {
            if (position < 0 || position >= _testString.Length)
            {
                return false;
                //throw new ArgumentOutOfRangeException("position");
            }

            return InsertAt(input.ToString(), position);
        }

        /// <summary>
        /// Attempts to insert the specified character at the specified position in the test string, shifting characters
        /// at upper positions (if any) to make room for the input.
        /// On exit the testPosition contains last position where the primary operation was actually performed if successful,
        /// otherwise the first position that made the test fail. This position is relative to the test string.
        /// The MaskedTextResultHint out param gives more information about the operation result.
        /// Returns true on success, false otherwise.
        /// </summary>
        public bool InsertAt(char input, int position, out int testPosition, out MaskedTextResultHint resultHint)
        {
            return InsertAt(input.ToString(), position, out testPosition, out resultHint);
        }

        /// <summary>
        /// Attempts to insert the characters in the specified string in at the specified position in the test string.
        /// (Insert characters in the virtual string).
        /// Returns true on success, false otherwise.
        /// </summary>
        public bool InsertAt(string input, int position)
        {
            int dummyVar;
            MaskedTextResultHint dummyVar2;
            return InsertAt(input, position, out dummyVar, out dummyVar2);
        }

        /// <summary>
        /// Attempts to insert the characters in the specified string in at the specified position in the test string,
        /// shifting characters at upper positions (if any) to make room for the input.
        /// On exit the testPosition contains last position where the primary operation was actually performed if successful,
        /// otherwise the first position that made the test fail. This position is relative to the test string.
        /// The MaskedTextResultHint out param gives more information about the operation result.
        /// Returns true on success, false otherwise.
        /// </summary>
        public bool InsertAt(string input, int position, out int testPosition, out MaskedTextResultHint resultHint)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            if (position < 0 || position >= _testString.Length)
            {
                testPosition = position;
                resultHint = MaskedTextResultHint.PositionOutOfRange;
                return false;
                //throw new ArgumentOutOfRangeException("position");
            }

            return InsertAtInt(input, position, out testPosition, out resultHint, false);
        }

        /// <summary>
        /// Attempts to insert the characters in the specified string in at the specified position in the test string,
        /// shifting characters at upper positions (if any) to make room for the input.
        /// It performs the insertion if the testOnly parameter is false and the test passes.
        /// On exit the testPosition contains last position where the primary operation was actually performed if successful,
        /// otherwise the first position that made the test fail. This position is relative to the test string.
        /// The MaskedTextResultHint out param gives more information about the operation result.
        /// Returns true on success, false otherwise.
        /// </summary>
        private bool InsertAtInt(string input, int position, out int testPosition, out MaskedTextResultHint resultHint, bool testOnly)
        {
            Debug.Assert(input != null && position >= 0 && position < _testString.Length, "input param out of range.");

            if (input.Length == 0) // nothing to insert.
            {
                testPosition = position;
                resultHint = MaskedTextResultHint.NoEffect;
                return true;
            }

            // Test input string first. testPosition will containt the position of the last inserting character from the input.
            if (!TestString(input, position, out testPosition, out resultHint))
            {
                return false;
            }

            // Now check if we need to open room for the input characters (shift characters right) and if so test the shifting characters.

            int srcPos = FindEditPositionFrom(position, FORWARD);               // source position.
            bool shiftNeeded = FindAssignedEditPositionInRange(srcPos, testPosition, FORWARD) != INVALID_INDEX;
            int lastAssignedPos = LastAssignedPosition;

            if (shiftNeeded && (testPosition == _testString.Length - 1)) // no room for shifting.
            {
                resultHint = MaskedTextResultHint.UnavailableEditPosition;
                testPosition = _testString.Length;
                return false;
            }

            int dstPos = FindEditPositionFrom(testPosition + 1, FORWARD);  // destination position.

            if (shiftNeeded)
            {
                // Temp hint used not to overwrite the primary operation result hint (from TestString).
                MaskedTextResultHint tempHint = MaskedTextResultHint.Unknown;

                // Test shifting characters.
                while (true)
                {
                    if (dstPos == INVALID_INDEX)
                    {
                        resultHint = MaskedTextResultHint.UnavailableEditPosition;
                        testPosition = _testString.Length;
                        return false;
                    }

                    CharDescriptor chDex = _stringDescriptor[srcPos];

                    if (chDex.IsAssigned) // only test assigned positions.
                    {
                        if (!TestChar(_testString[srcPos], dstPos, out tempHint))
                        {
                            resultHint = tempHint;
                            testPosition = dstPos;
                            return false;
                        }
                    }

                    if (srcPos == lastAssignedPos) // all shifting positions tested?
                    {
                        break;
                    }

                    srcPos = FindEditPositionFrom(srcPos + 1, FORWARD);
                    dstPos = FindEditPositionFrom(dstPos + 1, FORWARD);
                }

                if (tempHint > resultHint)
                {
                    resultHint = tempHint;
                }
            }

            if (testOnly)
            {
                return true; // test done!
            }

            // Tests passed so we can go ahead and shift the existing characters (if needed) and insert the new ones.

            if (shiftNeeded)
            {
                while (srcPos >= position)
                {
                    CharDescriptor chDex = _stringDescriptor[srcPos];

                    if (chDex.IsAssigned)
                    {
                        SetChar(_testString[srcPos], dstPos);
                    }
                    else
                    {
                        ResetChar(dstPos);
                    }

                    dstPos = FindEditPositionFrom(dstPos - 1, BACKWARD);
                    srcPos = FindEditPositionFrom(srcPos - 1, BACKWARD);
                }
            }

            // Finally set the input characters.
            SetString(input, position);

            return true;
        }

        /// <summary>
        /// Helper function for testing char in ascii mode.
        /// </summary>
        private static bool IsAscii(char c)
        {
            //ASCII non-control chars ['!'-'/', '0'-'9', ':'-'@', 'A'-'Z', '['-'''', 'a'-'z', '{'-'~'] all consecutive.
            return (c >= '!' && c <= '~');
        }

        /// <summary>
        /// Helper function for alphanumeric char in ascii mode.
        /// </summary>
        private static bool IsAciiAlphanumeric(char c)
        {
            return (c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
        }

        /// <summary>
        /// Helper function for testing mask language alphanumeric identifiers.
        /// </summary>
        private static bool IsAlphanumeric(char c)
        {
            return char.IsLetter(c) || char.IsDigit(c);
        }

        /// <summary>
        /// Helper function for testing letter char in ascii mode.
        /// </summary>
        private static bool IsAsciiLetter(char c)
        {
            return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
        }

        /// <summary>
        /// Checks whether the specified position is available for assignment. Returns false if it is assigned
        /// or it is not editable, true otherwise.
        /// </summary>
        public bool IsAvailablePosition(int position)
        {
            if (position < 0 || position >= _testString.Length)
            {
                return false;
                //throw new ArgumentOutOfRangeException("position");
            }

            CharDescriptor chDex = _stringDescriptor[position];
            return IsEditPosition(chDex) && !chDex.IsAssigned;
        }

        /// <summary>
        /// Checks whether the specified position in the test string is editable.
        /// </summary>
        public bool IsEditPosition(int position)
        {
            if (position < 0 || position >= _testString.Length)
            {
                return false;
                //throw new ArgumentOutOfRangeException("position");
            }

            CharDescriptor chDex = _stringDescriptor[position];
            return IsEditPosition(chDex);
        }

        private static bool IsEditPosition(CharDescriptor charDescriptor)
        {
            return (charDescriptor.CharType == CharType.EditRequired || charDescriptor.CharType == CharType.EditOptional);
        }

        /// <summary>
        /// Checks whether the character in the specified position is a literal and the same as the specified character.
        /// </summary>
        private static bool IsLiteralPosition(CharDescriptor charDescriptor)
        {
            return (charDescriptor.CharType == CharType.Literal) || (charDescriptor.CharType == CharType.Separator);
        }

        /// <summary>
        /// Checks whether the specified character is valid as part of a mask or an input string.
        /// </summary>
        private static bool IsPrintableChar(char c)
        {
            return char.IsLetterOrDigit(c) || char.IsPunctuation(c) || char.IsSymbol(c) || (c == SPACE_CHAR);
        }

        /// <summary>
        /// Checks whether the specified character is a valid input char.
        /// </summary>
        public static bool IsValidInputChar(char c)
        {
            return IsPrintableChar(c);
        }

        /// <summary>
        /// Checks whether the specified character is a valid input char.
        /// </summary>
        public static bool IsValidMaskChar(char c)
        {
            return IsPrintableChar(c);
        }

        /// <summary>
        /// Checks whether the specified character is a valid password char.
        /// </summary>
        public static bool IsValidPasswordChar(char c)
        {
            return IsPrintableChar(c) || (c == '\0');  // null character means password reset.
        }

        /// <summary>
        /// Removes the last character from the formatted string. (Remove last character in virtual string).
        /// </summary>
        public bool Remove()
        {
            int dummyVar;
            MaskedTextResultHint dummyVar2;
            return Remove(out dummyVar, out dummyVar2);
        }

        /// <summary>
        /// Removes the last character from the formatted string. (Remove last character in virtual string).
        /// On exit the out param contains the position where the operation was actually performed.
        /// This position is relative to the test string.
        /// The MaskedTextResultHint out param gives more information about the operation result.
        /// Returns true on success, false otherwise.
        /// </summary>
        public bool Remove(out int testPosition, out MaskedTextResultHint resultHint)
        {
            int lastAssignedPos = LastAssignedPosition;

            if (lastAssignedPos == INVALID_INDEX)
            {
                testPosition = 0;
                resultHint = MaskedTextResultHint.NoEffect;
                return true; // nothing to remove.
            }

            ResetChar(lastAssignedPos);

            testPosition = lastAssignedPos;
            resultHint = MaskedTextResultHint.Success;

            return true;
        }

        /// <summary>
        /// Removes the character from the formatted string at the specified position and shifts characters
        /// left.
        /// True if character shifting is successful.
        /// </summary>
        public bool RemoveAt(int position)
        {
            return RemoveAt(position, position);
        }

        /// <summary>
        /// Removes all characters in edit position from in the test string at the specified start and end positions
        /// and shifts any remaining characters left. (Remove characters from the virtual string).
        /// Returns true on success, false otherwise.
        /// </summary>
        public bool RemoveAt(int startPosition, int endPosition)
        {
            int dummyVar;
            MaskedTextResultHint dummyVar2;
            return RemoveAt(startPosition, endPosition, out dummyVar, out dummyVar2);
        }

        /// <summary>
        /// Removes all characters in edit position from in the test string at the specified start and end positions
        /// and shifts any remaining characters left.
        /// On exit the testPosition contains last position where the primary operation was actually performed if successful,
        /// otherwise the first position that made the test fail. This position is relative to the test string.
        /// The MaskedTextResultHint out param gives more information about the operation result.
        /// Returns true on success, false otherwise.
        /// </summary>
        public bool RemoveAt(int startPosition, int endPosition, out int testPosition, out MaskedTextResultHint resultHint)
        {
            if (endPosition >= _testString.Length)
            {
                testPosition = endPosition;
                resultHint = MaskedTextResultHint.PositionOutOfRange;
                return false;
                //throw new ArgumentOutOfRangeException("endPosition");
            }

            if (startPosition < 0 || startPosition > endPosition)
            {
                testPosition = startPosition;
                resultHint = MaskedTextResultHint.PositionOutOfRange;
                return false;
                //throw new ArgumentOutOfRangeException("startPosition");
            }

            return RemoveAtInt(startPosition, endPosition, out testPosition, out resultHint, /*testOnly*/ false);
        }

        /// <summary>
        /// Removes all characters in edit position from in the test string at the specified start and end positions
        /// and shifts any remaining characters left.
        /// If testOnly parameter is set to false and the test passes it performs the operations on the characters.
        /// On exit the testPosition contains last position where the primary operation was actually performed if successful,
        /// otherwise the first position that made the test fail. This position is relative to the test string.
        /// The MaskedTextResultHint out param gives more information about the operation result.
        /// Returns true on success, false otherwise.
        /// </summary>
        private bool RemoveAtInt(int startPosition, int endPosition, out int testPosition, out MaskedTextResultHint resultHint, bool testOnly)
        {
            Debug.Assert(startPosition >= 0 && startPosition <= endPosition && endPosition < _testString.Length, "Out of range input value.");

            // Check if we need to shift characters left to occupied the positions left by the characters being removed.
            int lastAssignedPos = LastAssignedPosition;
            int dstPos = FindEditPositionInRange(startPosition, endPosition, FORWARD); // first edit position in range.

            resultHint = MaskedTextResultHint.NoEffect;

            if (dstPos == INVALID_INDEX || dstPos > lastAssignedPos) // nothing to remove.
            {
                testPosition = startPosition;
                return true;
            }

            testPosition = startPosition;    // On remove range, testPosition remains the same as startPosition.

            bool shiftNeeded = endPosition < lastAssignedPos; // last assigned position is upper.

            // if there are assigned characters to be removed (could be that the range doesn't have one, in such case we may be just
            // be shifting chars), the result hint is success, let's check.
            if (FindAssignedEditPositionInRange(startPosition, endPosition, FORWARD) != INVALID_INDEX)
            {
                resultHint = MaskedTextResultHint.Success;
            }

            if (shiftNeeded)
            {
                // Test shifting characters.

                int srcPos = FindEditPositionFrom(endPosition + 1, FORWARD);  // first position to shift left.
                int shiftStart = srcPos; // cache it here so we don't have to search for it later if needed.
                MaskedTextResultHint testHint;

                startPosition = dstPos; // actual start position.

                while (true)
                {
                    char srcCh = _testString[srcPos];
                    CharDescriptor chDex = _stringDescriptor[srcPos];

                    // if the shifting character is the prompt and it is at an unassigned position we don't need to test it.
                    if (srcCh != PromptChar || chDex.IsAssigned)
                    {
                        if (!TestChar(srcCh, dstPos, out testHint))
                        {
                            resultHint = testHint;
                            testPosition = dstPos; // failed position.
                            return false;
                        }
                    }

                    if (srcPos == lastAssignedPos)
                    {
                        break;
                    }

                    srcPos = FindEditPositionFrom(srcPos + 1, FORWARD);
                    dstPos = FindEditPositionFrom(dstPos + 1, FORWARD);
                }

                // shifting characters is a resultHint == sideEffect, update hint if no characters removed (which would be hint == success).
                if (MaskedTextResultHint.SideEffect > resultHint)
                {
                    resultHint = MaskedTextResultHint.SideEffect;
                }

                if (testOnly)
                {
                    return true; // test completed.
                }

                // test passed so shift characters.
                srcPos = shiftStart;
                dstPos = startPosition;

                while (true)
                {
                    char srcCh = _testString[srcPos];
                    CharDescriptor chDex = _stringDescriptor[srcPos];

                    // if the shifting character is the prompt and it is at an unassigned position we just reset the destination position.
                    if (srcCh == PromptChar && !chDex.IsAssigned)
                    {
                        ResetChar(dstPos);
                    }
                    else
                    {
                        SetChar(srcCh, dstPos);
                        ResetChar(srcPos);
                    }

                    if (srcPos == lastAssignedPos)
                    {
                        break;
                    }

                    srcPos = FindEditPositionFrom(srcPos + 1, FORWARD);
                    dstPos = FindEditPositionFrom(dstPos + 1, FORWARD);
                }

                // If shifting character are less than characters to remove in the range, we need to remove the remaining ones in the range;
                // update startPosition and ResetString belwo will take care of that.
                startPosition = dstPos + 1;
            }

            if (startPosition <= endPosition)
            {
                ResetString(startPosition, endPosition);
            }

            return true;
        }

        /// <summary>
        /// Replaces the first editable character in the test string from the specified position, with the specified
        /// character (Replace is performed in the virtual string), unless the character at the specified position
        /// is to be escaped.
        /// Returns true on success, false otherwise.
        /// </summary>
        public bool Replace(char input, int position)
        {
            int dummyVar;
            MaskedTextResultHint dummyVar2;
            return Replace(input, position, out dummyVar, out dummyVar2);
        }

        /// <summary>
        /// Replaces the first editable character in the test string from the specified position, with the specified
        /// character, unless the character at the specified position is to be escaped.
        /// On exit the testPosition contains last position where the primary operation was actually performed if successful,
        /// otherwise the first position that made the test fail. This position is relative to the test string.
        /// The MaskedTextResultHint out param gives more information about the operation result.
        /// Returns true on success, false otherwise.
        /// </summary>
        public bool Replace(char input, int position, out int testPosition, out MaskedTextResultHint resultHint)
        {
            if (position < 0 || position >= _testString.Length)
            {
                testPosition = position;
                resultHint = MaskedTextResultHint.PositionOutOfRange;
                return false;
                //throw new ArgumentOutOfRangeException("position");
            }

            testPosition = position;

            // If character is not to be escaped, we need to find the first edit position to test it in.
            if (!TestEscapeChar(input, testPosition))
            {
                testPosition = FindEditPositionFrom(testPosition, FORWARD);
            }

            if (testPosition == INVALID_INDEX)
            {
                resultHint = MaskedTextResultHint.UnavailableEditPosition;
                testPosition = position;
                return false;
            }

            if (!TestSetChar(input, testPosition, out resultHint))
            {
                return false;
            }

            return true;
        }


        /// <summary>
        /// Replaces the first editable character in the test string from the specified position, with the specified
        /// character and removes any remaining characters in the range unless the character at the specified position
        /// is to be escaped.
        /// If specified range covers more than one assigned edit character, shift-left is performed after replacing
        /// the first character. This is useful when in an edit box the user selects text and types a character to replace it.
        /// On exit the testPosition contains last position where the primary operation was actually performed if successful,
        /// otherwise the first position that made the test fail. This position is relative to the test string.
        /// The MaskedTextResultHint out param gives more information about the operation result.
        /// Returns true on success, false otherwise.
        /// </summary>
        public bool Replace(char input, int startPosition, int endPosition, out int testPosition, out MaskedTextResultHint resultHint)
        {
            if (endPosition >= _testString.Length)
            {
                testPosition = endPosition;
                resultHint = MaskedTextResultHint.PositionOutOfRange;
                return false;
                //throw new ArgumentOutOfRangeException("endPosition");
            }

            if (startPosition < 0 || startPosition > endPosition)
            {
                testPosition = startPosition;
                resultHint = MaskedTextResultHint.PositionOutOfRange;
                return false;
                //throw new ArgumentOutOfRangeException("startPosition");
            }

            if (startPosition == endPosition)
            {
                testPosition = startPosition;
                return TestSetChar(input, startPosition, out resultHint);
            }

            return Replace(input.ToString(), startPosition, endPosition, out testPosition, out resultHint);
        }

        /// <summary>
        /// Replaces the character at the first edit position from the one specified with the first character in the input;
        /// the rest of the characters in the input will be placed in the test string according to the InsertMode (insert/replace).
        /// (Replace is performed in the virtual text).
        /// Returns true on success, false otherwise.
        /// </summary>
        public bool Replace(string input, int position)
        {
            int dummyVar;
            MaskedTextResultHint dummyVar2;
            return Replace(input, position, out dummyVar, out dummyVar2);
        }

        /// <summary>
        /// Replaces the character at the first edit position from the one specified with the first character in the input;
        /// the rest of the characters in the input will be placed in the test string according to the InsertMode (insert/replace),
        /// shifting characters at upper positions (if any) to make room for the entire input.
        /// On exit the testPosition contains last position where the primary operation was actually performed if successful,
        /// otherwise the first position that made the test fail. This position is relative to the test string.
        /// The MaskedTextResultHint out param gives more information about the operation result.
        /// Returns true on success, false otherwise.
        /// </summary>
        public bool Replace(string input, int position, out int testPosition, out MaskedTextResultHint resultHint)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            if (position < 0 || position >= _testString.Length)
            {
                testPosition = position;
                resultHint = MaskedTextResultHint.PositionOutOfRange;
                return false;
                //throw new ArgumentOutOfRangeException("position");
            }

            if (input.Length == 0) // remove the character at position.
            {
                return RemoveAt(position, position, out testPosition, out resultHint);
            }

            // At this point, we are replacing characters with the ones in the input.

            if (!TestSetString(input, position, out testPosition, out resultHint))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Replaces the characters in the specified range with the characters in the input string and shifts
        /// characters appropriately (removing or inserting characters according to whether the input string is
        /// shorter or larger than the specified range.
        /// On exit the testPosition contains last position where the primary operation was actually performed if successful,
        /// otherwise the first position that made the test fail. This position is relative to the test string.
        /// The MaskedTextResultHint out param gives more information about the operation result.
        /// Returns true on success, false otherwise.
        /// </summary>
        public bool Replace(string input, int startPosition, int endPosition, out int testPosition, out MaskedTextResultHint resultHint)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            if (endPosition >= _testString.Length)
            {
                testPosition = endPosition;
                resultHint = MaskedTextResultHint.PositionOutOfRange;
                return false;
                //throw new ArgumentOutOfRangeException("endPosition");
            }

            if (startPosition < 0 || startPosition > endPosition)
            {
                testPosition = startPosition;
                resultHint = MaskedTextResultHint.PositionOutOfRange;
                return false;
                //throw new ArgumentOutOfRangeException("startPosition");
            }

            if (input.Length == 0) // remove character at position.
            {
                return RemoveAt(startPosition, endPosition, out testPosition, out resultHint);
            }

            // If replacing the entire text with a same-lenght text, we are just setting (not replacing) the test string to the new value;
            // in this case we just call SetString.
            // If the text length is different than the specified range we would need to remove or insert characters; there are three possible
            // cases as follows:
            // 1. The text length is the same as edit positions in the range (or no assigned chars): just replace the text, no additional operations needed.
            // 2. The text is shorter: replace the text in the text string and remove (range - text.Length) characters.
            // 3. The text is larger: replace range count characters and insert (range - text.Length) characters.

            // Test input string first and get the last test position to determine what to do.
            if (!TestString(input, startPosition, out testPosition, out resultHint))
            {
                return false;
            }

            if (AssignedEditPositionCount > 0)
            {
                // cache out params to preserve the ones from the primary operation (in case of success).
                int tempPos;
                MaskedTextResultHint tempHint;

                if (testPosition < endPosition) // Case 2. Replace + Remove.
                {
                    // Test removing remaining characters.
                    if (!RemoveAtInt(testPosition + 1, endPosition, out tempPos, out tempHint, /*testOnly*/ false))
                    {
                        testPosition = tempPos;
                        resultHint = tempHint;
                        return false;
                    }

                    // If current result hint is not success (no effect), and character shifting is actually performed, hint = side effect.
                    if (tempHint == MaskedTextResultHint.Success && resultHint != tempHint)
                    {
                        resultHint = MaskedTextResultHint.SideEffect;
                    }
                }
                else if (testPosition > endPosition) // Case 3. Replace + Insert.
                {
                    // Test shifting existing characters to make room for inserting part of the input.
                    int lastAssignedPos = LastAssignedPosition;
                    int dstPos = testPosition + 1;
                    int srcPos = endPosition + 1;

                    while (true)
                    {
                        srcPos = FindEditPositionFrom(srcPos, FORWARD);
                        dstPos = FindEditPositionFrom(dstPos, FORWARD);

                        if (dstPos == INVALID_INDEX)
                        {
                            testPosition = _testString.Length;
                            resultHint = MaskedTextResultHint.UnavailableEditPosition;
                            return false;
                        }

                        if (!TestChar(_testString[srcPos], dstPos, out tempHint))
                        {
                            testPosition = dstPos;
                            resultHint = tempHint;
                            return false;
                        }

                        // If current result hint is not success (no effect), and character shifting is actually performed, hint = success effect.
                        if (tempHint == MaskedTextResultHint.Success && resultHint != tempHint)
                        {
                            resultHint = MaskedTextResultHint.Success;
                        }

                        if (srcPos == lastAssignedPos)
                        {
                            break;
                        }

                        srcPos++;
                        dstPos++;
                    }

                    // shift test passed, now do it.

                    while (dstPos > testPosition)
                    {
                        SetChar(_testString[srcPos], dstPos);

                        srcPos = FindEditPositionFrom(srcPos - 1, BACKWARD);
                        dstPos = FindEditPositionFrom(dstPos - 1, BACKWARD);
                    }
                }
                // else endPosition == testPosition, this means replacing the entire text which is the same as Set().
            }

            // in all cases we need to replace the input.
            SetString(input, startPosition);
            return true;
        }

        /// <summary>
        /// Resets the test string character at the specified position.
        /// </summary>
        private void ResetChar(int testPosition)
        {
            CharDescriptor chDex = _stringDescriptor[testPosition];

            if (IsEditPosition(testPosition) && chDex.IsAssigned)
            {
                chDex.IsAssigned = false;
                _testString[testPosition] = _promptChar;
                AssignedEditPositionCount--;

                if (chDex.CharType == CharType.EditRequired)
                {
                    _requiredCharCount--;
                }

                Debug.Assert(AssignedEditPositionCount >= 0, "Invalid count of assigned chars.");
            }
        }

        /// <summary>
        /// Resets characters in the test string in the range defined by the specified positions.
        /// Position is relative to the test string and count is the number of edit characters to reset.
        /// </summary>
        private void ResetString(int startPosition, int endPosition)
        {
            Debug.Assert(startPosition >= 0 && endPosition >= 0 && endPosition >= startPosition && endPosition < _testString.Length, "position out of range.");

            startPosition = FindAssignedEditPositionFrom(startPosition, FORWARD);

            if (startPosition != INVALID_INDEX)
            {
                endPosition = FindAssignedEditPositionFrom(endPosition, BACKWARD);

                while (startPosition <= endPosition)
                {
                    startPosition = FindAssignedEditPositionFrom(startPosition, FORWARD);
                    ResetChar(startPosition);
                    startPosition++;
                }
            }
        }

        /// <summary>
        /// Sets the edit characters in the test string to the ones specified in the input string if all characters
        /// are valid.
        /// If passwordChar is assigned, it is rendered in the output string instead of the user-supplied values.
        /// </summary>
        public bool Set(string input)
        {
            int dummyVar;
            MaskedTextResultHint dummyVar2;

            return Set(input, out dummyVar, out dummyVar2);
        }

        /// <summary>
        /// Sets the edit characters in the test string to the ones specified in the input string if all characters
        /// are valid.
        /// On exit the testPosition contains last position where the primary operation was actually performed if successful,
        /// otherwise the first position that made the test fail. This position is relative to the test string.
        /// The MaskedTextResultHint out param gives more information about the operation result.
        /// If passwordChar is assigned, it is rendered in the output string instead of the user-supplied values.
        /// </summary>
        public bool Set(string input, out int testPosition, out MaskedTextResultHint resultHint)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            resultHint = MaskedTextResultHint.Unknown;
            testPosition = 0;

            if (input.Length == 0) // Clearing the input text.
            {
                Clear(out resultHint);
                return true;
            }

            if (!TestSetString(input, testPosition, out testPosition, out resultHint))
            {
                return false;
            }

            // Reset remaining characters (if any).
            int resetPos = FindAssignedEditPositionFrom(testPosition + 1, FORWARD);

            if (resetPos != INVALID_INDEX)
            {
                ResetString(resetPos, _testString.Length - 1);
            }

            return true;
        }

        /// <summary>
        /// Sets the character at the specified position in the test string to the specified value.
        /// Returns true on success, false otherwise.
        /// </summary>
        private void SetChar(char input, int position)
        {
            Debug.Assert(position >= 0 && position < _testString.Length, "Position out of range.");

            CharDescriptor chDex = _stringDescriptor[position];
            SetChar(input, position, chDex);
        }

        /// <summary>
        /// Sets the character at the specified position in the test string to the specified value.
        /// SetChar increments the number of assigned characters in the test string.
        /// </summary>
        private void SetChar(char input, int position, CharDescriptor charDescriptor)
        {
            Debug.Assert(position >= 0 && position < _testString.Length, "Position out of range.");
            Debug.Assert(charDescriptor != null, "Null character descriptor.");

            // Get the character info from the char descriptor table.
            CharDescriptor charDex = _stringDescriptor[position];

            // If input is space or prompt and is to be escaped, we are actually resetting the position if assigned,
            // this doesn't affect literal positions.
            if (TestEscapeChar(input, position, charDescriptor))
            {
                ResetChar(position);
                return;
            }

            Debug.Assert(!IsLiteralPosition(charDex), "Setting char in literal position.");

            if (char.IsLetter(input))
            {
                if (char.IsUpper(input))
                {
                    if (charDescriptor.CaseConversion == CaseConversion.ToLower)
                    {
                        input = Culture.TextInfo.ToLower(input);
                    }
                }
                else // Char.IsLower( input )
                {
                    if (charDescriptor.CaseConversion == CaseConversion.ToUpper)
                    {
                        input = Culture.TextInfo.ToUpper(input);
                    }
                }
            }

            _testString[position] = input;

            if (!charDescriptor.IsAssigned) // if position not counted for already (replace case) we do it (add case).
            {
                charDescriptor.IsAssigned = true;
                AssignedEditPositionCount++;

                if (charDescriptor.CharType == CharType.EditRequired)
                {
                    _requiredCharCount++;
                }
            }

            Debug.Assert(AssignedEditPositionCount <= EditPositionCount, "Invalid count of assigned chars.");
        }

        /// <summary>
        /// Sets the characters in the test string starting from the specified position, to the ones in the input
        /// string. It assumes there's enough edit positions to hold the characters in the input string (so call
        /// TestString before calling SetString).
        /// The position is relative to the test string.
        /// </summary>
        private void SetString(string input, int testPosition)
        {
            foreach (char ch in input)
            {
                // If character is not to be escaped, we need to find the first edit position to test it in.
                if (!TestEscapeChar(ch, testPosition))
                {
                    testPosition = FindEditPositionFrom(testPosition, FORWARD);
                }

                SetChar(ch, testPosition);
                testPosition++;
            }
        }

#if OUT
        // HERE FOR REF -
        // VSW#482024 (Consider adding a method to synchroniza input processing options with output formatting options to guarantee text round-tripping.

        /// <summary>
        /// Upadate the input processing options according to the output formatting options to guarantee
        /// text round-tripping, this is: the Text property does not change when setting it to the value
        /// obtain from it; for a control: when copying the text to the clipboard and then pasting it back
        /// while selecting the entire text.
        /// </summary>
        private void SynchronizeInputOptions(MaskedTextProvider mtp, bool includePrompt, bool includeLiterals)
        {
            // Input options are processed in the following order:
            // 1. Literals
            // 2. Prompts
            // 3. Spaces.

            // If literals not included in the output, it should not attempt to skip literals.
            mtp.SkipLiterals = includeLiterals;

            // MaskedTextProvider processes space as follows:
            // If it is an input character, it would be processed as such (no scaping).
            // If it is a literal, it would be processed first since literals are processed first.
            // If it is the same as the prompt, the value of IncludePrompt does not matter because the output
            // will be the same; this case should be treated as if IncludePrompt was true. Observe that
            // AllowPromptAsInput would not be affected because ResetOnPrompt has higher precedence.
            if (mtp.PromptChar == ' ')
            {
                includePrompt = true;
            }

            // If prompts are not present in the output, spaces will replace the prompts and will be process
            // by ResetOnSpace. Literals characters same as the prompt will be processed as literals first.
            // If prompts present positions will be rest.
            // Exception: PromptChar == space.
            mtp.ResetOnPrompt = includePrompt;

            // If no prompts in the output, the input may contain spaces replacing the prompt, reset on space
            // should be enabled. If prompts included in the output, spaces will be processed as literals.
            // Exception: PromptChar == space.
            mtp.ResetOnSpace = !includePrompt;
        }
#endif

        /// <summary>
        /// Tests whether the character at the specified position in the test string can be set to the specified
        /// value.
        /// The position specified is relative to the test string.
        /// The MaskedTextResultHint out param gives more information about the operation result.
        /// Returns true on success, false otherwise.
        /// </summary>
        private bool TestChar(char input, int position, out MaskedTextResultHint resultHint)
        {
            // boundary checks are performed in the public methods.
            Debug.Assert(position >= 0 && position < _testString.Length, "Position out of range.");

            if (!IsPrintableChar(input))
            {
                resultHint = MaskedTextResultHint.InvalidInput;
                return false;
            }

            // Get the character info from the char descriptor table.
            CharDescriptor charDex = _stringDescriptor[position];

            // Test if character should be escaped.
            // Test literals first - See VSW#454461. See commented-out method SynchronizeInputOptions()

            if (IsLiteralPosition(charDex))
            {
                if (SkipLiterals && (input == _testString[position]))
                {
                    resultHint = MaskedTextResultHint.CharacterEscaped;
                    return true;
                }

                resultHint = MaskedTextResultHint.NonEditPosition;
                return false;
            }

            if (input == _promptChar)
            {
                if (ResetOnPrompt)
                {
                    if (IsEditPosition(charDex) && charDex.IsAssigned) // Position would be reset.
                    {
                        resultHint = MaskedTextResultHint.SideEffect;
                    }
                    else
                    {
                        resultHint = MaskedTextResultHint.CharacterEscaped;
                    }
                    return true; // test does not fail for prompt when it is to be scaped.
                }

                // Escaping precedes AllowPromptAsInput. Now test for it.
                if (!AllowPromptAsInput)
                {
                    resultHint = MaskedTextResultHint.PromptCharNotAllowed;
                    return false;
                }
            }

            if (input == SPACE_CHAR && ResetOnSpace)
            {
                if (IsEditPosition(charDex) && charDex.IsAssigned) // Position would be reset.
                {
                    resultHint = MaskedTextResultHint.SideEffect;
                }
                else
                {
                    resultHint = MaskedTextResultHint.CharacterEscaped;
                }
                return true;
            }


            // Character was not escaped, now test it against the mask.

            // Test the character against the mask constraints. The switch tests false conditions.
            // Space char succeeds the test if the char type is optional.
            switch (Mask[charDex.MaskPosition])
            {
                case '#':   // digit or plus/minus sign optional.
                    if (!char.IsDigit(input) && (input != '-') && (input != '+') && input != SPACE_CHAR)
                    {
                        resultHint = MaskedTextResultHint.DigitExpected;
                        return false;
                    }
                    break;

                case '0':   // digit required.
                    if (!char.IsDigit(input))
                    {
                        resultHint = MaskedTextResultHint.DigitExpected;
                        return false;
                    }
                    break;

                case '9':   // digit optional.
                    if (!char.IsDigit(input) && input != SPACE_CHAR)
                    {
                        resultHint = MaskedTextResultHint.DigitExpected;
                        return false;
                    }
                    break;

                case 'L':   // letter required.
                    if (!char.IsLetter(input))
                    {
                        resultHint = MaskedTextResultHint.LetterExpected;
                        return false;
                    }
                    if (!IsAsciiLetter(input) && AsciiOnly)
                    {
                        resultHint = MaskedTextResultHint.AsciiCharacterExpected;
                        return false;
                    }
                    break;

                case '?':   // letter optional.
                    if (!char.IsLetter(input) && input != SPACE_CHAR)
                    {
                        resultHint = MaskedTextResultHint.LetterExpected;
                        return false;
                    }
                    if (!IsAsciiLetter(input) && AsciiOnly)
                    {
                        resultHint = MaskedTextResultHint.AsciiCharacterExpected;
                        return false;
                    }
                    break;

                case '&':   // any character required.
                    if (!IsAscii(input) && AsciiOnly)
                    {
                        resultHint = MaskedTextResultHint.AsciiCharacterExpected;
                        return false;
                    }
                    break;

                case 'C':   // any character optional.
                    if ((!IsAscii(input) && AsciiOnly) && input != SPACE_CHAR)
                    {
                        resultHint = MaskedTextResultHint.AsciiCharacterExpected;
                        return false;
                    }
                    break;

                case 'A':   // Alphanumeric required.
                    if (!IsAlphanumeric(input))
                    {
                        resultHint = MaskedTextResultHint.AlphanumericCharacterExpected;
                        return false;
                    }
                    if (!IsAciiAlphanumeric(input) && AsciiOnly)
                    {
                        resultHint = MaskedTextResultHint.AsciiCharacterExpected;
                        return false;
                    }
                    break;

                case 'a':   // Alphanumeric optional.
                    if (!IsAlphanumeric(input) && input != SPACE_CHAR)
                    {
                        resultHint = MaskedTextResultHint.AlphanumericCharacterExpected;
                        return false;
                    }
                    if (!IsAciiAlphanumeric(input) && AsciiOnly)
                    {
                        resultHint = MaskedTextResultHint.AsciiCharacterExpected;
                        return false;
                    }
                    break;

                default:
                    Debug.Fail("Invalid mask language character.");
                    break;
            }

            // Test passed.

            if (input == _testString[position] && charDex.IsAssigned)  // setting char would not make any difference
            {
                resultHint = MaskedTextResultHint.NoEffect;
            }
            else
            {
                resultHint = MaskedTextResultHint.Success;
            }

            return true;
        }

        /// <summary>
        /// Tests if the character at the specified position in the test string is to be escaped.
        /// Returns true on success, false otherwise.
        /// </summary>
        private bool TestEscapeChar(char input, int position)
        {
            CharDescriptor chDex = _stringDescriptor[position];
            return TestEscapeChar(input, position, chDex);
        }
        private bool TestEscapeChar(char input, int position, CharDescriptor charDex)
        {
            // Test literals first. See VSW#454461.
            // If the position holds a literal, it is escaped only if the input is the same as the literal independently on
            // the input value (space, prompt,...).
            if (IsLiteralPosition(charDex))
            {
                return SkipLiterals && input == _testString[position];
            }

            if ((ResetOnPrompt && (input == _promptChar)) || (ResetOnSpace && (input == SPACE_CHAR)))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tests if the character at the specified position in the test string can be set to the value specified,
        /// and sets the character to that value if the test is successful.
        /// The position specified is relative to the test string.
        /// The MaskedTextResultHint out param gives more information about the operation result.
        /// Returns true on success, false otherwise.
        /// </summary>
        private bool TestSetChar(char input, int position, out MaskedTextResultHint resultHint)
        {
            if (TestChar(input, position, out resultHint))
            {
                if (resultHint == MaskedTextResultHint.Success || resultHint == MaskedTextResultHint.SideEffect) // the character is not to be escaped.
                {
                    SetChar(input, position);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Test the characters in the specified string against the test string, starting from the specified position.
        /// If the test is successful, the characters in the test string are set appropriately.
        /// On exit the testPosition contains last position where the primary operation was actually performed if successful,
        /// otherwise the first position that made the test fail. This position is relative to the test string.
        /// The MaskedTextResultHint out param gives more information about the operation result.
        /// Returns true on success, false otherwise.
        /// </summary>
        private bool TestSetString(string input, int position, out int testPosition, out MaskedTextResultHint resultHint)
        {
            if (TestString(input, position, out testPosition, out resultHint))
            {
                SetString(input, position);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Test the characters in the specified string against the test string, starting from the specified position.
        /// On exit the testPosition contains last position where the primary operation was actually performed if successful,
        /// otherwise the first position that made the test fail. This position is relative to the test string.
        /// The successCount out param contains the number of characters that would be actually set (not escaped).
        /// The MaskedTextResultHint out param gives more information about the operation result.
        /// Returns true on success, false otherwise.
        /// </summary>
        private bool TestString(string input, int position, out int testPosition, out MaskedTextResultHint resultHint)
        {
            Debug.Assert(input != null, "null input.");
            Debug.Assert(position >= 0, "Position out of range.");

            resultHint = MaskedTextResultHint.Unknown;
            testPosition = position;

            if (input.Length == 0)
            {
                return true;
            }

            // If any char is actually accepted, then the hint is success, otherwise whatever the last character result is.
            // Need a temp variable for
            MaskedTextResultHint tempHint = resultHint;

            foreach (char ch in input)
            {
                if (testPosition >= _testString.Length)
                {
                    resultHint = MaskedTextResultHint.UnavailableEditPosition;
                    return false;
                }

                // If character is not to be escaped, we need to find an edit position to test it in.
                if (!TestEscapeChar(ch, testPosition))
                {
                    testPosition = FindEditPositionFrom(testPosition, FORWARD);

                    if (testPosition == INVALID_INDEX)
                    {
                        testPosition = _testString.Length;
                        resultHint = MaskedTextResultHint.UnavailableEditPosition;
                        return false;
                    }
                }

                // Test/Set char will scape prompt, space and literals if needed.
                if (!TestChar(ch, testPosition, out tempHint))
                {
                    resultHint = tempHint;
                    return false;
                }

                // Result precedence: Success, SideEffect, NoEffect, CharacterEscaped.
                if (tempHint > resultHint)
                {
                    resultHint = tempHint;
                }

                testPosition++;
            }

            testPosition--;

            return true;
        }

        /// <summary>
        /// Returns a formatted string based on the mask, honoring only the PasswordChar property. prompt character
        /// and literals are always included. This is the text to be shown in a control when it has the focus.
        /// </summary>
        public string ToDisplayString()
        {
            if (!IsPassword || AssignedEditPositionCount == 0) // just return the testString since it contains the formatted text.
            {
                return _testString.ToString();
            }

            // Copy test string and replace edit chars with password.
            StringBuilder st = new StringBuilder(_testString.Length);

            for (int position = 0; position < _testString.Length; position++)
            {
                CharDescriptor chDex = _stringDescriptor[position];
                st.Append(IsEditPosition(chDex) && chDex.IsAssigned ? _passwordChar : _testString[position]);
            }

            return st.ToString();
        }

        /// <summary>
        /// Returns a formatted string based on the mask, honoring  IncludePrompt and IncludeLiterals but ignoring
        /// PasswordChar.
        /// </summary>
        public override string ToString()
        {
            return ToString(/*ignorePwdChar*/ true, IncludePrompt, IncludeLiterals, 0, _testString.Length);
        }

        /// <summary>
        /// Returns a formatted string based on the mask, honoring the IncludePrompt and IncludeLiterals properties,
        /// and PasswordChar depending on the value of the 'ignorePasswordChar' parameter.
        /// </summary>
        public string ToString(bool ignorePasswordChar)
        {
            return ToString(ignorePasswordChar, IncludePrompt, IncludeLiterals, 0, _testString.Length);
        }

        /// <summary>
        /// Returns a formatted string starting at the specified position and for the specified number of character,
        /// based on the mask, honoring IncludePrompt and IncludeLiterals but ignoring PasswordChar.
        /// Parameters are relative to the test string.
        /// </summary>
        public string ToString(int startPosition, int length)
        {
            return ToString(/*ignorePwdChar*/ true, IncludePrompt, IncludeLiterals, startPosition, length);
        }

        /// <summary>
        /// Returns a formatted string starting at the specified position and for the specified number of character,
        /// based on the mask, honoring the IncludePrompt, IncludeLiterals properties and PasswordChar depending on
        /// the 'ignorePasswordChar' parameter.
        /// Parameters are relative to the test string.
        /// </summary>
        public string ToString(bool ignorePasswordChar, int startPosition, int length)
        {
            return ToString(ignorePasswordChar, IncludePrompt, IncludeLiterals, startPosition, length);
        }

        /// <summary>
        /// Returns a formatted string based on the mask, ignoring the PasswordChar and according to the includePrompt
        /// and includeLiterals parameters.
        /// </summary>
        public string ToString(bool includePrompt, bool includeLiterals)
        {
            return ToString( /*ignorePwdChar*/ true, includePrompt, includeLiterals, 0, _testString.Length);
        }

        /// <summary>
        /// Returns a formatted string starting at the specified position and for the specified number of character,
        /// based on the mask, according to the ignorePasswordChar, includePrompt and includeLiterals parameters.
        /// Parameters are relative to the test string.
        /// </summary>
        public string ToString(bool includePrompt, bool includeLiterals, int startPosition, int length)
        {
            return ToString( /*ignorePwdChar*/ true, includePrompt, includeLiterals, startPosition, length);
        }

        /// <summary>
        /// Returns a formatted string starting at the specified position and for the specified number of character,
        /// based on the mask, according to the ignorePasswordChar, includePrompt and includeLiterals parameters.
        /// Parameters are relative to the test string.
        /// </summary>
        public string ToString(bool ignorePasswordChar, bool includePrompt, bool includeLiterals, int startPosition, int length)
        {
            if (length <= 0)
            {
                return string.Empty;
            }

            if (startPosition < 0)
            {
                startPosition = 0;
                //throw new ArgumentOutOfRangeException("startPosition");
            }

            if (startPosition >= _testString.Length)
            {
                return string.Empty;
                //throw new ArgumentOutOfRangeException("startPosition");
            }

            int maxLength = _testString.Length - startPosition;

            if (length > maxLength)
            {
                length = maxLength;
                //throw new ArgumentOutOfRangeException("length");
            }

            if (!IsPassword || ignorePasswordChar) // we may not need to format the text...
            {
                if (includePrompt && includeLiterals)
                {
                    return _testString.ToString(startPosition, length); // testString contains just what the user is asking for.
                }
            }

            // Build the formatted string ...

            StringBuilder st = new StringBuilder();
            int lastPosition = startPosition + length - 1;

            if (!includePrompt)
            {
                // If prompt is not to be included we need to replace it with a space, but only for unassigned positions below
                // the last assigned position or last literal position if including literals, whichever is higher; upper unassigned
                // positions are not included in the resulting string.

                int lastLiteralPos = includeLiterals ? FindNonEditPositionInRange(startPosition, lastPosition, BACKWARD) : InvalidIndex;
                int lastAssignedPos = FindAssignedEditPositionInRange(lastLiteralPos == InvalidIndex ? startPosition : lastLiteralPos, lastPosition, BACKWARD);

                // If lastLiteralPos is in the range and lastAssignedPos is not InvalidIndex, the lastAssignedPos is the upper limit
                // we are looking for since it is searched in the range from lastLiteralPos and lastPosition. In any other case
                // lastLiteral would contain the upper position we are looking for or InvalidIndex, meaning all characters in the
                // range are to be ignored, in this case a null string should be returned.

                lastPosition = lastAssignedPos != InvalidIndex ? lastAssignedPos : lastLiteralPos;

                if (lastPosition == InvalidIndex)
                {
                    return string.Empty;
                }
            }

            for (int index = startPosition; index <= lastPosition; index++)
            {
                char ch = _testString[index];
                CharDescriptor chDex = _stringDescriptor[index];

                switch (chDex.CharType)
                {
                    case CharType.EditOptional:
                    case CharType.EditRequired:
                        if (chDex.IsAssigned)
                        {
                            if (IsPassword && !ignorePasswordChar)
                            {
                                st.Append(_passwordChar); // replace edit char with pwd char.
                                break;
                            }
                        }
                        else
                        {
                            if (!includePrompt)
                            {
                                st.Append(SPACE_CHAR); // replace prompt with space.
                                break;
                            }
                        }

                        goto default;

                    case CharType.Separator:
                    case CharType.Literal:
                        if (!includeLiterals)
                        {
                            break;  // exclude literals.
                        }
                        goto default;

                    default:
                        st.Append(ch);
                        break;
                }
            }

            return st.ToString();
        }

        /// <summary>
        /// Tests whether the specified character would be set successfully at the specified position.
        /// </summary>
        public bool VerifyChar(char input, int position, out MaskedTextResultHint hint)
        {
            hint = MaskedTextResultHint.NoEffect;

            if (position < 0 || position >= _testString.Length)
            {
                hint = MaskedTextResultHint.PositionOutOfRange;
                return false;
            }

            return TestChar(input, position, out hint);
        }

        /// <summary>
        /// Tests whether the specified character would be escaped at the specified position.
        /// </summary>
        public bool VerifyEscapeChar(char input, int position)
        {
            if (position < 0 || position >= _testString.Length)
            {
                return false;
            }

            return TestEscapeChar(input, position);
        }

        /// <summary>
        /// Verifies the test string against the mask.
        /// </summary>
        public bool VerifyString(string input)
        {
            int dummyVar;
            MaskedTextResultHint dummyVar2;
            return VerifyString(input, out dummyVar, out dummyVar2);
        }

        /// <summary>
        /// Verifies the test string against the mask.
        /// On exit the testPosition contains last position where the primary operation was actually performed if successful,
        /// otherwise the first position that made the test fail. This position is relative to the test string.
        /// The MaskedTextResultHint out param gives more information about the operation result.
        /// Returns true on success, false otherwise.
        /// </summary>
        public bool VerifyString(string input, out int testPosition, out MaskedTextResultHint resultHint)
        {
            testPosition = 0;

            if (input == null || input.Length == 0) // nothing to verify.
            {
                resultHint = MaskedTextResultHint.NoEffect;
                return true;
            }

            return TestString(input, 0, out testPosition, out resultHint);
        }
    }
}
