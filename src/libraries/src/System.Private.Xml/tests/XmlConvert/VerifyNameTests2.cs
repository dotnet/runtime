// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    internal class VerifyNameTests2 : CTestCase
    {
        public override void AddChildren()
        {
            AddChild(new CVariation(v10) { Attribute = new Variation("VerifyNMTOKEN.high surrogate") { Param = 1 } });
            AddChild(new CVariation(v10) { Attribute = new Variation("VerifyXmlChars.high surrogate") { Param = 5 } });
            AddChild(new CVariation(v10) { Attribute = new Variation("VerifyPublicId.high surrogate") { Param = 6 } });
            AddChild(new CVariation(v10) { Attribute = new Variation("VerifyWhitespace.high surrogate") { Param = 7 } });
            AddChild(new CVariation(v10) { Attribute = new Variation("VerifyName.high surrogate") { Param = 2 } });
            AddChild(new CVariation(v10) { Attribute = new Variation("VerifyNCName.high surrogate") { Param = 3 } });
            AddChild(new CVariation(v11) { Attribute = new Variation("VerifyPublicId.low surrogate") { Param = 6 } });
            AddChild(new CVariation(v11) { Attribute = new Variation("VerifyWhitespace.low surrogate") { Param = 7 } });
            AddChild(new CVariation(v11) { Attribute = new Variation("VerifyName.low surrogate") { Param = 2 } });
            AddChild(new CVariation(v11) { Attribute = new Variation("VerifyNCName.low surrogate") { Param = 3 } });
            AddChild(new CVariation(v11) { Attribute = new Variation("VerifyXmlChars.low surrogate") { Param = 5 } });
            AddChild(new CVariation(v11) { Attribute = new Variation("VerifyNMTOKEN.low surrogate") { Param = 1 } });
            AddChild(new CVariation(v12) { Attribute = new Variation("VerifyWhitespace.special symbols") { Params = new object[] { 7, true } } });
            AddChild(new CVariation(v12) { Attribute = new Variation("VerifyPublicId.special symbols") { Params = new object[] { 6, true } } });
            AddChild(new CVariation(v12) { Attribute = new Variation("VerifyNMTOKEN.special symbols") { Params = new object[] { 1, true } } });
            AddChild(new CVariation(v12) { Attribute = new Variation("VerifyName.special symbols") { Params = new object[] { 2, true } } });
            AddChild(new CVariation(v12) { Attribute = new Variation("VerifyNCName.special symbols") { Params = new object[] { 3, true } } });
            AddChild(new CVariation(v12) { Attribute = new Variation("VerifyXmlChars.special symbols") { Params = new object[] { 5, false } } });
            AddChild(new CVariation(v13) { Attribute = new Variation("Test for VerifyNMTOKEN(abcd\ralfafkjha)") { Params = new object[] { "abcd\ralfafkjha", "invalid" } } });
            AddChild(new CVariation(v13) { Attribute = new Variation("Test for VerifyNMTOKEN(abcd)") { Params = new object[] { "abcd", "valid" } } });
            AddChild(new CVariation(v13) { Attribute = new Variation("Test for VerifyNMTOKEN(abcd  def)") { Params = new object[] { "abcd  def", "invalid" } } });
            AddChild(new CVariation(v13) { Attribute = new Variation("Test for VerifyNMTOKEN(abcd\tdef)") { Params = new object[] { "abcd\tdef", "invalid" } } });
            AddChild(new CVariation(v13) { Attribute = new Variation("Test for VerifyNMTOKEN( \b)") { Params = new object[] { " \b", "invalid" } } });
            AddChild(new CVariation(v13) { Attribute = new Variation("Test for VerifyNMTOKEN(abcd efgh)") { Params = new object[] { "abcd efgh", "invalid" } } });
            AddChild(new CVariation(v13) { Attribute = new Variation("Test for VerifyNMTOKEN()") { Params = new object[] { "", "invalid" } } });
            AddChild(new CVariation(v13) { Attribute = new Variation("Test for VerifyNMTOKEN( abcd)") { Params = new object[] { " abcd", "invalid" } } });
            AddChild(new CVariation(v13) { Attribute = new Variation("Test for VerifyNMTOKEN(abcd\nalfafkjha)") { Params = new object[] { "abcd ", "invalid" } } });
            AddChild(new CVariation(v13) { Attribute = new Variation("Test for VerifyNMTOKEN(abcd\nalfafkjha)") { Params = new object[] { "abcd\nalfafkjha", "invalid" } } });
            AddChild(new CVariation(v15) { Attribute = new Variation("23.Test for VerifyName(:a\ud801\udc01b)") { Params = new object[] { 23, "invalid" } } });
            AddChild(new CVariation(v15) { Attribute = new Variation("5.Test for VerifyName(abcd\nalfafkjha)") { Params = new object[] { 5, "invalid" } } });
            AddChild(new CVariation(v15) { Attribute = new Variation("8.Test for VerifyName(abcd\tdef)") { Params = new object[] { 8, "invalid" } } });
            AddChild(new CVariation(v15) { Attribute = new Variation("9.Test for VerifyName( \b)") { Params = new object[] { 9, "invalid" } } });
            AddChild(new CVariation(v15) { Attribute = new Variation("10.Test for VerifyName(\ud801\udc01)") { Params = new object[] { 10, "invalid" } } });
            AddChild(new CVariation(v15) { Attribute = new Variation("11.Test for VerifyName( \ud801\udc01)") { Params = new object[] { 11, "invalid" } } });
            AddChild(new CVariation(v15) { Attribute = new Variation("12.Test for VerifyName(\ud801\udc01 )") { Params = new object[] { 12, "invalid" } } });
            AddChild(new CVariation(v15) { Attribute = new Variation("13.Test for VerifyName(\ud801 \udc01)") { Params = new object[] { 13, "invalid" } } });
            AddChild(new CVariation(v15) { Attribute = new Variation("14.Test for VerifyName(\ud801  \udc01)") { Params = new object[] { 14, "invalid" } } });
            AddChild(new CVariation(v15) { Attribute = new Variation("15.Test for VerifyName(\ud801\r\udc01)") { Params = new object[] { 15, "invalid" } } });
            AddChild(new CVariation(v15) { Attribute = new Variation("16.Test for VerifyName(\ud801\n\udc01)") { Params = new object[] { 16, "invalid" } } });
            AddChild(new CVariation(v15) { Attribute = new Variation("17.Test for VerifyName(\ud801\t\udc001)") { Params = new object[] { 17, "invalid" } } });
            AddChild(new CVariation(v15) { Attribute = new Variation("18.Test for VerifyName(a\ud801\udc01b)") { Params = new object[] { 18, "invalid" } } });
            AddChild(new CVariation(v15) { Attribute = new Variation("19.Test for VerifyName(a\udc01\ud801b)") { Params = new object[] { 19, "invalid" } } });
            AddChild(new CVariation(v15) { Attribute = new Variation("20.Test for VerifyName(a\ud801b)") { Params = new object[] { 20, "invalid" } } });
            AddChild(new CVariation(v15) { Attribute = new Variation("21.Test for VerifyName(a\udc01b)") { Params = new object[] { 21, "invalid" } } });
            AddChild(new CVariation(v15) { Attribute = new Variation("22.Test for VerifyName(\ud801\udc01:)") { Params = new object[] { 22, "invalid" } } });
            AddChild(new CVariation(v15) { Attribute = new Variation("1.Test for VerifyName(abcd)") { Params = new object[] { 1, "valid" } } });
            AddChild(new CVariation(v15) { Attribute = new Variation("24.Test for VerifyName(a\ud801\udc01:b)") { Params = new object[] { 24, "invalid" } } });
            AddChild(new CVariation(v15) { Attribute = new Variation("25.Test for VerifyName(a\udbff\udc01\b)") { Params = new object[] { 25, "invalid" } } });
            AddChild(new CVariation(v15) { Attribute = new Variation("6.Test for VerifyName(abcd\ralfafkjha)") { Params = new object[] { 6, "invalid" } } });
            AddChild(new CVariation(v15) { Attribute = new Variation("7.Test for VerifyName(abcd  def)") { Params = new object[] { 7, "invalid" } } });
            AddChild(new CVariation(v15) { Attribute = new Variation("2.Test for VerifyName(abcd efgh)") { Params = new object[] { 2, "invalid" } } });
            AddChild(new CVariation(v15) { Attribute = new Variation("3.Test for VerifyName( abcd)") { Params = new object[] { 3, "invalid" } } });
            AddChild(new CVariation(v15) { Attribute = new Variation("4.Test for VerifyName(abcd\nalfafkjha)") { Params = new object[] { 4, "invalid" } } });
        }

        private int v10()
        {
            var param = (int)CurVariation.Param;
            string actName = "\uD812";
            string expName = null;
            try
            {
                switch (param)
                {
                    case 1:
                        expName = XmlConvert.VerifyNMTOKEN(actName);
                        break;
                    case 2:
                        expName = XmlConvert.VerifyName(actName);
                        break;
                    case 3:
                        expName = XmlConvert.VerifyNCName(actName);
                        break;
                    case 5:
                        expName = XmlConvert.VerifyXmlChars(actName);
                        break;
                    case 6:
                        expName = XmlConvert.VerifyPublicId(actName);
                        break;
                    case 7:
                        expName = XmlConvert.VerifyWhitespace(actName);
                        break;
                }
            }
            catch (XmlException e)
            {
                CError.WriteLine(e.LineNumber);
                CError.WriteLine(e.LinePosition);
                return param != 4 ? TEST_PASS : TEST_FAIL; //param4 -> VerifyToken should not throw here
            }
            return TEST_FAIL;
        }

        private int v11()
        {
            var param = (int)CurVariation.Param;
            string actName = "\uDF20";
            string expName = null;
            try
            {
                switch (param)
                {
                    case 1:
                        expName = XmlConvert.VerifyNMTOKEN(actName);
                        break;
                    case 2:
                        expName = XmlConvert.VerifyName(actName);
                        break;
                    case 3:
                        expName = XmlConvert.VerifyNCName(actName);
                        break;
                    case 5:
                        expName = XmlConvert.VerifyXmlChars(actName);
                        break;
                    case 6:
                        expName = XmlConvert.VerifyPublicId(actName);
                        break;
                    case 7:
                        expName = XmlConvert.VerifyWhitespace(actName);
                        break;
                }
            }
            catch (XmlException e)
            {
                CError.WriteLine(e.LineNumber);
                CError.WriteLine(e.LinePosition);
                return param != 4 ? TEST_PASS : TEST_FAIL; //param4 -> VerifyToken should not throw here
            }
            return TEST_FAIL;
        }


        /// <summary>
        /// Params[] = { VariationNumber, shouldThrow }
        /// </summary>
        /// <returns></returns>public int v12()
        private int v12()
        {
            var param = (int)CurVariation.Params[0];
            var shouldThrow = (bool)CurVariation.Params[1];
            string actName = "<#$%^&*@<>";
            string expName = null;
            try
            {
                switch (param)
                {
                    case 1:
                        expName = XmlConvert.VerifyNMTOKEN(actName);
                        break;
                    case 2:
                        expName = XmlConvert.VerifyName(actName);
                        break;
                    case 3:
                        expName = XmlConvert.VerifyNCName(actName);
                        break;
                    case 5:
                        expName = XmlConvert.VerifyXmlChars(actName);
                        break;
                    case 6:
                        expName = XmlConvert.VerifyPublicId(actName);
                        break;
                    case 7:
                        expName = XmlConvert.VerifyWhitespace(actName);
                        break;
                }
            }
            catch (XmlException e)
            {
                CError.WriteLine(e.LineNumber);
                CError.WriteLine(e.LinePosition);
                return shouldThrow ? TEST_PASS : TEST_FAIL;
            }
            CError.Compare(expName, actName, "Name");
            return TEST_PASS;
        }

        private int v13()
        {
            string input = CurVariation.Params[0].ToString();
            string expected = CurVariation.Params[1].ToString();
            try
            {
                XmlConvert.VerifyNMTOKEN(input);
            }
            catch (XmlException e)
            {
                CError.WriteLine(e.LineNumber);
                CError.WriteLine(e.LinePosition);
                return (expected.Equals("invalid")) ? TEST_PASS : TEST_FAIL;
            }
            return (expected.Equals("valid")) ? TEST_PASS : TEST_FAIL;
        }

        private int v15()
        {
            var param = (int)CurVariation.Params[0];
            string input = string.Empty;
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

            string expected = CurVariation.Params[1].ToString();
            try
            {
                XmlConvert.VerifyName(input);
            }
            catch (XmlException e)
            {
                CError.WriteLine(e.LineNumber);
                CError.WriteLine(e.LinePosition);
                return (expected.Equals("invalid")) ? TEST_PASS : TEST_FAIL;
            }
            return (expected.Equals("valid")) ? TEST_PASS : TEST_FAIL;
        }
    }
}
