// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    internal abstract class XmlIdeographicCharConvertTests : CTestCase
    {
        #region Static Fields

        public static string[] _Expbyte_Ideographic = { "_x3006_", "_x3008_", "_x3020_", "_x302A_", "_x4DFF_", "_x9FA6_" };

        public static byte[] _byte_Ideographic = { 0x06, 0x30, 0x08, 0x30, 0x20, 0x30, 0x2A, 0x30, 0xFF, 0x4D, 0xA6, 0x9F };

        #endregion

        #region Public Methods and Operators

        public override int Init(object objParam)
        {
            return TEST_PASS;
        }
        #endregion
    }
}
