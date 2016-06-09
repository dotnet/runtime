// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MS.Internal.Xml.XPath
{
    using System;
    using System.Xml;
    using System.Xml.XPath;
    using System.Diagnostics;
    using System.Globalization;
    using System.Text;
    using System.Collections;

    internal sealed class XPathScanner
    {
        private string _xpathExpr;
        private int _xpathExprIndex;
        private LexKind _kind;
        private char _currentChar;
        private string _name;
        private string _prefix;
        private string _stringValue;
        private double _numberValue = double.NaN;
        private bool _canBeFunction;
        private XmlCharType _xmlCharType = XmlCharType.Instance;

        public XPathScanner(string xpathExpr)
        {
            if (xpathExpr == null)
            {
                throw XPathException.Create(Res.Xp_ExprExpected, string.Empty);
            }
            _xpathExpr = xpathExpr;
            NextChar();
            NextLex();
        }

        public string SourceText { get { return _xpathExpr; } }

        private char CurerntChar { get { return _currentChar; } }

        private bool NextChar()
        {
            Debug.Assert(0 <= _xpathExprIndex && _xpathExprIndex <= _xpathExpr.Length);
            if (_xpathExprIndex < _xpathExpr.Length)
            {
                _currentChar = _xpathExpr[_xpathExprIndex++];
                return true;
            }
            else
            {
                _currentChar = '\0';
                return false;
            }
        }

#if XML10_FIFTH_EDITION
        private char PeekNextChar() {
            Debug.Assert(0 <= xpathExprIndex && xpathExprIndex <= xpathExpr.Length);
            if (xpathExprIndex < xpathExpr.Length) {
                return xpathExpr[xpathExprIndex];
            }
            else {
                Debug.Assert(xpathExprIndex == xpathExpr.Length);
                return '\0';
            }
        }
#endif

        public LexKind Kind { get { return _kind; } }

        public string Name
        {
            get
            {
                Debug.Assert(_kind == LexKind.Name || _kind == LexKind.Axe);
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

        public string StringValue
        {
            get
            {
                Debug.Assert(_kind == LexKind.String);
                Debug.Assert(_stringValue != null);
                return _stringValue;
            }
        }

        public double NumberValue
        {
            get
            {
                Debug.Assert(_kind == LexKind.Number);
                Debug.Assert(_numberValue != double.NaN);
                return _numberValue;
            }
        }

        // To parse PathExpr we need a way to distinct name from function. 
        // THis distinction can't be done without context: "or (1 != 0)" this this a function or 'or' in OrExp 
        public bool CanBeFunction
        {
            get
            {
                Debug.Assert(_kind == LexKind.Name);
                return _canBeFunction;
            }
        }

        private void SkipSpace()
        {
            while (_xmlCharType.IsWhiteSpace(this.CurerntChar) && NextChar()) ;
        }

        public bool NextLex()
        {
            SkipSpace();
            switch (this.CurerntChar)
            {
                case '\0':
                    _kind = LexKind.Eof;
                    return false;
                case ',':
                case '@':
                case '(':
                case ')':
                case '|':
                case '*':
                case '[':
                case ']':
                case '+':
                case '-':
                case '=':
                case '#':
                case '$':
                    _kind = (LexKind)Convert.ToInt32(this.CurerntChar, CultureInfo.InvariantCulture);
                    NextChar();
                    break;
                case '<':
                    _kind = LexKind.Lt;
                    NextChar();
                    if (this.CurerntChar == '=')
                    {
                        _kind = LexKind.Le;
                        NextChar();
                    }
                    break;
                case '>':
                    _kind = LexKind.Gt;
                    NextChar();
                    if (this.CurerntChar == '=')
                    {
                        _kind = LexKind.Ge;
                        NextChar();
                    }
                    break;
                case '!':
                    _kind = LexKind.Bang;
                    NextChar();
                    if (this.CurerntChar == '=')
                    {
                        _kind = LexKind.Ne;
                        NextChar();
                    }
                    break;
                case '.':
                    _kind = LexKind.Dot;
                    NextChar();
                    if (this.CurerntChar == '.')
                    {
                        _kind = LexKind.DotDot;
                        NextChar();
                    }
                    else if (XmlCharType.IsDigit(this.CurerntChar))
                    {
                        _kind = LexKind.Number;
                        _numberValue = ScanFraction();
                    }
                    break;
                case '/':
                    _kind = LexKind.Slash;
                    NextChar();
                    if (this.CurerntChar == '/')
                    {
                        _kind = LexKind.SlashSlash;
                        NextChar();
                    }
                    break;
                case '"':
                case '\'':
                    _kind = LexKind.String;
                    _stringValue = ScanString();
                    break;
                default:
                    if (XmlCharType.IsDigit(this.CurerntChar))
                    {
                        _kind = LexKind.Number;
                        _numberValue = ScanNumber();
                    }
                    else if (_xmlCharType.IsStartNCNameSingleChar(this.CurerntChar)
#if XML10_FIFTH_EDITION
                    || xmlCharType.IsNCNameHighSurrogateChar(this.CurerntChar) 
#endif
                    )
                    {
                        _kind = LexKind.Name;
                        _name = ScanName();
                        _prefix = string.Empty;
                        // "foo:bar" is one lexem not three because it doesn't allow spaces in between
                        // We should distinct it from "foo::" and need process "foo ::" as well
                        if (this.CurerntChar == ':')
                        {
                            NextChar();
                            // can be "foo:bar" or "foo::"
                            if (this.CurerntChar == ':')
                            {   // "foo::"
                                NextChar();
                                _kind = LexKind.Axe;
                            }
                            else
                            {                          // "foo:*", "foo:bar" or "foo: "
                                _prefix = _name;
                                if (this.CurerntChar == '*')
                                {
                                    NextChar();
                                    _name = "*";
                                }
                                else if (_xmlCharType.IsStartNCNameSingleChar(this.CurerntChar)
#if XML10_FIFTH_EDITION
                                || xmlCharType.IsNCNameHighSurrogateChar(this.CurerntChar)
#endif
                                )
                                {
                                    _name = ScanName();
                                }
                                else
                                {
                                    throw XPathException.Create(Res.Xp_InvalidName, SourceText);
                                }
                            }
                        }
                        else
                        {
                            SkipSpace();
                            if (this.CurerntChar == ':')
                            {
                                NextChar();
                                // it can be "foo ::" or just "foo :"
                                if (this.CurerntChar == ':')
                                {
                                    NextChar();
                                    _kind = LexKind.Axe;
                                }
                                else
                                {
                                    throw XPathException.Create(Res.Xp_InvalidName, SourceText);
                                }
                            }
                        }
                        SkipSpace();
                        _canBeFunction = (this.CurerntChar == '(');
                    }
                    else
                    {
                        throw XPathException.Create(Res.Xp_InvalidToken, SourceText);
                    }
                    break;
            }
            return true;
        }

        private double ScanNumber()
        {
            Debug.Assert(this.CurerntChar == '.' || XmlCharType.IsDigit(this.CurerntChar));
            int start = _xpathExprIndex - 1;
            int len = 0;
            while (XmlCharType.IsDigit(this.CurerntChar))
            {
                NextChar(); len++;
            }
            if (this.CurerntChar == '.')
            {
                NextChar(); len++;
                while (XmlCharType.IsDigit(this.CurerntChar))
                {
                    NextChar(); len++;
                }
            }
            return XmlConvert.ToXPathDouble(_xpathExpr.Substring(start, len));
        }

        private double ScanFraction()
        {
            Debug.Assert(XmlCharType.IsDigit(this.CurerntChar));
            int start = _xpathExprIndex - 2;
            Debug.Assert(0 <= start && _xpathExpr[start] == '.');
            int len = 1; // '.'
            while (XmlCharType.IsDigit(this.CurerntChar))
            {
                NextChar(); len++;
            }
            return XmlConvert.ToXPathDouble(_xpathExpr.Substring(start, len));
        }

        private string ScanString()
        {
            char endChar = this.CurerntChar;
            NextChar();
            int start = _xpathExprIndex - 1;
            int len = 0;
            while (this.CurerntChar != endChar)
            {
                if (!NextChar())
                {
                    throw XPathException.Create(Res.Xp_UnclosedString);
                }
                len++;
            }
            Debug.Assert(this.CurerntChar == endChar);
            NextChar();
            return _xpathExpr.Substring(start, len);
        }

        private string ScanName()
        {
            Debug.Assert(_xmlCharType.IsStartNCNameSingleChar(this.CurerntChar)
#if XML10_FIFTH_EDITION
                || xmlCharType.IsNCNameHighSurrogateChar(this.CurerntChar)
#endif
                );
            int start = _xpathExprIndex - 1;
            int len = 0;

            for (;;)
            {
                if (_xmlCharType.IsNCNameSingleChar(this.CurerntChar))
                {
                    NextChar();
                    len++;
                }
#if XML10_FIFTH_EDITION
                else if (xmlCharType.IsNCNameSurrogateChar(this.PeekNextChar(), this.CurerntChar)) {
                    NextChar(); 
                    NextChar(); 
                    len += 2;
                }
#endif
                else
                {
                    break;
                }
            }
            return _xpathExpr.Substring(start, len);
        }

        public enum LexKind
        {
            Comma = ',',
            Slash = '/',
            At = '@',
            Dot = '.',
            LParens = '(',
            RParens = ')',
            LBracket = '[',
            RBracket = ']',
            Star = '*',
            Plus = '+',
            Minus = '-',
            Eq = '=',
            Lt = '<',
            Gt = '>',
            Bang = '!',
            Dollar = '$',
            Apos = '\'',
            Quote = '"',
            Union = '|',
            Ne = 'N',   // !=
            Le = 'L',   // <=
            Ge = 'G',   // >=
            And = 'A',   // &&
            Or = 'O',   // ||
            DotDot = 'D',   // ..
            SlashSlash = 'S',   // //
            Name = 'n',   // XML _Name
            String = 's',   // Quoted string constant
            Number = 'd',   // _Number constant
            Axe = 'a',   // Axe (like child::)
            Eof = 'E',
        };
    }
}

