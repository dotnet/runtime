// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;

namespace System.ComponentModel.Tests
{
    public class Int16ConverterTests : TypeConverterTestBase
    {
        public override TypeConverter Converter => new Int16Converter();

        public override IEnumerable<ConvertTest> ConvertToTestData()
        {
            yield return ConvertTest.Valid((short)-1, "-1");
            yield return ConvertTest.Valid((short)2, (short)2, CultureInfo.InvariantCulture);
            yield return ConvertTest.Valid((short)3, (float)3.0);
        }

        public override IEnumerable<ConvertTest> ConvertFromTestData()
        {
            yield return ConvertTest.Valid("1", (short)1);
            yield return ConvertTest.Valid("-1  ", (short)-1);
            yield return ConvertTest.Valid("#2", (short)2);
            yield return ConvertTest.Valid("+7", (short)7);

            yield return ConvertTest.Throws<ArgumentException, Exception>("8.0");
            yield return ConvertTest.Throws<ArgumentException, Exception>("");
            yield return ConvertTest.Throws<ArgumentException, Exception>("bad");

            yield return ConvertTest.Throws<ArgumentException, Exception>("32768");
            yield return ConvertTest.Throws<ArgumentException, Exception>("-32769");

            yield return ConvertTest.CantConvert(new object());
        }
    }
}
