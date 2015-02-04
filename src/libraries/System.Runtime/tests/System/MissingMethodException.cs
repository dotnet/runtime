// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

public static class MissingMethodExceptionTests
{
    internal const int COR_E_MISSINGMETHOD = unchecked((int)0x80131513);
    internal const int COR_E_MISSINGFIELD = unchecked((int)0x80131511);

    [Fact]
    public static void MissingMethodException_Ctor1()
    {
        MissingMethodException i = new MissingMethodException();
        //Assert.Equal("Attempted to access a missing method.", i.Message, "TestCtor1_001 failed");
        Assert.Equal(COR_E_MISSINGMETHOD, i.HResult);
    }

    [Fact]
    public static void MissingMethodException_Ctor2()
    {
        MissingMethodException i = new MissingMethodException("Created MissingMethodException");

        Assert.Equal("Created MissingMethodException", i.Message);
        Assert.Equal(COR_E_MISSINGMETHOD, i.HResult);
    }

    [Fact]
    public static void MissingMethodException_Ctor3()
    {
        Exception ex = new Exception("Created inner exception");
        MissingMethodException i = new MissingMethodException("Created MissingMethodException", ex);

        Assert.Equal("Created MissingMethodException", i.Message);
        Assert.Equal(COR_E_MISSINGMETHOD, i.HResult);
        Assert.Equal(i.InnerException.Message, "Created inner exception");
        Assert.Equal(i.InnerException.HResult, ex.HResult);
    }
}