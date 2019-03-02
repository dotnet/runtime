// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static class SpanTests
    {
        [Fact]
        public static void NullObjectInputFail()
        {
            Assert.Throws<ArgumentNullException>(() => JsonSerializer.Parse<string>((ReadOnlySpan<byte>)null));
        }

        [Theory]
        [MemberData(nameof(ReadSuccessCases))]
        public static void Read(Type classType, byte[] data)
        {
            object obj = JsonSerializer.Parse(data, classType);
            Assert.IsAssignableFrom(typeof(ITestClass), obj);
            ((ITestClass)obj).Verify();
        }

        [Fact]
        public static void ReadGenericApi()
        {
            SimpleTestClass obj = JsonSerializer.Parse<SimpleTestClass>(SimpleTestClass.s_data);
            obj.Verify();
        }

        [Fact]
        public static void VerifyValueFail()
        {
            Assert.Throws<ArgumentNullException>(() => JsonSerializer.ToString(new object(), (Type)null));
        }

        [Fact]
        public static void VerifyTypeFail()
        {
            Assert.Throws<ArgumentException>(() => JsonSerializer.ToString(1, typeof(string)));
        }

        [Fact]
        public static void NullObjectOutput()
        {
            byte[] encodedNull = Encoding.UTF8.GetBytes(@"null");

            {
                byte[] output = JsonSerializer.ToBytes(null, null);
                Assert.Equal(encodedNull, output);
            }

            {
                byte[] output = JsonSerializer.ToBytes(null, typeof(NullTests));
                Assert.Equal(encodedNull, output);
            }
        }

        public static IEnumerable<object[]> ReadSuccessCases
        {
            get
            {
                return TestData.ReadSuccessCases;
            }
        }
    }
}
