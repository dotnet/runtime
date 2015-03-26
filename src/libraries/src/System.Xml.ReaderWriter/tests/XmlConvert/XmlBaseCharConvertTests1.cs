// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml;
using OLEDB.Test.ModuleCore;

namespace XmlConvertTests
{
    internal class XmlBaseCharConvertTests1 : XmlBaseCharConvertTests
    {
        #region Constructors and Destructors

        public XmlBaseCharConvertTests1()
        {
            for (int i = 0; i < _byte_BaseChar.Length; i = i + 2)
            {
                AddVariation(new CVariation(this, "EncodeName-EncodeLocalName : " + _Expbyte_BaseChar[i / 2], XmlEncodeName4));
            }
        }

        #endregion

        #region Public Methods and Operators

        public int XmlEncodeName4()
        {
            int i = ((CurVariation.id) - 1) * 2;
            string strEnVal = String.Empty;

            strEnVal = XmlConvert.EncodeName((BitConverter.ToChar(_byte_BaseChar, i)).ToString());
            CError.Compare(strEnVal, _Expbyte_BaseChar[i / 2], "Comparison failed at " + i);
            return TEST_PASS;
        }
        #endregion
    }
}