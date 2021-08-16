// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using OLEDB.Test.ModuleCore;
using System.Buffers.Binary;

namespace System.Xml.Tests
{
    internal class XmlDigitCharConvertTests3 : XmlDigitCharConvertTests
    {
        #region Constructors and Destructors

        public XmlDigitCharConvertTests3()
        {
            for (int i = 0; i < _byte_Digit.Length; i = i + 2)
            {
                AddVariation(new CVariation(this, "EncodeName-DecodeName : " + _Expbyte_Digit[i / 2], XmlEncodeName3));
            }
        }

        #endregion

        #region Public Methods and Operators

        public int XmlEncodeName3()
        {
            int i = ((CurVariation.id) - 1) * 2;
            string strDeVal = string.Empty;
            string strEnVal = string.Empty;
            string strVal = string.Empty;

            char c = (char)BinaryPrimitives.ReadUInt16LittleEndian(new Span<byte>(_byte_Digit, i, 2));
            strVal = c.ToString();
            strEnVal = XmlConvert.EncodeName(strVal);
            CError.Compare(strEnVal, _Expbyte_Digit[i / 2], "Encode Comparison failed at " + i);

            strDeVal = XmlConvert.DecodeName(strEnVal);
            CError.Compare(strDeVal, strVal, "Decode Comparison failed at " + i);
            return TEST_PASS;
        }
        #endregion
    }
}
