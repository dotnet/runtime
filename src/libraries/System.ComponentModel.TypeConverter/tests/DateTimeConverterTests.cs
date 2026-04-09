// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class DateTimeConverterTests : TypeConverterTestBase
    {
        public override TypeConverter Converter => new DateTimeConverter();

        public override IEnumerable<ConvertTest> ConvertFromTestData()
        {
            DateTime date = new DateTime(1998, 12, 5);
            yield return ConvertTest.Valid("", DateTime.MinValue);
            yield return ConvertTest.Valid("    ", DateTime.MinValue);
            yield return ConvertTest.Valid(date.ToString(), date);
            yield return ConvertTest.Valid(date.ToString(CultureInfo.InvariantCulture.DateTimeFormat), date, CultureInfo.InvariantCulture);
            yield return ConvertTest.Valid(" " + date.ToString(CultureInfo.InvariantCulture.DateTimeFormat) + " ", date, CultureInfo.InvariantCulture);

            yield return ConvertTest.Throws<FormatException>("invalid");

            yield return ConvertTest.CantConvertFrom(new object());
            yield return ConvertTest.CantConvertFrom(1);
        }

        public override IEnumerable<ConvertTest> ConvertToTestData()
        {
            CultureInfo frenchCulture = new CultureInfo("fr-FR");
            CultureInfo polandCulture = new CultureInfo("pl-PL");
            DateTimeFormatInfo formatInfo = CultureInfo.CurrentCulture.DateTimeFormat;
            DateTime date = new DateTime(1998, 12, 5);
            yield return ConvertTest.Valid(date, date.ToString(formatInfo.ShortDatePattern));
            yield return ConvertTest.Valid(date, date.ToString(frenchCulture.DateTimeFormat.ShortDatePattern, frenchCulture.DateTimeFormat), frenchCulture)
                .WithRemoteInvokeCulture(frenchCulture);
            yield return ConvertTest.Valid(date, date.ToString(frenchCulture.DateTimeFormat.ShortDatePattern, frenchCulture.DateTimeFormat), frenchCulture)
                .WithRemoteInvokeCulture(polandCulture);
            yield return ConvertTest.Valid(date, date.ToString(polandCulture.DateTimeFormat.ShortDatePattern, polandCulture.DateTimeFormat), polandCulture)
                .WithRemoteInvokeCulture(polandCulture);
            yield return ConvertTest.Valid(date, date.ToString(polandCulture.DateTimeFormat.ShortDatePattern, polandCulture.DateTimeFormat), polandCulture)
                .WithRemoteInvokeCulture(frenchCulture);
            yield return ConvertTest.Valid(date, "1998-12-05", CultureInfo.InvariantCulture)
                .WithRemoteInvokeCulture(polandCulture);

            DateTime dateWithTime = new DateTime(1998, 12, 5, 22, 30, 30);
            yield return ConvertTest.Valid(dateWithTime, dateWithTime.ToString(formatInfo.ShortDatePattern + " " + formatInfo.ShortTimePattern));
            yield return ConvertTest.Valid(dateWithTime, dateWithTime.ToString(frenchCulture.DateTimeFormat.ShortDatePattern + " " + frenchCulture.DateTimeFormat.ShortTimePattern, frenchCulture.DateTimeFormat), frenchCulture)
                .WithRemoteInvokeCulture(frenchCulture);
            yield return ConvertTest.Valid(dateWithTime, dateWithTime.ToString(frenchCulture.DateTimeFormat.ShortDatePattern + " " + frenchCulture.DateTimeFormat.ShortTimePattern, frenchCulture.DateTimeFormat), frenchCulture)
                .WithRemoteInvokeCulture(polandCulture);
            yield return ConvertTest.Valid(dateWithTime, dateWithTime.ToString(polandCulture.DateTimeFormat.ShortDatePattern + " " + polandCulture.DateTimeFormat.ShortTimePattern, polandCulture.DateTimeFormat), polandCulture)
                .WithRemoteInvokeCulture(polandCulture);
            yield return ConvertTest.Valid(dateWithTime, dateWithTime.ToString(polandCulture.DateTimeFormat.ShortDatePattern + " " + polandCulture.DateTimeFormat.ShortTimePattern, polandCulture.DateTimeFormat), polandCulture)
                .WithRemoteInvokeCulture(frenchCulture);
            yield return ConvertTest.Valid(dateWithTime, "12/05/1998 22:30:30", CultureInfo.InvariantCulture)
                .WithRemoteInvokeCulture(polandCulture);

            yield return ConvertTest.Valid(DateTime.MinValue, string.Empty);

            yield return ConvertTest.Valid(
                new DateTime(),
                new InstanceDescriptor(
                    typeof(DateTime).GetConstructor(new Type[] { typeof(long) }),
                    new object[] { (long)0 }
                )
            );
            yield return ConvertTest.Valid(
                date,
                new InstanceDescriptor(
                    typeof(DateTime).GetConstructor(new Type[] { typeof(int), typeof(int), typeof(int), typeof(int),  typeof(int), typeof(int), typeof(int) }),
                    new object[] { 1998, 12, 5, 0, 0, 0, 0 }
                )
            );
            yield return ConvertTest.Valid(
                dateWithTime,
                new InstanceDescriptor(
                    typeof(DateTime).GetConstructor(new Type[] { typeof(int), typeof(int), typeof(int), typeof(int),  typeof(int), typeof(int), typeof(int) }),
                    new object[] { 1998, 12, 5, 22, 30, 30, 0 }
                )
            );
            yield return ConvertTest.Valid(
                dateWithTime,
                new InstanceDescriptor(
                    typeof(DateTime).GetConstructor(new Type[] { typeof(int), typeof(int), typeof(int), typeof(int),  typeof(int), typeof(int), typeof(int) }),
                    new object[] { 1998, 12, 5, 22, 30, 30, 0 }
                ),
                CultureInfo.InvariantCulture
            );

            yield return ConvertTest.CantConvertTo(new DateTime(), typeof(DateTime));
            yield return ConvertTest.CantConvertTo(new DateTime(), typeof(int));
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
