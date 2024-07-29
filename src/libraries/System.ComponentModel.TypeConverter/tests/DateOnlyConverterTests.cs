// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class DateOnlyConverterTests : TypeConverterTestBase
    {
        public override TypeConverter Converter => new DateOnlyConverter();

        public override IEnumerable<ConvertTest> ConvertFromTestData()
        {
            DateOnly dateOnly = new DateOnly(1998, 12, 5);
            yield return ConvertTest.Valid("", DateOnly.MinValue);
            yield return ConvertTest.Valid("    ", DateOnly.MinValue);
            yield return ConvertTest.Valid(dateOnly.ToString(), dateOnly);
            yield return ConvertTest.Valid(dateOnly.ToString(CultureInfo.InvariantCulture.DateTimeFormat), dateOnly, CultureInfo.InvariantCulture);
            yield return ConvertTest.Valid(" " + dateOnly.ToString(CultureInfo.InvariantCulture.DateTimeFormat) + " ", dateOnly, CultureInfo.InvariantCulture);

            yield return ConvertTest.Throws<FormatException>("invalid");

            yield return ConvertTest.CantConvertFrom(new object());
            yield return ConvertTest.CantConvertFrom(1);
        }

        public override IEnumerable<ConvertTest> ConvertToTestData()
        {
            CultureInfo frenchCulture = new CultureInfo("fr-FR");
            CultureInfo polandCulture = new CultureInfo("pl-PL");
            DateTimeFormatInfo formatInfo = CultureInfo.CurrentCulture.DateTimeFormat;
            DateOnly dateOnly = new DateOnly(1998, 12, 5);
            yield return ConvertTest.Valid(dateOnly, dateOnly.ToString(formatInfo.ShortDatePattern));
            yield return ConvertTest.Valid(dateOnly, dateOnly.ToString(frenchCulture.DateTimeFormat.ShortDatePattern, frenchCulture.DateTimeFormat), frenchCulture)
                .WithRemoteInvokeCulture(frenchCulture);
            yield return ConvertTest.Valid(dateOnly, dateOnly.ToString(frenchCulture.DateTimeFormat.ShortDatePattern, frenchCulture.DateTimeFormat), frenchCulture)
                .WithRemoteInvokeCulture(polandCulture);
            yield return ConvertTest.Valid(dateOnly, dateOnly.ToString(polandCulture.DateTimeFormat.ShortDatePattern, polandCulture.DateTimeFormat), polandCulture)
                .WithRemoteInvokeCulture(polandCulture);
            yield return ConvertTest.Valid(dateOnly, dateOnly.ToString(polandCulture.DateTimeFormat.ShortDatePattern, polandCulture.DateTimeFormat), polandCulture)
                .WithRemoteInvokeCulture(frenchCulture);
            yield return ConvertTest.Valid(dateOnly, "1998-12-05", CultureInfo.InvariantCulture)
                .WithRemoteInvokeCulture(polandCulture);

            yield return ConvertTest.Valid(DateOnly.MinValue, string.Empty);

            yield return ConvertTest.Valid(
                dateOnly,
                new InstanceDescriptor(
                    typeof(DateOnly).GetConstructor(new Type[] { typeof(int), typeof(int), typeof(int) }),
                    new object[] { 1998, 12, 5 }
                )
            );

            yield return ConvertTest.CantConvertTo(new DateOnly(), typeof(DateOnly));
            yield return ConvertTest.CantConvertTo(new DateOnly(), typeof(int));
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
