// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

using Xunit;

namespace System.Net.Http.Functional.Tests
{
    public class HttpMethodTest
    {
        private static readonly object[][] s_staticHttpMethods =
        {
            new object[] { HttpMethod.Get },
            new object[] { HttpMethod.Put },
            new object[] { HttpMethod.Post },
            new object[] { HttpMethod.Delete },
            new object[] { HttpMethod.Head },
            new object[] { HttpMethod.Options },
            new object[] { HttpMethod.Trace }
        };

        public static IEnumerable<object[]> StaticHttpMethods { get { return s_staticHttpMethods; } }

        [Fact]
        public void StaticProperties_VerifyValues_PropertyNameMatchesHttpMethodName()
        {
            Assert.Equal("GET", HttpMethod.Get.Method);
            Assert.Equal("PUT", HttpMethod.Put.Method);
            Assert.Equal("POST", HttpMethod.Post.Method);
            Assert.Equal("DELETE", HttpMethod.Delete.Method);
            Assert.Equal("HEAD", HttpMethod.Head.Method);
            Assert.Equal("OPTIONS", HttpMethod.Options.Method);
            Assert.Equal("TRACE", HttpMethod.Trace.Method);
        }

        [Fact]
        public void Ctor_ValidMethodToken_Success()
        {
            new HttpMethod("GET");
            new HttpMethod("custom");

            // Note that '!' is the first ASCII char after CTLs and '~' is the last character before DEL char.
            new HttpMethod("validtoken!#$%&'*+-.0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz^_`|~");
        }

        [Fact]
        public void Ctor_NullMethod_Exception()
        {
            Assert.Throws<ArgumentException>(() => { new HttpMethod(null); } );
        }

        // TODO: This should be a [Theory]
        [Fact]
        public void Ctor_SeparatorInMethod_Exception()
        {
            char[] separators = new char[] { '(', ')', '<', '>', '@', ',', ';', ':', '\\', '"', '/', '[', ']', 
                '?', '=', '{', '}', ' ', '\t' };

            for (int i = 0; i < separators.Length; i++)
            {
                Assert.Throws<FormatException>(() => { new HttpMethod("Get" + separators[i]); } );
            }
        }

        [Fact]
        public void Equals_DifferentComparisonMethodsForSameMethods_MethodsConsideredEqual()
        {
            // Positive test cases
            Assert.True(new HttpMethod("GET") == HttpMethod.Get);
            Assert.False(new HttpMethod("GET") != HttpMethod.Get);
            Assert.True((new HttpMethod("GET")).Equals(HttpMethod.Get)); 

            Assert.True(new HttpMethod("get") == HttpMethod.Get);
            Assert.False(new HttpMethod("get") != HttpMethod.Get);
            Assert.True((new HttpMethod("get")).Equals(HttpMethod.Get));
        }

        [Fact]
        public void Equals_CompareWithMethodCastedToObject_ReturnsTrue()
        {
            object other = new HttpMethod("GET");
            Assert.True(HttpMethod.Get.Equals(other));
            Assert.False(HttpMethod.Get.Equals("GET"));
        }

        [Fact]
        public void Equals_NullComparand_ReturnsFalse()
        {
            Assert.False(null == HttpMethod.Options);
            Assert.False(HttpMethod.Trace == null);
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("get")]
        [InlineData("Get")]
        [InlineData("CUSTOM")]
        [InlineData("cUsToM")]
        public void GetHashCode_CustomStringMethod_SameAsStringToUpperInvariantHashCode(string input)
        {
            HttpMethod method = new HttpMethod(input);
            Assert.Equal(input.ToUpperInvariant().GetHashCode(), method.GetHashCode());
        }

        [Theory]
        [MemberData(nameof(StaticHttpMethods))]
        public void GetHashCode_StaticMethods_SameAsStringToUpperInvariantHashCode(HttpMethod method)
        {
            Assert.Equal(method.ToString().ToUpperInvariant().GetHashCode(), method.GetHashCode());
        }

        [Fact]
        public void GetHashCode_DifferentlyCasedMethod_SameHashCode()
        {
            string input = "GeT";
            HttpMethod method = new HttpMethod(input);
            Assert.Equal(HttpMethod.Get.GetHashCode(), method.GetHashCode());
        }

        [Fact]
        public void ToString_UseCustomStringMethod_SameAsString()
        {
            string custom = "custom";
            HttpMethod method = new HttpMethod(custom);
            Assert.Equal(custom, method.ToString());
        }

        [Fact]
        public void Method_AccessProperty_MatchesCtorString()
        {
            HttpMethod method = new HttpMethod("custom");
            Assert.Equal("custom", method.Method);
        }
    }
}
