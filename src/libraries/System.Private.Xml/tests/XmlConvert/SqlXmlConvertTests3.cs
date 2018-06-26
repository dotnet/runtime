// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    internal class SqlXmlConvertTests3 : SqlXmlConvertTests
    {
        #region Constructors and Destructors

        public SqlXmlConvertTests3()
        {
            foreach (string token in strEncodeLocal)
            {
                AddVariation(new CVariation(this, "EncodeLocalName only with " + token, XmlEncodeName3));
            }
        }

        #endregion

        #region Public Methods and Operators

        public int XmlEncodeName3()
        {
            int i = (CurVariation.id) - 1;
            string strEnVal = string.Empty;

            CError.WriteLine(strEncodeLocal[i]);
            strEnVal = XmlConvert.EncodeLocalName(strEncodeLocal[i]);
            if (strExpEncodeLocal[i] == "6_x003A_7")
            {
                CError.Compare(strEnVal, "_x0036__x003A_7", "Comparison failed at " + i);
            }
            else
            {
                CError.Compare(strEnVal, strExpEncodeLocal[i], "Comparison failed at " + i);
            }
            return TEST_PASS;
        }
        #endregion
    }
}
