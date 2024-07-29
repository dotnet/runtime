// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

public partial class VectorTest
{
    const int Pass = 100;
    const int Fail = -1;

    // An array containing 1,2,3,4...
    static readonly byte[] bytePattern;

    static VectorTest()
    {
        bytePattern = new byte[64]; // enough to test up to 512 bit SIMD vectors

        for (int i = 0; i < bytePattern.Length; i++)
            bytePattern[i] = (byte)(i + 1);
    }

    static T[] GetPatternAs<T>() where T : struct
    {
        T[] pattern = new T[bytePattern.Length]; // should be bytes.Length / sizeof(T) but sizeof(T) doesn't work
                                                 // so we'll settle for a larger than needed array
        Buffer.BlockCopy(bytePattern, 0, pattern, 0, bytePattern.Length);
        return pattern;
    }

    // Get a Vector<T> containing a specific bit pattern
    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<T> GetVector<T>() where T : struct => new Vector<T>(GetPatternAs<T>());

    // Cast a Vector<T> to specific vector type (we can cast from a generic value but the cast itself can't
    // be generic so we need one method for each possible vector element type).
    static Vector<Byte>   To_Byte   <T> (Vector<T> from) where T : struct => (Vector<Byte>  ) from;
    static Vector<SByte>  To_SByte  <T> (Vector<T> from) where T : struct => (Vector<SByte> ) from;
    static Vector<UInt16> To_UInt16 <T> (Vector<T> from) where T : struct => (Vector<UInt16>) from;
    static Vector<Int16>  To_Int16  <T> (Vector<T> from) where T : struct => (Vector<Int16> ) from;
    static Vector<UInt32> To_UInt32 <T> (Vector<T> from) where T : struct => (Vector<UInt32>) from;
    static Vector<Int32>  To_Int32  <T> (Vector<T> from) where T : struct => (Vector<Int32> ) from;
    static Vector<UInt64> To_UInt64 <T> (Vector<T> from) where T : struct => (Vector<UInt64>) from;
    static Vector<Int64>  To_Int64  <T> (Vector<T> from) where T : struct => (Vector<Int64> ) from;
    static Vector<Single> To_Single <T> (Vector<T> from) where T : struct => (Vector<Single>) from;
    static Vector<Double> To_Double <T> (Vector<T> from) where T : struct => (Vector<Double>) from;
    static Vector<nuint>  To_NUInt  <T> (Vector<T> from) where T : struct => (Vector<nuint>)  from;
    static Vector<nint>   To_NInt   <T> (Vector<T> from) where T : struct => (Vector<nint>)   from;

