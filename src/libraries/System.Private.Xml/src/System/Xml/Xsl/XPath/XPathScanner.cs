// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// <spec>http://www.w3.org/TR/xpath#exprlex</spec>
//------------------------------------------------------------------------------

using System.Diagnostics;

namespace System.Xml.Xsl.XPath
{
    // Extends XPathOperator enumeration
    internal enum LexKind
    {
        Unknown,        // Unknown lexeme
        Or,             // Operator 'or'
        And,            // Operator 'and'
        Eq,             // Operator '='
        Ne,             // Operator '!='
        Lt,             // Operator '<'
        Le,             // Operator '<='
        Gt,             // Operator '>'
        Ge,             // Operator '>='
        Plus,           // Operator '+'
        Minus,          // Operator '-'
        Multiply,       // Operator '*'
        Divide,         // Operator 'div'
        Modulo,         // Operator 'mod'
        UnaryMinus,     // Not used
        Union,          // Operator '|'
        LastOperator = Union,

        DotDot,         // '..'
        ColonColon,     // '::'
        SlashSlash,     // Operator '//'
        Number,         // Number (numeric literal)
        Axis,           // AxisName

        Name,           // NameTest, NodeType, FunctionName, AxisName, second part of VariableReference
        String,         // Literal (string literal)
        Eof,            // End of the expression

        FirstStringable = Name,
        LastNonChar = Eof,

        LParens = '(',
        RParens = ')',
        LBracket = '[',
        RBracket = ']',
        Dot = '.',
        At = '@',
        Comma = ',',

        Star = '*',      // NameTest
        Slash = '/',      // Operator '/'
        Dollar = '$',      // First part of VariableReference
        RBrace = '}',      // Used for AVTs
    };

    internal sealed class XPathScanner
    {
        private string _xpathExpr;
        private int _curIndex;
        private char _curChar;
        private LexKind _kind;
        private string _name;
        private string _prefix;
        private string _stringValue;
        private bool _canBeFunction;
        private int _lexStart;
        private int _prevLexEnd;
        private LexKind _prevKind;
        private XPathAxis _axis;

        private XmlCharType _xmlCharType = XmlCharType.Instance;

        public XPathScanner(string xpathExpr) : this(xpathExpr, 0) { }

        public XPathScanner(string xpathExpr, int startFrom)
        {
            Debug.Assert(xpathExpr != null);
            _xpathExpr = xpathExpr;
            _kind = LexKind.Unknown;
            SetSourceIndex(startFrom);
            NextLex();
        }

        public string Source { get { return _xpathExpr; } }
        public LexKind Kind { get { return _kind; } }
        public int LexStart { get { return _lexStart; } }
        public int LexSize { get { return _curIndex - _lexStart; } }
        public int PrevLexEnd { get { return _prevLexEnd; } }

        private void SetSourceIndex(int index)
        {
            Debug.Assert(0 <= index && index <= _xpathExpr.Length);
            _curIndex = index - 1;
            NextChar();
        }

        private void NextChar()
        {
            Debug.Assert(-1 <= _curIndex && _curIndex < _xpathExpr.Length);
            _curIndex++;
            if (_curIndex < _xpathExpr.Length)
            {
                _curChar = _xpathExpr[_curIndex];
            }
            else
            {
                Debug.Assert(_curIndex == _xpathExpr.Length);
                _curChar = '\0';
            }
        }

#if XML10_FIFTH_EDITION
        private char PeekNextChar() {
            Debug.Assert(-1 <= curIndex && curIndex <= xpathExpr.Length);
            if (curIndex + 1 < xpathExpr.Length) {
                return xpathExpr[curIndex + 1];
            }
            else {
                return '\0';
            }
        }
#endif

        public string Name
        {
            get
            {
                Debug.Assert(_kind == LexKind.Name);
                Debug.Assert(_name != null);
                return _name;
            }
        }

        public string Prefix
        {
            get
            {
                Debug.Assert(_kind == LexKind.Name);
                Debug.Assert(_prefix != null);
                return _prefix;
            }
        }

