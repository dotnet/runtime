// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class GetExceptionForHRTests
    {
        [Theory]
        [InlineData(unchecked((int)0x80020006))]
        [InlineData(unchecked((int)0x80020101))]
        public void GetExceptionForHR_NoErrorInfo_ReturnsValidException(int errorCode)
        {
            ClearCurrentIErrorInfo();

            Exception ex = Assert.IsType<COMException>(Marshal.GetExceptionForHR(errorCode));
            Assert.Equal(errorCode, ex.HResult);
            Assert.Null(ex.InnerException);
            Assert.Null(ex.HelpLink);
            Assert.NotEmpty(ex.Message);
            Assert.Null(ex.Source);
            Assert.Null(ex.StackTrace);
            Assert.Null(ex.TargetSite);
        }

        public static IEnumerable<object[]> GetExceptionForHR_ErrorInfo_TestData()
        {
            yield return new object[] { unchecked((int)0x80020006), IntPtr.Zero };
            yield return new object[] { unchecked((int)0x80020101), IntPtr.Zero };
            yield return new object[] { unchecked((int)0x80020006), (IntPtr)(-1) };
            yield return new object[] { unchecked((int)0x80020101), (IntPtr)(-1) };
        }

        [Theory]
        [MemberData(nameof(GetExceptionForHR_ErrorInfo_TestData))]
        public void GetExceptionForHR_ErrorInfo_ReturnsValidException(int errorCode, IntPtr errorInfo)
        {
            ClearCurrentIErrorInfo();

            Exception ex = Assert.IsType<COMException>(Marshal.GetExceptionForHR(errorCode, errorInfo));
            Assert.Equal(errorCode, ex.HResult);
            Assert.Null(ex.InnerException);
            Assert.Null(ex.HelpLink);
            Assert.NotEmpty(ex.Message);
            Assert.Null(ex.Source);
            Assert.Null(ex.StackTrace);
            Assert.Null(ex.TargetSite);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void GetExceptionForHR_ThrowExceptionForHR_ThrowsSameException()
        {
            const int ErrorCode = unchecked((int)0x80131D0B);

            ClearCurrentIErrorInfo();
            var getHRException = (COMException)Marshal.GetExceptionForHR(ErrorCode);
            Assert.Equal(ErrorCode, getHRException.HResult);
            try
            {
                Marshal.ThrowExceptionForHR(ErrorCode);
            }
            catch (COMException e)
            {
                Assert.Equal(ErrorCode, e.HResult);
                Assert.Equal(e.HResult, getHRException.HResult);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void GetExceptionForHR_InvalidHR_ReturnsNull(int errorCode)
        {
            Assert.Null(Marshal.GetExceptionForHR(errorCode));
            Assert.Null(Marshal.GetExceptionForHR(errorCode, IntPtr.Zero));
        }

        private static void ClearCurrentIErrorInfo()
        {
            // Ensure that if the thread's current IErrorInfo
            // is set during a run that it is thrown away prior
            // to interpreting the HRESULT.
            Marshal.GetExceptionForHR(unchecked((int)0x80040001));
        }
    }
}
