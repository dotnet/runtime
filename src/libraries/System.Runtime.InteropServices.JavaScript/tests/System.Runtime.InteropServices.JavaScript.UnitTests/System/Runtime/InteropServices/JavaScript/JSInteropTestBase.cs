// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading;
using Xunit;
using System.Diagnostics.CodeAnalysis;
#pragma warning disable xUnit1026 // Theory methods should use all of their parameters

namespace System.Runtime.InteropServices.JavaScript.Tests
{
    public class JSInteropTestBase
    {
        public static IEnumerable<object[]> MarshalCharCases()
        {
            yield return new object[] { (char)42 };
            yield return new object[] { (char)1 };
            yield return new object[] { '\u017D' };
            yield return new object[] { '\u2661' };
            yield return new object[] { char.MaxValue };
            yield return new object[] { char.MinValue };
        }

        public static IEnumerable<object[]> MarshalByteCases()
        {
            yield return new object[] { (byte)42 };
            yield return new object[] { (byte)1 };
            yield return new object[] { byte.MaxValue };
            yield return new object[] { byte.MinValue };
        }

        public static IEnumerable<object[]> MarshalInt16Cases()
        {
            yield return new object[] { 42 };
            yield return new object[] { 0 };
            yield return new object[] { 1 };
            yield return new object[] { -1 };
            yield return new object[] { short.MaxValue };
            yield return new object[] { short.MinValue };
        }

        public static IEnumerable<object[]> MarshalInt32Cases()
        {
            yield return new object[] { 42 };
            yield return new object[] { 0 };
            yield return new object[] { 1 };
            yield return new object[] { -1 };
            yield return new object[] { int.MaxValue };
            yield return new object[] { int.MinValue };
        }

        public static IEnumerable<object[]> OutOfRangeCases()
        {
            yield return new object[] { double.MaxValue, "Value is not an integer" };
            yield return new object[] { double.MinValue, "Value is not an integer" };
            yield return new object[] { double.NaN, "Value is not an integer" };
            yield return new object[] { double.NegativeInfinity, "Value is not an integer" };
            yield return new object[] { double.PositiveInfinity, "Value is not an integer" };
            yield return new object[] { (double)MAX_SAFE_INTEGER, "Overflow" };
        }

        const long MAX_SAFE_INTEGER = 9007199254740991L;// Number.MAX_SAFE_INTEGER
        const long MIN_SAFE_INTEGER = -9007199254740991L;// Number.MIN_SAFE_INTEGER
        public static IEnumerable<object[]> MarshalInt52Cases()
        {
            yield return new object[] { -1 };
            yield return new object[] { 42 };
            yield return new object[] { 0 };
            yield return new object[] { 1 };
            yield return new object[] { MAX_SAFE_INTEGER };
            yield return new object[] { MIN_SAFE_INTEGER };
        }

        public static IEnumerable<object[]> MarshalBigInt64Cases()
        {
            yield return new object[] { -1 };
            yield return new object[] { 42 };
            yield return new object[] { 0 };
            yield return new object[] { 1 };
            yield return new object[] { MAX_SAFE_INTEGER };
            yield return new object[] { MIN_SAFE_INTEGER };
            yield return new object[] { long.MinValue };
            yield return new object[] { long.MaxValue };
        }

        public static IEnumerable<object[]> MarshalDoubleCases()
        {
            yield return new object[] { Math.PI };
            yield return new object[] { 0.0 };
            yield return new object[] { double.MaxValue };
            yield return new object[] { double.MinValue };
            yield return new object[] { double.NegativeInfinity };
            yield return new object[] { double.PositiveInfinity };
            yield return new object[] { double.NaN };
        }

        public static IEnumerable<object[]> MarshalSingleCases()
        {
            yield return new object[] { (float)Math.PI };
            yield return new object[] { 0.0f };
            yield return new object[] { float.MaxValue };
            yield return new object[] { float.MinValue };
            yield return new object[] { float.NegativeInfinity };
            yield return new object[] { float.PositiveInfinity };
            yield return new object[] { float.NaN };
        }

        public static IEnumerable<object[]> MarshalIntPtrCases()
        {
            yield return new object[] { (IntPtr)42 };
            yield return new object[] { IntPtr.Zero };
            yield return new object[] { (IntPtr)1 };
            yield return new object[] { (IntPtr)(-1) };
            yield return new object[] { IntPtr.MaxValue };
            yield return new object[] { IntPtr.MinValue };
        }

