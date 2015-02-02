// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

public static class MethodAccessExceptionTests
{
    internal const int COR_E_METHODACCESS = unchecked((int)0x80131510);
    internal const int COR_E_FIELDACCESS = unchecked((int)0x80131507);

    [Fact]
    public static void MethodAccessException_Ctor1()
    {
        MethodAccessException i = new MethodAccessException();
        //Assert.Equal("Attempted to access a missing method.", i.Message, "TestCtor1_001 failed");
        Assert.Equal(COR_E_METHODACCESS, i.HResult);
    }

    [Fact]
    public static void MethodAccessException_Ctor2()
    {
        MethodAccessException i = new MethodAccessException("Created MethodAccessException");

        Assert.Equal("Created MethodAccessException", i.Message);
        Assert.Equal(COR_E_METHODACCESS, i.HResult);
    }

    [Fact]
    public static void MethodAccessException_Ctor3()
    {
        Exception ex = new Exception("Created inner exception");
        MethodAccessException i = new MethodAccessException("Created MethodAccessException", ex);

        Assert.Equal("Created MethodAccessException", i.Message);
        Assert.Equal(COR_E_METHODACCESS, i.HResult);
        Assert.Equal(i.InnerException.Message, "Created inner exception");
        Assert.Equal(i.InnerException.HResult, ex.HResult);
    }
}