// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

class ExactSpellingTest
{
    class Ansi
    {
        [DllImport("ExactSpellingNative", CharSet = CharSet.Ansi, ExactSpelling = true)]
        public static extern int Marshal_Int_InOut([In, Out] int intValue);

        [DllImport("ExactSpellingNative", CharSet = CharSet.Ansi, ExactSpelling = true)]
        public static extern int MarshalPointer_Int_InOut([In, Out] ref int intValue);

        [DllImport("ExactSpellingNative", CharSet = CharSet.Ansi, ExactSpelling = false)]
        public static extern int Marshal_Int_InOut2([In, Out] int intValue);

        [DllImport("ExactSpellingNative", CharSet = CharSet.Ansi, ExactSpelling = false)]
        public static extern int MarshalPointer_Int_InOut2([In, Out] ref int intValue);
    }

    class Unicode
    {
        [DllImport("ExactSpellingNative", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int Marshal_Int_InOut([In, Out] int intValue);

        [DllImport("ExactSpellingNative", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int MarshalPointer_Int_InOut([In, Out] ref int intValue);

        [DllImport("ExactSpellingNative", CharSet = CharSet.Unicode, ExactSpelling = false)]
        public static extern int Marshal_Int_InOut2([In, Out] int intValue);

        [DllImport("ExactSpellingNative", CharSet = CharSet.Unicode, ExactSpelling = false)]
        public static extern int MarshalPointer_Int_InOut2([In, Out] ref int intValue);
    }

    class Auto
    {
        [DllImport("ExactSpellingNative", CharSet = CharSet.Auto, ExactSpelling = false)]
        public static extern int Marshal_Int_InOut2([In, Out] int intValue);
    }

    public static int Main(string[] args)
    {
        int failures = 0;
        int intManaged = 1000;
        int intNative = 2000;
        int intReturn = 3000;
        
        Console.WriteLine("Method Unicode.Marshal_Int_InOut: ExactSpelling = true");
        int int1 = intManaged;
        int intRet1 = Unicode.Marshal_Int_InOut(int1);
        failures += Verify(intReturn, intManaged, intRet1, int1);
        
        Console.WriteLine("Method Unicode.MarshalPointer_Int_InOut: ExactSpelling = true");
        int int2 = intManaged;
        int intRet2 = Unicode.MarshalPointer_Int_InOut(ref int2);
        
        failures += Verify(intReturn, intNative, intRet2, int2);

        Console.WriteLine("Method Ansi.Marshal_Int_InOut: ExactSpelling = true");
        int int3 = intManaged;
        int intRet3 = Ansi.Marshal_Int_InOut(int3);
        failures += Verify(intReturn, intManaged, intRet3, int3);

        Console.WriteLine("Method Ansi.MarshalPointer_Int_InOut: ExactSpelling = true");
        int int4 = intManaged;
        int intRet4 = Ansi.MarshalPointer_Int_InOut(ref int4);
        failures += Verify(intReturn, intNative, intRet4, int4);

        int intReturnAnsi = 4000;
        int intReturnUnicode = 5000;

        Console.WriteLine("Method Unicode.Marshal_Int_InOut2: ExactSpelling = false");
        int int5 = intManaged;
        int intRet5 = Unicode.Marshal_Int_InOut2(int5);
        failures += Verify(intReturnUnicode, intManaged, intRet5, int5);
        
        Console.WriteLine("Method Unicode.MarshalPointer_Int_InOut2: ExactSpelling = false");
        int int6 = intManaged;
        int intRet6 = Unicode.MarshalPointer_Int_InOut2(ref int6);
        failures += Verify(intReturnUnicode, intNative, intRet6, int6);

        Console.WriteLine("Method Ansi.Marshal_Int_InOut2: ExactSpelling = false");
        int int7 = intManaged;
        int intRet7 = Ansi.Marshal_Int_InOut2(int7);
        failures += Verify(intReturnAnsi, intManaged, intRet7, int7);

        Console.WriteLine("Method Ansi.MarshalPointer_Int_InOut2: ExactSpelling = false");
        int int8 = intManaged;
        int intRet8 = Ansi.MarshalPointer_Int_InOut2(ref int8);
        failures += Verify(intReturnAnsi, intNative, intRet8, int8);

        Console.WriteLine("Method Auto.Marshal_Int_InOut: ExactSpelling = false. Verify CharSet.Auto behavior per-platform.");
        int int9 = intManaged;
        int intRet9 = Auto.Marshal_Int_InOut2(int9);
#if PLATFORM_WINDOWS
        failures += Verify(intReturnUnicode, intManaged, intRet9, int9);
#else
        failures += Verify(intReturnAnsi, intManaged, intRet9, int9);
#endif
        
        return 100 + failures;
    }

    private static int Verify(int expectedReturnValue, int expectedParameterValue, int actualReturnValue, int actualParameterValue)
    {
        int failures = 0;
        if (expectedReturnValue != actualReturnValue)
        {
            failures++;
            Console.WriteLine($"The return value is wrong. Expected {expectedReturnValue}, got {actualReturnValue}");
        }
        if (expectedParameterValue != actualParameterValue)
        {
            failures++;
            Console.WriteLine($"The parameter value is changed. Expected {expectedParameterValue}, got {actualParameterValue}");
        }

        return failures;
    }
}
