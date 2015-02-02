// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

public static class MissingFieldExceptionTests
{
    internal const int COR_E_MISSINGMETHOD = unchecked((int)0x80131513);
    internal const int COR_E_MISSINGFIELD = unchecked((int)0x80131511);

    [Fact]
    public static void MissingFieldException_Ctor1()
    {
        MissingFieldException i = new MissingFieldException();
        //Assert.Equal("Attempted to access a non-existing field.", i.Message, "TestCtor1_001 failed");
        Assert.Equal(COR_E_MISSINGFIELD, i.HResult);
    }

    [Fact]
    public static void MissingFieldException_Ctor2()
    {
        MissingFieldException i = new MissingFieldException("Created MissingFieldException");

        Assert.Equal("Created MissingFieldException", i.Message);
        Assert.Equal(COR_E_MISSINGFIELD, i.HResult);
    }

    [Fact]
    public static void MissingFieldException_Ctor3()
    {
        Exception ex = new Exception("Created inner exception");
        MissingFieldException i = new MissingFieldException("Created MissingFieldException", ex);

        Assert.Equal("Created MissingFieldException", i.Message);
        Assert.Equal(COR_E_MISSINGFIELD, i.HResult);
        Assert.Equal(i.InnerException.Message, "Created inner exception");
        Assert.Equal(i.InnerException.HResult, ex.HResult);
    }
}