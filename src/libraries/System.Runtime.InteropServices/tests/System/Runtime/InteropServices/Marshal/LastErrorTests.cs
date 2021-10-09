// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class LastErrorTests
    {
        [Fact]
        public void LastPInvokeError_RoundTrip()
        {
            int errorExpected = 123;
            Marshal.SetLastPInvokeError(errorExpected);
            Assert.Equal(errorExpected, Marshal.GetLastPInvokeError());
        }

        [Fact]
        public void LastSystemError_RoundTrip()
        {
            int errorExpected = 123;
            Marshal.SetLastSystemError(errorExpected);
            Assert.Equal(errorExpected, Marshal.GetLastSystemError());
        }

        [Fact]
        public void SetLastPInvokeError_GetLastWin32Error()
        {
            int errorExpected = 123;
            Marshal.SetLastPInvokeError(errorExpected);
            Assert.Equal(errorExpected, Marshal.GetLastWin32Error());
        }

        [Fact]
        public void SetLastSystemError_PInvokeErrorUnchanged()
        {
            int pinvokeError = 123;
            Marshal.SetLastPInvokeError(pinvokeError);

            int systemError = pinvokeError + 1;
            Marshal.SetLastSystemError(systemError);

            // Setting last system error should not affect the last P/Invoke error
            int pinvokeActual = Marshal.GetLastPInvokeError();
            Assert.NotEqual(systemError, pinvokeActual);
            Assert.Equal(pinvokeError, pinvokeActual);

            int win32Actual = Marshal.GetLastWin32Error();
            Assert.NotEqual(systemError, win32Actual);
            Assert.Equal(pinvokeError, win32Actual);
        }
    }
}
