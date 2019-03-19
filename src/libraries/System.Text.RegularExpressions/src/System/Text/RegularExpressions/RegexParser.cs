// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This RegexParser class is internal to the Regex package.
// It builds a tree of RegexNodes from a regular expression

// Implementation notes:
//
// It would be nice to get rid of the comment modes, since the
// ScanBlank() calls are just kind of duct-taped in.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace System.Text.RegularExpressions
{
    internal ref struct RegexParser
    {
        private const int EscapeMaxBufferSize = 256;
        private const int OptionStackDefaultSize = 32;
        private const int MaxValueDiv10 = int.MaxValue / 10;
        private const int MaxValueMod10 = int.MaxValue % 10;

        private RegexNode _stack;
        private RegexNode _group;
        private RegexNode _alternation;
        private RegexNode _concatenation;
        private RegexNode _unit;

        private readonly string _pattern;
        private int _currentPos;
        private readonly CultureInfo _culture;

        private int _autocap;
        private int _capcount;
        private int _captop;
        private int _capsize;

        private Hashtable _caps;
        private Hashtable _capnames;

        private int[] _capnumlist;
        private List<string> _capnamelist;

        private RegexOptions _options;
        private ValueListBuilder<RegexOptions> _optionsStack;

        private bool _ignoreNextParen; // flag to skip capturing a parentheses group

        private RegexParser(string pattern, RegexOptions options, CultureInfo culture, Hashtable caps, int capsize, Hashtable capnames, Span<RegexOptions> optionSpan)
        {
            Debug.Assert(pattern != null, "Pattern must be set");
            Debug.Assert(culture != null, "Culture must be set");

            _pattern = pattern;
            _options = options;
            _culture = culture;
            _caps = caps;
            _capsize = capsize;
            _capnames = capnames;

            _optionsStack = new ValueListBuilder<RegexOptions>(optionSpan);
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

        private RegexParser(string pattern, RegexOptions options, CultureInfo culture, Span<RegexOptions> optionSpan)
            : this(pattern, options, culture, new Hashtable(), default, null, optionSpan)
        {
        }

        public static RegexTree Parse(string pattern, RegexOptions options, CultureInfo culture)
        {
            Span<RegexOptions> optionSpan = stackalloc RegexOptions[OptionStackDefaultSize];
            var parser = new RegexParser(pattern, options, culture, optionSpan);

            parser.CountCaptures();
            parser.Reset(options);
            RegexNode root = parser.ScanRegex();
            string[] capnamelist = parser._capnamelist?.ToArray();
            var tree = new RegexTree(root, parser._caps, parser._capnumlist, parser._captop, parser._capnames, capnamelist, options);
            parser.Dispose();

            return tree;
        }

        /// <summary>
        /// This static call constructs a flat concatenation node given a replacement pattern.
        /// </summary>
        public static RegexReplacement ParseReplacement(string pattern, RegexOptions options, Hashtable caps, int capsize, Hashtable capnames)
        {
            CultureInfo culture = (options & RegexOptions.CultureInvariant) != 0 ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture;
            Span<RegexOptions> optionSpan = stackalloc RegexOptions[OptionStackDefaultSize];
            var parser = new RegexParser(pattern, options, culture, caps, capsize, capnames, optionSpan);

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
            Span<char> buffer = input.Length <= (EscapeMaxBufferSize / 3) ? stackalloc char[EscapeMaxBufferSize] : default;
            ValueStringBuilder vsb = !buffer.IsEmpty ?
                new ValueStringBuilder(buffer) :
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
                        break;

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
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '\\')
                {
                    return UnescapeImpl(input, i);
                }
            }

            return input;
        }

        private static string UnescapeImpl(string input, int i)
        {
            Span<RegexOptions> optionSpan = stackalloc RegexOptions[OptionStackDefaultSize];
            var parser = new RegexParser(input, RegexOptions.None, CultureInfo.InvariantCulture, optionSpan);

            // In the worst case the escaped string has the same length.
            // For small inputs we use stack allocation.
            Span<char> buffer = input.Length <= EscapeMaxBufferSize ? stackalloc char[EscapeMaxBufferSize] : default;
            ValueStringBuilder vsb = !buffer.IsEmpty ?
                new ValueStringBuilder(buffer) :
                new ValueStringBuilder(input.Length);

            vsb.Append(input.AsSpan(0, i));
            do
            {
                i++;
                parser.Textto(i);
                if (i < input.Length)
                    vsb.Append(parser.ScanCharEscape());
                i = parser.Textpos();
                int lastpos = i;
                while (i < input.Length && input[i] != '\\')
                    i++;
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
                    while (CharsRight() > 0 && (!IsStopperX(ch = RightChar()) || (ch == '{' && !IsTrueQuantifier())))
                        MoveRight();
                else
                    while (CharsRight() > 0 && (!IsSpecial(ch = RightChar()) || (ch == '{' && !IsTrueQuantifier())))
                        MoveRight();

                int endpos = Textpos();

                ScanBlank();

                if (CharsRight() == 0)
                    ch = '!'; // nonspecial, means at end
                else if (IsSpecial(ch = RightChar()))
                {
                    isQuantifier = IsQuantifier(ch);
                    MoveRight();
                }
                else
                    ch = ' '; // nonspecial, means at ordinary char

                if (startpos < endpos)
                {
                    int cchUnquantified = endpos - startpos - (isQuantifier ? 1 : 0);

                    wasPrevQuantifier = false;

                    if (cchUnquantified > 0)
                        AddConcatenate(startpos, cchUnquantified, false);

                    if (isQuantifier)
                        AddUnitOne(CharAt(endpos - 1));
                }

                switch (ch)
                {
                    case '!':
                        goto BreakOuterScan;

                    case ' ':
                        goto ContinueOuterScan;

                    case '[':
                        AddUnitSet(ScanCharClass(UseOptionI(), scanOnly: false).ToStringClass());
                        break;

                    case '(':
                        {
                            RegexNode grouper;

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
                            throw MakeException(RegexParseError.TooManyParentheses, SR.TooManyParens);

                        AddGroup();
                        PopGroup();
                        PopOptions();

                        if (Unit() == null)
                            goto ContinueOuterScan;
                        break;

                    case '\\':
                        if (CharsRight() == 0)
                            throw MakeException(RegexParseError.IllegalEndEscape, SR.IllegalEndEscape);

                        AddUnitNode(ScanBackslash(scanOnly: false));
                        break;

                    case '^':
                        AddUnitType(UseOptionM() ? RegexNode.Bol : RegexNode.Beginning);
                        break;

                    case '$':
                        AddUnitType(UseOptionM() ? RegexNode.Eol : RegexNode.EndZ);
                        break;

                    case '.':
                        if (UseOptionS())
                            AddUnitSet(RegexCharClass.AnyClass);
                        else
                            AddUnitNotone('\n');
                        break;

                    case '{':
                    case '*':
                    case '+':
                    case '?':
                        if (Unit() == null)
                        {
                            if (wasPrevQuantifier)
                                throw MakeException(RegexParseError.NestedQuantify, SR.Format(SR.NestedQuantify, ch));
                            else
                                throw MakeException(RegexParseError.QuantifyAfterNothing, SR.QuantifyAfterNothing);
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
                    bool lazy;

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

                    if (CharsRight() == 0 || RightChar() != '?')
                        lazy = false;
                    else
                    {
                        MoveRight();
                        lazy = true;
                    }

                    if (min > max)
                        throw MakeException(RegexParseError.IllegalRange, SR.IllegalRange);

                    AddConcatenate(lazy, min, max);
                }

            ContinueOuterScan:
                ;
            }

        BreakOuterScan:
            ;

            if (!EmptyStack())
                throw MakeException(RegexParseError.NotEnoughParentheses, SR.NotEnoughParens);

            AddGroup();

            return Unit();
        }

        /*
         * Simple parsing for replacement patterns
         */
        private RegexNode ScanReplacement()
        {
            _concatenation = new RegexNode(RegexNode.Concatenate, _options);

            for (; ;)
            {
                int c = CharsRight();
                if (c == 0)
                    break;

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
                        AddUnitNode(ScanDollar());
                    AddConcatenate();
                }
            }

            return _concatenation;
        }

        /*
         * Scans contents of [] (not including []'s), and converts to a
         * RegexCharClass.
         */
        private RegexCharClass ScanCharClass(bool caseInsensitive, bool scanOnly)
        {
            char ch = '\0';
            char chPrev = '\0';
            bool inRange = false;
            bool firstChar = true;
            bool closed = false;

            RegexCharClass cc;

            cc = scanOnly ? null : new RegexCharClass();

            if (CharsRight() > 0 && RightChar() == '^')
            {
                MoveRight();
                if (!scanOnly)
                    cc.Negate = true;
            }

            for (; CharsRight() > 0; firstChar = false)
            {
                bool fTranslatedChar = false;
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
                                    throw MakeException(RegexParseError.BadClassInCharRange, SR.Format(SR.BadClassInCharRange, ch));
                                cc.AddDigit(UseOptionE(), ch == 'D', _pattern, _currentPos);
                            }
                            continue;

                        case 'S':
                        case 's':
                            if (!scanOnly)
                            {
                                if (inRange)
                                    throw MakeException(RegexParseError.BadClassInCharRange, SR.Format(SR.BadClassInCharRange, ch));
                                cc.AddSpace(UseOptionE(), ch == 'S');
                            }
                            continue;

                        case 'W':
                        case 'w':
                            if (!scanOnly)
                            {
                                if (inRange)
                                    throw MakeException(RegexParseError.BadClassInCharRange, SR.Format(SR.BadClassInCharRange, ch));

                                cc.AddWord(UseOptionE(), ch == 'W');
                            }
                            continue;

                        case 'p':
                        case 'P':
                            if (!scanOnly)
                            {
                                if (inRange)
                                    throw MakeException(RegexParseError.BadClassInCharRange, SR.Format(SR.BadClassInCharRange, ch));
                                cc.AddCategoryFromName(ParseProperty(), (ch != 'p'), caseInsensitive, _pattern, _currentPos);
                            }
                            else
                                ParseProperty();

                            continue;

                        case '-':
                            if (!scanOnly)
                                cc.AddRange(ch, ch);
                            continue;

                        default:
                            MoveLeft();
                            ch = ScanCharEscape(); // non-literal character
                            fTranslatedChar = true;
                            break;          // this break will only break out of the switch
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
                            Textto(savePos);
                    }
                }

                if (inRange)
                {
                    inRange = false;
                    if (!scanOnly)
                    {
                        if (ch == '[' && !fTranslatedChar && !firstChar)
                        {
                            // We thought we were in a range, but we're actually starting a subtraction.
                            // In that case, we'll add chPrev to our char class, skip the opening [, and
                            // scan the new character class recursively.
                            cc.AddChar(chPrev);
                            cc.AddSubtraction(ScanCharClass(caseInsensitive, scanOnly));

                            if (CharsRight() > 0 && RightChar() != ']')
                                throw MakeException(RegexParseError.SubtractionMustBeLast, SR.SubtractionMustBeLast);
                        }
                        else
                        {
                            // a regular range, like a-z
                            if (chPrev > ch)
                                throw MakeException(RegexParseError.ReversedCharRange, SR.ReversedCharRange);
                            cc.AddRange(chPrev, ch);
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
                else if (CharsRight() >= 1 && ch == '-' && !fTranslatedChar && RightChar() == '[' && !firstChar)
                {
                    // we aren't in a range, and now there is a subtraction.  Usually this happens
                    // only when a subtraction follows a range, like [a-z-[b]]
                    if (!scanOnly)
                    {
                        MoveRight(1);
                        cc.AddSubtraction(ScanCharClass(caseInsensitive, scanOnly));

                        if (CharsRight() > 0 && RightChar() != ']')
                            throw MakeException(RegexParseError.SubtractionMustBeLast, SR.SubtractionMustBeLast);
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
                        cc.AddRange(ch, ch);
                }
            }

            if (!closed)
                throw MakeException(RegexParseError.UnterminatedBracket, SR.UnterminatedBracket);

            if (!scanOnly && caseInsensitive)
                cc.AddLowercase(_culture);

            return cc;
        }

        /*
         * Scans chars following a '(' (not counting the '('), and returns
         * a RegexNode for the type of group scanned, or null if the group
         * simply changed options (?cimsx-cimsx) or was a comment (#...).
         */
        private RegexNode ScanGroupOpen()
        {
            // just return a RegexNode if we have:
            // 1. "(" followed by nothing
            // 2. "(x" where x != ?
            // 3. "(?)"
            if (CharsRight() == 0 || RightChar() != '?' || (RightChar() == '?' && (CharsRight() > 1 && RightChar(1) == ')')))
            {
                if (UseOptionN() || _ignoreNextParen)
                {
                    _ignoreNextParen = false;
                    return new RegexNode(RegexNode.Group, _options);
                }
                else
                    return new RegexNode(RegexNode.Capture, _options, _autocap++, -1);
            }

            MoveRight();

            for (; ;)
            {
                if (CharsRight() == 0)
                    break;

                int NodeType;
                char close = '>';
                char ch;
                switch (ch = RightCharMoveRight())
                {
                    case ':':
                        // noncapturing group
                        NodeType = RegexNode.Group;
                        break;

                    case '=':
                        // lookahead assertion
                        _options &= ~(RegexOptions.RightToLeft);
                        NodeType = RegexNode.Require;
                        break;

                    case '!':
                        // negative lookahead assertion
                        _options &= ~(RegexOptions.RightToLeft);
                        NodeType = RegexNode.Prevent;
                        break;

                    case '>':
                        // greedy subexpression
                        NodeType = RegexNode.Greedy;
                        break;

                    case '\'':
                        close = '\'';
                        goto case '<';
                    // fallthrough

                    case '<':
                        if (CharsRight() == 0)
                            goto BreakRecognize;

                        switch (ch = RightCharMoveRight())
                        {
                            case '=':
                                if (close == '\'')
                                    goto BreakRecognize;

                                // lookbehind assertion
                                _options |= RegexOptions.RightToLeft;
                                NodeType = RegexNode.Require;
                                break;

                            case '!':
                                if (close == '\'')
                                    goto BreakRecognize;

                                // negative lookbehind assertion
                                _options |= RegexOptions.RightToLeft;
                                NodeType = RegexNode.Prevent;
                                break;

                            default:
                                MoveLeft();
                                int capnum = -1;
                                int uncapnum = -1;
                                bool proceed = false;

                                // grab part before -

                                if (ch >= '0' && ch <= '9')
                                {
                                    capnum = ScanDecimal();

                                    if (!IsCaptureSlot(capnum))
                                        capnum = -1;

                                    // check if we have bogus characters after the number
                                    if (CharsRight() > 0 && !(RightChar() == close || RightChar() == '-'))
                                        throw MakeException(RegexParseError.InvalidGroupName, SR.InvalidGroupName);
                                    if (capnum == 0)
                                        throw MakeException(RegexParseError.CapnumNotZero, SR.CapnumNotZero);
                                }
                                else if (RegexCharClass.IsWordChar(ch))
                                {
                                    string capname = ScanCapname();

                                    if (IsCaptureName(capname))
                                        capnum = CaptureSlotFromName(capname);

                                    // check if we have bogus character after the name
                                    if (CharsRight() > 0 && !(RightChar() == close || RightChar() == '-'))
                                        throw MakeException(RegexParseError.InvalidGroupName, SR.InvalidGroupName);
                                }
                                else if (ch == '-')
                                {
                                    proceed = true;
                                }
                                else
                                {
                                    // bad group name - starts with something other than a word character and isn't a number
                                    throw MakeException(RegexParseError.InvalidGroupName, SR.InvalidGroupName);
                                }

                                // grab part after - if any

                                if ((capnum != -1 || proceed == true) && CharsRight() > 1 && RightChar() == '-')
                                {
                                    MoveRight();
                                    ch = RightChar();

                                    if (ch >= '0' && ch <= '9')
                                    {
                                        uncapnum = ScanDecimal();

                                        if (!IsCaptureSlot(uncapnum))
                                            throw MakeException(RegexParseError.UndefinedBackref, SR.Format(SR.UndefinedBackref, uncapnum));

                                        // check if we have bogus characters after the number
                                        if (CharsRight() > 0 && RightChar() != close)
                                            throw MakeException(RegexParseError.InvalidGroupName, SR.InvalidGroupName);
                                    }
                                    else if (RegexCharClass.IsWordChar(ch))
                                    {
                                        string uncapname = ScanCapname();

                                        if (IsCaptureName(uncapname))
                                            uncapnum = CaptureSlotFromName(uncapname);
                                        else
                                            throw MakeException(RegexParseError.UndefinedNameRef, SR.Format(SR.UndefinedNameRef, uncapname));

                                        // check if we have bogus character after the name
                                        if (CharsRight() > 0 && RightChar() != close)
                                            throw MakeException(RegexParseError.InvalidGroupName, SR.InvalidGroupName);
                                    }
                                    else
                                    {
                                        // bad group name - starts with something other than a word character and isn't a number
                                        throw MakeException(RegexParseError.InvalidGroupName, SR.InvalidGroupName);
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
                                        return new RegexNode(RegexNode.Testref, _options, capnum);
                                    else
                                        throw MakeException(RegexParseError.UndefinedReference, SR.Format(SR.UndefinedReference, capnum.ToString()));
                                }
                                else
                                    throw MakeException(RegexParseError.MalformedReference, SR.Format(SR.MalformedReference, capnum.ToString()));
                            }
                            else if (RegexCharClass.IsWordChar(ch))
                            {
                                string capname = ScanCapname();

                                if (IsCaptureName(capname) && CharsRight() > 0 && RightCharMoveRight() == ')')
                                    return new RegexNode(RegexNode.Testref, _options, CaptureSlotFromName(capname));
                            }
                        }
                        // not a backref
                        NodeType = RegexNode.Testgroup;
                        Textto(parenPos - 1);       // jump to the start of the parentheses
                        _ignoreNextParen = true;    // but make sure we don't try to capture the insides

                        int charsRight = CharsRight();
                        if (charsRight >= 3 && RightChar(1) == '?')
                        {
                            char rightchar2 = RightChar(2);
                            // disallow comments in the condition
                            if (rightchar2 == '#')
                                throw MakeException(RegexParseError.AlternationCantHaveComment, SR.AlternationCantHaveComment);

                            // disallow named capture group (?<..>..) in the condition
                            if (rightchar2 == '\'')
                                throw MakeException(RegexParseError.AlternationCantCapture, SR.AlternationCantCapture);
                            else
                            {
                                if (charsRight >= 4 && (rightchar2 == '<' && RightChar(3) != '!' && RightChar(3) != '='))
                                    throw MakeException(RegexParseError.AlternationCantCapture, SR.AlternationCantCapture);
                            }
                        }

                        break;


                    default:
                        MoveLeft();

                        NodeType = RegexNode.Group;
                        // Disallow options in the children of a testgroup node
                        if (_group.NType != RegexNode.Testgroup)
                            ScanOptions();
                        if (CharsRight() == 0)
                            goto BreakRecognize;

                        if ((ch = RightCharMoveRight()) == ')')
                            return null;

                        if (ch != ':')
                            goto BreakRecognize;
                        break;
                }

                return new RegexNode(NodeType, _options);
            }

        BreakRecognize:
            ;
            // break Recognize comes here

            throw MakeException(RegexParseError.UnrecognizedGrouping, SR.UnrecognizedGrouping);
        }

        /*
         * Scans whitespace or x-mode comments.
         */
        private void ScanBlank()
        {
            if (UseOptionX())
            {
                for (; ;)
                {
                    while (CharsRight() > 0 && IsSpace(RightChar()))
                        MoveRight();

                    if (CharsRight() == 0)
                        break;

                    if (RightChar() == '#')
                    {
                        while (CharsRight() > 0 && RightChar() != '\n')
                            MoveRight();
                    }
                    else if (CharsRight() >= 3 && RightChar(2) == '#' &&
                             RightChar(1) == '?' && RightChar() == '(')
                    {
                        while (CharsRight() > 0 && RightChar() != ')')
                            MoveRight();
                        if (CharsRight() == 0)
                            throw MakeException(RegexParseError.UnterminatedComment, SR.UnterminatedComment);
                        MoveRight();
                    }
                    else
                        break;
                }
            }
            else
            {
                for (; ;)
                {
                    if (CharsRight() < 3 || RightChar(2) != '#' ||
                        RightChar(1) != '?' || RightChar() != '(')
                        return;

                    // skip comment (?# ...)
                    while (CharsRight() > 0 && RightChar() != ')')
                        MoveRight();
                    if (CharsRight() == 0)
                        throw MakeException(RegexParseError.UnterminatedComment, SR.UnterminatedComment);
                    MoveRight();
                }
            }
        }

        /*
         * Scans chars following a '\' (not counting the '\'), and returns
         * a RegexNode for the type of atom scanned.
         */
        private RegexNode ScanBackslash(bool scanOnly)
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
                    if (scanOnly)
                        return null;
                    return new RegexNode(TypeFromCode(ch), _options);

                case 'w':
                    MoveRight();
                    if (scanOnly)
                        return null;
                    if (UseOptionE())
                        return new RegexNode(RegexNode.Set, _options, RegexCharClass.ECMAWordClass);
                    return new RegexNode(RegexNode.Set, _options, RegexCharClass.WordClass);

                case 'W':
                    MoveRight();
                    if (scanOnly)
                        return null;
                    if (UseOptionE())
                        return new RegexNode(RegexNode.Set, _options, RegexCharClass.NotECMAWordClass);
                    return new RegexNode(RegexNode.Set, _options, RegexCharClass.NotWordClass);

                case 's':
                    MoveRight();
                    if (scanOnly)
                        return null;
                    if (UseOptionE())
                        return new RegexNode(RegexNode.Set, _options, RegexCharClass.ECMASpaceClass);
                    return new RegexNode(RegexNode.Set, _options, RegexCharClass.SpaceClass);

                case 'S':
                    MoveRight();
                    if (scanOnly)
                        return null;
                    if (UseOptionE())
                        return new RegexNode(RegexNode.Set, _options, RegexCharClass.NotECMASpaceClass);
                    return new RegexNode(RegexNode.Set, _options, RegexCharClass.NotSpaceClass);

                case 'd':
                    MoveRight();
                    if (scanOnly)
                        return null;
                    if (UseOptionE())
                        return new RegexNode(RegexNode.Set, _options, RegexCharClass.ECMADigitClass);
                    return new RegexNode(RegexNode.Set, _options, RegexCharClass.DigitClass);

                case 'D':
                    MoveRight();
                    if (scanOnly)
                        return null;
                    if (UseOptionE())
                        return new RegexNode(RegexNode.Set, _options, RegexCharClass.NotECMADigitClass);
                    return new RegexNode(RegexNode.Set, _options, RegexCharClass.NotDigitClass);

                case 'p':
                case 'P':
                    MoveRight();
                    if (scanOnly)
                        return null;
                    var cc = new RegexCharClass();
                    cc.AddCategoryFromName(ParseProperty(), (ch != 'p'), UseOptionI(), _pattern, _currentPos);
                    if (UseOptionI())
                        cc.AddLowercase(_culture);

                    return new RegexNode(RegexNode.Set, _options, cc.ToStringClass());

                default:
                    return ScanBasicBackslash(scanOnly);
            }
        }

        /*
         * Scans \-style backreferences and character escapes
         */
        private RegexNode ScanBasicBackslash(bool scanOnly)
        {
            if (CharsRight() == 0)
                throw MakeException(RegexParseError.IllegalEndEscape, SR.IllegalEndEscape);

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
                    throw MakeException(RegexParseError.MalformedNameRef, SR.MalformedNameRef);

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
                    if (scanOnly)
                        return null;
                    if (IsCaptureSlot(capnum))
                        return new RegexNode(RegexNode.Ref, _options, capnum);
                    else
                        throw MakeException(RegexParseError.UndefinedBackref, SR.Format(SR.UndefinedBackref, capnum.ToString()));
                }
            }

            // Try to parse backreference or octal: \1

            else if (!angled && ch >= '1' && ch <= '9')
            {
                if (UseOptionE())
                {
                    int capnum = -1;
                    int newcapnum = (int)(ch - '0');
                    int pos = Textpos() - 1;
                    while (newcapnum <= _captop)
                    {
                        if (IsCaptureSlot(newcapnum) && (_caps == null || (int)_caps[newcapnum] < pos))
                            capnum = newcapnum;
                        MoveRight();
                        if (CharsRight() == 0 || (ch = RightChar()) < '0' || ch > '9')
                            break;
                        newcapnum = newcapnum * 10 + (int)(ch - '0');
                    }
                    if (capnum >= 0)
                        return scanOnly ? null : new RegexNode(RegexNode.Ref, _options, capnum);
                }
                else
                {
                    int capnum = ScanDecimal();
                    if (scanOnly)
                        return null;
                    if (IsCaptureSlot(capnum))
                        return new RegexNode(RegexNode.Ref, _options, capnum);
                    else if (capnum <= 9)
                        throw MakeException(RegexParseError.UndefinedBackref, SR.Format(SR.UndefinedBackref, capnum.ToString()));
                }
            }


            // Try to parse backreference: \<foo>

            else if (angled && RegexCharClass.IsWordChar(ch))
            {
                string capname = ScanCapname();

                if (CharsRight() > 0 && RightCharMoveRight() == close)
                {
                    if (scanOnly)
                        return null;
                    if (IsCaptureName(capname))
                        return new RegexNode(RegexNode.Ref, _options, CaptureSlotFromName(capname));
                    else
                        throw MakeException(RegexParseError.UndefinedNameRef, SR.Format(SR.UndefinedNameRef, capname));
                }
            }

            // Not backreference: must be char code

            Textto(backpos);
            ch = ScanCharEscape();

            if (UseOptionI())
                ch = _culture.TextInfo.ToLower(ch);

            return scanOnly ? null : new RegexNode(RegexNode.One, _options, ch);
        }

        /*
         * Scans $ patterns recognized within replacement patterns
         */
        private RegexNode ScanDollar()
        {
            if (CharsRight() == 0)
                return new RegexNode(RegexNode.One, _options, '$');

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
                    int newcapnum = (int)(ch - '0');
                    MoveRight();
                    if (IsCaptureSlot(newcapnum))
                    {
                        capnum = newcapnum;
                        lastEndPos = Textpos();
                    }

                    while (CharsRight() > 0 && (ch = RightChar()) >= '0' && ch <= '9')
                    {
                        int digit = (int)(ch - '0');
                        if (newcapnum > (MaxValueDiv10) || (newcapnum == (MaxValueDiv10) && digit > (MaxValueMod10)))
                            throw MakeException(RegexParseError.CaptureGroupOutOfRange, SR.CaptureGroupOutOfRange);

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
                        return new RegexNode(RegexNode.Ref, _options, capnum);
                }
                else
                {
                    int capnum = ScanDecimal();
                    if (!angled || CharsRight() > 0 && RightCharMoveRight() == '}')
                    {
                        if (IsCaptureSlot(capnum))
                            return new RegexNode(RegexNode.Ref, _options, capnum);
                    }
                }
            }
            else if (angled && RegexCharClass.IsWordChar(ch))
            {
                string capname = ScanCapname();

                if (CharsRight() > 0 && RightCharMoveRight() == '}')
                {
                    if (IsCaptureName(capname))
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
            int d;
            int i;

            if (c > CharsRight())
                c = CharsRight();

            for (i = 0; c > 0 && unchecked((uint)(d = RightChar() - '0')) <= 7; c -= 1)
            {
                MoveRight();
                i *= 8;
                i += d;
                if (UseOptionE() && i >= 0x20)
                    break;
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

            while (CharsRight() > 0 && unchecked((uint)(d = (char)(RightChar() - '0'))) <= 9)
            {
                MoveRight();

                if (i > (MaxValueDiv10) || (i == (MaxValueDiv10) && d > (MaxValueMod10)))
                    throw MakeException(RegexParseError.CaptureGroupOutOfRange, SR.CaptureGroupOutOfRange);

                i *= 10;
                i += d;
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
                    i *= 0x10;
                    i += d;
                }
            }

            if (c > 0)
                throw MakeException(RegexParseError.TooFewHex, SR.TooFewHex);

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

            if (unchecked((uint)(d = ch - 'a')) <= 5)
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
                throw MakeException(RegexParseError.MissingControl, SR.MissingControl);

            char ch = RightCharMoveRight();

            // \ca interpreted as \cA

            if (ch >= 'a' && ch <= 'z')
                ch = (char)(ch - ('a' - 'A'));

            if (unchecked(ch = (char)(ch - '@')) < ' ')
                return ch;

            throw MakeException(RegexParseError.UnrecognizedControl, SR.UnrecognizedControl);
        }

        /*
         * Returns true for options allowed only at the top level
         */
        private bool IsOnlyTopOption(RegexOptions options)
        {
            return options == RegexOptions.RightToLeft ||
                options == RegexOptions.CultureInvariant ||
                options == RegexOptions.ECMAScript;
        }

        /*
         * Scans cimsx-cimsx option string, stops at the first unrecognized char.
         */
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
                        return;

                    if (off)
                        _options &= ~options;
                    else
                        _options |= options;
                }
            }
        }

        /*
         * Scans \ code for escape codes that map to single Unicode chars.
         */
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
                        throw MakeException(RegexParseError.UnrecognizedEscape, SR.Format(SR.UnrecognizedEscape, ch));
                    return ch;
            }
        }

        /*
         * Scans X for \p{X} or \P{X}
         */
        private string ParseProperty()
        {
            if (CharsRight() < 3)
            {
                throw MakeException(RegexParseError.IncompleteSlashP, SR.IncompleteSlashP);
            }

            char ch = RightCharMoveRight();
            if (ch != '{')
            {
                throw MakeException(RegexParseError.MalformedSlashP, SR.MalformedSlashP);
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
                throw MakeException(RegexParseError.IncompleteSlashP, SR.IncompleteSlashP);

            return capname;
        }

        /*
         * Returns ReNode type for zero-length assertions with a \ code.
         */
        private int TypeFromCode(char ch)
        {
            switch (ch)
            {
                case 'b':
                    return UseOptionE() ? RegexNode.ECMABoundary : RegexNode.Boundary;
                case 'B':
                    return UseOptionE() ? RegexNode.NonECMABoundary : RegexNode.Nonboundary;
                case 'A':
                    return RegexNode.Beginning;
                case 'G':
                    return RegexNode.Start;
                case 'Z':
                    return RegexNode.EndZ;
                case 'z':
                    return RegexNode.End;
                default:
                    return RegexNode.Nothing;
            }
        }

        /*
         * Returns option bit from single-char (?cimsx) code.
         */
        private static RegexOptions OptionFromCode(char ch)
        {
            // case-insensitive
            if (ch >= 'A' && ch <= 'Z')
                ch += (char)('a' - 'A');

            switch (ch)
            {
                case 'i':
                    return RegexOptions.IgnoreCase;
                case 'r':
                    return RegexOptions.RightToLeft;
                case 'm':
                    return RegexOptions.Multiline;
                case 'n':
                    return RegexOptions.ExplicitCapture;
                case 's':
                    return RegexOptions.Singleline;
                case 'x':
                    return RegexOptions.IgnorePatternWhitespace;
#if DEBUG
                case 'd':
                    return RegexOptions.Debug;
#endif
                case 'e':
                    return RegexOptions.ECMAScript;
                default:
                    return 0;
            }
        }

        /*
         * a prescanner for deducing the slots used for
         * captures by doing a partial tokenization of the pattern.
         */
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
                            ScanBackslash(scanOnly: true);
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
                            PopOptions();
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
                                        //if (_ignoreNextParen)
                                        //    throw MakeException(SR.AlternationCantCapture);
                                        if (ch >= '1' && ch <= '9')
                                            NoteCaptureSlot(ScanDecimal(), pos);
                                        else
                                            NoteCaptureName(ScanCapname(), pos);
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
                                // Add unnamend parentheses if ExplicitCapture is not set
                                // and the next parentheses is not ignored.
                                if (!UseOptionN() && !_ignoreNextParen)
                                    NoteCaptureSlot(_autocap++, pos);
                            }
                        }

                        _ignoreNextParen = false;
                        break;
                }
            }

            AssignNameSlots();
        }

        /*
         * Notes a used capture slot
         */
        private void NoteCaptureSlot(int i, int pos)
        {
            if (!_caps.ContainsKey(i))
            {
                // the rhs of the hashtable isn't used in the parser

                _caps.Add(i, pos);
                _capcount++;

                if (_captop <= i)
                {
                    _captop = i == int.MaxValue ? i : i + 1;
                }
            }
        }

        /*
         * Notes a used capture slot
         */
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
                _capnamelist.Add(name);
            }
        }

        /*
         * Assigns unused slot numbers to the capture names
         */
        private void AssignNameSlots()
        {
            if (_capnames != null)
            {
                for (int i = 0; i < _capnamelist.Count; i++)
                {
                    while (IsCaptureSlot(_autocap))
                        _autocap++;
                    string name = _capnamelist[i];
                    int pos = (int)_capnames[name];
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

                Array.Sort(_capnumlist, Comparer<int>.Default);
            }

            // merge capsnumlist into capnamelist

            if (_capnames != null || _capnumlist != null)
            {
                List<string> oldcapnamelist;
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
                    next = (int)_capnames[oldcapnamelist[0]];
                }

                for (int i = 0; i < _capcount; i++)
                {
                    int j = (_capnumlist == null) ? i : _capnumlist[i];

                    if (next == j)
                    {
                        _capnamelist.Add(oldcapnamelist[k++]);
                        next = (k == oldcapnamelist.Count) ? -1 : (int)_capnames[oldcapnamelist[k]];
                    }
                    else
                    {
                        string str = Convert.ToString(j, _culture);
                        _capnamelist.Add(str);
                        _capnames[str] = j;
                    }
                }
            }
        }

        /*
         * Looks up the slot number for a given name
         */
        private int CaptureSlotFromName(string capname)
        {
            return (int)_capnames[capname];
        }

        /*
         * True if the capture slot was noted
         */
        private bool IsCaptureSlot(int i)
        {
            if (_caps != null)
                return _caps.ContainsKey(i);

            return (i >= 0 && i < _capsize);
        }

        /*
         * Looks up the slot number for a given name
         */
        private bool IsCaptureName(string capname)
        {
            if (_capnames == null)
                return false;

            return _capnames.ContainsKey(capname);
        }

        /*
         * True if N option disabling '(' autocapture is on.
         */
        private bool UseOptionN()
        {
            return (_options & RegexOptions.ExplicitCapture) != 0;
        }

        /*
         * True if I option enabling case-insensitivity is on.
         */
        private bool UseOptionI()
        {
            return (_options & RegexOptions.IgnoreCase) != 0;
        }

        /*
         * True if M option altering meaning of $ and ^ is on.
         */
        private bool UseOptionM()
        {
            return (_options & RegexOptions.Multiline) != 0;
        }

        /*
         * True if S option altering meaning of . is on.
         */
        private bool UseOptionS()
        {
            return (_options & RegexOptions.Singleline) != 0;
        }

        /*
         * True if X option enabling whitespace/comment mode is on.
         */
        private bool UseOptionX()
        {
            return (_options & RegexOptions.IgnorePatternWhitespace) != 0;
        }

        /*
         * True if E option enabling ECMAScript behavior is on.
         */
        private bool UseOptionE()
        {
            return (_options & RegexOptions.ECMAScript) != 0;
        }

        private const byte Q = 5;    // quantifier
        private const byte S = 4;    // ordinary stopper
        private const byte Z = 3;    // ScanBlank stopper
        private const byte X = 2;    // whitespace
        private const byte E = 1;    // should be escaped

        /*
         * For categorizing ASCII characters.
        */
        private static readonly byte[] s_category = new byte[] {
            // 0 1 2 3 4 5 6 7 8 9 A B C D E F 0 1 2 3 4 5 6 7 8 9 A B C D E F
               0,0,0,0,0,0,0,0,0,X,X,0,X,X,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
            //   ! " # $ % & ' ( ) * + , - . / 0 1 2 3 4 5 6 7 8 9 : ; < = > ?
               X,0,0,Z,S,0,0,0,S,S,Q,Q,0,0,S,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,Q,
            // @ A B C D E F G H I J K L M N O P Q R S T U V W X Y Z [ \ ] ^ _
               0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,S,S,0,S,0,
            // ' a b c d e f g h i j k l m n o p q r s t u v w x y z { | } ~
               0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,Q,S,0,0,0};

        /*
         * Returns true for those characters that terminate a string of ordinary chars.
         */
        private static bool IsSpecial(char ch)
        {
            return (ch <= '|' && s_category[ch] >= S);
        }

        /*
         * Returns true for those characters that terminate a string of ordinary chars.
         */
        private static bool IsStopperX(char ch)
        {
            return (ch <= '|' && s_category[ch] >= X);
        }

        /*
         * Returns true for those characters that begin a quantifier.
         */
        private static bool IsQuantifier(char ch)
        {
            return (ch <= '{' && s_category[ch] >= Q);
        }

        private bool IsTrueQuantifier()
        {
            Debug.Assert(CharsRight() > 0, "The current reading position must not be at the end of the pattern");

            int startpos = Textpos();
            char ch = CharAt(startpos);
            if (ch != '{')
                return ch <= '{' && s_category[ch] >= Q;

            int pos = startpos;
            int nChars = CharsRight();
            while (--nChars > 0 && (ch = CharAt(++pos)) >= '0' && ch <= '9') ;

            if (nChars == 0 || pos - startpos == 1)
                return false;

            if (ch == '}')
                return true;

            if (ch != ',')
                return false;

            while (--nChars > 0 && (ch = CharAt(++pos)) >= '0' && ch <= '9') ;

            return nChars > 0 && ch == '}';
        }

        /*
         * Returns true for whitespace.
         */
        private static bool IsSpace(char ch)
        {
            return (ch <= ' ' && s_category[ch] == X);
        }

        /*
         * Returns true for chars that should be escaped.
         */
        private static bool IsMetachar(char ch)
        {
            return (ch <= '|' && s_category[ch] >= E);
        }


        /*
         * Add a string to the last concatenate.
         */
        private void AddConcatenate(int pos, int cch, bool isReplacement)
        {
            if (cch == 0)
                return;

            RegexNode node;
            if (cch > 1)
            {
                string str;
                if (UseOptionI() && !isReplacement)
                {
                    str = string.Create(cch, (_pattern, _culture, pos, cch), (span, state) =>
                        state._pattern.AsSpan(state.pos, state.cch).ToLower(span, state._culture));
                }
                else
                {
                    str = _pattern.Substring(pos, cch);
                }

                node = new RegexNode(RegexNode.Multi, _options, str);
            }
            else
            {
                char ch = _pattern[pos];

                if (UseOptionI() && !isReplacement)
                    ch = _culture.TextInfo.ToLower(ch);

                node = new RegexNode(RegexNode.One, _options, ch);
            }

            _concatenation.AddChild(node);
        }

        /*
         * Push the parser state (in response to an open paren)
         */
        private void PushGroup()
        {
            _group.Next = _stack;
            _alternation.Next = _group;
            _concatenation.Next = _alternation;
            _stack = _concatenation;
        }

        /*
         * Remember the pushed state (in response to a ')')
         */
        private void PopGroup()
        {
            _concatenation = _stack;
            _alternation = _concatenation.Next;
            _group = _alternation.Next;
            _stack = _group.Next;

            // The first () inside a Testgroup group goes directly to the group
            if (_group.Type() == RegexNode.Testgroup && _group.ChildCount() == 0)
            {
                if (_unit == null)
                    throw MakeException(RegexParseError.IllegalCondition, SR.IllegalCondition);

                _group.AddChild(_unit);
                _unit = null;
            }
        }

        /*
         * True if the group stack is empty.
         */
        private bool EmptyStack()
        {
            return _stack == null;
        }

        /*
         * Start a new round for the parser state (in response to an open paren or string start)
         */
        private void StartGroup(RegexNode openGroup)
        {
            _group = openGroup;
            _alternation = new RegexNode(RegexNode.Alternate, _options);
            _concatenation = new RegexNode(RegexNode.Concatenate, _options);
        }

        /*
         * Finish the current concatenation (in response to a |)
         */
        private void AddAlternate()
        {
            // The | parts inside a Testgroup group go directly to the group

            if (_group.Type() == RegexNode.Testgroup || _group.Type() == RegexNode.Testref)
            {
                _group.AddChild(_concatenation.ReverseLeft());
            }
            else
            {
                _alternation.AddChild(_concatenation.ReverseLeft());
            }

            _concatenation = new RegexNode(RegexNode.Concatenate, _options);
        }

        /*
         * Finish the current quantifiable (when a quantifier is not found or is not possible)
         */
        private void AddConcatenate()
        {
            // The first (| inside a Testgroup group goes directly to the group

            _concatenation.AddChild(_unit);
            _unit = null;
        }

        /*
         * Finish the current quantifiable (when a quantifier is found)
         */
        private void AddConcatenate(bool lazy, int min, int max)
        {
            _concatenation.AddChild(_unit.MakeQuantifier(lazy, min, max));
            _unit = null;
        }

        /*
         * Returns the current unit
         */
        private RegexNode Unit()
        {
            return _unit;
        }

        /*
         * Sets the current unit to a single char node
         */
        private void AddUnitOne(char ch)
        {
            if (UseOptionI())
                ch = _culture.TextInfo.ToLower(ch);

            _unit = new RegexNode(RegexNode.One, _options, ch);
        }

        /*
         * Sets the current unit to a single inverse-char node
         */
        private void AddUnitNotone(char ch)
        {
            if (UseOptionI())
                ch = _culture.TextInfo.ToLower(ch);

            _unit = new RegexNode(RegexNode.Notone, _options, ch);
        }

        /*
         * Sets the current unit to a single set node
         */
        private void AddUnitSet(string cc)
        {
            _unit = new RegexNode(RegexNode.Set, _options, cc);
        }

        /*
         * Sets the current unit to a subtree
         */
        private void AddUnitNode(RegexNode node)
        {
            _unit = node;
        }

        /*
         * Sets the current unit to an assertion of the specified type
         */
        private void AddUnitType(int type)
        {
            _unit = new RegexNode(type, _options);
        }

        /*
         * Finish the current group (in response to a ')' or end)
         */
        private void AddGroup()
        {
            if (_group.Type() == RegexNode.Testgroup || _group.Type() == RegexNode.Testref)
            {
                _group.AddChild(_concatenation.ReverseLeft());

                if (_group.Type() == RegexNode.Testref && _group.ChildCount() > 2 || _group.ChildCount() > 3)
                    throw MakeException(RegexParseError.TooManyAlternates, SR.TooManyAlternates);
            }
            else
            {
                _alternation.AddChild(_concatenation.ReverseLeft());
                _group.AddChild(_alternation);
            }

            _unit = _group;
        }

        /*
         * Saves options on a stack.
         */
        private void PushOptions()
        {
            _optionsStack.Append(_options);
        }

        /*
         * Recalls options from the stack.
         */
        private void PopOptions()
        {
            _options = _optionsStack.Pop();
        }

        /*
         * True if options stack is empty.
         */
        private bool EmptyOptionsStack()
        {
            return _optionsStack.Length == 0;
        }

        /*
         * Pops the options stack, but keeps the current options unchanged.
         */
        private void PopKeepOptions()
        {
            _optionsStack.Length--;
        }

        /*
         * Fills in a RegexParseException
         */
        private RegexParseException MakeException(RegexParseError error, string message)
        {
            return new RegexParseException(error, _currentPos, SR.Format(SR.MakeException, _pattern, _currentPos, message));
        }

        /*
         * Returns the current parsing position.
         */
        private int Textpos()
        {
            return _currentPos;
        }

        /*
         * Zaps to a specific parsing position.
         */
        private void Textto(int pos)
        {
            _currentPos = pos;
        }

        /*
         * Returns the char at the right of the current parsing position and advances to the right.
         */
        private char RightCharMoveRight()
        {
            return _pattern[_currentPos++];
        }

        /*
         * Moves the current position to the right.
         */
        private void MoveRight()
        {
            MoveRight(1);
        }

        private void MoveRight(int i)
        {
            _currentPos += i;
        }

        /*
         * Moves the current parsing position one to the left.
         */
        private void MoveLeft()
        {
            --_currentPos;
        }

        /*
         * Returns the char left of the current parsing position.
         */
        private char CharAt(int i)
        {
            return _pattern[i];
        }

        /*
         * Returns the char right of the current parsing position.
         */
        internal char RightChar()
        {
            return _pattern[_currentPos];
        }

        /*
         * Returns the char i chars right of the current parsing position.
         */
        private char RightChar(int i)
        {
            return _pattern[_currentPos + i];
        }

        /*
         * Number of characters to the right of the current parsing position.
         */
        private int CharsRight()
        {
            return _pattern.Length - _currentPos;
        }
    }
}