        public string RawValue
        {
            get
            {
                if (_kind == LexKind.Eof)
                {
                    return LexKindToString(_kind);
                }
                else
                {
                    return _xpathExpr.Substring(_lexStart, _curIndex - _lexStart);
                }
            }
        }

        public string StringValue
        {
            get
            {
                Debug.Assert(_kind == LexKind.String);
                Debug.Assert(_stringValue != null);
                return _stringValue;
            }
        }

        // Returns true if the character following an QName (possibly after intervening
        // ExprWhitespace) is '('. In this case the token must be recognized as a NodeType
        // or a FunctionName unless it is an OperatorName. This distinction cannot be done
        // without knowing the previous lexeme. For example, "or" in "... or (1 != 0)" may
        // be an OperatorName or a FunctionName.
        public bool CanBeFunction
        {
            get
            {
                Debug.Assert(_kind == LexKind.Name);
                return _canBeFunction;
            }
        }

        public XPathAxis Axis
        {
            get
            {
                Debug.Assert(_kind == LexKind.Axis);
                Debug.Assert(_axis != XPathAxis.Unknown);
                return _axis;
            }
        }

        private void SkipSpace()
        {
            while (_xmlCharType.IsWhiteSpace(_curChar))
            {
                NextChar();
            }
        }

        private static bool IsAsciiDigit(char ch)
        {
            return unchecked((uint)(ch - '0')) <= 9;
        }

