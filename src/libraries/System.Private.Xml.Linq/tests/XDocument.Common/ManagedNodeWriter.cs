// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CoreXml.Test.XLinq
{
    public class ManagedNodeWriter
    {
        public static bool DEBUG = false;

        private const string XML_DECL = "<?xml version='1.0' ?>\n";
        private const string S_ROOT = "<root>";
        private const string E_ROOT = "</root>";
        private const string E_NAME = "ELEMENT_";
        private const string A_NAME = "ATTRIB_";
        private const string A_VALUE = "VALUE_";
        private const string CDATA = "CDATA_";
        private const string TEXT = "TEXT_";
        private const string PI = "PI_";
        private const string COMMENT = "COMMENT_";

        private long _eCount = 0;    //element indexer
        private long _aCount = 0;    //attribute indexer
        private long _cCount = 0;    //Cdata indexer
        private long _tCount = 0;    //Text indexer
        private long _pCount = 0;    //PI Indexer
        private long _mCount = 0;    //Comment Indexer

        private StreamWriter _textWriter = null;
        private Stack<string> _elementStack = new Stack<string>();
        private StringBuilder _nodeQueue = new StringBuilder();

        private const string LT = "<";
        private const string GT = ">";
        private const string MT = "/>";
        private const string ET = "</";
        private const string SPACE = " ";
        private const string S_QUOTE = "'";
        private const string D_QUOTE = "\"";
        private const string EQ = "=";
        private const string LF = "\n";

        public ManagedNodeWriter()
        { }

        public ManagedNodeWriter(Stream myStream, Encoding enc)
        {
            _textWriter = new StreamWriter(myStream, enc);
        }

        /// <summary>
        /// GetNodes returns the existing XML string thats been written so far.
        /// </summary>
        /// <returns>String of XML</returns>
        public string GetNodes()
        {
            return _nodeQueue.ToString();
        }

        /// Closing the NodeWriter
        public void Close()
        {
            if (_textWriter != null)
            {
                _textWriter.Write(_nodeQueue.ToString());
                _textWriter.Dispose();
                _textWriter = null;
            }
        }

        /// Writing XML Decl
        public void PutDecl()
        {
            _nodeQueue.Append(XML_DECL);
        }


        /// Writing a Root Element.
        public void PutRoot()
        {
            _nodeQueue.Append(S_ROOT);
        }


        /// Writing End Root Element.
        public void PutEndRoot()
        {
            _nodeQueue.Append(E_ROOT);
        }


        /// Writing a start of open element.
        public void OpenElement()
        {
            string elem = LT + E_NAME + _eCount + SPACE;

            _nodeQueue.Append(elem);
            _elementStack.Push(E_NAME + _eCount);
            ++_eCount;
        }

        /// Writing a start of open element with user supplied name.
        public void OpenElement(string myName)
        {
            string elem = LT + myName + SPACE;
            _elementStack.Push(myName);
            _nodeQueue.Append(elem);
        }

        /// Closing the open element.
        public void CloseElement()
        {
            _nodeQueue.Append(GT);
        }

        // Closing the open element as empty element
        public void CloseEmptyElement()
        {
            _nodeQueue.Append(MT);
        }

        /// Writing an attribute.
        public void PutAttribute()
        {
            string attr = A_NAME + _aCount + EQ + S_QUOTE + A_VALUE + _aCount + S_QUOTE + SPACE;

            _nodeQueue.Append(attr);
            ++_aCount;
        }


        /// Overloaded PutAttribute which takes user values.
        public void PutAttribute(string myAttrName, string myAttrValue)
        {
            string attr = SPACE + myAttrName + EQ + S_QUOTE + myAttrValue + S_QUOTE;

            _nodeQueue.Append(attr);
        }


        /// Writing empty element.
        public void PutEmptyElement()
        {
            string elem = LT + E_NAME + _eCount + MT;

            _nodeQueue.Append(elem);
            ++_eCount;
        }

        /// Writing an end element from the stack.
        public void PutEndElement()
        {
            string elem = _elementStack.Pop();
            _nodeQueue.Append(ET + elem + GT);
        }

        /// Writing an end element for a given name.
        public void PutEndElement(string myName)
        {
            if (DEBUG)
            {
                string elem = _elementStack.Pop();
            }
            _nodeQueue.Append(ET + myName + GT);
        }

        /// <summary>
        /// Finish allows user to complete xml file with the end element tags that were so far open.
        /// </summary>
        public void Finish()
        {
            while (_elementStack.Count > 0)
            {
                string elem = _elementStack.Pop();
                _nodeQueue.Append(ET + elem + GT);
            }
        }

        /// Writing text.
        /// Note : This is basically equivalent to WriteRaw and the string may contain any number of embedded tags.
        ///        No checking is performed on them either.
        public void PutText(string myStr)
        {
            _nodeQueue.Append(myStr);
        }

        /// <summary>
        /// AutoGenerated Text
        /// </summary>
        public void PutText()
        {
            _nodeQueue.Append(TEXT + _tCount++);
        }

        /// <summary>
        /// Writing a Byte Array.
        /// </summary>
        /// <param name="bArr"></param>
        public void PutBytes(byte[] bArr)
        {
            foreach (byte b in bArr)
            {
                _nodeQueue.Append(b);
            }
        }

        public void PutByte()
        {
            _nodeQueue.Append(Convert.ToByte("a"));
        }

        /// <summary>
        /// Writes out CDATA Node.
        /// </summary>
        public void PutCData()
        {
            _nodeQueue.Append($"<![CDATA[{CDATA}{_cCount++}]]>");
        }

        /// <summary>
        /// Writes out a PI Node.
        /// </summary>
        public void PutPI()
        {
            _nodeQueue.Append($"<?{PI}{_pCount++}?>");
        }

        /// <summary>
        /// Writes out a Comment Node.
        /// </summary>
        public void PutComment()
        {
            _nodeQueue.Append($"<!--{COMMENT}{_mCount++} -->");
        }

        /// <summary>
        /// Writes out a single whitespace
        /// </summary>
        public void PutWhiteSpace()
        {
            _nodeQueue.Append(" ");
        }

        /// <summary>
        /// This method is a convenience method and a shortcut to create an XML string. Each character in the pattern
        /// maps to a particular Put/Open function and calls it for you. For e.g. XEAA/ will call PutDecl, OpenElement,
        /// PutAttribute, PutAttribute and CloseElement for you.
        /// The following is the list of all allowed characters and their function mappings :
        ///
        ///'X' :    PutDecl()
        ///'E' :    OpenElement()
        ///'M' :    CloseEmptyElement()
        ///'/' :    CloseElement()
        ///'e' :    PutEndElement()
        ///'A' :    PutAttribute()
        ///'P' :    PutPI()
        ///'T' :    PutText()
        ///'C' :    PutComment()
        ///'R' :    PutRoot()
        ///'r' :    PutEndRoot()
        ///'B' :    PutEndRoot()
        ///'W' :    PutWhiteSpace()
        ///
        /// </summary>
        /// <param name="pattern">String containing the pattern which you want to use to create
        /// the XML string. Refer to table above for supported chars.</param>

        public void PutPattern(string pattern)
        {
            char[] patternArr = pattern.ToCharArray();
            foreach (char ch in patternArr)
            {
                switch (ch)
                {
                    case 'X':
                        PutDecl();
                        break;
                    case 'E':
                        OpenElement();
                        break;
                    case 'M':
                        CloseEmptyElement();
                        break;
                    case '/':
                        CloseElement();
                        break;
                    case 'e':
                        PutEndElement();
                        break;
                    case 'A':
                        PutAttribute();
                        break;
                    case 'P':
                        PutPI();
                        break;
                    case 'T':
                        PutText();
                        break;
                    case 'C':
                        PutComment();
                        break;
                    case 'R':
                        PutRoot();
                        break;
                    case 'r':
                        PutEndRoot();
                        break;
                    case 'B':
                        PutEndRoot();
                        break;
                    case 'W':
                        PutWhiteSpace();
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
