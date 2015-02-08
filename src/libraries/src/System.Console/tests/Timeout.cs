// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

//
// System.Console BCL test cases
//
public class TimeOut
{
    [Fact]
    public static void OpenStandardXXX_WriteTimeOut()
    {
        using (Stream standardOut = Console.OpenStandardOutput(), standardIn = Console.OpenStandardInput(), standardError = Console.OpenStandardError())
        {
            Assert.Throws<InvalidOperationException>(() => standardOut.WriteTimeout);
            Assert.Throws<InvalidOperationException>(() => standardIn.WriteTimeout);
            Assert.Throws<InvalidOperationException>(() => standardError.WriteTimeout);

            Assert.Throws<InvalidOperationException>(() => standardOut.WriteTimeout = 500);
            Assert.Throws<InvalidOperationException>(() => standardIn.WriteTimeout = 500);
            Assert.Throws<InvalidOperationException>(() => standardError.WriteTimeout = 500);
        }
    }

    [Fact]
    public static void OpenStandardXXX_ReadTimeOut()
    {
        using (Stream standardOut = Console.OpenStandardOutput(), standardIn = Console.OpenStandardInput(), standardError = Console.OpenStandardError())
        {
            Assert.Throws<InvalidOperationException>(() => standardOut.ReadTimeout);
            Assert.Throws<InvalidOperationException>(() => standardIn.ReadTimeout);
            Assert.Throws<InvalidOperationException>(() => standardError.ReadTimeout);

            Assert.Throws<InvalidOperationException>(() => standardOut.ReadTimeout = 500);
            Assert.Throws<InvalidOperationException>(() => standardIn.ReadTimeout = 500);
            Assert.Throws<InvalidOperationException>(() => standardError.ReadTimeout = 500);
        }
    }

    [Fact]
    public static void OpenStandardXXX_CanTimeOut()
    {
        using (Stream standardOut = Console.OpenStandardOutput(), standardIn = Console.OpenStandardInput(), standardError = Console.OpenStandardError())
        {
            Assert.False(standardOut.CanTimeout);
            Assert.False(standardIn.CanTimeout);
            Assert.False(standardError.CanTimeout);
        }
    }
}
