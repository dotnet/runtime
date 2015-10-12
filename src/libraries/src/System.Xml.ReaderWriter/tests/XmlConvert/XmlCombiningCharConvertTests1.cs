// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    internal class XmlCombiningCharConvertTests1 : XmlCombiningCharConvertTests
    {
        #region Constructors and Destructors

        public XmlCombiningCharConvertTests1()
        {
            for (int i = 0; i < _byte_CombiningChar.Length; i = i + 2)
            {
                AddVariation(new CVariation(this, "EncodeName-EncodeLocalName : " + _Expbyte_CombiningChar[i / 2], XmlEncodeName1));
            }
        }

        #endregion

        #region Public Methods and Operators

        public int XmlEncodeName1()
        {
            int i = ((CurVariation.id) - 1) * 2;
            string strEnVal = String.Empty;

            strEnVal = XmlConvert.EncodeName((BitConverter.ToChar(_byte_CombiningChar, i)).ToString());
            CError.Compare(strEnVal, _Expbyte_CombiningChar[i / 2], "Comparison failed at " + i);
            return TEST_PASS;
        }
        #endregion
    }
}
