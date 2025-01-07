// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

class LCIDNative
{
    [DllImport(nameof(LCIDNative), CharSet = CharSet.Unicode)]
    [LCIDConversion(1)]
    public static extern void ReverseString(string str, out string result);

    [DllImport(nameof(LCIDNative), CharSet = CharSet.Unicode)]
    [LCIDConversion(0)]
    public static extern bool VerifyValidLCIDPassed(int lcid);
}

public class LCIDTest
{
    private static string Reverse(string s)
    {
        var chars = s.ToCharArray();
        Array.Reverse(chars);
        return new string(chars);
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)]
    [SkipOnMono("PInvoke LCIDConversionAttribute not supported on Mono")]
    [ActiveIssue("https://github.com/dotnet/runtimelab/issues/155", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
    public static int TestEntryPoint()
    {
        try
        {
            string testString = "Test string";
            LCIDNative.ReverseString(testString, out string reversed);
            Assert.Equal(Reverse(testString), reversed);
            CultureInfo originalCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo spanishCulture = new CultureInfo("es-ES", false);
                CultureInfo.CurrentCulture = spanishCulture;
                Assert.True(LCIDNative.VerifyValidLCIDPassed(CultureInfo.CurrentCulture.LCID));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }
        catch (System.Exception e)
        {
            Console.WriteLine(e.ToString());
            return 101;
        }
        return 100;
    }
}
