// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Globalization;

namespace System.ComponentModel.Tests
{
    public class Int128ConverterTests : BaseNumberConverterTests
    {
        public override TypeConverter Converter => new Int128Converter();

        public override IEnumerable<ConvertTest> ConvertToTestData()
        {
            yield return ConvertTest.Valid((Int128)(-1), "-1");
            yield return ConvertTest.Valid((Int128)(-1), "?1", new CustomPositiveSymbolCulture());

            yield return ConvertTest.CantConvertTo((Int128)3, typeof(InstanceDescriptor));
            yield return ConvertTest.CantConvertTo((Int128)3, typeof(object));
        }

        public override IEnumerable<ConvertTest> ConvertFromTestData()
        {
            yield return ConvertTest.Valid("1", (Int128)1);
            yield return ConvertTest.Valid(" -1 ", (Int128)(-1));
            yield return ConvertTest.Valid("#2", (Int128)2);
            yield return ConvertTest.Valid(" #2 ", (Int128)2);
            yield return ConvertTest.Valid("0x3", (Int128)3);
            yield return ConvertTest.Valid("0X3", (Int128)3);
            yield return ConvertTest.Valid(" 0X3 ", (Int128)3);
            yield return ConvertTest.Valid("&h4", (Int128)4);
            yield return ConvertTest.Valid("&H4", (Int128)4);
            yield return ConvertTest.Valid(" &H4 ", (Int128)4);
            yield return ConvertTest.Valid("+5", (Int128)5);
            yield return ConvertTest.Valid(" +5 ", (Int128)5);

            yield return ConvertTest.Throws<ArgumentException, Exception>("170141183460469231731687303715884105728");
            yield return ConvertTest.Throws<ArgumentException, Exception>("-170141183460469231731687303715884105729");

            foreach (ConvertTest test in base.ConvertFromTestData())
            {
                yield return test;
            }
        }
    }
}
