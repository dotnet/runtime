// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Globalization;

namespace System.ComponentModel.Tests
{
    public class Int64ConverterTests : BaseNumberConverterTests
    {
        public override TypeConverter Converter => new Int64Converter();

        public override IEnumerable<ConvertTest> ConvertToTestData()
        {
            yield return ConvertTest.Valid((long)-1, "-1");
            yield return ConvertTest.Valid((long)2, (long)2, CultureInfo.InvariantCulture);
            yield return ConvertTest.Valid((long)3, (float)3.0);

            yield return ConvertTest.Valid((long)-1, "?1", new CustomPositiveSymbolCulture());

            yield return ConvertTest.CantConvertTo((long)3, typeof(InstanceDescriptor));
            yield return ConvertTest.CantConvertTo((long)3, typeof(object));
        }

        public override IEnumerable<ConvertTest> ConvertFromTestData()
        {
            yield return ConvertTest.Valid("1", (long)1);
            yield return ConvertTest.Valid(" -1 ", (long)-1);
            yield return ConvertTest.Valid("#2", (long)2);
            yield return ConvertTest.Valid(" #2 ", (long)2);
            yield return ConvertTest.Valid("0x3", (long)3);
            yield return ConvertTest.Valid("0X3", (long)3);
            yield return ConvertTest.Valid(" 0X3 ", (long)3);
            yield return ConvertTest.Valid("&h4", (long)4);
            yield return ConvertTest.Valid("&H4", (long)4);
            yield return ConvertTest.Valid(" &H4 ", (long)4);
            yield return ConvertTest.Valid("+5", (long)5);
            yield return ConvertTest.Valid(" +5 ", (long)5);

            yield return ConvertTest.Throws<ArgumentException, Exception>("9223372036854775808");
            yield return ConvertTest.Throws<ArgumentException, Exception>("-9223372036854775809");

            foreach (ConvertTest test in base.ConvertFromTestData())
            {
                yield return test;
            }
        }
    }
}
