// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

public class GitHub_17073
{
    [MethodImpl(MethodImplOptions.NoInlining)] static bool True() => true;
    [MethodImpl(MethodImplOptions.NoInlining)] static bool False() => false;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Check(bool expected, bool actual, [CallerLineNumber] int line = 0)
    {
        if (expected != actual) Console.WriteLine("Failed at line {0}", line);
        return expected == actual;
    }

    [Fact]
    public static void Test()
    {
        bool r = true;
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedEqual_Normal(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedEqual_Normal(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedEqual_Normal(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedEqual_Normal(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedEqual_Normal(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedEqual_LogicalNot(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedEqual_LogicalNot(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedEqual_LogicalNot(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedEqual_LogicalNot(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedEqual_LogicalNot(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedEqual_Branch(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedEqual_Branch(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedEqual_Branch(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedEqual_Branch(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedEqual_Branch(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedEqual_Swap(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedEqual_Swap(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedEqual_Swap(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedEqual_Swap(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedEqual_Swap(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedEqual_Branch_Swap(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedEqual_Branch_Swap(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedEqual_Branch_Swap(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedEqual_Branch_Swap(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedEqual_Branch_Swap(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedEqual_Normal(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedEqual_Normal(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedEqual_Normal(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedEqual_Normal(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedEqual_Normal(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedEqual_LogicalNot(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedEqual_LogicalNot(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedEqual_LogicalNot(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedEqual_LogicalNot(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedEqual_LogicalNot(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedEqual_Branch(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedEqual_Branch(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedEqual_Branch(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedEqual_Branch(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedEqual_Branch(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedEqual_Swap(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedEqual_Swap(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedEqual_Swap(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedEqual_Swap(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedEqual_Swap(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedEqual_Branch_Swap(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedEqual_Branch_Swap(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedEqual_Branch_Swap(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedEqual_Branch_Swap(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedEqual_Branch_Swap(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedNotEqual_Normal(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedNotEqual_Normal(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedNotEqual_Normal(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedNotEqual_Normal(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedNotEqual_Normal(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedNotEqual_LogicalNot(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedNotEqual_LogicalNot(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedNotEqual_LogicalNot(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedNotEqual_LogicalNot(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedNotEqual_LogicalNot(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedNotEqual_Branch(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedNotEqual_Branch(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedNotEqual_Branch(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedNotEqual_Branch(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedNotEqual_Branch(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedNotEqual_Swap(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedNotEqual_Swap(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedNotEqual_Swap(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedNotEqual_Swap(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedNotEqual_Swap(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedNotEqual_Branch_Swap(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedNotEqual_Branch_Swap(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedNotEqual_Branch_Swap(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedNotEqual_Branch_Swap(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedNotEqual_Branch_Swap(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedNotEqual_Normal(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedNotEqual_Normal(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedNotEqual_Normal(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedNotEqual_Normal(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedNotEqual_Normal(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedNotEqual_LogicalNot(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedNotEqual_LogicalNot(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedNotEqual_LogicalNot(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedNotEqual_LogicalNot(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedNotEqual_LogicalNot(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedNotEqual_Branch(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedNotEqual_Branch(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedNotEqual_Branch(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedNotEqual_Branch(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedNotEqual_Branch(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedNotEqual_Swap(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedNotEqual_Swap(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedNotEqual_Swap(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedNotEqual_Swap(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedNotEqual_Swap(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedNotEqual_Branch_Swap(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedNotEqual_Branch_Swap(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedNotEqual_Branch_Swap(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedNotEqual_Branch_Swap(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedNotEqual_Branch_Swap(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedLessThan_Normal(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedLessThan_Normal(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedLessThan_Normal(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedLessThan_Normal(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedLessThan_Normal(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedLessThan_LogicalNot(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedLessThan_LogicalNot(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedLessThan_LogicalNot(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedLessThan_LogicalNot(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedLessThan_LogicalNot(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedLessThan_Branch(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedLessThan_Branch(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedLessThan_Branch(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedLessThan_Branch(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedLessThan_Branch(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedLessThan_Swap(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedLessThan_Swap(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedLessThan_Swap(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedLessThan_Swap(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedLessThan_Swap(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedLessThan_Branch_Swap(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedLessThan_Branch_Swap(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedLessThan_Branch_Swap(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedLessThan_Branch_Swap(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedLessThan_Branch_Swap(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedLessThan_Normal(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedLessThan_Normal(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedLessThan_Normal(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedLessThan_Normal(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedLessThan_Normal(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedLessThan_LogicalNot(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedLessThan_LogicalNot(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedLessThan_LogicalNot(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedLessThan_LogicalNot(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedLessThan_LogicalNot(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedLessThan_Branch(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedLessThan_Branch(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedLessThan_Branch(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedLessThan_Branch(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedLessThan_Branch(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedLessThan_Swap(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedLessThan_Swap(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedLessThan_Swap(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedLessThan_Swap(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedLessThan_Swap(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedLessThan_Branch_Swap(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedLessThan_Branch_Swap(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedLessThan_Branch_Swap(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedLessThan_Branch_Swap(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedLessThan_Branch_Swap(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedLessThanOrEqual_Normal(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedLessThanOrEqual_Normal(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedLessThanOrEqual_Normal(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedLessThanOrEqual_Normal(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedLessThanOrEqual_Normal(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedLessThanOrEqual_LogicalNot(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedLessThanOrEqual_LogicalNot(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedLessThanOrEqual_LogicalNot(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedLessThanOrEqual_LogicalNot(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedLessThanOrEqual_LogicalNot(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedLessThanOrEqual_Branch(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedLessThanOrEqual_Branch(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedLessThanOrEqual_Branch(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedLessThanOrEqual_Branch(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedLessThanOrEqual_Branch(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedLessThanOrEqual_Swap(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedLessThanOrEqual_Swap(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedLessThanOrEqual_Swap(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedLessThanOrEqual_Swap(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedLessThanOrEqual_Swap(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedLessThanOrEqual_Branch_Swap(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedLessThanOrEqual_Branch_Swap(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedLessThanOrEqual_Branch_Swap(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedLessThanOrEqual_Branch_Swap(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedLessThanOrEqual_Branch_Swap(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedLessThanOrEqual_Normal(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedLessThanOrEqual_Normal(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedLessThanOrEqual_Normal(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedLessThanOrEqual_Normal(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedLessThanOrEqual_Normal(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedLessThanOrEqual_LogicalNot(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedLessThanOrEqual_LogicalNot(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedLessThanOrEqual_LogicalNot(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedLessThanOrEqual_LogicalNot(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedLessThanOrEqual_LogicalNot(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedLessThanOrEqual_Branch(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedLessThanOrEqual_Branch(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedLessThanOrEqual_Branch(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedLessThanOrEqual_Branch(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedLessThanOrEqual_Branch(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedLessThanOrEqual_Swap(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedLessThanOrEqual_Swap(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedLessThanOrEqual_Swap(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedLessThanOrEqual_Swap(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedLessThanOrEqual_Swap(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedLessThanOrEqual_Branch_Swap(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedLessThanOrEqual_Branch_Swap(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedLessThanOrEqual_Branch_Swap(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedLessThanOrEqual_Branch_Swap(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedLessThanOrEqual_Branch_Swap(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedGreaterThan_Normal(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedGreaterThan_Normal(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedGreaterThan_Normal(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedGreaterThan_Normal(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedGreaterThan_Normal(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedGreaterThan_LogicalNot(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedGreaterThan_LogicalNot(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedGreaterThan_LogicalNot(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedGreaterThan_LogicalNot(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedGreaterThan_LogicalNot(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedGreaterThan_Branch(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedGreaterThan_Branch(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedGreaterThan_Branch(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedGreaterThan_Branch(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedGreaterThan_Branch(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedGreaterThan_Swap(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedGreaterThan_Swap(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedGreaterThan_Swap(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedGreaterThan_Swap(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedGreaterThan_Swap(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedGreaterThan_Branch_Swap(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedGreaterThan_Branch_Swap(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedGreaterThan_Branch_Swap(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedGreaterThan_Branch_Swap(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedGreaterThan_Branch_Swap(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedGreaterThan_Normal(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedGreaterThan_Normal(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedGreaterThan_Normal(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedGreaterThan_Normal(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedGreaterThan_Normal(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedGreaterThan_LogicalNot(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedGreaterThan_LogicalNot(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedGreaterThan_LogicalNot(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedGreaterThan_LogicalNot(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedGreaterThan_LogicalNot(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedGreaterThan_Branch(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedGreaterThan_Branch(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedGreaterThan_Branch(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedGreaterThan_Branch(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedGreaterThan_Branch(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedGreaterThan_Swap(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedGreaterThan_Swap(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedGreaterThan_Swap(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedGreaterThan_Swap(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedGreaterThan_Swap(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedGreaterThan_Branch_Swap(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedGreaterThan_Branch_Swap(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedGreaterThan_Branch_Swap(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedGreaterThan_Branch_Swap(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedGreaterThan_Branch_Swap(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedGreaterThanOrEqual_Normal(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedGreaterThanOrEqual_Normal(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedGreaterThanOrEqual_Normal(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedGreaterThanOrEqual_Normal(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedGreaterThanOrEqual_Normal(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedGreaterThanOrEqual_LogicalNot(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedGreaterThanOrEqual_LogicalNot(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedGreaterThanOrEqual_LogicalNot(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedGreaterThanOrEqual_LogicalNot(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedGreaterThanOrEqual_LogicalNot(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedGreaterThanOrEqual_Branch(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedGreaterThanOrEqual_Branch(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedGreaterThanOrEqual_Branch(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedGreaterThanOrEqual_Branch(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedGreaterThanOrEqual_Branch(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedGreaterThanOrEqual_Swap(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedGreaterThanOrEqual_Swap(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedGreaterThanOrEqual_Swap(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedGreaterThanOrEqual_Swap(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedGreaterThanOrEqual_Swap(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedGreaterThanOrEqual_Branch_Swap(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedGreaterThanOrEqual_Branch_Swap(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarOrderedGreaterThanOrEqual_Branch_Swap(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedGreaterThanOrEqual_Branch_Swap(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarOrderedGreaterThanOrEqual_Branch_Swap(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedGreaterThanOrEqual_Normal(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedGreaterThanOrEqual_Normal(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedGreaterThanOrEqual_Normal(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedGreaterThanOrEqual_Normal(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedGreaterThanOrEqual_Normal(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedGreaterThanOrEqual_LogicalNot(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedGreaterThanOrEqual_LogicalNot(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedGreaterThanOrEqual_LogicalNot(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedGreaterThanOrEqual_LogicalNot(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedGreaterThanOrEqual_LogicalNot(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedGreaterThanOrEqual_Branch(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedGreaterThanOrEqual_Branch(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedGreaterThanOrEqual_Branch(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedGreaterThanOrEqual_Branch(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedGreaterThanOrEqual_Branch(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedGreaterThanOrEqual_Swap(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedGreaterThanOrEqual_Swap(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedGreaterThanOrEqual_Swap(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedGreaterThanOrEqual_Swap(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedGreaterThanOrEqual_Swap(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedGreaterThanOrEqual_Branch_Swap(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedGreaterThanOrEqual_Branch_Swap(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarOrderedGreaterThanOrEqual_Branch_Swap(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedGreaterThanOrEqual_Branch_Swap(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarOrderedGreaterThanOrEqual_Branch_Swap(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedEqual_Normal(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedEqual_Normal(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedEqual_Normal(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedEqual_Normal(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedEqual_Normal(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedEqual_LogicalNot(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedEqual_LogicalNot(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedEqual_LogicalNot(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedEqual_LogicalNot(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedEqual_LogicalNot(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedEqual_Branch(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedEqual_Branch(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedEqual_Branch(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedEqual_Branch(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedEqual_Branch(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedEqual_Swap(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedEqual_Swap(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedEqual_Swap(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedEqual_Swap(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedEqual_Swap(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedEqual_Branch_Swap(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedEqual_Branch_Swap(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedEqual_Branch_Swap(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedEqual_Branch_Swap(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedEqual_Branch_Swap(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedEqual_Normal(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedEqual_Normal(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedEqual_Normal(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedEqual_Normal(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedEqual_Normal(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedEqual_LogicalNot(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedEqual_LogicalNot(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedEqual_LogicalNot(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedEqual_LogicalNot(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedEqual_LogicalNot(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedEqual_Branch(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedEqual_Branch(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedEqual_Branch(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedEqual_Branch(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedEqual_Branch(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedEqual_Swap(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedEqual_Swap(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedEqual_Swap(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedEqual_Swap(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedEqual_Swap(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedEqual_Branch_Swap(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedEqual_Branch_Swap(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedEqual_Branch_Swap(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedEqual_Branch_Swap(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedEqual_Branch_Swap(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedNotEqual_Normal(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedNotEqual_Normal(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedNotEqual_Normal(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedNotEqual_Normal(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedNotEqual_Normal(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedNotEqual_LogicalNot(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedNotEqual_LogicalNot(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedNotEqual_LogicalNot(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedNotEqual_LogicalNot(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedNotEqual_LogicalNot(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedNotEqual_Branch(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedNotEqual_Branch(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedNotEqual_Branch(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedNotEqual_Branch(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedNotEqual_Branch(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedNotEqual_Swap(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedNotEqual_Swap(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedNotEqual_Swap(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedNotEqual_Swap(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedNotEqual_Swap(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedNotEqual_Branch_Swap(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedNotEqual_Branch_Swap(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedNotEqual_Branch_Swap(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedNotEqual_Branch_Swap(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedNotEqual_Branch_Swap(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedNotEqual_Normal(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedNotEqual_Normal(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedNotEqual_Normal(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedNotEqual_Normal(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedNotEqual_Normal(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedNotEqual_LogicalNot(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedNotEqual_LogicalNot(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedNotEqual_LogicalNot(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedNotEqual_LogicalNot(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedNotEqual_LogicalNot(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedNotEqual_Branch(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedNotEqual_Branch(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedNotEqual_Branch(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedNotEqual_Branch(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedNotEqual_Branch(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedNotEqual_Swap(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedNotEqual_Swap(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedNotEqual_Swap(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedNotEqual_Swap(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedNotEqual_Swap(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedNotEqual_Branch_Swap(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedNotEqual_Branch_Swap(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedNotEqual_Branch_Swap(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedNotEqual_Branch_Swap(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedNotEqual_Branch_Swap(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedLessThan_Normal(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedLessThan_Normal(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedLessThan_Normal(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedLessThan_Normal(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedLessThan_Normal(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedLessThan_LogicalNot(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedLessThan_LogicalNot(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedLessThan_LogicalNot(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedLessThan_LogicalNot(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedLessThan_LogicalNot(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedLessThan_Branch(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedLessThan_Branch(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedLessThan_Branch(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedLessThan_Branch(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedLessThan_Branch(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedLessThan_Swap(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedLessThan_Swap(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedLessThan_Swap(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedLessThan_Swap(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedLessThan_Swap(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedLessThan_Branch_Swap(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedLessThan_Branch_Swap(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedLessThan_Branch_Swap(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedLessThan_Branch_Swap(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedLessThan_Branch_Swap(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedLessThan_Normal(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedLessThan_Normal(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedLessThan_Normal(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedLessThan_Normal(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedLessThan_Normal(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedLessThan_LogicalNot(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedLessThan_LogicalNot(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedLessThan_LogicalNot(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedLessThan_LogicalNot(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedLessThan_LogicalNot(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedLessThan_Branch(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedLessThan_Branch(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedLessThan_Branch(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedLessThan_Branch(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedLessThan_Branch(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedLessThan_Swap(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedLessThan_Swap(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedLessThan_Swap(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedLessThan_Swap(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedLessThan_Swap(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedLessThan_Branch_Swap(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedLessThan_Branch_Swap(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedLessThan_Branch_Swap(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedLessThan_Branch_Swap(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedLessThan_Branch_Swap(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedLessThanOrEqual_Normal(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedLessThanOrEqual_Normal(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedLessThanOrEqual_Normal(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedLessThanOrEqual_Normal(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedLessThanOrEqual_Normal(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedLessThanOrEqual_LogicalNot(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedLessThanOrEqual_LogicalNot(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedLessThanOrEqual_LogicalNot(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedLessThanOrEqual_LogicalNot(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedLessThanOrEqual_LogicalNot(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedLessThanOrEqual_Branch(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedLessThanOrEqual_Branch(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedLessThanOrEqual_Branch(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedLessThanOrEqual_Branch(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedLessThanOrEqual_Branch(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedLessThanOrEqual_Swap(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedLessThanOrEqual_Swap(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedLessThanOrEqual_Swap(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedLessThanOrEqual_Swap(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedLessThanOrEqual_Swap(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedLessThanOrEqual_Branch_Swap(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedLessThanOrEqual_Branch_Swap(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedLessThanOrEqual_Branch_Swap(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedLessThanOrEqual_Branch_Swap(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedLessThanOrEqual_Branch_Swap(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedLessThanOrEqual_Normal(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedLessThanOrEqual_Normal(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedLessThanOrEqual_Normal(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedLessThanOrEqual_Normal(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedLessThanOrEqual_Normal(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedLessThanOrEqual_LogicalNot(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedLessThanOrEqual_LogicalNot(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedLessThanOrEqual_LogicalNot(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedLessThanOrEqual_LogicalNot(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedLessThanOrEqual_LogicalNot(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedLessThanOrEqual_Branch(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedLessThanOrEqual_Branch(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedLessThanOrEqual_Branch(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedLessThanOrEqual_Branch(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedLessThanOrEqual_Branch(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedLessThanOrEqual_Swap(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedLessThanOrEqual_Swap(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedLessThanOrEqual_Swap(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedLessThanOrEqual_Swap(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedLessThanOrEqual_Swap(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedLessThanOrEqual_Branch_Swap(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedLessThanOrEqual_Branch_Swap(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedLessThanOrEqual_Branch_Swap(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedLessThanOrEqual_Branch_Swap(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedLessThanOrEqual_Branch_Swap(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedGreaterThan_Normal(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedGreaterThan_Normal(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedGreaterThan_Normal(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedGreaterThan_Normal(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedGreaterThan_Normal(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedGreaterThan_LogicalNot(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedGreaterThan_LogicalNot(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedGreaterThan_LogicalNot(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedGreaterThan_LogicalNot(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedGreaterThan_LogicalNot(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedGreaterThan_Branch(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedGreaterThan_Branch(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedGreaterThan_Branch(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedGreaterThan_Branch(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedGreaterThan_Branch(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedGreaterThan_Swap(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedGreaterThan_Swap(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedGreaterThan_Swap(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedGreaterThan_Swap(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedGreaterThan_Swap(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedGreaterThan_Branch_Swap(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedGreaterThan_Branch_Swap(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedGreaterThan_Branch_Swap(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedGreaterThan_Branch_Swap(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedGreaterThan_Branch_Swap(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedGreaterThan_Normal(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedGreaterThan_Normal(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedGreaterThan_Normal(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedGreaterThan_Normal(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedGreaterThan_Normal(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedGreaterThan_LogicalNot(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedGreaterThan_LogicalNot(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedGreaterThan_LogicalNot(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedGreaterThan_LogicalNot(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedGreaterThan_LogicalNot(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedGreaterThan_Branch(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedGreaterThan_Branch(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedGreaterThan_Branch(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedGreaterThan_Branch(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedGreaterThan_Branch(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedGreaterThan_Swap(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedGreaterThan_Swap(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedGreaterThan_Swap(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedGreaterThan_Swap(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedGreaterThan_Swap(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedGreaterThan_Branch_Swap(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedGreaterThan_Branch_Swap(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedGreaterThan_Branch_Swap(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedGreaterThan_Branch_Swap(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedGreaterThan_Branch_Swap(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedGreaterThanOrEqual_Normal(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedGreaterThanOrEqual_Normal(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedGreaterThanOrEqual_Normal(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedGreaterThanOrEqual_Normal(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedGreaterThanOrEqual_Normal(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedGreaterThanOrEqual_LogicalNot(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedGreaterThanOrEqual_LogicalNot(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedGreaterThanOrEqual_LogicalNot(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedGreaterThanOrEqual_LogicalNot(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedGreaterThanOrEqual_LogicalNot(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedGreaterThanOrEqual_Branch(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedGreaterThanOrEqual_Branch(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedGreaterThanOrEqual_Branch(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedGreaterThanOrEqual_Branch(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedGreaterThanOrEqual_Branch(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedGreaterThanOrEqual_Swap(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedGreaterThanOrEqual_Swap(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedGreaterThanOrEqual_Swap(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedGreaterThanOrEqual_Swap(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedGreaterThanOrEqual_Swap(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedGreaterThanOrEqual_Branch_Swap(Vector128.Create(42.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedGreaterThanOrEqual_Branch_Swap(Vector128.Create(41.0f), Vector128.Create(42.0f)));
        r &= !Sse.IsSupported || Check(true, Test_Sse_CompareScalarUnorderedGreaterThanOrEqual_Branch_Swap(Vector128.Create(42.0f), Vector128.Create(41.0f)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedGreaterThanOrEqual_Branch_Swap(Vector128.Create(42.0f), Vector128.Create(float.NaN)));
        r &= !Sse.IsSupported || Check(false, Test_Sse_CompareScalarUnorderedGreaterThanOrEqual_Branch_Swap(Vector128.Create(float.NaN), Vector128.Create(float.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedGreaterThanOrEqual_Normal(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedGreaterThanOrEqual_Normal(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedGreaterThanOrEqual_Normal(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedGreaterThanOrEqual_Normal(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedGreaterThanOrEqual_Normal(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedGreaterThanOrEqual_LogicalNot(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedGreaterThanOrEqual_LogicalNot(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedGreaterThanOrEqual_LogicalNot(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedGreaterThanOrEqual_LogicalNot(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedGreaterThanOrEqual_LogicalNot(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedGreaterThanOrEqual_Branch(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedGreaterThanOrEqual_Branch(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedGreaterThanOrEqual_Branch(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedGreaterThanOrEqual_Branch(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedGreaterThanOrEqual_Branch(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedGreaterThanOrEqual_Swap(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedGreaterThanOrEqual_Swap(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedGreaterThanOrEqual_Swap(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedGreaterThanOrEqual_Swap(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedGreaterThanOrEqual_Swap(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedGreaterThanOrEqual_Branch_Swap(Vector128.Create(42.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedGreaterThanOrEqual_Branch_Swap(Vector128.Create(41.0), Vector128.Create(42.0)));
        r &= !Sse2.IsSupported || Check(true, Test_Sse2_CompareScalarUnorderedGreaterThanOrEqual_Branch_Swap(Vector128.Create(42.0), Vector128.Create(41.0)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedGreaterThanOrEqual_Branch_Swap(Vector128.Create(42.0), Vector128.Create(double.NaN)));
        r &= !Sse2.IsSupported || Check(false, Test_Sse2_CompareScalarUnorderedGreaterThanOrEqual_Branch_Swap(Vector128.Create(double.NaN), Vector128.Create(double.NaN)));
        r &= !Sse41.IsSupported || Check(true, Test_Sse41_TestZ_Normal(Vector128.Create(0), Vector128.Create(0)));
        r &= !Sse41.IsSupported || Check(true, Test_Sse41_TestZ_Normal(Vector128.Create(1), Vector128.Create(2)));
        r &= !Sse41.IsSupported || Check(false, Test_Sse41_TestZ_Normal(Vector128.Create(2), Vector128.Create(3)));
        r &= !Sse41.IsSupported || Check(false, Test_Sse41_TestZ_Normal(Vector128.Create(3), Vector128.Create(2)));
        r &= !Sse41.IsSupported || Check(false, Test_Sse41_TestZ_LogicalNot(Vector128.Create(0), Vector128.Create(0)));
        r &= !Sse41.IsSupported || Check(false, Test_Sse41_TestZ_LogicalNot(Vector128.Create(1), Vector128.Create(2)));
        r &= !Sse41.IsSupported || Check(true, Test_Sse41_TestZ_LogicalNot(Vector128.Create(2), Vector128.Create(3)));
        r &= !Sse41.IsSupported || Check(true, Test_Sse41_TestZ_LogicalNot(Vector128.Create(3), Vector128.Create(2)));
        r &= !Sse41.IsSupported || Check(true, Test_Sse41_TestZ_Branch(Vector128.Create(0), Vector128.Create(0)));
        r &= !Sse41.IsSupported || Check(true, Test_Sse41_TestZ_Branch(Vector128.Create(1), Vector128.Create(2)));
        r &= !Sse41.IsSupported || Check(false, Test_Sse41_TestZ_Branch(Vector128.Create(2), Vector128.Create(3)));
        r &= !Sse41.IsSupported || Check(false, Test_Sse41_TestZ_Branch(Vector128.Create(3), Vector128.Create(2)));
        r &= !Sse41.IsSupported || Check(true, Test_Sse41_TestZ_Swap(Vector128.Create(0), Vector128.Create(0)));
        r &= !Sse41.IsSupported || Check(true, Test_Sse41_TestZ_Swap(Vector128.Create(1), Vector128.Create(2)));
        r &= !Sse41.IsSupported || Check(false, Test_Sse41_TestZ_Swap(Vector128.Create(2), Vector128.Create(3)));
        r &= !Sse41.IsSupported || Check(false, Test_Sse41_TestZ_Swap(Vector128.Create(3), Vector128.Create(2)));
        r &= !Sse41.IsSupported || Check(false, Test_Sse41_TestZ_LogicalNot_Swap(Vector128.Create(0), Vector128.Create(0)));
        r &= !Sse41.IsSupported || Check(false, Test_Sse41_TestZ_LogicalNot_Swap(Vector128.Create(1), Vector128.Create(2)));
        r &= !Sse41.IsSupported || Check(true, Test_Sse41_TestZ_LogicalNot_Swap(Vector128.Create(2), Vector128.Create(3)));
        r &= !Sse41.IsSupported || Check(true, Test_Sse41_TestZ_LogicalNot_Swap(Vector128.Create(3), Vector128.Create(2)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestZ_Normal(Vector128.Create(0), Vector128.Create(0)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestZ_Normal(Vector128.Create(1), Vector128.Create(2)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestZ_Normal(Vector128.Create(2), Vector128.Create(3)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestZ_Normal(Vector128.Create(3), Vector128.Create(2)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestZ_LogicalNot(Vector128.Create(0), Vector128.Create(0)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestZ_LogicalNot(Vector128.Create(1), Vector128.Create(2)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestZ_LogicalNot(Vector128.Create(2), Vector128.Create(3)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestZ_LogicalNot(Vector128.Create(3), Vector128.Create(2)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestZ_Branch(Vector128.Create(0), Vector128.Create(0)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestZ_Branch(Vector128.Create(1), Vector128.Create(2)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestZ_Branch(Vector128.Create(2), Vector128.Create(3)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestZ_Branch(Vector128.Create(3), Vector128.Create(2)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestZ_Swap(Vector128.Create(0), Vector128.Create(0)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestZ_Swap(Vector128.Create(1), Vector128.Create(2)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestZ_Swap(Vector128.Create(2), Vector128.Create(3)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestZ_Swap(Vector128.Create(3), Vector128.Create(2)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestZ_LogicalNot_Swap(Vector128.Create(0), Vector128.Create(0)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestZ_LogicalNot_Swap(Vector128.Create(1), Vector128.Create(2)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestZ_LogicalNot_Swap(Vector128.Create(2), Vector128.Create(3)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestZ_LogicalNot_Swap(Vector128.Create(3), Vector128.Create(2)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestZ_Normal(Vector256.Create(0), Vector256.Create(0)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestZ_Normal(Vector256.Create(1), Vector256.Create(2)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestZ_Normal(Vector256.Create(2), Vector256.Create(3)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestZ_Normal(Vector256.Create(3), Vector256.Create(2)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestZ_LogicalNot(Vector256.Create(0), Vector256.Create(0)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestZ_LogicalNot(Vector256.Create(1), Vector256.Create(2)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestZ_LogicalNot(Vector256.Create(2), Vector256.Create(3)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestZ_LogicalNot(Vector256.Create(3), Vector256.Create(2)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestZ_Branch(Vector256.Create(0), Vector256.Create(0)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestZ_Branch(Vector256.Create(1), Vector256.Create(2)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestZ_Branch(Vector256.Create(2), Vector256.Create(3)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestZ_Branch(Vector256.Create(3), Vector256.Create(2)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestZ_Swap(Vector256.Create(0), Vector256.Create(0)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestZ_Swap(Vector256.Create(1), Vector256.Create(2)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestZ_Swap(Vector256.Create(2), Vector256.Create(3)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestZ_Swap(Vector256.Create(3), Vector256.Create(2)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestZ_LogicalNot_Swap(Vector256.Create(0), Vector256.Create(0)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestZ_LogicalNot_Swap(Vector256.Create(1), Vector256.Create(2)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestZ_LogicalNot_Swap(Vector256.Create(2), Vector256.Create(3)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestZ_LogicalNot_Swap(Vector256.Create(3), Vector256.Create(2)));
        r &= !Sse41.IsSupported || Check(true, Test_Sse41_TestC_Normal(Vector128.Create(0), Vector128.Create(0)));
        r &= !Sse41.IsSupported || Check(false, Test_Sse41_TestC_Normal(Vector128.Create(1), Vector128.Create(2)));
        r &= !Sse41.IsSupported || Check(false, Test_Sse41_TestC_Normal(Vector128.Create(2), Vector128.Create(3)));
        r &= !Sse41.IsSupported || Check(true, Test_Sse41_TestC_Normal(Vector128.Create(3), Vector128.Create(2)));
        r &= !Sse41.IsSupported || Check(false, Test_Sse41_TestC_LogicalNot(Vector128.Create(0), Vector128.Create(0)));
        r &= !Sse41.IsSupported || Check(true, Test_Sse41_TestC_LogicalNot(Vector128.Create(1), Vector128.Create(2)));
        r &= !Sse41.IsSupported || Check(true, Test_Sse41_TestC_LogicalNot(Vector128.Create(2), Vector128.Create(3)));
        r &= !Sse41.IsSupported || Check(false, Test_Sse41_TestC_LogicalNot(Vector128.Create(3), Vector128.Create(2)));
        r &= !Sse41.IsSupported || Check(true, Test_Sse41_TestC_Branch(Vector128.Create(0), Vector128.Create(0)));
        r &= !Sse41.IsSupported || Check(false, Test_Sse41_TestC_Branch(Vector128.Create(1), Vector128.Create(2)));
        r &= !Sse41.IsSupported || Check(false, Test_Sse41_TestC_Branch(Vector128.Create(2), Vector128.Create(3)));
        r &= !Sse41.IsSupported || Check(true, Test_Sse41_TestC_Branch(Vector128.Create(3), Vector128.Create(2)));
        r &= !Sse41.IsSupported || Check(true, Test_Sse41_TestC_Swap(Vector128.Create(0), Vector128.Create(0)));
        r &= !Sse41.IsSupported || Check(false, Test_Sse41_TestC_Swap(Vector128.Create(1), Vector128.Create(2)));
        r &= !Sse41.IsSupported || Check(false, Test_Sse41_TestC_Swap(Vector128.Create(2), Vector128.Create(3)));
        r &= !Sse41.IsSupported || Check(true, Test_Sse41_TestC_Swap(Vector128.Create(3), Vector128.Create(2)));
        r &= !Sse41.IsSupported || Check(false, Test_Sse41_TestC_LogicalNot_Swap(Vector128.Create(0), Vector128.Create(0)));
        r &= !Sse41.IsSupported || Check(true, Test_Sse41_TestC_LogicalNot_Swap(Vector128.Create(1), Vector128.Create(2)));
        r &= !Sse41.IsSupported || Check(true, Test_Sse41_TestC_LogicalNot_Swap(Vector128.Create(2), Vector128.Create(3)));
        r &= !Sse41.IsSupported || Check(false, Test_Sse41_TestC_LogicalNot_Swap(Vector128.Create(3), Vector128.Create(2)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestC_Normal(Vector128.Create(0), Vector128.Create(0)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestC_Normal(Vector128.Create(1), Vector128.Create(2)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestC_Normal(Vector128.Create(2), Vector128.Create(3)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestC_Normal(Vector128.Create(3), Vector128.Create(2)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestC_LogicalNot(Vector128.Create(0), Vector128.Create(0)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestC_LogicalNot(Vector128.Create(1), Vector128.Create(2)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestC_LogicalNot(Vector128.Create(2), Vector128.Create(3)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestC_LogicalNot(Vector128.Create(3), Vector128.Create(2)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestC_Branch(Vector128.Create(0), Vector128.Create(0)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestC_Branch(Vector128.Create(1), Vector128.Create(2)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestC_Branch(Vector128.Create(2), Vector128.Create(3)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestC_Branch(Vector128.Create(3), Vector128.Create(2)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestC_Swap(Vector128.Create(0), Vector128.Create(0)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestC_Swap(Vector128.Create(1), Vector128.Create(2)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestC_Swap(Vector128.Create(2), Vector128.Create(3)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestC_Swap(Vector128.Create(3), Vector128.Create(2)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestC_LogicalNot_Swap(Vector128.Create(0), Vector128.Create(0)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestC_LogicalNot_Swap(Vector128.Create(1), Vector128.Create(2)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestC_LogicalNot_Swap(Vector128.Create(2), Vector128.Create(3)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestC_LogicalNot_Swap(Vector128.Create(3), Vector128.Create(2)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestC_Normal(Vector256.Create(0), Vector256.Create(0)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestC_Normal(Vector256.Create(1), Vector256.Create(2)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestC_Normal(Vector256.Create(2), Vector256.Create(3)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestC_Normal(Vector256.Create(3), Vector256.Create(2)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestC_LogicalNot(Vector256.Create(0), Vector256.Create(0)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestC_LogicalNot(Vector256.Create(1), Vector256.Create(2)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestC_LogicalNot(Vector256.Create(2), Vector256.Create(3)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestC_LogicalNot(Vector256.Create(3), Vector256.Create(2)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestC_Branch(Vector256.Create(0), Vector256.Create(0)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestC_Branch(Vector256.Create(1), Vector256.Create(2)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestC_Branch(Vector256.Create(2), Vector256.Create(3)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestC_Branch(Vector256.Create(3), Vector256.Create(2)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestC_Swap(Vector256.Create(0), Vector256.Create(0)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestC_Swap(Vector256.Create(1), Vector256.Create(2)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestC_Swap(Vector256.Create(2), Vector256.Create(3)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestC_Swap(Vector256.Create(3), Vector256.Create(2)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestC_LogicalNot_Swap(Vector256.Create(0), Vector256.Create(0)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestC_LogicalNot_Swap(Vector256.Create(1), Vector256.Create(2)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestC_LogicalNot_Swap(Vector256.Create(2), Vector256.Create(3)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestC_LogicalNot_Swap(Vector256.Create(3), Vector256.Create(2)));
        r &= !Sse41.IsSupported || Check(false, Test_Sse41_TestNotZAndNotC_Normal(Vector128.Create(0), Vector128.Create(0)));
        r &= !Sse41.IsSupported || Check(false, Test_Sse41_TestNotZAndNotC_Normal(Vector128.Create(1), Vector128.Create(2)));
        r &= !Sse41.IsSupported || Check(true, Test_Sse41_TestNotZAndNotC_Normal(Vector128.Create(2), Vector128.Create(3)));
        r &= !Sse41.IsSupported || Check(false, Test_Sse41_TestNotZAndNotC_Normal(Vector128.Create(3), Vector128.Create(2)));
        r &= !Sse41.IsSupported || Check(true, Test_Sse41_TestNotZAndNotC_LogicalNot(Vector128.Create(0), Vector128.Create(0)));
        r &= !Sse41.IsSupported || Check(true, Test_Sse41_TestNotZAndNotC_LogicalNot(Vector128.Create(1), Vector128.Create(2)));
        r &= !Sse41.IsSupported || Check(false, Test_Sse41_TestNotZAndNotC_LogicalNot(Vector128.Create(2), Vector128.Create(3)));
        r &= !Sse41.IsSupported || Check(true, Test_Sse41_TestNotZAndNotC_LogicalNot(Vector128.Create(3), Vector128.Create(2)));
        r &= !Sse41.IsSupported || Check(false, Test_Sse41_TestNotZAndNotC_Branch(Vector128.Create(0), Vector128.Create(0)));
        r &= !Sse41.IsSupported || Check(false, Test_Sse41_TestNotZAndNotC_Branch(Vector128.Create(1), Vector128.Create(2)));
        r &= !Sse41.IsSupported || Check(true, Test_Sse41_TestNotZAndNotC_Branch(Vector128.Create(2), Vector128.Create(3)));
        r &= !Sse41.IsSupported || Check(false, Test_Sse41_TestNotZAndNotC_Branch(Vector128.Create(3), Vector128.Create(2)));
        r &= !Sse41.IsSupported || Check(false, Test_Sse41_TestNotZAndNotC_Swap(Vector128.Create(0), Vector128.Create(0)));
        r &= !Sse41.IsSupported || Check(false, Test_Sse41_TestNotZAndNotC_Swap(Vector128.Create(1), Vector128.Create(2)));
        r &= !Sse41.IsSupported || Check(true, Test_Sse41_TestNotZAndNotC_Swap(Vector128.Create(2), Vector128.Create(3)));
        r &= !Sse41.IsSupported || Check(false, Test_Sse41_TestNotZAndNotC_Swap(Vector128.Create(3), Vector128.Create(2)));
        r &= !Sse41.IsSupported || Check(true, Test_Sse41_TestNotZAndNotC_LogicalNot_Swap(Vector128.Create(0), Vector128.Create(0)));
        r &= !Sse41.IsSupported || Check(true, Test_Sse41_TestNotZAndNotC_LogicalNot_Swap(Vector128.Create(1), Vector128.Create(2)));
        r &= !Sse41.IsSupported || Check(false, Test_Sse41_TestNotZAndNotC_LogicalNot_Swap(Vector128.Create(2), Vector128.Create(3)));
        r &= !Sse41.IsSupported || Check(true, Test_Sse41_TestNotZAndNotC_LogicalNot_Swap(Vector128.Create(3), Vector128.Create(2)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestNotZAndNotC_Normal(Vector128.Create(0), Vector128.Create(0)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestNotZAndNotC_Normal(Vector128.Create(1), Vector128.Create(2)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestNotZAndNotC_Normal(Vector128.Create(2), Vector128.Create(3)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestNotZAndNotC_Normal(Vector128.Create(3), Vector128.Create(2)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestNotZAndNotC_LogicalNot(Vector128.Create(0), Vector128.Create(0)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestNotZAndNotC_LogicalNot(Vector128.Create(1), Vector128.Create(2)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestNotZAndNotC_LogicalNot(Vector128.Create(2), Vector128.Create(3)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestNotZAndNotC_LogicalNot(Vector128.Create(3), Vector128.Create(2)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestNotZAndNotC_Branch(Vector128.Create(0), Vector128.Create(0)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestNotZAndNotC_Branch(Vector128.Create(1), Vector128.Create(2)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestNotZAndNotC_Branch(Vector128.Create(2), Vector128.Create(3)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestNotZAndNotC_Branch(Vector128.Create(3), Vector128.Create(2)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestNotZAndNotC_Swap(Vector128.Create(0), Vector128.Create(0)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestNotZAndNotC_Swap(Vector128.Create(1), Vector128.Create(2)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestNotZAndNotC_Swap(Vector128.Create(2), Vector128.Create(3)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestNotZAndNotC_Swap(Vector128.Create(3), Vector128.Create(2)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestNotZAndNotC_LogicalNot_Swap(Vector128.Create(0), Vector128.Create(0)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestNotZAndNotC_LogicalNot_Swap(Vector128.Create(1), Vector128.Create(2)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestNotZAndNotC_LogicalNot_Swap(Vector128.Create(2), Vector128.Create(3)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestNotZAndNotC_LogicalNot_Swap(Vector128.Create(3), Vector128.Create(2)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestNotZAndNotC_Normal(Vector256.Create(0), Vector256.Create(0)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestNotZAndNotC_Normal(Vector256.Create(1), Vector256.Create(2)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestNotZAndNotC_Normal(Vector256.Create(2), Vector256.Create(3)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestNotZAndNotC_Normal(Vector256.Create(3), Vector256.Create(2)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestNotZAndNotC_LogicalNot(Vector256.Create(0), Vector256.Create(0)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestNotZAndNotC_LogicalNot(Vector256.Create(1), Vector256.Create(2)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestNotZAndNotC_LogicalNot(Vector256.Create(2), Vector256.Create(3)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestNotZAndNotC_LogicalNot(Vector256.Create(3), Vector256.Create(2)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestNotZAndNotC_Branch(Vector256.Create(0), Vector256.Create(0)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestNotZAndNotC_Branch(Vector256.Create(1), Vector256.Create(2)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestNotZAndNotC_Branch(Vector256.Create(2), Vector256.Create(3)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestNotZAndNotC_Branch(Vector256.Create(3), Vector256.Create(2)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestNotZAndNotC_Swap(Vector256.Create(0), Vector256.Create(0)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestNotZAndNotC_Swap(Vector256.Create(1), Vector256.Create(2)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestNotZAndNotC_Swap(Vector256.Create(2), Vector256.Create(3)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestNotZAndNotC_Swap(Vector256.Create(3), Vector256.Create(2)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestNotZAndNotC_LogicalNot_Swap(Vector256.Create(0), Vector256.Create(0)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestNotZAndNotC_LogicalNot_Swap(Vector256.Create(1), Vector256.Create(2)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestNotZAndNotC_LogicalNot_Swap(Vector256.Create(2), Vector256.Create(3)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestNotZAndNotC_LogicalNot_Swap(Vector256.Create(3), Vector256.Create(2)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestZ_Normal(Vector128.Create(1.0f), Vector128.Create(1.0f)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestZ_Normal(Vector128.Create(1.0f), Vector128.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestZ_Normal(Vector128.Create(-1.0f), Vector128.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestZ_LogicalNot(Vector128.Create(1.0f), Vector128.Create(1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestZ_LogicalNot(Vector128.Create(1.0f), Vector128.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestZ_LogicalNot(Vector128.Create(-1.0f), Vector128.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestZ_Branch(Vector128.Create(1.0f), Vector128.Create(1.0f)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestZ_Branch(Vector128.Create(1.0f), Vector128.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestZ_Branch(Vector128.Create(-1.0f), Vector128.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestZ_Swap(Vector128.Create(1.0f), Vector128.Create(1.0f)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestZ_Swap(Vector128.Create(1.0f), Vector128.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestZ_Swap(Vector128.Create(-1.0f), Vector128.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestZ_LogicalNot_Branch_Swap(Vector128.Create(1.0f), Vector128.Create(1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestZ_LogicalNot_Branch_Swap(Vector128.Create(1.0f), Vector128.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestZ_LogicalNot_Branch_Swap(Vector128.Create(-1.0f), Vector128.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestZ_Normal(Vector256.Create(1.0f), Vector256.Create(1.0f)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestZ_Normal(Vector256.Create(1.0f), Vector256.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestZ_Normal(Vector256.Create(-1.0f), Vector256.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestZ_LogicalNot(Vector256.Create(1.0f), Vector256.Create(1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestZ_LogicalNot(Vector256.Create(1.0f), Vector256.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestZ_LogicalNot(Vector256.Create(-1.0f), Vector256.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestZ_Branch(Vector256.Create(1.0f), Vector256.Create(1.0f)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestZ_Branch(Vector256.Create(1.0f), Vector256.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestZ_Branch(Vector256.Create(-1.0f), Vector256.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestZ_Swap(Vector256.Create(1.0f), Vector256.Create(1.0f)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestZ_Swap(Vector256.Create(1.0f), Vector256.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestZ_Swap(Vector256.Create(-1.0f), Vector256.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestZ_LogicalNot_Branch_Swap(Vector256.Create(1.0f), Vector256.Create(1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestZ_LogicalNot_Branch_Swap(Vector256.Create(1.0f), Vector256.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestZ_LogicalNot_Branch_Swap(Vector256.Create(-1.0f), Vector256.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestC_Normal(Vector128.Create(1.0f), Vector128.Create(1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestC_Normal(Vector128.Create(1.0f), Vector128.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestC_Normal(Vector128.Create(-1.0f), Vector128.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestC_LogicalNot(Vector128.Create(1.0f), Vector128.Create(1.0f)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestC_LogicalNot(Vector128.Create(1.0f), Vector128.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestC_LogicalNot(Vector128.Create(-1.0f), Vector128.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestC_Branch(Vector128.Create(1.0f), Vector128.Create(1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestC_Branch(Vector128.Create(1.0f), Vector128.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestC_Branch(Vector128.Create(-1.0f), Vector128.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestC_Swap(Vector128.Create(1.0f), Vector128.Create(1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestC_Swap(Vector128.Create(1.0f), Vector128.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestC_Swap(Vector128.Create(-1.0f), Vector128.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestC_LogicalNot_Branch_Swap(Vector128.Create(1.0f), Vector128.Create(1.0f)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestC_LogicalNot_Branch_Swap(Vector128.Create(1.0f), Vector128.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestC_LogicalNot_Branch_Swap(Vector128.Create(-1.0f), Vector128.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestC_Normal(Vector256.Create(1.0f), Vector256.Create(1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestC_Normal(Vector256.Create(1.0f), Vector256.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestC_Normal(Vector256.Create(-1.0f), Vector256.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestC_LogicalNot(Vector256.Create(1.0f), Vector256.Create(1.0f)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestC_LogicalNot(Vector256.Create(1.0f), Vector256.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestC_LogicalNot(Vector256.Create(-1.0f), Vector256.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestC_Branch(Vector256.Create(1.0f), Vector256.Create(1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestC_Branch(Vector256.Create(1.0f), Vector256.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestC_Branch(Vector256.Create(-1.0f), Vector256.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestC_Swap(Vector256.Create(1.0f), Vector256.Create(1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestC_Swap(Vector256.Create(1.0f), Vector256.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestC_Swap(Vector256.Create(-1.0f), Vector256.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestC_LogicalNot_Branch_Swap(Vector256.Create(1.0f), Vector256.Create(1.0f)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestC_LogicalNot_Branch_Swap(Vector256.Create(1.0f), Vector256.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestC_LogicalNot_Branch_Swap(Vector256.Create(-1.0f), Vector256.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestNotZAndNotC_Normal(Vector128.Create(1.0f), Vector128.Create(1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestNotZAndNotC_Normal(Vector128.Create(1.0f), Vector128.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestNotZAndNotC_Normal(Vector128.Create(-1.0f), Vector128.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestNotZAndNotC_LogicalNot(Vector128.Create(1.0f), Vector128.Create(1.0f)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestNotZAndNotC_LogicalNot(Vector128.Create(1.0f), Vector128.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestNotZAndNotC_LogicalNot(Vector128.Create(-1.0f), Vector128.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestNotZAndNotC_Branch(Vector128.Create(1.0f), Vector128.Create(1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestNotZAndNotC_Branch(Vector128.Create(1.0f), Vector128.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestNotZAndNotC_Branch(Vector128.Create(-1.0f), Vector128.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestNotZAndNotC_Swap(Vector128.Create(1.0f), Vector128.Create(1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestNotZAndNotC_Swap(Vector128.Create(1.0f), Vector128.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestNotZAndNotC_Swap(Vector128.Create(-1.0f), Vector128.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestNotZAndNotC_LogicalNot_Branch_Swap(Vector128.Create(1.0f), Vector128.Create(1.0f)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestNotZAndNotC_LogicalNot_Branch_Swap(Vector128.Create(1.0f), Vector128.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestNotZAndNotC_LogicalNot_Branch_Swap(Vector128.Create(-1.0f), Vector128.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestNotZAndNotC_Normal(Vector256.Create(1.0f), Vector256.Create(1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestNotZAndNotC_Normal(Vector256.Create(1.0f), Vector256.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestNotZAndNotC_Normal(Vector256.Create(-1.0f), Vector256.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestNotZAndNotC_LogicalNot(Vector256.Create(1.0f), Vector256.Create(1.0f)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestNotZAndNotC_LogicalNot(Vector256.Create(1.0f), Vector256.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestNotZAndNotC_LogicalNot(Vector256.Create(-1.0f), Vector256.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestNotZAndNotC_Branch(Vector256.Create(1.0f), Vector256.Create(1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestNotZAndNotC_Branch(Vector256.Create(1.0f), Vector256.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestNotZAndNotC_Branch(Vector256.Create(-1.0f), Vector256.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestNotZAndNotC_Swap(Vector256.Create(1.0f), Vector256.Create(1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestNotZAndNotC_Swap(Vector256.Create(1.0f), Vector256.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(false, Test_Avx_TestNotZAndNotC_Swap(Vector256.Create(-1.0f), Vector256.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestNotZAndNotC_LogicalNot_Branch_Swap(Vector256.Create(1.0f), Vector256.Create(1.0f)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestNotZAndNotC_LogicalNot_Branch_Swap(Vector256.Create(1.0f), Vector256.Create(-1.0f)));
        r &= !Avx.IsSupported || Check(true, Test_Avx_TestNotZAndNotC_LogicalNot_Branch_Swap(Vector256.Create(-1.0f), Vector256.Create(-1.0f)));
        Assert.True(r);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarOrderedEqual_Normal(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarOrderedEqual(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarOrderedEqual_LogicalNot(in Vector128<Single> x, in Vector128<Single> y)
    {
        return !Sse.CompareScalarOrderedEqual(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarOrderedEqual_Branch(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarOrderedEqual(x, y) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarOrderedEqual_Swap(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarOrderedEqual(x, Sse.Or(y.AsSingle(), default).AsSingle());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarOrderedEqual_Branch_Swap(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarOrderedEqual(x, Sse.Or(y.AsSingle(), default).AsSingle()) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarOrderedEqual_Normal(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarOrderedEqual(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarOrderedEqual_LogicalNot(in Vector128<Double> x, in Vector128<Double> y)
    {
        return !Sse2.CompareScalarOrderedEqual(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarOrderedEqual_Branch(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarOrderedEqual(x, y) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarOrderedEqual_Swap(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarOrderedEqual(x, Sse2.Or(y.AsSingle(), default).AsDouble());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarOrderedEqual_Branch_Swap(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarOrderedEqual(x, Sse2.Or(y.AsSingle(), default).AsDouble()) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarOrderedNotEqual_Normal(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarOrderedNotEqual(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarOrderedNotEqual_LogicalNot(in Vector128<Single> x, in Vector128<Single> y)
    {
        return !Sse.CompareScalarOrderedNotEqual(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarOrderedNotEqual_Branch(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarOrderedNotEqual(x, y) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarOrderedNotEqual_Swap(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarOrderedNotEqual(x, Sse.Or(y.AsSingle(), default).AsSingle());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarOrderedNotEqual_Branch_Swap(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarOrderedNotEqual(x, Sse.Or(y.AsSingle(), default).AsSingle()) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarOrderedNotEqual_Normal(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarOrderedNotEqual(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarOrderedNotEqual_LogicalNot(in Vector128<Double> x, in Vector128<Double> y)
    {
        return !Sse2.CompareScalarOrderedNotEqual(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarOrderedNotEqual_Branch(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarOrderedNotEqual(x, y) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarOrderedNotEqual_Swap(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarOrderedNotEqual(x, Sse2.Or(y.AsSingle(), default).AsDouble());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarOrderedNotEqual_Branch_Swap(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarOrderedNotEqual(x, Sse2.Or(y.AsSingle(), default).AsDouble()) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarOrderedLessThan_Normal(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarOrderedLessThan(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarOrderedLessThan_LogicalNot(in Vector128<Single> x, in Vector128<Single> y)
    {
        return !Sse.CompareScalarOrderedLessThan(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarOrderedLessThan_Branch(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarOrderedLessThan(x, y) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarOrderedLessThan_Swap(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarOrderedLessThan(x, Sse.Or(y.AsSingle(), default).AsSingle());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarOrderedLessThan_Branch_Swap(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarOrderedLessThan(x, Sse.Or(y.AsSingle(), default).AsSingle()) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarOrderedLessThan_Normal(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarOrderedLessThan(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarOrderedLessThan_LogicalNot(in Vector128<Double> x, in Vector128<Double> y)
    {
        return !Sse2.CompareScalarOrderedLessThan(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarOrderedLessThan_Branch(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarOrderedLessThan(x, y) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarOrderedLessThan_Swap(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarOrderedLessThan(x, Sse2.Or(y.AsSingle(), default).AsDouble());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarOrderedLessThan_Branch_Swap(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarOrderedLessThan(x, Sse2.Or(y.AsSingle(), default).AsDouble()) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarOrderedLessThanOrEqual_Normal(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarOrderedLessThanOrEqual(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarOrderedLessThanOrEqual_LogicalNot(in Vector128<Single> x, in Vector128<Single> y)
    {
        return !Sse.CompareScalarOrderedLessThanOrEqual(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarOrderedLessThanOrEqual_Branch(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarOrderedLessThanOrEqual(x, y) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarOrderedLessThanOrEqual_Swap(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarOrderedLessThanOrEqual(x, Sse.Or(y.AsSingle(), default).AsSingle());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarOrderedLessThanOrEqual_Branch_Swap(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarOrderedLessThanOrEqual(x, Sse.Or(y.AsSingle(), default).AsSingle()) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarOrderedLessThanOrEqual_Normal(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarOrderedLessThanOrEqual(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarOrderedLessThanOrEqual_LogicalNot(in Vector128<Double> x, in Vector128<Double> y)
    {
        return !Sse2.CompareScalarOrderedLessThanOrEqual(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarOrderedLessThanOrEqual_Branch(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarOrderedLessThanOrEqual(x, y) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarOrderedLessThanOrEqual_Swap(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarOrderedLessThanOrEqual(x, Sse2.Or(y.AsSingle(), default).AsDouble());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarOrderedLessThanOrEqual_Branch_Swap(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarOrderedLessThanOrEqual(x, Sse2.Or(y.AsSingle(), default).AsDouble()) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarOrderedGreaterThan_Normal(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarOrderedGreaterThan(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarOrderedGreaterThan_LogicalNot(in Vector128<Single> x, in Vector128<Single> y)
    {
        return !Sse.CompareScalarOrderedGreaterThan(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarOrderedGreaterThan_Branch(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarOrderedGreaterThan(x, y) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarOrderedGreaterThan_Swap(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarOrderedGreaterThan(x, Sse.Or(y.AsSingle(), default).AsSingle());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarOrderedGreaterThan_Branch_Swap(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarOrderedGreaterThan(x, Sse.Or(y.AsSingle(), default).AsSingle()) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarOrderedGreaterThan_Normal(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarOrderedGreaterThan(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarOrderedGreaterThan_LogicalNot(in Vector128<Double> x, in Vector128<Double> y)
    {
        return !Sse2.CompareScalarOrderedGreaterThan(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarOrderedGreaterThan_Branch(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarOrderedGreaterThan(x, y) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarOrderedGreaterThan_Swap(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarOrderedGreaterThan(x, Sse2.Or(y.AsSingle(), default).AsDouble());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarOrderedGreaterThan_Branch_Swap(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarOrderedGreaterThan(x, Sse2.Or(y.AsSingle(), default).AsDouble()) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarOrderedGreaterThanOrEqual_Normal(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarOrderedGreaterThanOrEqual(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarOrderedGreaterThanOrEqual_LogicalNot(in Vector128<Single> x, in Vector128<Single> y)
    {
        return !Sse.CompareScalarOrderedGreaterThanOrEqual(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarOrderedGreaterThanOrEqual_Branch(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarOrderedGreaterThanOrEqual(x, y) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarOrderedGreaterThanOrEqual_Swap(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarOrderedGreaterThanOrEqual(x, Sse.Or(y.AsSingle(), default).AsSingle());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarOrderedGreaterThanOrEqual_Branch_Swap(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarOrderedGreaterThanOrEqual(x, Sse.Or(y.AsSingle(), default).AsSingle()) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarOrderedGreaterThanOrEqual_Normal(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarOrderedGreaterThanOrEqual(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarOrderedGreaterThanOrEqual_LogicalNot(in Vector128<Double> x, in Vector128<Double> y)
    {
        return !Sse2.CompareScalarOrderedGreaterThanOrEqual(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarOrderedGreaterThanOrEqual_Branch(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarOrderedGreaterThanOrEqual(x, y) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarOrderedGreaterThanOrEqual_Swap(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarOrderedGreaterThanOrEqual(x, Sse2.Or(y.AsSingle(), default).AsDouble());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarOrderedGreaterThanOrEqual_Branch_Swap(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarOrderedGreaterThanOrEqual(x, Sse2.Or(y.AsSingle(), default).AsDouble()) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarUnorderedEqual_Normal(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarUnorderedEqual(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarUnorderedEqual_LogicalNot(in Vector128<Single> x, in Vector128<Single> y)
    {
        return !Sse.CompareScalarUnorderedEqual(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarUnorderedEqual_Branch(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarUnorderedEqual(x, y) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarUnorderedEqual_Swap(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarUnorderedEqual(x, Sse.Or(y.AsSingle(), default).AsSingle());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarUnorderedEqual_Branch_Swap(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarUnorderedEqual(x, Sse.Or(y.AsSingle(), default).AsSingle()) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarUnorderedEqual_Normal(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarUnorderedEqual(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarUnorderedEqual_LogicalNot(in Vector128<Double> x, in Vector128<Double> y)
    {
        return !Sse2.CompareScalarUnorderedEqual(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarUnorderedEqual_Branch(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarUnorderedEqual(x, y) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarUnorderedEqual_Swap(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarUnorderedEqual(x, Sse2.Or(y.AsSingle(), default).AsDouble());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarUnorderedEqual_Branch_Swap(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarUnorderedEqual(x, Sse2.Or(y.AsSingle(), default).AsDouble()) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarUnorderedNotEqual_Normal(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarUnorderedNotEqual(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarUnorderedNotEqual_LogicalNot(in Vector128<Single> x, in Vector128<Single> y)
    {
        return !Sse.CompareScalarUnorderedNotEqual(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarUnorderedNotEqual_Branch(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarUnorderedNotEqual(x, y) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarUnorderedNotEqual_Swap(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarUnorderedNotEqual(x, Sse.Or(y.AsSingle(), default).AsSingle());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarUnorderedNotEqual_Branch_Swap(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarUnorderedNotEqual(x, Sse.Or(y.AsSingle(), default).AsSingle()) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarUnorderedNotEqual_Normal(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarUnorderedNotEqual(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarUnorderedNotEqual_LogicalNot(in Vector128<Double> x, in Vector128<Double> y)
    {
        return !Sse2.CompareScalarUnorderedNotEqual(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarUnorderedNotEqual_Branch(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarUnorderedNotEqual(x, y) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarUnorderedNotEqual_Swap(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarUnorderedNotEqual(x, Sse2.Or(y.AsSingle(), default).AsDouble());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarUnorderedNotEqual_Branch_Swap(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarUnorderedNotEqual(x, Sse2.Or(y.AsSingle(), default).AsDouble()) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarUnorderedLessThan_Normal(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarUnorderedLessThan(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarUnorderedLessThan_LogicalNot(in Vector128<Single> x, in Vector128<Single> y)
    {
        return !Sse.CompareScalarUnorderedLessThan(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarUnorderedLessThan_Branch(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarUnorderedLessThan(x, y) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarUnorderedLessThan_Swap(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarUnorderedLessThan(x, Sse.Or(y.AsSingle(), default).AsSingle());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarUnorderedLessThan_Branch_Swap(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarUnorderedLessThan(x, Sse.Or(y.AsSingle(), default).AsSingle()) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarUnorderedLessThan_Normal(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarUnorderedLessThan(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarUnorderedLessThan_LogicalNot(in Vector128<Double> x, in Vector128<Double> y)
    {
        return !Sse2.CompareScalarUnorderedLessThan(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarUnorderedLessThan_Branch(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarUnorderedLessThan(x, y) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarUnorderedLessThan_Swap(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarUnorderedLessThan(x, Sse2.Or(y.AsSingle(), default).AsDouble());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarUnorderedLessThan_Branch_Swap(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarUnorderedLessThan(x, Sse2.Or(y.AsSingle(), default).AsDouble()) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarUnorderedLessThanOrEqual_Normal(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarUnorderedLessThanOrEqual(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarUnorderedLessThanOrEqual_LogicalNot(in Vector128<Single> x, in Vector128<Single> y)
    {
        return !Sse.CompareScalarUnorderedLessThanOrEqual(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarUnorderedLessThanOrEqual_Branch(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarUnorderedLessThanOrEqual(x, y) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarUnorderedLessThanOrEqual_Swap(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarUnorderedLessThanOrEqual(x, Sse.Or(y.AsSingle(), default).AsSingle());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarUnorderedLessThanOrEqual_Branch_Swap(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarUnorderedLessThanOrEqual(x, Sse.Or(y.AsSingle(), default).AsSingle()) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarUnorderedLessThanOrEqual_Normal(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarUnorderedLessThanOrEqual(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarUnorderedLessThanOrEqual_LogicalNot(in Vector128<Double> x, in Vector128<Double> y)
    {
        return !Sse2.CompareScalarUnorderedLessThanOrEqual(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarUnorderedLessThanOrEqual_Branch(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarUnorderedLessThanOrEqual(x, y) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarUnorderedLessThanOrEqual_Swap(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarUnorderedLessThanOrEqual(x, Sse2.Or(y.AsSingle(), default).AsDouble());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarUnorderedLessThanOrEqual_Branch_Swap(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarUnorderedLessThanOrEqual(x, Sse2.Or(y.AsSingle(), default).AsDouble()) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarUnorderedGreaterThan_Normal(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarUnorderedGreaterThan(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarUnorderedGreaterThan_LogicalNot(in Vector128<Single> x, in Vector128<Single> y)
    {
        return !Sse.CompareScalarUnorderedGreaterThan(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarUnorderedGreaterThan_Branch(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarUnorderedGreaterThan(x, y) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarUnorderedGreaterThan_Swap(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarUnorderedGreaterThan(x, Sse.Or(y.AsSingle(), default).AsSingle());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarUnorderedGreaterThan_Branch_Swap(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarUnorderedGreaterThan(x, Sse.Or(y.AsSingle(), default).AsSingle()) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarUnorderedGreaterThan_Normal(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarUnorderedGreaterThan(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarUnorderedGreaterThan_LogicalNot(in Vector128<Double> x, in Vector128<Double> y)
    {
        return !Sse2.CompareScalarUnorderedGreaterThan(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarUnorderedGreaterThan_Branch(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarUnorderedGreaterThan(x, y) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarUnorderedGreaterThan_Swap(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarUnorderedGreaterThan(x, Sse2.Or(y.AsSingle(), default).AsDouble());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarUnorderedGreaterThan_Branch_Swap(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarUnorderedGreaterThan(x, Sse2.Or(y.AsSingle(), default).AsDouble()) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarUnorderedGreaterThanOrEqual_Normal(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarUnorderedGreaterThanOrEqual(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarUnorderedGreaterThanOrEqual_LogicalNot(in Vector128<Single> x, in Vector128<Single> y)
    {
        return !Sse.CompareScalarUnorderedGreaterThanOrEqual(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarUnorderedGreaterThanOrEqual_Branch(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarUnorderedGreaterThanOrEqual(x, y) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarUnorderedGreaterThanOrEqual_Swap(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarUnorderedGreaterThanOrEqual(x, Sse.Or(y.AsSingle(), default).AsSingle());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse_CompareScalarUnorderedGreaterThanOrEqual_Branch_Swap(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Sse.CompareScalarUnorderedGreaterThanOrEqual(x, Sse.Or(y.AsSingle(), default).AsSingle()) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarUnorderedGreaterThanOrEqual_Normal(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarUnorderedGreaterThanOrEqual(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarUnorderedGreaterThanOrEqual_LogicalNot(in Vector128<Double> x, in Vector128<Double> y)
    {
        return !Sse2.CompareScalarUnorderedGreaterThanOrEqual(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarUnorderedGreaterThanOrEqual_Branch(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarUnorderedGreaterThanOrEqual(x, y) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarUnorderedGreaterThanOrEqual_Swap(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarUnorderedGreaterThanOrEqual(x, Sse2.Or(y.AsSingle(), default).AsDouble());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse2_CompareScalarUnorderedGreaterThanOrEqual_Branch_Swap(in Vector128<Double> x, in Vector128<Double> y)
    {
        return Sse2.CompareScalarUnorderedGreaterThanOrEqual(x, Sse2.Or(y.AsSingle(), default).AsDouble()) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse41_TestZ_Normal(in Vector128<Int32> x, in Vector128<Int32> y)
    {
        return Sse41.TestZ(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse41_TestZ_LogicalNot(in Vector128<Int32> x, in Vector128<Int32> y)
    {
        return !Sse41.TestZ(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse41_TestZ_Branch(in Vector128<Int32> x, in Vector128<Int32> y)
    {
        return Sse41.TestZ(x, y) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse41_TestZ_Swap(in Vector128<Int32> x, in Vector128<Int32> y)
    {
        return Sse41.TestZ(x, Sse41.Or(y.AsSingle(), default).AsInt32());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse41_TestZ_LogicalNot_Swap(in Vector128<Int32> x, in Vector128<Int32> y)
    {
        return !Sse41.TestZ(x, Sse41.Or(y.AsSingle(), default).AsInt32());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestZ_Normal(in Vector128<Int32> x, in Vector128<Int32> y)
    {
        return Avx.TestZ(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestZ_LogicalNot(in Vector128<Int32> x, in Vector128<Int32> y)
    {
        return !Avx.TestZ(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestZ_Branch(in Vector128<Int32> x, in Vector128<Int32> y)
    {
        return Avx.TestZ(x, y) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestZ_Swap(in Vector128<Int32> x, in Vector128<Int32> y)
    {
        return Avx.TestZ(x, Avx.Or(y.AsSingle(), default).AsInt32());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestZ_LogicalNot_Swap(in Vector128<Int32> x, in Vector128<Int32> y)
    {
        return !Avx.TestZ(x, Avx.Or(y.AsSingle(), default).AsInt32());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestZ_Normal(in Vector256<Int32> x, in Vector256<Int32> y)
    {
        return Avx.TestZ(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestZ_LogicalNot(in Vector256<Int32> x, in Vector256<Int32> y)
    {
        return !Avx.TestZ(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestZ_Branch(in Vector256<Int32> x, in Vector256<Int32> y)
    {
        return Avx.TestZ(x, y) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestZ_Swap(in Vector256<Int32> x, in Vector256<Int32> y)
    {
        return Avx.TestZ(x, Avx.Or(y.AsSingle(), default).AsInt32());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestZ_LogicalNot_Swap(in Vector256<Int32> x, in Vector256<Int32> y)
    {
        return !Avx.TestZ(x, Avx.Or(y.AsSingle(), default).AsInt32());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse41_TestC_Normal(in Vector128<Int32> x, in Vector128<Int32> y)
    {
        return Sse41.TestC(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse41_TestC_LogicalNot(in Vector128<Int32> x, in Vector128<Int32> y)
    {
        return !Sse41.TestC(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse41_TestC_Branch(in Vector128<Int32> x, in Vector128<Int32> y)
    {
        return Sse41.TestC(x, y) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse41_TestC_Swap(in Vector128<Int32> x, in Vector128<Int32> y)
    {
        return Sse41.TestC(x, Sse41.Or(y.AsSingle(), default).AsInt32());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse41_TestC_LogicalNot_Swap(in Vector128<Int32> x, in Vector128<Int32> y)
    {
        return !Sse41.TestC(x, Sse41.Or(y.AsSingle(), default).AsInt32());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestC_Normal(in Vector128<Int32> x, in Vector128<Int32> y)
    {
        return Avx.TestC(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestC_LogicalNot(in Vector128<Int32> x, in Vector128<Int32> y)
    {
        return !Avx.TestC(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestC_Branch(in Vector128<Int32> x, in Vector128<Int32> y)
    {
        return Avx.TestC(x, y) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestC_Swap(in Vector128<Int32> x, in Vector128<Int32> y)
    {
        return Avx.TestC(x, Avx.Or(y.AsSingle(), default).AsInt32());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestC_LogicalNot_Swap(in Vector128<Int32> x, in Vector128<Int32> y)
    {
        return !Avx.TestC(x, Avx.Or(y.AsSingle(), default).AsInt32());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestC_Normal(in Vector256<Int32> x, in Vector256<Int32> y)
    {
        return Avx.TestC(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestC_LogicalNot(in Vector256<Int32> x, in Vector256<Int32> y)
    {
        return !Avx.TestC(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestC_Branch(in Vector256<Int32> x, in Vector256<Int32> y)
    {
        return Avx.TestC(x, y) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestC_Swap(in Vector256<Int32> x, in Vector256<Int32> y)
    {
        return Avx.TestC(x, Avx.Or(y.AsSingle(), default).AsInt32());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestC_LogicalNot_Swap(in Vector256<Int32> x, in Vector256<Int32> y)
    {
        return !Avx.TestC(x, Avx.Or(y.AsSingle(), default).AsInt32());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse41_TestNotZAndNotC_Normal(in Vector128<Int32> x, in Vector128<Int32> y)
    {
        return Sse41.TestNotZAndNotC(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse41_TestNotZAndNotC_LogicalNot(in Vector128<Int32> x, in Vector128<Int32> y)
    {
        return !Sse41.TestNotZAndNotC(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse41_TestNotZAndNotC_Branch(in Vector128<Int32> x, in Vector128<Int32> y)
    {
        return Sse41.TestNotZAndNotC(x, y) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse41_TestNotZAndNotC_Swap(in Vector128<Int32> x, in Vector128<Int32> y)
    {
        return Sse41.TestNotZAndNotC(x, Sse41.Or(y.AsSingle(), default).AsInt32());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Sse41_TestNotZAndNotC_LogicalNot_Swap(in Vector128<Int32> x, in Vector128<Int32> y)
    {
        return !Sse41.TestNotZAndNotC(x, Sse41.Or(y.AsSingle(), default).AsInt32());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestNotZAndNotC_Normal(in Vector128<Int32> x, in Vector128<Int32> y)
    {
        return Avx.TestNotZAndNotC(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestNotZAndNotC_LogicalNot(in Vector128<Int32> x, in Vector128<Int32> y)
    {
        return !Avx.TestNotZAndNotC(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestNotZAndNotC_Branch(in Vector128<Int32> x, in Vector128<Int32> y)
    {
        return Avx.TestNotZAndNotC(x, y) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestNotZAndNotC_Swap(in Vector128<Int32> x, in Vector128<Int32> y)
    {
        return Avx.TestNotZAndNotC(x, Avx.Or(y.AsSingle(), default).AsInt32());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestNotZAndNotC_LogicalNot_Swap(in Vector128<Int32> x, in Vector128<Int32> y)
    {
        return !Avx.TestNotZAndNotC(x, Avx.Or(y.AsSingle(), default).AsInt32());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestNotZAndNotC_Normal(in Vector256<Int32> x, in Vector256<Int32> y)
    {
        return Avx.TestNotZAndNotC(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestNotZAndNotC_LogicalNot(in Vector256<Int32> x, in Vector256<Int32> y)
    {
        return !Avx.TestNotZAndNotC(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestNotZAndNotC_Branch(in Vector256<Int32> x, in Vector256<Int32> y)
    {
        return Avx.TestNotZAndNotC(x, y) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestNotZAndNotC_Swap(in Vector256<Int32> x, in Vector256<Int32> y)
    {
        return Avx.TestNotZAndNotC(x, Avx.Or(y.AsSingle(), default).AsInt32());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestNotZAndNotC_LogicalNot_Swap(in Vector256<Int32> x, in Vector256<Int32> y)
    {
        return !Avx.TestNotZAndNotC(x, Avx.Or(y.AsSingle(), default).AsInt32());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestZ_Normal(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Avx.TestZ(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestZ_LogicalNot(in Vector128<Single> x, in Vector128<Single> y)
    {
        return !Avx.TestZ(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestZ_Branch(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Avx.TestZ(x, y) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestZ_Swap(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Avx.TestZ(x, Avx.Or(y.AsSingle(), default).AsSingle());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestZ_LogicalNot_Branch_Swap(in Vector128<Single> x, in Vector128<Single> y)
    {
        return !Avx.TestZ(x, Avx.Or(y.AsSingle(), default).AsSingle()) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestZ_Normal(in Vector256<Single> x, in Vector256<Single> y)
    {
        return Avx.TestZ(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestZ_LogicalNot(in Vector256<Single> x, in Vector256<Single> y)
    {
        return !Avx.TestZ(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestZ_Branch(in Vector256<Single> x, in Vector256<Single> y)
    {
        return Avx.TestZ(x, y) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestZ_Swap(in Vector256<Single> x, in Vector256<Single> y)
    {
        return Avx.TestZ(x, Avx.Or(y.AsSingle(), default).AsSingle());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestZ_LogicalNot_Branch_Swap(in Vector256<Single> x, in Vector256<Single> y)
    {
        return !Avx.TestZ(x, Avx.Or(y.AsSingle(), default).AsSingle()) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestC_Normal(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Avx.TestC(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestC_LogicalNot(in Vector128<Single> x, in Vector128<Single> y)
    {
        return !Avx.TestC(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestC_Branch(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Avx.TestC(x, y) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestC_Swap(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Avx.TestC(x, Avx.Or(y.AsSingle(), default).AsSingle());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestC_LogicalNot_Branch_Swap(in Vector128<Single> x, in Vector128<Single> y)
    {
        return !Avx.TestC(x, Avx.Or(y.AsSingle(), default).AsSingle()) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestC_Normal(in Vector256<Single> x, in Vector256<Single> y)
    {
        return Avx.TestC(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestC_LogicalNot(in Vector256<Single> x, in Vector256<Single> y)
    {
        return !Avx.TestC(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestC_Branch(in Vector256<Single> x, in Vector256<Single> y)
    {
        return Avx.TestC(x, y) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestC_Swap(in Vector256<Single> x, in Vector256<Single> y)
    {
        return Avx.TestC(x, Avx.Or(y.AsSingle(), default).AsSingle());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestC_LogicalNot_Branch_Swap(in Vector256<Single> x, in Vector256<Single> y)
    {
        return !Avx.TestC(x, Avx.Or(y.AsSingle(), default).AsSingle()) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestNotZAndNotC_Normal(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Avx.TestNotZAndNotC(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestNotZAndNotC_LogicalNot(in Vector128<Single> x, in Vector128<Single> y)
    {
        return !Avx.TestNotZAndNotC(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestNotZAndNotC_Branch(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Avx.TestNotZAndNotC(x, y) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestNotZAndNotC_Swap(in Vector128<Single> x, in Vector128<Single> y)
    {
        return Avx.TestNotZAndNotC(x, Avx.Or(y.AsSingle(), default).AsSingle());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestNotZAndNotC_LogicalNot_Branch_Swap(in Vector128<Single> x, in Vector128<Single> y)
    {
        return !Avx.TestNotZAndNotC(x, Avx.Or(y.AsSingle(), default).AsSingle()) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestNotZAndNotC_Normal(in Vector256<Single> x, in Vector256<Single> y)
    {
        return Avx.TestNotZAndNotC(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestNotZAndNotC_LogicalNot(in Vector256<Single> x, in Vector256<Single> y)
    {
        return !Avx.TestNotZAndNotC(x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestNotZAndNotC_Branch(in Vector256<Single> x, in Vector256<Single> y)
    {
        return Avx.TestNotZAndNotC(x, y) ? True() : False();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestNotZAndNotC_Swap(in Vector256<Single> x, in Vector256<Single> y)
    {
        return Avx.TestNotZAndNotC(x, Avx.Or(y.AsSingle(), default).AsSingle());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test_Avx_TestNotZAndNotC_LogicalNot_Branch_Swap(in Vector256<Single> x, in Vector256<Single> y)
    {
        return !Avx.TestNotZAndNotC(x, Avx.Or(y.AsSingle(), default).AsSingle()) ? True() : False();
    }
}
