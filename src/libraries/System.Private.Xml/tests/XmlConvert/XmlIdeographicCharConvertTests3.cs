// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    internal class XmlIdeographicCharConvertTests3 : XmlIdeographicCharConvertTests
    {
        #region Constructors and Destructors

        public XmlIdeographicCharConvertTests3()
        {
            for (int i = 0; i < _byte_Ideographic.Length; i = i + 2)
            {
                AddVariation(new CVariation(this, "EncodeName-DecodeName : " + _Expbyte_Ideographic[i / 2], XmlEncodeName));
            }
        }

        #endregion

        #region Public Methods and Operators

        public int XmlEncodeName()
        {
            int i = ((CurVariation.id) - 1) * 2;
            string strDeVal = string.Empty;
            string strEnVal = string.Empty;
            string strVal = string.Empty;

            strVal = (BitConverter.ToChar(_byte_Ideographic, i)).ToString();
            strEnVal = XmlConvert.EncodeName(strVal);
            CError.Compare(strEnVal, _Expbyte_Ideographic[i / 2], "Encode Comparison failed at " + i);

            strDeVal = XmlConvert.DecodeName(strEnVal);
            CError.Compare(strDeVal, strVal, "Decode Comparison failed at " + i);
            return TEST_PASS;
        }
        #endregion
    }
}
