// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

// Argument
using System;
namespace JitInliningTest
{
    class Args1
    {
        static string f1a(bool a) { return a.ToString(); }
        static string f1c(ref bool a) { return a.ToString(); }
        static void f1b(out bool a) { a = true; }

        static string f2a(char a) { return a.ToString(); }
        static string f2c(ref char a) { return a.ToString(); }
        static void f2b(out char a) { a = 'a'; }

        static string f3a(byte a) { return a.ToString(); }
        static string f3c(ref byte a) { return a.ToString(); }
        static void f3b(out byte a) { a = (byte)0; }

        static string f4a(short a) { return a.ToString(); }
        static string f4c(ref short a) { return a.ToString(); }
        static void f4b(out short a) { a = -1; }

        static string f5a(int a) { return a.ToString(); }
        static string f5c(ref int a) { return a.ToString(); }
        static void f5b(out int a) { a = -1; }

        static string f6a(long a) { return a.ToString(); }
        static string f6c(ref long a) { return a.ToString(); }
        static void f6b(out long a) { a = -1; }

        static string f7a(sbyte a) { return a.ToString(); }
        static string f7c(ref sbyte a) { return a.ToString(); }
        static void f7b(out sbyte a) { a = -1; }

        static string f8a(ushort a) { return a.ToString(); }
        static string f8c(ref ushort a) { return a.ToString(); }
        static void f8b(out ushort a) { a = 1; }

        static string f9a(uint a) { return a.ToString(); }
        static string f9c(ref uint a) { return a.ToString(); }
        static void f9b(out uint a) { a = 1; }

        static string f10a(ulong a) { return a.ToString(); }
        static string f10c(ref ulong a) { return a.ToString(); }
        static void f10b(out ulong a) { a = 1; }

        static string f11a(float a) { return a.ToString(); }
        static string f11c(ref float a) { return a.ToString(); }
        static void f11b(out float a) { a = -1; }

        static string f12a(double a) { return a.ToString(); }
        static string f12c(ref double a) { return a.ToString(); }
        static void f12b(out double a) { a = -1; }

        static string f13a(object a) { return a.ToString(); }
        static string f13c(ref object a) { return a.ToString(); }
        static void f13b(out object a) { a = -1; }

        static string f14a(string a) { return a.ToString(); }
        static string f14c(ref string a) { return a.ToString(); }
        static void f14b(out string a) { a = "INLINE"; }



        public static int Main()
        {
            f1a(true);
            f2a('A');
            f3a((byte)45);
            f4a((short)255);
            f5a(-5012);
            f6a((long)Int64.MinValue);
            f7a((sbyte)-1);
            f8a((ushort)129);
            f9a((uint)UInt32.MaxValue);
            f10a((ulong)0);
            f11a(1.234567F);
            f12a(123456.7890123456789D);
            f13a((object)0);
            f14a("PASS");

            bool b = false;
            char c = 'Z';
            byte b1 = Byte.MaxValue;
            short s1 = 0;
            int i1 = -1;
            long l1 = 1;
            sbyte b2 = SByte.MinValue;
            ushort s2 = UInt16.MinValue;
            uint i2 = 0;
            ulong l2 = UInt64.MinValue;
            float f = Single.NaN;
            double d = Double.Epsilon;
            object o = (object)0;
            string s = "NO-INLINE";

            f1b(out b);
            f2b(out c);
            f3b(out b1);
            f4b(out s1);
            f5b(out i1);
            f6b(out l1);
            f7b(out b2);
            f8b(out s2);
            f9b(out i2);
            f10b(out l2);
            f11b(out f);
            f12b(out d);
            f13b(out o);
            f14b(out s);

            f1c(ref b);
            f2c(ref c);
            f3c(ref b1);
            f4c(ref s1);
            f5c(ref i1);
            f6c(ref l1);
            f7c(ref b2);
            f8c(ref s2);
            f9c(ref i2);
            f10c(ref l2);
            f11c(ref f);
            f12c(ref d);
            f13c(ref o);
            f14c(ref s);

            return 100;
        }
    }

}

