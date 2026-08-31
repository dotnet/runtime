// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Data.Common.Tests
{
    public class DbExceptionTests
    {
        [Fact]
        public void Ctor_Parameterless_UsesDefaults()
        {
            var defaultException = new CustomDbException();
            Assert.False(string.IsNullOrWhiteSpace(defaultException.Message));
            Assert.Null(defaultException.InnerException);
            Assert.Equal(-2147467259, defaultException.ErrorCode);
        }

        [Fact]
        public void Ctor_ArgumentsRoundtrip()
        {
            var e = new CustomDbException("test");
            Assert.Equal("test", e.Message);

            var innerException = new Exception();
            e = new CustomDbException("test", innerException);
            Assert.Equal("test", e.Message);
            Assert.Same(innerException, e.InnerException);

            e = new CustomDbException("test", 4060);
            Assert.Equal("test", e.Message);
            Assert.Equal(4060, e.ErrorCode);
        }

        [Fact]
        public void IsTransient_is_false_by_default()
            => Assert.False(new CustomDbException().IsTransient);

        [Fact]
        public void SqlState_is_null_by_default()
            => Assert.Null(new CustomDbException().SqlState);

        private class CustomDbException : DbException
        {
            public CustomDbException() { }
            public CustomDbException(string message) : base(message) { }
            public CustomDbException(string message, int errorCode) : base(message, errorCode) { }
            public CustomDbException(string message, Exception innerException) : base(message, innerException) { }
        }
    }
}
