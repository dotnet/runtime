// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;


namespace Tests
{
    internal class Operators
    {
        private static bool s_t = true;
        private static bool s_f = false;
        private static byte s_by_13 = 13;
        private static byte s_by_3 = 3;
        private static sbyte s_sb_m3 = -3;
        private static sbyte s_sb_13 = 13;
        private static short s_sh_8712 = 8712;
        private static short s_sh_m973 = -973;
        private static ushort s_us_8712 = 8712;
        private static ushort s_us_973 = 973;
        private static int s_int_33452 = 33452;
        private static int s_int_m3097 = -3097;
        private static uint s_uint_33452 = 33452u;
        private static uint s_uint_3097 = 3097u;
        private static long s_long_x1 = -971239841234L;
        private static long s_long_x2 = 1876343;
        private static ulong s_ulong_x1 = 971239841234uL;
        private static ulong s_ulong_x2 = 1876343Lu;
        private static float s_float_x1 = -193.23F;
        private static float s_float_x2 = 1.712F;
        private static double s_double_x1 = -7423.2312;
        private static double s_double_x2 = 3.712987;
        private static double s_double_nan = 0.0 / 0.0;
        private static string s_string_null = null;
        private static string s_string1 = "hello ";
        private static string s_string2 = "world ";
        private static string s_string3 = "elvis ";

        static Operators()
        {
            System.Console.WriteLine(".cctor");
        }

