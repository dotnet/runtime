// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Globalization;

namespace System.ComponentModel.Tests
{
    public class UInt32ConverterTests : BaseNumberConverterTests
    {
        public override TypeConverter Converter => new UInt32Converter();

        public override IEnumerable<ConvertTest> ConvertToTestData()
        {
            yield return ConvertTest.Valid((uint)1, "1");
            yield return ConvertTest.Valid((uint)2, (uint)2, CultureInfo.InvariantCulture);
            yield return ConvertTest.Valid((uint)3, (float)3.0);

            yield return ConvertTest.CantConvertTo((uint)3, typeof(InstanceDescriptor));
            yield return ConvertTest.CantConvertTo((uint)3, typeof(object));
        }

        public override IEnumerable<ConvertTest> ConvertFromTestData()
        {
            yield return ConvertTest.Valid("1", (uint)1);
            yield return ConvertTest.Valid("#2", (uint)2);
            yield return ConvertTest.Valid(" #2 ", (uint)2);
            yield return ConvertTest.Valid("0x3", (uint)3);
            yield return ConvertTest.Valid("0X3", (uint)3);
            yield return ConvertTest.Valid(" 0X3 ", (uint)3);
            yield return ConvertTest.Valid("&h4", (uint)4);
            yield return ConvertTest.Valid("&H4", (uint)4);
            yield return ConvertTest.Valid(" &H4 ", (uint)4);
            yield return ConvertTest.Valid("+5", (uint)5);
            yield return ConvertTest.Valid(" +5 ", (uint)5);

            yield return ConvertTest.Valid("!1", (uint)1, new CustomPositiveSymbolCulture());

            yield return ConvertTest.Throws<ArgumentException, Exception>("-1");
            yield return ConvertTest.Throws<ArgumentException, Exception>("4294967296");

            foreach (ConvertTest test in base.ConvertFromTestData())
            {
                yield return test;
            }
        }
    }
}
