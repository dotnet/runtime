// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Data.Tests
{
    public class DataExceptionTest
    {
        [Fact]
        public void Ctor_Parameterless_UsesDefaults()
        {
            var e = new CustomDataException();
            Assert.False(string.IsNullOrWhiteSpace(e.Message));
            Assert.Null(e.InnerException);
            Assert.Equal(-2146232032, e.HResult);
        }

        [Fact]
        public void Ctor_ArgumentsRoundtrip()
        {
            var innerException = new Exception("inner exception");

            var e = new CustomDataException("test");
            Assert.Equal("test", e.Message);
            Assert.Null(e.InnerException);
            Assert.Equal(-2146232032, e.HResult);

            e = new CustomDataException("test", innerException);
            Assert.Equal("test", e.Message);
            Assert.Same(innerException, e.InnerException);
            Assert.Equal(-2146233087, e.HResult);
        }

        private class CustomDataException : DataException
        {
            public CustomDataException() { }
            public CustomDataException(string message) : base(message) { }
            public CustomDataException(string message, Exception innerException) : base(message, innerException) { }
        }
    }
}
