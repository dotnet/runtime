// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml;
using OLEDB.Test.ModuleCore;

namespace XmlConvertTests
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
            string strEnVal = String.Empty;

            CError.WriteLine(strEncode[i]);
            strEnVal = XmlConvert.EncodeNmToken(strEncode[i]);
            CError.Compare(strEnVal, strExpEncodeNmToken[i], "Comparison failed at " + i);
            return TEST_PASS;
        }
        #endregion
    }
}