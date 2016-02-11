// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System
{
    public static partial class BitConverter
    {
        public static readonly bool IsLittleEndian;
        public static long DoubleToInt64Bits(double value) { return default(long); }
        public static byte[] GetBytes(bool value) { return default(byte[]); }
        public static byte[] GetBytes(char value) { return default(byte[]); }
        public static byte[] GetBytes(double value) { return default(byte[]); }
        public static byte[] GetBytes(short value) { return default(byte[]); }
        public static byte[] GetBytes(int value) { return default(byte[]); }
        public static byte[] GetBytes(long value) { return default(byte[]); }
        public static byte[] GetBytes(float value) { return default(byte[]); }
        [System.CLSCompliantAttribute(false)]
        public static byte[] GetBytes(ushort value) { return default(byte[]); }
        [System.CLSCompliantAttribute(false)]
        public static byte[] GetBytes(uint value) { return default(byte[]); }
        [System.CLSCompliantAttribute(false)]
        public static byte[] GetBytes(ulong value) { return default(byte[]); }
        public static double Int64BitsToDouble(long value) { return default(double); }
        public static bool ToBoolean(byte[] value, int startIndex) { return default(bool); }
        public static char ToChar(byte[] value, int startIndex) { return default(char); }
        public static double ToDouble(byte[] value, int startIndex) { return default(double); }
        public static short ToInt16(byte[] value, int startIndex) { return default(short); }
        public static int ToInt32(byte[] value, int startIndex) { return default(int); }
        public static long ToInt64(byte[] value, int startIndex) { return default(long); }
        public static float ToSingle(byte[] value, int startIndex) { return default(float); }
        public static string ToString(byte[] value) { return default(string); }
        public static string ToString(byte[] value, int startIndex) { return default(string); }
        public static string ToString(byte[] value, int startIndex, int length) { return default(string); }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(byte[] value, int startIndex) { return default(ushort); }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(byte[] value, int startIndex) { return default(uint); }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(byte[] value, int startIndex) { return default(ulong); }
    }
    public static partial class Convert
    {
        public static object ChangeType(object value, System.Type conversionType) { return default(object); }
        public static object ChangeType(object value, System.Type conversionType, System.IFormatProvider provider) { return default(object); }
        public static object ChangeType(object value, System.TypeCode typeCode, System.IFormatProvider provider) { return default(object); }
        public static byte[] FromBase64CharArray(char[] inArray, int offset, int length) { return default(byte[]); }
        public static byte[] FromBase64String(string s) { return default(byte[]); }
        public static System.TypeCode GetTypeCode(object value) { return default(System.TypeCode); }
        public static int ToBase64CharArray(byte[] inArray, int offsetIn, int length, char[] outArray, int offsetOut) { return default(int); }
        public static string ToBase64String(byte[] inArray) { return default(string); }
        public static string ToBase64String(byte[] inArray, int offset, int length) { return default(string); }
        public static bool ToBoolean(bool value) { return default(bool); }
        public static bool ToBoolean(byte value) { return default(bool); }
        public static bool ToBoolean(decimal value) { return default(bool); }
        public static bool ToBoolean(double value) { return default(bool); }
        public static bool ToBoolean(short value) { return default(bool); }
        public static bool ToBoolean(int value) { return default(bool); }
        public static bool ToBoolean(long value) { return default(bool); }
        public static bool ToBoolean(object value) { return default(bool); }
        public static bool ToBoolean(object value, System.IFormatProvider provider) { return default(bool); }
        [System.CLSCompliantAttribute(false)]
        public static bool ToBoolean(sbyte value) { return default(bool); }
        public static bool ToBoolean(float value) { return default(bool); }
        public static bool ToBoolean(string value) { return default(bool); }
        public static bool ToBoolean(string value, System.IFormatProvider provider) { return default(bool); }
        [System.CLSCompliantAttribute(false)]
        public static bool ToBoolean(ushort value) { return default(bool); }
        [System.CLSCompliantAttribute(false)]
        public static bool ToBoolean(uint value) { return default(bool); }
        [System.CLSCompliantAttribute(false)]
        public static bool ToBoolean(ulong value) { return default(bool); }
        public static byte ToByte(bool value) { return default(byte); }
        public static byte ToByte(byte value) { return default(byte); }
        public static byte ToByte(char value) { return default(byte); }
        public static byte ToByte(decimal value) { return default(byte); }
        public static byte ToByte(double value) { return default(byte); }
        public static byte ToByte(short value) { return default(byte); }
        public static byte ToByte(int value) { return default(byte); }
        public static byte ToByte(long value) { return default(byte); }
        public static byte ToByte(object value) { return default(byte); }
        public static byte ToByte(object value, System.IFormatProvider provider) { return default(byte); }
        [System.CLSCompliantAttribute(false)]
        public static byte ToByte(sbyte value) { return default(byte); }
        public static byte ToByte(float value) { return default(byte); }
        public static byte ToByte(string value) { return default(byte); }
        public static byte ToByte(string value, System.IFormatProvider provider) { return default(byte); }
        public static byte ToByte(string value, int fromBase) { return default(byte); }
        [System.CLSCompliantAttribute(false)]
        public static byte ToByte(ushort value) { return default(byte); }
        [System.CLSCompliantAttribute(false)]
        public static byte ToByte(uint value) { return default(byte); }
        [System.CLSCompliantAttribute(false)]
        public static byte ToByte(ulong value) { return default(byte); }
        public static char ToChar(byte value) { return default(char); }
        public static char ToChar(short value) { return default(char); }
        public static char ToChar(int value) { return default(char); }
        public static char ToChar(long value) { return default(char); }
        public static char ToChar(object value) { return default(char); }
        public static char ToChar(object value, System.IFormatProvider provider) { return default(char); }
        [System.CLSCompliantAttribute(false)]
        public static char ToChar(sbyte value) { return default(char); }
        public static char ToChar(string value) { return default(char); }
        public static char ToChar(string value, System.IFormatProvider provider) { return default(char); }
        [System.CLSCompliantAttribute(false)]
        public static char ToChar(ushort value) { return default(char); }
        [System.CLSCompliantAttribute(false)]
        public static char ToChar(uint value) { return default(char); }
        [System.CLSCompliantAttribute(false)]
        public static char ToChar(ulong value) { return default(char); }
        public static System.DateTime ToDateTime(object value) { return default(System.DateTime); }
        public static System.DateTime ToDateTime(object value, System.IFormatProvider provider) { return default(System.DateTime); }
        public static System.DateTime ToDateTime(string value) { return default(System.DateTime); }
        public static System.DateTime ToDateTime(string value, System.IFormatProvider provider) { return default(System.DateTime); }
        public static decimal ToDecimal(bool value) { return default(decimal); }
        public static decimal ToDecimal(byte value) { return default(decimal); }
        public static decimal ToDecimal(decimal value) { return default(decimal); }
        public static decimal ToDecimal(double value) { return default(decimal); }
        public static decimal ToDecimal(short value) { return default(decimal); }
        public static decimal ToDecimal(int value) { return default(decimal); }
        public static decimal ToDecimal(long value) { return default(decimal); }
        public static decimal ToDecimal(object value) { return default(decimal); }
        public static decimal ToDecimal(object value, System.IFormatProvider provider) { return default(decimal); }
        [System.CLSCompliantAttribute(false)]
        public static decimal ToDecimal(sbyte value) { return default(decimal); }
        public static decimal ToDecimal(float value) { return default(decimal); }
        public static decimal ToDecimal(string value) { return default(decimal); }
        public static decimal ToDecimal(string value, System.IFormatProvider provider) { return default(decimal); }
        [System.CLSCompliantAttribute(false)]
        public static decimal ToDecimal(ushort value) { return default(decimal); }
        [System.CLSCompliantAttribute(false)]
        public static decimal ToDecimal(uint value) { return default(decimal); }
        [System.CLSCompliantAttribute(false)]
        public static decimal ToDecimal(ulong value) { return default(decimal); }
        public static double ToDouble(bool value) { return default(double); }
        public static double ToDouble(byte value) { return default(double); }
        public static double ToDouble(decimal value) { return default(double); }
        public static double ToDouble(double value) { return default(double); }
        public static double ToDouble(short value) { return default(double); }
        public static double ToDouble(int value) { return default(double); }
        public static double ToDouble(long value) { return default(double); }
        public static double ToDouble(object value) { return default(double); }
        public static double ToDouble(object value, System.IFormatProvider provider) { return default(double); }
        [System.CLSCompliantAttribute(false)]
        public static double ToDouble(sbyte value) { return default(double); }
        public static double ToDouble(float value) { return default(double); }
        public static double ToDouble(string value) { return default(double); }
        public static double ToDouble(string value, System.IFormatProvider provider) { return default(double); }
        [System.CLSCompliantAttribute(false)]
        public static double ToDouble(ushort value) { return default(double); }
        [System.CLSCompliantAttribute(false)]
        public static double ToDouble(uint value) { return default(double); }
        [System.CLSCompliantAttribute(false)]
        public static double ToDouble(ulong value) { return default(double); }
        public static short ToInt16(bool value) { return default(short); }
        public static short ToInt16(byte value) { return default(short); }
        public static short ToInt16(char value) { return default(short); }
        public static short ToInt16(decimal value) { return default(short); }
        public static short ToInt16(double value) { return default(short); }
        public static short ToInt16(short value) { return default(short); }
        public static short ToInt16(int value) { return default(short); }
        public static short ToInt16(long value) { return default(short); }
        public static short ToInt16(object value) { return default(short); }
        public static short ToInt16(object value, System.IFormatProvider provider) { return default(short); }
        [System.CLSCompliantAttribute(false)]
        public static short ToInt16(sbyte value) { return default(short); }
        public static short ToInt16(float value) { return default(short); }
        public static short ToInt16(string value) { return default(short); }
        public static short ToInt16(string value, System.IFormatProvider provider) { return default(short); }
        public static short ToInt16(string value, int fromBase) { return default(short); }
        [System.CLSCompliantAttribute(false)]
        public static short ToInt16(ushort value) { return default(short); }
        [System.CLSCompliantAttribute(false)]
        public static short ToInt16(uint value) { return default(short); }
        [System.CLSCompliantAttribute(false)]
        public static short ToInt16(ulong value) { return default(short); }
        public static int ToInt32(bool value) { return default(int); }
        public static int ToInt32(byte value) { return default(int); }
        public static int ToInt32(char value) { return default(int); }
        public static int ToInt32(decimal value) { return default(int); }
        public static int ToInt32(double value) { return default(int); }
        public static int ToInt32(short value) { return default(int); }
        public static int ToInt32(int value) { return default(int); }
        public static int ToInt32(long value) { return default(int); }
        public static int ToInt32(object value) { return default(int); }
        public static int ToInt32(object value, System.IFormatProvider provider) { return default(int); }
        [System.CLSCompliantAttribute(false)]
        public static int ToInt32(sbyte value) { return default(int); }
        public static int ToInt32(float value) { return default(int); }
        public static int ToInt32(string value) { return default(int); }
        public static int ToInt32(string value, System.IFormatProvider provider) { return default(int); }
        public static int ToInt32(string value, int fromBase) { return default(int); }
        [System.CLSCompliantAttribute(false)]
        public static int ToInt32(ushort value) { return default(int); }
        [System.CLSCompliantAttribute(false)]
        public static int ToInt32(uint value) { return default(int); }
        [System.CLSCompliantAttribute(false)]
        public static int ToInt32(ulong value) { return default(int); }
        public static long ToInt64(bool value) { return default(long); }
        public static long ToInt64(byte value) { return default(long); }
        public static long ToInt64(char value) { return default(long); }
        public static long ToInt64(decimal value) { return default(long); }
        public static long ToInt64(double value) { return default(long); }
        public static long ToInt64(short value) { return default(long); }
        public static long ToInt64(int value) { return default(long); }
        public static long ToInt64(long value) { return default(long); }
        public static long ToInt64(object value) { return default(long); }
        public static long ToInt64(object value, System.IFormatProvider provider) { return default(long); }
        [System.CLSCompliantAttribute(false)]
        public static long ToInt64(sbyte value) { return default(long); }
        public static long ToInt64(float value) { return default(long); }
        public static long ToInt64(string value) { return default(long); }
        public static long ToInt64(string value, System.IFormatProvider provider) { return default(long); }
        public static long ToInt64(string value, int fromBase) { return default(long); }
        [System.CLSCompliantAttribute(false)]
        public static long ToInt64(ushort value) { return default(long); }
        [System.CLSCompliantAttribute(false)]
        public static long ToInt64(uint value) { return default(long); }
        [System.CLSCompliantAttribute(false)]
        public static long ToInt64(ulong value) { return default(long); }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(bool value) { return default(sbyte); }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(byte value) { return default(sbyte); }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(char value) { return default(sbyte); }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(decimal value) { return default(sbyte); }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(double value) { return default(sbyte); }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(short value) { return default(sbyte); }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(int value) { return default(sbyte); }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(long value) { return default(sbyte); }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(object value) { return default(sbyte); }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(object value, System.IFormatProvider provider) { return default(sbyte); }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(sbyte value) { return default(sbyte); }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(float value) { return default(sbyte); }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(string value) { return default(sbyte); }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(string value, System.IFormatProvider provider) { return default(sbyte); }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(string value, int fromBase) { return default(sbyte); }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(ushort value) { return default(sbyte); }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(uint value) { return default(sbyte); }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(ulong value) { return default(sbyte); }
        public static float ToSingle(bool value) { return default(float); }
        public static float ToSingle(byte value) { return default(float); }
        public static float ToSingle(decimal value) { return default(float); }
        public static float ToSingle(double value) { return default(float); }
        public static float ToSingle(short value) { return default(float); }
        public static float ToSingle(int value) { return default(float); }
        public static float ToSingle(long value) { return default(float); }
        public static float ToSingle(object value) { return default(float); }
        public static float ToSingle(object value, System.IFormatProvider provider) { return default(float); }
        [System.CLSCompliantAttribute(false)]
        public static float ToSingle(sbyte value) { return default(float); }
        public static float ToSingle(float value) { return default(float); }
        public static float ToSingle(string value) { return default(float); }
        public static float ToSingle(string value, System.IFormatProvider provider) { return default(float); }
        [System.CLSCompliantAttribute(false)]
        public static float ToSingle(ushort value) { return default(float); }
        [System.CLSCompliantAttribute(false)]
        public static float ToSingle(uint value) { return default(float); }
        [System.CLSCompliantAttribute(false)]
        public static float ToSingle(ulong value) { return default(float); }
        public static string ToString(bool value) { return default(string); }
        public static string ToString(bool value, System.IFormatProvider provider) { return default(string); }
        public static string ToString(byte value) { return default(string); }
        public static string ToString(byte value, System.IFormatProvider provider) { return default(string); }
        public static string ToString(byte value, int toBase) { return default(string); }
        public static string ToString(char value) { return default(string); }
        public static string ToString(char value, System.IFormatProvider provider) { return default(string); }
        public static string ToString(System.DateTime value) { return default(string); }
        public static string ToString(System.DateTime value, System.IFormatProvider provider) { return default(string); }
        public static string ToString(decimal value) { return default(string); }
        public static string ToString(decimal value, System.IFormatProvider provider) { return default(string); }
        public static string ToString(double value) { return default(string); }
        public static string ToString(double value, System.IFormatProvider provider) { return default(string); }
        public static string ToString(short value) { return default(string); }
        public static string ToString(short value, System.IFormatProvider provider) { return default(string); }
        public static string ToString(short value, int toBase) { return default(string); }
        public static string ToString(int value) { return default(string); }
        public static string ToString(int value, System.IFormatProvider provider) { return default(string); }
        public static string ToString(int value, int toBase) { return default(string); }
        public static string ToString(long value) { return default(string); }
        public static string ToString(long value, System.IFormatProvider provider) { return default(string); }
        public static string ToString(long value, int toBase) { return default(string); }
        public static string ToString(object value) { return default(string); }
        public static string ToString(object value, System.IFormatProvider provider) { return default(string); }
        [System.CLSCompliantAttribute(false)]
        public static string ToString(sbyte value) { return default(string); }
        [System.CLSCompliantAttribute(false)]
        public static string ToString(sbyte value, System.IFormatProvider provider) { return default(string); }
        public static string ToString(float value) { return default(string); }
        public static string ToString(float value, System.IFormatProvider provider) { return default(string); }
        [System.CLSCompliantAttribute(false)]
        public static string ToString(ushort value) { return default(string); }
        [System.CLSCompliantAttribute(false)]
        public static string ToString(ushort value, System.IFormatProvider provider) { return default(string); }
        [System.CLSCompliantAttribute(false)]
        public static string ToString(uint value) { return default(string); }
        [System.CLSCompliantAttribute(false)]
        public static string ToString(uint value, System.IFormatProvider provider) { return default(string); }
        [System.CLSCompliantAttribute(false)]
        public static string ToString(ulong value) { return default(string); }
        [System.CLSCompliantAttribute(false)]
        public static string ToString(ulong value, System.IFormatProvider provider) { return default(string); }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(bool value) { return default(ushort); }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(byte value) { return default(ushort); }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(char value) { return default(ushort); }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(decimal value) { return default(ushort); }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(double value) { return default(ushort); }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(short value) { return default(ushort); }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(int value) { return default(ushort); }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(long value) { return default(ushort); }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(object value) { return default(ushort); }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(object value, System.IFormatProvider provider) { return default(ushort); }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(sbyte value) { return default(ushort); }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(float value) { return default(ushort); }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(string value) { return default(ushort); }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(string value, System.IFormatProvider provider) { return default(ushort); }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(string value, int fromBase) { return default(ushort); }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(ushort value) { return default(ushort); }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(uint value) { return default(ushort); }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUInt16(ulong value) { return default(ushort); }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(bool value) { return default(uint); }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(byte value) { return default(uint); }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(char value) { return default(uint); }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(decimal value) { return default(uint); }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(double value) { return default(uint); }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(short value) { return default(uint); }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(int value) { return default(uint); }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(long value) { return default(uint); }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(object value) { return default(uint); }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(object value, System.IFormatProvider provider) { return default(uint); }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(sbyte value) { return default(uint); }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(float value) { return default(uint); }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(string value) { return default(uint); }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(string value, System.IFormatProvider provider) { return default(uint); }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(string value, int fromBase) { return default(uint); }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(ushort value) { return default(uint); }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(uint value) { return default(uint); }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInt32(ulong value) { return default(uint); }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(bool value) { return default(ulong); }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(byte value) { return default(ulong); }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(char value) { return default(ulong); }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(decimal value) { return default(ulong); }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(double value) { return default(ulong); }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(short value) { return default(ulong); }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(int value) { return default(ulong); }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(long value) { return default(ulong); }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(object value) { return default(ulong); }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(object value, System.IFormatProvider provider) { return default(ulong); }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(sbyte value) { return default(ulong); }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(float value) { return default(ulong); }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(string value) { return default(ulong); }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(string value, System.IFormatProvider provider) { return default(ulong); }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(string value, int fromBase) { return default(ulong); }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(ushort value) { return default(ulong); }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(uint value) { return default(ulong); }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToUInt64(ulong value) { return default(ulong); }
    }
    public static partial class Environment
    {
        public static int CurrentManagedThreadId { get { return default(int); } }
        public static bool HasShutdownStarted { get { return default(bool); } }
        public static string MachineName { get { return default(string); } }
        public static string NewLine { get { return default(string); } }
        public static int ProcessorCount { get { return default(int); } }
        public static string StackTrace { get { return default(string); } }
        public static int TickCount { get { return default(int); } }
        public static string ExpandEnvironmentVariables(string name) { return default(string); }
        public static void Exit(int exitCode) {}
        [System.Security.SecurityCriticalAttribute]
        public static void FailFast(string message) { }
        [System.Security.SecurityCriticalAttribute]
        public static void FailFast(string message, System.Exception exception) { }
        public static string GetEnvironmentVariable(string variable) { return default(string); }
        public static System.Collections.IDictionary GetEnvironmentVariables() { return default(System.Collections.IDictionary); }
        public static void SetEnvironmentVariable(string variable, string value) { }
        public static string[] GetCommandLineArgs() { return default(string[]); }
    }
    public static partial class Math
    {
        public static decimal Abs(decimal value) { return default(decimal); }
        public static double Abs(double value) { return default(double); }
        public static short Abs(short value) { return default(short); }
        public static int Abs(int value) { return default(int); }
        public static long Abs(long value) { return default(long); }
        [System.CLSCompliantAttribute(false)]
        public static sbyte Abs(sbyte value) { return default(sbyte); }
        public static float Abs(float value) { return default(float); }
        public static double Acos(double d) { return default(double); }
        public static double Asin(double d) { return default(double); }
        public static double Atan(double d) { return default(double); }
        public static double Atan2(double y, double x) { return default(double); }
        public static decimal Ceiling(decimal d) { return default(decimal); }
        public static double Ceiling(double a) { return default(double); }
        public static double Cos(double d) { return default(double); }
        public static double Cosh(double value) { return default(double); }
        public static double Exp(double d) { return default(double); }
        public static decimal Floor(decimal d) { return default(decimal); }
        public static double Floor(double d) { return default(double); }
        public static double IEEERemainder(double x, double y) { return default(double); }
        public static double Log(double d) { return default(double); }
        public static double Log(double a, double newBase) { return default(double); }
        public static double Log10(double d) { return default(double); }
        public static byte Max(byte val1, byte val2) { return default(byte); }
        public static decimal Max(decimal val1, decimal val2) { return default(decimal); }
        public static double Max(double val1, double val2) { return default(double); }
        public static short Max(short val1, short val2) { return default(short); }
        public static int Max(int val1, int val2) { return default(int); }
        public static long Max(long val1, long val2) { return default(long); }
        [System.CLSCompliantAttribute(false)]
        public static sbyte Max(sbyte val1, sbyte val2) { return default(sbyte); }
        public static float Max(float val1, float val2) { return default(float); }
        [System.CLSCompliantAttribute(false)]
        public static ushort Max(ushort val1, ushort val2) { return default(ushort); }
        [System.CLSCompliantAttribute(false)]
        public static uint Max(uint val1, uint val2) { return default(uint); }
        [System.CLSCompliantAttribute(false)]
        public static ulong Max(ulong val1, ulong val2) { return default(ulong); }
        public static byte Min(byte val1, byte val2) { return default(byte); }
        public static decimal Min(decimal val1, decimal val2) { return default(decimal); }
        public static double Min(double val1, double val2) { return default(double); }
        public static short Min(short val1, short val2) { return default(short); }
        public static int Min(int val1, int val2) { return default(int); }
        public static long Min(long val1, long val2) { return default(long); }
        [System.CLSCompliantAttribute(false)]
        public static sbyte Min(sbyte val1, sbyte val2) { return default(sbyte); }
        public static float Min(float val1, float val2) { return default(float); }
        [System.CLSCompliantAttribute(false)]
        public static ushort Min(ushort val1, ushort val2) { return default(ushort); }
        [System.CLSCompliantAttribute(false)]
        public static uint Min(uint val1, uint val2) { return default(uint); }
        [System.CLSCompliantAttribute(false)]
        public static ulong Min(ulong val1, ulong val2) { return default(ulong); }
        public static double Pow(double x, double y) { return default(double); }
        public static decimal Round(decimal d) { return default(decimal); }
        public static decimal Round(decimal d, int decimals) { return default(decimal); }
        public static decimal Round(decimal d, int decimals, System.MidpointRounding mode) { return default(decimal); }
        public static decimal Round(decimal d, System.MidpointRounding mode) { return default(decimal); }
        public static double Round(double a) { return default(double); }
        public static double Round(double value, int digits) { return default(double); }
        public static double Round(double value, int digits, System.MidpointRounding mode) { return default(double); }
        public static double Round(double value, System.MidpointRounding mode) { return default(double); }
        public static int Sign(decimal value) { return default(int); }
        public static int Sign(double value) { return default(int); }
        public static int Sign(short value) { return default(int); }
        public static int Sign(int value) { return default(int); }
        public static int Sign(long value) { return default(int); }
        [System.CLSCompliantAttribute(false)]
        public static int Sign(sbyte value) { return default(int); }
        public static int Sign(float value) { return default(int); }
        public static double Sin(double a) { return default(double); }
        public static double Sinh(double value) { return default(double); }
        public static double Sqrt(double d) { return default(double); }
        public static double Tan(double a) { return default(double); }
        public static double Tanh(double value) { return default(double); }
        public static decimal Truncate(decimal d) { return default(decimal); }
        public static double Truncate(double d) { return default(double); }
    }
    public enum MidpointRounding
    {
        AwayFromZero = 1,
        ToEven = 0,
    }
    public partial class Progress<T> : System.IProgress<T>
    {
        public Progress() { }
        public Progress(System.Action<T> handler) { }
        public event System.EventHandler<T> ProgressChanged { add { } remove { } }
        protected virtual void OnReport(T value) { }
        void System.IProgress<T>.Report(T value) { }
    }
    public partial class Random
    {
        public Random() { }
        public Random(int Seed) { }
        public virtual int Next() { return default(int); }
        public virtual int Next(int maxValue) { return default(int); }
        public virtual int Next(int minValue, int maxValue) { return default(int); }
        public virtual void NextBytes(byte[] buffer) { }
        public virtual double NextDouble() { return default(double); }
        protected virtual double Sample() { return default(double); }
    }
    public abstract partial class StringComparer : System.Collections.Generic.IComparer<string>, System.Collections.Generic.IEqualityComparer<string>, System.Collections.IComparer, System.Collections.IEqualityComparer
    {
        protected StringComparer() { }
        public static System.StringComparer CurrentCulture { get { return default(System.StringComparer); } }
        public static System.StringComparer CurrentCultureIgnoreCase { get { return default(System.StringComparer); } }
        public static System.StringComparer Ordinal { get { return default(System.StringComparer); } }
        public static System.StringComparer OrdinalIgnoreCase { get { return default(System.StringComparer); } }
        public abstract int Compare(string x, string y);
        public abstract bool Equals(string x, string y);
        public abstract int GetHashCode(string obj);
        int System.Collections.IComparer.Compare(object x, object y) { return default(int); }
        bool System.Collections.IEqualityComparer.Equals(object x, object y) { return default(bool); }
        int System.Collections.IEqualityComparer.GetHashCode(object obj) { return default(int); }
    }
    public partial class UriBuilder
    {
        public UriBuilder() { }
        public UriBuilder(string uri) { }
        public UriBuilder(string schemeName, string hostName) { }
        public UriBuilder(string scheme, string host, int portNumber) { }
        public UriBuilder(string scheme, string host, int port, string pathValue) { }
        public UriBuilder(string scheme, string host, int port, string path, string extraValue) { }
        public UriBuilder(System.Uri uri) { }
        public string Fragment { get { return default(string); } set { } }
        public string Host { get { return default(string); } set { } }
        public string Password { get { return default(string); } set { } }
        public string Path { get { return default(string); } set { } }
        public int Port { get { return default(int); } set { } }
        public string Query { get { return default(string); } set { } }
        public string Scheme { get { return default(string); } set { } }
        public System.Uri Uri { get { return default(System.Uri); } }
        public string UserName { get { return default(string); } set { } }
        public override bool Equals(object rparam) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public override string ToString() { return default(string); }
    }
}
namespace System.Diagnostics
{
    public partial class Stopwatch
    {
        public static readonly long Frequency;
        public static readonly bool IsHighResolution;
        public Stopwatch() { }
        public System.TimeSpan Elapsed { get { return default(System.TimeSpan); } }
        public long ElapsedMilliseconds { get { return default(long); } }
        public long ElapsedTicks { get { return default(long); } }
        public bool IsRunning { get { return default(bool); } }
        public static long GetTimestamp() { return default(long); }
        public void Reset() { }
        public void Restart() { }
        public void Start() { }
        public static System.Diagnostics.Stopwatch StartNew() { return default(System.Diagnostics.Stopwatch); }
        public void Stop() { }
    }
}
namespace System.IO
{
    public static partial class Path
    {
        public static readonly char AltDirectorySeparatorChar;
        public static readonly char DirectorySeparatorChar;
        public static readonly char PathSeparator;
        public static readonly char VolumeSeparatorChar;
        public static string ChangeExtension(string path, string extension) { return default(string); }
        public static string Combine(string path1, string path2) { return default(string); }
        public static string Combine(string path1, string path2, string path3) { return default(string); }
        public static string Combine(params string[] paths) { return default(string); }
        public static string GetDirectoryName(string path) { return default(string); }
        public static string GetExtension(string path) { return default(string); }
        public static string GetFileName(string path) { return default(string); }
        public static string GetFileNameWithoutExtension(string path) { return default(string); }
        public static string GetFullPath(string path) { return default(string); }
        public static char[] GetInvalidFileNameChars() { return default(char[]); }
        public static char[] GetInvalidPathChars() { return default(char[]); }
        public static string GetPathRoot(string path) { return default(string); }
        public static string GetRandomFileName() { return default(string); }
        public static string GetTempFileName() { return default(string); }
        public static string GetTempPath() { return default(string); }
        public static bool HasExtension(string path) { return default(bool); }
        public static bool IsPathRooted(string path) { return default(bool); }
    }
}
namespace System.Net
{
    public static partial class WebUtility
    {
        public static string HtmlDecode(string value) { return default(string); }
        public static string HtmlEncode(string value) { return default(string); }
        public static string UrlDecode(string encodedValue) { return default(string); }
        public static byte[] UrlDecodeToBytes(byte[] encodedValue, int offset, int count) { return default(byte[]); }
        public static string UrlEncode(string value) { return default(string); }
        public static byte[] UrlEncodeToBytes(byte[] value, int offset, int count) { return default(byte[]); }
    }
}
namespace System.Runtime.Versioning
{
    public sealed partial class FrameworkName : System.IEquatable<System.Runtime.Versioning.FrameworkName>
    {
        public FrameworkName(string frameworkName) { }
        public FrameworkName(string identifier, System.Version version) { }
        public FrameworkName(string identifier, System.Version version, string profile) { }
        public string FullName { get { return default(string); } }
        public string Identifier { get { return default(string); } }
        public string Profile { get { return default(string); } }
        public System.Version Version { get { return default(System.Version); } }
        public override bool Equals(object obj) { return default(bool); }
        public bool Equals(System.Runtime.Versioning.FrameworkName other) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static bool operator ==(System.Runtime.Versioning.FrameworkName left, System.Runtime.Versioning.FrameworkName right) { return default(bool); }
        public static bool operator !=(System.Runtime.Versioning.FrameworkName left, System.Runtime.Versioning.FrameworkName right) { return default(bool); }
        public override string ToString() { return default(string); }
    }
}