        public void NextLex()
        {
            _prevLexEnd = _curIndex;
            _prevKind = _kind;
            SkipSpace();
            _lexStart = _curIndex;

            switch (_curChar)
            {
                case '\0':
                    _kind = LexKind.Eof;
                    return;
                case '(':
                case ')':
                case '[':
                case ']':
                case '@':
                case ',':
                case '$':
                case '}':
                    _kind = (LexKind)_curChar;
                    NextChar();
                    break;
                case '.':
                    NextChar();
                    if (_curChar == '.')
                    {
                        _kind = LexKind.DotDot;
                        NextChar();
                    }
                    else if (IsAsciiDigit(_curChar))
                    {
                        SetSourceIndex(_lexStart);
                        goto case '0';
                    }
                    else
                    {
                        _kind = LexKind.Dot;
                    }
                    break;
                case ':':
                    NextChar();
                    if (_curChar == ':')
                    {
                        _kind = LexKind.ColonColon;
                        NextChar();
                    }
                    else
                    {
                        _kind = LexKind.Unknown;
                    }
                    break;
                case '*':
                    _kind = LexKind.Star;
                    NextChar();
                    CheckOperator(true);
                    break;
                case '/':
                    NextChar();
                    if (_curChar == '/')
                    {
                        _kind = LexKind.SlashSlash;
                        NextChar();
                    }
                    else
                    {
                        _kind = LexKind.Slash;
                    }
                    break;
                case '|':
                    _kind = LexKind.Union;
                    NextChar();
                    break;
                case '+':
                    _kind = LexKind.Plus;
                    NextChar();
                    break;
                case '-':
                    _kind = LexKind.Minus;
                    NextChar();
                    break;
                case '=':
                    _kind = LexKind.Eq;
                    NextChar();
                    break;
                case '!':
                    NextChar();
                    if (_curChar == '=')
                    {
                        _kind = LexKind.Ne;
                        NextChar();
                    }
                    else
                    {
                        _kind = LexKind.Unknown;
                    }
                    break;
                case '<':
                    NextChar();
                    if (_curChar == '=')
                    {
                        _kind = LexKind.Le;
                        NextChar();
                    }
                    else
                    {
                        _kind = LexKind.Lt;
                    }
                    break;
                case '>':
                    NextChar();
                    if (_curChar == '=')
                    {
                        _kind = LexKind.Ge;
                        NextChar();
                    }
                    else
                    {
                        _kind = LexKind.Gt;
                    }
                    break;
                case '"':
                case '\'':
                    _kind = LexKind.String;
                    ScanString();
                    break;
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    _kind = LexKind.Number;
                    ScanNumber();
                    break;
                default:
                    if (_xmlCharType.IsStartNCNameSingleChar(_curChar)
#if XML10_FIFTH_EDITION
                        || xmlCharType.IsNCNameHighSurrogateChar(curChar)
#endif
                        )
                    {
                        _kind = LexKind.Name;
                        _name = ScanNCName();
                        _prefix = string.Empty;
                        _canBeFunction = false;
                        _axis = XPathAxis.Unknown;
                        bool colonColon = false;
                        int saveSourceIndex = _curIndex;

                        // "foo:bar" or "foo:*" -- one lexeme (no spaces allowed)
                        // "foo::" or "foo ::"  -- two lexemes, reported as one (AxisName)
                        // "foo:?" or "foo :?"  -- lexeme "foo" reported
                        if (_curChar == ':')
                        {
                            NextChar();
                            if (_curChar == ':')
                            {   // "foo::" -> OperatorName, AxisName
                                NextChar();
                                colonColon = true;
                                SetSourceIndex(saveSourceIndex);
                            }
                            else
                            {                // "foo:bar", "foo:*" or "foo:?"
                                if (_curChar == '*')
                                {
                                    NextChar();
                                    _prefix = _name;
                                    _name = "*";
                                }
                                else if (_xmlCharType.IsStartNCNameSingleChar(_curChar)
#if XML10_FIFTH_EDITION
                                    || xmlCharType.IsNCNameHighSurrogateChar(curChar)
#endif
                                    )
                                {
                                    _prefix = _name;
                                    _name = ScanNCName();
                                    // Look ahead for '(' to determine whether QName can be a FunctionName
                                    saveSourceIndex = _curIndex;
                                    SkipSpace();
                                    _canBeFunction = (_curChar == '(');
                                    SetSourceIndex(saveSourceIndex);
                                }
                                else
                                {            // "foo:?" -> OperatorName, NameTest
                                    // Return "foo" and leave ":" to be reported later as an unknown lexeme
                                    SetSourceIndex(saveSourceIndex);
                                }
                            }
                        }
                        else
                        {
                            SkipSpace();
                            if (_curChar == ':')
                            {   // "foo ::" or "foo :?"
                                NextChar();
                                if (_curChar == ':')
                                {
                                    NextChar();
                                    colonColon = true;
                                }
                                SetSourceIndex(saveSourceIndex);
                            }
                            else
                            {
                                _canBeFunction = (_curChar == '(');
                            }
                        }
                        if (!CheckOperator(false) && colonColon)
                        {
                            _axis = CheckAxis();
                        }
                    }
                    else
                    {
                        _kind = LexKind.Unknown;
                        NextChar();
                    }
                    break;
            }
        }

        private bool CheckOperator(bool star)
        {
            LexKind opKind;

            if (star)
            {
                opKind = LexKind.Multiply;
            }
            else
            {
                if (_prefix.Length != 0 || _name.Length > 3)
                    return false;

                switch (_name)
                {
                    case "or": opKind = LexKind.Or; break;
                    case "and": opKind = LexKind.And; break;
                    case "div": opKind = LexKind.Divide; break;
                    case "mod": opKind = LexKind.Modulo; break;
                    default: return false;
                }
            }

            // If there is a preceding token and the preceding token is not one of '@', '::', '(', '[', ',' or an Operator,
            // then a '*' must be recognized as a MultiplyOperator and an NCName must be recognized as an OperatorName.
            if (_prevKind <= LexKind.LastOperator)
                return false;

            switch (_prevKind)
            {
                case LexKind.Slash:
                case LexKind.SlashSlash:
                case LexKind.At:
                case LexKind.ColonColon:
                case LexKind.LParens:
                case LexKind.LBracket:
                case LexKind.Comma:
                case LexKind.Dollar:
                    return false;
            }

            _kind = opKind;
            return true;
        }

