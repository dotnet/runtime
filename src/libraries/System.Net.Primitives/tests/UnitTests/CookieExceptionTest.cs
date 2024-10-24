// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Net.Primitives.UnitTests.Tests;

public sealed class CookieExceptionTest
{
    [Fact]
    public void Constructor_Message()
    {
        var exception = new CookieException("Foo");
        Assert.Equal("Foo", exception.Message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Constructor_Message_InnerException(bool innerException)
    {
        var inner = innerException ? new Exception() : null;
        var exception = new CookieException("Foo", inner);

        Assert.Equal("Foo", exception.Message);
        Assert.Equal(inner, exception.InnerException);
    }
}
