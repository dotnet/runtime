// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    internal class VerifyNameTests1 : CTestCase
    {
        public override void AddChildren()
        {
            AddChild(new CVariation(v1) { Attribute = new Variation("VerifyWhitespace.First character in name 4e") { Param = 7 } });
            AddChild(new CVariation(v1) { Attribute = new Variation("VerifyName.First character in name 4e") { Param = 2 } });
            AddChild(new CVariation(v1) { Attribute = new Variation("VerifyNCName.First character in name 4e") { Param = 3 } });
            AddChild(new CVariation(v1) { Attribute = new Variation("VerifyNMTOKEN.First character in name 4e") { Param = 1 } });
            AddChild(new CVariation(v1) { Attribute = new Variation("VerifyXmlChars.First character in name 4e") { Param = 5 } });
            AddChild(new CVariation(v1) { Attribute = new Variation("VerifyPublicId.First character in name 4e") { Param = 6 } });
            AddChild(new CVariation(v2) { Attribute = new Variation("VerifyXmlChars.Second+ character in name") { Param = 5 } });
            AddChild(new CVariation(v2) { Attribute = new Variation("VerifyName.Second+ character in name") { Param = 2 } });
            AddChild(new CVariation(v2) { Attribute = new Variation("VerifyNCName.Second+ character in name") { Param = 3 } });
            AddChild(new CVariation(v2) { Attribute = new Variation("VerifyNMTOKEN.Second+ character in name") { Param = 1 } });
            AddChild(new CVariation(v2) { Attribute = new Variation("VerifyPublicId.Second+ character in name") { Param = 6 } });
            AddChild(new CVariation(v2) { Attribute = new Variation("VerifyWhitespace.Second+ character in name") { Param = 7 } });
            AddChild(new CVariation(v3) { Attribute = new Variation("VerifyPublicId.Last character in name 4e") { Param = 6 } });
            AddChild(new CVariation(v3) { Attribute = new Variation("VerifyWhitespace.Last character in name 4e") { Param = 7 } });
            AddChild(new CVariation(v3) { Attribute = new Variation("VerifyName.Last character in name 4e") { Param = 2 } });
            AddChild(new CVariation(v3) { Attribute = new Variation("VerifyNCName.Last character in name 4e") { Param = 3 } });
            AddChild(new CVariation(v3) { Attribute = new Variation("VerifyXmlChars.Last character in name 4e") { Param = 5 } });
            AddChild(new CVariation(v3) { Attribute = new Variation("VerifyNMTOKEN.Last character in name 4e") { Param = 1 } });
            AddChild(new CVariation(v4) { Attribute = new Variation("VerifyPublicId.Name with two colons") { Params = new object[] { 6, false } } });
            AddChild(new CVariation(v4) { Attribute = new Variation("VerifyNMTOKEN.Name with two colons") { Params = new object[] { 1, false } } });
            AddChild(new CVariation(v4) { Attribute = new Variation("VerifyWhitespace.Name with two colons") { Params = new object[] { 7, true } } });
            AddChild(new CVariation(v4) { Attribute = new Variation("VerifyName.Name with two colons") { Params = new object[] { 2, false } } });
            AddChild(new CVariation(v4) { Attribute = new Variation("VerifyNCName.Name with two colons") { Params = new object[] { 3, true } } });
            AddChild(new CVariation(v4) { Attribute = new Variation("VerifyXmlChars.Name with two colons") { Params = new object[] { 5, false } } });
            AddChild(new CVariation(v5) { Attribute = new Variation("VerifyPublicId.multiple non letter characters with :") { Params = new object[] { 6, false } } });
            AddChild(new CVariation(v5) { Attribute = new Variation("VerifyWhitespace.multiple non letter characters with :") { Params = new object[] { 7, true } } });
            AddChild(new CVariation(v5) { Attribute = new Variation("VerifyName.multiple non letter characters with :") { Params = new object[] { 2, false } } });
            AddChild(new CVariation(v5) { Attribute = new Variation("VerifyNCName.multiple non letter characters with :") { Params = new object[] { 3, true } } });
            AddChild(new CVariation(v5) { Attribute = new Variation("VerifyXmlChars.multiple non letter characters with :") { Params = new object[] { 5, false } } });
            AddChild(new CVariation(v5) { Attribute = new Variation("VerifyNMTOKEN.multiple non letter characters with :") { Params = new object[] { 1, false } } });
        }

        private int v1()
        {
            var param = (int)CurVariation.Param;
            for (int i = 0; i <= 0xFFFF; i++)
            {
                char c = Convert.ToChar(i);

                string actName = c.ToString();
                string expName = null;
                bool bException = false;

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
                catch (XmlException)
                {
                    bException = true;
                }

                bool bIsValid = false;
                switch (param)
                {
                    case 1:
                        bIsValid = UnicodeCharHelper.IsW3C_NameChar(c);
                        break;
                    case 2:
                        bIsValid = UnicodeCharHelper.IsW3C_StartNameChar(c);
                        break;
                    case 3:
                        bIsValid = UnicodeCharHelper.IsW3C_StartNCNameChar(c);
                        break;
                    case 4:
                        bIsValid = !(UnicodeCharHelper.IsW3C_WhitespaceChar(c));
                        break;
                    case 5:
                        bIsValid = UnicodeCharHelper.IsW3C_XmlChar(c);
                        break;
                    case 6:
                        bIsValid = UnicodeCharHelper.IsW3C_PubidChar(c);
                        break;
                    case 7:
                        bIsValid = UnicodeCharHelper.IsW3C_WhitespaceChar(c);
                        break;
                }
                if (bIsValid)
                {
                    CError.Compare(expName, actName, "Name");
                }
                else
                {
                    CError.Compare(bException, string.Format("Exception expected for char: 0x{0:x}", (int)c));
                }
            }
            return TEST_PASS;
        }

        private int v2()
        {
            var param = (int)CurVariation.Param;
            for (int i = 0; i <= 0xFFFF; i++)
            {
                char c = Convert.ToChar(i);

                string actName = String.Format("a{0}b", c);
                string expName = null;
                bool bException = false;

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
                catch (XmlException)
                {
                    bException = true;
                }

                bool bIsValid = false;
                switch (param)
                {
                    case 1:
                        bIsValid = UnicodeCharHelper.IsW3C_NameChar(c);
                        break;
                    case 2:
                        bIsValid = UnicodeCharHelper.IsW3C_NameChar(c);
                        break;
                    case 3:
                        bIsValid = UnicodeCharHelper.IsW3C_NCNameChar(c);
                        break;
                    case 4:
                        bIsValid = c == ' ' || !(UnicodeCharHelper.IsW3C_WhitespaceChar(c));
                        break;

                    case 5:
                        bIsValid = UnicodeCharHelper.IsW3C_XmlChar(c);
                        break;
                    case 6:
                        bIsValid = UnicodeCharHelper.IsW3C_PubidChar(c);
                        break;
                    case 7:
                        bIsValid = false;
                        break; //always false - other characters are not whitespace
                }
                if (bIsValid)
                {
                    CError.Compare(expName, actName, "Name");
                }
                else
                {
                    CError.Compare(bException, string.Format("Exception expected for char: 0x{0:x}", (int)c));
                }
            }
            return TEST_PASS;
        }

        private int v3()
        {
            var param = (int)CurVariation.Param;
            for (int i = 0; i <= 0xFFFF; i++)
            {
                char c = Convert.ToChar(i);

                string actName = String.Format("abc{0}", c);
                string expName = null;
                bool bException = false;

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
                catch (XmlException)
                {
                    bException = true;
                }

                bool bIsValid = false;
                switch (param)
                {
                    case 1:
                        bIsValid = UnicodeCharHelper.IsW3C_NameChar(c);
                        break;
                    case 2:
                        bIsValid = UnicodeCharHelper.IsW3C_NameChar(c);
                        break;
                    case 3:
                        bIsValid = UnicodeCharHelper.IsW3C_NCNameChar(c);
                        break;
                    case 4:
                        bIsValid = !(UnicodeCharHelper.IsW3C_WhitespaceChar(c));
                        break;

                    case 5:
                        bIsValid = UnicodeCharHelper.IsW3C_XmlChar(c);
                        break;
                    case 6:
                        bIsValid = UnicodeCharHelper.IsW3C_PubidChar(c);
                        break;
                    case 7:
                        bIsValid = false;
                        break; //always false - other characters are not whitespace      
                }
                if (bIsValid)
                {
                    CError.Compare(expName, actName, "Name");
                }
                else
                {
                    CError.Compare(bException, string.Format("Exception expected for char: 0x{0:x}", (int)c));
                }
            }
            return TEST_PASS;
        }

        /// <summary>
        /// Params[] = { VariationNumber, shouldThrow }
        /// </summary>
        private int v4()
        {
            var param = (int)CurVariation.Params[0];
            var shouldThrow = (bool)CurVariation.Params[1];
            string actName = "::";
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
                    default:
                        shouldThrow = false;
                        throw new Exception("Invalid variation. (param = " + param);
                }
                CError.Compare(expName, actName, "Name");
                return shouldThrow ? TEST_FAIL : TEST_PASS;
            }
            catch (XmlException e)
            {
                CError.WriteLine(e.LineNumber);
                CError.WriteLine(e.LinePosition);
                return shouldThrow ? TEST_PASS : TEST_FAIL;
            }
        }

        /// <summary>
        /// Params[] = { VariationNumber, shouldThrow }
        /// </summary>
        private int v5()
        {
            var param = (int)CurVariation.Params[0];
            var shouldThrow = (bool)CurVariation.Params[1];
            string actName = "_:-.:_-.";
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
                    default:
                        shouldThrow = false;
                        throw new Exception("Invalid variation. (param = " + param);
                }
                CError.Compare(expName, actName, "Name");
                return shouldThrow ? TEST_FAIL : TEST_PASS;
            }
            catch (XmlException e)
            {
                CError.WriteLine(e.LineNumber);
                CError.WriteLine(e.LinePosition);
                return shouldThrow ? TEST_PASS : TEST_FAIL;
            }
        }
    }
}
