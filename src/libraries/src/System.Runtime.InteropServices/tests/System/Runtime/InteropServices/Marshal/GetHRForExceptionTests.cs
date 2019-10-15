// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class GetHRForExceptionTests
    {
        [Fact]
        public void GetHRForException_ValidException_ReturnsValidHResult()
        {
            var e = new Exception();

            try
            {
                Assert.InRange(Marshal.GetHRForException(e), int.MinValue, -1);
                Assert.Equal(e.HResult, Marshal.GetHRForException(e));
            }
            finally
            {
                // This GetExceptionForHR call is needed to 'eat' the IErrorInfo put to TLS by
                // Marshal.GetHRForException call above. Otherwise other Marshal.GetExceptionForHR
                // calls would randomly return previous exception objects passed to
                // Marshal.GetHRForException.
                // The correct way is to use Marshal.GetHRForException at interop boundary only, but for the
                // time being we'll keep this code as-is.
                Marshal.GetExceptionForHR(e.HResult);
            }
        }

        [Fact]
        public void GetHRForException_CustomException_ReturnsExpected()
        {
            var exception = new CustomHRException();
            Assert.Equal(10, Marshal.GetHRForException(exception));
        }

        public class CustomHRException : Exception
        {
            public CustomHRException() : base()
            {
                HResult = 10;
            }
        }

        [Fact]
        public void GetHRForException_NullException_ReturnsZero()
        {
            Assert.Equal(0, Marshal.GetHRForException(null));
        }
    }
}
