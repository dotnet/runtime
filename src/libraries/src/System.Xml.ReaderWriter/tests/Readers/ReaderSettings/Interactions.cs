// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    [TestCase(Name = "LineInfo", Desc = "LineInfo")]
    public partial class TCLineInfo : TCXMLReaderBaseGeneral
    {
        [Variation("Line Number Offset negative values", Pri = 1)]
        public int ln02()
        {
            XmlReaderSettings rs = new XmlReaderSettings();
            rs.LineNumberOffset = -1;
            return TEST_PASS;
        }

        [Variation("Line Position Offset negative values", Pri = 1)]
        public int lp02()
        {
            XmlReaderSettings rs = new XmlReaderSettings();
            rs.LinePositionOffset = -1;
            return TEST_PASS;
        }
    }
}
