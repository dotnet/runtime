// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace System.ComponentModel.Tests
{
    public static partial class Win32ExceptionTestType
    {
        private const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
        private const int FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;
        private const int FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000;
        private const int ERROR_INSUFFICIENT_BUFFER = 0x7A;
        private const int FirstPassBufferSize = 256;
        private const int E_FAIL = unchecked((int)0x80004005);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "FormatMessageW", SetLastError = true, BestFitMapping = true)]
        private static extern int FormatMessage(
            int dwFlags,
            IntPtr lpSource_mustBeNull,
            uint dwMessageId,
            int dwLanguageId,
            StringBuilder lpBuffer,
            int nSize,
            IntPtr[] arguments);

        private static bool IsExceptionMessageLong(int errorCode)
        {
            StringBuilder sb = new StringBuilder(FirstPassBufferSize); // Buffer length in the first pass in the implementation.

            int result = FormatMessage(FORMAT_MESSAGE_IGNORE_INSERTS |
                                       FORMAT_MESSAGE_FROM_SYSTEM |
                                       FORMAT_MESSAGE_ARGUMENT_ARRAY,
                                       IntPtr.Zero, (uint)errorCode, 0, sb, sb.Capacity,
                                       null);
            if (result == 0)
            {
                return (Marshal.GetLastWin32Error() == ERROR_INSUFFICIENT_BUFFER);
            }

            return false;
        }

        [Fact]
        public static void InstantiateException()
        {
            int error = 5;
            string message = "This is an error message.";
            string toStringStart = string.Format(
                CultureInfo.InvariantCulture,
                "{0} ({1})",
                typeof(Win32Exception).ToString(),
                PlatformDetection.IsFullFramework ? $"0x{E_FAIL:X8}" : error.ToString(CultureInfo.InvariantCulture));

            Exception innerException = new FormatException();

            // Test each of the constructors and validate the properties of the resulting instance

            Win32Exception ex = new Win32Exception();
            Assert.Equal(expected: E_FAIL, actual: ex.HResult);

            ex = new Win32Exception(error);
            Assert.Equal(expected: E_FAIL, actual: ex.HResult);
            Assert.Equal(expected: error, actual: ex.NativeErrorCode);
            Assert.StartsWith(expectedStartString: toStringStart, actualString: ex.ToString(), comparisonType: StringComparison.Ordinal);

            ex = new Win32Exception(message);
            Assert.Equal(expected: E_FAIL, actual: ex.HResult);
            Assert.Equal(expected: message, actual: ex.Message);

            ex = new Win32Exception(error, message);
            Assert.Equal(expected: E_FAIL, actual: ex.HResult);
            Assert.Equal(expected: error, actual: ex.NativeErrorCode);
            Assert.Equal(expected: message, actual: ex.Message);
            Assert.StartsWith(expectedStartString: toStringStart, actualString: ex.ToString(), comparisonType: StringComparison.Ordinal);

            ex = new Win32Exception(message, innerException);
            Assert.Equal(expected: E_FAIL, actual: ex.HResult);
            Assert.Equal(expected: message, actual: ex.Message);
            Assert.Same(expected: innerException, actual: ex.InnerException);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Uses P/Invokes to check whether the exception resource length >256 chars
        public static void InstantiateExceptionWithLongErrorString()
        {
            // This test checks that Win32Exception supports error strings greater than 256 characters.
            // Since we will have to rely on a message associated with an error code,
            // we try to reduce the flakiness by doing the following.
            // 1. Call FormatMessage to check whether the exception resource length >256 chars.
            // 2. If true, we validate that Win32Exception class can retrieve the complete resource string.
            // 3. If not we skip testing.
            int errorCode = 0x268;
            if (IsExceptionMessageLong(errorCode)) // Localized error string for 0x268 is not guaranteed to be >256 chars. 
            {
                Win32Exception ex = new Win32Exception(errorCode);
                Assert.NotEqual("Unknown error (0x268)", ex.Message);
                Assert.True(ex.Message.Length > FirstPassBufferSize);

                ex = new Win32Exception(0x23);
                Assert.Equal(expected: "Unknown error (0x23)", actual: ex.Message);
            }
        }
    }
}
