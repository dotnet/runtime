// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    internal class SqlXmlConvertTests1 : SqlXmlConvertTests
    {
        #region Constructors and Destructors

        public SqlXmlConvertTests1()
        {
            foreach (string token in strEncode)
            {
                AddVariation(new CVariation(this, "EncodeName-EncodeLocalName with " + token, XmlEncodeName1));
            }
        }

        #endregion

        #region Public Methods and Operators

        public int XmlEncodeName1()
        {
            int i = (CurVariation.id) - 1;
            string strEnVal = String.Empty;

            CError.WriteLine(strEncode[i]);
            strEnVal = XmlConvert.EncodeName(strEncode[i]);
            CError.Compare(strEnVal, strExpEncode[i], "Comparison failed at " + i);

            strEnVal = XmlConvert.EncodeLocalName(strEncode[i]);
            CError.Compare(strEnVal, strExpEncode[i], "Comparison failed at " + i);

            return TEST_PASS;
        }
        #endregion
    }
}
