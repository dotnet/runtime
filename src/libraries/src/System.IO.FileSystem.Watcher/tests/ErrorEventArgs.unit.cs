// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

public class ErrorEventArgsTests
{
    private static void ValidateErrorEventArgs(Exception exception)
    {
        ErrorEventArgs args = new ErrorEventArgs(exception);

        Assert.Equal(exception, args.GetException());

        // Make sure method is consistent.
        Assert.Equal(exception, args.GetException());
    }

    [Fact]
    public static void ErrorEventArgs_ctor()
    {
        ValidateErrorEventArgs(null);

        ValidateErrorEventArgs(new Exception());
    }
}
