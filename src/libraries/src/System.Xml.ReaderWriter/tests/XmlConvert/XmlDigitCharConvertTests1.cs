// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml;
using OLEDB.Test.ModuleCore;

namespace XmlConvertTests
{
    internal class XmlDigitCharConvertTests1 : XmlDigitCharConvertTests
    {
        #region Constructors and Destructors

        public XmlDigitCharConvertTests1()
        {
            for (int i = 0; i < _byte_Digit.Length; i = i + 2)
            {
                AddVariation(new CVariation(this, "EncodeName-EncodeLocalName : " + _Expbyte_Digit[i / 2], XmlEncodeName1));
            }
        }

        #endregion

        #region Public Methods and Operators

        public int XmlEncodeName1()
        {
            int i = ((CurVariation.id) - 1) * 2;
            string strEnVal = String.Empty;

            strEnVal = XmlConvert.EncodeName((BitConverter.ToChar(_byte_Digit, i)).ToString());
            CError.Compare(strEnVal, _Expbyte_Digit[i / 2], "Comparison failed at " + i);
            return TEST_PASS;
        }
        #endregion
    }
}