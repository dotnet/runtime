// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using OLEDB.Test.ModuleCore;

/// <summary> 
/// This class is used to write out XML tokens. This tool can write 
/// well-formed as well as non well-formed file. This writer does not 
/// guarantee well-formedness. It does not implement XmlWriter.
///	The class can write tokens by explicit API calls as well as parse
///	a pattern string to generate these tokens.
/// </summary>
namespace XmlCoreTest.Common
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

        private long _eCount = 0; //element indexer
        private long _aCount = 0;   //attribute indexer
        private long _cCount = 0; //Cdata indexer
        private long _tCount = 0;   //Text indexer 
        private long _pCount = 0;   //PI Indexer
        private long _mCount = 0; //Comment Indexer

        private StreamWriter _textWriter = null;
        //Obviously performance is not a major requirement so
        //making use of out-of-box data structures to keep
        //state of the writer.

        //Managing the Element Stack.
        private Stack<string> _stack = null;
        //Managing the Node Queue.
        private StringBuilder _q = null;

        private const string LT = "<";
        private const string GT = ">";
        private const string MT = "/>";
        private const string ET = "</";
        private const string SPACE = " ";
        private const string S_QUOTE = "'";
        private const string D_QUOTE = "\"";
        private const string EQ = "=";
        private const string LF = "\n";

        private void Init()
        {
            _q = new StringBuilder();
            _stack = new Stack<string>();
        }

        private void Destroy()
        {
            _q = null;
            _stack = null;
        }

        /// <summary>
        /// Default Constructor.
        /// </summary>
        public ManagedNodeWriter()
        {
            Init();
        }

        /// 
        /// Overloaded Constructor with FileName to Write
        ///
        public ManagedNodeWriter(string filename)
        {
            Init();
            _textWriter = new StreamWriter(FilePathUtil.getStream(filename));
        }

        public ManagedNodeWriter(Stream myStream, Encoding enc)
        {
            Init();
            _textWriter = new StreamWriter(myStream, enc);
        }

        /// <summary>
        /// Similar to the XmlWriter WriteDocType
        /// </summary>
        /// <param name="name">Doctype name</param>
        /// <param name="sysid">System ID</param>
        /// <param name="pubid">Public ID</param>
        /// <param name="subset">Content Model</param>
        public void WriteDocType(string name, string sysid, string pubid, string subset)
        {
            StringBuilder dt = new StringBuilder();
            dt.Append("<!DOCTYPE ");
            dt.Append(name);

            if (pubid == null)
            {
                if (sysid != null)
                    dt.Append(" SYSTEM " + sysid);
            }
            else
            {
                dt.Append(" PUBLIC " + pubid);
                if (sysid != null)
                {
                    dt.Append(" " + sysid);
                }
            }

            dt.Append("[");
            if (subset != null)
                dt.Append(subset);

            dt.Append("]>");

            if (DEBUG)
                CError.WriteLine(dt.ToString());

            _q.Append(dt.ToString());
        }

        /// <summary>
        /// GetNodes returns the existing XML string thats been written so far.
        /// </summary>
        /// <returns>String of XML</returns>
        public string GetNodes()
        {
            return _q.ToString();
        }

        /// Closing the NodeWriter
        public void Close()
        {
            if (_textWriter != null)
            {
                _textWriter.Write(_q.ToString());
                //textWriter.Close();
                _textWriter.Dispose();
                _textWriter = null;
            }
            Destroy();
        }

        /// Writing XML Decl
        public void PutDecl()
        {
            _q.Append(XML_DECL);
        }


        /// Writing a Root Element.
        public void PutRoot()
        {
            _q.Append(S_ROOT);
        }


        /// Writing End Root Element.
        public void PutEndRoot()
        {
            _q.Append(E_ROOT);
        }


        /// Writing a start of open element.
        public void OpenElement()
        {
            string elem = LT + E_NAME + _eCount + SPACE;

            _q.Append(elem);
            _stack.Push(E_NAME + _eCount);
            ++_eCount;
        }

        /// Writing a start of open element with user supplied name.
        public void OpenElement(string myName)
        {
            string elem = LT + myName + SPACE;
            _stack.Push(myName);
            _q.Append(elem);
        }

        /// Closing the open element.
        public void CloseElement()
        {
            _q.Append(GT);
        }

        // Closing the open element as empty element
        public void CloseEmptyElement()
        {
            _q.Append(MT);
        }

        /// Writing an attribute.
        public void PutAttribute()
        {
            string attr = A_NAME + _aCount + EQ + S_QUOTE + A_VALUE + _aCount + S_QUOTE + SPACE;

            _q.Append(attr);
            ++_aCount;
        }


        /// Overloaded PutAttribute which takes user values.
        public void PutAttribute(string myAttrName, string myAttrValue)
        {
            string attr = SPACE + myAttrName + EQ + S_QUOTE + myAttrValue + S_QUOTE;

            _q.Append(attr);
        }


        /// Writing empty element.
        public void PutEmptyElement()
        {
            string elem = LT + E_NAME + _eCount + MT;

            _q.Append(elem);
            ++_eCount;
        }

        /// Writing an end element from the stack.
        public void PutEndElement()
        {
            string elem = (string)_stack.Pop();
            _q.Append(ET + elem + GT);
        }

        /// Writing an end element for a given name.
        public void PutEndElement(string myName)
        {
            if (DEBUG)
                CError.WriteLine("Popping : " + (string)_stack.Pop());
            _q.Append(ET + myName + GT);
        }

        /// <summary>
        /// Finish allows user to complete xml file with the end element tags that were so far open.
        /// </summary>
        public void Finish()
        {
            while (_stack.Count > 0)
            {
                string elem = (string)_stack.Pop();
                _q.Append(ET + elem + GT);
            }
        }

        /// Writing text.
        /// Note : This is basically equivalent to WriteRaw and the string may contain any number of embedded tags.
        ///			No checking is performed on them either.
        public void PutText(string myStr)
        {
            _q.Append(myStr);
        }

        /// <summary>
        /// AutoGenerated Text
        /// </summary>
        public void PutText()
        {
            _q.Append(TEXT + _tCount++);
        }

        /// <summary>
        /// Writing a Byte Array.
        /// </summary>
        /// <param name="bArr"></param>
        public void PutBytes(byte[] bArr)
        {
            foreach (byte b in bArr)
            {
                _q.Append(b);
            }
        }

        public void PutByte()
        {
            _q.Append(Convert.ToByte("a"));
        }

        /// <summary>
        /// Writes out CDATA Node.
        /// </summary>
        public void PutCData()
        {
            _q.Append("<![CDATA[" + CDATA + _cCount++ + "]]>");
        }

        /// <summary>
        /// Writes out a PI Node.
        /// </summary>
        public void PutPI()
        {
            _q.Append("<?" + PI + _pCount++ + "?>");
        }

        /// <summary>
        /// Writes out a Comment Node.
        /// </summary>
        public void PutComment()
        {
            _q.Append("<!--" + COMMENT + _mCount++ + " -->");
        }

        /// <summary>
        /// Writes out a single whitespace
        /// </summary>
        public void PutWhiteSpace()
        {
            _q.Append(" ");
        }

        /// <summary>
        /// This method is a convenience method and a shortcut to create an XML string. Each character in the pattern
        ///	maps to a particular Put/Open function and calls it for you. For e.g. XEAA/ will call PutDecl, OpenElement, 
        ///	PutAttribute, PutAttribute and CloseElement for you.
        ///	The following is the list of all allowed characters and their function mappings :
        ///
        ///'X' :	PutDecl()
        ///'E' :	OpenElement()
        ///'M' :	CloseEmptyElement()
        ///'/' :	CloseElement()
        ///'e' :	PutEndElement()
        ///'A' :	PutAttribute()
        ///'P' :	PutPI()
        ///'T' :	PutText()
        ///'C' :	PutComment()
        ///'R' :	PutRoot()
        ///'r' :	PutEndRoot()
        ///'B' :	PutEndRoot()
        ///'W' :	PutWhiteSpace()
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
                        CError.WriteLine("Skipping Character : " + ch);
                        break;
                }
            }
        }

        // Entry point.
        public static void Main(string[] args)
        {
            string filename = "temp.xml";
            ManagedNodeWriter mw = new ManagedNodeWriter(filename);
            ManagedNodeWriter mnw = new ManagedNodeWriter();
            mnw.PutPattern("X");

            int count = 0;
            do
            {
                mnw.PutPattern("E/");
                count++;
            } while (count < 65536);
            mnw.PutText("<a/>");
            mnw.Finish();
            StreamWriter sw = new StreamWriter(FilePathUtil.getStream("deep.xml"));
            sw.Write(mnw.GetNodes());
            sw.Dispose();
        }
    }
}
