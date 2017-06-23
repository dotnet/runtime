// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
#pragma warning disable 0618 // ErrorWrapper is marked as Obsolete.
    public class ErrorWrapperTests
    {
        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        public void Ctor_IntErrorCode(int value)
        {
            var wrapper = new ErrorWrapper(value);
            Assert.Equal(value, wrapper.ErrorCode);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        public void Ctor_ObjectErrorCode(object value)
        {
            var wrapper = new ErrorWrapper(value);
            Assert.Equal(value, wrapper.ErrorCode);
        }

        public static IEnumerable<object[]> Ctor_Exception_TestData()
        {
            yield return new object[] { null, 0 };

            var exception = new SubException();
            exception.SetHrResult(1000);
            yield return new object[] { exception, 1000 };
        }

        [Theory]
        [MemberData(nameof(Ctor_Exception_TestData))]
        public void Ctor_Exception(Exception exception, int expectedErrorCode)
        {
            var wrapper = new ErrorWrapper(exception);
            Assert.Equal(expectedErrorCode, wrapper.ErrorCode);
        }

        [Fact]
        public void Ctor_NonIntObjectValue_ThrowsInvalidCastException()
        {
            AssertExtensions.Throws<ArgumentException>("errorCode", () => new ErrorWrapper("1"));
        }

        public class SubException : Exception
        {
            public void SetHrResult(int hResult) => HResult = hResult;
        }
    }
#pragma warning restore 0618
}
