// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Reflection;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class TimeOnlyConverterTests : TypeConverterTestBase
    {
        public override TypeConverter Converter => new TimeOnlyConverter();

        public override IEnumerable<ConvertTest> ConvertFromTestData()
        {
            TimeOnly timeOnly = new TimeOnly(10, 30, 50);
            yield return ConvertTest.Valid("", TimeOnly.MinValue);

            yield return ConvertTest.Valid("    ", TimeOnly.MinValue);

            yield return ConvertTest.Valid(timeOnly.ToString(), TimeOnly.Parse(timeOnly.ToString()));

            yield return ConvertTest.Valid(timeOnly.ToString(CultureInfo.InvariantCulture.DateTimeFormat),
                                            TimeOnly.Parse(timeOnly.ToString(CultureInfo.InvariantCulture.DateTimeFormat)));

            yield return ConvertTest.Valid(" " + timeOnly.ToString(CultureInfo.InvariantCulture.DateTimeFormat) + " ",
                                            TimeOnly.Parse(timeOnly.ToString(CultureInfo.InvariantCulture.DateTimeFormat)));

            yield return ConvertTest.Throws<FormatException>("invalid");

            yield return ConvertTest.CantConvertFrom(new object());
            yield return ConvertTest.CantConvertFrom(1);
        }

        public override IEnumerable<ConvertTest> ConvertToTestData()
        {
            CultureInfo polandCulture = new CultureInfo("pl-PL");
            DateTimeFormatInfo formatInfo = CultureInfo.CurrentCulture.DateTimeFormat;
            TimeOnly timeOnly = new TimeOnly(10, 30, 50);

            yield return ConvertTest.Valid(timeOnly, timeOnly.ToString(formatInfo.ShortTimePattern));

            yield return ConvertTest.Valid(timeOnly, timeOnly.ToString(polandCulture.DateTimeFormat.ShortTimePattern, polandCulture.DateTimeFormat))
                .WithRemoteInvokeCulture(polandCulture);

            yield return ConvertTest.Valid(timeOnly, "10:30", CultureInfo.InvariantCulture);


            yield return ConvertTest.Valid(TimeOnly.MinValue, string.Empty);

            yield return ConvertTest.Valid(
                new TimeOnly(),
                new InstanceDescriptor(typeof(TimeOnly).GetConstructor(new Type[] { typeof(long) }), new object[] { (long)0 })
            );

            yield return ConvertTest.Valid(
                timeOnly,
                new InstanceDescriptor(
                    typeof(TimeOnly).GetConstructor(new Type[] { typeof(int), typeof(int), typeof(int), typeof(int), typeof(int) }),
                    new object[] { 10, 30, 50, 0, 0 }
                )
            );

            yield return ConvertTest.CantConvertTo(new TimeOnly(), typeof(TimeOnly));
            yield return ConvertTest.CantConvertTo(new TimeOnly(), typeof(int));
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
