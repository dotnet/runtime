// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    internal abstract class XmlEmbeddedNullCharConvertTests : CTestCase
    {
        #region Static Fields

        public static string[] _Expbyte_EmbeddedNull = { "_x0000_", "_x0001_", "_x0002_", "_x0003_", "_x0004_", "_x0005_", "_x0006_", "_x0007_", "_x0008_", "_x0009_", "_x0010_", "_x0011_", "_x0012_", "_x0013_", "_x0014_", "_x0015_", "_x0016_", "_x0017_", "_x0018_", "_x0019_" };

        public static byte[] _byte_EmbeddedNull = { 0x00, 0x00, 0x01, 0x00, 0x02, 0x00, 0x03, 0x00, 0x04, 0x00, 0x05, 0x00, 0x06, 0x00, 0x07, 0x00, 0x08, 0x00, 0x09, 0x00, 0x10, 0x00, 0x11, 0x00, 0x12, 0x00, 0x13, 0x00, 0x14, 0x00, 0x15, 0x00, 0x16, 0x00, 0x17, 0x00, 0x18, 0x00, 0x19, 0x00 };

        #endregion

        #region Public Methods and Operators

        public override int Init(object objParam)
        {
            return TEST_PASS;
        }
        #endregion
    }
}
