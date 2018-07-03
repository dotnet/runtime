// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    internal class XmlBaseCharConvertTests2 : XmlBaseCharConvertTests
    {
        #region Constructors and Destructors

        public XmlBaseCharConvertTests2()
        {
            for (int i = 0; i < _byte_BaseChar.Length; i = i + 2)
            {
                AddVariation(new CVariation(this, "EncodeNmToken-EncodeLocalNmToken : " + _Expbyte_BaseChar[i / 2], XmlEncodeName5));
            }
        }

        #endregion

        #region Public Methods and Operators

        public int XmlEncodeName5()
        {
            int i = ((CurVariation.id) - 1) * 2;
            string strEnVal = string.Empty;

            strEnVal = XmlConvert.EncodeNmToken((BitConverter.ToChar(_byte_BaseChar, i)).ToString());

            if (_Expbyte_BaseChar[i / 2] != "_x0387_" && _Expbyte_BaseChar[i / 2] != "_x0640_" && _Expbyte_BaseChar[i / 2] != "_x064B_" && _Expbyte_BaseChar[i / 2] != "_x0670_" && _Expbyte_BaseChar[i / 2] != "_x06D6_" && _Expbyte_BaseChar[i / 2] != "_x06E4_" && _Expbyte_BaseChar[i / 2] != "_x06E7_" && _Expbyte_BaseChar[i / 2] != "_x093C_" && _Expbyte_BaseChar[i / 2] != "_x093E_" && _Expbyte_BaseChar[i / 2] != "_x0962_" && _Expbyte_BaseChar[i / 2] != "_x09E2_" && _Expbyte_BaseChar[i / 2] != "_x09EF_" && _Expbyte_BaseChar[i / 2] != "_x0A71_" && _Expbyte_BaseChar[i / 2] != "_x0ABC_" && _Expbyte_BaseChar[i / 2] != "_x0ABE_" && _Expbyte_BaseChar[i / 2] != "_x0B3C_" && _Expbyte_BaseChar[i / 2] != "_x0B3E_" && _Expbyte_BaseChar[i / 2] != "_x0E31_" && _Expbyte_BaseChar[i / 2] != "_x0E34_" && _Expbyte_BaseChar[i / 2] != "_x0E46_" && _Expbyte_BaseChar[i / 2] != "_x0EB1_" && _Expbyte_BaseChar[i / 2] != "_x0EB4_" && _Expbyte_BaseChar[i / 2] != "_x0EBC_" && _Expbyte_BaseChar[i / 2] != "_x0F3F_")
            {
                CError.Compare(strEnVal, _Expbyte_BaseChar[i / 2], "Comparison failed at " + i);
            }
            return TEST_PASS;
        }
        #endregion
    }
}
