// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    internal class VerifyNameTests3 : CTestCase
    {
        public override void AddChildren()
        {
            AddChild(new CVariation(v16) { Attribute = new Variation("15.Test for VerifyNCName(\ud801\r\udc01)") { Params = new object[] { 15, "invalid" } } });
            AddChild(new CVariation(v16) { Attribute = new Variation("6.Test for VerifyNCName(abcd\ralfafkjha)") { Params = new object[] { 6, "invalid" } } });
            AddChild(new CVariation(v16) { Attribute = new Variation("8.Test for VerifyNCName(abcd\tdef)") { Params = new object[] { 8, "invalid" } } });
            AddChild(new CVariation(v16) { Attribute = new Variation("9.Test for VerifyNCName( \b)") { Params = new object[] { 9, "invalid" } } });
            AddChild(new CVariation(v16) { Attribute = new Variation("10.Test for VerifyNCName(\ud801\udc01)") { Params = new object[] { 10, "invalid" } } });
            AddChild(new CVariation(v16) { Attribute = new Variation("11.Test for VerifyNCName( \ud801\udc01)") { Params = new object[] { 11, "invalid" } } });
            AddChild(new CVariation(v16) { Attribute = new Variation("12.Test for VerifyNCName(\ud801\udc01 )") { Params = new object[] { 12, "invalid" } } });
            AddChild(new CVariation(v16) { Attribute = new Variation("13.Test for VerifyNCName(\ud801 \udc01)") { Params = new object[] { 13, "invalid" } } });
            AddChild(new CVariation(v16) { Attribute = new Variation("14.Test for VerifyNCName(\ud801  \udc01)") { Params = new object[] { 14, "invalid" } } });
            AddChild(new CVariation(v16) { Attribute = new Variation("1.Test for VerifyNCName(abcd)") { Params = new object[] { 1, "valid" } } });
            AddChild(new CVariation(v16) { Attribute = new Variation("16.Test for VerifyNCName(\ud801\n\udc01)") { Params = new object[] { 16, "invalid" } } });
            AddChild(new CVariation(v16) { Attribute = new Variation("17.Test for VerifyNCName(\ud801\t\udc001)") { Params = new object[] { 17, "invalid" } } });
            AddChild(new CVariation(v16) { Attribute = new Variation("18.Test for VerifyNCName(a\ud801\udc01b)") { Params = new object[] { 18, "invalid" } } });
            AddChild(new CVariation(v16) { Attribute = new Variation("19.Test for VerifyNCName(a\udc01\ud801b)") { Params = new object[] { 19, "invalid" } } });
            AddChild(new CVariation(v16) { Attribute = new Variation("20.Test for VerifyNCName(a\ud801b)") { Params = new object[] { 20, "invalid" } } });
            AddChild(new CVariation(v16) { Attribute = new Variation("21.Test for VerifyNCName(a\udc01b)") { Params = new object[] { 21, "invalid" } } });
            AddChild(new CVariation(v16) { Attribute = new Variation("22.Test for VerifyNCName(\ud801\udc01:)") { Params = new object[] { 22, "invalid" } } });
            AddChild(new CVariation(v16) { Attribute = new Variation("23.Test for VerifyNCName(:a\ud801\udc01b)") { Params = new object[] { 23, "invalid" } } });
            AddChild(new CVariation(v16) { Attribute = new Variation("24.Test for VerifyNCName(a\ud801\udc01:b)") { Params = new object[] { 24, "invalid" } } });
            AddChild(new CVariation(v16) { Attribute = new Variation("25.Test for VerifyNCName(a\udbff\udc01\b)") { Params = new object[] { 25, "invalid" } } });
            AddChild(new CVariation(v16) { Attribute = new Variation("7.Test for VerifyNCName(abcd  def)") { Params = new object[] { 7, "invalid" } } });
            AddChild(new CVariation(v16) { Attribute = new Variation("2.Test for VerifyNCName(abcd efgh)") { Params = new object[] { 2, "invalid" } } });
            AddChild(new CVariation(v16) { Attribute = new Variation("3.Test for VerifyNCName( abcd)") { Params = new object[] { 3, "invalid" } } });
            AddChild(new CVariation(v16) { Attribute = new Variation("4.Test for VerifyNCName(abcd\nalfafkjha)") { Params = new object[] { 4, "invalid" } } });
            AddChild(new CVariation(v16) { Attribute = new Variation("5.Test for VerifyNCName(abcd\nalfafkjha)") { Params = new object[] { 5, "invalid" } } });
            AddChild(new CVariation(v17) { Attribute = new Variation("Test for VerifyNCName(null)") { Param = 3 } });
            AddChild(new CVariation(v17) { Attribute = new Variation("Test for VerifyXmlChars(null)") { Param = 5 } });
            AddChild(new CVariation(v17) { Attribute = new Variation("Test for VerifyPublicId(null)") { Param = 6 } });
            AddChild(new CVariation(v17) { Attribute = new Variation("Test for VerifyWhitespace(null)") { Param = 7 } });
            AddChild(new CVariation(v17) { Attribute = new Variation("Test for VerifyName(null)") { Param = 2 } });
            AddChild(new CVariation(v17) { Attribute = new Variation("Test for VerifyNMTOKEN(null)") { Param = 1 } });
            AddChild(new CVariation(v18) { Attribute = new Variation("Test for VerifyPublicId(String.Empty)") { Params = new object[] { 6, null } } });
            AddChild(new CVariation(v18) { Attribute = new Variation("Test for VerifyWhitespace(String.Empty)") { Params = new object[] { 7, null } } });
            AddChild(new CVariation(v18) { Attribute = new Variation("Test for VerifyName(String.Empty)") { Params = new object[] { 2, typeof(ArgumentNullException) } } });
            AddChild(new CVariation(v18) { Attribute = new Variation("Test for VerifyNCName(String.Empty)") { Params = new object[] { 3, typeof(ArgumentNullException) } } });
            AddChild(new CVariation(v18) { Attribute = new Variation("Test for VerifyXmlChars(String.Empty)") { Params = new object[] { 5, null } } });
            AddChild(new CVariation(v18) { Attribute = new Variation("Test for VerifyNMTOKEN(String.Empty)") { Params = new object[] { 1, typeof(XmlException) } } });
        }

        private int v16()
        {
            var param = (int)CurVariation.Params[0];
            string input = String.Empty;
            switch (param)
            {
                case 1:
                    input = "abcd";
                    break;
                case 2:
                    input = "abcd efgh";
                    break;
                case 3:
                    input = " abcd";
                    break;
                case 4:
                    input = "abcd ";
                    break;
                case 5:
                    input = "abcd\nalfafkjha";
                    break;
                case 6:
                    input = "abcd\ralfafkjha";
                    break;
                case 7:
                    input = "abcd  def";
                    break;
                case 8:
                    input = "abcd\tdef";
                    break;
                case 9:
                    input = " \b";
                    break;
                case 10:
                    input = "\ud801\udc01";
                    break;
                case 11:
                    input = " \ud801\udc01";
                    break;
                case 12:
                    input = "\ud801\udc01 ";
                    break;
                case 13:
                    input = "\ud801 \udc01";
                    break;
                case 14:
                    input = "\ud801  \udc01";
                    break;
                case 15:
                    input = "\ud801\r\udc01";
                    break;
                case 16:
                    input = "\ud801\n\udc01";
                    break;
                case 17:
                    input = "\ud801\t\udc01";
                    break;
                case 18:
                    input = "a\ud801\udc01b";
                    break;
                case 19:
                    input = "a\udc01\ud801b";
                    break;
                case 20:
                    input = "a\ud801b";
                    break;
                case 21:
                    input = "a\udc01b";
                    break;
                case 22:
                    input = "\ud801\udc01:";
                    break;
                case 23:
                    input = ":a\ud801\udc01b";
                    break;
                case 24:
                    input = "a\ud801\udc01b:";
                    break;
                case 25:
                    input = "a\udbff\udc01\b";
                    break;
            }

            String expected = CurVariation.Params[1].ToString();
            try
            {
                XmlConvert.VerifyNCName(input);
            }
            catch (XmlException e)
            {
                CError.WriteLine(e.LineNumber);
                CError.WriteLine(e.LinePosition);
                return (expected.Equals("invalid")) ? TEST_PASS : TEST_FAIL;
            }
            return (expected.Equals("valid")) ? TEST_PASS : TEST_FAIL;
        }

        private int v17()
        {
            var param = (int)CurVariation.Param;
            try
            {
                switch (param)
                {
                    case 1:
                        XmlConvert.VerifyNMTOKEN(null);
                        break;
                    case 2:
                        XmlConvert.VerifyName(null);
                        break;
                    case 3:
                        XmlConvert.VerifyNCName(null);
                        break;
                    case 5:
                        XmlConvert.VerifyXmlChars(null);
                        break;
                    case 6:
                        XmlConvert.VerifyPublicId(null);
                        break;
                    case 7:
                        XmlConvert.VerifyWhitespace(null);
                        break;
                }
            }
            catch (ArgumentNullException)
            {
                return param != 4 ? TEST_PASS : TEST_FAIL; //param4 -> VerifyToken should not throw here
            }
            return TEST_FAIL;
        }

        /// <summary>
        /// Params[] = { VariationNumber, Exception type (null if exception not expected) }
        /// </summary>
        /// <returns></returns>
        private int v18()
        {
            var param = (int)CurVariation.Params[0];
            var exceptionType = (Type)CurVariation.Params[1];

            try
            {
                switch (param)
                {
                    case 1:
                        XmlConvert.VerifyNMTOKEN(String.Empty);
                        break;
                    case 2:
                        XmlConvert.VerifyName(String.Empty);
                        break;
                    case 3:
                        XmlConvert.VerifyNCName(String.Empty);
                        break;
                    case 5:
                        XmlConvert.VerifyXmlChars(String.Empty);
                        break;
                    case 6:
                        XmlConvert.VerifyPublicId(String.Empty);
                        break;
                    case 7:
                        XmlConvert.VerifyWhitespace(String.Empty);
                        break;
                }
            }
            catch (ArgumentException e)
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
