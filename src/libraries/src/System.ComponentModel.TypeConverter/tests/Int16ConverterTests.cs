// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Globalization;

namespace System.ComponentModel.Tests
{
    public class Int16ConverterTests : BaseNumberConverterTests
    {
        public override TypeConverter Converter => new Int16Converter();

        public override IEnumerable<ConvertTest> ConvertToTestData()
        {
            yield return ConvertTest.Valid((short)-1, "-1");
            yield return ConvertTest.Valid((short)2, (short)2, CultureInfo.InvariantCulture);
            yield return ConvertTest.Valid((short)3, (float)3.0);

            yield return ConvertTest.Valid((short)-1, "?1", new CustomPositiveSymbolCulture());

            yield return ConvertTest.CantConvertTo((short)3, typeof(InstanceDescriptor));
            yield return ConvertTest.CantConvertTo((short)3, typeof(object));
        }

        public override IEnumerable<ConvertTest> ConvertFromTestData()
        {
            yield return ConvertTest.Valid("1", (short)1);
            yield return ConvertTest.Valid(" -1 ", (short)-1);
            yield return ConvertTest.Valid("#2", (short)2);
            yield return ConvertTest.Valid(" #2 ", (short)2);
            yield return ConvertTest.Valid("0x3", (short)3);
            yield return ConvertTest.Valid("0X3", (short)3);
            yield return ConvertTest.Valid(" 0X3 ", (short)3);
            yield return ConvertTest.Valid("&h4", (short)4);
            yield return ConvertTest.Valid("&H4", (short)4);
            yield return ConvertTest.Valid(" &H4 ", (short)4);
            yield return ConvertTest.Valid("+5", (short)5);
            yield return ConvertTest.Valid(" +5 ", (short)5);

            yield return ConvertTest.Throws<ArgumentException, Exception>("32768");
            yield return ConvertTest.Throws<ArgumentException, Exception>("-32769");

            foreach (ConvertTest test in base.ConvertFromTestData())
            {
                yield return test;
            }
        }
    }
}
