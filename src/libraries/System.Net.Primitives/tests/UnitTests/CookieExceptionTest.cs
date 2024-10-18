// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Net.Primitives.UnitTests.Tests;

public sealed class CookieExceptionTest
{
    [Theory]
    [InlineData("Testing the CookieException")]
    [InlineData(null)]
    public void Constructor_Message(string? message)
    {
        var exception = new CookieException(message);
        Assert.Equal(message, exception.Message);
    }

    [Theory]
    [InlineData("Testing the CookieException", true)]
    [InlineData("Testing the CookieException", false)]
    [InlineData(null, true)]
    [InlineData(null, false)]
    public void Constructor_Message_InnerException(string? message, bool innerException)
    {
        var inner = innerException ? new Exception() : null;
        var exception = new CookieException(message, inner);

        Assert.Equal(message, exception.Message);
        Assert.Equal(inner, exception.InnerException);
    }
}
