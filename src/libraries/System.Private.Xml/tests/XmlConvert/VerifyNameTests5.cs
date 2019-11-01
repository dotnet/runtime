// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    internal class VerifyNameTests5 : CTestCase
    {
        public override void AddChildren()
        {
            AddChild(new CVariation(v6) { Attribute = new Variation("VerifyPublicId.two underscores") { Params = new object[] { 6, false } } });
            AddChild(new CVariation(v6) { Attribute = new Variation("VerifyXmlChars.two underscores") { Params = new object[] { 5, false } } });
            AddChild(new CVariation(v6) { Attribute = new Variation("VerifyNMTOKEN.two underscores") { Params = new object[] { 1, false } } });
            AddChild(new CVariation(v6) { Attribute = new Variation("VerifyWhitespace.two underscores") { Params = new object[] { 7, true } } });
            AddChild(new CVariation(v6) { Attribute = new Variation("VerifyName.two underscores") { Params = new object[] { 2, false } } });
            AddChild(new CVariation(v6) { Attribute = new Variation("VerifyNCName.two underscores") { Params = new object[] { 3, false } } });
            AddChild(new CVariation(v7) { Attribute = new Variation("VerifyPublicId.multiple non letter characters with .") { Params = new object[] { 6, false } } });
            AddChild(new CVariation(v7) { Attribute = new Variation("VerifyWhitespace.multiple non letter characters with .") { Params = new object[] { 7, true } } });
            AddChild(new CVariation(v7) { Attribute = new Variation("VerifyName.multiple non letter characters with .") { Params = new object[] { 2, false } } });
            AddChild(new CVariation(v7) { Attribute = new Variation("VerifyNCName.multiple non letter characters with .") { Params = new object[] { 3, false } } });
            AddChild(new CVariation(v7) { Attribute = new Variation("VerifyXmlChars.multiple non letter characters with .") { Params = new object[] { 5, false } } });
            AddChild(new CVariation(v7) { Attribute = new Variation("VerifyNMTOKEN.multiple non letter characters with .") { Params = new object[] { 1, false } } });
            AddChild(new CVariation(v8) { Attribute = new Variation("VerifyPublicId.valid surrogate pair") { Params = new object[] { 6, true } } });
            AddChild(new CVariation(v8) { Attribute = new Variation("VerifyNMTOKEN.valid surrogate pair") { Params = new object[] { 1, true } } });
            AddChild(new CVariation(v8) { Attribute = new Variation("VerifyWhitespace.valid surrogate pair") { Params = new object[] { 7, true } } });
            AddChild(new CVariation(v8) { Attribute = new Variation("VerifyName.valid surrogate pair") { Params = new object[] { 2, true } } });
            AddChild(new CVariation(v8) { Attribute = new Variation("VerifyNCName.valid surrogate pair") { Params = new object[] { 3, true } } });
            AddChild(new CVariation(v8) { Attribute = new Variation("VerifyXmlChars.valid surrogate pair") { Params = new object[] { 5, false } } });
            AddChild(new CVariation(v9) { Attribute = new Variation("VerifyPublicId.invalid surrogate pair") { Param = 6 } });
            AddChild(new CVariation(v9) { Attribute = new Variation("VerifyWhitespace.invalid surrogate pair") { Param = 7 } });
            AddChild(new CVariation(v9) { Attribute = new Variation("VerifyName.invalid surrogate pair") { Param = 2 } });
            AddChild(new CVariation(v9) { Attribute = new Variation("VerifyNCName.invalid surrogate pair") { Param = 3 } });
            AddChild(new CVariation(v9) { Attribute = new Variation("VerifyXmlChars.invalid surrogate pair") { Param = 5 } });
            AddChild(new CVariation(v9) { Attribute = new Variation("VerifyNMTOKEN.invalid surrogate pair") { Param = 1 } });
        }

        /// <summary>
        /// Params[] = { VariationNumber, shouldThrow }
        /// </summary>
        /// <returns></returns>
        private int v6()
        {
            var param = (int)CurVariation.Params[0];
            var shouldThrow = (bool)CurVariation.Params[1];
            string actName = "__";
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
        private int v7()
        {
            var param = (int)CurVariation.Params[0];
            var shouldThrow = (bool)CurVariation.Params[1];
            string actName = "_-_.-";
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
        /// <returns></returns>
        private int v8()
        {
            var param = (int)CurVariation.Params[0];
            var shouldThrow = (bool)CurVariation.Params[1];
            string actName = "\uD812\uDD12";
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


        private int v9()
        {
            var param = (int)CurVariation.Param;
            string actName = "\uDE34\uD9A2";
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
    }
}