    // Check the result of casting Vector<T> to a specific vector type.
    static bool Test_Byte   <T>() where T : struct => GetVector<Byte>  () == To_Byte   ( GetVector<T>() );
    static bool Test_SByte  <T>() where T : struct => GetVector<SByte> () == To_SByte  ( GetVector<T>() );
    static bool Test_UInt16 <T>() where T : struct => GetVector<UInt16>() == To_UInt16 ( GetVector<T>() );
    static bool Test_Int16  <T>() where T : struct => GetVector<Int16> () == To_Int16  ( GetVector<T>() );
    static bool Test_UInt32 <T>() where T : struct => GetVector<UInt32>() == To_UInt32 ( GetVector<T>() );
    static bool Test_Int32  <T>() where T : struct => GetVector<Int32> () == To_Int32  ( GetVector<T>() );
    static bool Test_UInt64 <T>() where T : struct => GetVector<UInt64>() == To_UInt64 ( GetVector<T>() );
    static bool Test_Int64  <T>() where T : struct => GetVector<Int64> () == To_Int64  ( GetVector<T>() );
    static bool Test_Single <T>() where T : struct => GetVector<Single>() == To_Single ( GetVector<T>() );
    static bool Test_Double <T>() where T : struct => GetVector<Double>() == To_Double ( GetVector<T>() );
    static bool Test_NUInt  <T>() where T : struct => GetVector<nuint> () == To_NUInt  ( GetVector<T>() );
    static bool Test_NInt   <T>() where T : struct => GetVector<nint>  () == To_NInt   ( GetVector<T>() );

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool ReportFailure(bool success, [CallerLineNumber] int line = 0)
    {
        if (!success)
        {
            Console.WriteLine($"Line {line} - FAIL");
        }

        return success;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        bool succeeded = true;

        succeeded &= ReportFailure( Test_Byte  <Byte>() );
        succeeded &= ReportFailure( Test_SByte <Byte>() );
        succeeded &= ReportFailure( Test_UInt16<Byte>() );
        succeeded &= ReportFailure( Test_Int16 <Byte>() );
        succeeded &= ReportFailure( Test_UInt32<Byte>() );
        succeeded &= ReportFailure( Test_Int32 <Byte>() );
        succeeded &= ReportFailure( Test_UInt64<Byte>() );
        succeeded &= ReportFailure( Test_Int64 <Byte>() );
        succeeded &= ReportFailure( Test_Single<Byte>() );
        succeeded &= ReportFailure( Test_Double<Byte>() );
        succeeded &= ReportFailure( Test_NUInt <Byte>() );
        succeeded &= ReportFailure( Test_NInt  <Byte>() );

        succeeded &= ReportFailure( Test_Byte  <SByte>() );
        succeeded &= ReportFailure( Test_SByte <SByte>() );
        succeeded &= ReportFailure( Test_UInt16<SByte>() );
        succeeded &= ReportFailure( Test_Int16 <SByte>() );
        succeeded &= ReportFailure( Test_UInt32<SByte>() );
        succeeded &= ReportFailure( Test_Int32 <SByte>() );
        succeeded &= ReportFailure( Test_UInt64<SByte>() );
        succeeded &= ReportFailure( Test_Int64 <SByte>() );
        succeeded &= ReportFailure( Test_Single<SByte>() );
        succeeded &= ReportFailure( Test_Double<SByte>() );
        succeeded &= ReportFailure( Test_NUInt <SByte>() );
        succeeded &= ReportFailure( Test_NInt  <SByte>() );

        succeeded &= ReportFailure( Test_Byte  <UInt16>() );
        succeeded &= ReportFailure( Test_SByte <UInt16>() );
        succeeded &= ReportFailure( Test_UInt16<UInt16>() );
        succeeded &= ReportFailure( Test_Int16 <UInt16>() );
        succeeded &= ReportFailure( Test_UInt32<UInt16>() );
        succeeded &= ReportFailure( Test_Int32 <UInt16>() );
        succeeded &= ReportFailure( Test_UInt64<UInt16>() );
        succeeded &= ReportFailure( Test_Int64 <UInt16>() );
        succeeded &= ReportFailure( Test_Single<UInt16>() );
        succeeded &= ReportFailure( Test_Double<UInt16>() );
        succeeded &= ReportFailure( Test_NUInt <UInt16>() );
        succeeded &= ReportFailure( Test_NInt  <UInt16>() );

        succeeded &= ReportFailure( Test_Byte  <Int16>() );
        succeeded &= ReportFailure( Test_SByte <Int16>() );
        succeeded &= ReportFailure( Test_UInt16<Int16>() );
        succeeded &= ReportFailure( Test_Int16 <Int16>() );
        succeeded &= ReportFailure( Test_UInt32<Int16>() );
        succeeded &= ReportFailure( Test_Int32 <Int16>() );
        succeeded &= ReportFailure( Test_UInt64<Int16>() );
        succeeded &= ReportFailure( Test_Int64 <Int16>() );
        succeeded &= ReportFailure( Test_Single<Int16>() );
        succeeded &= ReportFailure( Test_Double<Int16>() );
        succeeded &= ReportFailure( Test_NUInt <Int16>() );
        succeeded &= ReportFailure( Test_NInt  <Int16>() );

        succeeded &= ReportFailure( Test_Byte  <UInt32>() );
        succeeded &= ReportFailure( Test_SByte <UInt32>() );
        succeeded &= ReportFailure( Test_UInt16<UInt32>() );
        succeeded &= ReportFailure( Test_Int16 <UInt32>() );
        succeeded &= ReportFailure( Test_UInt32<UInt32>() );
        succeeded &= ReportFailure( Test_Int32 <UInt32>() );
        succeeded &= ReportFailure( Test_UInt64<UInt32>() );
        succeeded &= ReportFailure( Test_Int64 <UInt32>() );
        succeeded &= ReportFailure( Test_Single<UInt32>() );
        succeeded &= ReportFailure( Test_Double<UInt32>() );
        succeeded &= ReportFailure( Test_NUInt <UInt32>() );
        succeeded &= ReportFailure( Test_NInt  <UInt32>() );

        succeeded &= ReportFailure( Test_Byte  <Int32>() );
        succeeded &= ReportFailure( Test_SByte <Int32>() );
        succeeded &= ReportFailure( Test_UInt16<Int32>() );
        succeeded &= ReportFailure( Test_Int16 <Int32>() );
        succeeded &= ReportFailure( Test_UInt32<Int32>() );
        succeeded &= ReportFailure( Test_Int32 <Int32>() );
        succeeded &= ReportFailure( Test_UInt64<Int32>() );
        succeeded &= ReportFailure( Test_Int64 <Int32>() );
        succeeded &= ReportFailure( Test_Single<Int32>() );
        succeeded &= ReportFailure( Test_Double<Int32>() );
        succeeded &= ReportFailure( Test_NUInt <Int32>() );
        succeeded &= ReportFailure( Test_NInt  <Int32>() );

        succeeded &= ReportFailure( Test_Byte  <UInt64>() );
        succeeded &= ReportFailure( Test_SByte <UInt64>() );
        succeeded &= ReportFailure( Test_UInt16<UInt64>() );
        succeeded &= ReportFailure( Test_Int16 <UInt64>() );
        succeeded &= ReportFailure( Test_UInt32<UInt64>() );
        succeeded &= ReportFailure( Test_Int32 <UInt64>() );
        succeeded &= ReportFailure( Test_UInt64<UInt64>() );
        succeeded &= ReportFailure( Test_Int64 <UInt64>() );
        succeeded &= ReportFailure( Test_Single<UInt64>() );
        succeeded &= ReportFailure( Test_Double<UInt64>() );
        succeeded &= ReportFailure( Test_NUInt <UInt64>() );
        succeeded &= ReportFailure( Test_NInt  <UInt64>() );

        succeeded &= ReportFailure( Test_Byte  <Int64>() );
        succeeded &= ReportFailure( Test_SByte <Int64>() );
        succeeded &= ReportFailure( Test_UInt16<Int64>() );
        succeeded &= ReportFailure( Test_Int16 <Int64>() );
        succeeded &= ReportFailure( Test_UInt32<Int64>() );
        succeeded &= ReportFailure( Test_Int32 <Int64>() );
        succeeded &= ReportFailure( Test_UInt64<Int64>() );
        succeeded &= ReportFailure( Test_Int64 <Int64>() );
        succeeded &= ReportFailure( Test_Single<Int64>() );
        succeeded &= ReportFailure( Test_Double<Int64>() );
        succeeded &= ReportFailure( Test_NUInt <Int64>() );
        succeeded &= ReportFailure( Test_NInt  <Int64>() );

        succeeded &= ReportFailure( Test_Byte  <Single>() );
        succeeded &= ReportFailure( Test_SByte <Single>() );
        succeeded &= ReportFailure( Test_UInt16<Single>() );
        succeeded &= ReportFailure( Test_Int16 <Single>() );
        succeeded &= ReportFailure( Test_UInt32<Single>() );
        succeeded &= ReportFailure( Test_Int32 <Single>() );
        succeeded &= ReportFailure( Test_UInt64<Single>() );
        succeeded &= ReportFailure( Test_Int64 <Single>() );
        succeeded &= ReportFailure( Test_Single<Single>() );
        succeeded &= ReportFailure( Test_Double<Single>() );
        succeeded &= ReportFailure( Test_NUInt <Single>() );
        succeeded &= ReportFailure( Test_NInt  <Single>() );

        succeeded &= ReportFailure( Test_Byte  <Double>() );
        succeeded &= ReportFailure( Test_SByte <Double>() );
        succeeded &= ReportFailure( Test_UInt16<Double>() );
        succeeded &= ReportFailure( Test_Int16 <Double>() );
        succeeded &= ReportFailure( Test_UInt32<Double>() );
        succeeded &= ReportFailure( Test_Int32 <Double>() );
        succeeded &= ReportFailure( Test_UInt64<Double>() );
        succeeded &= ReportFailure( Test_Int64 <Double>() );
        succeeded &= ReportFailure( Test_Single<Double>() );
        succeeded &= ReportFailure( Test_Double<Double>() );
        succeeded &= ReportFailure( Test_NUInt <Double>() );
        succeeded &= ReportFailure( Test_NInt  <Double>() );

        succeeded &= ReportFailure( Test_Byte  <nuint>() );
        succeeded &= ReportFailure( Test_SByte <nuint>() );
        succeeded &= ReportFailure( Test_UInt16<nuint>() );
        succeeded &= ReportFailure( Test_Int16 <nuint>() );
        succeeded &= ReportFailure( Test_UInt32<nuint>() );
        succeeded &= ReportFailure( Test_Int32 <nuint>() );
        succeeded &= ReportFailure( Test_UInt64<nuint>() );
        succeeded &= ReportFailure( Test_Int64 <nuint>() );
        succeeded &= ReportFailure( Test_Single<nuint>() );
        succeeded &= ReportFailure( Test_Double<nuint>() );
        succeeded &= ReportFailure( Test_NUInt <nuint>() );
        succeeded &= ReportFailure( Test_NInt  <nuint>() );

        succeeded &= ReportFailure( Test_Byte  <nint>() );
        succeeded &= ReportFailure( Test_SByte <nint>() );
        succeeded &= ReportFailure( Test_UInt16<nint>() );
        succeeded &= ReportFailure( Test_Int16 <nint>() );
        succeeded &= ReportFailure( Test_UInt32<nint>() );
        succeeded &= ReportFailure( Test_Int32 <nint>() );
        succeeded &= ReportFailure( Test_UInt64<nint>() );
        succeeded &= ReportFailure( Test_Int64 <nint>() );
        succeeded &= ReportFailure( Test_Single<nint>() );
        succeeded &= ReportFailure( Test_Double<nint>() );
        succeeded &= ReportFailure( Test_NUInt <nint>() );
        succeeded &= ReportFailure( Test_NInt  <nint>() );

        using (JitLog jitLog = new JitLog())
        {
            succeeded &= ReportFailure( jitLog.Check("op_Explicit", "Byte"  ) );
            succeeded &= ReportFailure( jitLog.Check("op_Explicit", "SByte" ) );
            succeeded &= ReportFailure( jitLog.Check("op_Explicit", "UInt16") );
            succeeded &= ReportFailure( jitLog.Check("op_Explicit", "Int16" ) );
            succeeded &= ReportFailure( jitLog.Check("op_Explicit", "UInt32") );
            succeeded &= ReportFailure( jitLog.Check("op_Explicit", "Int32" ) );
            succeeded &= ReportFailure( jitLog.Check("op_Explicit", "UInt64") );
            succeeded &= ReportFailure( jitLog.Check("op_Explicit", "Int64" ) );
            succeeded &= ReportFailure( jitLog.Check("op_Explicit", "Single") );
            succeeded &= ReportFailure( jitLog.Check("op_Explicit", "Double") );
            succeeded &= ReportFailure( jitLog.Check("op_Explicit", "IntPtr") );
            succeeded &= ReportFailure( jitLog.Check("op_Explicit", "UIntPtr") );
        }

        return succeeded ? Pass : Fail;
    }
}
