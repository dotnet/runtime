// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Xml;
using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    internal sealed class XPathScanner
    {
        private readonly string _xpathExpr;
        private int _xpathExprIndex;
        private LexKind _kind;
        private char _currentChar;
        private string? _name;
        private string? _prefix;
        private string? _stringValue;
        private double _numberValue = double.NaN;
        private bool _canBeFunction;

        public XPathScanner(string xpathExpr)
        {
            if (xpathExpr == null)
            {
                throw XPathException.Create(SR.Xp_ExprExpected, string.Empty);
            }
            _xpathExpr = xpathExpr;
            NextChar();
            NextLex();
        }

        public string SourceText { get { return _xpathExpr; } }

        private char CurrentChar { get { return _currentChar; } }

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
                Debug.Assert(!double.IsNaN(_numberValue));
                return _numberValue;
            }
        }

        // To parse PathExpr we need a way to distinct name from function.
        // This distinction can't be done without context: "or (1 != 0)" this is a function or 'or' in OrExp
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
            while (XmlCharType.IsWhiteSpace(CurrentChar) && NextChar()) ;
        }

        public bool NextLex()
        {
            SkipSpace();
            switch (CurrentChar)
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
                    _kind = (LexKind)Convert.ToInt32(CurrentChar, CultureInfo.InvariantCulture);
                    NextChar();
                    break;
                case '<':
                    _kind = LexKind.Lt;
                    NextChar();
                    if (CurrentChar == '=')
                    {
                        _kind = LexKind.Le;
                        NextChar();
                    }
                    break;
                case '>':
                    _kind = LexKind.Gt;
                    NextChar();
                    if (CurrentChar == '=')
                    {
                        _kind = LexKind.Ge;
                        NextChar();
                    }
                    break;
                case '!':
                    _kind = LexKind.Bang;
                    NextChar();
                    if (CurrentChar == '=')
                    {
                        _kind = LexKind.Ne;
                        NextChar();
                    }
                    break;
                case '.':
                    _kind = LexKind.Dot;
                    NextChar();
                    if (CurrentChar == '.')
                    {
                        _kind = LexKind.DotDot;
                        NextChar();
                    }
                    else if (XmlCharType.IsDigit(CurrentChar))
                    {
                        _kind = LexKind.Number;
                        _numberValue = ScanFraction();
                    }
                    break;
                case '/':
                    _kind = LexKind.Slash;
                    NextChar();
                    if (CurrentChar == '/')
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
                    if (XmlCharType.IsDigit(CurrentChar))
                    {
                        _kind = LexKind.Number;
                        _numberValue = ScanNumber();
                    }
                    else if (XmlCharType.IsStartNCNameSingleChar(CurrentChar))
                    {
                        _kind = LexKind.Name;
                        _name = ScanName();
                        _prefix = string.Empty;
                        // "foo:bar" is one lexeme not three because it doesn't allow spaces in between
                        // We should distinct it from "foo::" and need process "foo ::" as well
                        if (CurrentChar == ':')
                        {
                            NextChar();
                            // can be "foo:bar" or "foo::"
                            if (CurrentChar == ':')
                            {   // "foo::"
                                NextChar();
                                _kind = LexKind.Axe;
                            }
                            else
                            {                          // "foo:*", "foo:bar" or "foo: "
                                _prefix = _name;
                                if (CurrentChar == '*')
                                {
                                    NextChar();
                                    _name = "*";
                                }
                                else if (XmlCharType.IsStartNCNameSingleChar(CurrentChar))
                                {
                                    _name = ScanName();
                                }
                                else
                                {
                                    throw XPathException.Create(SR.Xp_InvalidName, SourceText);
                                }
                            }
                        }
                        else
                        {
                            SkipSpace();
                            if (CurrentChar == ':')
                            {
                                NextChar();
                                // it can be "foo ::" or just "foo :"
                                if (CurrentChar == ':')
                                {
                                    NextChar();
                                    _kind = LexKind.Axe;
                                }
                                else
                                {
                                    throw XPathException.Create(SR.Xp_InvalidName, SourceText);
                                }
                            }
                        }
                        SkipSpace();
                        _canBeFunction = (CurrentChar == '(');
                    }
                    else
                    {
                        throw XPathException.Create(SR.Xp_InvalidToken, SourceText);
                    }
                    break;
            }
            return true;
        }

        private double ScanNumber()
        {
            Debug.Assert(CurrentChar == '.' || XmlCharType.IsDigit(CurrentChar));
            int start = _xpathExprIndex - 1;
            int len = 0;
            while (XmlCharType.IsDigit(CurrentChar))
            {
                NextChar(); len++;
            }
            if (CurrentChar == '.')
            {
                NextChar(); len++;
                while (XmlCharType.IsDigit(CurrentChar))
                {
                    NextChar(); len++;
                }
            }
            return XmlConvert.ToXPathDouble(_xpathExpr.Substring(start, len));
        }

        private double ScanFraction()
        {
            Debug.Assert(XmlCharType.IsDigit(CurrentChar));
            int start = _xpathExprIndex - 2;
            Debug.Assert(0 <= start && _xpathExpr[start] == '.');
            int len = 1; // '.'
            while (XmlCharType.IsDigit(CurrentChar))
            {
                NextChar(); len++;
            }
            return XmlConvert.ToXPathDouble(_xpathExpr.Substring(start, len));
        }

        private string ScanString()
        {
            char endChar = CurrentChar;
            NextChar();
            int start = _xpathExprIndex - 1;
            int len = 0;
            while (CurrentChar != endChar)
            {
                if (!NextChar())
                {
                    throw XPathException.Create(SR.Xp_UnclosedString);
                }
                len++;
            }
            Debug.Assert(CurrentChar == endChar);
            NextChar();
            return _xpathExpr.Substring(start, len);
        }

        private string ScanName()
        {
            Debug.Assert(XmlCharType.IsStartNCNameSingleChar(CurrentChar));
            int start = _xpathExprIndex - 1;
            int len = 0;

            while (true)
            {
                if (XmlCharType.IsNCNameSingleChar(CurrentChar))
                {
                    NextChar();
                    len++;
                }
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
