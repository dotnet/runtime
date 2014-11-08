// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Xml;
using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    internal sealed class XPathScanner
    {
        private string xpathExpr;
        private int xpathExprIndex;
        private LexKind kind;
        private char currentChar;
        private string name;
        private string prefix;
        private string stringValue;
        private double numberValue = double.NaN;
        private bool canBeFunction;
        private XmlCharType xmlCharType = XmlCharType.Instance;

        public XPathScanner(string xpathExpr)
        {
            if (xpathExpr == null)
            {
                throw XPathException.Create(SR.Xp_ExprExpected, string.Empty);
            }
            this.xpathExpr = xpathExpr;
            NextChar();
            NextLex();
        }

        public string SourceText { get { return this.xpathExpr; } }

        private char CurrentChar { get { return currentChar; } }

        private bool NextChar()
        {
            Debug.Assert(0 <= xpathExprIndex && xpathExprIndex <= xpathExpr.Length);
            if (xpathExprIndex < xpathExpr.Length)
            {
                currentChar = xpathExpr[xpathExprIndex++];
                return true;
            }
            else
            {
                currentChar = '\0';
                return false;
            }
        }

#if XML10_FIFTH_EDITION
        private char PeekNextChar()
        {
            Debug.Assert(0 <= xpathExprIndex && xpathExprIndex <= xpathExpr.Length);
            if (xpathExprIndex < xpathExpr.Length)
            {
                return xpathExpr[xpathExprIndex];
            }
            else
            {
                Debug.Assert(xpathExprIndex == xpathExpr.Length);
                return '\0';
            }
        }
#endif

        public LexKind Kind { get { return this.kind; } }

        public string Name
        {
            get
            {
                Debug.Assert(this.kind == LexKind.Name || this.kind == LexKind.Axe);
                Debug.Assert(this.name != null);
                return this.name;
            }
        }

        public string Prefix
        {
            get
            {
                Debug.Assert(this.kind == LexKind.Name);
                Debug.Assert(this.prefix != null);
                return this.prefix;
            }
        }

        public string StringValue
        {
            get
            {
                Debug.Assert(this.kind == LexKind.String);
                Debug.Assert(this.stringValue != null);
                return this.stringValue;
            }
        }

        public double NumberValue
        {
            get
            {
                Debug.Assert(this.kind == LexKind.Number);
                Debug.Assert(!double.IsNaN(this.numberValue));
                return this.numberValue;
            }
        }

        // To parse PathExpr we need a way to distinct name from function. 
        // THis distinction can't be done without context: "or (1 != 0)" this this a function or 'or' in OrExp 
        public bool CanBeFunction
        {
            get
            {
                Debug.Assert(this.kind == LexKind.Name);
                return this.canBeFunction;
            }
        }

        void SkipSpace()
        {
            while (xmlCharType.IsWhiteSpace(this.CurrentChar) && NextChar()) ;
        }

        public bool NextLex()
        {
            SkipSpace();
            switch (this.CurrentChar)
            {
                case '\0':
                    kind = LexKind.Eof;
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
                    kind = (LexKind)Convert.ToInt32(this.CurrentChar, CultureInfo.InvariantCulture);
                    NextChar();
                    break;
                case '<':
                    kind = LexKind.Lt;
                    NextChar();
                    if (this.CurrentChar == '=')
                    {
                        kind = LexKind.Le;
                        NextChar();
                    }
                    break;
                case '>':
                    kind = LexKind.Gt;
                    NextChar();
                    if (this.CurrentChar == '=')
                    {
                        kind = LexKind.Ge;
                        NextChar();
                    }
                    break;
                case '!':
                    kind = LexKind.Bang;
                    NextChar();
                    if (this.CurrentChar == '=')
                    {
                        kind = LexKind.Ne;
                        NextChar();
                    }
                    break;
                case '.':
                    kind = LexKind.Dot;
                    NextChar();
                    if (this.CurrentChar == '.')
                    {
                        kind = LexKind.DotDot;
                        NextChar();
                    }
                    else if (XmlCharType.IsDigit(this.CurrentChar))
                    {
                        kind = LexKind.Number;
                        numberValue = ScanFraction();
                    }
                    break;
                case '/':
                    kind = LexKind.Slash;
                    NextChar();
                    if (this.CurrentChar == '/')
                    {
                        kind = LexKind.SlashSlash;
                        NextChar();
                    }
                    break;
                case '"':
                case '\'':
                    this.kind = LexKind.String;
                    this.stringValue = ScanString();
                    break;
                default:
                    if (XmlCharType.IsDigit(this.CurrentChar))
                    {
                        kind = LexKind.Number;
                        numberValue = ScanNumber();
                    }
                    else if (xmlCharType.IsStartNCNameSingleChar(this.CurrentChar)
#if XML10_FIFTH_EDITION
                    || xmlCharType.IsNCNameHighSurrogateChar(this.CurerntChar)
#endif
                    )
                    {
                        kind = LexKind.Name;
                        this.name = ScanName();
                        this.prefix = string.Empty;
                        // "foo:bar" is one lexem not three because it doesn't allow spaces in between
                        // We should distinct it from "foo::" and need process "foo ::" as well
                        if (this.CurrentChar == ':')
                        {
                            NextChar();
                            // can be "foo:bar" or "foo::"
                            if (this.CurrentChar == ':')
                            {   // "foo::"
                                NextChar();
                                kind = LexKind.Axe;
                            }
                            else
                            {                          // "foo:*", "foo:bar" or "foo: "
                                this.prefix = this.name;
                                if (this.CurrentChar == '*')
                                {
                                    NextChar();
                                    this.name = "*";
                                }
                                else if (xmlCharType.IsStartNCNameSingleChar(this.CurrentChar)
#if XML10_FIFTH_EDITION
                                || xmlCharType.IsNCNameHighSurrogateChar(this.CurerntChar)
#endif
                                )
                                {
                                    this.name = ScanName();
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
                            if (this.CurrentChar == ':')
                            {
                                NextChar();
                                // it can be "foo ::" or just "foo :"
                                if (this.CurrentChar == ':')
                                {
                                    NextChar();
                                    kind = LexKind.Axe;
                                }
                                else
                                {
                                    throw XPathException.Create(SR.Xp_InvalidName, SourceText);
                                }
                            }
                        }
                        SkipSpace();
                        this.canBeFunction = (this.CurrentChar == '(');
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
            Debug.Assert(this.CurrentChar == '.' || XmlCharType.IsDigit(this.CurrentChar));
            int start = xpathExprIndex - 1;
            int len = 0;
            while (XmlCharType.IsDigit(this.CurrentChar))
            {
                NextChar(); len++;
            }
            if (this.CurrentChar == '.')
            {
                NextChar(); len++;
                while (XmlCharType.IsDigit(this.CurrentChar))
                {
                    NextChar(); len++;
                }
            }
            return XmlConvertEx.ToXPathDouble(this.xpathExpr.Substring(start, len));
        }

        private double ScanFraction()
        {
            Debug.Assert(XmlCharType.IsDigit(this.CurrentChar));
            int start = xpathExprIndex - 2;
            Debug.Assert(0 <= start && this.xpathExpr[start] == '.');
            int len = 1; // '.'
            while (XmlCharType.IsDigit(this.CurrentChar))
            {
                NextChar(); len++;
            }
            return XmlConvertEx.ToXPathDouble(this.xpathExpr.Substring(start, len));
        }

        private string ScanString()
        {
            char endChar = this.CurrentChar;
            NextChar();
            int start = xpathExprIndex - 1;
            int len = 0;
            while (this.CurrentChar != endChar)
            {
                if (!NextChar())
                {
                    throw XPathException.Create(SR.Xp_UnclosedString);
                }
                len++;
            }
            Debug.Assert(this.CurrentChar == endChar);
            NextChar();
            return this.xpathExpr.Substring(start, len);
        }

        private string ScanName()
        {
            Debug.Assert(xmlCharType.IsStartNCNameSingleChar(this.CurrentChar)
#if XML10_FIFTH_EDITION
                || xmlCharType.IsNCNameHighSurrogateChar(this.CurerntChar)
#endif
                );
            int start = xpathExprIndex - 1;
            int len = 0;

            for (;;)
            {
                if (xmlCharType.IsNCNameSingleChar(this.CurrentChar))
                {
                    NextChar();
                    len++;
                }
#if XML10_FIFTH_EDITION
                else if (xmlCharType.IsNCNameSurrogateChar(this.PeekNextChar(), this.CurerntChar))
                {
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
            return this.xpathExpr.Substring(start, len);
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

