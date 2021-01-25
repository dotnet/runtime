// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

unsafe partial class GenericsNative
{
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IComInterface<T> where T : unmanaged
    {
    }

    public struct Point1<T> where T : struct
    {
        public T e00;
    }

    public struct Point2<T> where T : struct
    {
        public T e00;
        public T e01;
    }

    public struct Point3<T> where T : struct
    {
        public T e00;
        public T e01;
        public T e02;
    }

    public struct Point4<T> where T : struct
    {
        public T e00;
        public T e01;
        public T e02;
        public T e03;
    }

    [StructLayout(LayoutKind.Sequential)]
    public class SequentialClass<T> where T : struct
    {
        public T e00;
    }
}

unsafe partial class GenericsTest
{
    public static int Main(string[] args)
    {
        try
        {
            Console.WriteLine("Testing IComInterface<bool>");
            TestIComInterfaceB();
            Console.WriteLine("Testing IComInterface<char>");
            TestIComInterfaceC();
            Console.WriteLine("Testing IComInterface<double>");
            TestIComInterfaceD();
            Console.WriteLine("Testing IComInterface<float>");
            TestIComInterfaceF();
            Console.WriteLine("Testing IComInterface<long>");
            TestIComInterfaceL();
            Console.WriteLine("Testing IComInterface<uint>");
            TestIComInterfaceU();
            Console.WriteLine();

            Console.WriteLine("Testing Nullable<bool>");
            TestNullableB();
            Console.WriteLine("Testing Nullable<char>");
            TestNullableC();
            Console.WriteLine("Testing Nullable<double>");
            TestNullableD();
            Console.WriteLine("Testing Nullable<float>");
            TestNullableF();
            Console.WriteLine("Testing Nullable<long>");
            TestNullableL();
            Console.WriteLine("Testing Nullable<uint>");
            TestNullableU();
            Console.WriteLine();

            Console.WriteLine("Testing Point1<bool>");
            TestPoint1B();
            Console.WriteLine("Testing Point1<char>");
            TestPoint1C();
            Console.WriteLine("Testing Point1<double>");
            TestPoint1D();
            Console.WriteLine("Testing Point1<float>");
            TestPoint1F();
            Console.WriteLine("Testing Point1<long>");
            TestPoint1L();
            Console.WriteLine("Testing Point1<uint>");
            TestPoint1U();
            Console.WriteLine();

            Console.WriteLine("Testing Point2<bool>");
            TestPoint2B();
            Console.WriteLine("Testing Point2<char>");
            TestPoint2C();
            Console.WriteLine("Testing Point2<double>");
            TestPoint2D();
            Console.WriteLine("Testing Point2<float>");
            TestPoint2F();
            Console.WriteLine("Testing Point2<long>");
            TestPoint2L();
            Console.WriteLine("Testing Point2<uint>");
            TestPoint2U();
            Console.WriteLine();

            Console.WriteLine("Testing Point3<bool>");
            TestPoint3B();
            Console.WriteLine("Testing Point3<char>");
            TestPoint3C();
            Console.WriteLine("Testing Point3<double>");
            TestPoint3D();
            Console.WriteLine("Testing Point3<float>");
            TestPoint3F();
            Console.WriteLine("Testing Point3<long>");
            TestPoint3L();
            Console.WriteLine("Testing Point3<uint>");
            TestPoint3U();
            Console.WriteLine();

            Console.WriteLine("Testing Point4<bool>");
            TestPoint4B();
            Console.WriteLine("Testing Point4<char>");
            TestPoint4C();
            Console.WriteLine("Testing Point4<double>");
            TestPoint4D();
            Console.WriteLine("Testing Point4<float>");
            TestPoint4F();
            Console.WriteLine("Testing Point4<long>");
            TestPoint4L();
            Console.WriteLine("Testing Point4<uint>");
            TestPoint4U();
            Console.WriteLine();

            Console.WriteLine("Testing ReadOnlySpan<bool>");
            TestReadOnlySpanB();
            Console.WriteLine("Testing ReadOnlySpan<char>");
            TestReadOnlySpanC();
            Console.WriteLine("Testing ReadOnlySpan<double>");
            TestReadOnlySpanD();
            Console.WriteLine("Testing ReadOnlySpan<float>");
            TestReadOnlySpanF();
            Console.WriteLine("Testing ReadOnlySpan<long>");
            TestReadOnlySpanL();
            Console.WriteLine("Testing ReadOnlySpan<uint>");
            TestReadOnlySpanU();
            Console.WriteLine();

            Console.WriteLine("Testing SequentialClass<bool>");
            TestSequentialClassB();
            Console.WriteLine("Testing SequentialClass<char>");
            TestSequentialClassC();
            Console.WriteLine("Testing SequentialClass<double>");
            TestSequentialClassD();
            Console.WriteLine("Testing SequentialClass<float>");
            TestSequentialClassF();
            Console.WriteLine("Testing SequentialClass<long>");
            TestSequentialClassL();
            Console.WriteLine("Testing SequentialClass<uint>");
            TestSequentialClassU();
            Console.WriteLine();

            Console.WriteLine("Testing Span<bool>");
            TestSpanB();
            Console.WriteLine("Testing Span<char>");
            TestSpanC();
            Console.WriteLine("Testing Span<double>");
            TestSpanD();
            Console.WriteLine("Testing Span<float>");
            TestSpanF();
            Console.WriteLine("Testing Span<long>");
            TestSpanL();
            Console.WriteLine("Testing Span<uint>");
            TestSpanU();
            Console.WriteLine();

            Console.WriteLine("Testing Vector64<bool>");
            TestVector64B();
            Console.WriteLine("Testing Vector64<char>");
            TestVector64C();
            Console.WriteLine("Testing Vector64<double>");
            TestVector64D();
            Console.WriteLine("Testing Vector64<float>");
            TestVector64F();
            Console.WriteLine("Testing Vector64<long>");
            TestVector64L();
            Console.WriteLine("Testing Vector64<uint>");
            TestVector64U();
            Console.WriteLine();

            Console.WriteLine("Testing Vector128<bool>");
            TestVector128B();
            Console.WriteLine("Testing Vector128<char>");
            TestVector128C();
            Console.WriteLine("Testing Vector128<double>");
            TestVector128D();
            Console.WriteLine("Testing Vector128<float>");
            TestVector128F();
            Console.WriteLine("Testing Vector128<long>");
            TestVector128L();
            Console.WriteLine("Testing Vector128<uint>");
            TestVector128U();
            Console.WriteLine();

            Console.WriteLine("Testing Vector256<bool>");
            TestVector256B();
            Console.WriteLine("Testing Vector256<char>");
            TestVector256C();
            Console.WriteLine("Testing Vector256<double>");
            TestVector256D();
            Console.WriteLine("Testing Vector256<float>");
            TestVector256F();
            Console.WriteLine("Testing Vector256<long>");
            TestVector256L();
            Console.WriteLine("Testing Vector256<uint>");
            TestVector256U();
            Console.WriteLine();

            Console.WriteLine("Testing Vector<bool>");
            TestVectorB();
            Console.WriteLine("Testing Vector<char>");
            TestVectorC();
            Console.WriteLine("Testing Vector<double>");
            TestVectorD();
            Console.WriteLine("Testing Vector<float>");
            TestVectorF();
            Console.WriteLine("Testing Vector<long>");
            TestVectorL();
            Console.WriteLine("Testing Vector<uint>");
            TestVectorU();
            Console.WriteLine();
        }
        catch (System.Exception ex)
        {
            Console.WriteLine(ex);
            return 0;
        }
        return 100;
    }
}