        public static int Main()
        {
            System.Console.WriteLine("----------------");

#pragma warning disable 1718
            bool b1 = s_t && s_t;
#pragma warning restore
            bool b2 = s_t && s_f;
            bool b3 = s_f && s_t;
#pragma warning disable 1718
            bool b4 = s_f && s_f;
            bool b5 = s_t || s_t;
#pragma warning restore
            bool b6 = s_t || s_f;
            bool b7 = s_f || s_t;
            bool b8 = s_f || s_f;
#pragma warning disable 1718
            bool b9 = s_t == s_t;
#pragma warning restore
            bool b10 = s_t == s_f;
            bool b11 = s_f == s_t;
#pragma warning disable 1718
            bool b12 = s_f == s_f;
            bool b13 = s_t != s_t;
#pragma warning restore
            bool b14 = s_t != s_f;
            bool b15 = s_f != s_t;
#pragma warning disable 1718
            bool b16 = s_f != s_f;
#pragma warning restore
            bool b17 = !s_t;
            bool b18 = !s_f;
            bool b19 = !(!s_t && (!s_f || s_t));

            System.Console.WriteLine("----------------");

            byte by1 = (byte)(s_by_13 + s_by_3);
            byte by2 = (byte)(s_by_13 - s_by_3);
            byte by3 = (byte)(s_by_13 * s_by_3);
            byte by4 = (byte)(s_by_13 / s_by_3);
            byte by5 = (byte)(s_by_13 % s_by_3);
            byte by6 = (byte)(s_by_13 & s_by_3);
            byte by7 = (byte)(s_by_13 | s_by_3);
            byte by8 = (byte)(s_by_13 ^ s_by_3);
            byte by9 = (byte)(-s_by_13);
            byte by10 = (byte)(s_by_13 >> 1);
            byte by11 = (byte)(s_by_13 >> 1);
#pragma warning disable 1718
            bool by12 = (s_by_13 == s_by_13);
#pragma warning restore
            bool by13 = (s_by_13 == s_by_3);
#pragma warning disable 1718
            bool by14 = (s_by_13 != s_by_13);
#pragma warning restore
            bool by15 = (s_by_13 != s_by_3);
#pragma warning disable 1718
            bool by16 = (s_by_13 >= s_by_13);
#pragma warning restore
            bool by17 = (s_by_13 >= s_by_3);
#pragma warning disable 1718
            bool by18 = (s_by_13 <= s_by_13);
#pragma warning restore
            bool by19 = (s_by_13 <= s_by_3);
#pragma warning disable 1718
            bool by20 = (s_by_13 < s_by_13);
#pragma warning restore
            bool by21 = (s_by_13 < s_by_3);
#pragma warning disable 1718
            bool by22 = (s_by_13 > s_by_13);
#pragma warning restore
            bool by23 = (s_by_13 > s_by_3);

            System.Console.WriteLine("----------------");

            sbyte sb1 = (sbyte)(s_sb_13 + s_sb_m3);
            sbyte sb2 = (sbyte)(s_sb_13 - s_sb_m3);
            sbyte sb3 = (sbyte)(s_sb_13 * s_sb_m3);
            sbyte sb4 = (sbyte)(s_sb_13 / s_sb_m3);
            sbyte sb5 = (sbyte)(s_sb_13 % s_sb_m3);
            sbyte sb6 = (sbyte)(s_sb_13 & s_sb_m3);
            sbyte sb7 = (sbyte)(s_sb_13 | s_sb_m3);
            sbyte sb8 = (sbyte)(s_sb_13 ^ s_sb_m3);
            sbyte sb9 = (sbyte)(-s_sb_13);
            sbyte sb10 = (sbyte)(s_sb_m3 >> 1);
            sbyte sb11 = (sbyte)(s_sb_13 >> 1);
#pragma warning disable 1718
            bool sb12 = (s_sb_13 == s_sb_13);
#pragma warning restore
            bool sb13 = (s_sb_13 == s_sb_m3);
#pragma warning disable 1718
            bool sb14 = (s_sb_13 != s_sb_13);
#pragma warning restore
            bool sb15 = (s_sb_13 != s_sb_m3);
#pragma warning disable 1718
            bool sb16 = (s_sb_13 >= s_sb_13);
#pragma warning restore
            bool sb17 = (s_sb_13 >= s_sb_m3);
#pragma warning disable 1718
            bool sb18 = (s_sb_13 <= s_sb_13);
#pragma warning restore
            bool sb19 = (s_sb_13 <= s_sb_m3);
#pragma warning disable 1718
            bool sb20 = (s_sb_13 < s_sb_13);
#pragma warning restore
            bool sb21 = (s_sb_13 < s_sb_m3);
#pragma warning disable 1718
            bool sb22 = (s_sb_13 > s_sb_13);
#pragma warning restore
            bool sb23 = (s_sb_13 > s_sb_m3);

            System.Console.WriteLine("----------------");

            short sh1 = (short)(s_sh_8712 + s_sh_m973);
            short sh2 = (short)(s_sh_8712 - s_sh_m973);
            short sh3 = (short)(s_sh_8712 * s_sh_m973);
            short sh4 = (short)(s_sh_8712 / s_sh_m973);
            short sh5 = (short)(s_sh_8712 % s_sh_m973);
            short sh6 = (short)(s_sh_8712 & s_sh_m973);
            short sh7 = (short)(s_sh_8712 | s_sh_m973);
            short sh8 = (short)(s_sh_8712 ^ s_sh_m973);
            short sh9 = (short)(-s_sh_8712);
            short sh10 = (short)(s_sh_8712 >> 1);
            short sh11 = (short)((ushort)s_sh_8712 >> 1);
#pragma warning disable 1718
            bool sh12 = (s_sh_8712 == s_sh_8712);
#pragma warning restore
            bool sh13 = (s_sh_8712 == s_sh_m973);
#pragma warning disable 1718
            bool sh14 = (s_sh_8712 != s_sh_8712);
#pragma warning restore
            bool sh15 = (s_sh_8712 != s_sh_m973);
#pragma warning disable 1718
            bool sh16 = (s_sh_8712 >= s_sh_8712);
#pragma warning restore
            bool sh17 = (s_sh_8712 >= s_sh_m973);
#pragma warning disable 1718
            bool sh18 = (s_sh_8712 <= s_sh_8712);
#pragma warning restore
            bool sh19 = (s_sh_8712 <= s_sh_m973);
#pragma warning disable 1718
            bool sh20 = (s_sh_8712 < s_sh_8712);
#pragma warning restore
            bool sh21 = (s_sh_8712 < s_sh_m973);
#pragma warning disable 1718
            bool sh22 = (s_sh_8712 > s_sh_8712);
#pragma warning restore
            bool sh23 = (s_sh_8712 > s_sh_m973);

            System.Console.WriteLine("----------------");

            ushort us1 = (ushort)(s_us_8712 + s_us_973);
            ushort us2 = (ushort)(s_us_8712 - s_us_973);
            ushort us3 = (ushort)(s_us_8712 * s_us_973);
            ushort us4 = (ushort)(s_us_8712 / s_us_973);
            ushort us5 = (ushort)(s_us_8712 % s_us_973);
            ushort us6 = (ushort)(s_us_8712 & s_us_973);
            ushort us7 = (ushort)(s_us_8712 | s_us_973);
            ushort us8 = (ushort)(s_us_8712 ^ s_us_973);
            int us9 = -s_us_8712;
            ushort us10 = (ushort)((short)s_us_8712 >> 1);
            ushort us11 = (ushort)(s_us_8712 >> 1);
#pragma warning disable 1718
            bool us12 = (s_us_8712 == s_us_8712);
#pragma warning restore
            bool us13 = (s_us_8712 == s_us_973);
#pragma warning disable 1718
            bool us14 = (s_us_8712 != s_us_8712);
#pragma warning restore
            bool us15 = (s_us_8712 != s_us_973);
#pragma warning disable 1718
            bool us16 = (s_us_8712 >= s_us_8712);
#pragma warning restore
            bool us17 = (s_us_8712 >= s_us_973);
#pragma warning disable 1718
            bool us18 = (s_us_8712 <= s_us_8712);
#pragma warning restore
            bool us19 = (s_us_8712 <= s_us_973);
#pragma warning disable 1718
            bool us20 = (s_us_8712 < s_us_8712);
#pragma warning restore
            bool us21 = (s_us_8712 < s_us_973);
#pragma warning disable 1718
            bool us22 = (s_us_8712 > s_us_8712);
#pragma warning restore
            bool us23 = (s_us_8712 > s_us_973);

            System.Console.WriteLine("----------------");

            int int1 = s_int_33452 + s_int_m3097;
            int int2 = s_int_33452 - s_int_m3097;
            int int3 = (int)(s_int_33452 * s_int_m3097);
            int int4 = s_int_33452 / s_int_m3097;
            int int5 = s_int_33452 % s_int_m3097;
            int int6 = s_int_33452 & s_int_m3097;
            int int7 = s_int_33452 | s_int_m3097;
            int int8 = s_int_33452 ^ s_int_m3097;
            int int9 = (-s_int_33452);
            int int10 = s_int_33452 >> 1;
            int int11 = (int)((uint)s_int_33452 >> 1);
#pragma warning disable 1718
            bool int12 = (s_int_33452 == s_int_33452);
#pragma warning restore
            bool int13 = (s_int_33452 == s_int_m3097);
#pragma warning disable 1718
            bool int14 = (s_int_33452 != s_int_33452);
#pragma warning restore
            bool int15 = (s_int_33452 != s_int_m3097);
#pragma warning disable 1718
            bool int16 = (s_int_33452 >= s_int_33452);
#pragma warning restore
            bool int17 = (s_int_33452 >= s_int_m3097);
#pragma warning disable 1718
            bool int18 = (s_int_33452 <= s_int_33452);
#pragma warning restore
            bool int19 = (s_int_33452 <= s_int_m3097);
#pragma warning disable 1718
            bool int20 = (s_int_33452 < s_int_33452);
#pragma warning restore
            bool int21 = (s_int_33452 < s_int_m3097);
#pragma warning disable 1718
            bool int22 = (s_int_33452 > s_int_33452);
#pragma warning restore
            bool int23 = (s_int_33452 > s_int_m3097);

            System.Console.WriteLine("----------------");

            uint uint1 = s_uint_33452 + s_uint_3097;
            uint uint2 = s_uint_33452 - s_uint_3097;
            uint uint3 = (uint)(s_uint_33452 * s_uint_3097);
            uint uint4 = s_uint_33452 / s_uint_3097;
            uint uint5 = s_uint_33452 % s_uint_3097;
            uint uint6 = s_uint_33452 & s_uint_3097;
            uint uint7 = s_uint_33452 | s_uint_3097;
            uint uint8 = s_uint_33452 ^ s_uint_3097;
            long uint9 = -s_uint_33452;
            uint uint10 = s_uint_33452 >> 1;
            uint uint11 = s_uint_33452 >> 1;
#pragma warning disable 1718
            bool uint12 = (s_uint_33452 == s_uint_33452);
#pragma warning restore
            bool uint13 = (s_uint_33452 == s_uint_3097);
#pragma warning disable 1718
            bool uint14 = (s_uint_33452 != s_uint_33452);
#pragma warning restore
            bool uint15 = (s_uint_33452 != s_uint_3097);
#pragma warning disable 1718
            bool uint16 = (s_uint_33452 >= s_uint_33452);
#pragma warning restore
            bool uint17 = (s_uint_33452 >= s_uint_3097);
#pragma warning disable 1718
            bool uint18 = (s_uint_33452 <= s_uint_33452);
#pragma warning restore
            bool uint19 = (s_uint_33452 <= s_uint_3097);
#pragma warning disable 1718
            bool uint20 = (s_uint_33452 < s_uint_33452);
#pragma warning restore
            bool uint21 = (s_uint_33452 < s_uint_3097);
#pragma warning disable 1718
            bool uint22 = (s_uint_33452 > s_uint_33452);
#pragma warning restore
            bool uint23 = (s_uint_33452 > s_uint_3097);

            System.Console.WriteLine("----------------");

            long long1 = s_long_x1 + s_long_x2;
            long long2 = s_long_x1 - s_long_x2;
            long long3 = s_long_x1 * s_long_x2;
            long long4 = s_long_x1 / s_long_x2;
            long long5 = s_long_x1 % s_long_x2;
            long long6 = s_long_x1 & s_long_x2;
            long long7 = s_long_x1 | s_long_x2;
            long long8 = s_long_x1 ^ s_long_x2;
            long long9 = (-s_long_x1);
            long long10 = s_long_x1 >> 1;
            long long11 = (long)((ulong)s_long_x1 >> 1);
#pragma warning disable 1718
            bool long12 = (s_long_x1 == s_long_x1);
#pragma warning restore
            bool long13 = (s_long_x1 == s_long_x2);
#pragma warning disable 1718
            bool long14 = (s_long_x1 != s_long_x1);
#pragma warning restore
            bool long15 = (s_long_x1 != s_long_x2);
#pragma warning disable 1718
            bool long16 = (s_long_x1 >= s_long_x1);
#pragma warning restore
            bool long17 = (s_long_x1 >= s_long_x2);
#pragma warning disable 1718
            bool long18 = (s_long_x1 <= s_long_x1);
#pragma warning restore
            bool long19 = (s_long_x1 <= s_long_x2);
#pragma warning disable 1718
            bool long20 = (s_long_x1 < s_long_x1);
#pragma warning restore
            bool long21 = (s_long_x1 < s_long_x2);
#pragma warning disable 1718
            bool long22 = (s_long_x1 > s_long_x1);
#pragma warning restore
            bool long23 = (s_long_x1 > s_long_x2);

            System.Console.WriteLine("----------------");

            ulong ulong1 = s_ulong_x1 + s_ulong_x2;
            ulong ulong2 = s_ulong_x1 - s_ulong_x2;
            ulong ulong3 = s_ulong_x1 * s_ulong_x2;
            ulong ulong4 = s_ulong_x1 / s_ulong_x2;
            ulong ulong5 = s_ulong_x1 % s_ulong_x2;
            ulong ulong6 = s_ulong_x1 & s_ulong_x2;
            ulong ulong7 = s_ulong_x1 | s_ulong_x2;
            ulong ulong8 = s_ulong_x1 ^ s_ulong_x2;
            ulong ulong10 = s_ulong_x1 >> 1;
            ulong ulong11 = (ulong)(s_ulong_x1 >> 1);
#pragma warning disable 1718
            bool ulong12 = (s_ulong_x1 == s_ulong_x1);
#pragma warning restore
            bool ulong13 = (s_ulong_x1 == s_ulong_x2);
#pragma warning disable 1718
            bool ulong14 = (s_ulong_x1 != s_ulong_x1);
#pragma warning restore
            bool ulong15 = (s_ulong_x1 != s_ulong_x2);
#pragma warning disable 1718
            bool ulong16 = (s_ulong_x1 >= s_ulong_x1);
#pragma warning restore
            bool ulong17 = (s_ulong_x1 >= s_ulong_x2);
#pragma warning disable 1718
            bool ulong18 = (s_ulong_x1 <= s_ulong_x1);
#pragma warning restore
            bool ulong19 = (s_ulong_x1 <= s_ulong_x2);
#pragma warning disable 1718
            bool ulong20 = (s_ulong_x1 < s_ulong_x1);
#pragma warning restore
            bool ulong21 = (s_ulong_x1 < s_ulong_x2);
#pragma warning disable 1718
            bool ulong22 = (s_ulong_x1 > s_ulong_x1);
#pragma warning restore
            bool ulong23 = (s_ulong_x1 > s_ulong_x2);

            System.Console.WriteLine("----------------");

            float float1 = s_float_x1 + s_float_x2;
            float float2 = s_float_x1 - s_float_x2;
            float float3 = s_float_x1 * s_float_x2;
            float float4 = s_float_x1 / s_float_x2;
            float float5 = s_float_x1 % s_float_x2;
            float float9 = (-s_float_x1);
#pragma warning disable 1718
            bool float12 = (s_float_x1 == s_float_x1);
#pragma warning restore
            bool float13 = (s_float_x1 == s_float_x2);
#pragma warning disable 1718
            bool float14 = (s_float_x1 != s_float_x1);
#pragma warning restore
            bool float15 = (s_float_x1 != s_float_x2);
#pragma warning disable 1718
            bool float16 = (s_float_x1 >= s_float_x1);
#pragma warning restore
            bool float17 = (s_float_x1 >= s_float_x2);
#pragma warning disable 1718
            bool float18 = (s_float_x1 <= s_float_x1);
#pragma warning restore
            bool float19 = (s_float_x1 <= s_float_x2);
#pragma warning disable 1718
            bool float20 = (s_float_x1 < s_float_x1);
#pragma warning restore
            bool float21 = (s_float_x1 < s_float_x2);
#pragma warning disable 1718
            bool float22 = (s_float_x1 > s_float_x1);
#pragma warning restore
            bool float23 = (s_float_x1 > s_float_x2);

            System.Console.WriteLine("----------------");

            double double1 = s_double_x1 + s_double_x2;
            double double2 = s_double_x1 - s_double_x2;
            double double3 = s_double_x1 * s_double_x2;
            double double4 = s_double_x1 / s_double_x2;
            double double5 = s_double_x1 % s_double_x2;
            double double9 = (-s_double_x1);
#pragma warning disable 1718
            bool double12 = (s_double_x1 == s_double_x1);
#pragma warning restore
            bool double13 = (s_double_x1 == s_double_x2);
#pragma warning disable 1718
            bool double14 = (s_double_x1 != s_double_x1);
#pragma warning restore
            bool double15 = (s_double_x1 != s_double_x2);
#pragma warning disable 1718
            bool double16 = (s_double_x1 >= s_double_x1);
#pragma warning restore
            bool double17 = (s_double_x1 >= s_double_x2);
#pragma warning disable 1718
            bool double18 = (s_double_x1 <= s_double_x1);
#pragma warning restore
            bool double19 = (s_double_x1 <= s_double_x2);
#pragma warning disable 1718
            bool double20 = (s_double_x1 < s_double_x1);
#pragma warning restore
            bool double21 = (s_double_x1 < s_double_x2);
#pragma warning disable 1718
            bool double22 = (s_double_x1 > s_double_x1);
#pragma warning restore
            bool double23 = (s_double_x1 > s_double_x2);
#pragma warning disable 1718
            bool double24 = (s_double_nan == s_double_nan);
#pragma warning restore
            bool double25 = (s_double_nan == s_double_x2);
#pragma warning disable 1718
            bool double26 = (s_double_nan != s_double_nan);
#pragma warning restore
            bool double27 = (s_double_nan != s_double_x2);
#pragma warning disable 1718
            bool double28 = (s_double_nan >= s_double_nan);
#pragma warning restore
            bool double29 = (s_double_nan >= s_double_x2);
#pragma warning disable 1718
            bool double30 = (s_double_nan <= s_double_nan);
#pragma warning restore
            bool double31 = (s_double_nan <= s_double_x2);
#pragma warning disable 1718
            bool double32 = (s_double_nan < s_double_nan);
#pragma warning restore
            bool double33 = (s_double_nan < s_double_x2);
#pragma warning disable 1718
            bool double34 = (s_double_nan > s_double_nan);
#pragma warning restore
            bool double35 = (s_double_nan > s_double_x2);

            System.Console.WriteLine("----------------");

            string string4 = s_string1 + s_string2;
            string string5 = s_string1 + s_string2 + s_string3;
            string string6 = s_string1 + s_string2 + s_string3 + s_string1;
            string string7 = s_string1 + s_string2 + s_string3 + s_string1 + s_string2;
            string string8 = "eric " + "is " + s_string3 + s_string1 + "clapton ";
            string string9 = s_string1 + s_string_null;
            string string10 = s_string1 + s_string_null + s_string3;
            string string11 = s_string_null + s_string2;


            Console.WriteLine("Booleans:");
            Console.WriteLine(s_t);
            Console.WriteLine(s_f);
            Console.WriteLine(b1);
            Console.WriteLine(b2);
            Console.WriteLine(b3);
            Console.WriteLine(b4);
            Console.WriteLine(b5);
            Console.WriteLine(b6);
            Console.WriteLine(b7);
            Console.WriteLine(b8);
            Console.WriteLine(b9);
            Console.WriteLine(b10);
            Console.WriteLine(b11);
            Console.WriteLine(b12);
            Console.WriteLine(b13);
            Console.WriteLine(b14);
            Console.WriteLine(b15);
            Console.WriteLine(b16);
            Console.WriteLine(b17);
            Console.WriteLine(b18);
            Console.WriteLine(b19);

            Console.WriteLine("Bytes:");
            Console.WriteLine(s_by_13);
            Console.WriteLine(s_by_3);
            Console.WriteLine(by1);
            Console.WriteLine(by2);
            Console.WriteLine(by3);
            Console.WriteLine(by4);
            Console.WriteLine(by5);
            Console.WriteLine(by6);
            Console.WriteLine(by7);
            Console.WriteLine(by8);
            Console.WriteLine(by9);
            Console.WriteLine(by10);
            Console.WriteLine(by11);
            Console.WriteLine(by12);
            Console.WriteLine(by13);
            Console.WriteLine(by14);
            Console.WriteLine(by15);
            Console.WriteLine(by16);
            Console.WriteLine(by17);
            Console.WriteLine(by18);
            Console.WriteLine(by19);
            Console.WriteLine(by20);
            Console.WriteLine(by21);
            Console.WriteLine(by22);
            Console.WriteLine(by23);

            Console.WriteLine("SBytes:");
            Console.WriteLine(s_sb_13);
            Console.WriteLine(s_sb_m3);
            Console.WriteLine(sb1);
            Console.WriteLine(sb2);
            Console.WriteLine(sb3);
            Console.WriteLine(sb4);
            Console.WriteLine(sb5);
            Console.WriteLine(sb6);
            Console.WriteLine(sb7);
            Console.WriteLine(sb8);
            Console.WriteLine(sb9);
            Console.WriteLine(sb10);
            Console.WriteLine(sb11);
            Console.WriteLine(sb12);
            Console.WriteLine(sb13);
            Console.WriteLine(sb14);
            Console.WriteLine(sb15);
            Console.WriteLine(sb16);
            Console.WriteLine(sb17);
            Console.WriteLine(sb18);
            Console.WriteLine(sb19);
            Console.WriteLine(sb20);
            Console.WriteLine(sb21);
            Console.WriteLine(sb22);
            Console.WriteLine(sb23);

            Console.WriteLine("Shorts:");
            Console.WriteLine(s_sh_8712);
            Console.WriteLine(s_sh_m973);
            Console.WriteLine(sh1);
            Console.WriteLine(sh2);
            Console.WriteLine(sh3);
            Console.WriteLine(sh4);
            Console.WriteLine(sh5);
            Console.WriteLine(sh6);
            Console.WriteLine(sh7);
            Console.WriteLine(sh8);
            Console.WriteLine(sh9);
            Console.WriteLine(sh10);
            Console.WriteLine(sh11);
            Console.WriteLine(sh12);
            Console.WriteLine(sh13);
            Console.WriteLine(sh14);
            Console.WriteLine(sh15);
            Console.WriteLine(sh16);
            Console.WriteLine(sh17);
            Console.WriteLine(sh18);
            Console.WriteLine(sh19);
            Console.WriteLine(sh20);
            Console.WriteLine(sh21);
            Console.WriteLine(sh22);
            Console.WriteLine(sh23);

            Console.WriteLine("UShorts:");
            Console.WriteLine(s_us_8712);
            Console.WriteLine(s_us_973);
            Console.WriteLine(us1);
            Console.WriteLine(us2);
            Console.WriteLine(us3);
            Console.WriteLine(us4);
            Console.WriteLine(us5);
            Console.WriteLine(us6);
            Console.WriteLine(us7);
            Console.WriteLine(us8);
            Console.WriteLine(us9);
            Console.WriteLine(us10);
            Console.WriteLine(us11);
            Console.WriteLine(us12);
            Console.WriteLine(us13);
            Console.WriteLine(us14);
            Console.WriteLine(us15);
            Console.WriteLine(us16);
            Console.WriteLine(us17);
            Console.WriteLine(us18);
            Console.WriteLine(us19);
            Console.WriteLine(us20);
            Console.WriteLine(us21);
            Console.WriteLine(us22);
            Console.WriteLine(us23);

            Console.WriteLine("Ints:");
            Console.WriteLine(s_int_33452);
            Console.WriteLine(s_int_m3097);
            Console.WriteLine(int1);
            Console.WriteLine(int2);
            Console.WriteLine(int3);
            Console.WriteLine(int4);
            Console.WriteLine(int5);
            Console.WriteLine(int6);
            Console.WriteLine(int7);
            Console.WriteLine(int8);
            Console.WriteLine(int9);
            Console.WriteLine(int10);
            Console.WriteLine(int11);
            Console.WriteLine(int12);
            Console.WriteLine(int13);
            Console.WriteLine(int14);
            Console.WriteLine(int15);
            Console.WriteLine(int16);
            Console.WriteLine(int17);
            Console.WriteLine(int18);
            Console.WriteLine(int19);
            Console.WriteLine(int20);
            Console.WriteLine(int21);
            Console.WriteLine(int22);
            Console.WriteLine(int23);

            Console.WriteLine("UInts:");
            Console.WriteLine(s_uint_33452);
            Console.WriteLine(s_uint_3097);
            Console.WriteLine(uint1);
            Console.WriteLine(uint2);
            Console.WriteLine(uint3);
            Console.WriteLine(uint4);
            Console.WriteLine(uint5);
            Console.WriteLine(uint6);
            Console.WriteLine(uint7);
            Console.WriteLine(uint8);
            Console.WriteLine(uint9);
            Console.WriteLine(uint10);
            Console.WriteLine(uint11);
            Console.WriteLine(uint12);
            Console.WriteLine(uint13);
            Console.WriteLine(uint14);
            Console.WriteLine(uint15);
            Console.WriteLine(uint16);
            Console.WriteLine(uint17);
            Console.WriteLine(uint18);
            Console.WriteLine(uint19);
            Console.WriteLine(uint20);
            Console.WriteLine(uint21);
            Console.WriteLine(uint22);
            Console.WriteLine(uint23);

            Console.WriteLine("Longs:");
            Console.WriteLine(s_long_x1);
            Console.WriteLine(s_long_x2);
            Console.WriteLine(long1);
            Console.WriteLine(long2);
            Console.WriteLine(long3);
            Console.WriteLine(long4);
            Console.WriteLine(long5);
            Console.WriteLine(long6);
            Console.WriteLine(long7);
            Console.WriteLine(long8);
            Console.WriteLine(long9);
            Console.WriteLine(long10);
            Console.WriteLine(long11);
            Console.WriteLine(long12);
            Console.WriteLine(long13);
            Console.WriteLine(long14);
            Console.WriteLine(long15);
            Console.WriteLine(long16);
            Console.WriteLine(long17);
            Console.WriteLine(long18);
            Console.WriteLine(long19);
            Console.WriteLine(long20);
            Console.WriteLine(long21);
            Console.WriteLine(long22);
            Console.WriteLine(long23);

            Console.WriteLine("ULongs:");
            Console.WriteLine(s_ulong_x1);
            Console.WriteLine(s_ulong_x2);
            Console.WriteLine(ulong1);
            Console.WriteLine(ulong2);
            Console.WriteLine(ulong3);
            Console.WriteLine(ulong4);
            Console.WriteLine(ulong5);
            Console.WriteLine(ulong6);
            Console.WriteLine(ulong7);
            Console.WriteLine(ulong8);
            Console.WriteLine(ulong10);
            Console.WriteLine(ulong11);
            Console.WriteLine(ulong12);
            Console.WriteLine(ulong13);
            Console.WriteLine(ulong14);
            Console.WriteLine(ulong15);
            Console.WriteLine(ulong16);
            Console.WriteLine(ulong17);
            Console.WriteLine(ulong18);
            Console.WriteLine(ulong19);
            Console.WriteLine(ulong20);
            Console.WriteLine(ulong21);
            Console.WriteLine(ulong22);
            Console.WriteLine(ulong23);

            Console.WriteLine("Floats:");
            Console.WriteLine(s_float_x1);
            Console.WriteLine(s_float_x2);
            Console.WriteLine(float1);
            Console.WriteLine(float2);
            Console.WriteLine(float3);
            Console.WriteLine(float4);
            Console.WriteLine(float5);
            Console.WriteLine(float9);
            Console.WriteLine(float12);
            Console.WriteLine(float13);
            Console.WriteLine(float14);
            Console.WriteLine(float15);
            Console.WriteLine(float16);
            Console.WriteLine(float17);
            Console.WriteLine(float18);
            Console.WriteLine(float19);
            Console.WriteLine(float20);
            Console.WriteLine(float21);
            Console.WriteLine(float22);
            Console.WriteLine(float23);

            Console.WriteLine("Doubles:");
            Console.WriteLine(s_double_x1);
            Console.WriteLine(s_double_x2);
            Console.WriteLine(s_double_nan);
            Console.WriteLine(double1);
            Console.WriteLine(double2);
            Console.WriteLine(double3);
            Console.WriteLine(double4);
            Console.WriteLine(double5);
            Console.WriteLine(double9);
            Console.WriteLine(double12);
            Console.WriteLine(double13);
            Console.WriteLine(double14);
            Console.WriteLine(double15);
            Console.WriteLine(double16);
            Console.WriteLine(double17);
            Console.WriteLine(double18);
            Console.WriteLine(double19);
            Console.WriteLine(double20);
            Console.WriteLine(double21);
            Console.WriteLine(double22);
            Console.WriteLine(double23);
            Console.WriteLine(double24);
            Console.WriteLine(double25);
            Console.WriteLine(double26);
            Console.WriteLine(double27);
            Console.WriteLine(double28);
            Console.WriteLine(double29);
            Console.WriteLine(double30);
            Console.WriteLine(double31);
            Console.WriteLine(double32);
            Console.WriteLine(double33);
            Console.WriteLine(double34);
            Console.WriteLine(double35);

            Console.WriteLine("Strings:");
            Console.WriteLine(s_string1);
            Console.WriteLine(s_string2);
            Console.WriteLine(s_string3);
            Console.WriteLine(string4);
            Console.WriteLine(string5);
            Console.WriteLine(string6);
            Console.WriteLine(string7);
            Console.WriteLine(string8);
            Console.WriteLine(string9);
            Console.WriteLine(string10);
            Console.WriteLine(string11);

            return 100;
        }
    }
}
