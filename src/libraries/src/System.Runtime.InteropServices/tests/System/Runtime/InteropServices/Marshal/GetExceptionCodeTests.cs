// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

#pragma warning disable CS0618 // Type or member is obsolete

namespace System.Runtime.InteropServices.Tests
{
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

        [Theory]
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
