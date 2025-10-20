using System;

namespace System
{
    public static class Modern
    {
        public static void writeln(object value)
        {
            Console.WriteLine(value);
        }
        public static void write(object value)
        {
            Console.Write(value);
        }
        public static string? readln()
        {
            return Console.ReadLine();
        }
        public static int toint(object value)
        {
            return Convert.ToInt32(value);
        }
        public static byte tobyte(object value)
        {
            return Convert.ToByte(value);
        }
        public static short toshort(object value)
        {
            return Convert.ToInt16(value);
        }
        public static long tolong(object value)
        {
            return Convert.ToInt64(value);
        }
        public static float tofloat(object value)
        {
            return Convert.ToSingle(value);
        }
        public static double todouble(object value)
        {
            return Convert.ToDouble(value);
        }
        public static bool tobool(object value)
        {
            return Convert.ToBoolean(value);
        }
        public static char tochar(object value)
        {
            return Convert.ToChar(value);
        }
        public static decimal todecimal(object value)
        {
            return Convert.ToDecimal(value);
        }
        public static DateTime todatetime(object value)
        {
            return Convert.ToDateTime(value);
        }
        public static DateOnly todate(DateTime value)
        {
            return DateOnly.FromDateTime(value);
        }
        public static TimeOnly totime(DateTime value)
        {
            return TimeOnly.FromDateTime(value);
        }
        public static string tostring(object value)
        {
            return value.ToString() ?? string.Empty;
        }
    }
}
