// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Dynamic.Operator.Tests
{
    public static class LiftCommon
    {
        public static bool? s_bool = null;
        public static byte? s_byte = null;
        public static char? s_char = null;
        public static decimal? s_decimal = null;
        public static double? s_double = null;
        public static float? s_float = null;
        public static int? s_int = null;
        public static long? s_long = null;
        public static object s_object = new object();
        public static sbyte? s_sbyte = null;
        public static short? s_short = 6;
        public static string s_string = "a";
        public static uint? s_uint = null;
        public static ulong? s_ulong = 2;
        public static ushort? s_ushort = 7;
    }

    public static class TypeCommon
    {
        public static bool s_bool = true;
        public static byte s_byte = 1;
        public static char s_char = 'a';
        public static decimal s_decimal = 1M;
        public static double s_double = 10.1;
        public static float s_float = 10.1f;
        public static int s_int = 10;
        public static long s_long = 5;
        public static object s_object = new object();
        public static sbyte s_sbyte = 10;
        public static short s_short = 6;
        public static string s_string = "a";
        public static uint s_uint = 15;
        public static ulong s_ulong = 2;
        public static ushort s_ushort = 7;
    }
}