        public static IEnumerable<object[]> MarshalDateTimeCases()
        {
            yield return new object[] { new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
            yield return new object[] { TrimNano(DateTime.UtcNow) };
            yield return new object[] { TrimNano(DateTime.MaxValue) };
        }

        public static IEnumerable<object[]> MarshalDateTimeOffsetCases()
        {
            yield return new object[] { DateTimeOffset.FromUnixTimeSeconds(0) };
            yield return new object[] { TrimNano(DateTimeOffset.UtcNow) };
            yield return new object[] { TrimNano(DateTimeOffset.MaxValue) };
        }

        public static IEnumerable<object[]> MarshalByteArrayCases()
        {
            yield return new object[] { new byte[] { 1, 2, 3, byte.MaxValue, byte.MinValue } };
            yield return new object[] { new byte[] { } };
            yield return new object[] { null };
        }

        public static IEnumerable<object[]> MarshalIntArrayCases()
        {
            yield return new object[] { new int[] { 1, 2, 3, int.MaxValue, int.MinValue } };
            yield return new object[] { new int[] { } };
            yield return new object[] { null };
        }

        public static IEnumerable<object[]> MarshalDoubleArrayCases()
        {
            yield return new object[] { new double[] { 1, 2, 3, double.MaxValue, double.MinValue, double.Pi, double.NegativeInfinity, double.PositiveInfinity, double.NaN } };
            yield return new object[] { new double[] { } };
            yield return new object[] { null };
        }

        public static IEnumerable<object[]> MarshalStringArrayCases()
        {
            yield return new object[] { new string[] { "\u0050\u0159\u00ed\u006c\u0069\u0161", "\u017e\u006c\u0075\u0165\u006f\u0075\u010d\u006b\u00fd" } };
            yield return new object[] { new string[] { string.Intern("hello"), string.Empty, null } };
            yield return new object[] { new string[] { } };
            yield return new object[] { null };
        }

        public static IEnumerable<object[]> MarshalBooleanCases()
        {
            yield return new object[] { true };
            yield return new object[] { false };
        }

        public static IEnumerable<object[]> MarshalObjectArrayCasesToDouble()
        {
            yield return new object[] { new object[] { (byte)42 } };
            yield return new object[] { new object[] { (short)42 } };
            yield return new object[] { new object[] { 42 } };
            yield return new object[] { new object[] { 3.14f } };
            yield return new object[] { new object[] { 'A' } };
        }

        protected delegate void dummyDelegate();
        protected static void dummyDelegateA()
        {
        }

        public class SomethingRef
        {
        }

        public class SomethingStruct
        {
        }

        public static IEnumerable<object[]> MarshalObjectArrayCasesThrow()
        {
            yield return new object[] { new object[] { () => { } } };
            yield return new object[] { new object[] { (int a) => { } } };
            yield return new object[] { new object[] { (int a) => { return a; } } };
            yield return new object[] { new object[] { (dummyDelegate)dummyDelegateA } };
            yield return new object[] { new object[] { 0L } };
            yield return new object[] { new object[] { 0UL } };
            yield return new object[] { new object[] { (sbyte)0 } };
            yield return new object[] { new object[] { (ushort)0 } };
            yield return new object[] { new object[] { new SomethingStruct[] { } } };
            yield return new object[] { new object[] { new SomethingRef[] { }, } };
            yield return new object[] { new object[] { new ArraySegment<byte>(new byte[] { 11 }), } };
        }

        public static IEnumerable<object[]> MarshalObjectArrayCases()
        {
            yield return new object[] { new object[] { string.Intern("hello"), string.Empty } };
            yield return new object[] { new object[] { 1.1d, new DateTime(2022, 5, 8, 14, 55, 01, DateTimeKind.Utc), false, true } };
            yield return new object[] { new object[] { new double?(1.1d), new DateTime?(new DateTime(2022, 5, 8, 14, 55, 01, DateTimeKind.Utc)), new bool?(false), new bool?(true) } };
            yield return new object[] { new object[] { null, new object(), new SomethingRef(), new SomethingStruct(), new Exception("test") } };
            yield return new object[] { new object[] { "JSData" } }; // special cased, so we call createData in the test itself
            yield return new object[] { new object[] { new byte[] { }, new int[] { }, new double[] { }, new string[] { }, new object[] { } } };
            yield return new object[] { new object[] { new byte[] { 1, 2, 3 }, new int[] { 1, 2, 3 }, new double[] { 1, 2, 3 }, new string[] { "a", "b", "c" }, new object[] { } } };
            yield return new object[] { new object[] { new object[] { new byte[] { 1, 2, 3 }, new int[] { 1, 2, 3 }, new double[] { 1, 2, 3 }, new string[] { "a", "b", "c" }, new object(), new SomethingRef(), new SomethingStruct(), new Exception("test") } } };
            yield return new object[] { new object[] { } };
            yield return new object[] { null };
        }

        public static IEnumerable<object[]> MarshalNullableBooleanCases()
        {
            yield return new object[] { null };
            yield return new object[] { true };
            yield return new object[] { false };
        }

        public static IEnumerable<object[]> MarshalNullableInt32Cases()
        {
            yield return new object[] { null };
            yield return new object[] { 42 };
            yield return new object[] { 0 };
            yield return new object[] { 1 };
            yield return new object[] { -1 };
            yield return new object[] { int.MaxValue };
            yield return new object[] { int.MinValue };
        }

        public static IEnumerable<object[]> MarshalNullableBigInt64Cases()
        {
            yield return new object[] { null };
            yield return new object[] { 42L };
            yield return new object[] { 0L };
            yield return new object[] { 1L };
            yield return new object[] { -1L };
            yield return new object[] { MAX_SAFE_INTEGER };
            yield return new object[] { MIN_SAFE_INTEGER };
            yield return new object[] { long.MaxValue };
            yield return new object[] { long.MinValue };
        }

        public static IEnumerable<object[]> MarshalNullableIntPtrCases()
        {
            yield return new object[] { null };
            yield return new object[] { (IntPtr)42 };
            yield return new object[] { IntPtr.Zero };
            yield return new object[] { (IntPtr)1 };
            yield return new object[] { (IntPtr)(-1) };
            yield return new object[] { IntPtr.MaxValue };
            yield return new object[] { IntPtr.MinValue };
        }

        public static IEnumerable<object[]> MarshalNullableDoubleCases()
        {
            yield return new object[] { null };
            yield return new object[] { Math.PI };
            yield return new object[] { 0.0 };
            yield return new object[] { double.MaxValue };
            yield return new object[] { double.MinValue };
            yield return new object[] { double.NegativeInfinity };
            yield return new object[] { double.PositiveInfinity };
            yield return new object[] { double.NaN };
        }

        public static IEnumerable<object[]> MarshalNullableDateTimeCases()
        {
            yield return new object[] { null };
            yield return new object[] { new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
            yield return new object[] { TrimNano(DateTime.UtcNow) };
            yield return new object[] { TrimNano(DateTime.MaxValue) };
        }

        public static IEnumerable<object[]> MarshalStringCases()
        {
            yield return new object[] { null };
            yield return new object[] { string.Empty };
            yield return new object[] { "Ahoj" + Random.Shared.Next() };// shorted than 256 -> check in JS interned
            yield return new object[] { "Ahoj" + new string('!', 300) };// longer than 256 -> no check in JS interned
            yield return new object[] { string.Intern("dotnet") };
        }

        public static IEnumerable<object[]> MarshalObjectCases()
        {
            yield return new object[] { new object(), "ManagedObject" };
            yield return new object[] { null, null };
        }

        public static IEnumerable<object[]> MarshalExceptionCases()
        {
            yield return new object[] { new Exception("Test"), "ManagedError" };
            yield return new object[] { null, "JSTestError" };
            yield return new object[] { null, null };
        }

        public static IEnumerable<object[]> MarshalIJSObjectCases()
        {
            yield return new object[] { null, "JSData" };
            yield return new object[] { null, null };
        }

        public static IEnumerable<object[]> TaskCases()
        {
            yield return new object[] { Math.PI };
            yield return new object[] { 0 };
            yield return new object[] { "test" };
            yield return new object[] { null };
        }

        public async Task InitializeAsync()
        {
            await JavaScriptTestHelper.InitializeAsync();
        }

        public async Task DisposeAsync()
        {
            await JavaScriptTestHelper.DisposeAsync();
        }

        // js Date doesn't have nanosecond precision
        public static DateTime TrimNano(DateTime date)
        {
            return new DateTime(date.Ticks - (date.Ticks % TimeSpan.TicksPerMillisecond), DateTimeKind.Utc);
        }

        public static DateTimeOffset TrimNano(DateTimeOffset date)
        {
            return new DateTime(date.Ticks - (date.Ticks % TimeSpan.TicksPerMillisecond), DateTimeKind.Utc);
        }
    }
}
