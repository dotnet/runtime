// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    internal class EncodeDecodeTests : SqlXmlConvertTests
    {
        #region Public Methods and Operators

        public override void AddChildren()
        {
            AddChild(new CVariation(v1) { Attribute = new Variation("EncodeName with null and String.Empty") });
            AddChild(new CVariation(v2) { Attribute = new Variation("EncodeName with surrogates") });
            AddChild(new CVariation(v3) { Attribute = new Variation("EncodeName in uppercase") });
            AddChild(new CVariation(v4) { Attribute = new Variation("DecodeName with uppercase/lowercase") });
        }

        //[Variation("EncodeName with null and String.Empty")]
        public int v1()
        {
            CError.Compare(XmlConvert.EncodeName(null), null, " en null");
            CError.Compare(XmlConvert.EncodeName(string.Empty), string.Empty, "en empty");
            CError.Compare(XmlConvert.EncodeLocalName(null), null, "eln null");
            CError.Compare(XmlConvert.EncodeLocalName(string.Empty), string.Empty, "eln empty");
            CError.Compare(XmlConvert.DecodeName(null), null, " de null");
            CError.Compare(XmlConvert.DecodeName(string.Empty), string.Empty, "de empty");
            CError.Compare(XmlConvert.EncodeNmToken(null), null, " ent null");
            CError.Compare(XmlConvert.EncodeNmToken(string.Empty), string.Empty, "ent empty");
            return TEST_PASS;
        }

        //[Variation("EncodeName with surrogates")]
        public int v2()
        {
            string sVal = "\uD800\uDC00\uDBFF\uDFFF\uD800\uDC00";
            CError.Compare(XmlConvert.EncodeName(sVal), "_x00010000__x0010FFFF__x00010000_", "ent empty");

            return TEST_PASS;
        }

        //[Variation("EncodeName in uppercase")]
        public int v3()
        {
            byte[] _dbyte = { 0x71, 0xFF };
            string strUni = string.Empty;
            for (int i = 0; i < _dbyte.Length; i = i + 2)
            {
                strUni += (BitConverter.ToChar(_dbyte, i)).ToString();
            }
            CError.WriteLine(strUni + " " + XmlConvert.EncodeName(strUni));
            CError.Compare(XmlConvert.EncodeName(strUni), "_xFF71_", "EncodeName");
            CError.Compare(XmlConvert.EncodeLocalName(strUni), "_xFF71_", "EncodeLocalName");
            CError.Compare(XmlConvert.EncodeNmToken(strUni), "_xFF71_", "EncodeNmToken");

            return TEST_PASS;
        }

        //[Variation("DecodeName with uppercase/lowercase")]
        public int v4()
        {
            CError.Equals(XmlConvert.EncodeName("_xFF71__xff71_"), "_x005F_xFF71__x005F_xff71_", "EncodeName");
            CError.Equals(XmlConvert.EncodeLocalName("_xFF71__xff71_"), "_x005F_xFF71__x005F_xff71_", "EncodeLocalName");
            CError.Equals(XmlConvert.EncodeNmToken("_xFF71__xff71_"), "_x005F_xFF71__x005F_xff71_", "EncodeNmToken");
            CError.Equals(XmlConvert.DecodeName("_xFF71__xff71_"), "\uFF71\uFF71", "DecodeName");
            return TEST_PASS;
        }
        #endregion
    }
}
