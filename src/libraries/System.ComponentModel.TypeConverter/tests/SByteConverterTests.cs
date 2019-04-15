// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Globalization;

namespace System.ComponentModel.Tests
{
    public class SByteConverterTests : BaseNumberConverterTests
    {
        public override TypeConverter Converter => new SByteConverter();

        public override IEnumerable<ConvertTest> ConvertToTestData()
        {
            yield return ConvertTest.Valid((sbyte)-1, "-1");
            yield return ConvertTest.Valid((sbyte)2, (sbyte)2, CultureInfo.InvariantCulture);
            yield return ConvertTest.Valid((sbyte)3, (float)3.0);

            yield return ConvertTest.Valid((sbyte)-1, "?1", new CustomPositiveSymbolCulture());

            yield return ConvertTest.CantConvertTo((sbyte)3, typeof(InstanceDescriptor));
            yield return ConvertTest.CantConvertTo((sbyte)3, typeof(object));
        }

        public override IEnumerable<ConvertTest> ConvertFromTestData()
        {
            yield return ConvertTest.Valid("1", (sbyte)1);
            yield return ConvertTest.Valid(" -1 ", (sbyte)-1);
            yield return ConvertTest.Valid("#2", (sbyte)2);
            yield return ConvertTest.Valid(" #2 ", (sbyte)2);
            yield return ConvertTest.Valid("0x3", (sbyte)3);
            yield return ConvertTest.Valid("0X3", (sbyte)3);
            yield return ConvertTest.Valid(" 0X3 ", (sbyte)3);
            yield return ConvertTest.Valid("&h4", (sbyte)4);
            yield return ConvertTest.Valid("&H4", (sbyte)4);
            yield return ConvertTest.Valid(" &H4 ", (sbyte)4);
            yield return ConvertTest.Valid("+5", (sbyte)5);
            yield return ConvertTest.Valid(" +5 ", (sbyte)5);

            yield return ConvertTest.Valid("!1", (sbyte)1, new CustomPositiveSymbolCulture());

            yield return ConvertTest.Throws<ArgumentException, Exception>("128");
            yield return ConvertTest.Throws<ArgumentException, Exception>("-129");

            foreach (ConvertTest test in base.ConvertFromTestData())
            {
                yield return test;
            }
        }
    }
}
