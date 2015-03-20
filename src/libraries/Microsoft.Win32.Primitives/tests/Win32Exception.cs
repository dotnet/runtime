// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using Xunit;

namespace Microsoft.Win32.Primitives.Tests
{
    public static class Win32ExceptionTestType
    {
        [Fact]
        public static void InstantiateException()
        {
            int error = 5;
            string message = "This is an error message.";
            Exception innerException = new FormatException();

            // Test each of the constructors and validate the properties of the resulting instance

            Win32Exception ex = new Win32Exception();
            Assert.Equal(expected: E_FAIL, actual: ex.HResult);

            ex = new Win32Exception(error);
            Assert.Equal(expected: E_FAIL, actual: ex.HResult);
            Assert.Equal(expected: error, actual: ex.NativeErrorCode);

            ex = new Win32Exception(message);
            Assert.Equal(expected: E_FAIL, actual: ex.HResult);
            Assert.Equal(expected: message, actual: ex.Message);

            ex = new Win32Exception(error, message);
            Assert.Equal(expected: E_FAIL, actual: ex.HResult);
            Assert.Equal(expected: error, actual: ex.NativeErrorCode);
            Assert.Equal(expected: message, actual: ex.Message);

            ex = new Win32Exception(message, innerException);
            Assert.Equal(expected: E_FAIL, actual: ex.HResult);
            Assert.Equal(expected: message, actual: ex.Message);
            Assert.Same(expected: innerException, actual: ex.InnerException);
        }

        private const int E_FAIL = unchecked((int)0x80004005);

        [Fact]
        public static void InstantiateExceptionWithLongErrorString()
        {
            // This test checks that Win32Exception supports error strings greater than 256 characters.
            // Since we will have to rely on a message associated with an error code,
            // we try to reduce the flakiness by testing the following 3 scenarios:
            // 1. Validating the positive case, that an errorCode with a message string greater than 256 characters is supported.
            // 2. Validating that the message string retrieved is actually greater than 256 characters.
            // 3. Validating that the default error string is what we assume it is, by checking against an error code 
            //    that does not exist today.
            // If the corresponding errors or error strings in Windows change and invalidate these cases, this test will break,
            // and we can revise it accordingly.

            Win32Exception ex = new Win32Exception(0x268);
            if (ex.Message.Length > 256) // Message length for 0x268 is not > 256 characters in all cultures.
            {
                Assert.NotEqual("Unknown error (0x268)", ex.Message);
                Assert.True(ex.Message.Length > 256);

                ex = new Win32Exception(0x23);
                Assert.Equal(expected: "Unknown error (0x23)", actual: ex.Message);
            }
        }

    }
}
