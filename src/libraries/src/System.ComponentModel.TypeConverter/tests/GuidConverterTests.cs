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
    public class GuidConverterTests : ConverterTestBase
    {
        [Theory]
        [InlineData(typeof(string), true)]
        [InlineData(typeof(InstanceDescriptor), true)]
        [InlineData(typeof(Guid), false)]
        [InlineData(null, false)]
        public void CanConvertFrom_Invoke_ReturnsExpected(Type sourceType, bool expected)
        {
            var converter = new GuidConverter();
            Assert.Equal(expected, converter.CanConvertFrom(sourceType));
        }

        public static IEnumerable<object[]> ConvertFrom_TestData()
        {
            yield return new object[] { " {30da92c0-23e8-42a0-ae7c-734a0e5d2782}", new Guid(0x30da92c0, 0x23e8, 0x42a0, 0xae, 0x7c, 0x73, 0x4a, 0x0e, 0x5d, 0x27, 0x82) };
            yield return new object[] { "{30da92c0-23e8-42a0-ae7c-734a0e5d2782}", new Guid(0x30da92c0, 0x23e8, 0x42a0, 0xae, 0x7c, 0x73, 0x4a, 0x0e, 0x5d, 0x27, 0x82) };
            yield return new object[] { " \t\r\n {30da92c0-23e8-42a0-ae7c-734a0e5d2782} \t\r\n ", new Guid(0x30da92c0, 0x23e8, 0x42a0, 0xae, 0x7c, 0x73, 0x4a, 0x0e, 0x5d, 0x27, 0x82) };
        }

        [Theory]
        [MemberData(nameof(ConvertFrom_TestData))]
        public void ConvertFrom_Invoke_ReturnsExpected(object value, object expected)
        {
            var converter = new GuidConverter();
            Assert.Equal(expected, converter.ConvertFrom(value));
        }

        [Theory]
        [InlineData("")]
        [InlineData("invalid")]
        public void ConvertFrom_InvalidString_ThrowsFormatException(string value)
        {
            var converter = new GuidConverter();
            Assert.Throws<FormatException>(() => converter.ConvertFrom(value));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(1)]
        public void ConvertFrom_InvalidValue_ThrowsNotSupportedException(object value)
        {
            var converter = new GuidConverter();
            Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(value));
        }

        [Theory]
        [InlineData(typeof(string), true)]
        [InlineData(typeof(InstanceDescriptor), true)]
        [InlineData(typeof(Guid), false)]
        [InlineData(null, false)]
        public void CanConvertTo_Invoke_ReturnsExpected(Type destinationType, bool expected)
        {
            var converter = new GuidConverter();
            Assert.Equal(expected, converter.CanConvertTo(destinationType));
        }

        [Fact]
        public void ConvertTo_String_ReturnsExpected()
        {
            var converter = new GuidConverter();
            var value = new Guid(0x30da92c0, 0x23e8, 0x42a0, 0xae, 0x7c, 0x73, 0x4a, 0x0e, 0x5d, 0x27, 0x82);
            Assert.Equal("30da92c0-23e8-42a0-ae7c-734a0e5d2782", converter.ConvertTo(value, typeof(string)));
        }

        [Fact]
        public void ConvertTo_InstanceDescriptor_ReturnsExpected()
        {
            var converter = new GuidConverter();
            var value = new Guid(0x30da92c0, 0x23e8, 0x42a0, 0xae, 0x7c, 0x73, 0x4a, 0x0e, 0x5d, 0x27, 0x82);
            InstanceDescriptor descriptor = Assert.IsType<InstanceDescriptor>(converter.ConvertTo(value, typeof(InstanceDescriptor)));
            ConstructorInfo constructor = Assert.IsAssignableFrom<ConstructorInfo>(descriptor.MemberInfo);
            Assert.Equal(new Type[] { typeof(string) }, constructor.GetParameters().Select(p => p.ParameterType));
            Assert.Equal(new object[] { "30da92c0-23e8-42a0-ae7c-734a0e5d2782" }, descriptor.Arguments);
        }

        [Theory]
        [InlineData(typeof(InstanceDescriptor))]
        [InlineData(typeof(int))]
        public void ConvertTo_InvalidValue_ThrowsNotSupportedException(Type destinationType)
        {
            var converter = new GuidConverter();
            Assert.Throws<NotSupportedException>(() => converter.ConvertTo(new object(), destinationType));
        }
    }
}
