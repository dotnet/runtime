// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class TimeSpanConverterTests : TypeConverterTestBase
    {
        public override TypeConverter Converter => new TimeSpanConverter();

        public override IEnumerable<ConvertTest> ConvertFromTestData()
        {
            yield return ConvertTest.Valid("  1000.00:00:00  ", new TimeSpan(1000, 0, 0, 0, 0), CultureInfo.InvariantCulture);
            yield return ConvertTest.Valid("1000", new TimeSpan(1000, 0, 0, 0, 0));

            yield return ConvertTest.Throws<FormatException>("invalid");
            yield return ConvertTest.Throws<FormatException>("  ");
            yield return ConvertTest.Throws<FormatException>(string.Empty);

            yield return ConvertTest.CantConvertFrom(1);
            yield return ConvertTest.CantConvertFrom(new object());
        }

        public override IEnumerable<ConvertTest> ConvertToTestData()
        {
            var timeSpan = new TimeSpan(1000, 0, 0, 0, 0);
            yield return ConvertTest.Valid(timeSpan, timeSpan.ToString());

            yield return ConvertTest.Valid(
                timeSpan,
                new InstanceDescriptor(
                    typeof(TimeSpan).GetMethod(nameof(TimeSpan.Parse), new Type[] { typeof(string) }),
                    new object[] { timeSpan.ToString() }
                )
            );

            yield return ConvertTest.CantConvertTo(new TimeSpan(1000, 0, 0, 0, 0), typeof(object));
        }

        [Theory]
        [InlineData(typeof(InstanceDescriptor))]
        [InlineData(typeof(int))]
        public void ConvertTo_InvalidValue_ThrowsNotSupportedException(Type destinationType)
        {
            Assert.Throws<NotSupportedException>(() => Converter.ConvertTo(new object(), destinationType));
        }
    }
}
