// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class StringTests
    {
        [Fact]
        public static void VerifyValueFail()
        {
            Assert.Throws<ArgumentNullException>(() => JsonSerializer.ToString("", (Type)null));
        }

        [Fact]
        public static void VerifyTypeFail()
        {
            Assert.Throws<ArgumentException>(() => JsonSerializer.ToString(1, typeof(string)));
        }

        [Fact]
        public static void NullObjectOutput()
        {
            {
                string output = JsonSerializer.ToString<string>(null);
                Assert.Equal("null", output);
            }

            {
                string output = JsonSerializer.ToString<string>(null, null);
                Assert.Equal("null", output);
            }
        }

        [Theory]
        [MemberData(nameof(WriteSuccessCases))]
        public static void Write(ITestClass testObj)
        {
            string json;

            {
                testObj.Initialize();
                testObj.Verify();
                json = JsonSerializer.ToString(testObj, testObj.GetType());
            }

            {
                ITestClass obj = (ITestClass)JsonSerializer.Parse(json, testObj.GetType());
                obj.Verify();
            }
        }

        public static IEnumerable<object[]> WriteSuccessCases
        {
            get
            {
                return TestData.WriteSuccessCases;
            }
        }
    }
}