        private XPathAxis CheckAxis()
        {
            _kind = LexKind.Axis;
            switch (_name)
            {
                case "ancestor": return XPathAxis.Ancestor;
                case "ancestor-or-self": return XPathAxis.AncestorOrSelf;
                case "attribute": return XPathAxis.Attribute;
                case "child": return XPathAxis.Child;
                case "descendant": return XPathAxis.Descendant;
                case "descendant-or-self": return XPathAxis.DescendantOrSelf;
                case "following": return XPathAxis.Following;
                case "following-sibling": return XPathAxis.FollowingSibling;
                case "namespace": return XPathAxis.Namespace;
                case "parent": return XPathAxis.Parent;
                case "preceding": return XPathAxis.Preceding;
                case "preceding-sibling": return XPathAxis.PrecedingSibling;
                case "self": return XPathAxis.Self;
                default: _kind = LexKind.Name; return XPathAxis.Unknown;
            }
        }

        private void ScanNumber()
        {
            Debug.Assert(IsAsciiDigit(_curChar) || _curChar == '.');
            while (IsAsciiDigit(_curChar))
            {
                NextChar();
            }
            if (_curChar == '.')
            {
                NextChar();
                while (IsAsciiDigit(_curChar))
                {
                    NextChar();
                }
            }
            if ((_curChar & (~0x20)) == 'E')
            {
                NextChar();
                if (_curChar == '+' || _curChar == '-')
                {
                    NextChar();
                }
                while (IsAsciiDigit(_curChar))
                {
                    NextChar();
                }
                throw CreateException(SR.XPath_ScientificNotation);
            }
        }

        private void ScanString()
        {
            int startIdx = _curIndex + 1;
            int endIdx = _xpathExpr.IndexOf(_curChar, startIdx);

            if (endIdx < 0)
            {
                SetSourceIndex(_xpathExpr.Length);
                throw CreateException(SR.XPath_UnclosedString);
            }

            _stringValue = _xpathExpr.Substring(startIdx, endIdx - startIdx);
            SetSourceIndex(endIdx + 1);
        }

        private string ScanNCName()
        {
            Debug.Assert(_xmlCharType.IsStartNCNameSingleChar(_curChar)
#if XML10_FIFTH_EDITION
                || xmlCharType.IsNCNameHighSurrogateChar(curChar)
#endif
                );
            int start = _curIndex;
            for (;;)
            {
                if (_xmlCharType.IsNCNameSingleChar(_curChar))
                {
                    NextChar();
                }
#if XML10_FIFTH_EDITION
                else if (xmlCharType.IsNCNameSurrogateChar(PeekNextChar(), curChar)) {
                    NextChar();
                    NextChar();
                }
#endif
                else
                {
                    break;
                }
            }
            return _xpathExpr.Substring(start, _curIndex - start);
        }

        public void PassToken(LexKind t)
        {
            CheckToken(t);
            NextLex();
        }

        public void CheckToken(LexKind t)
        {
            Debug.Assert(LexKind.FirstStringable <= t);
            if (_kind != t)
            {
                if (t == LexKind.Eof)
                {
                    throw CreateException(SR.XPath_EofExpected, RawValue);
                }
                else
                {
                    throw CreateException(SR.XPath_TokenExpected, LexKindToString(t), RawValue);
                }
            }
        }

        // May be called for the following tokens: Name, String, Eof, Comma, LParens, RParens, LBracket, RBracket, RBrace
        private string LexKindToString(LexKind t)
        {
            Debug.Assert(LexKind.FirstStringable <= t);

            if (LexKind.LastNonChar < t)
            {
                Debug.Assert("()[].@,*/$}".IndexOf((char)t) >= 0);
                return new String((char)t, 1);
            }

            switch (t)
            {
                case LexKind.Name: return "<name>";
                case LexKind.String: return "<string literal>";
                case LexKind.Eof: return "<eof>";
                default:
                    Debug.Fail("Unexpected LexKind: " + t.ToString());
                    return string.Empty;
            }
        }

        public XPathCompileException CreateException(string resId, params string[] args)
        {
            return new XPathCompileException(_xpathExpr, _lexStart, _curIndex, resId, args);
        }
    }
}
