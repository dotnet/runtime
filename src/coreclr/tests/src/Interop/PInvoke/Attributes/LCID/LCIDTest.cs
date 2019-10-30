// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using TestLibrary;

class LCIDNative
{
    [DllImport(nameof(LCIDNative), CharSet = CharSet.Unicode)]
    [LCIDConversion(1)]
    public static extern void ReverseString(string str, out string result);

    [DllImport(nameof(LCIDNative), CharSet = CharSet.Unicode)]
    [LCIDConversion(0)]
    public static extern bool VerifyValidLCIDPassed(int lcid);
}

class LCIDTest
{
    private static string Reverse(string s)
    {
        var chars = s.ToCharArray();
        Array.Reverse(chars);
        return new string(chars);
    }

    public static int Main()
    {
        try
        {
            string testString = "Test string";
            LCIDNative.ReverseString(testString, out string reversed);
            Assert.AreEqual(Reverse(testString), reversed);
            CultureInfo originalCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo spanishCulture = new CultureInfo("es-ES", false);
                CultureInfo.CurrentCulture = spanishCulture;
                Assert.IsTrue(LCIDNative.VerifyValidLCIDPassed(CultureInfo.CurrentCulture.LCID));
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
