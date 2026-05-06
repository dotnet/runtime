// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Globalization;

namespace System.ComponentModel.Tests
{
    public class UInt128ConverterTests : BaseNumberConverterTests
    {
        public override TypeConverter Converter => new UInt128Converter();

        public override IEnumerable<ConvertTest> ConvertToTestData()
        {
            yield return ConvertTest.Valid((UInt128)1, "1");

            yield return ConvertTest.CantConvertTo((UInt128)3, typeof(InstanceDescriptor));
            yield return ConvertTest.CantConvertTo((UInt128)3, typeof(object));
        }

        public override IEnumerable<ConvertTest> ConvertFromTestData()
        {
            yield return ConvertTest.Valid("1", (UInt128)1);
            yield return ConvertTest.Valid("#2", (UInt128)2);
            yield return ConvertTest.Valid(" #2 ", (UInt128)2);
            yield return ConvertTest.Valid("0x3", (UInt128)3);
            yield return ConvertTest.Valid("0X3", (UInt128)3);
            yield return ConvertTest.Valid(" 0X3 ", (UInt128)3);
            yield return ConvertTest.Valid("&h4", (UInt128)4);
            yield return ConvertTest.Valid("&H4", (UInt128)4);
            yield return ConvertTest.Valid(" &H4 ", (UInt128)4);
            yield return ConvertTest.Valid("+5", (UInt128)5);
            yield return ConvertTest.Valid(" +5 ", (UInt128)5);

            yield return ConvertTest.Valid("!1", (UInt128)1, new CustomPositiveSymbolCulture());

            yield return ConvertTest.Throws<ArgumentException, Exception>("-1");
            yield return ConvertTest.Throws<ArgumentException, Exception>("340282366920938463463374607431768211456");

            foreach (ConvertTest test in base.ConvertFromTestData())
            {
                yield return test;
            }
        }
    }
}
