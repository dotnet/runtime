// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Data.Tests
{
    public class DataExceptionTest
    {
        [Fact]
        public void Ctor_Parameterless_UsesDefaults()
        {
            const int COR_E_Data = unchecked((int)(0x80131920));
            var e = new CustomDataException();
            Assert.False(string.IsNullOrWhiteSpace(e.Message));
            Assert.Null(e.InnerException);
            Assert.Equal(COR_E_Data, e.HResult);
        }

        [Fact]
        public void Ctor_ArgumentsRoundtrip()
        {
            const int COR_E_SYSTEM = unchecked((int)0x80131501);
            const int COR_E_Data = unchecked((int)(0x80131920));
            var innerException = new Exception("inner exception");

            var e = new CustomDataException("test");
            Assert.Equal("test", e.Message);
            Assert.Null(e.InnerException);
            Assert.Equal(COR_E_Data, e.HResult);

            e = new CustomDataException("test", innerException);
            Assert.Equal("test", e.Message);
            Assert.Same(innerException, e.InnerException);
            Assert.Equal(COR_E_SYSTEM, e.HResult);
        }

        private class CustomDataException : DataException
        {
            public CustomDataException() { }
            public CustomDataException(string message) : base(message) { }
            public CustomDataException(string message, Exception innerException) : base(message, innerException) { }
        }
    }
}
