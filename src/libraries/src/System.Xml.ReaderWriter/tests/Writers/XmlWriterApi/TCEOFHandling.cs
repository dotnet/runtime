// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    public partial class TCEOFHandling : XmlFactoryWriterTestCaseBase
    {
        // Type is System.Xml.Tests.TCEOFHandling
        // Test Case
        public override void AddChildren()
        {
            if (!(WriterType == WriterType.UnicodeWriter || WriterType == WriterType.UTF8Writer || WriterType == WriterType.WrappedWriter
                || WriterType == WriterType.CharCheckingWriter || WriterType == WriterType.UTF8WriterIndent || WriterType == WriterType.UnicodeWriterIndent))
            {
                return;
            }
            // for function EOF_Handling_01
            {
                this.AddChild(new CVariation(EOF_Handling_01) { Attribute = new Variation("NewLineHandling Default value - NewLineHandling.Replace") { id = 1, Pri = 0 } });
            }


            // for function EOF_Handling_02
            {
                this.AddChild(new CVariation(EOF_Handling_02) { Attribute = new Variation("XmlWriter creation with NewLineHandling.None") { Param = 2, id = 4, Pri = 0 } });
                this.AddChild(new CVariation(EOF_Handling_02) { Attribute = new Variation("XmlWriter creation with NewLineHandling.Replace") { Param = 0, id = 3, Pri = 0 } });
                this.AddChild(new CVariation(EOF_Handling_02) { Attribute = new Variation("XmlWriter creation with NewLineHandling.Entitize") { Param = 1, id = 2, Pri = 0 } });
            }


            // for function EOF_Handling_03
            {
                this.AddChild(new CVariation(EOF_Handling_03) { Attribute = new Variation("Check for combinations of NewLine characters in element with 'Entitize'") { Param = 1, id = 5, Pri = 0 } });
                this.AddChild(new CVariation(EOF_Handling_03) { Attribute = new Variation("Check for combinations of NewLine characters in element with 'Replace'") { Param = 0, id = 6, Pri = 0 } });
                this.AddChild(new CVariation(EOF_Handling_03) { Attribute = new Variation("Check for combinations of NewLine characters in element with 'None'") { Param = 2, id = 7, Pri = 0 } });
            }


            // for function EOF_Handling_04
            {
                this.AddChild(new CVariation(EOF_Handling_04) { Attribute = new Variation("Check for combinations of entities in element with 'None'") { Param = 2, id = 10, Pri = 0 } });
                this.AddChild(new CVariation(EOF_Handling_04) { Attribute = new Variation("Check for combinations of entities in element with 'Replace'") { Param = 0, id = 9, Pri = 0 } });
                this.AddChild(new CVariation(EOF_Handling_04) { Attribute = new Variation("Check for combinations of entities in element with 'Entitize'") { Param = 1, id = 8, Pri = 0 } });
            }


            // for function EOF_Handling_05
            {
                this.AddChild(new CVariation(EOF_Handling_05) { Attribute = new Variation("Check for combinations of NewLine characters and entities in element with 'Replace'") { Param = 0, id = 12, Pri = 0 } });
                this.AddChild(new CVariation(EOF_Handling_05) { Attribute = new Variation("Check for combinations of NewLine characters and entities in element with 'Entitize'") { Param = 1, id = 11, Pri = 0 } });
                this.AddChild(new CVariation(EOF_Handling_05) { Attribute = new Variation("Check for combinations of NewLine characters and entities in element with 'None'") { Param = 2, id = 13, Pri = 0 } });
            }


            // for function EOF_Handling_06
            {
                this.AddChild(new CVariation(EOF_Handling_06) { Attribute = new Variation("Check for tab character in element with 'Entitize'") { Param = 1, id = 14, Pri = 0 } });
                this.AddChild(new CVariation(EOF_Handling_06) { Attribute = new Variation("Check for tab character in element with 'Replace'") { Param = 0, id = 15, Pri = 0 } });
                this.AddChild(new CVariation(EOF_Handling_06) { Attribute = new Variation("Check for tab character in element with 'None'") { Param = 2, id = 16, Pri = 0 } });
            }


            // for function EOF_Handling_07
            {
                this.AddChild(new CVariation(EOF_Handling_07) { Attribute = new Variation("Check for combinations of NewLine characters in attribute with 'Entitize'") { Param = 1, id = 17, Pri = 0 } });
                this.AddChild(new CVariation(EOF_Handling_07) { Attribute = new Variation("Check for combinations of NewLine characters in attribute with 'None'") { Param = 2, id = 19, Pri = 0 } });
                this.AddChild(new CVariation(EOF_Handling_07) { Attribute = new Variation("Check for combinations of NewLine characters in attribute with 'Replace'") { Param = 0, id = 18, Pri = 0 } });
            }


            // for function EOF_Handling_08
            {
                this.AddChild(new CVariation(EOF_Handling_08) { Attribute = new Variation("Check for combinations of entities in attribute with 'Entitize'") { Param = 1, id = 20, Pri = 0 } });
                this.AddChild(new CVariation(EOF_Handling_08) { Attribute = new Variation("Check for combinations of entities in attribute with 'None'") { Param = 2, id = 22, Pri = 0 } });
                this.AddChild(new CVariation(EOF_Handling_08) { Attribute = new Variation("Check for combinations of entities in attribute with 'Replace'") { Param = 0, id = 21, Pri = 0 } });
            }


            // for function EOF_Handling_09
            {
                this.AddChild(new CVariation(EOF_Handling_09) { Attribute = new Variation("Check for combinations of NewLine characters and entities in element with 'Entitize'") { Param = 1, id = 23, Pri = 0 } });
                this.AddChild(new CVariation(EOF_Handling_09) { Attribute = new Variation("Check for combinations of NewLine characters and entities in element with 'Replace'") { Param = 0, id = 24, Pri = 0 } });
                this.AddChild(new CVariation(EOF_Handling_09) { Attribute = new Variation("Check for combinations of NewLine characters and entities in element with 'None'") { Param = 2, id = 25, Pri = 0 } });
            }


            // for function EOF_Handling_10
            {
                this.AddChild(new CVariation(EOF_Handling_10) { Attribute = new Variation("Check for tab character in attribute with 'Replace'") { Param = 0, id = 27, Pri = 0 } });
                this.AddChild(new CVariation(EOF_Handling_10) { Attribute = new Variation("Check for tab character in attribute with 'None'") { Param = 2, id = 28, Pri = 0 } });
                this.AddChild(new CVariation(EOF_Handling_10) { Attribute = new Variation("Check for tab character in attribute with 'Entitize'") { Param = 1, id = 26, Pri = 0 } });
            }


            // for function EOF_Handling_11
            {
                this.AddChild(new CVariation(EOF_Handling_11) { Attribute = new Variation("NewLineChars and IndentChars Default values and test for proper indentation, Entitize") { Param = 1, id = 29, Pri = 1 } });
                this.AddChild(new CVariation(EOF_Handling_11) { Attribute = new Variation("NewLineChars and IndentChars Default values and test for proper indentation, None") { Param = 2, id = 31, Pri = 1 } });
                this.AddChild(new CVariation(EOF_Handling_11) { Attribute = new Variation("NewLineChars and IndentChars Default values and test for proper indentation, Replace") { Param = 0, id = 30, Pri = 1 } });
            }


            // for function EOF_Handling_13
            {
                this.AddChild(new CVariation(EOF_Handling_13) { Attribute = new Variation("6.Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; None, '&#xA;', '  '") { Params = new object[] { NewLineHandling.None, "&#xA;", "  " }, id = 37, Pri = 2 } });
                this.AddChild(new CVariation(EOF_Handling_13) { Attribute = new Variation("1.Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; Entitize, '\\r', '  '") { Params = new object[] { NewLineHandling.Entitize, "\r", "  " }, id = 32, Pri = 2 } });
                this.AddChild(new CVariation(EOF_Handling_13) { Attribute = new Variation("7.Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; Entitize, '\\r', '\\n'") { Params = new object[] { NewLineHandling.Entitize, "\r", "\n" }, id = 38, Pri = 2 } });
                this.AddChild(new CVariation(EOF_Handling_13) { Attribute = new Variation("9.Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; None, '\\r', '\\n'") { Params = new object[] { NewLineHandling.None, "\r", "\n" }, id = 40, Pri = 2 } });
                this.AddChild(new CVariation(EOF_Handling_13) { Attribute = new Variation("2.Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; Replace, '\\r', '  '") { Params = new object[] { NewLineHandling.Replace, "\r", "  " }, id = 33, Pri = 2 } });
                this.AddChild(new CVariation(EOF_Handling_13) { Attribute = new Variation("3.Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; None, '\\r', '  '") { Params = new object[] { NewLineHandling.None, "\r", "  " }, id = 34, Pri = 2 } });
                this.AddChild(new CVariation(EOF_Handling_13) { Attribute = new Variation("4.Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; Entitize, '&#xA;', '  '") { Params = new object[] { NewLineHandling.Entitize, "&#xA;", "  " }, id = 35, Pri = 2 } });
                this.AddChild(new CVariation(EOF_Handling_13) { Attribute = new Variation("5.Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; Replace, '&#xA;', '  '") { Params = new object[] { NewLineHandling.Replace, "&#xA;", "  " }, id = 36, Pri = 2 } });
                this.AddChild(new CVariation(EOF_Handling_13) { Attribute = new Variation("8.Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; Replace, '\\r', '\\n'") { Params = new object[] { NewLineHandling.Replace, "\r", "\n" }, id = 39, Pri = 2 } });
            }


            // for function EOF_Handling_14
            {
                this.AddChild(new CVariation(EOF_Handling_14) { Attribute = new Variation("NewLine handling in text node when Indent=true; None, '---'") { Params = new object[] { NewLineHandling.None, "---" }, id = 49, Pri = 2 } });
                this.AddChild(new CVariation(EOF_Handling_14) { Attribute = new Variation("NewLine handling in text node when Indent=true; Entitize, '\\r\\n'") { Params = new object[] { NewLineHandling.Entitize, "\r\n" }, id = 41, Pri = 1 } });
                this.AddChild(new CVariation(EOF_Handling_14) { Attribute = new Variation("NewLine handling in text node when Indent=true; Replace, '\\r\\n'") { Params = new object[] { NewLineHandling.Replace, "\r\n" }, id = 42, Pri = 1 } });
                this.AddChild(new CVariation(EOF_Handling_14) { Attribute = new Variation("NewLine handling in text node when Indent=true; None, '\\r\\n'") { Params = new object[] { NewLineHandling.None, "\r\n" }, id = 43, Pri = 1 } });
                this.AddChild(new CVariation(EOF_Handling_14) { Attribute = new Variation("NewLine handling in text node when Indent=true; Entitize, '\\r'") { Params = new object[] { NewLineHandling.Entitize, "\r" }, id = 44, Pri = 2 } });
                this.AddChild(new CVariation(EOF_Handling_14) { Attribute = new Variation("NewLine handling in text node when Indent=true; Replace, '\\r'") { Params = new object[] { NewLineHandling.Replace, "\r" }, id = 45, Pri = 2 } });
                this.AddChild(new CVariation(EOF_Handling_14) { Attribute = new Variation("NewLine handling in text node when Indent=true; None, '\\r'") { Params = new object[] { NewLineHandling.None, "\r" }, id = 46, Pri = 2 } });
                this.AddChild(new CVariation(EOF_Handling_14) { Attribute = new Variation("NewLine handling in text node when Indent=true; Entitize, '---'") { Params = new object[] { NewLineHandling.Entitize, "---" }, id = 47, Pri = 2 } });
                this.AddChild(new CVariation(EOF_Handling_14) { Attribute = new Variation("NewLine handling in text node when Indent=true; Replace, '---'") { Params = new object[] { NewLineHandling.Replace, "---" }, id = 48, Pri = 2 } });
            }


            // for function EOF_Handling_15
            {
                this.AddChild(new CVariation(EOF_Handling_15) { Attribute = new Variation("NewLine handling in attribute when Indent=true; None, '---'") { Params = new object[] { NewLineHandling.None, "---" } } });
                this.AddChild(new CVariation(EOF_Handling_15) { Attribute = new Variation("NewLine handling in attribute when Indent=true; Entitize, '---'") { Params = new object[] { NewLineHandling.Entitize, "---" }, id = 54, Pri = 2 } });
                this.AddChild(new CVariation(EOF_Handling_15) { Attribute = new Variation("NewLine handling in attribute when Indent=true; Replace, '---'") { Params = new object[] { NewLineHandling.Replace, "---" }, id = 55, Pri = 2 } });
                this.AddChild(new CVariation(EOF_Handling_15) { Attribute = new Variation("NewLine handling in attribute when Indent=true; Entitize, '\\r\\n'") { Params = new object[] { NewLineHandling.Entitize, "\r\n" }, id = 50, Pri = 1 } });
                this.AddChild(new CVariation(EOF_Handling_15) { Attribute = new Variation("NewLine handling in attribute when Indent=true; Replace, '\\r\\n'") { Params = new object[] { NewLineHandling.Replace, "\r\n" }, id = 51, Pri = 1 } });
                this.AddChild(new CVariation(EOF_Handling_15) { Attribute = new Variation("NewLine handling in attribute when Indent=true; None, '\\r\\n'") { Params = new object[] { NewLineHandling.None, "\r\n" }, id = 52, Pri = 1 } });
                this.AddChild(new CVariation(EOF_Handling_15) { Attribute = new Variation("NewLine handling in attribute when Indent=true; Entitize, '\\r'") { Params = new object[] { NewLineHandling.Entitize, "\r" }, id = 53, Pri = 2 } });
                this.AddChild(new CVariation(EOF_Handling_15) { Attribute = new Variation("NewLine handling in attribute when Indent=true; Replace, '\\r'") { Params = new object[] { NewLineHandling.Replace, "\r" }, id = 54, Pri = 2 } });
                this.AddChild(new CVariation(EOF_Handling_15) { Attribute = new Variation("NewLine handling in attribute when Indent=true; None, '\\r'") { Params = new object[] { NewLineHandling.None, "\r" }, id = 54, Pri = 2 } });
            }


            // for function EOF_Handling_16
            {
                this.AddChild(new CVariation(EOF_Handling_16) { Attribute = new Variation("NewLine handling betwwen attributes when NewLineOnAttributes=true; Replace, '\\r\\n'") { Params = new object[] { NewLineHandling.Replace, "\r\n" }, id = 57, Pri = 1 } });
                this.AddChild(new CVariation(EOF_Handling_16) { Attribute = new Variation("NewLine handling between attributes when NewLineOnAttributes=true; Entitize, '\\r\\n'") { Params = new object[] { NewLineHandling.Entitize, "\r\n" }, id = 56, Pri = 1 } });
                this.AddChild(new CVariation(EOF_Handling_16) { Attribute = new Variation("NewLine handling between attributes when NewLineOnAttributes=true; None, '\\r\\n'") { Params = new object[] { NewLineHandling.None, "\r\n" }, id = 58, Pri = 1 } });
                this.AddChild(new CVariation(EOF_Handling_16) { Attribute = new Variation("NewLine handling between attributes when NewLineOnAttributes=true; Entitize, '\\r'") { Params = new object[] { NewLineHandling.Entitize, "\r" }, id = 59, Pri = 2 } });
                this.AddChild(new CVariation(EOF_Handling_16) { Attribute = new Variation("NewLine handling between attributes when NewLineOnAttributes=true; Replace, '\\r'") { Params = new object[] { NewLineHandling.Replace, "\r" }, id = 60, Pri = 2 } });
                this.AddChild(new CVariation(EOF_Handling_16) { Attribute = new Variation("NewLine handling between attributes when NewLineOnAttributes=true; None, '\\r'") { Params = new object[] { NewLineHandling.None, "\r" }, id = 61, Pri = 2 } });
                this.AddChild(new CVariation(EOF_Handling_16) { Attribute = new Variation("NewLine handling between attributes when NewLineOnAttributes=true; Entitize, '---'") { Params = new object[] { NewLineHandling.Entitize, "---" }, id = 62, Pri = 2 } });
                this.AddChild(new CVariation(EOF_Handling_16) { Attribute = new Variation("NewLine handling between attributes when NewLineOnAttributes=true; Replace, '---'") { Params = new object[] { NewLineHandling.Replace, "---" }, id = 63, Pri = 2 } });
                this.AddChild(new CVariation(EOF_Handling_16) { Attribute = new Variation("NewLine handling between attributes when NewLineOnAttributes=true; None, '---'") { Params = new object[] { NewLineHandling.None, "---" }, id = 64, Pri = 2 } });
            }


            // for function EOF_Handling_17
            {
                this.AddChild(new CVariation(EOF_Handling_17) { Attribute = new Variation("Sanity tests for various types of nodes with 'Entitize'") { Param = 1, id = 65, Pri = 0 } });
                this.AddChild(new CVariation(EOF_Handling_17) { Attribute = new Variation("Sanity tests for various types of nodes with 'Replace'") { Param = 0, id = 66, Pri = 0 } });
                this.AddChild(new CVariation(EOF_Handling_17) { Attribute = new Variation("Sanity tests for various types of nodes with 'None'") { Param = 2, id = 67, Pri = 0 } });
            }


            // for function EOF_Handling_18
            {
                this.AddChild(new CVariation(EOF_Handling_18) { Attribute = new Variation("Custom NewLineChars inside CDATA & Comment when Indent=true; None, '\\r\\n'") { Params = new object[] { NewLineHandling.None, "\r\n" }, id = 70, Pri = 1 } });
                this.AddChild(new CVariation(EOF_Handling_18) { Attribute = new Variation("Custom NewLineChars inside CDATA & Comment when Indent=true; None, '---'") { Params = new object[] { NewLineHandling.None, "---" }, id = 76, Pri = 2 } });
                this.AddChild(new CVariation(EOF_Handling_18) { Attribute = new Variation("Custom NewLineChars inside CDATA & Comment when Indent=true; Replace, '\\r\\n'") { Params = new object[] { NewLineHandling.Replace, "\r\n" }, id = 69, Pri = 1 } });
                this.AddChild(new CVariation(EOF_Handling_18) { Attribute = new Variation("Custom NewLineChars inside CDATA & Comment when Indent=true; Entitize, '\\r\\n'") { Params = new object[] { NewLineHandling.Entitize, "\r\n" }, id = 68, Pri = 1 } });
                this.AddChild(new CVariation(EOF_Handling_18) { Attribute = new Variation("Custom NewLineChars inside CDATA & Comment when Indent=true; Entitize, '\\r'") { Params = new object[] { NewLineHandling.Entitize, "\r" }, id = 71, Pri = 2 } });
                this.AddChild(new CVariation(EOF_Handling_18) { Attribute = new Variation("Custom NewLineChars inside CDATA & Comment when Indent=true; Replace, '\\r'") { Params = new object[] { NewLineHandling.Replace, "\r" }, id = 72, Pri = 2 } });
                this.AddChild(new CVariation(EOF_Handling_18) { Attribute = new Variation("Custom NewLineChars inside CDATA & Comment when Indent=true; None, '\\r'") { Params = new object[] { NewLineHandling.None, "\r" }, id = 73, Pri = 2 } });
                this.AddChild(new CVariation(EOF_Handling_18) { Attribute = new Variation("Custom NewLineChars inside CDATA & Comment when Indent=true; Entitize, '---'") { Params = new object[] { NewLineHandling.Entitize, "---" }, id = 74, Pri = 2 } });
                this.AddChild(new CVariation(EOF_Handling_18) { Attribute = new Variation("Custom NewLineChars inside CDATA & Comment when Indent=true; Replace, '---'") { Params = new object[] { NewLineHandling.Replace, "---" }, id = 75, Pri = 2 } });
            }


            // for function EOF_Handling_19
            {
                this.AddChild(new CVariation(EOF_Handling_19) { Attribute = new Variation("Wrapped writer tests for various types of nodes with 'Replace'") { Param = 0, Priority = 2 } });
                this.AddChild(new CVariation(EOF_Handling_19) { Attribute = new Variation("Wrapped writer tests for various types of nodes with 'None'") { Param = 2, Priority = 2 } });
                this.AddChild(new CVariation(EOF_Handling_19) { Attribute = new Variation("Wrapped writer tests for various types of nodes with 'Entitize'") { Param = 1, Priority = 2 } });
            }


            // for function EOF_Handling_20
            {
                this.AddChild(new CVariation(EOF_Handling_20) { Attribute = new Variation("XmlWriterSettings.Indent - valid values") { Param = 12, Priority = 2 } });
                this.AddChild(new CVariation(EOF_Handling_20) { Attribute = new Variation("XmlWriterSettings.Indent - valid values") { Param = 11, Priority = 2 } });
                this.AddChild(new CVariation(EOF_Handling_20) { Attribute = new Variation("XmlWriterSettings.IndentChars - valid values") { Param = 1, Priority = 2 } });
                this.AddChild(new CVariation(EOF_Handling_20) { Attribute = new Variation("XmlWriterSettings.NewLineChars - valid values") { Param = 2, Priority = 2 } });
                this.AddChild(new CVariation(EOF_Handling_20) { Attribute = new Variation("XmlWriterSettings.IndentChars - valid values") { Param = 3, Priority = 2 } });
                this.AddChild(new CVariation(EOF_Handling_20) { Attribute = new Variation("XmlWriterSettings.NewLineChars - valid values") { Param = 4, Priority = 2 } });
                this.AddChild(new CVariation(EOF_Handling_20) { Attribute = new Variation("XmlWriterSettings.IndentChars - valid values") { Param = 5, Priority = 2 } });
                this.AddChild(new CVariation(EOF_Handling_20) { Attribute = new Variation("XmlWriterSettings.NewLineChars - valid values") { Param = 6, Priority = 2 } });
                this.AddChild(new CVariation(EOF_Handling_20) { Attribute = new Variation("XmlWriterSettings.IndentChars - valid values") { Param = 7, Priority = 2 } });
                this.AddChild(new CVariation(EOF_Handling_20) { Attribute = new Variation("XmlWriterSettings.NewLineChars - valid values") { Param = 8, Priority = 2 } });
                this.AddChild(new CVariation(EOF_Handling_20) { Attribute = new Variation("XmlWriterSettings.IndentChars - valid values") { Param = 9, Priority = 2 } });
                this.AddChild(new CVariation(EOF_Handling_20) { Attribute = new Variation("XmlWriterSettings.NewLineChars - valid values") { Param = 10, Priority = 2 } });
            }


            // for function EOF_Handling_21
            {
                this.AddChild(new CVariation(EOF_Handling_21) { Attribute = new Variation("10.Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; Entitize, '\ud800\udc00', '\ud800\udc00'") { Params = new object[] { NewLineHandling.Entitize, "\ud800\udc00", "\ud800\udc00" } } });
                this.AddChild(new CVariation(EOF_Handling_21) { Attribute = new Variation("11.Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; Replace, '\ud800\udc00', '  '") { Params = new object[] { NewLineHandling.Replace, "\ud800\udc00", "\ud800\udc00" } } });
                this.AddChild(new CVariation(EOF_Handling_21) { Attribute = new Variation("14.Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; Replace, '&lt;&gt;', '&lt;&gt;'") { Params = new object[] { NewLineHandling.Replace, "&lt;&gt;", "&lt;&gt;" } } });
                this.AddChild(new CVariation(EOF_Handling_21) { Attribute = new Variation("12.Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; None, '\ud800\udc00', '&lt;&gt;'") { Params = new object[] { NewLineHandling.None, "\ud800\udc00", "\ud800\udc00" } } });
                this.AddChild(new CVariation(EOF_Handling_21) { Attribute = new Variation("15.Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; None, '&lt;&gt;', '&lt;&gt;'") { Params = new object[] { NewLineHandling.None, "&lt;&gt;", "&lt;&gt;" } } });
                this.AddChild(new CVariation(EOF_Handling_21) { Attribute = new Variation("13.Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; Entitize, '&lt;&gt;', '&lt;&gt;'") { Params = new object[] { NewLineHandling.Entitize, "&lt;&gt;", "&lt;&gt;" } } });
            }


            // for function EOF_Handling_22
            {
                this.AddChild(new CVariation(EOF_Handling_22) { Attribute = new Variation("18.Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; None, '\uDE40\uDA72', '\uDE40\uDA72'") { Params = new object[] { NewLineHandling.None } } });
                this.AddChild(new CVariation(EOF_Handling_22) { Attribute = new Variation("17.Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; Replace, '\uDE40\uDA72', '\uDE40\uDA72'") { Params = new object[] { NewLineHandling.Replace } } });
                this.AddChild(new CVariation(EOF_Handling_22) { Attribute = new Variation("16.Test fo proper indentation and newline handling when Indent = true, with custom NewLineChars and IndentChars; Entitize, '\uDE40\uDA72', '\uDE40\uDA72'") { Params = new object[] { NewLineHandling.Entitize } } });
            }
        }
    }
}
