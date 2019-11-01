// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class GuidConverterTests : TypeConverterTestBase
    {
        public override TypeConverter Converter => new GuidConverter();

        public override IEnumerable<ConvertTest> ConvertFromTestData()
        {
            yield return ConvertTest.Valid(" {30da92c0-23e8-42a0-ae7c-734a0e5d2782}", new Guid(0x30da92c0, 0x23e8, 0x42a0, 0xae, 0x7c, 0x73, 0x4a, 0x0e, 0x5d, 0x27, 0x82));
            yield return ConvertTest.Valid("{30da92c0-23e8-42a0-ae7c-734a0e5d2782}", new Guid(0x30da92c0, 0x23e8, 0x42a0, 0xae, 0x7c, 0x73, 0x4a, 0x0e, 0x5d, 0x27, 0x82));
            yield return ConvertTest.Valid(" \t\r\n {30da92c0-23e8-42a0-ae7c-734a0e5d2782} \t\r\n ", new Guid(0x30da92c0, 0x23e8, 0x42a0, 0xae, 0x7c, 0x73, 0x4a, 0x0e, 0x5d, 0x27, 0x82));

            yield return ConvertTest.Throws<FormatException>("");
            yield return ConvertTest.Throws<FormatException>("invalid");
            yield return ConvertTest.CantConvertFrom(1);
            yield return ConvertTest.CantConvertFrom(new object());
        }

        public override IEnumerable<ConvertTest> ConvertToTestData()
        {
            const string GuidString = "30da92c0-23e8-42a0-ae7c-734a0e5d2782";
            yield return ConvertTest.Valid(new Guid(0x30da92c0, 0x23e8, 0x42a0, 0xae, 0x7c, 0x73, 0x4a, 0x0e, 0x5d, 0x27, 0x82), GuidString);
            yield return ConvertTest.Valid(
                new Guid(0x30da92c0, 0x23e8, 0x42a0, 0xae, 0x7c, 0x73, 0x4a, 0x0e, 0x5d, 0x27, 0x82),
                new InstanceDescriptor(
                    typeof(Guid).GetConstructor(new Type[] { typeof(string) }),
                    new object[] { GuidString }
                )
            );
            yield return ConvertTest.CantConvertTo(new Guid(), typeof(Guid));
            yield return ConvertTest.CantConvertTo(new Guid(), typeof(int));
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
