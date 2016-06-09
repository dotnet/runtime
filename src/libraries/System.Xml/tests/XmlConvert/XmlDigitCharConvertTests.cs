// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    internal abstract class XmlDigitCharConvertTests : CTestCase
    {
        #region Static Fields

        public static string[] _Expbyte_Digit = { "_x002F_", "_x065F_", "_x066A_", "_x06EF_", "_x06FA_", "_x0965_", "_x0970_", "_x09E5_", "_x0A65_", "_x0A70_", "_x0AE5_", "_x0AF0_", "_x0B65_", "_x0B70_", "_x0BE6_", "_x0BF0_", "_x0C65_", "_x0C70_", "_x0CE5_", "_x0CF0_", "_x0D65_", "_x0D70_", "_x0E4F_", "_x0E5A_", "_x0ECF_", "_x0F1F_", "_x0F2A_" };

        public static byte[] _byte_Digit = { 0x2F, 0x00, 0x5F, 0x06, 0x6A, 0x06, 0xEF, 0x06, 0xFA, 0x06, 0x65, 0x09, 0x70, 0x09, 0xE5, 0x09, 0x65, 0x0A, 0x70, 0x0A, 0xE5, 0x0A, 0xF0, 0x0A, 0x65, 0x0B, 0x70, 0x0B, 0xE6, 0x0B, 0xF0, 0x0B, 0x65, 0x0C, 0x70, 0x0C, 0xE5, 0x0C, 0xF0, 0x0C, 0x65, 0x0D, 0x70, 0x0D, 0x4F, 0x0E, 0x5A, 0x0E, 0xCF, 0x0E, 0x1F, 0x0F, 0x2A, 0x0F };

        #endregion

        #region Public Methods and Operators

        public override int Init(object objParam)
        {
            return TEST_PASS;
        }
        #endregion
    }
}
