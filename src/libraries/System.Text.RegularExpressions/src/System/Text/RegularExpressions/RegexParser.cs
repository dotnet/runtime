// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
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
        private int _pos;
        private readonly CultureInfo _culture;
        private RegexCaseBehavior _caseBehavior;
        private bool _hasIgnoreCaseBackreferenceNodes;

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
            _caseBehavior = default;
            _hasIgnoreCaseBackreferenceNodes = false;
            _caps = caps;
            _capsize = capsize;
            _capnames = capnames;

            _optionsStack = new ValueListBuilder<int>(optionSpan);
            _stack = null;
            _group = null;
            _alternation = null;
            _concatenation = null;
            _unit = null;
            _pos = 0;
            _autocap = 0;
            _capcount = 0;
            _captop = 0;
            _capnumlist = null;
            _capnamelist = null;
            _ignoreNextParen = false;
        }

        /// <summary>Gets the culture to use based on the specified options.</summary>
        internal static CultureInfo GetTargetCulture(RegexOptions options) =>
#pragma warning disable RS1035 // The symbol 'CultureInfo.CurrentCulture' is banned for use by analyzers.
            (options & RegexOptions.CultureInvariant) != 0 ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture;
#pragma warning restore RS1035

        public static RegexOptions ParseOptionsInPattern(string pattern, RegexOptions options)
        {
            using var parser = new RegexParser(pattern, options, CultureInfo.InvariantCulture, // since we won't perform case conversions, culture doesn't matter in this case.
                new Hashtable(), 0, null, stackalloc int[OptionStackDefaultSize]);

            // We don't really need to Count the Captures, but this method will already do a quick
            // pass through the pattern, and will scan the options found and return them as an out
            // parameter, so we use that to get out the pattern inline options.
            parser.CountCaptures(out RegexOptions foundOptionsInPattern);
            parser.Reset(options);
            return foundOptionsInPattern;
        }

        public static RegexTree Parse(string pattern, RegexOptions options, CultureInfo culture)
        {
            using var parser = new RegexParser(pattern, options, culture, new Hashtable(), 0, null, stackalloc int[OptionStackDefaultSize]);

            parser.CountCaptures(out _);
            parser.Reset(options);
            RegexNode root = parser.ScanRegex();

            int[]? captureNumberList = parser._capnumlist;
            Hashtable? sparseMapping = parser._caps;
            int captop = parser._captop;

            int captureCount;
            if (captureNumberList == null || captop == captureNumberList.Length)
            {
                // The capture list isn't sparse.  Null out the capture mapping as it's not necessary,
                // and store the number of captures.
                captureCount = captop;
                sparseMapping = null;
            }
            else
            {
                // The capture list is sparse.  Store the number of captures, and populate the number-to-names-list.
                captureCount = captureNumberList.Length;
                for (int i = 0; i < captureNumberList.Length; i++)
                {
                    sparseMapping[captureNumberList[i]] = i;
                }
            }

            return new RegexTree(root, captureCount, parser._capnamelist?.ToArray(), parser._capnames!, sparseMapping, options, parser._hasIgnoreCaseBackreferenceNodes ? culture : null);
        }

        /// <summary>This static call constructs a flat concatenation node given a replacement pattern.</summary>
        public static RegexReplacement ParseReplacement(string pattern, RegexOptions options, Hashtable caps, int capsize, Hashtable capnames)
        {
#pragma warning disable RS1035 // The symbol 'CultureInfo.CurrentCulture' is banned for use by analyzers.
            CultureInfo culture = (options & RegexOptions.CultureInvariant) != 0 ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture;
#pragma warning restore RS1035
            using var parser = new RegexParser(pattern, options, culture, caps, capsize, capnames, stackalloc int[OptionStackDefaultSize]);

            RegexNode root = parser.ScanReplacement();
            var regexReplacement = new RegexReplacement(pattern, root, caps);

            return regexReplacement;
        }

        /// <summary>Escapes all metacharacters (including |,(,),[,{,|,^,$,*,+,?,\, spaces and #)</summary>
        public static string Escape(string input)
        {
            int indexOfMetachar = IndexOfMetachar(input.AsSpan());
            return indexOfMetachar < 0
                ? input
                : EscapeImpl(input.AsSpan(), indexOfMetachar);
        }

        private static string EscapeImpl(ReadOnlySpan<char> input, int indexOfMetachar)
        {
            // For small inputs we allocate on the stack. In most cases a buffer three
            // times larger the original string should be sufficient as usually not all
            // characters need to be encoded.
            // For larger string we rent the input string's length plus a fixed
            // conservative amount of chars from the ArrayPool.
            ValueStringBuilder vsb = input.Length <= (EscapeMaxBufferSize / 3) ?
                new ValueStringBuilder(stackalloc char[EscapeMaxBufferSize]) :
                new ValueStringBuilder(input.Length + 200);

            while (true)
            {
                vsb.Append(input.Slice(0, indexOfMetachar));
                input = input.Slice(indexOfMetachar);

                if (input.IsEmpty)
                {
                    break;
                }

                char ch = input[0];

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

                vsb.Append('\\');
                vsb.Append(ch);
                input = input.Slice(1);

                indexOfMetachar = IndexOfMetachar(input);
                if (indexOfMetachar < 0)
                {
                    indexOfMetachar = input.Length;
                }
            }

            return vsb.ToString();
        }

        /// <summary> Unescapes all metacharacters (including (,),[,],{,},|,^,$,*,+,?,\, spaces and #)</summary>
        public static string Unescape(string input)
        {
            int i = input.IndexOf('\\');
            return i >= 0 ?
                UnescapeImpl(input, i) :
                input;
        }

        private static string UnescapeImpl(string input, int i)
        {
            var parser = new RegexParser(input, RegexOptions.None, CultureInfo.InvariantCulture, new Hashtable(), 0, null, stackalloc int[OptionStackDefaultSize]);

            // In the worst case the escaped string has the same length.
            // For small inputs we use stack allocation.
            ValueStringBuilder vsb = input.Length <= EscapeMaxBufferSize ?
                new ValueStringBuilder(stackalloc char[EscapeMaxBufferSize]) :
                new ValueStringBuilder(input.Length);

            vsb.Append(input.AsSpan(0, i));
            do
            {
                i++;
                parser._pos = i;
                if (i < input.Length)
                {
                    vsb.Append(parser.ScanCharEscape());
                }

                i = parser._pos;
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

        /// <summary>Resets parsing to the beginning of the pattern</summary>
        private void Reset(RegexOptions options)
        {
            _pos = 0;
            _autocap = 1;
            _ignoreNextParen = false;
            _optionsStack.Length = 0;
            _options = options;
            _stack = null;
        }

        public void Dispose() => _optionsStack.Dispose();

        /// <summary>The main parsing function</summary>
        private RegexNode ScanRegex()
        {
            char ch;
            bool isQuantifier = false;

            // For the main Capture object, strip out the IgnoreCase option. The rest of the nodes will strip it out depending on the content
            // of each node.
            StartGroup(new RegexNode(RegexNodeKind.Capture, (_options & ~RegexOptions.IgnoreCase), 0, -1));

            while (_pos < _pattern.Length)
            {
                bool wasPrevQuantifier = isQuantifier;
                isQuantifier = false;

                ScanBlank();

                int startpos = _pos;

                // move past all of the normal characters.  We'll stop when we hit some kind of control character,
                // or if IgnorePatternWhiteSpace is on, we'll stop when we see some whitespace.
                if ((_options & RegexOptions.IgnorePatternWhitespace) != 0)
                {
                    while (_pos < _pattern.Length && (!IsSpecialOrSpace(ch = _pattern[_pos]) || (ch == '{' && !IsTrueQuantifier())))
                        _pos++;
                }
                else
                {
                    while (_pos < _pattern.Length && (!IsSpecial(ch = _pattern[_pos]) || (ch == '{' && !IsTrueQuantifier())))
                        _pos++;
                }

                int endpos = _pos;

                ScanBlank();

                if (_pos == _pattern.Length)
                {
                    ch = '!'; // nonspecial, means at end
                }
                else if (IsSpecial(ch = _pattern[_pos]))
                {
                    isQuantifier = IsQuantifier(ch);
                    _pos++;
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
                        AddToConcatenate(startpos, cchUnquantified, false);
                    }

                    if (isQuantifier)
                    {
                        _unit = RegexNode.CreateOneWithCaseConversion(_pattern[endpos - 1], _options, _culture, ref _caseBehavior);
                    }
                }

                switch (ch)
                {
                    case '!':
                        goto BreakOuterScan;

                    case ' ':
                        goto ContinueOuterScan;

                    case '[':
                        {
                            string setString = ScanCharClass((_options & RegexOptions.IgnoreCase) != 0, scanOnly: false)!.ToStringClass();
                            _unit = new RegexNode(RegexNodeKind.Set, _options & ~RegexOptions.IgnoreCase, setString);
                        }
                        break;

                    case '(':
                        _optionsStack.Append((int)_options);
                        if (ScanGroupOpen() is RegexNode grouper)
                        {
                            PushGroup();
                            StartGroup(grouper);
                        }
                        else
                        {
                            _optionsStack.Length--;
                        }
                        continue;

                    case '|':
                        AddAlternate();
                        goto ContinueOuterScan;

                    case ')':
                        if (_stack == null)
                        {
                            throw MakeException(RegexParseError.InsufficientOpeningParentheses, SR.InsufficientOpeningParentheses);
                        }

                        AddGroup();
                        PopGroup();
                        _options = (RegexOptions)_optionsStack.Pop();

                        if (_unit == null)
                        {
                            goto ContinueOuterScan;
                        }
                        break;

                    case '\\':
                        if (_pos == _pattern.Length)
                        {
                            throw MakeException(RegexParseError.UnescapedEndingBackslash, SR.UnescapedEndingBackslash);
                        }

                        _unit = ScanBackslash(scanOnly: false)!;
                        break;

                    case '^':
                        _unit = new RegexNode((_options & RegexOptions.Multiline) != 0 ? RegexNodeKind.Bol : RegexNodeKind.Beginning, _options);
                        break;

                    case '$':
                        _unit = new RegexNode((_options & RegexOptions.Multiline) != 0 ? RegexNodeKind.Eol : RegexNodeKind.EndZ, _options);
                        break;

                    case '.':
                        _unit = (_options & RegexOptions.Singleline) != 0 ?
                            new RegexNode(RegexNodeKind.Set, _options & ~RegexOptions.IgnoreCase, RegexCharClass.AnyClass) :
                            new RegexNode(RegexNodeKind.Notone, _options & ~RegexOptions.IgnoreCase, '\n');
                        break;

                    case '{':
                    case '*':
                    case '+':
                    case '?':
                        if (_unit == null)
                        {
                            throw wasPrevQuantifier ?
                                MakeException(RegexParseError.NestedQuantifiersNotParenthesized, SR.Format(SR.NestedQuantifiersNotParenthesized, ch)) :
                                MakeException(RegexParseError.QuantifierAfterNothing, SR.Format(SR.QuantifierAfterNothing, ch));
                        }
                        --_pos;
                        break;

                    default:
                        Debug.Fail($"Unexpected char {ch}");
                        break;
                }

                ScanBlank();

                if (_pos == _pattern.Length || !(isQuantifier = IsTrueQuantifier()))
                {
                    _concatenation!.AddChild(_unit!);
                    _unit = null;
                    goto ContinueOuterScan;
                }

                ch = _pattern[_pos++];

                // Handle quantifiers
                while (_unit != null)
                {
                    int min = 0, max = 0;

                    switch (ch)
                    {
                        case '*':
                            max = int.MaxValue;
                            break;

                        case '?':
                            max = 1;
                            break;

                        case '+':
                            min = 1;
                            max = int.MaxValue;
                            break;

                        case '{':
                            startpos = _pos;
                            max = min = ScanDecimal();
                            if (startpos < _pos)
                            {
                                if (_pos < _pattern.Length && _pattern[_pos] == ',')
                                {
                                    _pos++;
                                    max = _pos == _pattern.Length || _pattern[_pos] == '}' ? int.MaxValue : ScanDecimal();
                                }
                            }

                            if (startpos == _pos || _pos == _pattern.Length || _pattern[_pos++] != '}')
                            {
                                _concatenation!.AddChild(_unit!);
                                _unit = null;
                                _pos = startpos - 1;
                                goto ContinueOuterScan;
                            }

                            break;

                        default:
                            Debug.Fail($"Unexpected char {ch}");
                            break;
                    }

                    ScanBlank();

                    bool lazy = false;
                    if (_pos < _pattern.Length && _pattern[_pos] == '?')
                    {
                        _pos++;
                        lazy = true;
                    }

                    if (min > max)
                    {
                        throw MakeException(RegexParseError.ReversedQuantifierRange, SR.ReversedQuantifierRange);
                    }

                    _concatenation!.AddChild(_unit!.MakeQuantifier(lazy, min, max));
                    _unit = null;
                }

            ContinueOuterScan:
                ;
            }

        BreakOuterScan:
            ;

            if (_stack != null)
            {
                throw MakeException(RegexParseError.InsufficientClosingParentheses, SR.InsufficientClosingParentheses);
            }

            AddGroup();

            return _unit!.FinalOptimize();
        }

        /// <summary>Simple parsing for replacement patterns</summary>
        private RegexNode ScanReplacement()
        {
            _concatenation = new RegexNode(RegexNodeKind.Concatenate, _options);

            while (_pos < _pattern.Length)
            {
                int startpos = _pos;

                _pos = _pattern.IndexOf('$', _pos);
                if (_pos < 0)
                {
                    _pos = _pattern.Length;
                }

                AddToConcatenate(startpos, _pos - startpos, isReplacement: true);

                if (_pos < _pattern.Length)
                {
                    _pos++;
                    _concatenation.AddChild(ScanDollar());
                    _unit = null;
                }
            }

            return _concatenation;
        }

        /// <summary>Scans contents of [] (not including []'s), and converts to a RegexCharClass</summary>
        private RegexCharClass? ScanCharClass(bool caseInsensitive, bool scanOnly)
        {
            char ch;
            char chPrev = '\0';
            bool inRange = false;
            bool firstChar = true;
            bool closed = false;

            RegexCharClass? charClass = scanOnly ? null : new RegexCharClass();

            if (_pos < _pattern.Length && _pattern[_pos] == '^')
            {
                _pos++;
                if (!scanOnly)
                {
                    charClass!.Negate = true;
                }
                if ((_options & RegexOptions.ECMAScript) != 0 && _pattern[_pos] == ']')
                {
                    firstChar = false;
                }
            }

            for (; _pos < _pattern.Length; firstChar = false)
            {
                bool translatedChar = false;
                ch = _pattern[_pos++];
                if (ch == ']')
                {
                    if (!firstChar)
                    {
                        closed = true;
                        break;
                    }
                }
                else if (ch == '\\' && _pos < _pattern.Length)
                {
                    switch (ch = _pattern[_pos++])
                    {
                        case 'D':
                        case 'd':
                            if (!scanOnly)
                            {
                                if (inRange)
                                {
                                    throw MakeException(RegexParseError.ShorthandClassInCharacterRange, SR.Format(SR.ShorthandClassInCharacterRange, ch));
                                }
                                charClass!.AddDigit((_options & RegexOptions.ECMAScript) != 0, ch == 'D', _pattern, _pos);
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
                                charClass!.AddSpace((_options & RegexOptions.ECMAScript) != 0, ch == 'S');
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

                                charClass!.AddWord((_options & RegexOptions.ECMAScript) != 0, ch == 'W');
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

                                charClass!.AddCategoryFromName(ParseProperty(), ch != 'p', caseInsensitive, _pattern, _pos);
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
                            --_pos;
                            ch = ScanCharEscape(); // non-literal character
                            translatedChar = true;
                            break; // this break will only break out of the switch
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

                            if (_pos < _pattern.Length && _pattern[_pos] != ']')
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
                else if (_pos + 1 < _pattern.Length && _pattern[_pos] == '-' && _pattern[_pos + 1] != ']')
                {
                    // this could be the start of a range
                    chPrev = ch;
                    inRange = true;
                    _pos++;
                }
                else if (_pos < _pattern.Length && ch == '-' && !translatedChar && _pattern[_pos] == '[' && !firstChar)
                {
                    // we aren't in a range, and now there is a subtraction.  Usually this happens
                    // only when a subtraction follows a range, like [a-z-[b]]
                    _pos++;
                    RegexCharClass? rcc = ScanCharClass(caseInsensitive, scanOnly);
                    if (!scanOnly)
                    {
                        charClass!.AddSubtraction(rcc!);

                        if (_pos < _pattern.Length && _pattern[_pos] != ']')
                        {
                            throw MakeException(RegexParseError.ExclusionGroupNotLast, SR.ExclusionGroupNotLast);
                        }
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
                charClass!.AddCaseEquivalences(_culture);
            }

            return charClass;
        }

        /// <summary>
        /// Scans chars following a '(' (not counting the '('), and returns
        /// a RegexNode for the type of group scanned, or null if the group
        /// simply changed options (?cimsx-cimsx) or was a comment (#...).
        /// </summary>
        private RegexNode? ScanGroupOpen()
        {
            // just return a RegexNode if we have:
            // 1. "(" followed by nothing
            // 2. "(x" where x != ?
            // 3. "(?)"
            if (_pos == _pattern.Length || _pattern[_pos] != '?' || (_pos + 1 < _pattern.Length && _pattern[_pos + 1] == ')'))
            {
                if ((_options & RegexOptions.ExplicitCapture) != 0 || _ignoreNextParen)
                {
                    _ignoreNextParen = false;
                    return new RegexNode(RegexNodeKind.Group, _options);
                }
                else
                {
                    return new RegexNode(RegexNodeKind.Capture, _options, _autocap++, -1);
                }
            }

            _pos++;

            while (true)
            {
                if (_pos == _pattern.Length)
                {
                    break;
                }

                RegexNodeKind nodeType;
                char close = '>';
                char ch = _pattern[_pos++];
                switch (ch)
                {
                    case ':':
                        // noncapturing group
                        nodeType = RegexNodeKind.Group;
                        break;

                    case '=':
                        // lookahead assertion
                        _options &= ~RegexOptions.RightToLeft;
                        nodeType = RegexNodeKind.PositiveLookaround;
                        break;

                    case '!':
                        // negative lookahead assertion
                        _options &= ~RegexOptions.RightToLeft;
                        nodeType = RegexNodeKind.NegativeLookaround;
                        break;

                    case '>':
                        // atomic subexpression
                        nodeType = RegexNodeKind.Atomic;
                        break;

                    case '\'':
                        close = '\'';
                        goto case '<'; // fallthrough

                    case '<':
                        if (_pos == _pattern.Length)
                        {
                            goto BreakRecognize;
                        }

                        switch (ch = _pattern[_pos++])
                        {
                            case '=':
                                if (close == '\'')
                                {
                                    goto BreakRecognize;
                                }

                                // lookbehind assertion
                                _options |= RegexOptions.RightToLeft;
                                nodeType = RegexNodeKind.PositiveLookaround;
                                break;

                            case '!':
                                if (close == '\'')
                                {
                                    goto BreakRecognize;
                                }

                                // negative lookbehind assertion
                                _options |= RegexOptions.RightToLeft;
                                nodeType = RegexNodeKind.NegativeLookaround;
                                break;

                            default:
                                --_pos;
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
                                    if (_pos < _pattern.Length && !(_pattern[_pos] == close || _pattern[_pos] == '-'))
                                    {
                                        throw MakeException(RegexParseError.CaptureGroupNameInvalid, SR.CaptureGroupNameInvalid);
                                    }

                                    if (capnum == 0)
                                    {
                                        throw MakeException(RegexParseError.CaptureGroupOfZero, SR.CaptureGroupOfZero);
                                    }
                                }
                                else if (RegexCharClass.IsBoundaryWordChar(ch))
                                {
                                    string capname = ScanCapname();

                                    if (_capnames?[capname] is int tmpCapnum)
                                    {
                                        capnum = tmpCapnum;
                                    }

                                    // check if we have bogus character after the name
                                    if (_pos < _pattern.Length && !(_pattern[_pos] == close || _pattern[_pos] == '-'))
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

                                if ((capnum != -1 || proceed) && _pos + 1 < _pattern.Length && _pattern[_pos] == '-')
                                {
                                    _pos++;
                                    ch = _pattern[_pos];

                                    if ((uint)(ch - '0') <= 9)
                                    {
                                        uncapnum = ScanDecimal();

                                        if (!IsCaptureSlot(uncapnum))
                                        {
                                            throw MakeException(RegexParseError.UndefinedNumberedReference, SR.Format(SR.UndefinedNumberedReference, uncapnum));
                                        }

                                        // check if we have bogus characters after the number
                                        if (_pos < _pattern.Length && _pattern[_pos] != close)
                                        {
                                            throw MakeException(RegexParseError.CaptureGroupNameInvalid, SR.CaptureGroupNameInvalid);
                                        }
                                    }
                                    else if (RegexCharClass.IsBoundaryWordChar(ch))
                                    {
                                        string uncapname = ScanCapname();

                                        uncapnum = _capnames?[uncapname] is int tmpCapnum ?
                                            tmpCapnum :
                                            throw MakeException(RegexParseError.UndefinedNamedReference, SR.Format(SR.UndefinedNamedReference, uncapname));

                                        // check if we have bogus character after the name
                                        if (_pos < _pattern.Length && _pattern[_pos] != close)
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

                                if ((capnum != -1 || uncapnum != -1) && _pos < _pattern.Length && _pattern[_pos++] == close)
                                {
                                    return new RegexNode(RegexNodeKind.Capture, _options, capnum, uncapnum);
                                }
                                goto BreakRecognize;
                        }
                        break;

                    case '(':
                        // conditional alternation construct (?(...) | )

                        int parenPos = _pos;
                        if (_pos < _pattern.Length)
                        {
                            ch = _pattern[_pos];

                            // check if the alternation condition is a backref
                            if (ch is >= '0' and <= '9')
                            {
                                int capnum = ScanDecimal();
                                if (_pos < _pattern.Length && _pattern[_pos++] == ')')
                                {
                                    if (IsCaptureSlot(capnum))
                                    {
                                        return new RegexNode(RegexNodeKind.BackreferenceConditional, _options, capnum);
                                    }

                                    throw MakeException(RegexParseError.AlternationHasUndefinedReference, SR.Format(SR.AlternationHasUndefinedReference, capnum.ToString()));
                                }

                                throw MakeException(RegexParseError.AlternationHasMalformedReference, SR.Format(SR.AlternationHasMalformedReference, capnum.ToString()));
                            }
                            else if (RegexCharClass.IsBoundaryWordChar(ch))
                            {
                                string capname = ScanCapname();

                                if (_capnames?[capname] is int tmpCapnum && _pos < _pattern.Length && _pattern[_pos++] == ')')
                                {
                                    return new RegexNode(RegexNodeKind.BackreferenceConditional, _options, tmpCapnum);
                                }
                            }
                        }
                        // not a backref
                        nodeType = RegexNodeKind.ExpressionConditional;
                        _pos = parenPos - 1;       // jump to the start of the parentheses
                        _ignoreNextParen = true;    // but make sure we don't try to capture the insides

                        if (_pos + 2 < _pattern.Length && _pattern[_pos + 1] == '?')
                        {
                            // disallow comments in the condition
                            if (_pattern[_pos + 2] == '#')
                            {
                                throw MakeException(RegexParseError.AlternationHasComment, SR.AlternationHasComment);
                            }

                            // disallow named capture group (?<..>..) in the condition
                            if (_pattern[_pos + 2] == '\'' || (_pos + 3 < _pattern.Length && _pattern[_pos + 2] == '<' && _pattern[_pos + 3] != '!' && _pattern[_pos + 3] != '='))
                            {
                                throw MakeException(RegexParseError.AlternationHasNamedCapture, SR.AlternationHasNamedCapture);
                            }
                        }

                        break;

                    default:
                        --_pos;

                        nodeType = RegexNodeKind.Group;
                        // Disallow options in the children of a testgroup node
                        if (_group!.Kind != RegexNodeKind.ExpressionConditional)
                        {
                            ScanOptions();
                        }

                        if (_pos == _pattern.Length)
                        {
                            goto BreakRecognize;
                        }

                        if ((ch = _pattern[_pos++]) == ')')
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

        /// <summary>Scans whitespace or x-mode comments</summary>
        private void ScanBlank()
        {
            while (true)
            {
                if ((_options & RegexOptions.IgnorePatternWhitespace) != 0)
                {
                    while (_pos < _pattern.Length && IsSpace(_pattern[_pos]))
                    {
                        _pos++;
                    }
                }

                if ((_options & RegexOptions.IgnorePatternWhitespace) != 0 && _pos < _pattern.Length && _pattern[_pos] == '#')
                {
                    _pos = _pattern.IndexOf('\n', _pos);
                    if (_pos < 0)
                    {
                        _pos = _pattern.Length;
                    }
                }
                else if (_pos + 2 < _pattern.Length && _pattern[_pos + 2] == '#' && _pattern[_pos + 1] == '?' && _pattern[_pos] == '(')
                {
                    _pos = _pattern.IndexOf(')', _pos);
                    if (_pos < 0)
                    {
                        _pos = _pattern.Length;
                        throw MakeException(RegexParseError.UnterminatedComment, SR.UnterminatedComment);
                    }

                    _pos++;
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>Scans chars following a '\' (not counting the '\'), and returns a RegexNode for the type of atom scanned</summary>
        private RegexNode? ScanBackslash(bool scanOnly)
        {
            Debug.Assert(_pos < _pattern.Length, "The current reading position must not be at the end of the pattern");

            char ch;
            switch (ch = _pattern[_pos])
            {
                case 'b':
                case 'B':
                case 'A':
                case 'G':
                case 'Z':
                case 'z':
                    _pos++;
                    return scanOnly ? null :
                        new RegexNode(TypeFromCode(ch), _options);

                case 'w':
                    _pos++;
                    return scanOnly ? null :
                        new RegexNode(RegexNodeKind.Set, (_options & ~RegexOptions.IgnoreCase), (_options & RegexOptions.ECMAScript) != 0 ? RegexCharClass.ECMAWordClass : RegexCharClass.WordClass);

                case 'W':
                    _pos++;
                    return scanOnly ? null :
                        new RegexNode(RegexNodeKind.Set, (_options & ~RegexOptions.IgnoreCase), (_options & RegexOptions.ECMAScript) != 0 ? RegexCharClass.NotECMAWordClass : RegexCharClass.NotWordClass);

                case 's':
                    _pos++;
                    return scanOnly ? null :
                        new RegexNode(RegexNodeKind.Set, (_options & ~RegexOptions.IgnoreCase), (_options & RegexOptions.ECMAScript) != 0 ? RegexCharClass.ECMASpaceClass : RegexCharClass.SpaceClass);

                case 'S':
                    _pos++;
                    return scanOnly ? null :
                        new RegexNode(RegexNodeKind.Set, (_options & ~RegexOptions.IgnoreCase), (_options & RegexOptions.ECMAScript) != 0 ? RegexCharClass.NotECMASpaceClass : RegexCharClass.NotSpaceClass);

                case 'd':
                    _pos++;
                    return scanOnly ? null :
                        new RegexNode(RegexNodeKind.Set, (_options & ~RegexOptions.IgnoreCase), (_options & RegexOptions.ECMAScript) != 0 ? RegexCharClass.ECMADigitClass : RegexCharClass.DigitClass);

                case 'D':
                    _pos++;
                    return scanOnly ? null :
                        new RegexNode(RegexNodeKind.Set, (_options & ~RegexOptions.IgnoreCase), (_options & RegexOptions.ECMAScript) != 0 ? RegexCharClass.NotECMADigitClass : RegexCharClass.NotDigitClass);

                case 'p':
                case 'P':
                    _pos++;
                    if (scanOnly)
                    {
                        return null;
                    }

                    var cc = new RegexCharClass();
                    cc.AddCategoryFromName(ParseProperty(), ch != 'p', (_options & RegexOptions.IgnoreCase) != 0, _pattern, _pos);
                    if ((_options & RegexOptions.IgnoreCase) != 0)
                    {
                        cc.AddCaseEquivalences(_culture);
                    }

                    return new RegexNode(RegexNodeKind.Set, (_options & ~RegexOptions.IgnoreCase), cc.ToStringClass());

                default:
                    RegexNode? result = ScanBasicBackslash(scanOnly);
                    if (result != null && result.Kind == RegexNodeKind.Backreference && (result.Options & RegexOptions.IgnoreCase) != 0)
                    {
                        _hasIgnoreCaseBackreferenceNodes = true;
                    }
                    return result;
            }
        }

        /// <summary>Scans \-style backreferences and character escapes</summary>
        private RegexNode? ScanBasicBackslash(bool scanOnly)
        {
            Debug.Assert(_pos < _pattern.Length, "The current reading position must not be at the end of the pattern");

            int backpos = _pos;
            char close = '\0';
            bool angled = false;
            char ch = _pattern[_pos];

            // allow \k<foo> instead of \<foo>, which is now deprecated

            if (ch == 'k')
            {
                if (_pos + 1 < _pattern.Length)
                {
                    _pos++;
                    ch = _pattern[_pos++];
                    if (ch is '<' or '\'')
                    {
                        angled = true;
                        close = (ch == '\'') ? '\'' : '>';
                    }
                }

                if (!angled || _pos == _pattern.Length)
                {
                    throw MakeException(RegexParseError.MalformedNamedReference, SR.MalformedNamedReference);
                }

                ch = _pattern[_pos];
            }

            // Note angle without \g

            else if ((ch == '<' || ch == '\'') && _pos + 1 < _pattern.Length)
            {
                angled = true;
                close = (ch == '\'') ? '\'' : '>';
                _pos++;
                ch = _pattern[_pos];
            }

            // Try to parse backreference: \<1>

            if (angled && ch >= '0' && ch <= '9')
            {
                int capnum = ScanDecimal();

                if (_pos < _pattern.Length && _pattern[_pos++] == close)
                {
                    return
                        scanOnly ? null :
                        IsCaptureSlot(capnum) ? new RegexNode(RegexNodeKind.Backreference, _options, capnum) :
                        throw MakeException(RegexParseError.UndefinedNumberedReference, SR.Format(SR.UndefinedNumberedReference, capnum.ToString()));
                }
            }

            // Try to parse backreference or octal: \1

            else if (!angled && ch >= '1' && ch <= '9')
            {
                if ((_options & RegexOptions.ECMAScript) != 0)
                {
                    int capnum = -1;
                    int newcapnum = ch - '0';
                    int pos = _pos - 1;
                    while (newcapnum <= _captop)
                    {
                        if (IsCaptureSlot(newcapnum) && (_caps == null || (int)_caps[newcapnum]! < pos))
                        {
                            capnum = newcapnum;
                        }

                        _pos++;
                        if (_pos == _pattern.Length || (ch = _pattern[_pos]) < '0' || ch > '9')
                        {
                            break;
                        }

                        newcapnum = newcapnum * 10 + (ch - '0');
                    }

                    if (capnum >= 0)
                    {
                        return scanOnly ? null : new RegexNode(RegexNodeKind.Backreference, _options, capnum);
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
                        return new RegexNode(RegexNodeKind.Backreference, _options, capnum);
                    }

                    if (capnum <= 9)
                    {
                        throw MakeException(RegexParseError.UndefinedNumberedReference, SR.Format(SR.UndefinedNumberedReference, capnum.ToString()));
                    }
                }
            }

            // Try to parse backreference: \<foo>

            else if (angled && RegexCharClass.IsBoundaryWordChar(ch))
            {
                string capname = ScanCapname();

                if (_pos < _pattern.Length && _pattern[_pos++] == close)
                {
                    return
                        scanOnly ? null :
                        _capnames?[capname] is int tmpCapnum ? new RegexNode(RegexNodeKind.Backreference, _options, tmpCapnum) :
                        throw MakeException(RegexParseError.UndefinedNamedReference, SR.Format(SR.UndefinedNamedReference, capname));
                }
            }

            // Not backreference: must be char code

            _pos = backpos;
            ch = ScanCharEscape();

            return !scanOnly ?
                RegexNode.CreateOneWithCaseConversion(ch, _options, _culture, ref _caseBehavior) :
                null;
        }

        /// <summary>Scans $ patterns recognized within replacement patterns</summary>
        private RegexNode ScanDollar()
        {
            if (_pos == _pattern.Length)
            {
                return RegexNode.CreateOneWithCaseConversion('$', _options, _culture, ref _caseBehavior);
            }

            char ch = _pattern[_pos];
            bool angled;
            int backpos = _pos;
            int lastEndPos = backpos;

            // Note angle

            if (ch == '{' && _pos + 1 < _pattern.Length)
            {
                angled = true;
                _pos++;
                ch = _pattern[_pos];
            }
            else
            {
                angled = false;
            }

            // Try to parse backreference: \1 or \{1} or \{cap}

            if (ch is >= '0' and <= '9')
            {
                if (!angled && (_options & RegexOptions.ECMAScript) != 0)
                {
                    int capnum = -1;
                    int newcapnum = ch - '0';
                    _pos++;
                    if (IsCaptureSlot(newcapnum))
                    {
                        capnum = newcapnum;
                        lastEndPos = _pos;
                    }

                    while (_pos < _pattern.Length && (ch = _pattern[_pos]) >= '0' && ch <= '9')
                    {
                        int digit = ch - '0';
                        if (newcapnum > MaxValueDiv10 || (newcapnum == MaxValueDiv10 && digit > MaxValueMod10))
                        {
                            throw MakeException(RegexParseError.QuantifierOrCaptureGroupOutOfRange, SR.QuantifierOrCaptureGroupOutOfRange);
                        }

                        newcapnum = newcapnum * 10 + digit;

                        _pos++;
                        if (IsCaptureSlot(newcapnum))
                        {
                            capnum = newcapnum;
                            lastEndPos = _pos;
                        }
                    }
                    _pos = lastEndPos;
                    if (capnum >= 0)
                    {
                        return new RegexNode(RegexNodeKind.Backreference, _options, capnum);
                    }
                }
                else
                {
                    int capnum = ScanDecimal();
                    if (!angled || _pos < _pattern.Length && _pattern[_pos++] == '}')
                    {
                        if (IsCaptureSlot(capnum))
                        {
                            return new RegexNode(RegexNodeKind.Backreference, _options, capnum);
                        }
                    }
                }
            }
            else if (angled && RegexCharClass.IsBoundaryWordChar(ch))
            {
                string capname = ScanCapname();
                if (_pos < _pattern.Length && _pattern[_pos++] == '}')
                {
                    if (_capnames?[capname] is int tmpCapnum)
                    {
                        return new RegexNode(RegexNodeKind.Backreference, _options, tmpCapnum);
                    }
                }
            }
            else if (!angled)
            {
                int capnum = 1;

                switch (ch)
                {
                    case '$':
                        _pos++;
                        return RegexNode.CreateOneWithCaseConversion('$', _options, _culture, ref _caseBehavior);

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
                    _pos++;
                    return new RegexNode(RegexNodeKind.Backreference, _options, capnum);
                }
            }

            // unrecognized $: literalize

            _pos = backpos;
            return RegexNode.CreateOneWithCaseConversion('$', _options, _culture, ref _caseBehavior);
        }

        /// <summary>Scans a capture name: consumes word chars</summary>
        private string ScanCapname()
        {
            int startpos = _pos;

            while (_pos < _pattern.Length)
            {
                if (!RegexCharClass.IsBoundaryWordChar(_pattern[_pos++]))
                {
                    --_pos;
                    break;
                }
            }

            return _pattern.Substring(startpos, _pos - startpos);
        }

        /// <summary>Scans up to three octal digits (stops before exceeding 0377)</summary>
        private char ScanOctal()
        {
            // Consume octal chars only up to 3 digits and value 0377
            int c = Math.Min(3, _pattern.Length - _pos);
            int d;
            int i;
            for (i = 0; c > 0 && (uint)(d = _pattern[_pos] - '0') <= 7; c -= 1)
            {
                _pos++;
                i = (i * 8) + d;
                if ((_options & RegexOptions.ECMAScript) != 0 && i >= 0x20)
                {
                    break;
                }
            }

            // Octal codes only go up to 255.  Any larger and the behavior that Perl follows
            // is simply to truncate the high bits.
            i &= 0xFF;

            return (char)i;
        }

        /// <summary>Scans any number of decimal digits (pegs value at 2^31-1 if too large)</summary>
        private int ScanDecimal()
        {
            int i = 0;
            int d;

            while (_pos < _pattern.Length && (uint)(d = (char)(_pattern[_pos] - '0')) <= 9)
            {
                _pos++;

                if (i > MaxValueDiv10 || (i == MaxValueDiv10 && d > MaxValueMod10))
                {
                    throw MakeException(RegexParseError.QuantifierOrCaptureGroupOutOfRange, SR.QuantifierOrCaptureGroupOutOfRange);
                }

                i = (i * 10) + d;
            }

            return i;
        }

        /// <summary>Scans exactly c hex digits (c=2 for \xFF, c=4 for \uFFFF)</summary>
        private char ScanHex(int c)
        {
            int i = 0;

            if (_pos + c <= _pattern.Length)
            {
                for (; c > 0; c -= 1)
                {
                    char ch = _pattern[_pos++];
                    int result = HexConverter.FromChar(ch);
                    if (result == 0xFF)
                    {
                        break;
                    }

                    i = (i * 0x10) + result;
                }
            }

            if (c > 0)
            {
                throw MakeException(RegexParseError.InsufficientOrInvalidHexDigits, SR.InsufficientOrInvalidHexDigits);
            }

            return (char)i;
        }

        /// <summary>Grabs and converts an ASCII control character</summary>
        private char ScanControl()
        {
            if (_pos == _pattern.Length)
            {
                throw MakeException(RegexParseError.MissingControlCharacter, SR.MissingControlCharacter);
            }

            char ch = _pattern[_pos++];

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

        /// <summary>Scans cimsx-cimsx option string, stops at the first unrecognized char.</summary>
        private void ScanOptions()
        {
            for (bool off = false; _pos < _pattern.Length; _pos++)
            {
                char ch = _pattern[_pos];

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
                    RegexOptions options = (char)(ch | 0x20) switch
                    {
                        'i' => RegexOptions.IgnoreCase,
                        'm' => RegexOptions.Multiline,
                        'n' => RegexOptions.ExplicitCapture,
                        's' => RegexOptions.Singleline,
                        'x' => RegexOptions.IgnorePatternWhitespace,
                        _ => RegexOptions.None,
                    };
                    if (options == 0)
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
            char ch = _pattern[_pos++];

            if (ch is >= '0' and <= '7')
            {
                --_pos;
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
                    if ((_options & RegexOptions.ECMAScript) == 0 && RegexCharClass.IsBoundaryWordChar(ch))
                    {
                        throw MakeException(RegexParseError.UnrecognizedEscape, SR.Format(SR.UnrecognizedEscape, ch));
                    }
                    return ch;
            }
        }

        /// <summary>Scans X for \p{X} or \P{X}</summary>
        private string ParseProperty()
        {
            if (_pos + 2 >= _pattern.Length)
            {
                throw MakeException(RegexParseError.InvalidUnicodePropertyEscape, SR.InvalidUnicodePropertyEscape);
            }

            char ch = _pattern[_pos++];
            if (ch != '{')
            {
                throw MakeException(RegexParseError.MalformedUnicodePropertyEscape, SR.MalformedUnicodePropertyEscape);
            }

            int startpos = _pos;
            while (_pos < _pattern.Length)
            {
                ch = _pattern[_pos++];
                if (!(RegexCharClass.IsBoundaryWordChar(ch) || ch == '-'))
                {
                    --_pos;
                    break;
                }
            }

            string capname = _pattern.Substring(startpos, _pos - startpos);

            if (_pos == _pattern.Length || _pattern[_pos++] != '}')
            {
                throw MakeException(RegexParseError.InvalidUnicodePropertyEscape, SR.InvalidUnicodePropertyEscape);
            }

            return capname;
        }

        /// <summary>Returns the node kind for zero-length assertions with a \ code.</summary>
        private readonly RegexNodeKind TypeFromCode(char ch) =>
            ch switch
            {
                'b' => (_options & RegexOptions.ECMAScript) != 0 ? RegexNodeKind.ECMABoundary : RegexNodeKind.Boundary,
                'B' => (_options & RegexOptions.ECMAScript) != 0 ? RegexNodeKind.NonECMABoundary : RegexNodeKind.NonBoundary,
                'A' => RegexNodeKind.Beginning,
                'G' => RegexNodeKind.Start,
                'Z' => RegexNodeKind.EndZ,
                'z' => RegexNodeKind.End,
                _ => RegexNodeKind.Nothing,
            };

        /// <summary>
        /// A prescanner for deducing the slots used for captures by doing a partial tokenization of the pattern.
        /// </summary>
        private void CountCaptures(out RegexOptions optionsFoundInPattern)
        {
            NoteCaptureSlot(0, 0);
            optionsFoundInPattern = RegexOptions.None;
            _autocap = 1;

            while (_pos < _pattern.Length)
            {
                int pos = _pos;
                char ch = _pattern[_pos++];
                switch (ch)
                {
                    case '\\':
                        if (_pos < _pattern.Length)
                        {
                            ScanBackslash(scanOnly: true);
                        }
                        break;

                    case '#':
                        if ((_options & RegexOptions.IgnorePatternWhitespace) != 0)
                        {
                            --_pos;
                            ScanBlank();
                        }
                        break;

                    case '[':
                        ScanCharClass(caseInsensitive: false, scanOnly: true);
                        break;

                    case ')':
                        if (_optionsStack.Length != 0)
                        {
                            _options = (RegexOptions)_optionsStack.Pop();
                        }
                        break;

                    case '(':
                        if (_pos + 1 < _pattern.Length && _pattern[_pos + 1] == '#' && _pattern[_pos] == '?')
                        {
                            // we have a comment (?#
                            --_pos;
                            ScanBlank();
                        }
                        else
                        {
                            _optionsStack.Append((int)_options);
                            if (_pos < _pattern.Length && _pattern[_pos] == '?')
                            {
                                // we have (?...
                                _pos++;

                                if (_pos + 1 < _pattern.Length && (_pattern[_pos] == '<' || _pattern[_pos] == '\''))
                                {
                                    // named group: (?<... or (?'...

                                    _pos++;
                                    ch = _pattern[_pos];

                                    if (ch != '0' && RegexCharClass.IsBoundaryWordChar(ch))
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
                                    optionsFoundInPattern |= _options;

                                    if (_pos < _pattern.Length)
                                    {
                                        if (_pattern[_pos] == ')')
                                        {
                                            // (?cimsx-cimsx)
                                            _pos++;
                                            _optionsStack.Length--;
                                        }
                                        else if (_pattern[_pos] == '(')
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
                                if ((_options & RegexOptions.ExplicitCapture) == 0 && !_ignoreNextParen)
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

        /// <summary>True if the capture slot was noted</summary>
        private readonly bool IsCaptureSlot(int i)
        {
            if (_caps != null)
            {
                return _caps.ContainsKey(i);
            }

            return i >= 0 && i < _capsize;
        }

        /// <summary>
        /// When generating code on a regex that uses a sparse set
        /// of capture slots, we hash them to a dense set of indices
        /// for an array of capture slots. Instead of doing the hash
        /// at match time, it's done at compile time, here.
        /// </summary>
        internal static int MapCaptureNumber(int capnum, Hashtable? caps) =>
            capnum == -1 ? -1 :
            caps != null ? (int)caps[capnum]! :
            capnum;

        private const byte Q = 4;    // quantifier          * + ? {
        private const byte S = 3;    // stopper             $ ( ) . [ \ ^ |
        private const byte Z = 2;    // # stopper           #
        private const byte W = 1;    // whitespace          \t \n \f \r ' '

        /// <summary>For categorizing ASCII characters.</summary>
        private static ReadOnlySpan<byte> Category =>
        [
            // 0  1  2  3  4  5  6  7  8  9  A  B  C  D  E  F  0  1  2  3  4  5  6  7  8  9  A  B  C  D  E  F
               0, 0, 0, 0, 0, 0, 0, 0, 0, W, W, 0, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            //    !  "  #  $  %  &  '  (  )  *  +  ,  -  .  /  0  1  2  3  4  5  6  7  8  9  :  ;  <  =  >  ?
               W, 0, 0, Z, S, 0, 0, 0, S, S, Q, Q, 0, 0, S, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, Q,
            // @  A  B  C  D  E  F  G  H  I  J  K  L  M  N  O  P  Q  R  S  T  U  V  W  X  Y  Z  [  \  ]  ^  _
               0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, S, S, 0, S, 0,
            // '  a  b  c  d  e  f  g  h  i  j  k  l  m  n  o  p  q  r  s  t  u  v  w  x  y  z  {  |  }  ~
               0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, Q, S, 0, 0, 0
        ];

#if NET8_0_OR_GREATER
        private static readonly SearchValues<char> s_metachars =
            SearchValues.Create("\t\n\f\r #$()*+.?[\\^{|");

        private static int IndexOfMetachar(ReadOnlySpan<char> input) =>
            input.IndexOfAny(s_metachars);
#else
        private static int IndexOfMetachar(ReadOnlySpan<char> input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] <= '|' && Category[input[i]] > 0)
                {
                    return i;
                }
            }

            return -1;
        }
#endif

        /// <summary>Returns true for those characters that terminate a string of ordinary chars.</summary>
        private static bool IsSpecial(char ch) => ch <= '|' && Category[ch] >= S;

        /// <summary>Returns true for those characters including whitespace that terminate a string of ordinary chars.</summary>
        private static bool IsSpecialOrSpace(char ch) => ch <= '|' && Category[ch] >= W;

        /// <summary>Returns true for those characters that begin a quantifier.</summary>
        private static bool IsQuantifier(char ch) => ch <= '{' && Category[ch] == Q;

        /// <summary>Returns true for whitespace.</summary>
        private static bool IsSpace(char ch) => ch <= ' ' && Category[ch] == W;

        private readonly bool IsTrueQuantifier()
        {
            Debug.Assert(_pos < _pattern.Length, "The current reading position must not be at the end of the pattern");

            int startpos = _pos;
            char ch = _pattern[startpos];
            if (ch != '{')
            {
                return ch <= '{' && Category[ch] >= Q;
            }

            int pos = startpos;
            int nChars = _pattern.Length - _pos;
            while (--nChars > 0 && (uint)((ch = _pattern[++pos]) - '0') <= 9) ;

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

            while (--nChars > 0 && (uint)((ch = _pattern[++pos]) - '0') <= 9) ;

            return nChars > 0 && ch == '}';
        }

        /// <summary>Add a string to the last concatenate.</summary>
        private void AddToConcatenate(int pos, int cch, bool isReplacement)
        {
            switch (cch)
            {
                case 0:
                    return;

                case 1:
                    _concatenation!.AddChild(RegexNode.CreateOneWithCaseConversion(_pattern[pos], isReplacement ? _options & ~RegexOptions.IgnoreCase : _options, _culture, ref _caseBehavior));
                    break;

                case > 1 when (_options & RegexOptions.IgnoreCase) == 0 || isReplacement || !RegexCharClass.ParticipatesInCaseConversion(_pattern.AsSpan(pos, cch)):
                    _concatenation!.AddChild(new RegexNode(RegexNodeKind.Multi, _options & ~RegexOptions.IgnoreCase, _pattern.Substring(pos, cch)));
                    break;

                default:
                    foreach (char c in _pattern.AsSpan(pos, cch))
                    {
                        _concatenation!.AddChild(RegexNode.CreateOneWithCaseConversion(c, _options, _culture, ref _caseBehavior));
                    }
                    break;
            }
        }

        /// <summary>Push the parser state (in response to an open paren)</summary>
        private void PushGroup()
        {
            _group!.Parent = _stack;
            _alternation!.Parent = _group;
            _concatenation!.Parent = _alternation;
            _stack = _concatenation;
        }

        /// <summary>Remember the pushed state (in response to a ')')</summary>
        private void PopGroup()
        {
            _concatenation = _stack;
            _alternation = _concatenation!.Parent;
            _group = _alternation!.Parent;
            _stack = _group!.Parent;

            // The first () inside a Testgroup group goes directly to the group
            if (_group.Kind == RegexNodeKind.ExpressionConditional && _group.ChildCount() == 0)
            {
                if (_unit == null)
                {
                    throw MakeException(RegexParseError.AlternationHasMalformedCondition, SR.AlternationHasMalformedCondition);
                }

                _group.AddChild(_unit);
                _unit = null;
            }
        }

        /// <summary>Start a new round for the parser state (in response to an open paren or string start)</summary>
        private void StartGroup(RegexNode openGroup)
        {
            _group = openGroup;
            _alternation = new RegexNode(RegexNodeKind.Alternate, _options);
            _concatenation = new RegexNode(RegexNodeKind.Concatenate, _options);
        }

        /// <summary>Finish the current concatenation (in response to a |)</summary>
        private void AddAlternate()
        {
            // The | parts inside a Testgroup group go directly to the group

            if (_group!.Kind is RegexNodeKind.ExpressionConditional or RegexNodeKind.BackreferenceConditional)
            {
                _group.AddChild(_concatenation!.ReverseConcatenationIfRightToLeft());
            }
            else
            {
                _alternation!.AddChild(_concatenation!.ReverseConcatenationIfRightToLeft());
            }

            _concatenation = new RegexNode(RegexNodeKind.Concatenate, _options);
        }

        /// <summary>Finish the current group (in response to a ')' or end)</summary>
        private void AddGroup()
        {
            if (_group!.Kind is RegexNodeKind.ExpressionConditional or RegexNodeKind.BackreferenceConditional)
            {
                _group.AddChild(_concatenation!.ReverseConcatenationIfRightToLeft());

                if (_group.Kind == RegexNodeKind.BackreferenceConditional && _group.ChildCount() > 2 || _group.ChildCount() > 3)
                {
                    throw MakeException(RegexParseError.AlternationHasTooManyConditions, SR.AlternationHasTooManyConditions);
                }
            }
            else
            {
                _alternation!.AddChild(_concatenation!.ReverseConcatenationIfRightToLeft());
                _group.AddChild(_alternation);
            }

            _unit = _group;
        }

        /// <summary>Fills in a RegexParseException</summary>
        private readonly RegexParseException MakeException(RegexParseError error, string message) =>
            new RegexParseException(error, _pos, SR.Format(SR.MakeException, _pattern, _pos, message));

        /// <summary>Gets group name from its number.</summary>
        internal static string GroupNameFromNumber(Hashtable? caps, string[]? capslist, int capsize, int i)
        {
            if (capslist is null)
            {
                if ((uint)i < (uint)capsize)
                {
                    return ((uint)i).ToString();
                }
            }
            else
            {
                if ((caps is null || caps.TryGetValue(i, out i)) &&
                    (uint)i < (uint)capslist.Length)
                {
                    return capslist[i];
                }
            }

            return string.Empty;
        }
    }
}
