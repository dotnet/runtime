// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml;
using OLEDB.Test.ModuleCore;

namespace XmlConvertTests
{
    internal class SqlXmlConvertTests2 : SqlXmlConvertTests
    {
        #region Constructors and Destructors

        public SqlXmlConvertTests2()
        {
            foreach (string token in strEncode)
            {
                AddVariation(new CVariation(this, "EncodeName-DecodeName with " + token, XmlEncodeName2));
            }
        }

        #endregion

        #region Public Methods and Operators

        public int XmlEncodeName2()
        {
            int i = (CurVariation.id) - 1;
            string strDeVal = String.Empty;
            string strEnVal = String.Empty;

            CError.WriteLine(strEncode[i]);
            strEnVal = XmlConvert.EncodeName(strEncode[i]);
            CError.Compare(strEnVal, strExpEncode[i], "Encode Comparison failed at " + i);

            strDeVal = XmlConvert.DecodeName(strEnVal);
            CError.Compare(strDeVal, strEncode[i], "Decode Comparison failed at " + i);
            return TEST_PASS;
        }
        #endregion
    }
}