// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Threading.Threads.Tests
{
    public static class ExceptionTests
    {
        private const int ThreadInterruptedException_HResult = unchecked((int)0x80131519);
        private const int ThreadStateException_HResult = unchecked((int)0x80131520);

        private static void ConstructorTest<T>(
            int expectedHResult,
            Func<T> createDefault,
            Func<string, T> createWithMessage,
            Func<string, Exception, T> createWithMessageAndException)
            where T : Exception
        {
            var ex = createDefault();
            Assert.False(string.IsNullOrEmpty(ex.Message));
            Assert.Null(ex.InnerException);
            Assert.Equal(expectedHResult, ex.HResult);

            var message = "foo";
            ex = createWithMessage(message);
            Assert.Equal(message, ex.Message);
            Assert.Null(ex.InnerException);
            Assert.Equal(expectedHResult, ex.HResult);

            var innerException = new Exception();
            ex = createWithMessageAndException(message, innerException);
            Assert.Equal(message, ex.Message);
            Assert.Equal(innerException, ex.InnerException);
            Assert.Equal(expectedHResult, ex.HResult);
        }

        [Fact]
        public static void ThreadInterruptedException_ConstructorTest()
        {
            ConstructorTest<ThreadInterruptedException>(
                ThreadInterruptedException_HResult,
                () => new ThreadInterruptedException(),
                message => new ThreadInterruptedException(message),
                (message, innerException) => new ThreadInterruptedException(message, innerException));
        }

        [Fact]
        public static void ThreadStateException_ConstructorTest()
        {
            ConstructorTest<ThreadStateException>(
                ThreadStateException_HResult,
                () => new ThreadStateException(),
                message => new ThreadStateException(message),
                (message, innerException) => new ThreadStateException(message, innerException));
        }
    }
}
