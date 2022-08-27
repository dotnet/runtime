// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

#pragma warning disable CS0618 // Type or member is obsolete

namespace System.Runtime.InteropServices.Tests
{
    [SkipOnMono("Marshal.GetExceptionCode will not be implemented in Mono, see https://github.com/mono/mono/issues/15085.")]
    public class GetExceptionCodeTests
    {
        [Fact]
        public void GetExceptionCode_NoException_ReturnsZero()
        {
            Assert.Equal(0, Marshal.GetExceptionCode());
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(10)]
        public void GetExceptionCode_NormalExceptionInsideCatch_ReturnsExpected(int hresult)
        {
            try
            {
                throw new HResultException(hresult);
            }
            catch
            {
                int exceptionCode = Marshal.GetExceptionCode();
                Assert.NotEqual(0, Marshal.GetExceptionCode());
                Assert.NotEqual(hresult, exceptionCode);
                Assert.Equal(exceptionCode, Marshal.GetExceptionCode());
            }

            Assert.Equal(0, Marshal.GetExceptionCode());
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNativeAot))]
        [InlineData(-1)]
        [InlineData(10)]
        public void GetExceptionCode_ComExceptionInsideCatch_ReturnsExpected(int errorCode)
        {
            try
            {
                throw new COMException("message", errorCode);
            }
            catch
            {
                int exceptionCode = Marshal.GetExceptionCode();
                Assert.NotEqual(0, Marshal.GetExceptionCode());
                Assert.NotEqual(errorCode, exceptionCode);
                Assert.Equal(exceptionCode, Marshal.GetExceptionCode());
            }

            Assert.Equal(0, Marshal.GetExceptionCode());
        }

        public class HResultException : Exception
        {
            public HResultException(int hresult) : base()
            {
                HResult = hresult;
            }
        }
    }
}

#pragma warning restore CS0618 // Type or member is obsolete
