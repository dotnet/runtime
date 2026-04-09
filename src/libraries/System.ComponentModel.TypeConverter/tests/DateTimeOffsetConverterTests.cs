// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class DateTimeOffsetConverterTests : TypeConverterTestBase
    {
        public override TypeConverter Converter => new DateTimeOffsetConverter();

        public override IEnumerable<ConvertTest> ConvertFromTestData()
        {
            DateTimeOffset offset = new DateTimeOffset(new DateTime(1998, 12, 5));
            yield return ConvertTest.Valid("", DateTimeOffset.MinValue);
            yield return ConvertTest.Valid("    ", DateTimeOffset.MinValue);
            yield return ConvertTest.Valid(offset.ToString(), offset);
            yield return ConvertTest.Valid(offset.ToString(CultureInfo.InvariantCulture.DateTimeFormat), offset, CultureInfo.InvariantCulture);
            yield return ConvertTest.Valid(" " + offset.ToString(CultureInfo.InvariantCulture.DateTimeFormat) + " ", offset, CultureInfo.InvariantCulture);

            yield return ConvertTest.Throws<FormatException>("invalid");

            yield return ConvertTest.CantConvertFrom(new object());
            yield return ConvertTest.CantConvertFrom(1);
        }

        public override IEnumerable<ConvertTest> ConvertToTestData()
        {
            CultureInfo frenchCulture = new CultureInfo("fr-FR");
            CultureInfo polandCulture = new CultureInfo("pl-PL");
            DateTimeFormatInfo formatInfo = CultureInfo.CurrentCulture.DateTimeFormat;
            DateTimeOffset offset = new DateTimeOffset(new DateTime(1998, 12, 5));
            yield return ConvertTest.Valid(offset, offset.ToString(formatInfo.ShortDatePattern + " zzz"));
            yield return ConvertTest.Valid(offset, offset.ToString(frenchCulture.DateTimeFormat.ShortDatePattern + " zzz", frenchCulture.DateTimeFormat), frenchCulture)
            .WithRemoteInvokeCulture(frenchCulture);
            yield return ConvertTest.Valid(offset, offset.ToString(frenchCulture.DateTimeFormat.ShortDatePattern + " zzz", frenchCulture.DateTimeFormat), frenchCulture)
            .WithRemoteInvokeCulture(polandCulture);
            yield return ConvertTest.Valid(offset, offset.ToString(polandCulture.DateTimeFormat.ShortDatePattern + " zzz", polandCulture.DateTimeFormat), polandCulture)
            .WithRemoteInvokeCulture(polandCulture);
            yield return ConvertTest.Valid(offset, offset.ToString(polandCulture.DateTimeFormat.ShortDatePattern + " zzz", polandCulture.DateTimeFormat), polandCulture)
                .WithRemoteInvokeCulture(frenchCulture);
            yield return ConvertTest.Valid(offset, offset.ToString("yyyy-MM-dd zzz", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture)
                .WithRemoteInvokeCulture(polandCulture);

            DateTimeOffset offsetWithTime = new DateTimeOffset(new DateTime(1998, 12, 5, 22, 30, 30));
            yield return ConvertTest.Valid(offsetWithTime, offsetWithTime.ToString(formatInfo.ShortDatePattern + " " + formatInfo.ShortTimePattern + " zzz"));
            yield return ConvertTest.Valid(offsetWithTime, offsetWithTime.ToString(frenchCulture.DateTimeFormat.ShortDatePattern + " " + frenchCulture.DateTimeFormat.ShortTimePattern + " zzz", frenchCulture.DateTimeFormat), frenchCulture)
                .WithRemoteInvokeCulture(frenchCulture);
            yield return ConvertTest.Valid(offsetWithTime, offsetWithTime.ToString(frenchCulture.DateTimeFormat.ShortDatePattern + " " + frenchCulture.DateTimeFormat.ShortTimePattern + " zzz", frenchCulture.DateTimeFormat), frenchCulture)
                .WithRemoteInvokeCulture(polandCulture);
            yield return ConvertTest.Valid(offsetWithTime, offsetWithTime.ToString(polandCulture.DateTimeFormat.ShortDatePattern + " " + polandCulture.DateTimeFormat.ShortTimePattern + " zzz", polandCulture.DateTimeFormat), polandCulture)
                .WithRemoteInvokeCulture(polandCulture);
            yield return ConvertTest.Valid(offsetWithTime, offsetWithTime.ToString(polandCulture.DateTimeFormat.ShortDatePattern + " " + polandCulture.DateTimeFormat.ShortTimePattern + " zzz", polandCulture.DateTimeFormat), polandCulture)
                .WithRemoteInvokeCulture(frenchCulture);
            yield return ConvertTest.Valid(offsetWithTime, offsetWithTime.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture)
                .WithRemoteInvokeCulture(polandCulture);

            yield return ConvertTest.Valid(DateTimeOffset.MinValue, string.Empty);

            yield return ConvertTest.Valid(
                new DateTimeOffset(),
                new InstanceDescriptor(
                    typeof(DateTimeOffset).GetConstructor(new Type[] { typeof(long) }),
                    new object[] { (long)0 }
                )
            );
            yield return ConvertTest.Valid(
                offset,
                new InstanceDescriptor(
                    typeof(DateTimeOffset).GetConstructor(new Type[] { typeof(int), typeof(int), typeof(int), typeof(int),  typeof(int), typeof(int), typeof(int), typeof(TimeSpan) }),
                    new object[] { 1998, 12, 5, 0, 0, 0, 0, offset.Offset }
                )
            );
            yield return ConvertTest.Valid(
                offsetWithTime,
                new InstanceDescriptor(
                    typeof(DateTimeOffset).GetConstructor(new Type[] { typeof(int), typeof(int), typeof(int), typeof(int),  typeof(int), typeof(int), typeof(int), typeof(TimeSpan) }),
                    new object[] { 1998, 12, 5, 22, 30, 30, 0, offsetWithTime.Offset }
                )
            );
            yield return ConvertTest.Valid(
                offsetWithTime,
                new InstanceDescriptor(
                    typeof(DateTimeOffset).GetConstructor(new Type[] { typeof(int), typeof(int), typeof(int), typeof(int),  typeof(int), typeof(int), typeof(int), typeof(TimeSpan) }),
                    new object[] { 1998, 12, 5, 22, 30, 30, 0, offsetWithTime.Offset }
                ),
                CultureInfo.InvariantCulture
            );

            yield return ConvertTest.CantConvertTo(new DateTimeOffset(), typeof(DateTimeOffset));
            yield return ConvertTest.CantConvertTo(new DateTimeOffset(), typeof(int));
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
