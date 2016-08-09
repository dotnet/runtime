// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace TernaryTreeGenerator {
    using System;
    using System.IO;
    using System.Text;
    using System.Diagnostics;

    //
    // HTML Element/Attribute name <-> XSL output property mapping
    //
    /// <include file='doc\TernaryTreeGenerator.uex' path='docs/doc[@for="HtmlProps"]/*' />
    public class HtmlProps {
        /// <include file='doc\TernaryTreeGenerator.uex' path='docs/doc[@for="HtmlProps.nameHTML;"]/*' />
        public String  nameHTML;
        /// <include file='doc\TernaryTreeGenerator.uex' path='docs/doc[@for="HtmlProps.properties;"]/*' />
        public byte    properties;
        /// <include file='doc\TernaryTreeGenerator.uex' path='docs/doc[@for="HtmlProps.visited;"]/*' />
        public bool    visited;
        /// <include file='doc\TernaryTreeGenerator.uex' path='docs/doc[@for="HtmlProps.HtmlProps"]/*' />
        public HtmlProps(String nameHTML, byte properties) {
            this.nameHTML = nameHTML;
            this.properties = properties;
        }
    }


    //
    // TernaryTreeRW    // -------------
    //
    // NOTE: This class is only meant to be used to generate static tables to be used
    //       later with TernaryTreeRO.  It should not be used in production code.
    //
    /// <include file='doc\TernaryTreeGenerator.uex' path='docs/doc[@for="TernaryTreeRW"]/*' />
    public class TernaryTreeRW {
        /// <include file='doc\TernaryTreeGenerator.uex' path='docs/doc[@for="TernaryTreeRW.MAX_NODES"]/*' />
        public const int MAX_NODES = 2000;
        // Since _lt and _gt are just offsets in the node buffer, the node buffer size is limited
        byte [,] nodeBuffer;
        //node indexer;
        int endNodePos;

        int numHtmlElements;
        int numHtmlAttributes;

        static HtmlProps [] htmlElements = {
            new HtmlProps("a",              (byte)(ElementProperties.URI_PARENT)),
            new HtmlProps("address",        (byte)(ElementProperties.BLOCK_WS)),
            new HtmlProps("applet",         (byte)(ElementProperties.BLOCK_WS)),
            new HtmlProps("area",           (byte)(ElementProperties.BOOL_PARENT | ElementProperties.URI_PARENT | ElementProperties.EMPTY | ElementProperties.BLOCK_WS)),
            new HtmlProps("base",           (byte)(ElementProperties.URI_PARENT | ElementProperties.EMPTY | ElementProperties.BLOCK_WS)),
            new HtmlProps("basefont",       (byte)(ElementProperties.EMPTY | ElementProperties.BLOCK_WS)),
            new HtmlProps("blockquote",     (byte)(ElementProperties.URI_PARENT | ElementProperties.BLOCK_WS)),
            new HtmlProps("body",           (byte)(ElementProperties.BLOCK_WS)),
            new HtmlProps("br",             (byte)(ElementProperties.EMPTY)),
            new HtmlProps("button",         (byte)(ElementProperties.BOOL_PARENT)),
            new HtmlProps("caption",        (byte)(ElementProperties.BLOCK_WS)),
            new HtmlProps("center",         (byte)(ElementProperties.BLOCK_WS)),
            new HtmlProps("col",            (byte)(ElementProperties.EMPTY | ElementProperties.BLOCK_WS)),
            new HtmlProps("colgroup",       (byte)(ElementProperties.BLOCK_WS)),
            new HtmlProps("dd",             (byte)(ElementProperties.BLOCK_WS)),
            new HtmlProps("del",            (byte)(ElementProperties.URI_PARENT | ElementProperties.BLOCK_WS)),
            new HtmlProps("dir",            (byte)(ElementProperties.BOOL_PARENT | ElementProperties.BLOCK_WS)),
            new HtmlProps("div",            (byte)(ElementProperties.BLOCK_WS)),
            new HtmlProps("dl",             (byte)(ElementProperties.BOOL_PARENT | ElementProperties.BLOCK_WS)),
            new HtmlProps("dt",             (byte)(ElementProperties.BLOCK_WS)),
            new HtmlProps("fieldset",       (byte)(ElementProperties.BLOCK_WS)),
            new HtmlProps("font",           (byte)(ElementProperties.BLOCK_WS)),
            new HtmlProps("form",           (byte)(ElementProperties.URI_PARENT | ElementProperties.BLOCK_WS)),
            new HtmlProps("frame",          (byte)(ElementProperties.BOOL_PARENT | ElementProperties.EMPTY | ElementProperties.BLOCK_WS)),
            new HtmlProps("frameset",       (byte)(ElementProperties.BLOCK_WS)),
            new HtmlProps("h1",             (byte)(ElementProperties.BLOCK_WS)),
            new HtmlProps("h2",             (byte)(ElementProperties.BLOCK_WS)),
            new HtmlProps("h3",             (byte)(ElementProperties.BLOCK_WS)),
            new HtmlProps("h4",             (byte)(ElementProperties.BLOCK_WS)),
            new HtmlProps("h5",             (byte)(ElementProperties.BLOCK_WS)),
            new HtmlProps("h6",             (byte)(ElementProperties.BLOCK_WS)),
            new HtmlProps("head",           (byte)(ElementProperties.URI_PARENT | ElementProperties.BLOCK_WS | ElementProperties.HEAD)),
            new HtmlProps("hr",             (byte)(ElementProperties.BOOL_PARENT | ElementProperties.EMPTY | ElementProperties.BLOCK_WS)),
            new HtmlProps("html",           (byte)(ElementProperties.BLOCK_WS)),
            new HtmlProps("iframe",         (byte)(ElementProperties.BLOCK_WS)),
            new HtmlProps("img",            (byte)(ElementProperties.BOOL_PARENT | ElementProperties.URI_PARENT | ElementProperties.EMPTY)),
            new HtmlProps("input",          (byte)(ElementProperties.BOOL_PARENT | ElementProperties.URI_PARENT | ElementProperties.EMPTY)),
            new HtmlProps("ins",            (byte)(ElementProperties.URI_PARENT | ElementProperties.BLOCK_WS)),
            new HtmlProps("isindex",        (byte)(ElementProperties.EMPTY | ElementProperties.BLOCK_WS)),
            new HtmlProps("legend",         (byte)(ElementProperties.BLOCK_WS)),
            new HtmlProps("li",             (byte)(ElementProperties.BLOCK_WS)),
            new HtmlProps("link",           (byte)(ElementProperties.URI_PARENT | ElementProperties.EMPTY | ElementProperties.BLOCK_WS)),
            new HtmlProps("map",            (byte)(ElementProperties.BLOCK_WS)),
            new HtmlProps("menu",           (byte)(ElementProperties.BOOL_PARENT | ElementProperties.BLOCK_WS)),
            new HtmlProps("meta",           (byte)(ElementProperties.EMPTY | ElementProperties.BLOCK_WS)),
            new HtmlProps("noframes",       (byte)(ElementProperties.BLOCK_WS)),
            new HtmlProps("noscript",       (byte)(ElementProperties.BLOCK_WS)),
            new HtmlProps("object",         (byte)(ElementProperties.BOOL_PARENT | ElementProperties.URI_PARENT)),
            new HtmlProps("ol",             (byte)(ElementProperties.BOOL_PARENT | ElementProperties.BLOCK_WS)),
            new HtmlProps("optgroup",       (byte)(ElementProperties.BOOL_PARENT | ElementProperties.BLOCK_WS)),
            new HtmlProps("option",         (byte)(ElementProperties.BOOL_PARENT | ElementProperties.BLOCK_WS)),
            new HtmlProps("p",              (byte)(ElementProperties.BLOCK_WS)),
            new HtmlProps("param",          (byte)(ElementProperties.EMPTY | ElementProperties.BLOCK_WS)),
            new HtmlProps("pre",            (byte)(ElementProperties.BLOCK_WS)),
            new HtmlProps("q",              (byte)(ElementProperties.URI_PARENT)),
            new HtmlProps("s",              (byte)(ElementProperties.BLOCK_WS)),
            new HtmlProps("script",         (byte)(ElementProperties.NO_ENTITIES | ElementProperties.BOOL_PARENT | ElementProperties.URI_PARENT)),
            new HtmlProps("select",         (byte)(ElementProperties.BOOL_PARENT)),
            new HtmlProps("strike",         (byte)(ElementProperties.BLOCK_WS)),
            new HtmlProps("style",          (byte)(ElementProperties.NO_ENTITIES | ElementProperties.BLOCK_WS)),
            new HtmlProps("table",          (byte)(ElementProperties.URI_PARENT | ElementProperties.BLOCK_WS)),
            new HtmlProps("tbody",          (byte)(ElementProperties.BLOCK_WS)),
            new HtmlProps("td",             (byte)(ElementProperties.BOOL_PARENT | ElementProperties.BLOCK_WS)),
            new HtmlProps("textarea",       (byte)(ElementProperties.BOOL_PARENT)),
            new HtmlProps("tfoot",          (byte)(ElementProperties.BLOCK_WS)),
            new HtmlProps("th",             (byte)(ElementProperties.BOOL_PARENT | ElementProperties.BLOCK_WS)),
            new HtmlProps("thead",          (byte)(ElementProperties.BLOCK_WS)),
            new HtmlProps("title",          (byte)(ElementProperties.BLOCK_WS)),
            new HtmlProps("tr",             (byte)(ElementProperties.BLOCK_WS)),
            new HtmlProps("ul",             (byte)(ElementProperties.BOOL_PARENT | ElementProperties.BLOCK_WS)),
            new HtmlProps("xmp",            (byte)(ElementProperties.BLOCK_WS)),
        };

        static HtmlProps [] htmlAttributes = {
            new HtmlProps("action",         (byte)AttributeProperties.URI),
            new HtmlProps("checked",        (byte)AttributeProperties.BOOLEAN),
            new HtmlProps("cite",           (byte)AttributeProperties.URI),
            new HtmlProps("classid",        (byte)AttributeProperties.URI),
            new HtmlProps("codebase",       (byte)AttributeProperties.URI),
            new HtmlProps("compact",        (byte)AttributeProperties.BOOLEAN),
            new HtmlProps("data",           (byte)AttributeProperties.URI),
            new HtmlProps("datasrc",        (byte)AttributeProperties.URI),
            new HtmlProps("declare",        (byte)AttributeProperties.BOOLEAN),
            new HtmlProps("defer",          (byte)AttributeProperties.BOOLEAN),
            new HtmlProps("disabled",       (byte)AttributeProperties.BOOLEAN),
            new HtmlProps("for",            (byte)AttributeProperties.URI),
            new HtmlProps("href",           (byte)AttributeProperties.URI),
            new HtmlProps("ismap",          (byte)AttributeProperties.BOOLEAN),
            new HtmlProps("longdesc",       (byte)AttributeProperties.URI),
            new HtmlProps("multiple",       (byte)AttributeProperties.BOOLEAN),
            new HtmlProps("name",           (byte)AttributeProperties.URI),
            new HtmlProps("nohref",         (byte)AttributeProperties.BOOLEAN),
            new HtmlProps("noresize",       (byte)AttributeProperties.BOOLEAN),
            new HtmlProps("noshade",        (byte)AttributeProperties.BOOLEAN),
            new HtmlProps("nowrap",         (byte)AttributeProperties.BOOLEAN),
            new HtmlProps("profile",        (byte)AttributeProperties.URI),
            new HtmlProps("readonly",       (byte)AttributeProperties.BOOLEAN),
            new HtmlProps("selected",       (byte)AttributeProperties.BOOLEAN),
            new HtmlProps("src",            (byte)AttributeProperties.URI),
            new HtmlProps("usemap",         (byte)AttributeProperties.URI),
        };

        /// <include file='doc\TernaryTreeGenerator.uex' path='docs/doc[@for="TernaryTreeRW.TernaryTreeRW"]/*' />
        public  TernaryTreeRW() {

            //create the two dimentional byte array
            nodeBuffer        = new byte[MAX_NODES, 4];
            numHtmlElements   = htmlElements.Length;
            numHtmlAttributes = htmlAttributes.Length;
            endNodePos        = 1;
        }

        /// <include file='doc\TernaryTreeGenerator.uex' path='docs/doc[@for="TernaryTreeRW.lengthNodes"]/*' />
        public int lengthNodes() {
            return endNodePos;
        }
        /// <include file='doc\TernaryTreeGenerator.uex' path='docs/doc[@for="TernaryTreeRW.lengthBytes"]/*' />
        public int lengthBytes() {
            return endNodePos;
        }


        //  ----------------------------------------------------------------------------
        //    addCaseInsensitiveString()
        //
        //    Insert a string into this ternary tree.  Assert if it's already been inserted
        //    or if the path offset variables (_lt, _gt) are overflowed.
        //
        /// <include file='doc\TernaryTreeGenerator.uex' path='docs/doc[@for="TernaryTreeRW.AddCaseInsensitiveString"]/*' />
        public void AddCaseInsensitiveString(String stringToAdd, byte data) {


            int charToAdd;
            int charInTheTree;
            int stringPos = 0;
            int nodePos = 0;

            if (stringToAdd.Length == 0) {
                charToAdd = 0;
            }
            else {
                charToAdd = stringToAdd[0];

                // Normalize to upper case
                if (charToAdd >= 'a' && charToAdd <= 'z') charToAdd -= ('a' - 'A');
            }

            do {


                // Only ASCII characters are allowed
                if (charToAdd > 'z') {
                    Console.WriteLine("Only ASCII characters are allowed");
                }

                charInTheTree = nodeBuffer[nodePos, (int)TernaryTreeByte.characterByte];

                //Console.Write("charToAdd: {0},charInTheTree: {1}, nodePos: {2}, endNodePos {3}, ", Convert.ToChar(charToAdd), Convert.ToChar(charInTheTree), nodePos, endNodePos);
                //Console.WriteLine("Left :{0}, Right:{0}", nodeBuffer[nodePos, (int)TernaryTreeByte.leftTree], nodeBuffer[nodePos, (int)TernaryTreeByte.rightTree]);


                if (charToAdd == charInTheTree) {
                    // This character already appears in the tree; take the equals path
                    nodePos++;
                    stringPos++;

                    if (stringPos == stringToAdd.Length) {
                        charToAdd = 0;
                    }
                    else {
                        charToAdd = stringToAdd[stringPos];

                        // Normalize to upper case
                        if (charToAdd >= 'a' && charToAdd <= 'z') charToAdd -= ('a' - 'A');

                    }
                } else {

                    if (charToAdd < charInTheTree) {

                        if (nodeBuffer[nodePos, (int)TernaryTreeByte.leftTree] == 0x0) {

                            // Create a new path less than the current character
                            if (endNodePos - nodePos > 255) {
                                throw new Exception("Too many characters have been added");
                            } else {
                                nodeBuffer[nodePos, (int)TernaryTreeByte.leftTree] = (byte)(endNodePos - nodePos);
                            }
                        } else {

                            // Take the existing less than path
                            nodePos = nodePos + nodeBuffer[nodePos, (int)TernaryTreeByte.leftTree];
                            continue;
                        }

                    } else {

                        if (nodeBuffer[nodePos, (int)TernaryTreeByte.rightTree] == 0x0) {
                            // Create a new path greater than the current character
                            if (endNodePos - nodePos > 255) {
                                throw new Exception("Too many characters have been added");
                            } else {
                                nodeBuffer[nodePos, (int)TernaryTreeByte.rightTree] = (byte)(endNodePos - nodePos);
                            }


                        } else {
                            // Take the existing greater than path
                            nodePos = nodePos + nodeBuffer[nodePos, (int)TernaryTreeByte.rightTree] ;
                            continue;
                        }
                    }


                    // Add new nodes to hold rest of string

                    for (int i = stringPos; i <stringToAdd.Length; ++ i) {

                        charToAdd = stringToAdd[i];
                        // Normalize to upper case
                        if (charToAdd >= 'a' && charToAdd <= 'z') charToAdd -= ('a' - 'A');

                        nodePos = endNodePos++;
                        nodeBuffer[nodePos, (int)TernaryTreeByte.characterByte] = (byte) charToAdd;

                    }

                    // Store terminating null to indicate that this is the end of the string
                    nodePos = endNodePos ++;
                    nodeBuffer[nodePos, (int)TernaryTreeByte.characterByte] = 0;
                    nodeBuffer[nodePos, (int)TernaryTreeByte.data] = data;
                    break;
                }

            } while (true);

        }

        /// <include file='doc\TernaryTreeGenerator.uex' path='docs/doc[@for="TernaryTreeRW.GenerateHtmlElementTable"]/*' />
        public void GenerateHtmlElementTable() {

            // Verify that the element and attribute tables are sorted correctly
            htmlElements[0].visited = false;

            for (int i = 1; i < numHtmlElements; i++) {
                htmlElements[i].visited = false;
                if (htmlElements[i - 1].nameHTML.CompareTo(htmlElements[i].nameHTML) > 0) {
                    throw new Exception("String table not correctly sorted");
                }
            }

            // Add strings from the tables to the ternary trees
            AddMidHtmlElement(0, numHtmlElements - 1);

        }

        /// <include file='doc\TernaryTreeGenerator.uex' path='docs/doc[@for="TernaryTreeRW.GenerateHtmlAttributeTable"]/*' />
        public void GenerateHtmlAttributeTable() {

            // Verify that the element and attribute tables are sorted correctly

            htmlAttributes[0].visited = false;

            for (int i = 1; i < numHtmlAttributes; i++) {
                htmlAttributes[i].visited = false;

                if (htmlAttributes[i - 1].nameHTML.CompareTo(htmlAttributes[i].nameHTML) > 0) {
                    throw new Exception("String table not correctly sorted");
                }
            }

            // Add strings from the tables to the ternary trees
            AddMidHtmlAttribute(0, numHtmlAttributes - 1);

        }



        //  ----------------------------------------------------------------------------
        //    addMidHtml()
        //
        //    Given a sorted list of HTML names/properties, adds the middle name to the
        //    ternary tree and partitions the remaining halves, which are then recursively
        //    sent to this procedure.
        //
        void AddMidHtmlElement(int startPos, int endPos) {

            int midPos;

            if (startPos > endPos)
                return;

            midPos = (startPos + endPos) / 2;

            AddCaseInsensitiveString(htmlElements[midPos].nameHTML, htmlElements[midPos].properties);

            AddMidHtmlElement(startPos, midPos - 1);
            AddMidHtmlElement(midPos + 1, endPos);
        }

        //  ----------------------------------------------------------------------------
        //    addMidHtml()
        //
        //    Given a sorted list of HTML names/properties, adds the middle name to the
        //    ternary tree and partitions the remaining halves, which are then recursively
        //    sent to this procedure.
        //
        void AddMidHtmlAttribute(int startPos, int endPos) {
            int midPos;
            if (startPos > endPos)
                return;

            midPos = (startPos + endPos) / 2;
            AddCaseInsensitiveString(htmlAttributes[midPos].nameHTML, htmlAttributes[midPos].properties);


            AddMidHtmlAttribute(startPos, midPos - 1);
            AddMidHtmlAttribute(midPos + 1, endPos);
        }

        //----------------------------------------------------------------------------
        //    OutputFileHeader()
        //

        /// <include file='doc\TernaryTreeGenerator.uex' path='docs/doc[@for="TernaryTreeRW.OutputFileHeader"]/*' />
        public void OutputFileHeader(StreamWriter streamWriter) {
            string indent1 = "    ";

            streamWriter.WriteLine("//------------------------------------------------------------------------------");
            streamWriter.WriteLine("// <copyright file=\"TernaryTreeGenerator.cs\" company=\"Microsoft\">");
            streamWriter.WriteLine("//     Copyright (c) Microsoft Corporation.  All rights reserved.");
            streamWriter.WriteLine("// </copyright>");
            streamWriter.WriteLine("// <owner current=\"true\" primary=\"true\">derekdb</owner>");
            streamWriter.WriteLine("//------------------------------------------------------------------------------");

            streamWriter.WriteLine("//");
            streamWriter.WriteLine("// This file is generated by TernaryTreeGenerator.cs,");
            streamWriter.WriteLine("// and is used by the TernaryTreeRO class.\n");
            streamWriter.WriteLine("//");
            streamWriter.WriteLine("// It contains the state for a ternary tree used to map HTML\n");
            streamWriter.WriteLine("// keywords to XSL output properties.\n");
            streamWriter.WriteLine("//");
            streamWriter.WriteLine("// Do not modify this file directly! (as if you could)\n");
            streamWriter.WriteLine("//");

            streamWriter.WriteLine("namespace System.Xml {");
            streamWriter.WriteLine("{0}internal abstract class HtmlTernaryTree {{", indent1);


        }

        //
        //    Dump a C-style BYTE table to the output file.  This table will contain the
        //    persisted state of this ternary tree.
        //

        /// <include file='doc\TernaryTreeGenerator.uex' path='docs/doc[@for="TernaryTreeRW.OutputFileFooter"]/*' />
        public void OutputFileFooter(StreamWriter streamWriter) {
            streamWriter.WriteLine("    }");
            streamWriter.WriteLine("}");
            streamWriter.Flush();
        }


        //----------------------------------------------------------------------------
        //    dumpTree()
        //
        //    Dump a C-style BYTE table to the output file.  This table will contain the
        //    persisted state of this ternary tree.
        //

        /// <include file='doc\TernaryTreeGenerator.uex' path='docs/doc[@for="TernaryTreeRW.dumpTree"]/*' />
        public void dumpTree(StreamWriter streamWriter, String wszName) {
            int n = 0;
            string indent3 = "           ";
            string indent2 = "        ";
            string indent1 = "    ";

            streamWriter.WriteLine("{0}internal static byte [] {1} = {{", indent2, wszName);
            streamWriter.Write(indent3);

            for (int pos = 1; pos < endNodePos; pos ++ ) {
                streamWriter.Write( "{0},{1},{2},{3},", nodeBuffer[pos, (int)TernaryTreeByte.characterByte],
                                      nodeBuffer[pos, (int)TernaryTreeByte.leftTree],
                                      nodeBuffer[pos, (int)TernaryTreeByte.rightTree],
                                      nodeBuffer[pos, (int)TernaryTreeByte.data]);
                n++;

                if (n % 7 == 0 ){
                    streamWriter.WriteLine();
                    streamWriter.Write(indent3);
                } else {
                    streamWriter.Write(" ");
                }
            }
            streamWriter.WriteLine();
            streamWriter.WriteLine("{0}}};",indent2);
            streamWriter.Flush();
        }
    }


    /// <include file='doc\TernaryTreeGenerator.uex' path='docs/doc[@for="XsltableMain"]/*' />
    public class XsltableMain {
        /// <include file='doc\TernaryTreeGenerator.uex' path='docs/doc[@for="XsltableMain.Main"]/*' />
        public static void Main() {
            try {
                FileStream fs = new FileStream("..\\..\\..\\HtmlTernaryTree.cs", FileMode.Create, FileAccess.Write, FileShare.Read);
                StreamWriter streamWriter = new StreamWriter(fs);
                TernaryTreeRW ternaryTreeRW = new TernaryTreeRW();
                ternaryTreeRW.OutputFileHeader(streamWriter);
                ternaryTreeRW.GenerateHtmlElementTable();
                ternaryTreeRW.dumpTree(streamWriter, "htmlElements");
                ternaryTreeRW = new TernaryTreeRW();
                ternaryTreeRW.GenerateHtmlAttributeTable();
                ternaryTreeRW.dumpTree(streamWriter, "htmlAttributes");
                ternaryTreeRW.OutputFileFooter(streamWriter);
                fs.Flush();
                fs.Close();
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }

        }
    }
}
