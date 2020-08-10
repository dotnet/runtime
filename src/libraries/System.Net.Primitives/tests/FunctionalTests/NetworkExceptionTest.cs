// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Net.Primitives.Functional.Tests
{
    public static class NetworkExceptionTest
    {
        [Fact]
        public static void Create_AllErrorCodes_Success()
        {
            foreach (NetworkError error in Enum.GetValues(typeof(NetworkError)))
            {
                NetworkException e = new NetworkException(error);
                Assert.Equal(error, e.NetworkError);
                Assert.Null(e.InnerException);
                Assert.NotNull(e.Message);
            }
        }

        [Fact]
        public static void Create_InnerExceptionAndMessage_Success()
        {
            const string Message = "Hello";
            Exception inner = new Exception();

            NetworkException e = new NetworkException(Message, NetworkError.Unknown, inner);

            Assert.Equal(inner, e.InnerException);
            Assert.Equal(Message, e.Message);
        }
    }
}
