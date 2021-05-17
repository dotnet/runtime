// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;

//
// System.Console BCL test cases
//
public class TimeOut
{
    [Fact]
    [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "Not supported on Browser, iOS, or tvOS.")]
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
    [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "Not supported on Browser, iOS, or tvOS.")]
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
    [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "Not supported on Browser, iOS, or tvOS.")]
    public static void OpenStandardXXX_CanTimeOut()
    {
        using (Stream standardOut = Console.OpenStandardOutput(), standardIn = Console.OpenStandardInput(), standardError = Console.OpenStandardError())
        {
            Assert.False(standardOut.CanTimeout);
            Assert.False(standardIn.CanTimeout);
            Assert.False(standardError.CanTimeout);
        }
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Browser)]
    public static void OpenStandardInput_Throws_PlatformNotSupportedException()
    {
        Assert.Throws<PlatformNotSupportedException>(() => Console.OpenStandardInput());
    }
}
