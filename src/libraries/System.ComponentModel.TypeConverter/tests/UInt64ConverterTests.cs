// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Globalization;

namespace System.ComponentModel.Tests
{
    public class UInt64ConverterTests : BaseNumberConverterTests
    {
        public override TypeConverter Converter => new UInt64Converter();

        public override IEnumerable<ConvertTest> ConvertToTestData()
        {
            yield return ConvertTest.Valid((ulong)1, "1");
            yield return ConvertTest.Valid((ulong)2, (ulong)2, CultureInfo.InvariantCulture);
            yield return ConvertTest.Valid((ulong)3, (float)3.0);

            yield return ConvertTest.CantConvertTo((ulong)3, typeof(InstanceDescriptor));
            yield return ConvertTest.CantConvertTo((ulong)3, typeof(object));
        }

        public override IEnumerable<ConvertTest> ConvertFromTestData()
        {
            yield return ConvertTest.Valid("1", (ulong)1);
            yield return ConvertTest.Valid("#2", (ulong)2);
            yield return ConvertTest.Valid(" #2 ", (ulong)2);
            yield return ConvertTest.Valid("0x3", (ulong)3);
            yield return ConvertTest.Valid("0X3", (ulong)3);
            yield return ConvertTest.Valid(" 0X3 ", (ulong)3);
            yield return ConvertTest.Valid("&h4", (ulong)4);
            yield return ConvertTest.Valid("&H4", (ulong)4);
            yield return ConvertTest.Valid(" &H4 ", (ulong)4);
            yield return ConvertTest.Valid("+5", (ulong)5);
            yield return ConvertTest.Valid(" +5 ", (ulong)5);

            yield return ConvertTest.Valid("!1", (ulong)1, new CustomPositiveSymbolCulture());

            yield return ConvertTest.Throws<ArgumentException, Exception>("-1");
            yield return ConvertTest.Throws<ArgumentException, Exception>("18446744073709551616");

            foreach (ConvertTest test in base.ConvertFromTestData())
            {
                yield return test;
            }
        }
    }
}
