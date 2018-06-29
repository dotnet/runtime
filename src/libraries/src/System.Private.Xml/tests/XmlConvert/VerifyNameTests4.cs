// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    internal class VerifyNameTests4 : CTestCase
    {
        public override void AddChildren()
        {
            AddChild(new CVariation(TFS_469847) { Attribute = new Variation("Test for VerifyNMTOKEN(foo\U00010000bar)") { Param = 1 } });
            AddChild(new CVariation(TFS_469847) { Attribute = new Variation("Test for VerifyNCName(foo\U00010000bar)") { Param = 3 } });
            AddChild(new CVariation(TFS_469847) { Attribute = new Variation("Test for VerifyTOKEN(foo\U00010000bar)") { Param = 4 } });
            AddChild(new CVariation(TFS_469847) { Attribute = new Variation("Test for VerifyXmlChars(foo\U00010000bar)") { Param = 5 } });
            AddChild(new CVariation(TFS_469847) { Attribute = new Variation("Test for VerifyName(foo\U00010000bar)") { Param = 2 } });
            AddChild(new CVariation(VerifyXmlCharsTests) { Attribute = new Variation("9.Test for VerifyXmlChars(a\udfff\udbffb)") { Params = new object[] { 9, typeof(XmlException) } } });
            AddChild(new CVariation(VerifyXmlCharsTests) { Attribute = new Variation("5.Test for VerifyXmlChars(a\udbff\udfffb)") { Params = new object[] { 5, null } } });
            AddChild(new CVariation(VerifyXmlCharsTests) { Attribute = new Variation("8.Test for VerifyXmlChars(abcddcba\udbff\udfff)") { Params = new object[] { 8, null } } });
            AddChild(new CVariation(VerifyXmlCharsTests) { Attribute = new Variation("1.Test for VerifyXmlChars(null)") { Params = new object[] { 1, typeof(ArgumentNullException) } } });
            AddChild(new CVariation(VerifyXmlCharsTests) { Attribute = new Variation("10.Test for VerifyXmlChars(a\udfffb)") { Params = new object[] { 10, typeof(XmlException) } } });
            AddChild(new CVariation(VerifyXmlCharsTests) { Attribute = new Variation("11.Test for VerifyXmlChars(a\udbffb)") { Params = new object[] { 11, typeof(XmlException) } } });
            AddChild(new CVariation(VerifyXmlCharsTests) { Attribute = new Variation("12.Test for VerifyXmlChars(abcd\udbff \udfffdcba)") { Params = new object[] { 12, typeof(XmlException) } } });
            AddChild(new CVariation(VerifyXmlCharsTests) { Attribute = new Variation("13.Test for VerifyXmlChars(\uffffabcd\ud801\udc01dcba)") { Params = new object[] { 13, typeof(XmlException) } } });
            AddChild(new CVariation(VerifyXmlCharsTests) { Attribute = new Variation("14.Test for VerifyXmlChars(abcd\uffff\ud801\udc01dcba)") { Params = new object[] { 14, typeof(XmlException) } } });
            AddChild(new CVariation(VerifyXmlCharsTests) { Attribute = new Variation("15.Test for VerifyXmlChars(abcd\ud801\udc01dcba\uffff)") { Params = new object[] { 15, typeof(XmlException) } } });
            AddChild(new CVariation(VerifyXmlCharsTests) { Attribute = new Variation("6.Test for VerifyXmlChars(abcd\udbff\udfffdcba)") { Params = new object[] { 6, null } } });
            AddChild(new CVariation(VerifyXmlCharsTests) { Attribute = new Variation("7.Test for VerifyXmlChars(\udbff\udfffabcddcba)") { Params = new object[] { 7, null } } });
            AddChild(new CVariation(VerifyXmlCharsTests) { Attribute = new Variation("2.Test for VerifyXmlChars(string.Empty)") { Params = new object[] { 2, null } } });
            AddChild(new CVariation(VerifyXmlCharsTests) { Attribute = new Variation("3.Test for VerifyXmlChars(a)") { Params = new object[] { 3, null } } });
            AddChild(new CVariation(VerifyXmlCharsTests) { Attribute = new Variation("4.Test for VerifyXmlChars(ab)") { Params = new object[] { 4, null } } });
            AddChild(new CVariation(VerifyPublicId) { Attribute = new Variation("Test for VerifyPublicId (null)") { Params = new object[] { null, typeof(ArgumentNullException) } } });
            AddChild(new CVariation(VerifyPublicId) { Attribute = new Variation("Test for VerifyPublicId (string.Empty)") { Params = new object[] { "", null } } });
            AddChild(new CVariation(VerifyPublicId) { Attribute = new Variation("Test for VerifyPublicId (!)") { Params = new object[] { "!", null } } });
            AddChild(new CVariation(VerifyPublicId) { Attribute = new Variation("Test for VerifyPublicId (ab)") { Params = new object[] { "ab", null } } });
            AddChild(new CVariation(VerifyPublicId) { Attribute = new Variation("Test for VerifyPublicId (+,./)") { Params = new object[] { "+,./", null } } });
            AddChild(new CVariation(VerifyPublicId) { Attribute = new Variation("Test for VerifyPublicId (a-zA-Z0-9-'( )+,./:=?;!*#\n@$_%\\r)") { Params = new object[] { "a-zA-Z0-9-'( )+,./:=?;!*#\n@$_%\r", null } } });
            AddChild(new CVariation(VerifyPublicId) { Attribute = new Variation("Test for VerifyPublicId (\\udb01\\udc01abc)") { Params = new object[] { "\udb01\udc01abc", typeof(XmlException) } } });
            AddChild(new CVariation(VerifyPublicId) { Attribute = new Variation("Test for VerifyPublicId (a\\udb01\\udc01bc)") { Params = new object[] { "a\udb01\udc01bc", typeof(XmlException) } } });
            AddChild(new CVariation(VerifyPublicId) { Attribute = new Variation("Test for VerifyPublicId (abc\\udb01\\udc01)") { Params = new object[] { "abc\udb01\udc01", typeof(XmlException) } } });
            AddChild(new CVariation(VerifyPublicId) { Attribute = new Variation("Test for VerifyPublicId (\\udb01abc)") { Params = new object[] { "\udb01abc", typeof(XmlException) } } });
            AddChild(new CVariation(VerifyPublicId) { Attribute = new Variation("Test for VerifyPublicId (a\\udb01abc)") { Params = new object[] { "a\udb01abc", typeof(XmlException) } } });
            AddChild(new CVariation(VerifyPublicId) { Attribute = new Variation("Test for VerifyPublicId (abc\\udb01)") { Params = new object[] { "abc\udb01", typeof(XmlException) } } });
            AddChild(new CVariation(VerifyPublicId) { Attribute = new Variation("Test for VerifyPublicId (\\udf01abc)") { Params = new object[] { "\udf01abc", typeof(XmlException) } } });
            AddChild(new CVariation(VerifyPublicId) { Attribute = new Variation("Test for VerifyPublicId (a\\udf01abc)") { Params = new object[] { "a\udf01abc", typeof(XmlException) } } });
            AddChild(new CVariation(VerifyPublicId) { Attribute = new Variation("Test for VerifyPublicId (abc\\udf01)") { Params = new object[] { "abc\udf01", typeof(XmlException) } } });
            AddChild(new CVariation(VerifyPublicId) { Attribute = new Variation("Test for VerifyPublicId (\\uffffabc)") { Params = new object[] { "\uffffabc", typeof(XmlException) } } });
            AddChild(new CVariation(VerifyPublicId) { Attribute = new Variation("Test for VerifyPublicId (a\\uffffabc)") { Params = new object[] { "a\uffffabc", typeof(XmlException) } } });
            AddChild(new CVariation(VerifyPublicId) { Attribute = new Variation("Test for VerifyPublicId (abc\\uffff)") { Params = new object[] { "abc\uffff", typeof(XmlException) } } });
            AddChild(new CVariation(VerifyWhitespace) { Attribute = new Variation("Test for VerifyWhitespace(\t\t)") { Params = new object[] { "\t\t", null } } });
            AddChild(new CVariation(VerifyWhitespace) { Attribute = new Variation("Test for VerifyWhitespace(  )") { Params = new object[] { "  ", null } } });
            AddChild(new CVariation(VerifyWhitespace) { Attribute = new Variation("Test for VerifyWhitespace(\n\n)") { Params = new object[] { "\n\n", null } } });
            AddChild(new CVariation(VerifyWhitespace) { Attribute = new Variation("Test for VerifyWhitespace(\r\r)") { Params = new object[] { "\r\r", null } } });
            AddChild(new CVariation(VerifyWhitespace) { Attribute = new Variation("Test for VerifyWhitespace(null)") { Params = new object[] { null, typeof(ArgumentNullException) } } });
            AddChild(new CVariation(VerifyWhitespace) { Attribute = new Variation("Test for VerifyWhitespace(   )") { Params = new object[] { "   ", null } } });
            AddChild(new CVariation(VerifyWhitespace) { Attribute = new Variation("Test for VerifyWhitespace(\n\n\n)") { Params = new object[] { "\n\n\n", null } } });
            AddChild(new CVariation(VerifyWhitespace) { Attribute = new Variation("Test for VerifyWhitespace(\r\r\r)") { Params = new object[] { "\r\r\r", null } } });
            AddChild(new CVariation(VerifyWhitespace) { Attribute = new Variation("Test for VerifyWhitespace(\t\t\t)") { Params = new object[] { "\t\t\t", null } } });
            AddChild(new CVariation(VerifyWhitespace) { Attribute = new Variation("Test for VerifyWhitespace(    )") { Params = new object[] { "    ", null } } });
            AddChild(new CVariation(VerifyWhitespace) { Attribute = new Variation("Test for VerifyWhitespace(\n\n\n\n)") { Params = new object[] { "\n\n\n\n", null } } });
            AddChild(new CVariation(VerifyWhitespace) { Attribute = new Variation("Test for VerifyWhitespace(\r\r\r\r)") { Params = new object[] { "\r\r\r\r", null } } });
            AddChild(new CVariation(VerifyWhitespace) { Attribute = new Variation("Test for VerifyWhitespace(\t\t\t\t)") { Params = new object[] { "\t\t\t\t", null } } });
            AddChild(new CVariation(VerifyWhitespace) { Attribute = new Variation("Test for VerifyWhitespace(\n\r\t\n)") { Params = new object[] { "\n\r\t\n", null } } });
            AddChild(new CVariation(VerifyWhitespace) { Attribute = new Variation("Test for VerifyWhitespace(\n\r\t )") { Params = new object[] { "\n\r\t ", null } } });
            AddChild(new CVariation(VerifyWhitespace) { Attribute = new Variation("Test for VerifyWhitespace(a\n\r\t\n)") { Params = new object[] { "a\n\r\t\n", typeof(XmlException) } } });
            AddChild(new CVariation(VerifyWhitespace) { Attribute = new Variation("Test for VerifyWhitespace(\n\r\t\na)") { Params = new object[] { "\n\r\t\na", typeof(XmlException) } } });
            AddChild(new CVariation(VerifyWhitespace) { Attribute = new Variation("Test for VerifyWhitespace(\n\rb\t\n)") { Params = new object[] { "\n\rb\t\n", typeof(XmlException) } } });
            AddChild(new CVariation(VerifyWhitespace) { Attribute = new Variation("Test for VerifyWhitespace(\ud801\udc01\n\r\t\n)") { Params = new object[] { "\ud801\udc01\n\r\t\n", typeof(XmlException) } } });
            AddChild(new CVariation(VerifyWhitespace) { Attribute = new Variation("Test for VerifyWhitespace(\n\r\t\n\ud801\udc01)") { Params = new object[] { "\n\r\t\n\ud801\udc01", typeof(XmlException) } } });
            AddChild(new CVariation(VerifyWhitespace) { Attribute = new Variation("Test for VerifyWhitespace(\\n\\r\\ud801\\udc01\\t\\n)") { Params = new object[] { "\n\r\ud801\udc01\t\n", typeof(XmlException) } } });
            AddChild(new CVariation(VerifyWhitespace) { Attribute = new Variation("Test for VerifyWhitespace(\\udc01\\ud801\\n\\r\\t\\n)") { Params = new object[] { "\ufffd\ufffd\ufffd\ufffd\n\r\t\n", typeof(XmlException) } } });
            AddChild(new CVariation(VerifyWhitespace) { Attribute = new Variation("Test for VerifyWhitespace(\\n\\r\\t\\n\\udc01\\ud801)") { Params = new object[] { "\n\r\t\n\ufffd\ufffd\ufffd\ufffd", typeof(XmlException) } } });
            AddChild(new CVariation(VerifyWhitespace) { Attribute = new Variation("Test for VerifyWhitespace(\\n\\r\\udc01\\ud801\\t\n)") { Params = new object[] { "\n\r\ufffd\ufffd\ufffd\ufffd\t\n", typeof(XmlException) } } });
            AddChild(new CVariation(VerifyWhitespace) { Attribute = new Variation("Test for VerifyWhitespace(\ud801\n\r\t\n)") { Params = new object[] { "\ufffd\ufffd\n\r\t\n", typeof(XmlException) } } });
            AddChild(new CVariation(VerifyWhitespace) { Attribute = new Variation("Test for VerifyWhitespace(\n\r\t\n\udc01)") { Params = new object[] { "\n\r\t\n\ufffd\ufffd", typeof(XmlException) } } });
            AddChild(new CVariation(VerifyWhitespace) { Attribute = new Variation("Test for VerifyWhitespace(\n\r\ud801\t\n)") { Params = new object[] { "\n\r\ufffd\ufffd\t\n", typeof(XmlException) } } });
            AddChild(new CVariation(VerifyWhitespace) { Attribute = new Variation("Test for VerifyWhitespace(\t)") { Params = new object[] { "\t", null } } });
            AddChild(new CVariation(VerifyWhitespace) { Attribute = new Variation("Test for VerifyWhitespace(\n\r\t\n\udc01)") { Params = new object[] { "\n\r\t\n\ufffd\ufffd", typeof(XmlException) } } });
            AddChild(new CVariation(VerifyWhitespace) { Attribute = new Variation("Test for VerifyWhitespace(\n\r\udc01\t\n)") { Params = new object[] { "\n\r\ufffd\ufffd\t\n", typeof(XmlException) } } });
            AddChild(new CVariation(VerifyWhitespace) { Attribute = new Variation("Test for VerifyWhitespace(\uffff\n\r\t\n)") { Params = new object[] { "\uffff\n\r\t\n", typeof(XmlException) } } });
            AddChild(new CVariation(VerifyWhitespace) { Attribute = new Variation("Test for VerifyWhitespace(\n\r\t\n\uffff)") { Params = new object[] { "\n\r\t\n\uffff", typeof(XmlException) } } });
            AddChild(new CVariation(VerifyWhitespace) { Attribute = new Variation("Test for VerifyWhitespace(\n\r\uffff\t\n)") { Params = new object[] { "\n\r\uffff\t\n", typeof(XmlException) } } });
            AddChild(new CVariation(VerifyWhitespace) { Attribute = new Variation("Test for VerifyWhitespace(string.Empty)") { Params = new object[] { "", null } } });
            AddChild(new CVariation(VerifyWhitespace) { Attribute = new Variation("Test for VerifyWhitespace(\udc01\n\r\t\n)") { Params = new object[] { "\ufffd\ufffd\n\r\t\n", typeof(XmlException) } } });
            AddChild(new CVariation(VerifyWhitespace) { Attribute = new Variation("Test for VerifyWhitespace( )") { Params = new object[] { " ", null } } });
            AddChild(new CVariation(VerifyWhitespace) { Attribute = new Variation("Test for VerifyWhitespace(\n)") { Params = new object[] { "\n", null } } });
            AddChild(new CVariation(VerifyWhitespace) { Attribute = new Variation("Test for VerifyWhitespace(\r)") { Params = new object[] { "\r", null } } });
        }

        private int TFS_469847()
        {
            var param = (int)CurVariation.Param;
            try
            {
                switch (param)
                {
                    case 1:
                        XmlConvert.VerifyNMTOKEN("foo\ud800\udc00bar");
                        break;
                    case 2:
                        XmlConvert.VerifyName("foo\ud800\udc00bar");
                        break;
                    case 3:
                        XmlConvert.VerifyNCName("foo\ud800\udc00bar");
                        break;
                    case 5:
                        XmlConvert.VerifyXmlChars("foo\ud800\udc00bar");
                        break;
                }
            }
            catch (XmlException e)
            {
                CError.WriteLine(e.Message);
                return TEST_PASS;
            }
            return (param == 4 || param == 5) ? TEST_PASS : TEST_FAIL;
        }

        private int VerifyPublicId()
        {
            var inputString = (string)CurVariation.Params[0];
            var exceptionType = (Type)CurVariation.Params[1];

            try
            {
                string outString = XmlConvert.VerifyPublicId(inputString);
                CError.Compare(inputString, outString, "Content");
            }
            catch (ArgumentNullException e)
            {
                return (exceptionType != null && e.GetType().Name == exceptionType.Name) ? TEST_PASS : TEST_FAIL;
            }
            catch (XmlException e)
            {
                CError.WriteLine(e.LineNumber);
                CError.WriteLine(e.LinePosition);
                return (exceptionType != null && e.GetType().Name == exceptionType.Name) ? TEST_PASS : TEST_FAIL;
            }
            return exceptionType == null ? TEST_PASS : TEST_FAIL;
        }

        /// <summary>
        /// Params[] = { inputString, shouldThrow }
        /// </summary>
        private int VerifyWhitespace()
        {
            var inputString = (string)CurVariation.Params[0];
            var exceptionType = (Type)CurVariation.Params[1];

            try
            {
                string outString = XmlConvert.VerifyWhitespace(inputString);
                CError.Compare(inputString, outString, "Content");
            }
            catch (ArgumentNullException e)
            {
                return (exceptionType != null && e.GetType().Name == exceptionType.Name) ? TEST_PASS : TEST_FAIL;
            }
            catch (XmlException e)
            {
                CError.WriteLine(e.LineNumber);
                CError.WriteLine(e.LinePosition);
                return (exceptionType != null && e.GetType().Name == exceptionType.Name) ? TEST_PASS : TEST_FAIL;
            }
            return exceptionType == null ? TEST_PASS : TEST_FAIL;
        }

        private int VerifyXmlCharsTests()
        {
            var param = (int)CurVariation.Params[0];
            var exceptionType = (Type)CurVariation.Params[1];
            string inputString = string.Empty;
            switch (param)
            {
                case 1:
                    inputString = null;
                    break;
                case 2:
                    inputString = "";
                    break;
                case 3:
                    inputString = "a";
                    break;
                case 4:
                    inputString = "ab";
                    break;
                case 5:
                    inputString = "a\udbff\udfffb";
                    break;
                case 6:
                    inputString = "abcd\udbff\udfffdcba";
                    break;
                case 7:
                    inputString = "\udbff\udfffabcddcba";
                    break;
                case 8:
                    inputString = "abcddcba\udbff\udfff";
                    break;
                case 9:
                    inputString = "a\udfff\udbffb";
                    break;
                case 10:
                    inputString = "a\udfffb";
                    break;
                case 11:
                    inputString = "a\udbffb";
                    break;
                case 12:
                    inputString = "abcd\udbff \udfffdcba";
                    break;
                case 13:
                    inputString = "\uffffabcd\ud801\udc01dcba";
                    break;
                case 14:
                    inputString = "abcd\uffff\ud801\udc01dcba";
                    break;
                case 15:
                    inputString = "abcd\ud801\udc01dcba\uffff";
                    break;
            }
            try
            {
                string outString = XmlConvert.VerifyXmlChars(inputString);
                CError.Compare(inputString, outString, "Content");
            }
            catch (ArgumentNullException e)
            {
                return (exceptionType != null && e.GetType().Name == exceptionType.Name) ? TEST_PASS : TEST_FAIL;
            }
            catch (XmlException e)
            {
                CError.WriteLine(e.LineNumber);
                CError.WriteLine(e.LinePosition);
                return (exceptionType != null && e.GetType().Name == exceptionType.Name) ? TEST_PASS : TEST_FAIL;
            }
            return exceptionType == null ? TEST_PASS : TEST_FAIL;
        }
    }
}
