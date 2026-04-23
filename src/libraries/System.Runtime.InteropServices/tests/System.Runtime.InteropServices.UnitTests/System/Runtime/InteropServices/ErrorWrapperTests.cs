// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
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

        [Fact]
        public void Ctor_NullException()
        {
            var wrapper = new ErrorWrapper((Exception)null);
            Assert.Equal(0, wrapper.ErrorCode);
        }

        [Fact]
        public void Ctor_ExceptionWithHResult()
        {
            var exception = new SubException();
            exception.SetHrResult(1000);
            var wrapper = new ErrorWrapper(exception);
            Assert.Equal(1000, wrapper.ErrorCode);
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
}
