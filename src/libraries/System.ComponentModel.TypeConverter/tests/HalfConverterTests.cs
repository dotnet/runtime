// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Globalization;

namespace System.ComponentModel.Tests
{
    public class HalfConverterTests : BaseNumberConverterTests
    {
        public override TypeConverter Converter => new HalfConverter();

        public override IEnumerable<ConvertTest> ConvertToTestData()
        {
            yield return ConvertTest.Valid((Half)(-1), "-1");
            yield return ConvertTest.Valid((Half)1.1, ((Half)1.1).ToString());

            yield return ConvertTest.Valid((Half)(-1), "?1", new CustomPositiveSymbolCulture());

            yield return ConvertTest.CantConvertTo((Half)3, typeof(InstanceDescriptor));
            yield return ConvertTest.CantConvertTo((Half)3, typeof(object));
        }

        public override IEnumerable<ConvertTest> ConvertFromTestData()
        {
            yield return ConvertTest.Valid("1", (Half)1);
            yield return ConvertTest.Valid(1.1.ToString(), (Half)1.1);
            yield return ConvertTest.Valid(" -1 ", (Half)(-1));
            yield return ConvertTest.Valid("+5", (Half)5);
            yield return ConvertTest.Valid(" +5 ", (Half)5);

            yield return ConvertTest.Throws<ArgumentException, Exception>("#2");
            yield return ConvertTest.Throws<ArgumentException, Exception>(" #2 ");
            yield return ConvertTest.Throws<ArgumentException, Exception>("0x3");
            yield return ConvertTest.Throws<ArgumentException>("0X3");
            yield return ConvertTest.Throws<ArgumentException>(" 0X3 ");
            yield return ConvertTest.Throws<ArgumentException>("&h4");
            yield return ConvertTest.Throws<ArgumentException>("&H4");
            yield return ConvertTest.Throws<ArgumentException>(" &H4 ");

            foreach (ConvertTest test in base.ConvertFromTestData())
            {
                yield return test;
            }
        }
    }
}
