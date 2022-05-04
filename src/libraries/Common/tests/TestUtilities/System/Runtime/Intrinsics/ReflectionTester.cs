// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Runtime.Intrinsics;

public sealed class ReflectionTester
{
    // Tests must be deterministic, so we provide a seed
    private static readonly Random s_testRandom = new(1234);

    public static void Test(Type type)
    {
        foreach (MethodInfo methodInfo in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            object?[] parameters = GetRandomParameters(methodInfo).ToArray();
            try
            {
                methodInfo.Invoke(null, parameters);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is ArgumentOutOfRangeException or NotImplementedException)
            {
                // ignore
            }
            catch (Exception ex)
            {
                throw new Exception($"Exception while testing {type.FullName}.{methodInfo.Name}", ex);
            }
        }
    }

    private static unsafe object GetPtr(Type type)
    {
        void* ptr = NativeMemory.AlignedAlloc(512, 32);
        return Pointer.Box(ptr, type);
    }

    private static IEnumerable<object?> GetRandomParameters(MethodBase methodInfo)
    {
        foreach (ParameterInfo parameterInfo in methodInfo.GetParameters())
        {
            #region Pointer Types
            if (parameterInfo.ParameterType.IsPointer)
            {
                object ptr = GetPtr(parameterInfo.ParameterType);
                yield return ptr;
            }
            #endregion Pointer Types
            #region Enumerations
            else if (parameterInfo.ParameterType.IsEnum)
            {
                Array values = parameterInfo.ParameterType.GetEnumValues();
                yield return values.GetValue(s_testRandom.Next(values.Length - 1));
            }
            #endregion
            #region Scalar Types
            else if (parameterInfo.ParameterType == typeof(sbyte))
            {
                byte[] buf = new byte[1];
                s_testRandom.NextBytes(buf);
                yield return (sbyte)buf[0];
            }
            else if (parameterInfo.ParameterType == typeof(ushort))
            {
                yield return (ushort)s_testRandom.Next(ushort.MinValue, ushort.MaxValue);
            }
            else if (parameterInfo.ParameterType == typeof(uint))
            {
                int i = s_testRandom.Next();
                yield return Unsafe.As<int, uint>(ref i);
            }
            else if (parameterInfo.ParameterType == typeof(long))
            {
                long l = s_testRandom.NextInt64();
                yield return Unsafe.As<long, ulong>(ref l);
            }
            else if (parameterInfo.ParameterType == typeof(nuint))
            {
                nint ni = s_testRandom.Next();
                yield return Unsafe.As<nint, nuint>(ref ni);
            }
            else if (parameterInfo.ParameterType == typeof(byte))
            {
                byte[] buf = new byte[1];
                s_testRandom.NextBytes(buf);
                yield return buf[0];
            }
            else if (parameterInfo.ParameterType == typeof(short))
            {
                yield return (short)s_testRandom.Next(short.MinValue, short.MaxValue);
            }
            else if (parameterInfo.ParameterType == typeof(int))
            {
                yield return s_testRandom.Next();
            }
            else if (parameterInfo.ParameterType == typeof(long))
            {
                yield return s_testRandom.NextInt64();
            }
            else if (parameterInfo.ParameterType == typeof(nint))
            {
                yield return (nint)s_testRandom.Next();
            }
            else if (parameterInfo.ParameterType == typeof(Half))
            {
                short s = (short)s_testRandom.Next(short.MinValue, short.MaxValue);
                yield return Unsafe.As<short, Half>(ref s);
            }
            else if (parameterInfo.ParameterType == typeof(float))
            {
                int i = s_testRandom.Next();
                yield return Unsafe.As<int, float>(ref i);
            }
            else if (parameterInfo.ParameterType == typeof(long))
            {
                long l = s_testRandom.NextInt64();
                yield return Unsafe.As<long, double>(ref l);
            }
            #endregion Scalar Types
            #region Vector64<T>
            else if (parameterInfo.ParameterType == typeof(Vector64<sbyte>))
            {
                yield return RandomVector64<sbyte>();
            }
            else if (parameterInfo.ParameterType == typeof(Vector64<short>))
            {
                yield return RandomVector64<short>();
            }
            else if (parameterInfo.ParameterType == typeof(Vector64<int>))
            {
                yield return RandomVector64<int>();
            }
            else if (parameterInfo.ParameterType == typeof(Vector64<long>))
            {
                yield return RandomVector64<long>();
            }
            else if (parameterInfo.ParameterType == typeof(Vector64<byte>))
            {
                yield return RandomVector64<byte>();
            }
            else if (parameterInfo.ParameterType == typeof(Vector64<nint>))
            {
                yield return RandomVector64<nint>();
            }
            else if (parameterInfo.ParameterType == typeof(Vector64<byte>))
            {
                yield return RandomVector64<byte>();
            }
            else if (parameterInfo.ParameterType == typeof(Vector64<ushort>))
            {
                yield return RandomVector64<ushort>();
            }
            else if (parameterInfo.ParameterType == typeof(Vector64<uint>))
            {
                yield return RandomVector64<uint>();
            }
            else if (parameterInfo.ParameterType == typeof(Vector64<ulong>))
            {
                yield return RandomVector64<ulong>();
            }
            else if (parameterInfo.ParameterType == typeof(Vector64<nuint>))
            {
                yield return RandomVector64<nuint>();
            }
            else if (parameterInfo.ParameterType == typeof(Vector64<Half>))
            {
                yield return RandomVector64<Half>();
            }
            else if (parameterInfo.ParameterType == typeof(Vector64<float>))
            {
                yield return RandomVector64<float>();
            }
            else if (parameterInfo.ParameterType == typeof(Vector64<double>))
            {
                yield return RandomVector64<double>();
            }
            #endregion Vector64<T>
            #region Vector128<T>
            else if (parameterInfo.ParameterType == typeof(Vector128<sbyte>))
            {
                yield return RandomVector128<sbyte>();
            }
            else if (parameterInfo.ParameterType == typeof(Vector128<short>))
            {
                yield return RandomVector128<short>();
            }
            else if (parameterInfo.ParameterType == typeof(Vector128<int>))
            {
                yield return RandomVector128<int>();
            }
            else if (parameterInfo.ParameterType == typeof(Vector128<long>))
            {
                yield return RandomVector128<long>();
            }
            else if (parameterInfo.ParameterType == typeof(Vector128<byte>))
            {
                yield return RandomVector128<byte>();
            }
            else if (parameterInfo.ParameterType == typeof(Vector128<nint>))
            {
                yield return RandomVector128<nint>();
            }
            else if (parameterInfo.ParameterType == typeof(Vector128<byte>))
            {
                yield return RandomVector128<byte>();
            }
            else if (parameterInfo.ParameterType == typeof(Vector128<ushort>))
            {
                yield return RandomVector128<ushort>();
            }
            else if (parameterInfo.ParameterType == typeof(Vector128<uint>))
            {
                yield return RandomVector128<uint>();
            }
            else if (parameterInfo.ParameterType == typeof(Vector128<ulong>))
            {
                yield return RandomVector128<ulong>();
            }
            else if (parameterInfo.ParameterType == typeof(Vector128<nuint>))
            {
                yield return RandomVector128<nuint>();
            }
            else if (parameterInfo.ParameterType == typeof(Vector128<Half>))
            {
                yield return RandomVector128<Half>();
            }
            else if (parameterInfo.ParameterType == typeof(Vector128<float>))
            {
                yield return RandomVector128<float>();
            }
            else if (parameterInfo.ParameterType == typeof(Vector128<double>))
            {
                yield return RandomVector128<double>();
            }
            #endregion Vector128<T>
            #region Vector256<T>
            else if (parameterInfo.ParameterType == typeof(Vector256<sbyte>))
            {
                yield return RandomVector256<sbyte>();
            }
            else if (parameterInfo.ParameterType == typeof(Vector256<short>))
            {
                yield return RandomVector256<short>();
            }
            else if (parameterInfo.ParameterType == typeof(Vector256<int>))
            {
                yield return RandomVector256<int>();
            }
            else if (parameterInfo.ParameterType == typeof(Vector256<long>))
            {
                yield return RandomVector256<long>();
            }
            else if (parameterInfo.ParameterType == typeof(Vector256<byte>))
            {
                yield return RandomVector256<byte>();
            }
            else if (parameterInfo.ParameterType == typeof(Vector256<nint>))
            {
                yield return RandomVector256<nint>();
            }
            else if (parameterInfo.ParameterType == typeof(Vector256<byte>))
            {
                yield return RandomVector256<byte>();
            }
            else if (parameterInfo.ParameterType == typeof(Vector256<ushort>))
            {
                yield return RandomVector256<ushort>();
            }
            else if (parameterInfo.ParameterType == typeof(Vector256<uint>))
            {
                yield return RandomVector256<uint>();
            }
            else if (parameterInfo.ParameterType == typeof(Vector256<ulong>))
            {
                yield return RandomVector256<ulong>();
            }
            else if (parameterInfo.ParameterType == typeof(Vector256<nuint>))
            {
                yield return RandomVector256<nuint>();
            }
            else if (parameterInfo.ParameterType == typeof(Vector256<Half>))
            {
                yield return RandomVector256<Half>();
            }
            else if (parameterInfo.ParameterType == typeof(Vector256<float>))
            {
                yield return RandomVector256<float>();
            }
            else if (parameterInfo.ParameterType == typeof(Vector256<double>))
            {
                yield return RandomVector256<double>();
            }
            #endregion Vector256<T>
            else
            {
                throw new InvalidOperationException($"Invalid parameter type of {parameterInfo.ParameterType}");
            }
        }
    }

    private static Vector64<T> RandomVector64<T>() where T : struct
    {
        Vector64<long> vector = Vector64.Create(s_testRandom.NextInt64());
        return Unsafe.As<Vector64<long>, Vector64<T>>(ref vector);
    }

    private static Vector128<T> RandomVector128<T>() where T : struct
    {
        var vector = Vector128.Create(RandomVector64<long>(), RandomVector64<long>());
        return Unsafe.As<Vector128<long>, Vector128<T>>(ref vector);
    }

    private static Vector256<T> RandomVector256<T>() where T : struct
    {
        var vector = Vector256.Create(RandomVector128<long>(), RandomVector128<long>());
        return Unsafe.As<Vector256<long>, Vector256<T>>(ref vector);
    }

}
