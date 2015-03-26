// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml;
using OLEDB.Test.ModuleCore;

namespace XmlConvertTests
{
    internal class XmlCombiningCharConvertTests2 : XmlCombiningCharConvertTests
    {
        #region Constructors and Destructors

        public XmlCombiningCharConvertTests2()
        {
            for (int i = 0; i < _byte_CombiningChar.Length; i = i + 2)
            {
                AddVariation(new CVariation(this, "EncodeNmToken-EncodeLocalNmToken : " + _Expbyte_CombiningChar[i / 2], XmlEncodeName2));
            }
        }

        #endregion

        #region Public Methods and Operators

        public int XmlEncodeName2()
        {
            int i = ((CurVariation.id) - 1) * 2;
            string strEnVal = String.Empty;

            strEnVal = XmlConvert.EncodeNmToken((BitConverter.ToChar(_byte_CombiningChar, i)).ToString());
            if (_Expbyte_CombiningChar[i / 2] != "_x0A6F_" && _Expbyte_CombiningChar[i / 2] != "_x0E46_")
            {
                CError.Compare(strEnVal, _Expbyte_CombiningChar[i / 2], "Comparison failed at " + i);
            }
            return TEST_PASS;
        }
        #endregion
    }
}