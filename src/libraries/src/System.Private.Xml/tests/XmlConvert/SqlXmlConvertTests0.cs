// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    internal class SqlXmlConvertTests0 : SqlXmlConvertTests
    {
        #region Constructors and Destructors

        public SqlXmlConvertTests0()
        {
            foreach (string token in strEncode)
            {
                AddVariation(new CVariation(this, "EncodeNmToken-EncodeLocalNmToken with " + token, XmlEncodeName2));
            }
        }

        #endregion

        #region Public Methods and Operators

        public int XmlEncodeName2()
        {
            int i = (CurVariation.id) - 1;
            string strEnVal = string.Empty;

            CError.WriteLine(strEncode[i]);
            strEnVal = XmlConvert.EncodeNmToken(strEncode[i]);
            CError.Compare(strEnVal, strExpEncodeNmToken[i], "Comparison failed at " + i);
            return TEST_PASS;
        }
        #endregion
    }
}
