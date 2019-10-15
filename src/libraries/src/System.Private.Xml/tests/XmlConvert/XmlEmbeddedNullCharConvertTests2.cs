// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    internal class XmlEmbeddedNullCharConvertTests2 : XmlEmbeddedNullCharConvertTests
    {
        #region Constructors and Destructors

        public XmlEmbeddedNullCharConvertTests2()
        {
            for (int i = 0; i < _byte_EmbeddedNull.Length; i = i + 2)
            {
                AddVariation(new CVariation(this, "EncodeNmToken-EncodeLocalNmToken : " + _Expbyte_EmbeddedNull[i / 2], XmlEncodeName2));
            }
        }

        #endregion

        #region Public Methods and Operators

        public int XmlEncodeName2()
        {
            int i = ((CurVariation.id) - 1) * 2;
            string strEnVal = string.Empty;

            strEnVal = XmlConvert.EncodeNmToken((BitConverter.ToChar(_byte_EmbeddedNull, i)).ToString());
            CError.Compare(strEnVal, _Expbyte_EmbeddedNull[i / 2], "Comparison failed at " + i);
            return TEST_PASS;
        }
        #endregion
    }
}
