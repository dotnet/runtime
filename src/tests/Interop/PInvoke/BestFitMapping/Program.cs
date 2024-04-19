// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Runtime.InteropServices;
using TestLibrary;

using Xunit;

public class Program
{
    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)]
    [SkipOnMono("Mono doesn't support interop BestFitMapping and ThrowOnUnmappableChar attributes")]
    [ActiveIssue("https://github.com/dotnet/runtimelab/issues/155", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
    public static void TestEntryPoint()
    {
        if (System.Globalization.CultureInfo.CurrentCulture.Name != "en-US")
        {
            Console.WriteLine("Non-US English platforms are not supported.\nPassing without running tests");

            Console.WriteLine("--- Success");
            return;
        }

        Console.WriteLine("Validating char marshalling...");
        Char.PInvoke_Default.RunTest();
        Char.PInvoke_False_False.RunTest();
        Char.PInvoke_False_True.RunTest();
        Char.PInvoke_True_False.RunTest();
        Char.PInvoke_True_True.RunTest();

        Console.WriteLine("Validating LPStr marshalling...");
        LPStr.PInvoke_Default.RunTest();
        LPStr.PInvoke_False_False.RunTest();
        LPStr.PInvoke_False_True.RunTest();
        LPStr.PInvoke_True_False.RunTest();
        LPStr.PInvoke_True_True.RunTest();
    }
}
