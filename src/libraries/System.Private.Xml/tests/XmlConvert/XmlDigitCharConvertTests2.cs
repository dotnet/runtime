// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using OLEDB.Test.ModuleCore;
using System.Buffers.Binary;

namespace System.Xml.Tests
{
    internal class XmlDigitCharConvertTests2 : XmlDigitCharConvertTests
    {
        #region Constructors and Destructors

        public XmlDigitCharConvertTests2()
        {
            for (int i = 0; i < _byte_Digit.Length; i = i + 2)
            {
                AddVariation(new CVariation(this, "EncodeNmToken-EncodeLocalNmToken : " + _Expbyte_Digit[i / 2], XmlEncodeName2));
            }
        }

        #endregion

        #region Public Methods and Operators

        public int XmlEncodeName2()
        {
            int i = ((CurVariation.id) - 1) * 2;
            string strEnVal = string.Empty;

            char c = (char)BinaryPrimitives.ReadUInt16LittleEndian(new Span<byte>(_byte_Digit, i, 2));
            strEnVal = XmlConvert.EncodeNmToken(c.ToString());
            if (_Expbyte_Digit[i / 2] != "_x0A70_")
            {
                CError.Compare(strEnVal, _Expbyte_Digit[i / 2], "Comparison failed at " + i);
            }
            return TEST_PASS;
        }
        #endregion
    }
}
