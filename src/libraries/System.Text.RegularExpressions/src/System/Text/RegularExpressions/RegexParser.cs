// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace System.Text.RegularExpressions
{
    /// <summary>Builds a tree of RegexNodes from a regular expression.</summary>
    internal ref struct RegexParser
    {
        // Implementation notes:
        // It would be nice to get rid of the comment modes, since the
        // ScanBlank() calls are just kind of duct-taped in.

        private const int EscapeMaxBufferSize = 256;
        private const int OptionStackDefaultSize = 32;
        private const int MaxValueDiv10 = int.MaxValue / 10;
        private const int MaxValueMod10 = int.MaxValue % 10;

        private RegexNode? _stack;
        private RegexNode? _group;
        private RegexNode? _alternation;
        private RegexNode? _concatenation;
        private RegexNode? _unit;

        private readonly string _pattern;
        private int _currentPos;
        private readonly CultureInfo _culture;

        private int _autocap;
        private int _capcount;
        private int _captop;
        private readonly int _capsize;

        private readonly Hashtable _caps;
        private Hashtable? _capnames;

        private int[]? _capnumlist;
        private List<string>? _capnamelist;

        private RegexOptions _options;
        // NOTE: _optionsStack is ValueListBuilder<int> to ensure that
        //       ArrayPool<int>.Shared, not ArrayPool<RegexOptions>.Shared,
        //       will be created if the stackalloc'd capacity is ever exceeded.
        private ValueListBuilder<int> _optionsStack;

        private bool _ignoreNextParen; // flag to skip capturing a parentheses group

        private RegexParser(string pattern, RegexOptions options, CultureInfo culture, Hashtable caps, int capsize, Hashtable? capnames, Span<int> optionSpan)
        {
            Debug.Assert(pattern != null, "Pattern must be set");
            Debug.Assert(culture != null, "Culture must be set");

            _pattern = pattern;
            _options = options;
            _culture = culture;
            _caps = caps;
            _capsize = capsize;
            _capnames = capnames;

            _optionsStack = new ValueListBuilder<int>(optionSpan);
            _stack = default;
            _group = default;
            _alternation = default;
            _concatenation = default;
            _unit = default;
            _currentPos = 0;
            _autocap = default;
            _capcount = default;
            _captop = default;
            _capnumlist = default;
            _capnamelist = default;
            _ignoreNextParen = false;
        }

        private RegexParser(string pattern, RegexOptions options, CultureInfo culture, Span<int> optionSpan)
            : this(pattern, options, culture, new Hashtable(), default, null, optionSpan)
        {
        }

        public static RegexTree Parse(string pattern, RegexOptions options, CultureInfo culture)
        {
            var parser = new RegexParser(pattern, options, culture, stackalloc int[OptionStackDefaultSize]);

            parser.CountCaptures();
            parser.Reset(options);
            RegexNode root = parser.ScanRegex();
            int minRequiredLength = root.ComputeMinLength();
            string[]? capnamelist = parser._capnamelist?.ToArray();
            var tree = new RegexTree(root, parser._caps, parser._capnumlist!, parser._captop, parser._capnames!, capnamelist!, options, minRequiredLength);
            parser.Dispose();

            return tree;
        }

        /// <summary>
        /// This static call constructs a flat concatenation node given a replacement pattern.
        /// </summary>
        public static RegexReplacement ParseReplacement(string pattern, RegexOptions options, Hashtable caps, int capsize, Hashtable capnames)
        {
            CultureInfo culture = (options & RegexOptions.CultureInvariant) != 0 ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture;
            var parser = new RegexParser(pattern, options, culture, caps, capsize, capnames, stackalloc int[OptionStackDefaultSize]);

            RegexNode root = parser.ScanReplacement();
            var regexReplacement = new RegexReplacement(pattern, root, caps);
            parser.Dispose();

            return regexReplacement;
        }

        /// <summary>
        /// Escapes all metacharacters (including |,(,),[,{,|,^,$,*,+,?,\, spaces and #)
        /// </summary>
        public static string Escape(string input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                if (IsMetachar(input[i]))
                {
                    return EscapeImpl(input, i);
                }
            }

            return input;
        }

        private static string EscapeImpl(string input, int i)
        {
            // For small inputs we allocate on the stack. In most cases a buffer three
            // times larger the original string should be sufficient as usually not all
            // characters need to be encoded.
            // For larger string we rent the input string's length plus a fixed
            // conservative amount of chars from the ArrayPool.
            ValueStringBuilder vsb = input.Length <= (EscapeMaxBufferSize / 3) ?
                new ValueStringBuilder(stackalloc char[EscapeMaxBufferSize]) :
                new ValueStringBuilder(input.Length + 200);

            char ch = input[i];
            vsb.Append(input.AsSpan(0, i));

            do
            {
                vsb.Append('\\');
                switch (ch)
                {
                    case '\n':
                        ch = 'n';
                        break;
                    case '\r':
                        ch = 'r';
                        break;
                    case '\t':
                        ch = 't';
                        break;
                    case '\f':
                        ch = 'f';
                        break;
                }

                vsb.Append(ch);
                i++;
                int lastpos = i;

                while (i < input.Length)
                {
                    ch = input[i];
                    if (IsMetachar(ch))
                    {
                        break;
                    }

                    i++;
                }

                vsb.Append(input.AsSpan(lastpos, i - lastpos));
            } while (i < input.Length);

            return vsb.ToString();
        }

        /// <summary>
        /// Unescapes all metacharacters (including (,),[,],{,},|,^,$,*,+,?,\, spaces and #)
        /// </summary>
        public static string Unescape(string input)
        {
            int i = input.IndexOf('\\');
            return i >= 0 ?
                UnescapeImpl(input, i) :
                input;
        }

        private static string UnescapeImpl(string input, int i)
        {
            var parser = new RegexParser(input, RegexOptions.None, CultureInfo.InvariantCulture, stackalloc int[OptionStackDefaultSize]);

            // In the worst case the escaped string has the same length.
            // For small inputs we use stack allocation.
            ValueStringBuilder vsb = input.Length <= EscapeMaxBufferSize ?
                new ValueStringBuilder(stackalloc char[EscapeMaxBufferSize]) :
                new ValueStringBuilder(input.Length);

            vsb.Append(input.AsSpan(0, i));
            do
            {
                i++;
                parser.Textto(i);
                if (i < input.Length)
                {
                    vsb.Append(parser.ScanCharEscape());
                }

                i = parser.Textpos();
                int lastpos = i;
                while (i < input.Length && input[i] != '\\')
                {
                    i++;
                }

                vsb.Append(input.AsSpan(lastpos, i - lastpos));
            } while (i < input.Length);

            parser.Dispose();

            return vsb.ToString();
        }

        /// <summary>
        /// Resets parsing to the beginning of the pattern.
        /// </summary>
        private void Reset(RegexOptions options)
        {
            _currentPos = 0;
            _autocap = 1;
            _ignoreNextParen = false;
            _optionsStack.Length = 0;
            _options = options;
            _stack = null;
        }

        public void Dispose()
        {
            _optionsStack.Dispose();
        }

        /*
         * The main parsing function.
         */

        private RegexNode ScanRegex()
        {
            char ch = '@'; // nonspecial ch, means at beginning
            bool isQuantifier = false;

            StartGroup(new RegexNode(RegexNode.Capture, _options, 0, -1));

            while (CharsRight() > 0)
            {
                bool wasPrevQuantifier = isQuantifier;
                isQuantifier = false;

                ScanBlank();

                int startpos = Textpos();

                // move past all of the normal characters.  We'll stop when we hit some kind of control character,
                // or if IgnorePatternWhiteSpace is on, we'll stop when we see some whitespace.
                if (UseOptionX())
                {
                    while (CharsRight() > 0 && (!IsStopperX(ch = RightChar()) || (ch == '{' && !IsTrueQuantifier())))
                        MoveRight();
                }
                else
                {
                    while (CharsRight() > 0 && (!IsSpecial(ch = RightChar()) || (ch == '{' && !IsTrueQuantifier())))
                        MoveRight();
                }

                int endpos = Textpos();

                ScanBlank();

                if (CharsRight() == 0)
                {
                    ch = '!'; // nonspecial, means at end
                }
                else if (IsSpecial(ch = RightChar()))
                {
                    isQuantifier = IsQuantifier(ch);
                    MoveRight();
                }
                else
                {
                    ch = ' '; // nonspecial, means at ordinary char
                }

                if (startpos < endpos)
                {
                    int cchUnquantified = endpos - startpos - (isQuantifier ? 1 : 0);

                    wasPrevQuantifier = false;

                    if (cchUnquantified > 0)
                    {
                        AddConcatenate(startpos, cchUnquantified, false);
                    }

                    if (isQuantifier)
                    {
                        AddUnitOne(CharAt(endpos - 1));
                    }
                }

                switch (ch)
                {
                    case '!':
                        goto BreakOuterScan;

                    case ' ':
                        goto ContinueOuterScan;

                    case '[':
                        AddUnitSet(ScanCharClass(UseOptionI(), scanOnly: false)!.ToStringClass());
                        break;

                    case '(':
                        {
                            RegexNode? grouper;

                            PushOptions();

                            if (null == (grouper = ScanGroupOpen()))
                            {
                                PopKeepOptions();
                            }
                            else
                            {
                                PushGroup();
                                StartGroup(grouper);
                            }
                        }
                        continue;

                    case '|':
                        AddAlternate();
                        goto ContinueOuterScan;

                    case ')':
                        if (EmptyStack())
                        {
                            throw MakeException(RegexParseError.InsufficientOpeningParentheses, SR.InsufficientOpeningParentheses);
                        }

                        AddGroup();
                        PopGroup();
                        PopOptions();

                        if (Unit() == null)
                        {
                            goto ContinueOuterScan;
                        }
                        break;

                    case '\\':
                        if (CharsRight() == 0)
                        {
                            throw MakeException(RegexParseError.UnescapedEndingBackslash, SR.UnescapedEndingBackslash);
                        }

                        AddUnitNode(ScanBackslash(scanOnly: false)!);
                        break;

                    case '^':
                        AddUnitType(UseOptionM() ? RegexNode.Bol : RegexNode.Beginning);
                        break;

                    case '$':
                        AddUnitType(UseOptionM() ? RegexNode.Eol : RegexNode.EndZ);
                        break;

                    case '.':
                        if (UseOptionS())
                        {
                            AddUnitSet(RegexCharClass.AnyClass);
                        }
                        else
                        {
                            AddUnitNotone('\n');
                        }
                        break;

                    case '{':
                    case '*':
                    case '+':
                    case '?':
                        if (Unit() == null)
                        {
                            throw wasPrevQuantifier ?
                                MakeException(RegexParseError.NestedQuantifiersNotParenthesized, SR.Format(SR.NestedQuantifiersNotParenthesized, ch)) :
                                MakeException(RegexParseError.QuantifierAfterNothing, SR.QuantifierAfterNothing);
                        }
                        MoveLeft();
                        break;

                    default:
                        throw new InvalidOperationException(SR.InternalError_ScanRegex);
                }

                ScanBlank();

                if (CharsRight() == 0 || !(isQuantifier = IsTrueQuantifier()))
                {
                    AddConcatenate();
                    goto ContinueOuterScan;
                }

                ch = RightCharMoveRight();

                // Handle quantifiers
                while (Unit() != null)
                {
                    int min;
                    int max;

                    switch (ch)
                    {
                        case '*':
                            min = 0;
                            max = int.MaxValue;
                            break;

                        case '?':
                            min = 0;
                            max = 1;
                            break;

                        case '+':
                            min = 1;
                            max = int.MaxValue;
                            break;

                        case '{':
                            {
                                startpos = Textpos();
                                max = min = ScanDecimal();
                                if (startpos < Textpos())
                                {
                                    if (CharsRight() > 0 && RightChar() == ',')
                                    {
                                        MoveRight();
                                        if (CharsRight() == 0 || RightChar() == '}')
                                            max = int.MaxValue;
                                        else
                                            max = ScanDecimal();
                                    }
                                }

                                if (startpos == Textpos() || CharsRight() == 0 || RightCharMoveRight() != '}')
                                {
                                    AddConcatenate();
                                    Textto(startpos - 1);
                                    goto ContinueOuterScan;
                                }
                            }

                            break;

                        default:
                            throw new InvalidOperationException(SR.InternalError_ScanRegex);
                    }

                    ScanBlank();

                    bool lazy = false;
                    if (CharsRight() != 0 && RightChar() == '?')
                    {
                        MoveRight();
                        lazy = true;
                    }

                    if (min > max)
                    {
                        throw MakeException(RegexParseError.ReversedQuantifierRange, SR.ReversedQuantifierRange);
                    }

                    AddConcatenate(lazy, min, max);
                }

            ContinueOuterScan:
                ;
            }

        BreakOuterScan:
            ;

            if (!EmptyStack())
            {
                throw MakeException(RegexParseError.InsufficientClosingParentheses, SR.InsufficientClosingParentheses);
            }

            AddGroup();

            return Unit()!.FinalOptimize();
        }

        /*
         * Simple parsing for replacement patterns
         */
        private RegexNode ScanReplacement()
        {
            _concatenation = new RegexNode(RegexNode.Concatenate, _options);

            while (true)
            {
                int c = CharsRight();
                if (c == 0)
                {
                    break;
                }

                int startpos = Textpos();

                while (c > 0 && RightChar() != '$')
                {
                    MoveRight();
                    c--;
                }

                AddConcatenate(startpos, Textpos() - startpos, true);

                if (c > 0)
                {
                    if (RightCharMoveRight() == '$')
                    {
                        AddUnitNode(ScanDollar());
                    }

                    AddConcatenate();
                }
            }

            return _concatenation;
        }

        /*
         * Scans contents of [] (not including []'s), and converts to a
         * RegexCharClass.
         */
        private RegexCharClass? ScanCharClass(bool caseInsensitive, bool scanOnly)
        {
            char ch = '\0';
            char chPrev = '\0';
            bool inRange = false;
            bool firstChar = true;
            bool closed = false;

            RegexCharClass? charClass = scanOnly ? null : new RegexCharClass();

            if (CharsRight() > 0 && RightChar() == '^')
            {
                MoveRight();
                if (!scanOnly)
                {
                    charClass!.Negate = true;
                }
                if ((_options & RegexOptions.ECMAScript) != 0 && CharAt(_currentPos) == ']')
                {
                    firstChar = false;
                }
            }

            for (; CharsRight() > 0; firstChar = false)
            {
                bool translatedChar = false;
                ch = RightCharMoveRight();
                if (ch == ']')
                {
                    if (!firstChar)
                    {
                        closed = true;
                        break;
                    }
                }
                else if (ch == '\\' && CharsRight() > 0)
                {
                    switch (ch = RightCharMoveRight())
                    {
                        case 'D':
                        case 'd':
                            if (!scanOnly)
                            {
                                if (inRange)
                                {
                                    throw MakeException(RegexParseError.ShorthandClassInCharacterRange, SR.Format(SR.ShorthandClassInCharacterRange, ch));
                                }
                                charClass!.AddDigit(UseOptionE(), ch == 'D', _pattern, _currentPos);
                            }
                            continue;

                        case 'S':
                        case 's':
                            if (!scanOnly)
                            {
                                if (inRange)
                                {
                                    throw MakeException(RegexParseError.ShorthandClassInCharacterRange, SR.Format(SR.ShorthandClassInCharacterRange, ch));
                                }
                                charClass!.AddSpace(UseOptionE(), ch == 'S');
                            }
                            continue;

                        case 'W':
                        case 'w':
                            if (!scanOnly)
                            {
                                if (inRange)
                                {
                                    throw MakeException(RegexParseError.ShorthandClassInCharacterRange, SR.Format(SR.ShorthandClassInCharacterRange, ch));
                                }

                                charClass!.AddWord(UseOptionE(), ch == 'W');
                            }
                            continue;

                        case 'p':
                        case 'P':
                            if (!scanOnly)
                            {
                                if (inRange)
                                {
                                    throw MakeException(RegexParseError.ShorthandClassInCharacterRange, SR.Format(SR.ShorthandClassInCharacterRange, ch));
                                }

                                charClass!.AddCategoryFromName(ParseProperty(), ch != 'p', caseInsensitive, _pattern, _currentPos);
                            }
                            else
                            {
                                ParseProperty();
                            }
                            continue;

                        case '-':
                            if (!scanOnly)
                            {
                                if (inRange)
                                {
                                    if (chPrev > ch)
                                    {
                                        throw MakeException(RegexParseError.ReversedCharacterRange, SR.ReversedCharacterRange);
                                    }

                                    charClass!.AddRange(chPrev, ch);
                                    inRange = false;
                                    chPrev = '\0';
                                }
                                else
                                {
                                    charClass!.AddRange(ch, ch);
                                }
                            }
                            continue;

                        default:
                            MoveLeft();
                            ch = ScanCharEscape(); // non-literal character
                            translatedChar = true;
                            break; // this break will only break out of the switch
                    }
                }
                else if (ch == '[')
                {
                    // This is code for Posix style properties - [:Ll:] or [:IsTibetan:].
                    // It currently doesn't do anything other than skip the whole thing!
                    if (CharsRight() > 0 && RightChar() == ':' && !inRange)
                    {
                        int savePos = Textpos();

                        MoveRight();
                        if (CharsRight() < 2 || RightCharMoveRight() != ':' || RightCharMoveRight() != ']')
                        {
                            Textto(savePos);
                        }
                    }
                }

                if (inRange)
                {
                    inRange = false;
                    if (!scanOnly)
                    {
                        if (ch == '[' && !translatedChar && !firstChar)
                        {
                            // We thought we were in a range, but we're actually starting a subtraction.
                            // In that case, we'll add chPrev to our char class, skip the opening [, and
                            // scan the new character class recursively.
                            charClass!.AddChar(chPrev);
                            charClass.AddSubtraction(ScanCharClass(caseInsensitive, scanOnly)!);

                            if (CharsRight() > 0 && RightChar() != ']')
                            {
                                throw MakeException(RegexParseError.ExclusionGroupNotLast, SR.ExclusionGroupNotLast);
                            }
                        }
                        else
                        {
                            // a regular range, like a-z
                            if (chPrev > ch)
                            {
                                throw MakeException(RegexParseError.ReversedCharacterRange, SR.ReversedCharacterRange);
                            }
                            charClass!.AddRange(chPrev, ch);
                        }
                    }
                }
                else if (CharsRight() >= 2 && RightChar() == '-' && RightChar(1) != ']')
                {
                    // this could be the start of a range
                    chPrev = ch;
                    inRange = true;
                    MoveRight();
                }
                else if (CharsRight() >= 1 && ch == '-' && !translatedChar && RightChar() == '[' && !firstChar)
                {
                    // we aren't in a range, and now there is a subtraction.  Usually this happens
                    // only when a subtraction follows a range, like [a-z-[b]]
                    if (!scanOnly)
                    {
                        MoveRight(1);
                        charClass!.AddSubtraction(ScanCharClass(caseInsensitive, scanOnly)!);

                        if (CharsRight() > 0 && RightChar() != ']')
                        {
                            throw MakeException(RegexParseError.ExclusionGroupNotLast, SR.ExclusionGroupNotLast);
                        }
                    }
                    else
                    {
                        MoveRight(1);
                        ScanCharClass(caseInsensitive, scanOnly);
                    }
                }
                else
                {
                    if (!scanOnly)
                    {
                        charClass!.AddRange(ch, ch);
                    }
                }
            }

            if (!closed)
            {
                throw MakeException(RegexParseError.UnterminatedBracket, SR.UnterminatedBracket);
            }

            if (!scanOnly && caseInsensitive)
            {
                charClass!.AddLowercase(_culture);
            }

            return charClass;
        }

        /*
         * Scans chars following a '(' (not counting the '('), and returns
         * a RegexNode for the type of group scanned, or null if the group
         * simply changed options (?cimsx-cimsx) or was a comment (#...).
         */
        private RegexNode? ScanGroupOpen()
        {
            // just return a RegexNode if we have:
            // 1. "(" followed by nothing
            // 2. "(x" where x != ?
            // 3. "(?)"
            if (CharsRight() == 0 || RightChar() != '?' || (RightChar() == '?' && CharsRight() > 1 && RightChar(1) == ')'))
            {
                if (UseOptionN() || _ignoreNextParen)
                {
                    _ignoreNextParen = false;
                    return new RegexNode(RegexNode.Group, _options);
                }
                else
                {
                    return new RegexNode(RegexNode.Capture, _options, _autocap++, -1);
                }
            }

            MoveRight();

            while (true)
            {
                if (CharsRight() == 0)
                {
                    break;
                }

                int nodeType;
                char close = '>';
                char ch = RightCharMoveRight();
                switch (ch)
                {
                    case ':':
                        // noncapturing group
                        nodeType = RegexNode.Group;
                        break;

                    case '=':
                        // lookahead assertion
                        _options &= ~RegexOptions.RightToLeft;
                        nodeType = RegexNode.Require;
                        break;

                    case '!':
                        // negative lookahead assertion
                        _options &= ~RegexOptions.RightToLeft;
                        nodeType = RegexNode.Prevent;
                        break;

                    case '>':
                        // atomic subexpression
                        nodeType = RegexNode.Atomic;
                        break;

                    case '\'':
                        close = '\'';
                        goto case '<'; // fallthrough

                    case '<':
                        if (CharsRight() == 0)
                        {
                            goto BreakRecognize;
                        }

                        switch (ch = RightCharMoveRight())
                        {
                            case '=':
                                if (close == '\'')
                                {
                                    goto BreakRecognize;
                                }

                                // lookbehind assertion
                                _options |= RegexOptions.RightToLeft;
                                nodeType = RegexNode.Require;
                                break;

                            case '!':
                                if (close == '\'')
                                {
                                    goto BreakRecognize;
                                }

                                // negative lookbehind assertion
                                _options |= RegexOptions.RightToLeft;
                                nodeType = RegexNode.Prevent;
                                break;

                            default:
                                MoveLeft();
                                int capnum = -1;
                                int uncapnum = -1;
                                bool proceed = false;

                                // grab part before -

                                if ((uint)(ch - '0') <= 9)
                                {
                                    capnum = ScanDecimal();

                                    if (!IsCaptureSlot(capnum))
                                    {
                                        capnum = -1;
                                    }

                                    // check if we have bogus characters after the number
                                    if (CharsRight() > 0 && !(RightChar() == close || RightChar() == '-'))
                                    {
                                        throw MakeException(RegexParseError.CaptureGroupNameInvalid, SR.CaptureGroupNameInvalid);
                                    }

                                    if (capnum == 0)
                                    {
                                        throw MakeException(RegexParseError.CaptureGroupOfZero, SR.CaptureGroupOfZero);
                                    }
                                }
                                else if (RegexCharClass.IsWordChar(ch))
                                {
                                    string capname = ScanCapname();

                                    if (IsCaptureName(capname))
                                    {
                                        capnum = CaptureSlotFromName(capname);
                                    }

                                    // check if we have bogus character after the name
                                    if (CharsRight() > 0 && !(RightChar() == close || RightChar() == '-'))
                                    {
                                        throw MakeException(RegexParseError.CaptureGroupNameInvalid, SR.CaptureGroupNameInvalid);
                                    }
                                }
                                else if (ch == '-')
                                {
                                    proceed = true;
                                }
                                else
                                {
                                    // bad group name - starts with something other than a word character and isn't a number
                                    throw MakeException(RegexParseError.CaptureGroupNameInvalid, SR.CaptureGroupNameInvalid);
                                }

                                // grab part after - if any

                                if ((capnum != -1 || proceed == true) && CharsRight() > 1 && RightChar() == '-')
                                {
                                    MoveRight();
                                    ch = RightChar();

                                    if ((uint)(ch - '0') <= 9)
                                    {
                                        uncapnum = ScanDecimal();

                                        if (!IsCaptureSlot(uncapnum))
                                        {
                                            throw MakeException(RegexParseError.UndefinedNumberedReference, SR.Format(SR.UndefinedNumberedReference, uncapnum));
                                        }

                                        // check if we have bogus characters after the number
                                        if (CharsRight() > 0 && RightChar() != close)
                                        {
                                            throw MakeException(RegexParseError.CaptureGroupNameInvalid, SR.CaptureGroupNameInvalid);
                                        }
                                    }
                                    else if (RegexCharClass.IsWordChar(ch))
                                    {
                                        string uncapname = ScanCapname();

                                        if (IsCaptureName(uncapname))
                                        {
                                            uncapnum = CaptureSlotFromName(uncapname);
                                        }
                                        else
                                        {
                                            throw MakeException(RegexParseError.UndefinedNamedReference, SR.Format(SR.UndefinedNamedReference, uncapname));
                                        }

                                        // check if we have bogus character after the name
                                        if (CharsRight() > 0 && RightChar() != close)
                                        {
                                            throw MakeException(RegexParseError.CaptureGroupNameInvalid, SR.CaptureGroupNameInvalid);
                                        }
                                    }
                                    else
                                    {
                                        // bad group name - starts with something other than a word character and isn't a number
                                        throw MakeException(RegexParseError.CaptureGroupNameInvalid, SR.CaptureGroupNameInvalid);
                                    }
                                }

                                // actually make the node

                                if ((capnum != -1 || uncapnum != -1) && CharsRight() > 0 && RightCharMoveRight() == close)
                                {
                                    return new RegexNode(RegexNode.Capture, _options, capnum, uncapnum);
                                }
                                goto BreakRecognize;
                        }
                        break;

                    case '(':
                        // alternation construct (?(...) | )

                        int parenPos = Textpos();
                        if (CharsRight() > 0)
                        {
                            ch = RightChar();

                            // check if the alternation condition is a backref
                            if (ch >= '0' && ch <= '9')
                            {
                                int capnum = ScanDecimal();
                                if (CharsRight() > 0 && RightCharMoveRight() == ')')
                                {
                                    if (IsCaptureSlot(capnum))
                                    {
                                        return new RegexNode(RegexNode.Testref, _options, capnum);
                                    }

                                    throw MakeException(RegexParseError.AlternationHasUndefinedReference, SR.Format(SR.AlternationHasUndefinedReference, capnum.ToString()));
                                }

                                throw MakeException(RegexParseError.AlternationHasMalformedReference, SR.Format(SR.AlternationHasMalformedReference, capnum.ToString()));
                            }
                            else if (RegexCharClass.IsWordChar(ch))
                            {
                                string capname = ScanCapname();

                                if (IsCaptureName(capname) && CharsRight() > 0 && RightCharMoveRight() == ')')
                                {
                                    return new RegexNode(RegexNode.Testref, _options, CaptureSlotFromName(capname));
                                }
                            }
                        }
                        // not a backref
                        nodeType = RegexNode.Testgroup;
                        Textto(parenPos - 1);       // jump to the start of the parentheses
                        _ignoreNextParen = true;    // but make sure we don't try to capture the insides

                        int charsRight = CharsRight();
                        if (charsRight >= 3 && RightChar(1) == '?')
                        {
                            char rightchar2 = RightChar(2);

                            // disallow comments in the condition
                            if (rightchar2 == '#')
                            {
                                throw MakeException(RegexParseError.AlternationHasComment, SR.AlternationHasComment);
                            }

                            // disallow named capture group (?<..>..) in the condition
                            if (rightchar2 == '\'')
                            {
                                throw MakeException(RegexParseError.AlternationHasNamedCapture, SR.AlternationHasNamedCapture);
                            }

                            if (charsRight >= 4 && rightchar2 == '<' && RightChar(3) != '!' && RightChar(3) != '=')
                            {
                                throw MakeException(RegexParseError.AlternationHasNamedCapture, SR.AlternationHasNamedCapture);
                            }
                        }

                        break;

                    default:
                        MoveLeft();

                        nodeType = RegexNode.Group;
                        // Disallow options in the children of a testgroup node
                        if (_group!.Type != RegexNode.Testgroup)
                        {
                            ScanOptions();
                        }

                        if (CharsRight() == 0)
                        {
                            goto BreakRecognize;
                        }

                        if ((ch = RightCharMoveRight()) == ')')
                        {
                            return null;
                        }

                        if (ch != ':')
                        {
                            goto BreakRecognize;
                        }
                        break;
                }

                return new RegexNode(nodeType, _options);
            }

        BreakRecognize:
            ;
            // break Recognize comes here

            throw MakeException(RegexParseError.InvalidGroupingConstruct, SR.InvalidGroupingConstruct);
        }

        /*
         * Scans whitespace or x-mode comments.
         */
        private void ScanBlank()
        {
            if (UseOptionX())
            {
                while (true)
                {
                    while (CharsRight() > 0 && IsSpace(RightChar()))
                    {
                        MoveRight();
                    }

                    if (CharsRight() == 0)
                    {
                        break;
                    }

                    if (RightChar() == '#')
                    {
                        while (CharsRight() > 0 && RightChar() != '\n')
                        {
                            MoveRight();
                        }
                    }
                    else if (CharsRight() >= 3 && RightChar(2) == '#' && RightChar(1) == '?' && RightChar() == '(')
                    {
                        while (CharsRight() > 0 && RightChar() != ')')
                        {
                            MoveRight();
                        }

                        if (CharsRight() == 0)
                        {
                            throw MakeException(RegexParseError.UnterminatedComment, SR.UnterminatedComment);
                        }

                        MoveRight();
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else
            {
                while (true)
                {
                    if (CharsRight() < 3 || RightChar(2) != '#' || RightChar(1) != '?' || RightChar() != '(')
                    {
                        return;
                    }

                    // skip comment (?# ...)
                    while (CharsRight() > 0 && RightChar() != ')')
                    {
                        MoveRight();
                    }

                    if (CharsRight() == 0)
                    {
                        throw MakeException(RegexParseError.UnterminatedComment, SR.UnterminatedComment);
                    }

                    MoveRight();
                }
            }
        }

        /// <summary>
        /// Scans chars following a '\' (not counting the '\'), and returns
        /// a RegexNode for the type of atom scanned.
        /// </summary>
        private RegexNode? ScanBackslash(bool scanOnly)
        {
            Debug.Assert(CharsRight() > 0, "The current reading position must not be at the end of the pattern");

            char ch;
            switch (ch = RightChar())
            {
                case 'b':
                case 'B':
                case 'A':
                case 'G':
                case 'Z':
                case 'z':
                    MoveRight();
                    return scanOnly ? null :
                        new RegexNode(TypeFromCode(ch), _options);

                case 'w':
                    MoveRight();
                    return scanOnly ? null :
                        new RegexNode(RegexNode.Set, _options, UseOptionE() ? RegexCharClass.ECMAWordClass : RegexCharClass.WordClass);

                case 'W':
                    MoveRight();
                    return scanOnly ? null :
                        new RegexNode(RegexNode.Set, _options, UseOptionE() ? RegexCharClass.NotECMAWordClass : RegexCharClass.NotWordClass);

                case 's':
                    MoveRight();
                    return scanOnly ? null :
                        new RegexNode(RegexNode.Set, _options, UseOptionE() ? RegexCharClass.ECMASpaceClass : RegexCharClass.SpaceClass);

                case 'S':
                    MoveRight();
                    return scanOnly ? null :
                        new RegexNode(RegexNode.Set, _options, UseOptionE() ? RegexCharClass.NotECMASpaceClass : RegexCharClass.NotSpaceClass);

                case 'd':
                    MoveRight();
                    return scanOnly ? null :
                        new RegexNode(RegexNode.Set, _options, UseOptionE() ? RegexCharClass.ECMADigitClass : RegexCharClass.DigitClass);

                case 'D':
                    MoveRight();
                    return scanOnly ? null :
                        new RegexNode(RegexNode.Set, _options, UseOptionE() ? RegexCharClass.NotECMADigitClass : RegexCharClass.NotDigitClass);

                case 'p':
                case 'P':
                    MoveRight();
                    if (scanOnly)
                    {
                        return null;
                    }

                    var cc = new RegexCharClass();
                    cc.AddCategoryFromName(ParseProperty(), ch != 'p', UseOptionI(), _pattern, _currentPos);
                    if (UseOptionI())
                    {
                        cc.AddLowercase(_culture);
                    }

                    return new RegexNode(RegexNode.Set, _options, cc.ToStringClass());

                default:
                    return ScanBasicBackslash(scanOnly);
            }
        }

        /// <summary>Scans \-style backreferences and character escapes</summary>
        private RegexNode? ScanBasicBackslash(bool scanOnly)
        {
            if (CharsRight() == 0)
            {
                throw MakeException(RegexParseError.UnescapedEndingBackslash, SR.UnescapedEndingBackslash);
            }

            int backpos = Textpos();
            char close = '\0';
            bool angled = false;
            char ch = RightChar();

            // allow \k<foo> instead of \<foo>, which is now deprecated

            if (ch == 'k')
            {
                if (CharsRight() >= 2)
                {
                    MoveRight();
                    ch = RightCharMoveRight();
                    if (ch == '<' || ch == '\'')
                    {
                        angled = true;
                        close = (ch == '\'') ? '\'' : '>';
                    }
                }

                if (!angled || CharsRight() <= 0)
                {
                    throw MakeException(RegexParseError.MalformedNamedReference, SR.MalformedNamedReference);
                }

                ch = RightChar();
            }

            // Note angle without \g

            else if ((ch == '<' || ch == '\'') && CharsRight() > 1)
            {
                angled = true;
                close = (ch == '\'') ? '\'' : '>';
                MoveRight();
                ch = RightChar();
            }

            // Try to parse backreference: \<1>

            if (angled && ch >= '0' && ch <= '9')
            {
                int capnum = ScanDecimal();

                if (CharsRight() > 0 && RightCharMoveRight() == close)
                {
                    return
                        scanOnly ? null :
                        IsCaptureSlot(capnum) ? new RegexNode(RegexNode.Ref, _options, capnum) :
                        throw MakeException(RegexParseError.UndefinedNumberedReference, SR.Format(SR.UndefinedNumberedReference, capnum.ToString()));
                }
            }

            // Try to parse backreference or octal: \1

            else if (!angled && ch >= '1' && ch <= '9')
            {
                if (UseOptionE())
                {
                    int capnum = -1;
                    int newcapnum = ch - '0';
                    int pos = Textpos() - 1;
                    while (newcapnum <= _captop)
                    {
                        if (IsCaptureSlot(newcapnum) && (_caps == null || (int)_caps[newcapnum]! < pos))
                        {
                            capnum = newcapnum;
                        }

                        MoveRight();
                        if (CharsRight() == 0 || (ch = RightChar()) < '0' || ch > '9')
                        {
                            break;
                        }

                        newcapnum = newcapnum * 10 + (ch - '0');
                    }

                    if (capnum >= 0)
                    {
                        return scanOnly ? null : new RegexNode(RegexNode.Ref, _options, capnum);
                    }
                }
                else
                {
                    int capnum = ScanDecimal();

                    if (scanOnly)
                    {
                        return null;
                    }

                    if (IsCaptureSlot(capnum))
                    {
                        return new RegexNode(RegexNode.Ref, _options, capnum);
                    }

                    if (capnum <= 9)
                    {
                        throw MakeException(RegexParseError.UndefinedNumberedReference, SR.Format(SR.UndefinedNumberedReference, capnum.ToString()));
                    }
                }
            }

            // Try to parse backreference: \<foo>

            else if (angled && RegexCharClass.IsWordChar(ch))
            {
                string capname = ScanCapname();

                if (CharsRight() > 0 && RightCharMoveRight() == close)
                {
                    return
                        scanOnly ? null :
                        IsCaptureName(capname) ? new RegexNode(RegexNode.Ref, _options, CaptureSlotFromName(capname)) :
                        throw MakeException(RegexParseError.UndefinedNamedReference, SR.Format(SR.UndefinedNamedReference, capname));
                }
            }

            // Not backreference: must be char code

            Textto(backpos);
            ch = ScanCharEscape();

            if (UseOptionI())
            {
                ch = _culture.TextInfo.ToLower(ch);
            }

            return scanOnly ? null : new RegexNode(RegexNode.One, _options, ch);
        }

        /*
         * Scans $ patterns recognized within replacement patterns
         */
        private RegexNode ScanDollar()
        {
            if (CharsRight() == 0)
            {
                return new RegexNode(RegexNode.One, _options, '$');
            }

            char ch = RightChar();
            bool angled;
            int backpos = Textpos();
            int lastEndPos = backpos;

            // Note angle

            if (ch == '{' && CharsRight() > 1)
            {
                angled = true;
                MoveRight();
                ch = RightChar();
            }
            else
            {
                angled = false;
            }

            // Try to parse backreference: \1 or \{1} or \{cap}

            if (ch >= '0' && ch <= '9')
            {
                if (!angled && UseOptionE())
                {
                    int capnum = -1;
                    int newcapnum = ch - '0';
                    MoveRight();
                    if (IsCaptureSlot(newcapnum))
                    {
                        capnum = newcapnum;
                        lastEndPos = Textpos();
                    }

                    while (CharsRight() > 0 && (ch = RightChar()) >= '0' && ch <= '9')
                    {
                        int digit = ch - '0';
                        if (newcapnum > MaxValueDiv10 || (newcapnum == MaxValueDiv10 && digit > MaxValueMod10))
                        {
                            throw MakeException(RegexParseError.QuantifierOrCaptureGroupOutOfRange, SR.QuantifierOrCaptureGroupOutOfRange);
                        }

                        newcapnum = newcapnum * 10 + digit;

                        MoveRight();
                        if (IsCaptureSlot(newcapnum))
                        {
                            capnum = newcapnum;
                            lastEndPos = Textpos();
                        }
                    }
                    Textto(lastEndPos);
                    if (capnum >= 0)
                    {
                        return new RegexNode(RegexNode.Ref, _options, capnum);
                    }
                }
                else
                {
                    int capnum = ScanDecimal();
                    if (!angled || CharsRight() > 0 && RightCharMoveRight() == '}')
                    {
                        if (IsCaptureSlot(capnum))
                        {
                            return new RegexNode(RegexNode.Ref, _options, capnum);
                        }
                    }
                }
            }
            else if (angled && RegexCharClass.IsWordChar(ch))
            {
                string capname = ScanCapname();
                if (CharsRight() > 0 && RightCharMoveRight() == '}' && IsCaptureName(capname))
                {
                    return new RegexNode(RegexNode.Ref, _options, CaptureSlotFromName(capname));
                }
            }
            else if (!angled)
            {
                int capnum = 1;

                switch (ch)
                {
                    case '$':
                        MoveRight();
                        return new RegexNode(RegexNode.One, _options, '$');

                    case '&':
                        capnum = 0;
                        break;

                    case '`':
                        capnum = RegexReplacement.LeftPortion;
                        break;

                    case '\'':
                        capnum = RegexReplacement.RightPortion;
                        break;

                    case '+':
                        capnum = RegexReplacement.LastGroup;
                        break;

                    case '_':
                        capnum = RegexReplacement.WholeString;
                        break;
                }

                if (capnum != 1)
                {
                    MoveRight();
                    return new RegexNode(RegexNode.Ref, _options, capnum);
                }
            }

            // unrecognized $: literalize

            Textto(backpos);
            return new RegexNode(RegexNode.One, _options, '$');
        }

        /*
         * Scans a capture name: consumes word chars
         */
        private string ScanCapname()
        {
            int startpos = Textpos();

            while (CharsRight() > 0)
            {
                if (!RegexCharClass.IsWordChar(RightCharMoveRight()))
                {
                    MoveLeft();
                    break;
                }
            }

            return _pattern.Substring(startpos, Textpos() - startpos);
        }


        /*
         * Scans up to three octal digits (stops before exceeding 0377).
         */
        private char ScanOctal()
        {
            // Consume octal chars only up to 3 digits and value 0377
            int c = 3;
            if (c > CharsRight())
            {
                c = CharsRight();
            }

            int d;
            int i;
            for (i = 0; c > 0 && (uint)(d = RightChar() - '0') <= 7; c -= 1)
            {
                MoveRight();
                i = (i * 8) + d;
                if (UseOptionE() && i >= 0x20)
                {
                    break;
                }
            }

            // Octal codes only go up to 255.  Any larger and the behavior that Perl follows
            // is simply to truncate the high bits.
            i &= 0xFF;

            return (char)i;
        }

        /*
         * Scans any number of decimal digits (pegs value at 2^31-1 if too large)
         */
        private int ScanDecimal()
        {
            int i = 0;
            int d;

            while (CharsRight() > 0 && (uint)(d = (char)(RightChar() - '0')) <= 9)
            {
                MoveRight();

                if (i > MaxValueDiv10 || (i == MaxValueDiv10 && d > MaxValueMod10))
                {
                    throw MakeException(RegexParseError.QuantifierOrCaptureGroupOutOfRange, SR.QuantifierOrCaptureGroupOutOfRange);
                }

                i = (i * 10) + d;
            }

            return i;
        }

        /*
         * Scans exactly c hex digits (c=2 for \xFF, c=4 for \uFFFF)
         */
        private char ScanHex(int c)
        {
            int i = 0;
            int d;

            if (CharsRight() >= c)
            {
                for (; c > 0 && ((d = HexDigit(RightCharMoveRight())) >= 0); c -= 1)
                {
                    i = (i * 0x10) + d;
                }
            }

            if (c > 0)
            {
                throw MakeException(RegexParseError.InsufficientOrInvalidHexDigits, SR.InsufficientOrInvalidHexDigits);
            }

            return (char)i;
        }

        /*
         * Returns n <= 0xF for a hex digit.
         */
        private static int HexDigit(char ch)
        {
            int d;

            if ((uint)(d = ch - '0') <= 9)
                return d;

            if ((uint)(d = ch - 'a') <= 5)
                return d + 0xa;

            if ((uint)(d = ch - 'A') <= 5)
                return d + 0xa;

            return -1;
        }

        /*
         * Grabs and converts an ASCII control character
         */
        private char ScanControl()
        {
            if (CharsRight() == 0)
            {
                throw MakeException(RegexParseError.MissingControlCharacter, SR.MissingControlCharacter);
            }

            char ch = RightCharMoveRight();

            // \ca interpreted as \cA

            if ((uint)(ch - 'a') <= 'z' - 'a')
            {
                ch = (char)(ch - ('a' - 'A'));
            }

            if ((ch = (char)(ch - '@')) < ' ')
            {
                return ch;
            }

            throw MakeException(RegexParseError.UnrecognizedControlCharacter, SR.UnrecognizedControlCharacter);
        }

        /// <summary>Returns true for options allowed only at the top level</summary>
        private bool IsOnlyTopOption(RegexOptions options) =>
            options == RegexOptions.RightToLeft ||
            options == RegexOptions.CultureInvariant ||
            options == RegexOptions.ECMAScript;

        /// <summary>Scans cimsx-cimsx option string, stops at the first unrecognized char.</summary>
        private void ScanOptions()
        {
            for (bool off = false; CharsRight() > 0; MoveRight())
            {
                char ch = RightChar();

                if (ch == '-')
                {
                    off = true;
                }
                else if (ch == '+')
                {
                    off = false;
                }
                else
                {
                    RegexOptions options = OptionFromCode(ch);
                    if (options == 0 || IsOnlyTopOption(options))
                    {
                        return;
                    }

                    if (off)
                    {
                        _options &= ~options;
                    }
                    else
                    {
                        _options |= options;
                    }
                }
            }
        }

        /// <summary>Scans \ code for escape codes that map to single Unicode chars.</summary>
        private char ScanCharEscape()
        {
            char ch = RightCharMoveRight();

            if (ch >= '0' && ch <= '7')
            {
                MoveLeft();
                return ScanOctal();
            }

            switch (ch)
            {
                case 'x':
                    return ScanHex(2);
                case 'u':
                    return ScanHex(4);
                case 'a':
                    return '\u0007';
                case 'b':
                    return '\b';
                case 'e':
                    return '\u001B';
                case 'f':
                    return '\f';
                case 'n':
                    return '\n';
                case 'r':
                    return '\r';
                case 't':
                    return '\t';
                case 'v':
                    return '\u000B';
                case 'c':
                    return ScanControl();
                default:
                    if (!UseOptionE() && RegexCharClass.IsWordChar(ch))
                    {
                        throw MakeException(RegexParseError.UnrecognizedEscape, SR.Format(SR.UnrecognizedEscape, ch));
                    }
                    return ch;
            }
        }

        /// <summary>Scans X for \p{X} or \P{X}</summary>
        private string ParseProperty()
        {
            if (CharsRight() < 3)
            {
                throw MakeException(RegexParseError.InvalidUnicodePropertyEscape, SR.InvalidUnicodePropertyEscape);
            }

            char ch = RightCharMoveRight();
            if (ch != '{')
            {
                throw MakeException(RegexParseError.MalformedUnicodePropertyEscape, SR.MalformedUnicodePropertyEscape);
            }

            int startpos = Textpos();
            while (CharsRight() > 0)
            {
                ch = RightCharMoveRight();
                if (!(RegexCharClass.IsWordChar(ch) || ch == '-'))
                {
                    MoveLeft();
                    break;
                }
            }

            string capname = _pattern.Substring(startpos, Textpos() - startpos);

            if (CharsRight() == 0 || RightCharMoveRight() != '}')
            {
                throw MakeException(RegexParseError.InvalidUnicodePropertyEscape, SR.InvalidUnicodePropertyEscape);
            }

            return capname;
        }

        /// <summary>Returns ReNode type for zero-length assertions with a \ code.</summary>
        private int TypeFromCode(char ch) =>
            ch switch
            {
                'b' => UseOptionE() ? RegexNode.ECMABoundary : RegexNode.Boundary,
                'B' => UseOptionE() ? RegexNode.NonECMABoundary : RegexNode.NonBoundary,
                'A' => RegexNode.Beginning,
                'G' => RegexNode.Start,
                'Z' => RegexNode.EndZ,
                'z' => RegexNode.End,
                _ => RegexNode.Nothing,
            };

        /// <summary>Returns option bit from single-char (?cimsx) code.</summary>
        private static RegexOptions OptionFromCode(char ch)
        {
            // case-insensitive
            if ((uint)(ch - 'A') <= 'Z' - 'A')
            {
                ch += (char)('a' - 'A');
            }

            return ch switch
            {
                'i' => RegexOptions.IgnoreCase,
                'r' => RegexOptions.RightToLeft,
                'm' => RegexOptions.Multiline,
                'n' => RegexOptions.ExplicitCapture,
                's' => RegexOptions.Singleline,
                'x' => RegexOptions.IgnorePatternWhitespace,
#if DEBUG
                'd' => RegexOptions.Debug,
#endif
                'e' => RegexOptions.ECMAScript,
                _ => 0,
            };
        }

        /// <summary>
        /// A prescanner for deducing the slots used for captures by doing a partial tokenization of the pattern.
        /// </summary>
        private void CountCaptures()
        {
            NoteCaptureSlot(0, 0);

            _autocap = 1;

            while (CharsRight() > 0)
            {
                int pos = Textpos();
                char ch = RightCharMoveRight();
                switch (ch)
                {
                    case '\\':
                        if (CharsRight() > 0)
                        {
                            ScanBackslash(scanOnly: true);
                        }
                        break;

                    case '#':
                        if (UseOptionX())
                        {
                            MoveLeft();
                            ScanBlank();
                        }
                        break;

                    case '[':
                        ScanCharClass(caseInsensitive: false, scanOnly: true);
                        break;

                    case ')':
                        if (!EmptyOptionsStack())
                        {
                            PopOptions();
                        }
                        break;

                    case '(':
                        if (CharsRight() >= 2 && RightChar(1) == '#' && RightChar() == '?')
                        {
                            // we have a comment (?#
                            MoveLeft();
                            ScanBlank();
                        }
                        else
                        {
                            PushOptions();
                            if (CharsRight() > 0 && RightChar() == '?')
                            {
                                // we have (?...
                                MoveRight();

                                if (CharsRight() > 1 && (RightChar() == '<' || RightChar() == '\''))
                                {
                                    // named group: (?<... or (?'...

                                    MoveRight();
                                    ch = RightChar();

                                    if (ch != '0' && RegexCharClass.IsWordChar(ch))
                                    {
                                        if ((uint)(ch - '1') <= '9' - '1')
                                        {
                                            NoteCaptureSlot(ScanDecimal(), pos);
                                        }
                                        else
                                        {
                                            NoteCaptureName(ScanCapname(), pos);
                                        }
                                    }
                                }
                                else
                                {
                                    // (?...

                                    // get the options if it's an option construct (?cimsx-cimsx...)
                                    ScanOptions();

                                    if (CharsRight() > 0)
                                    {
                                        if (RightChar() == ')')
                                        {
                                            // (?cimsx-cimsx)
                                            MoveRight();
                                            PopKeepOptions();
                                        }
                                        else if (RightChar() == '(')
                                        {
                                            // alternation construct: (?(foo)yes|no)
                                            // ignore the next paren so we don't capture the condition
                                            _ignoreNextParen = true;

                                            // break from here so we don't reset _ignoreNextParen
                                            break;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // Simple (unnamed) capture group.
                                // Add unnamed parentheses if ExplicitCapture is not set
                                // and the next parentheses is not ignored.
                                if (!UseOptionN() && !_ignoreNextParen)
                                {
                                    NoteCaptureSlot(_autocap++, pos);
                                }
                            }
                        }

                        _ignoreNextParen = false;
                        break;
                }
            }

            AssignNameSlots();
        }

        /// <summary>Notes a used capture slot</summary>
        private void NoteCaptureSlot(int i, int pos)
        {
            object boxedI = i; // workaround to remove a boxed int when adding to the hashtable
            if (!_caps.ContainsKey(boxedI))
            {
                // the rhs of the hashtable isn't used in the parser

                _caps.Add(boxedI, pos);
                _capcount++;

                if (_captop <= i)
                {
                    _captop = i == int.MaxValue ? i : i + 1;
                }
            }
        }

        /// <summary>Notes a used capture slot</summary>
        private void NoteCaptureName(string name, int pos)
        {
            if (_capnames == null)
            {
                _capnames = new Hashtable();
                _capnamelist = new List<string>();
            }

            if (!_capnames.ContainsKey(name))
            {
                _capnames.Add(name, pos);
                _capnamelist!.Add(name);
            }
        }

        /// <summary>Assigns unused slot numbers to the capture names.</summary>
        private void AssignNameSlots()
        {
            if (_capnames != null)
            {
                for (int i = 0; i < _capnamelist!.Count; i++)
                {
                    while (IsCaptureSlot(_autocap))
                    {
                        _autocap++;
                    }

                    string name = _capnamelist[i];
                    int pos = (int)_capnames[name]!;
                    _capnames[name] = _autocap;
                    NoteCaptureSlot(_autocap, pos);

                    _autocap++;
                }
            }

            // if the caps array has at least one gap, construct the list of used slots

            if (_capcount < _captop)
            {
                _capnumlist = new int[_capcount];
                int i = 0;

                // Manual use of IDictionaryEnumerator instead of foreach to avoid DictionaryEntry box allocations.
                IDictionaryEnumerator de = _caps.GetEnumerator();
                while (de.MoveNext())
                {
                    _capnumlist[i++] = (int)de.Key;
                }

                Array.Sort(_capnumlist);
            }

            // merge capsnumlist into capnamelist

            if (_capnames != null || _capnumlist != null)
            {
                List<string>? oldcapnamelist;
                int next;
                int k = 0;

                if (_capnames == null)
                {
                    oldcapnamelist = null;
                    _capnames = new Hashtable();
                    _capnamelist = new List<string>();
                    next = -1;
                }
                else
                {
                    oldcapnamelist = _capnamelist;
                    _capnamelist = new List<string>();
                    next = (int)_capnames[oldcapnamelist![0]]!;
                }

                for (int i = 0; i < _capcount; i++)
                {
                    int j = (_capnumlist == null) ? i : _capnumlist[i];

                    if (next == j)
                    {
                        _capnamelist.Add(oldcapnamelist![k++]);
                        next = (k == oldcapnamelist.Count) ? -1 : (int)_capnames[oldcapnamelist[k]]!;
                    }
                    else
                    {
                        string str = j.ToString(_culture);
                        _capnamelist.Add(str);
                        _capnames[str] = j;
                    }
                }
            }
        }

        /// <summary>Looks up the slot number for a given name.</summary>
        private int CaptureSlotFromName(string capname) => (int)_capnames![capname]!;

        /// <summary>True if the capture slot was noted</summary>
        private bool IsCaptureSlot(int i)
        {
            if (_caps != null)
            {
                return _caps.ContainsKey(i);
            }

            return i >= 0 && i < _capsize;
        }

        /// <summary>Looks up the slot number for a given name</summary>
        private bool IsCaptureName(string capname) => _capnames != null && _capnames.ContainsKey(capname);

        /// <summary>True if N option disabling '(' autocapture is on.</summary>
        private bool UseOptionN() => (_options & RegexOptions.ExplicitCapture) != 0;

        /// <summary>True if I option enabling case-insensitivity is on.</summary>
        private bool UseOptionI() => (_options & RegexOptions.IgnoreCase) != 0;

        /// <summary>True if M option altering meaning of $ and ^ is on.</summary>
        private bool UseOptionM() => (_options & RegexOptions.Multiline) != 0;

        /// <summary>True if S option altering meaning of . is on.</summary>
        private bool UseOptionS() => (_options & RegexOptions.Singleline) != 0;

        /// <summary> True if X option enabling whitespace/comment mode is on.</summary>
        private bool UseOptionX() => (_options & RegexOptions.IgnorePatternWhitespace) != 0;

        /// <summary>True if E option enabling ECMAScript behavior is on.</summary>
        private bool UseOptionE() => (_options & RegexOptions.ECMAScript) != 0;

        private const byte Q = 5;    // quantifier
        private const byte S = 4;    // ordinary stopper
        private const byte Z = 3;    // ScanBlank stopper
        private const byte X = 2;    // whitespace
        private const byte E = 1;    // should be escaped

        /// <summary>For categorizing ASCII characters.</summary>
        private static ReadOnlySpan<byte> Category => new byte[] {
            // 0  1  2  3  4  5  6  7  8  9  A  B  C  D  E  F  0  1  2  3  4  5  6  7  8  9  A  B  C  D  E  F
               0, 0, 0, 0, 0, 0, 0, 0, 0, X, X, 0, X, X, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            //    !  "  #  $  %  &  '  (  )  *  +  ,  -  .  /  0  1  2  3  4  5  6  7  8  9  :  ;  <  =  >  ?
               X, 0, 0, Z, S, 0, 0, 0, S, S, Q, Q, 0, 0, S, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, Q,
            // @  A  B  C  D  E  F  G  H  I  J  K  L  M  N  O  P  Q  R  S  T  U  V  W  X  Y  Z  [  \  ]  ^  _
               0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, S, S, 0, S, 0,
            // '  a  b  c  d  e  f  g  h  i  j  k  l  m  n  o  p  q  r  s  t  u  v  w  x  y  z  {  |  }  ~
               0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, Q, S, 0, 0, 0};

        /// <summary>Returns true for those characters that terminate a string of ordinary chars.</summary>
        private static bool IsSpecial(char ch) => ch <= '|' && Category[ch] >= S;

        /// <summary>Returns true for those characters that terminate a string of ordinary chars.</summary>
        private static bool IsStopperX(char ch) => ch <= '|' && Category[ch] >= X;

        /// <summary>Returns true for those characters that begin a quantifier.</summary>
        private static bool IsQuantifier(char ch) => ch <= '{' && Category[ch] >= Q;

        private bool IsTrueQuantifier()
        {
            Debug.Assert(CharsRight() > 0, "The current reading position must not be at the end of the pattern");

            int startpos = Textpos();
            char ch = CharAt(startpos);
            if (ch != '{')
            {
                return ch <= '{' && Category[ch] >= Q;
            }

            int pos = startpos;
            int nChars = CharsRight();
            while (--nChars > 0 && (uint)((ch = CharAt(++pos)) - '0') <= 9) ;

            if (nChars == 0 || pos - startpos == 1)
            {
                return false;
            }

            if (ch == '}')
            {
                return true;
            }

            if (ch != ',')
            {
                return false;
            }

            while (--nChars > 0 && (uint)((ch = CharAt(++pos)) - '0') <= 9) ;

            return nChars > 0 && ch == '}';
        }

        /// <summary>Returns true for whitespace.</summary>
        private static bool IsSpace(char ch) => ch <= ' ' && Category[ch] == X;

        /// <summary>Returns true for chars that should be escaped.</summary>
        private static bool IsMetachar(char ch) => ch <= '|' && Category[ch] >= E;

        /// <summary>Add a string to the last concatenate.</summary>
        private void AddConcatenate(int pos, int cch, bool isReplacement)
        {
            if (cch == 0)
            {
                return;
            }

            RegexNode node;
            if (cch > 1)
            {
                string str = UseOptionI() && !isReplacement ?
                    string.Create(cch, (_pattern, _culture, pos, cch), static (span, state) => state._pattern.AsSpan(state.pos, state.cch).ToLower(span, state._culture)) :
                    _pattern.Substring(pos, cch);

                node = new RegexNode(RegexNode.Multi, _options, str);
            }
            else
            {
                char ch = _pattern[pos];

                if (UseOptionI() && !isReplacement)
                {
                    ch = _culture.TextInfo.ToLower(ch);
                }

                node = new RegexNode(RegexNode.One, _options, ch);
            }

            _concatenation!.AddChild(node);
        }

        /// <summary>Push the parser state (in response to an open paren)</summary>
        private void PushGroup()
        {
            _group!.Next = _stack;
            _alternation!.Next = _group;
            _concatenation!.Next = _alternation;
            _stack = _concatenation;
        }

        /// <summary>Remember the pushed state (in response to a ')')</summary>
        private void PopGroup()
        {
            _concatenation = _stack;
            _alternation = _concatenation!.Next;
            _group = _alternation!.Next;
            _stack = _group!.Next;

            // The first () inside a Testgroup group goes directly to the group
            if (_group.Type == RegexNode.Testgroup && _group.ChildCount() == 0)
            {
                if (_unit == null)
                {
                    throw MakeException(RegexParseError.AlternationHasMalformedCondition, SR.AlternationHasMalformedCondition);
                }

                _group.AddChild(_unit);
                _unit = null;
            }
        }

        /// <summary>True if the group stack is empty.</summary>
        private bool EmptyStack() => _stack == null;

        /// <summary>Start a new round for the parser state (in response to an open paren or string start)</summary>
        private void StartGroup(RegexNode openGroup)
        {
            _group = openGroup;
            _alternation = new RegexNode(RegexNode.Alternate, _options);
            _concatenation = new RegexNode(RegexNode.Concatenate, _options);
        }

        /// <summary>Finish the current concatenation (in response to a |)</summary>
        private void AddAlternate()
        {
            // The | parts inside a Testgroup group go directly to the group

            if (_group!.Type == RegexNode.Testgroup || _group.Type == RegexNode.Testref)
            {
                _group.AddChild(_concatenation!.ReverseLeft());
            }
            else
            {
                _alternation!.AddChild(_concatenation!.ReverseLeft());
            }

            _concatenation = new RegexNode(RegexNode.Concatenate, _options);
        }

        /// <summary>Finish the current quantifiable (when a quantifier is not found or is not possible)</summary>
        private void AddConcatenate()
        {
            // The first (| inside a Testgroup group goes directly to the group

            _concatenation!.AddChild(_unit!);
            _unit = null;
        }

        /// <summary>Finish the current quantifiable (when a quantifier is found)</summary>
        private void AddConcatenate(bool lazy, int min, int max)
        {
            _concatenation!.AddChild(_unit!.MakeQuantifier(lazy, min, max));
            _unit = null;
        }

        /// <summary>Returns the current unit</summary>
        private RegexNode? Unit() => _unit;

        /// <summary>Sets the current unit to a single char node</summary>
        private void AddUnitOne(char ch)
        {
            if (UseOptionI())
            {
                ch = _culture.TextInfo.ToLower(ch);
            }

            _unit = new RegexNode(RegexNode.One, _options, ch);
        }

        /// <summary>Sets the current unit to a single inverse-char node</summary>
        private void AddUnitNotone(char ch)
        {
            if (UseOptionI())
            {
                ch = _culture.TextInfo.ToLower(ch);
            }

            _unit = new RegexNode(RegexNode.Notone, _options, ch);
        }

        /// <summary>Sets the current unit to a single set node</summary>
        private void AddUnitSet(string cc)
        {
            _unit = new RegexNode(RegexNode.Set, _options, cc);
        }

        /// <summary>Sets the current unit to a subtree</summary>
        private void AddUnitNode(RegexNode node)
        {
            _unit = node;
        }

        /// <summary>Sets the current unit to an assertion of the specified type</summary>
        private void AddUnitType(int type)
        {
            _unit = new RegexNode(type, _options);
        }

        /// <summary>Finish the current group (in response to a ')' or end)</summary>
        private void AddGroup()
        {
            if (_group!.Type == RegexNode.Testgroup || _group.Type == RegexNode.Testref)
            {
                _group.AddChild(_concatenation!.ReverseLeft());

                if (_group.Type == RegexNode.Testref && _group.ChildCount() > 2 || _group.ChildCount() > 3)
                {
                    throw MakeException(RegexParseError.AlternationHasTooManyConditions, SR.AlternationHasTooManyConditions);
                }
            }
            else
            {
                _alternation!.AddChild(_concatenation!.ReverseLeft());
                _group.AddChild(_alternation);
            }

            _unit = _group;
        }

        /// <summary>Saves options on a stack.</summary>
        private void PushOptions() => _optionsStack.Append((int)_options);

        /// <summary>Recalls options from the stack.</summary>
        private void PopOptions() => _options = (RegexOptions)_optionsStack.Pop();

        /// <summary>True if options stack is empty.</summary>
        private bool EmptyOptionsStack() => _optionsStack.Length == 0;

        /// <summary>Pops the options stack, but keeps the current options unchanged.</summary>
        private void PopKeepOptions() => _optionsStack.Length--;

        /// <summary>Fills in a RegexParseException</summary>
        private RegexParseException MakeException(RegexParseError error, string message) =>
            new RegexParseException(error, _currentPos, SR.Format(SR.MakeException, _pattern, _currentPos, message));

        /// <summary>Returns the current parsing position.</summary>
        private int Textpos() => _currentPos;

        /// <summary>Zaps to a specific parsing position.</summary>
        private void Textto(int pos) => _currentPos = pos;

        /// <summary>Returns the char at the right of the current parsing position and advances to the right.</summary>
        private char RightCharMoveRight() => _pattern[_currentPos++];

        /// <summary>Moves the current position to the right.</summary>
        private void MoveRight() => _currentPos++;

        private void MoveRight(int i) => _currentPos += i;

        /// <summary>Moves the current parsing position one to the left.</summary>
        private void MoveLeft() => --_currentPos;

        /// <summary>Returns the char left of the current parsing position.</summary>
        private char CharAt(int i) => _pattern[i];

        /// <summary>Returns the char right of the current parsing position.</summary>
        private char RightChar() => _pattern[_currentPos];

        /// <summary>Returns the char i chars right of the current parsing position.</summary>
        private char RightChar(int i) => _pattern[_currentPos + i];

        /// <summary>Number of characters to the right of the current parsing position.</summary>
        private int CharsRight() => _pattern.Length - _currentPos;
    }
}
