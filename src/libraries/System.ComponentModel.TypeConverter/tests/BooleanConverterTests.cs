// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Linq;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class BooleanConverterTests : TypeConverterTestBase
    {
        public override TypeConverter Converter => new BooleanConverter();

        public override bool StandardValuesSupported => true;
        public override bool StandardValuesExclusive => true;

        public override IEnumerable<ConvertTest> ConvertToTestData()
        {
            yield return ConvertTest.Valid(true, Boolean.TrueString);
            yield return ConvertTest.Valid(1, "1");

            yield return ConvertTest.CantConvertTo(true, typeof(bool));
            yield return ConvertTest.CantConvertTo(true, typeof(InstanceDescriptor));
            yield return ConvertTest.CantConvertTo(true, typeof(object));
        }

        public override IEnumerable<ConvertTest> ConvertFromTestData()
        {
            yield return ConvertTest.Valid("false  ", false);
            yield return ConvertTest.Valid("true", true, CultureInfo.InvariantCulture);

            yield return ConvertTest.Throws<FormatException>("1");

            yield return ConvertTest.CantConvertFrom(1);
            yield return ConvertTest.CantConvertFrom(null);
        }

        [Fact]
        public void StandardValues_Get_ReturnsExpected()
        {
            ICollection values = Converter.GetStandardValues();
            Assert.Same(values, Converter.GetStandardValues());
            Assert.Equal(new object[] { true, false }, values.Cast<object>());
        }
    }
}
