// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Xsl.XsltOld
{
    using System.Diagnostics;
    using System.Text;
    using System.Globalization;
    using System.Collections;
    using System.Collections.Generic;
    using System.Xml.XPath;
    using System.Xml.Xsl.Runtime;

    internal class NumberAction : ContainerAction
    {
        private const long msofnfcNil = 0x00000000;     // no flags
        private const long msofnfcTraditional = 0x00000001;     // use traditional numbering
        private const long msofnfcAlwaysFormat = 0x00000002;     // if requested format is not supported, use Arabic (Western) style

        private const int cchMaxFormat = 63;     // max size of formatted result
        private const int cchMaxFormatDecimal = 11;    // max size of formatted decimal result (doesn't handle the case of a very large pwszSeparator or minlen)

        internal class FormatInfo
        {
            public bool isSeparator;      // False for alphanumeric strings of chars
            public NumberingSequence numSequence;      // Specifies numbering sequence
            public int length;           // Minimum length of decimal numbers (if necessary, pad to left with zeros)
            public string formatString;     // Format string for separator token

            public FormatInfo(bool isSeparator, string formatString)
            {
                this.isSeparator = isSeparator;
                this.formatString = formatString;
            }

            public FormatInfo() { }
        }

        private static FormatInfo s_defaultFormat = new FormatInfo(false, "0");
        private static FormatInfo s_defaultSeparator = new FormatInfo(true, ".");

        private class NumberingFormat : NumberFormatterBase
        {
            private NumberingSequence _seq;
            private int _cMinLen;
            private string _separator;
            private int _sizeGroup;

            internal NumberingFormat() { }

            internal void setNumberingType(NumberingSequence seq) { _seq = seq; }
            //void setLangID(LID langid) {_langid = langid;}
            //internal void setTraditional(bool fTraditional) {_grfnfc = fTraditional ? msofnfcTraditional : 0;}
            internal void setMinLen(int cMinLen) { _cMinLen = cMinLen; }
            internal void setGroupingSeparator(string separator) { _separator = separator; }

            internal void setGroupingSize(int sizeGroup)
            {
                if (0 <= sizeGroup && sizeGroup <= 9)
                {
                    _sizeGroup = sizeGroup;
                }
            }

            internal string FormatItem(object value)
            {
                double dblVal;

                if (value is int)
                {
                    dblVal = (int)value;
                }
                else
                {
                    dblVal = XmlConvert.ToXPathDouble(value);

                    if (0.5 <= dblVal && !double.IsPositiveInfinity(dblVal))
                    {
                        dblVal = XmlConvert.XPathRound(dblVal);
                    }
                    else
                    {
                        // It is an error if the number is NaN, infinite or less than 0.5; an XSLT processor may signal the error;
                        // if it does not signal the error, it must recover by converting the number to a string as if by a call
                        // to the string function and inserting the resulting string into the result tree.
                        return XmlConvert.ToXPathString(value);
                    }
                }

                Debug.Assert(dblVal >= 1);

                switch (_seq)
                {
                    case NumberingSequence.Arabic:
                        break;
                    case NumberingSequence.UCLetter:
                    case NumberingSequence.LCLetter:
                        if (dblVal <= MaxAlphabeticValue)
                        {
                            StringBuilder sb = new StringBuilder();
                            ConvertToAlphabetic(sb, dblVal, _seq == NumberingSequence.UCLetter ? 'A' : 'a', 26);
                            return sb.ToString();
                        }
                        break;
                    case NumberingSequence.UCRoman:
                    case NumberingSequence.LCRoman:
                        if (dblVal <= MaxRomanValue)
                        {
                            StringBuilder sb = new StringBuilder();
                            ConvertToRoman(sb, dblVal, _seq == NumberingSequence.UCRoman);
                            return sb.ToString();
                        }
                        break;
                }

                return ConvertToArabic(dblVal, _cMinLen, _sizeGroup, _separator);
            }

            private static string ConvertToArabic(double val, int minLength, int groupSize, string groupSeparator)
            {
                string str;

                if (groupSize != 0 && groupSeparator != null)
                {
                    NumberFormatInfo NumberFormat = new NumberFormatInfo();
                    NumberFormat.NumberGroupSizes = new int[] { groupSize };
                    NumberFormat.NumberGroupSeparator = groupSeparator;
                    if (Math.Floor(val) == val)
                    {
                        NumberFormat.NumberDecimalDigits = 0;
                    }
                    str = val.ToString("N", NumberFormat);
                }
                else
                {
                    str = Convert.ToString(val, CultureInfo.InvariantCulture);
                }

                if (str.Length >= minLength)
                {
                    return str;
                }
                else
                {
                    StringBuilder sb = new StringBuilder(minLength);
                    sb.Append('0', minLength - str.Length);
                    sb.Append(str);
                    return sb.ToString();
                }
            }
        }

        // States:
        private const int OutputNumber = 2;

        private string _level;
        private string _countPattern;
        private int _countKey = Compiler.InvalidQueryKey;
        private string _from;
        private int _fromKey = Compiler.InvalidQueryKey;
        private string _value;
        private int _valueKey = Compiler.InvalidQueryKey;
        private Avt _formatAvt;
        private Avt _langAvt;
        private Avt _letterAvt;
        private Avt _groupingSepAvt;
        private Avt _groupingSizeAvt;
        // Compile time precalculated AVTs
        private List<FormatInfo> _formatTokens;
        private string _lang;
        private string _letter;
        private string _groupingSep;
        private string _groupingSize;
        private bool _forwardCompatibility;

        internal override bool CompileAttribute(Compiler compiler)
        {
            string name = compiler.Input.LocalName;
            string value = compiler.Input.Value;
            if (Ref.Equal(name, compiler.Atoms.Level))
            {
                if (value != "any" && value != "multiple" && value != "single")
                {
                    throw XsltException.Create(SR.Xslt_InvalidAttrValue, "level", value);
                }
                _level = value;
            }
            else if (Ref.Equal(name, compiler.Atoms.Count))
            {
                _countPattern = value;
                _countKey = compiler.AddQuery(value, /*allowVars:*/true, /*allowKey:*/true, /*pattern*/true);
            }
            else if (Ref.Equal(name, compiler.Atoms.From))
            {
                _from = value;
                _fromKey = compiler.AddQuery(value, /*allowVars:*/true, /*allowKey:*/true, /*pattern*/true);
            }
            else if (Ref.Equal(name, compiler.Atoms.Value))
            {
                _value = value;
                _valueKey = compiler.AddQuery(value);
            }
            else if (Ref.Equal(name, compiler.Atoms.Format))
            {
                _formatAvt = Avt.CompileAvt(compiler, value);
            }
            else if (Ref.Equal(name, compiler.Atoms.Lang))
            {
                _langAvt = Avt.CompileAvt(compiler, value);
            }
            else if (Ref.Equal(name, compiler.Atoms.LetterValue))
            {
                _letterAvt = Avt.CompileAvt(compiler, value);
            }
            else if (Ref.Equal(name, compiler.Atoms.GroupingSeparator))
            {
                _groupingSepAvt = Avt.CompileAvt(compiler, value);
            }
            else if (Ref.Equal(name, compiler.Atoms.GroupingSize))
            {
                _groupingSizeAvt = Avt.CompileAvt(compiler, value);
            }
            else
            {
                return false;
            }
            return true;
        }

        internal override void Compile(Compiler compiler)
        {
            CompileAttributes(compiler);
            CheckEmpty(compiler);

            _forwardCompatibility = compiler.ForwardCompatibility;
            _formatTokens = ParseFormat(PrecalculateAvt(ref _formatAvt));
            _letter = ParseLetter(PrecalculateAvt(ref _letterAvt));
            _lang = PrecalculateAvt(ref _langAvt);
            _groupingSep = PrecalculateAvt(ref _groupingSepAvt);
            if (_groupingSep != null && _groupingSep.Length > 1)
            {
                throw XsltException.Create(SR.Xslt_CharAttribute, "grouping-separator");
            }
            _groupingSize = PrecalculateAvt(ref _groupingSizeAvt);
        }

        private int numberAny(Processor processor, ActionFrame frame)
        {
            int result = 0;
            // Our current point will be our end point in this search
            XPathNavigator endNode = frame.Node;
            if (endNode.NodeType == XPathNodeType.Attribute || endNode.NodeType == XPathNodeType.Namespace)
            {
                endNode = endNode.Clone();
                endNode.MoveToParent();
            }
            XPathNavigator startNode = endNode.Clone();

            if (_fromKey != Compiler.InvalidQueryKey)
            {
                bool hitFrom = false;
                // First try to find start by traversing up. This gives the best candidate or we hit root
                do
                {
                    if (processor.Matches(startNode, _fromKey))
                    {
                        hitFrom = true;
                        break;
                    }
                } while (startNode.MoveToParent());

                Debug.Assert(
                    processor.Matches(startNode, _fromKey) ||   // we hit 'from' or
                    startNode.NodeType == XPathNodeType.Root        // we are at root
                );

                // from this point (matched parent | root) create descendent quiery:
                // we have to reset 'result' on each 'from' node, because this point can' be not last from point;
                XPathNodeIterator sel = startNode.SelectDescendants(XPathNodeType.All, /*matchSelf:*/ true);
                while (sel.MoveNext())
                {
                    if (processor.Matches(sel.Current, _fromKey))
                    {
                        hitFrom = true;
                        result = 0;
                    }
                    else if (MatchCountKey(processor, frame.Node, sel.Current))
                    {
                        result++;
                    }
                    if (sel.Current.IsSamePosition(endNode))
                    {
                        break;
                    }
                }
                if (!hitFrom)
                {
                    result = 0;
                }
            }
            else
            {
                // without 'from' we startting from the root
                startNode.MoveToRoot();
                XPathNodeIterator sel = startNode.SelectDescendants(XPathNodeType.All, /*matchSelf:*/ true);
                // and count root node by itself
                while (sel.MoveNext())
                {
                    if (MatchCountKey(processor, frame.Node, sel.Current))
                    {
                        result++;
                    }
                    if (sel.Current.IsSamePosition(endNode))
                    {
                        break;
                    }
                }
            }
            return result;
        }

        // check 'from' condition:
        // if 'from' exist it has to be ancestor-or-self for the nav
        private bool checkFrom(Processor processor, XPathNavigator nav)
        {
            if (_fromKey == Compiler.InvalidQueryKey)
            {
                return true;
            }
            do
            {
                if (processor.Matches(nav, _fromKey))
                {
                    return true;
                }
            } while (nav.MoveToParent());
            return false;
        }

        private bool moveToCount(XPathNavigator nav, Processor processor, XPathNavigator contextNode)
        {
            do
            {
                if (_fromKey != Compiler.InvalidQueryKey && processor.Matches(nav, _fromKey))
                {
                    return false;
                }
                if (MatchCountKey(processor, contextNode, nav))
                {
                    return true;
                }
            } while (nav.MoveToParent());
            return false;
        }

        private int numberCount(XPathNavigator nav, Processor processor, XPathNavigator contextNode)
        {
            Debug.Assert(nav.NodeType != XPathNodeType.Attribute && nav.NodeType != XPathNodeType.Namespace);
            Debug.Assert(MatchCountKey(processor, contextNode, nav));
            XPathNavigator runner = nav.Clone();
            int number = 1;
            if (runner.MoveToParent())
            {
                runner.MoveToFirstChild();
                while (!runner.IsSamePosition(nav))
                {
                    if (MatchCountKey(processor, contextNode, runner))
                    {
                        number++;
                    }
                    if (!runner.MoveToNext())
                    {
                        Debug.Fail("We implementing preceding-sibling::node() and some how miss context node 'nav'");
                        break;
                    }
                }
            }
            return number;
        }

        private static object SimplifyValue(object value)
        {
            // If result of xsl:number is not in correct range it should be returned as is.
            // so we need intermidiate string value.
            // If it's already a double we would like to keep it as double.
            // So this function converts to string only if result is nodeset or RTF
            Debug.Assert(!(value is int));
            if (Type.GetTypeCode(value.GetType()) == TypeCode.Object)
            {
                XPathNodeIterator nodeset = value as XPathNodeIterator;
                if (nodeset != null)
                {
                    if (nodeset.MoveNext())
                    {
                        return nodeset.Current.Value;
                    }
                    return string.Empty;
                }
                XPathNavigator nav = value as XPathNavigator;
                if (nav != null)
                {
                    return nav.Value;
                }
            }
            return value;
        }

        internal override void Execute(Processor processor, ActionFrame frame)
        {
            Debug.Assert(processor != null && frame != null);
            ArrayList list = processor.NumberList;
            switch (frame.State)
            {
                case Initialized:
                    Debug.Assert(frame != null);
                    Debug.Assert(frame.NodeSet != null);
                    list.Clear();
                    if (_valueKey != Compiler.InvalidQueryKey)
                    {
                        list.Add(SimplifyValue(processor.Evaluate(frame, _valueKey)));
                    }
                    else if (_level == "any")
                    {
                        int number = numberAny(processor, frame);
                        if (number != 0)
                        {
                            list.Add(number);
                        }
                    }
                    else
                    {
                        bool multiple = (_level == "multiple");
                        XPathNavigator contextNode = frame.Node;         // context of xsl:number element. We using this node in MatchCountKey()
                        XPathNavigator countNode = frame.Node.Clone(); // node we count for
                        if (countNode.NodeType == XPathNodeType.Attribute || countNode.NodeType == XPathNodeType.Namespace)
                        {
                            countNode.MoveToParent();
                        }
                        while (moveToCount(countNode, processor, contextNode))
                        {
                            list.Insert(0, numberCount(countNode, processor, contextNode));
                            if (!multiple || !countNode.MoveToParent())
                            {
                                break;
                            }
                        }
                        if (!checkFrom(processor, countNode))
                        {
                            list.Clear();
                        }
                    }

                    /*CalculatingFormat:*/
                    frame.StoredOutput = Format(list,
                        _formatAvt == null ? _formatTokens : ParseFormat(_formatAvt.Evaluate(processor, frame)),
                        _langAvt == null ? _lang : _langAvt.Evaluate(processor, frame),
                        _letterAvt == null ? _letter : ParseLetter(_letterAvt.Evaluate(processor, frame)),
                        _groupingSepAvt == null ? _groupingSep : _groupingSepAvt.Evaluate(processor, frame),
                        _groupingSizeAvt == null ? _groupingSize : _groupingSizeAvt.Evaluate(processor, frame)
                    );
                    goto case OutputNumber;
                case OutputNumber:
                    Debug.Assert(frame.StoredOutput != null);
                    if (!processor.TextEvent(frame.StoredOutput))
                    {
                        frame.State = OutputNumber;
                        break;
                    }
                    frame.Finished();
                    break;
                default:
                    Debug.Fail("Invalid Number Action execution state");
                    break;
            }
        }

        private bool MatchCountKey(Processor processor, XPathNavigator contextNode, XPathNavigator nav)
        {
            if (_countKey != Compiler.InvalidQueryKey)
            {
                return processor.Matches(nav, _countKey);
            }
            if (contextNode.Name == nav.Name && BasicNodeType(contextNode.NodeType) == BasicNodeType(nav.NodeType))
            {
                return true;
            }
            return false;
        }

        private XPathNodeType BasicNodeType(XPathNodeType type)
        {
            if (type == XPathNodeType.SignificantWhitespace || type == XPathNodeType.Whitespace)
            {
                return XPathNodeType.Text;
            }
            else
            {
                return type;
            }
        }

        // SDUB: perf.
        // for each call to xsl:number Format() will build new NumberingFormat object.
        // in case of no AVTs we can build this object at compile time and reuse it on execution time.
        // even partial step in this derection will be usefull (when cFormats == 0)

        private static string Format(ArrayList numberlist, List<FormatInfo> tokens, string lang, string letter, string groupingSep, string groupingSize)
        {
            StringBuilder result = new StringBuilder();
            int cFormats = 0;
            if (tokens != null)
            {
                cFormats = tokens.Count;
            }

            NumberingFormat numberingFormat = new NumberingFormat();
            if (groupingSize != null)
            {
                try
                {
                    numberingFormat.setGroupingSize(Convert.ToInt32(groupingSize, CultureInfo.InvariantCulture));
                }
                catch (System.FormatException) { }
                catch (System.OverflowException) { }
            }
            if (groupingSep != null)
            {
                if (groupingSep.Length > 1)
                {
                    // It is a breaking change to throw an exception, SQLBUDT 324367
                    //throw XsltException.Create(SR.Xslt_CharAttribute, "grouping-separator");
                }
                numberingFormat.setGroupingSeparator(groupingSep);
            }
            if (0 < cFormats)
            {
                FormatInfo prefix = tokens[0];
                Debug.Assert(prefix == null || prefix.isSeparator);
                FormatInfo sufix = null;
                if (cFormats % 2 == 1)
                {
                    sufix = tokens[cFormats - 1];
                    cFormats--;
                }
                FormatInfo periodicSeparator = 2 < cFormats ? tokens[cFormats - 2] : s_defaultSeparator;
                FormatInfo periodicFormat = 0 < cFormats ? tokens[cFormats - 1] : s_defaultFormat;
                if (prefix != null)
                {
                    result.Append(prefix.formatString);
                }
                int numberlistCount = numberlist.Count;
                for (int i = 0; i < numberlistCount; i++)
                {
                    int formatIndex = i * 2;
                    bool haveFormat = formatIndex < cFormats;
                    if (0 < i)
                    {
                        FormatInfo thisSeparator = haveFormat ? tokens[formatIndex + 0] : periodicSeparator;
                        Debug.Assert(thisSeparator.isSeparator);
                        result.Append(thisSeparator.formatString);
                    }

                    FormatInfo thisFormat = haveFormat ? tokens[formatIndex + 1] : periodicFormat;
                    Debug.Assert(!thisFormat.isSeparator);

                    //numberingFormat.setletter(this.letter);
                    //numberingFormat.setLang(this.lang);

                    numberingFormat.setNumberingType(thisFormat.numSequence);
                    numberingFormat.setMinLen(thisFormat.length);
                    result.Append(numberingFormat.FormatItem(numberlist[i]));
                }

                if (sufix != null)
                {
                    result.Append(sufix.formatString);
                }
            }
            else
            {
                numberingFormat.setNumberingType(NumberingSequence.Arabic);
                for (int i = 0; i < numberlist.Count; i++)
                {
                    if (i != 0)
                    {
                        result.Append(".");
                    }
                    result.Append(numberingFormat.FormatItem(numberlist[i]));
                }
            }
            return result.ToString();
        }

        /*
        ----------------------------------------------------------------------------
            mapFormatToken()

            Maps a token of alphanumeric characters to a numbering format ID and a
            minimum length bound.  Tokens specify the character(s) that begins a
            Unicode
            numbering sequence.  For example, "i" specifies lower case roman numeral
            numbering.  Leading "zeros" specify a minimum length to be maintained by
            padding, if necessary.
        ----------------------------------------------------------------------------
        */
        private static void mapFormatToken(string wsToken, int startLen, int tokLen, out NumberingSequence seq, out int pminlen)
        {
            char wch = wsToken[startLen];
            bool UseArabic = false;
            pminlen = 1;
            seq = NumberingSequence.Nil;

            switch ((int)wch)
            {
                case 0x0030:    // Digit zero
                case 0x0966:    // Hindi digit zero
                case 0x0e50:    // Thai digit zero
                case 0xc77b:    // Korean digit zero
                case 0xff10:    // Digit zero (double-byte)
                    do
                    {
                        // Leading zeros request padding.  Track how much.
                        pminlen++;
                    } while ((--tokLen > 0) && (wch == wsToken[++startLen]));

                    if (wsToken[startLen] != (char)(wch + 1))
                    {
                        // If next character isn't "one", then use Arabic
                        UseArabic = true;
                    }
                    break;
            }

            if (!UseArabic)
            {
                // Map characters of token to number format ID
                switch ((int)wsToken[startLen])
                {
                    case 0x0031: seq = NumberingSequence.Arabic; break;
                    case 0x0041: seq = NumberingSequence.UCLetter; break;
                    case 0x0049: seq = NumberingSequence.UCRoman; break;
                    case 0x0061: seq = NumberingSequence.LCLetter; break;
                    case 0x0069: seq = NumberingSequence.LCRoman; break;
                    case 0x0410: seq = NumberingSequence.UCRus; break;
                    case 0x0430: seq = NumberingSequence.LCRus; break;
                    case 0x05d0: seq = NumberingSequence.Hebrew; break;
                    case 0x0623: seq = NumberingSequence.ArabicScript; break;
                    case 0x0905: seq = NumberingSequence.Hindi2; break;
                    case 0x0915: seq = NumberingSequence.Hindi1; break;
                    case 0x0967: seq = NumberingSequence.Hindi3; break;
                    case 0x0e01: seq = NumberingSequence.Thai1; break;
                    case 0x0e51: seq = NumberingSequence.Thai2; break;
                    case 0x30a2: seq = NumberingSequence.DAiueo; break;
                    case 0x30a4: seq = NumberingSequence.DIroha; break;
                    case 0x3131: seq = NumberingSequence.DChosung; break;
                    case 0x4e00: seq = NumberingSequence.FEDecimal; break;
                    case 0x58f1: seq = NumberingSequence.DbNum3; break;
                    case 0x58f9: seq = NumberingSequence.ChnCmplx; break;
                    case 0x5b50: seq = NumberingSequence.Zodiac2; break;
                    case 0xac00: seq = NumberingSequence.Ganada; break;
                    case 0xc77c: seq = NumberingSequence.KorDbNum1; break;
                    case 0xd558: seq = NumberingSequence.KorDbNum3; break;
                    case 0xff11: seq = NumberingSequence.DArabic; break;
                    case 0xff71: seq = NumberingSequence.Aiueo; break;
                    case 0xff72: seq = NumberingSequence.Iroha; break;

                    case 0x7532:
                        if (tokLen > 1 && wsToken[startLen + 1] == 0x5b50)
                        {
                            // 60-based Zodiak numbering begins with two characters
                            seq = NumberingSequence.Zodiac3;
                            tokLen--;
                            startLen++;
                        }
                        else
                        {
                            // 10-based Zodiak numbering begins with one character
                            seq = NumberingSequence.Zodiac1;
                        }
                        break;
                    default:
                        seq = NumberingSequence.Arabic;
                        break;
                }
            }

            //if (tokLen != 1 || UseArabic) {
            if (UseArabic)
            {
                // If remaining token length is not 1, then don't recognize
                // sequence and default to Arabic with no zero padding.
                seq = NumberingSequence.Arabic;
                pminlen = 0;
            }
        }


        /*
        ----------------------------------------------------------------------------
            parseFormat()

            Parse format string into format tokens (alphanumeric) and separators
            (non-alphanumeric).

        */
        private static List<FormatInfo> ParseFormat(string formatString)
        {
            if (formatString == null || formatString.Length == 0)
            {
                return null;
            }
            int length = 0;
            bool lastAlphaNumeric = CharUtil.IsAlphaNumeric(formatString[length]);
            List<FormatInfo> tokens = new List<FormatInfo>();
            int count = 0;

            if (lastAlphaNumeric)
            {
                // If the first one is alpha num add empty separator as a prefix.
                tokens.Add(null);
            }

            while (length <= formatString.Length)
            {
                // Loop until a switch from format token to separator is detected (or vice-versa)
                bool currentchar = length < formatString.Length ? CharUtil.IsAlphaNumeric(formatString[length]) : !lastAlphaNumeric;
                if (lastAlphaNumeric != currentchar)
                {
                    FormatInfo formatInfo = new FormatInfo();
                    if (lastAlphaNumeric)
                    {
                        // We just finished a format token.  Map it to a numbering format ID and a min-length bound.
                        mapFormatToken(formatString, count, length - count, out formatInfo.numSequence, out formatInfo.length);
                    }
                    else
                    {
                        formatInfo.isSeparator = true;
                        // We just finished a separator.  Save its length and a pointer to it.
                        formatInfo.formatString = formatString.Substring(count, length - count);
                    }
                    count = length;
                    length++;
                    // Begin parsing the next format token or separator

                    tokens.Add(formatInfo);
                    // Flip flag from format token to separator (or vice-versa)
                    lastAlphaNumeric = currentchar;
                }
                else
                {
                    length++;
                }
            }

            return tokens;
        }

        private string ParseLetter(string letter)
        {
            if (letter == null || letter == "traditional" || letter == "alphabetic")
            {
                return letter;
            }
            if (!_forwardCompatibility)
            {
                throw XsltException.Create(SR.Xslt_InvalidAttrValue, "letter-value", letter);
            }
            return null;
        }
    }
}
