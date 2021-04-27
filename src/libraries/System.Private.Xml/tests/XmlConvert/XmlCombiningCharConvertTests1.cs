// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using OLEDB.Test.ModuleCore;
using System.Buffers.Binary;

namespace System.Xml.Tests
{
    internal class XmlCombiningCharConvertTests1 : XmlCombiningCharConvertTests
    {
        #region Constructors and Destructors

        public XmlCombiningCharConvertTests1()
        {
            for (int i = 0; i < _byte_CombiningChar.Length; i = i + 2)
            {
                AddVariation(new CVariation(this, "EncodeName-EncodeLocalName : " + _Expbyte_CombiningChar[i / 2], XmlEncodeName1));
            }
        }

        #endregion

        #region Public Methods and Operators

        public int XmlEncodeName1()
        {
            int i = ((CurVariation.id) - 1) * 2;
            string strEnVal = string.Empty;

            char c = (char)BinaryPrimitives.ReadUInt16LittleEndian(new Span<byte>(_byte_CombiningChar, i, 2));
            strEnVal = XmlConvert.EncodeName(c.ToString());
            CError.Compare(strEnVal, _Expbyte_CombiningChar[i / 2], "Comparison failed at " + i);
            return TEST_PASS;
        }
        #endregion
    }
}
