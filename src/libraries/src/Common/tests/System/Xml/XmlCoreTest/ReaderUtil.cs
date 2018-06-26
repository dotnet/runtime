// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/// This class is primarily used to test buffer boundary integrity of readers.
///	This class constructs a memory stream from the given buffer boundary length such that
///	the required tag completely lies exactly on the start and end of buffer boundary.
/// The class makes up the additional bytes by filling in whitespace if so.
///	The first buffer length consists of the XML Decl and the Root Start (done by PrepareStream() )
/// The next buffer length consists of the actual start and end text with the variable content stretched
/// out to end at the buffer boundary.
///
using System;
using System.Xml;
using System.Text;
using System.IO;

using System.Globalization;
using System.Diagnostics;

using OLEDB.Test.ModuleCore;

namespace XmlCoreTest.Common
{
    /// This class adds the functionality to add a string and a char
    /// to the memory stream class.
    public class CustomMemoryStream : MemoryStream
    {
        public void WriteString(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                WriteByte((byte)(c & 0xFF));
                WriteByte((byte)((c >> 8) & 0xFF));
            }
        }
        public void WriteChar(char c)
        {
            WriteByte((byte)(c & 0xFF));
            WriteByte((byte)((c >> 8) & 0xFF));
        }
    }

    /// <summary>
    ///  This class contains helper methods for Readers.
    ///	 ConvertToBinaryStream : Converts the given xml string to the binary equivalent of the string and returns it
    ///		using a memory stream.
    /// Common usage pattern would be something like :
    /// XmlReader.Create( new MemoryStream(ReaderHelper.ConvertToBinaryStream("<elem>abc</elem>", true, false)), "baseUri", readerSettings );
    /// </summary>
    public static partial class ReaderHelper
    {
        public class ReaderUtilException : XmlException
        {
            public ReaderUtilException(string msg) : base(msg)
            {
            }
        }
    }

    // This class creates an XML tag which is exactly the length of the buffer
    // boundary. 
    public class BufferBoundary
    {
        public static string START_TAG = "<?pi ";
        public static string END_TAG = "?>";

        private const int _4K = 4096;
        // count values (useful for cases like tagContent = "attr%1='val%1' "
        public int count = 0;
        public int bufferBoundaryLength;

        private static bool s_debug = false;
        // flag for replacing tagContent % with count values
        public bool replaceFlag = false;

        public CustomMemoryStream memoryStream = new CustomMemoryStream();

        public string xmlDecl = "<?xml version='1.0'?>";
        public string rootStart = "<doc>";
        public string rootEnd = "</doc>";
        public string startText = null;
        public string endText = null;
        public string tagContent = null;
        // for the iterative tagContent
        public string iterContent = null;
        public StringBuilder nodeValue = new StringBuilder();


        //Overloaded Constructor.
        public BufferBoundary(string sTag, string eTag, string cntt, int buffLen)
        {
            startText = sTag;
            endText = eTag;
            tagContent = cntt;
            bufferBoundaryLength = buffLen;
        }

        private bool _prepared = false;
        private bool _finished = false;

        //This fills the first bufferboundary bytes with buffer length spaces.
        public void PrepareStream()
        {
            if (_prepared)
                return;
            memoryStream.WriteString(xmlDecl);
            memoryStream.WriteString(rootStart);
            memoryStream.WriteString(
                GetSpaces(bufferBoundaryLength - (xmlDecl.Length + rootStart.Length) * 2));
            _prepared = true;
        }

        //This fills the first bufferboundary bytes with buffer length spaces and initial xml string user wants.
        public void PrepareStream(string xml)
        {
            if (_prepared)
                return;
            memoryStream.WriteString(xmlDecl);
            memoryStream.WriteString(rootStart);
            memoryStream.WriteString(xml);
            memoryStream.WriteString(GetSpaces(bufferBoundaryLength - (xmlDecl.Length + rootStart.Length + xml.Length) * 2));
            _prepared = true;
        }

        //This writes out the end of root element
        public void FinishStream()
        {
            if (_finished)
                return;

            memoryStream.WriteString(rootEnd);
            _finished = true;
        }

        //This writes out the end of root element after writing some endXml
        public void FinishStream(string endXml)
        {
            if (_finished)
                return;

            memoryStream.WriteString(endXml);
            memoryStream.WriteString(rootEnd);
            _finished = true;
        }

        //This places the required tags at the buffer boundary.
        public MemoryStream StringAtBufferBoundary()
        {
            CError.WriteLine("MemoryStreamLength " + memoryStream.Length);
            long lengthSoFar = memoryStream.Length;

            //if parameters are to be replaced with count.
            if (tagContent.IndexOf("{0}") > -1)
                replaceFlag = true;

            memoryStream.WriteString(startText);

            if (s_debug)
                CError.WriteLine("Stream Length after start tag = " + memoryStream.Length);

            while (true)
            {
                if (replaceFlag)
                {
                    iterContent = string.Format(tagContent, ++count);
                }
                else
                {
                    iterContent = tagContent;
                }

                if (s_debug)
                    CError.Write((bufferBoundaryLength - nodeValue.Length * 2 - startText.Length * 2 - endText.Length * 2 - iterContent.Length * 2) + "|");

                if (bufferBoundaryLength - nodeValue.Length * 2 - startText.Length * 2 - endText.Length * 2 - iterContent.Length * 2 < 0)
                {
                    break;
                }
                nodeValue.Append(iterContent);
                memoryStream.WriteString(iterContent);
            }

            if (s_debug) CError.WriteLine("\nCount = " + (count - 1));
            if (s_debug) CError.WriteLine("Stream Length = " + nodeValue.Length);
            if (s_debug) CError.WriteLine("Stream Length after tagContent tag = " + memoryStream.Length);
            if (s_debug) CError.WriteLine("Node Value = " + nodeValue);

            long spaces = bufferBoundaryLength - (nodeValue.Length + endText.Length + startText.Length) * 2;
            if (s_debug) CError.WriteLine("Spaces Requested = " + spaces / 2);
            nodeValue.Append(GetSpaces(spaces));
            memoryStream.WriteString(GetSpaces(spaces));
            memoryStream.WriteString(endText);

            if (s_debug) CError.WriteLine("Stream Length before FinishStream = " + memoryStream.Length);

            return memoryStream;
        }

        // This function builds a string made up of specified number of spaces.
        public static string GetSpaces(long spaces)
        {
            StringBuilder sb = new StringBuilder();
            long actualSpaces = spaces / 2;
            while (actualSpaces-- > 0)
            {
                sb.Append(" ");
            }
            return sb.ToString();
        }

        // Just a test function.
        public MemoryStream Test()
        {
            PrepareStream();
            FinishStream();
            return memoryStream;
        }
    }//End BufferBoundary
}
