using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm.Arm64;

namespace Arm64intrisicsTest
{
    struct DataSet<TBaseType, TVectorType>
            where TBaseType : struct
            where TVectorType : new()
    {
        private static TVectorType _vectorX;
        private static TVectorType _vectorY;

        public static TVectorType vectorX { get { return _vectorX; }}
        public static TVectorType vectorY { get { return _vectorY; }}

        public static TBaseType[] arrayX { get; private set; }
        public static TBaseType[] arrayY { get; private set; }

        public static unsafe void setData(TBaseType[] x, TBaseType[] y)
        {
            arrayX = x;
            arrayY = y;

            GCHandle handleSrc = GCHandle.Alloc(x, GCHandleType.Pinned);

            try
            {
                var ptrSrc = (byte*) handleSrc.AddrOfPinnedObject().ToPointer();

                _vectorX = Unsafe.Read<TVectorType>(ptrSrc);
            }
            finally
            {
                handleSrc.Free();
            }


            handleSrc = GCHandle.Alloc(y, GCHandleType.Pinned);

            try
            {
                var ptrSrc = (byte*) handleSrc.AddrOfPinnedObject().ToPointer();

                _vectorY = Unsafe.Read<TVectorType>(ptrSrc);
            }
            finally
            {
                handleSrc.Free();
            }
        }
    };

    class Program
    {
        static unsafe TBaseType[] writeVector<TBaseType, TVectorType>(TVectorType src)
            where TBaseType : struct
            where TVectorType : new()
        {
            var length = Unsafe.SizeOf<TVectorType>() / Unsafe.SizeOf<TBaseType>();
            var dst = new TBaseType[length];

            GCHandle handleSrc = GCHandle.Alloc(src, GCHandleType.Pinned);
            GCHandle handleDst = GCHandle.Alloc(dst, GCHandleType.Pinned);

            try
            {
                var ptrSrc = (byte*) handleSrc.AddrOfPinnedObject().ToPointer();
                var ptrDst = (byte*) handleDst.AddrOfPinnedObject().ToPointer();

                for (int i = 0; i < Unsafe.SizeOf<TVectorType>(); ++i)
                {
                    ptrDst[i] = ptrSrc[i];
                }
            }
            finally
            {
                handleSrc.Free();
                handleDst.Free();
            }

            return dst;
        }

        static void testBinOp<TBaseType, TVectorType>(String testCaseDescription,
                                                      Func<TVectorType, TVectorType, TVectorType> binOp,
                                                      Func<TBaseType, TBaseType, TBaseType> check)
            where TBaseType : struct, IComparable
            where TVectorType : new()
        {
            testBinOp<TBaseType, TVectorType, TBaseType, TVectorType>(testCaseDescription, binOp, check);
        }

        static void dumpVector<TBaseType, TVectorType>(String name, TVectorType vector)
            where TBaseType : struct, IComparable
            where TVectorType : new()
        {
            var result = writeVector<TBaseType, TVectorType>(vector);

            Console.Write(name);
            Console.Write(" : { ");
            for (int i = 0; i < result.Length; i++)
            {
                Console.Write($"{result[i]} ");
            }
            Console.WriteLine("}");
        }

        static void testBinOp<TBaseType, TVectorType, TBaseReturnType, TVectorReturnType>(String testCaseDescription,
                                                      Func<TVectorType, TVectorType, TVectorReturnType> binOp,
                                                      Func<TBaseType, TBaseType, TBaseReturnType> check)
            where TBaseType : struct, IComparable
            where TVectorType : new()
            where TBaseReturnType : struct, IComparable
            where TVectorReturnType : new()
        {
            bool failed = false;
            try
            {
                var vLeft  = DataSet<TBaseType, TVectorType>.vectorX;
                var vRight = DataSet<TBaseType, TVectorType>.vectorY;

                var vResult = binOp(vLeft, vRight);

                var result = writeVector<TBaseReturnType, TVectorReturnType>(vResult);

                var left   = DataSet<TBaseType, TVectorType>.arrayX;
                var right  = DataSet<TBaseType, TVectorType>.arrayY;

                for (int i = 0; i < result.Length; i++)
                {
                    var expected = check(left[i], right[i]);

                    if (result[i].CompareTo(expected) != 0)
                    {
                        if(!failed)
                        {
                            Console.WriteLine($"testBinOp<{typeof(TBaseType).Name}, {typeof(TVectorType).Name} >{testCaseDescription}: Check Failed");
                            dumpVector<TBaseType, TVectorType>("vLeft", vLeft);
                            dumpVector<TBaseType, TVectorType>("vRight", vRight);
                            dumpVector<TBaseReturnType, TVectorReturnType>("vResult", vResult);
                        }
                        Console.WriteLine($"check({left[i]}, {right[i]}) : result[{i}] = {result[i]}, expected {expected}");
                        failed = true;
                    }
                }
            }
            catch
            {
                Console.WriteLine($"testBinOp<{typeof(TBaseType).Name}, {typeof(TVectorType).Name} >{testCaseDescription}: Unexpected exception");
                throw;
            }

            if (failed)
            {
                throw new Exception($"testBinOp<{typeof(TBaseType).Name}, {typeof(TVectorType).Name} >{testCaseDescription}: Failed");
            }
        }

        static void testExtractOp<TBaseType, TVectorType>(String testCaseDescription,
                                                      Func<TVectorType, TBaseType> extractOp,
                                                      Func<TBaseType[], TBaseType> check)
            where TBaseType : struct, IComparable
            where TVectorType : new()
        {
            bool failed = false;
            try
            {
                var vLeft  = DataSet<TBaseType, TVectorType>.vectorX;

                var vResult = extractOp(vLeft);

                var left   = DataSet<TBaseType, TVectorType>.arrayX;

                var expected = check(left);

                if (vResult.CompareTo(expected) != 0)
                {
                    if(!failed)
                    {
                        Console.WriteLine($"testExtractOp<{typeof(TBaseType).Name}, {typeof(TVectorType).Name} >{testCaseDescription}: Check Failed");
                    }
                    Console.WriteLine($"check(left) : vResult = {vResult}, expected {expected}");
                    failed = true;
                }
            }
            catch
            {
                Console.WriteLine($"testBinOp<{typeof(TBaseType).Name}, {typeof(TVectorType).Name} >{testCaseDescription}: Unexpected exception");
                throw;
            }

            if (failed)
            {
                throw new Exception($"testBinOp<{typeof(TBaseType).Name}, {typeof(TVectorType).Name} >{testCaseDescription}: Failed");
            }
        }

        static void testPermuteOp<TBaseType, TVectorType>(String testCaseDescription,
                                                      Func<TVectorType, TVectorType, TVectorType> binOp,
                                                      Func<int, TBaseType[], TBaseType[], TBaseType> check)
            where TBaseType : struct, IComparable
            where TVectorType : new()
        {
            bool failed = false;
            try
            {
                var vLeft  = DataSet<TBaseType, TVectorType>.vectorX;
                var vRight = DataSet<TBaseType, TVectorType>.vectorY;

                var vResult = binOp(vLeft, vRight);

                var result = writeVector<TBaseType, TVectorType>(vResult);

                var left   = DataSet<TBaseType, TVectorType>.arrayX;
                var right  = DataSet<TBaseType, TVectorType>.arrayY;

                for (int i = 0; i < result.Length; i++)
                {
                    var expected = check(i, left, right);

                    if (result[i].CompareTo(expected) != 0)
                    {
                        if(!failed)
                        {
                            Console.WriteLine($"testPermuteOp<{typeof(TBaseType).Name}, {typeof(TVectorType).Name} >{testCaseDescription}: Check Failed");
                        }
                        Console.WriteLine($"check({left[i]}, {right[i]}) : result[{i}] = {result[i]}, expected {expected}");
                        failed = true;
                    }
                }
            }
            catch
            {
                Console.WriteLine($"testPermuteOp<{typeof(TBaseType).Name}, {typeof(TVectorType).Name} >{testCaseDescription}: Unexpected exception");
                throw;
            }

            if (failed)
            {
                throw new Exception($"testPermuteOp<{typeof(TBaseType).Name}, {typeof(TVectorType).Name} >{testCaseDescription}: Failed");
            }
        }

        static void testThrowsArgumentOutOfRangeException<TBaseType, TVectorType>(String testCaseDescription,
                                                                Func<TVectorType, TVectorType, TBaseType> binOp)
            where TBaseType : struct
            where TVectorType : struct
        {
            testThrowsArgumentOutOfRangeException<TBaseType, TVectorType, TBaseType>(testCaseDescription, binOp);
        }

        static void testThrowsArgumentOutOfRangeException<TBaseType, TVectorType, TReturnType>(String testCaseDescription,
                                                                Func<TVectorType, TVectorType, TReturnType> binOp)
            where TBaseType : struct
            where TVectorType : struct
            where TReturnType : struct
        {
            var v = DataSet<TBaseType, TVectorType>.vectorX;

            bool caughtArgRangeEx = false;

            try
            {
                binOp(v,v);
            }
            catch (ArgumentOutOfRangeException)
            {
                caughtArgRangeEx = true;
            }
            catch
            {
                Console.WriteLine($"testThrowsArgumentOutOfRangeException: Unexpected exception");
                throw;
            }

            if (caughtArgRangeEx == false)
            {
                throw new Exception($"testThrowsArgumentOutOfRangeException<{typeof(TBaseType).Name}, {typeof(TVectorType).Name} >{testCaseDescription}: Failed");
            }
        }

        static void testThrowsTypeNotSupported<TVectorType>(String testCaseDescription,
                                                                Func<TVectorType, TVectorType, TVectorType> binOp)
            where TVectorType : new()
        {
            TVectorType v = new TVectorType();

            bool notSupported = false;

            try
            {
                binOp(v,v);
            }
            catch (NotSupportedException e)
            {
                notSupported = true;
            }
            catch
            {
                Console.WriteLine($"testThrowsTypeNotSupported: Unexpected exception");
                throw;
            }

            if (notSupported == false)
            {
                throw new Exception($"testThrowsTypeNotSupported<{typeof(TVectorType).Name} >{testCaseDescription}: Failed");
            }
        }

        static void testThrowsPlatformNotSupported<TVectorType>(String testCaseDescription,
                                                                Func<TVectorType, TVectorType, TVectorType> binOp)
            where TVectorType : new()
        {
            testThrowsPlatformNotSupported<TVectorType, TVectorType>(testCaseDescription, binOp);
        }

        static void testThrowsPlatformNotSupported<TVectorType, TVectorTypeReturn>(String testCaseDescription,
                                                                Func<TVectorType, TVectorType, TVectorTypeReturn> binOp)
            where TVectorType : new()
        {
            TVectorType v = new TVectorType();

            bool notSupported = false;

            try
            {
                binOp(v,v);
            }
            catch (PlatformNotSupportedException)
            {
                notSupported = true;
            }
            catch
            {
                Console.WriteLine($"testThrowsPlatformNotSupported: Unexpected exception");
                throw;
            }

            if (notSupported == false)
            {
                throw new Exception($"testThrowsPlatformNotSupported<{typeof(TVectorType).Name} >{testCaseDescription}: Failed");
            }
        }

        static uint bits(float x)
        {
            return BitConverter.ToUInt32(BitConverter.GetBytes(x));
        }

        static ulong bits(double x)
        {
            return BitConverter.ToUInt64(BitConverter.GetBytes(x));
        }

        static float bitsToFloat(uint x)
        {
            return BitConverter.ToSingle(BitConverter.GetBytes(x));
        }

        static double bitsToDouble(ulong x)
        {
            return BitConverter.ToDouble(BitConverter.GetBytes(x));
        }

        static ulong bitsToUint64<T>(T x)
        {
            return Unsafe.As<T, ulong>(ref x) & ~(~0UL << 8*Unsafe.SizeOf<T>());
        }

        static T boolTo<T>(bool x)
            where T : IConvertible
        {
            ulong result = x ? ~0UL : 0UL;

            if (typeof(T) == typeof(double)) return (T) Convert.ChangeType(     bitsToDouble(result), typeof(T));
            if (typeof(T) == typeof(float))  return (T) Convert.ChangeType(bitsToFloat((uint)result), typeof(T));
            if (typeof(T) == typeof(byte)  ) return (T) Convert.ChangeType(          (byte)  result,  typeof(T));
            if (typeof(T) == typeof(sbyte) ) return (T) Convert.ChangeType(          (sbyte) result,  typeof(T));
            if (typeof(T) == typeof(ushort)) return (T) Convert.ChangeType(          (ushort)result,  typeof(T));
            if (typeof(T) == typeof(short) ) return (T) Convert.ChangeType(          (short) result,  typeof(T));
            if (typeof(T) == typeof(uint)  ) return (T) Convert.ChangeType(          (uint)  result,  typeof(T));
            if (typeof(T) == typeof(int)   ) return (T) Convert.ChangeType(          (int)   result,  typeof(T));
            if (typeof(T) == typeof(ulong) ) return (T) Convert.ChangeType(          (ulong) result,  typeof(T));
            if (typeof(T) == typeof(long)  ) return (T) Convert.ChangeType(          (long)  result,  typeof(T));

            throw new Exception("Unexpected Type");
        }

        static T popCount<T>(T x)
            where T : IConvertible
        {
            ulong result = 0;
            ulong value = bitsToUint64(x);

            while(value != 0)
            {
                result++;
                value = value & (value - 1);
            }

            return (T) Convert.ChangeType(result, typeof(T));

            throw new Exception("Unexpected Type");
        }

        static T leadingZero<T>(T x)
            where T : IConvertible
        {
            ulong compare = 0x1UL << (8*Unsafe.SizeOf<T>() - 1);
            ulong result = 0;
            ulong value = bitsToUint64(x);

            while(value < compare)
            {
                result++;
                compare >>= 1;
            }

            return (T) Convert.ChangeType(result,  typeof(T));

            throw new Exception("Unexpected Type");
        }

        static T leadingSign<T>(T x)
            where T : IConvertible
        {
            ulong value = bitsToUint64(x);
            ulong signBit = value & (0x1UL << (8*Unsafe.SizeOf<T>() - 1));
            ulong result = 0;

            if (signBit == 0)
            {
                result = (ulong) Convert.ChangeType(leadingZero(x), typeof(ulong));
            }
            else
            {
                result = (ulong) Convert.ChangeType(leadingZero((T) Convert.ChangeType(value ^ (signBit + (signBit - 1)),  typeof(T))), typeof(ulong));
            }

            return (T) Convert.ChangeType(result - 1, typeof(T));
        }

        static void TestAbs()
        {
            String name = "Abs";

            if (Simd.IsSupported)
            {
                testBinOp<float,  Vector128<float> , float,  Vector128<float >>(name, (x, y) => Simd.Abs(x), (x, y) =>          Math.Abs(x));
                testBinOp<double, Vector128<double>, double, Vector128<double>>(name, (x, y) => Simd.Abs(x), (x, y) =>          Math.Abs(x));
                testBinOp<sbyte,  Vector128<sbyte> , byte,   Vector128<byte  >>(name, (x, y) => Simd.Abs(x), (x, y) => (byte)   Math.Abs(x));
                testBinOp<short,  Vector128<short> , ushort, Vector128<ushort>>(name, (x, y) => Simd.Abs(x), (x, y) => (ushort) Math.Abs(x));
                testBinOp<int,    Vector128<int>   , uint,   Vector128<uint  >>(name, (x, y) => Simd.Abs(x), (x, y) => (uint)   Math.Abs(x));
                testBinOp<long,   Vector128<long>  , ulong,  Vector128<ulong >>(name, (x, y) => Simd.Abs(x), (x, y) => (ulong)  Math.Abs(x));
                testBinOp<float,  Vector64<float>  , float,  Vector64< float >>(name, (x, y) => Simd.Abs(x), (x, y) =>          Math.Abs(x));
                testBinOp<sbyte,  Vector64<sbyte>  , byte,   Vector64< byte  >>(name, (x, y) => Simd.Abs(x), (x, y) => (byte)   Math.Abs(x));
                testBinOp<short,  Vector64<short>  , ushort, Vector64< ushort>>(name, (x, y) => Simd.Abs(x), (x, y) => (ushort) Math.Abs(x));
                testBinOp<int,    Vector64<int>    , uint,   Vector64< uint  >>(name, (x, y) => Simd.Abs(x), (x, y) => (uint)   Math.Abs(x));
            }
            else
            {
                testThrowsPlatformNotSupported<Vector64<float>  , Vector64< float >>(name, (x, y) => Simd.Abs(x));
                testThrowsPlatformNotSupported<Vector64<sbyte>  , Vector64< byte  >>(name, (x, y) => Simd.Abs(x));
                testThrowsPlatformNotSupported<Vector64<short>  , Vector64< ushort>>(name, (x, y) => Simd.Abs(x));
                testThrowsPlatformNotSupported<Vector64<int>    , Vector64< uint  >>(name, (x, y) => Simd.Abs(x));
                testThrowsPlatformNotSupported<Vector128<float> , Vector128<float >>(name, (x, y) => Simd.Abs(x));
                testThrowsPlatformNotSupported<Vector128<double>, Vector128<double>>(name, (x, y) => Simd.Abs(x));
                testThrowsPlatformNotSupported<Vector128<sbyte> , Vector128<byte  >>(name, (x, y) => Simd.Abs(x));
                testThrowsPlatformNotSupported<Vector128<short> , Vector128<ushort>>(name, (x, y) => Simd.Abs(x));
                testThrowsPlatformNotSupported<Vector128<int>   , Vector128<uint  >>(name, (x, y) => Simd.Abs(x));
                testThrowsPlatformNotSupported<Vector128<long>  , Vector128<ulong >>(name, (x, y) => Simd.Abs(x));
            }

            Console.WriteLine($"Test{name} passed");
        }

        static void TestAdd()
        {
            String name = "Add";

            if (Simd.IsSupported)
            {
                testBinOp<float,  Vector128<float >>(name, (x, y) => Simd.Add(x, y), (x, y) =>         (x + y));
                testBinOp<double, Vector128<double>>(name, (x, y) => Simd.Add(x, y), (x, y) =>         (x + y));
                testBinOp<sbyte,  Vector128<sbyte >>(name, (x, y) => Simd.Add(x, y), (x, y) => (sbyte) (x + y));
                testBinOp<byte,   Vector128<byte  >>(name, (x, y) => Simd.Add(x, y), (x, y) => (byte)  (x + y));
                testBinOp<short,  Vector128<short >>(name, (x, y) => Simd.Add(x, y), (x, y) => (short) (x + y));
                testBinOp<ushort, Vector128<ushort>>(name, (x, y) => Simd.Add(x, y), (x, y) => (ushort)(x + y));
                testBinOp<int,    Vector128<int   >>(name, (x, y) => Simd.Add(x, y), (x, y) =>         (x + y));
                testBinOp<uint,   Vector128<uint  >>(name, (x, y) => Simd.Add(x, y), (x, y) =>         (x + y));
                testBinOp<long,   Vector128<long  >>(name, (x, y) => Simd.Add(x, y), (x, y) =>         (x + y));
                testBinOp<ulong,  Vector128<ulong >>(name, (x, y) => Simd.Add(x, y), (x, y) =>         (x + y));
                testBinOp<float,  Vector64< float >>(name, (x, y) => Simd.Add(x, y), (x, y) =>         (x + y));
                testBinOp<sbyte,  Vector64< sbyte >>(name, (x, y) => Simd.Add(x, y), (x, y) => (sbyte) (x + y));
                testBinOp<byte,   Vector64< byte  >>(name, (x, y) => Simd.Add(x, y), (x, y) => (byte)  (x + y));
                testBinOp<short,  Vector64< short >>(name, (x, y) => Simd.Add(x, y), (x, y) => (short) (x + y));
                testBinOp<ushort, Vector64< ushort>>(name, (x, y) => Simd.Add(x, y), (x, y) => (ushort)(x + y));
                testBinOp<int,    Vector64< int   >>(name, (x, y) => Simd.Add(x, y), (x, y) =>         (x + y));
                testBinOp<uint,   Vector64< uint  >>(name, (x, y) => Simd.Add(x, y), (x, y) =>         (x + y));

                testThrowsTypeNotSupported<Vector128<Vector128<long> >>(name, (x, y) => Simd.Add(x, y));

                testThrowsTypeNotSupported<Vector64< long >>(name, (x, y) => Simd.Add(x, y));
                testThrowsTypeNotSupported<Vector64< ulong>>(name, (x, y) => Simd.Add(x, y));
                testThrowsTypeNotSupported<Vector64<double>>(name, (x, y) => Simd.Add(x, y));
            }
            else
            {
                testThrowsPlatformNotSupported<Vector64< float >>(name, (x, y) => Simd.Add(x, y));
                testThrowsPlatformNotSupported<Vector64< double>>(name, (x, y) => Simd.Add(x, y));
                testThrowsPlatformNotSupported<Vector64< sbyte >>(name, (x, y) => Simd.Add(x, y));
                testThrowsPlatformNotSupported<Vector64< byte  >>(name, (x, y) => Simd.Add(x, y));
                testThrowsPlatformNotSupported<Vector64< short >>(name, (x, y) => Simd.Add(x, y));
                testThrowsPlatformNotSupported<Vector64< ushort>>(name, (x, y) => Simd.Add(x, y));
                testThrowsPlatformNotSupported<Vector64< int   >>(name, (x, y) => Simd.Add(x, y));
                testThrowsPlatformNotSupported<Vector64< uint  >>(name, (x, y) => Simd.Add(x, y));
                testThrowsPlatformNotSupported<Vector64< long  >>(name, (x, y) => Simd.Add(x, y));
                testThrowsPlatformNotSupported<Vector64< ulong >>(name, (x, y) => Simd.Add(x, y));
                testThrowsPlatformNotSupported<Vector128<float >>(name, (x, y) => Simd.Add(x, y));
                testThrowsPlatformNotSupported<Vector128<double>>(name, (x, y) => Simd.Add(x, y));
                testThrowsPlatformNotSupported<Vector128<sbyte >>(name, (x, y) => Simd.Add(x, y));
                testThrowsPlatformNotSupported<Vector128<byte  >>(name, (x, y) => Simd.Add(x, y));
                testThrowsPlatformNotSupported<Vector128<short >>(name, (x, y) => Simd.Add(x, y));
                testThrowsPlatformNotSupported<Vector128<ushort>>(name, (x, y) => Simd.Add(x, y));
                testThrowsPlatformNotSupported<Vector128<int   >>(name, (x, y) => Simd.Add(x, y));
                testThrowsPlatformNotSupported<Vector128<uint  >>(name, (x, y) => Simd.Add(x, y));
                testThrowsPlatformNotSupported<Vector128<long  >>(name, (x, y) => Simd.Add(x, y));
                testThrowsPlatformNotSupported<Vector128<ulong >>(name, (x, y) => Simd.Add(x, y));
            }

            Console.WriteLine($"Test{name} passed");
        }

        static void TestAnd()
        {
            String name = "And";

            if (Simd.IsSupported)
            {
                testBinOp<float,  Vector128<float >>(name, (x, y) => Simd.And(x, y), (x, y) => bitsToFloat (bits(x) & bits(y)));
                testBinOp<double, Vector128<double>>(name, (x, y) => Simd.And(x, y), (x, y) => bitsToDouble(bits(x) & bits(y)));
                testBinOp<sbyte,  Vector128<sbyte >>(name, (x, y) => Simd.And(x, y), (x, y) => (sbyte)     (     x  &      y ));
                testBinOp<byte,   Vector128<byte  >>(name, (x, y) => Simd.And(x, y), (x, y) => (byte)      (     x  &      y ));
                testBinOp<short,  Vector128<short >>(name, (x, y) => Simd.And(x, y), (x, y) => (short)     (     x  &      y ));
                testBinOp<ushort, Vector128<ushort>>(name, (x, y) => Simd.And(x, y), (x, y) => (ushort)    (     x  &      y ));
                testBinOp<int,    Vector128<int   >>(name, (x, y) => Simd.And(x, y), (x, y) =>             (     x  &      y ));
                testBinOp<uint,   Vector128<uint  >>(name, (x, y) => Simd.And(x, y), (x, y) =>             (     x  &      y ));
                testBinOp<long,   Vector128<long  >>(name, (x, y) => Simd.And(x, y), (x, y) =>             (     x  &      y ));
                testBinOp<ulong,  Vector128<ulong >>(name, (x, y) => Simd.And(x, y), (x, y) =>             (     x  &      y ));
                testBinOp<float,  Vector64< float >>(name, (x, y) => Simd.And(x, y), (x, y) => bitsToFloat (bits(x) & bits(y)));
                testBinOp<sbyte,  Vector64< sbyte >>(name, (x, y) => Simd.And(x, y), (x, y) => (sbyte)     (     x  &      y ));
                testBinOp<byte,   Vector64< byte  >>(name, (x, y) => Simd.And(x, y), (x, y) => (byte)      (     x  &      y ));
                testBinOp<short,  Vector64< short >>(name, (x, y) => Simd.And(x, y), (x, y) => (short)     (     x  &      y ));
                testBinOp<ushort, Vector64< ushort>>(name, (x, y) => Simd.And(x, y), (x, y) => (ushort)    (     x  &      y ));
                testBinOp<int,    Vector64< int   >>(name, (x, y) => Simd.And(x, y), (x, y) =>             (     x  &      y ));
                testBinOp<uint,   Vector64< uint  >>(name, (x, y) => Simd.And(x, y), (x, y) =>             (     x  &      y ));

                testThrowsTypeNotSupported<Vector128<Vector128<long> >>(name, (x, y) => Simd.And(x, y));

                testThrowsTypeNotSupported<Vector64< long  >>(name, (x, y) => Simd.And(x, y));
                testThrowsTypeNotSupported<Vector64< ulong >>(name, (x, y) => Simd.And(x, y));
                testThrowsTypeNotSupported<Vector64< double>>(name, (x, y) => Simd.And(x, y));
            }
            else
            {
                testThrowsPlatformNotSupported<Vector64< float >>(name, (x, y) => Simd.And(x, y));
                testThrowsPlatformNotSupported<Vector64< double>>(name, (x, y) => Simd.And(x, y));
                testThrowsPlatformNotSupported<Vector64< sbyte >>(name, (x, y) => Simd.And(x, y));
                testThrowsPlatformNotSupported<Vector64< byte  >>(name, (x, y) => Simd.And(x, y));
                testThrowsPlatformNotSupported<Vector64< short >>(name, (x, y) => Simd.And(x, y));
                testThrowsPlatformNotSupported<Vector64< ushort>>(name, (x, y) => Simd.And(x, y));
                testThrowsPlatformNotSupported<Vector64< int   >>(name, (x, y) => Simd.And(x, y));
                testThrowsPlatformNotSupported<Vector64< uint  >>(name, (x, y) => Simd.And(x, y));
                testThrowsPlatformNotSupported<Vector64< long  >>(name, (x, y) => Simd.And(x, y));
                testThrowsPlatformNotSupported<Vector64< ulong >>(name, (x, y) => Simd.And(x, y));
                testThrowsPlatformNotSupported<Vector128<float >>(name, (x, y) => Simd.And(x, y));
                testThrowsPlatformNotSupported<Vector128<double>>(name, (x, y) => Simd.And(x, y));
                testThrowsPlatformNotSupported<Vector128<sbyte >>(name, (x, y) => Simd.And(x, y));
                testThrowsPlatformNotSupported<Vector128<byte  >>(name, (x, y) => Simd.And(x, y));
                testThrowsPlatformNotSupported<Vector128<short >>(name, (x, y) => Simd.And(x, y));
                testThrowsPlatformNotSupported<Vector128<ushort>>(name, (x, y) => Simd.And(x, y));
                testThrowsPlatformNotSupported<Vector128<int   >>(name, (x, y) => Simd.And(x, y));
                testThrowsPlatformNotSupported<Vector128<uint  >>(name, (x, y) => Simd.And(x, y));
                testThrowsPlatformNotSupported<Vector128<long  >>(name, (x, y) => Simd.And(x, y));
                testThrowsPlatformNotSupported<Vector128<ulong >>(name, (x, y) => Simd.And(x, y));
            }

            Console.WriteLine($"Test{name} passed");
        }

        static void TestAndNot()
        {
            String name = "AndNot";

            if (Simd.IsSupported)
            {
                testBinOp<float,  Vector128<float >>(name, (x, y) => Simd.AndNot(x, y), (x, y) => bitsToFloat (bits(x) & ~bits(y)));
                testBinOp<double, Vector128<double>>(name, (x, y) => Simd.AndNot(x, y), (x, y) => bitsToDouble(bits(x) & ~bits(y)));
                testBinOp<sbyte,  Vector128<sbyte >>(name, (x, y) => Simd.AndNot(x, y), (x, y) => (sbyte)     (     x  & ~     y ));
                testBinOp<byte,   Vector128<byte  >>(name, (x, y) => Simd.AndNot(x, y), (x, y) => (byte)      (     x  & ~     y ));
                testBinOp<short,  Vector128<short >>(name, (x, y) => Simd.AndNot(x, y), (x, y) => (short)     (     x  & ~     y ));
                testBinOp<ushort, Vector128<ushort>>(name, (x, y) => Simd.AndNot(x, y), (x, y) => (ushort)    (     x  & ~     y ));
                testBinOp<int,    Vector128<int   >>(name, (x, y) => Simd.AndNot(x, y), (x, y) =>             (     x  & ~     y ));
                testBinOp<uint,   Vector128<uint  >>(name, (x, y) => Simd.AndNot(x, y), (x, y) =>             (     x  & ~     y ));
                testBinOp<long,   Vector128<long  >>(name, (x, y) => Simd.AndNot(x, y), (x, y) =>             (     x  & ~     y ));
                testBinOp<ulong,  Vector128<ulong >>(name, (x, y) => Simd.AndNot(x, y), (x, y) =>             (     x  & ~     y ));
                testBinOp<float,  Vector64< float >>(name, (x, y) => Simd.AndNot(x, y), (x, y) => bitsToFloat (bits(x) & ~bits(y)));
                testBinOp<sbyte,  Vector64< sbyte >>(name, (x, y) => Simd.AndNot(x, y), (x, y) => (sbyte)     (     x  & ~     y ));
                testBinOp<byte,   Vector64< byte  >>(name, (x, y) => Simd.AndNot(x, y), (x, y) => (byte)      (     x  & ~     y ));
                testBinOp<short,  Vector64< short >>(name, (x, y) => Simd.AndNot(x, y), (x, y) => (short)     (     x  & ~     y ));
                testBinOp<ushort, Vector64< ushort>>(name, (x, y) => Simd.AndNot(x, y), (x, y) => (ushort)    (     x  & ~     y ));
                testBinOp<int,    Vector64< int   >>(name, (x, y) => Simd.AndNot(x, y), (x, y) =>             (     x  & ~     y ));
                testBinOp<uint,   Vector64< uint  >>(name, (x, y) => Simd.AndNot(x, y), (x, y) =>             (     x  & ~     y ));

                testThrowsTypeNotSupported<Vector128<Vector128<long> >>(name, (x, y) => Simd.AndNot(x, y));

                testThrowsTypeNotSupported<Vector64< long >>(name, (x, y) => Simd.AndNot(x, y));
                testThrowsTypeNotSupported<Vector64< ulong>>(name, (x, y) => Simd.AndNot(x, y));
                testThrowsTypeNotSupported<Vector64<double>>(name, (x, y) => Simd.AndNot(x, y));
            }
            else
            {
                testThrowsPlatformNotSupported<Vector64< float >>(name, (x, y) => Simd.AndNot(x, y));
                testThrowsPlatformNotSupported<Vector64< double>>(name, (x, y) => Simd.AndNot(x, y));
                testThrowsPlatformNotSupported<Vector64< sbyte >>(name, (x, y) => Simd.AndNot(x, y));
                testThrowsPlatformNotSupported<Vector64< byte  >>(name, (x, y) => Simd.AndNot(x, y));
                testThrowsPlatformNotSupported<Vector64< short >>(name, (x, y) => Simd.AndNot(x, y));
                testThrowsPlatformNotSupported<Vector64< ushort>>(name, (x, y) => Simd.AndNot(x, y));
                testThrowsPlatformNotSupported<Vector64< int   >>(name, (x, y) => Simd.AndNot(x, y));
                testThrowsPlatformNotSupported<Vector64< uint  >>(name, (x, y) => Simd.AndNot(x, y));
                testThrowsPlatformNotSupported<Vector64< long  >>(name, (x, y) => Simd.AndNot(x, y));
                testThrowsPlatformNotSupported<Vector64< ulong >>(name, (x, y) => Simd.AndNot(x, y));
                testThrowsPlatformNotSupported<Vector128<float >>(name, (x, y) => Simd.AndNot(x, y));
                testThrowsPlatformNotSupported<Vector128<double>>(name, (x, y) => Simd.AndNot(x, y));
                testThrowsPlatformNotSupported<Vector128<sbyte >>(name, (x, y) => Simd.AndNot(x, y));
                testThrowsPlatformNotSupported<Vector128<byte  >>(name, (x, y) => Simd.AndNot(x, y));
                testThrowsPlatformNotSupported<Vector128<short >>(name, (x, y) => Simd.AndNot(x, y));
                testThrowsPlatformNotSupported<Vector128<ushort>>(name, (x, y) => Simd.AndNot(x, y));
                testThrowsPlatformNotSupported<Vector128<int   >>(name, (x, y) => Simd.AndNot(x, y));
                testThrowsPlatformNotSupported<Vector128<uint  >>(name, (x, y) => Simd.AndNot(x, y));
                testThrowsPlatformNotSupported<Vector128<long  >>(name, (x, y) => Simd.AndNot(x, y));
                testThrowsPlatformNotSupported<Vector128<ulong >>(name, (x, y) => Simd.AndNot(x, y));
            }

            Console.WriteLine($"Test{name} passed");
        }

        static void TestBitwiseSelect()
        {
            String name = "BitwiseSelect";

            if (Simd.IsSupported)
            {
                testBinOp<float,  Vector128<float >>(name, (x, y) => Simd.BitwiseSelect(Simd.Add(x,y), x, y), (x, y) => bitsToFloat (bits(y) ^ (bits(x + y) & (bits(x) ^ bits(y)))));
                testBinOp<double, Vector128<double>>(name, (x, y) => Simd.BitwiseSelect(Simd.Add(x,y), x, y), (x, y) => bitsToDouble(bits(y) ^ (bits(x + y) & (bits(x) ^ bits(y)))));
                testBinOp<sbyte,  Vector128<sbyte >>(name, (x, y) => Simd.BitwiseSelect(Simd.Add(x,y), x, y), (x, y) => (sbyte)     (    (y) ^ (    (x + y) & (    (x) ^     (y)))));
                testBinOp<byte,   Vector128<byte  >>(name, (x, y) => Simd.BitwiseSelect(Simd.Add(x,y), x, y), (x, y) => (byte)      (    (y) ^ (    (x + y) & (    (x) ^     (y)))));
                testBinOp<short,  Vector128<short >>(name, (x, y) => Simd.BitwiseSelect(Simd.Add(x,y), x, y), (x, y) => (short)     (    (y) ^ (    (x + y) & (    (x) ^     (y)))));
                testBinOp<ushort, Vector128<ushort>>(name, (x, y) => Simd.BitwiseSelect(Simd.Add(x,y), x, y), (x, y) => (ushort)    (    (y) ^ (    (x + y) & (    (x) ^     (y)))));
                testBinOp<int,    Vector128<int   >>(name, (x, y) => Simd.BitwiseSelect(Simd.Add(x,y), x, y), (x, y) =>             (    (y) ^ (    (x + y) & (    (x) ^     (y)))));
                testBinOp<uint,   Vector128<uint  >>(name, (x, y) => Simd.BitwiseSelect(Simd.Add(x,y), x, y), (x, y) =>             (    (y) ^ (    (x + y) & (    (x) ^     (y)))));
                testBinOp<long,   Vector128<long  >>(name, (x, y) => Simd.BitwiseSelect(Simd.Add(x,y), x, y), (x, y) =>             (    (y) ^ (    (x + y) & (    (x) ^     (y)))));
                testBinOp<ulong,  Vector128<ulong >>(name, (x, y) => Simd.BitwiseSelect(Simd.Add(x,y), x, y), (x, y) =>             (    (y) ^ (    (x + y) & (    (x) ^     (y)))));
                testBinOp<float,  Vector64< float >>(name, (x, y) => Simd.BitwiseSelect(Simd.Add(x,y), x, y), (x, y) => bitsToFloat (bits(y) ^ (bits(x + y) & (bits(x) ^ bits(y)))));
                testBinOp<sbyte,  Vector64< sbyte >>(name, (x, y) => Simd.BitwiseSelect(Simd.Add(x,y), x, y), (x, y) => (sbyte)     (    (y) ^ (    (x + y) & (    (x) ^     (y)))));
                testBinOp<byte,   Vector64< byte  >>(name, (x, y) => Simd.BitwiseSelect(Simd.Add(x,y), x, y), (x, y) => (byte)      (    (y) ^ (    (x + y) & (    (x) ^     (y)))));
                testBinOp<short,  Vector64< short >>(name, (x, y) => Simd.BitwiseSelect(Simd.Add(x,y), x, y), (x, y) => (short)     (    (y) ^ (    (x + y) & (    (x) ^     (y)))));
                testBinOp<ushort, Vector64< ushort>>(name, (x, y) => Simd.BitwiseSelect(Simd.Add(x,y), x, y), (x, y) => (ushort)    (    (y) ^ (    (x + y) & (    (x) ^     (y)))));
                testBinOp<int,    Vector64< int   >>(name, (x, y) => Simd.BitwiseSelect(Simd.Add(x,y), x, y), (x, y) =>             (    (y) ^ (    (x + y) & (    (x) ^     (y)))));
                testBinOp<uint,   Vector64< uint  >>(name, (x, y) => Simd.BitwiseSelect(Simd.Add(x,y), x, y), (x, y) =>             (    (y) ^ (    (x + y) & (    (x) ^     (y)))));

                testThrowsTypeNotSupported<Vector128<Vector128<long> >>(name, (x, y) => Simd.BitwiseSelect(x, x, y));

                testThrowsTypeNotSupported<Vector64< long >>(name, (x, y) => Simd.BitwiseSelect(x, x, y));
                testThrowsTypeNotSupported<Vector64< ulong>>(name, (x, y) => Simd.BitwiseSelect(x, x, y));
                testThrowsTypeNotSupported<Vector64<double>>(name, (x, y) => Simd.BitwiseSelect(x, x, y));
            }
            else
            {
                testThrowsPlatformNotSupported<Vector64< float >>(name, (x, y) => Simd.BitwiseSelect(x, x, y));
                testThrowsPlatformNotSupported<Vector64< double>>(name, (x, y) => Simd.BitwiseSelect(x, x, y));
                testThrowsPlatformNotSupported<Vector64< sbyte >>(name, (x, y) => Simd.BitwiseSelect(x, x, y));
                testThrowsPlatformNotSupported<Vector64< byte  >>(name, (x, y) => Simd.BitwiseSelect(x, x, y));
                testThrowsPlatformNotSupported<Vector64< short >>(name, (x, y) => Simd.BitwiseSelect(x, x, y));
                testThrowsPlatformNotSupported<Vector64< ushort>>(name, (x, y) => Simd.BitwiseSelect(x, x, y));
                testThrowsPlatformNotSupported<Vector64< int   >>(name, (x, y) => Simd.BitwiseSelect(x, x, y));
                testThrowsPlatformNotSupported<Vector64< uint  >>(name, (x, y) => Simd.BitwiseSelect(x, x, y));
                testThrowsPlatformNotSupported<Vector64< long  >>(name, (x, y) => Simd.BitwiseSelect(x, x, y));
                testThrowsPlatformNotSupported<Vector64< ulong >>(name, (x, y) => Simd.BitwiseSelect(x, x, y));
                testThrowsPlatformNotSupported<Vector128<float >>(name, (x, y) => Simd.BitwiseSelect(x, x, y));
                testThrowsPlatformNotSupported<Vector128<double>>(name, (x, y) => Simd.BitwiseSelect(x, x, y));
                testThrowsPlatformNotSupported<Vector128<sbyte >>(name, (x, y) => Simd.BitwiseSelect(x, x, y));
                testThrowsPlatformNotSupported<Vector128<byte  >>(name, (x, y) => Simd.BitwiseSelect(x, x, y));
                testThrowsPlatformNotSupported<Vector128<short >>(name, (x, y) => Simd.BitwiseSelect(x, x, y));
                testThrowsPlatformNotSupported<Vector128<ushort>>(name, (x, y) => Simd.BitwiseSelect(x, x, y));
                testThrowsPlatformNotSupported<Vector128<int   >>(name, (x, y) => Simd.BitwiseSelect(x, x, y));
                testThrowsPlatformNotSupported<Vector128<uint  >>(name, (x, y) => Simd.BitwiseSelect(x, x, y));
                testThrowsPlatformNotSupported<Vector128<long  >>(name, (x, y) => Simd.BitwiseSelect(x, x, y));
                testThrowsPlatformNotSupported<Vector128<ulong >>(name, (x, y) => Simd.BitwiseSelect(x, x, y));
            }

            Console.WriteLine($"Test{name} passed");
        }

        static void TestCompareEqual()
        {
            String name = "CompareEqual";

            if (Simd.IsSupported)
            {
                testBinOp<float,  Vector128<float >>(name, (x, y) => Simd.CompareEqual(x, y), (x, y) => boolTo<float >(x == y));
                testBinOp<double, Vector128<double>>(name, (x, y) => Simd.CompareEqual(x, y), (x, y) => boolTo<double>(x == y));
                testBinOp<sbyte,  Vector128<sbyte >>(name, (x, y) => Simd.CompareEqual(x, y), (x, y) => boolTo<sbyte >(x == y));
                testBinOp<byte,   Vector128<byte  >>(name, (x, y) => Simd.CompareEqual(x, y), (x, y) => boolTo<byte  >(x == y));
                testBinOp<short,  Vector128<short >>(name, (x, y) => Simd.CompareEqual(x, y), (x, y) => boolTo<short >(x == y));
                testBinOp<ushort, Vector128<ushort>>(name, (x, y) => Simd.CompareEqual(x, y), (x, y) => boolTo<ushort>(x == y));
                testBinOp<int,    Vector128<int   >>(name, (x, y) => Simd.CompareEqual(x, y), (x, y) => boolTo<int   >(x == y));
                testBinOp<uint,   Vector128<uint  >>(name, (x, y) => Simd.CompareEqual(x, y), (x, y) => boolTo<uint  >(x == y));
                testBinOp<long,   Vector128<long  >>(name, (x, y) => Simd.CompareEqual(x, y), (x, y) => boolTo<long  >(x == y));
                testBinOp<ulong,  Vector128<ulong >>(name, (x, y) => Simd.CompareEqual(x, y), (x, y) => boolTo<ulong >(x == y));
                testBinOp<float,  Vector64< float >>(name, (x, y) => Simd.CompareEqual(x, y), (x, y) => boolTo<float >(x == y));
                testBinOp<sbyte,  Vector64< sbyte >>(name, (x, y) => Simd.CompareEqual(x, y), (x, y) => boolTo<sbyte >(x == y));
                testBinOp<byte,   Vector64< byte  >>(name, (x, y) => Simd.CompareEqual(x, y), (x, y) => boolTo<byte  >(x == y));
                testBinOp<short,  Vector64< short >>(name, (x, y) => Simd.CompareEqual(x, y), (x, y) => boolTo<short >(x == y));
                testBinOp<ushort, Vector64< ushort>>(name, (x, y) => Simd.CompareEqual(x, y), (x, y) => boolTo<ushort>(x == y));
                testBinOp<int,    Vector64< int   >>(name, (x, y) => Simd.CompareEqual(x, y), (x, y) => boolTo<int   >(x == y));
                testBinOp<uint,   Vector64< uint  >>(name, (x, y) => Simd.CompareEqual(x, y), (x, y) => boolTo<uint  >(x == y));

                testThrowsTypeNotSupported<Vector128<Vector128<long> >>(name, (x, y) => Simd.CompareEqual(x, y));

                testThrowsTypeNotSupported<Vector64< long >>(name, (x, y) => Simd.CompareEqual(x, y));
                testThrowsTypeNotSupported<Vector64< ulong>>(name, (x, y) => Simd.CompareEqual(x, y));
                testThrowsTypeNotSupported<Vector64<double>>(name, (x, y) => Simd.CompareEqual(x, y));
            }
            else
            {
                testThrowsPlatformNotSupported<Vector64< float >>(name, (x, y) => Simd.CompareEqual(x, y));
                testThrowsPlatformNotSupported<Vector64< double>>(name, (x, y) => Simd.CompareEqual(x, y));
                testThrowsPlatformNotSupported<Vector64< sbyte >>(name, (x, y) => Simd.CompareEqual(x, y));
                testThrowsPlatformNotSupported<Vector64< byte  >>(name, (x, y) => Simd.CompareEqual(x, y));
                testThrowsPlatformNotSupported<Vector64< short >>(name, (x, y) => Simd.CompareEqual(x, y));
                testThrowsPlatformNotSupported<Vector64< ushort>>(name, (x, y) => Simd.CompareEqual(x, y));
                testThrowsPlatformNotSupported<Vector64< int   >>(name, (x, y) => Simd.CompareEqual(x, y));
                testThrowsPlatformNotSupported<Vector64< uint  >>(name, (x, y) => Simd.CompareEqual(x, y));
                testThrowsPlatformNotSupported<Vector64< long  >>(name, (x, y) => Simd.CompareEqual(x, y));
                testThrowsPlatformNotSupported<Vector64< ulong >>(name, (x, y) => Simd.CompareEqual(x, y));
                testThrowsPlatformNotSupported<Vector128<float >>(name, (x, y) => Simd.CompareEqual(x, y));
                testThrowsPlatformNotSupported<Vector128<double>>(name, (x, y) => Simd.CompareEqual(x, y));
                testThrowsPlatformNotSupported<Vector128<sbyte >>(name, (x, y) => Simd.CompareEqual(x, y));
                testThrowsPlatformNotSupported<Vector128<byte  >>(name, (x, y) => Simd.CompareEqual(x, y));
                testThrowsPlatformNotSupported<Vector128<short >>(name, (x, y) => Simd.CompareEqual(x, y));
                testThrowsPlatformNotSupported<Vector128<ushort>>(name, (x, y) => Simd.CompareEqual(x, y));
                testThrowsPlatformNotSupported<Vector128<int   >>(name, (x, y) => Simd.CompareEqual(x, y));
                testThrowsPlatformNotSupported<Vector128<uint  >>(name, (x, y) => Simd.CompareEqual(x, y));
                testThrowsPlatformNotSupported<Vector128<long  >>(name, (x, y) => Simd.CompareEqual(x, y));
                testThrowsPlatformNotSupported<Vector128<ulong >>(name, (x, y) => Simd.CompareEqual(x, y));
            }

            Console.WriteLine($"Test{name} passed");
        }

        static void TestCompareEqualZero()
        {
            String name = "CompareEqualZero";

            if (Simd.IsSupported)
            {
                testBinOp<float,  Vector128<float >>(name, (x, y) => Simd.CompareEqualZero(x), (x, y) => boolTo<float >(x == 0));
                testBinOp<double, Vector128<double>>(name, (x, y) => Simd.CompareEqualZero(x), (x, y) => boolTo<double>(x == 0));
                testBinOp<sbyte,  Vector128<sbyte >>(name, (x, y) => Simd.CompareEqualZero(x), (x, y) => boolTo<sbyte >(x == 0));
                testBinOp<byte,   Vector128<byte  >>(name, (x, y) => Simd.CompareEqualZero(x), (x, y) => boolTo<byte  >(x == 0));
                testBinOp<short,  Vector128<short >>(name, (x, y) => Simd.CompareEqualZero(x), (x, y) => boolTo<short >(x == 0));
                testBinOp<ushort, Vector128<ushort>>(name, (x, y) => Simd.CompareEqualZero(x), (x, y) => boolTo<ushort>(x == 0));
                testBinOp<int,    Vector128<int   >>(name, (x, y) => Simd.CompareEqualZero(x), (x, y) => boolTo<int   >(x == 0));
                testBinOp<uint,   Vector128<uint  >>(name, (x, y) => Simd.CompareEqualZero(x), (x, y) => boolTo<uint  >(x == 0));
                testBinOp<long,   Vector128<long  >>(name, (x, y) => Simd.CompareEqualZero(x), (x, y) => boolTo<long  >(x == 0));
                testBinOp<ulong,  Vector128<ulong >>(name, (x, y) => Simd.CompareEqualZero(x), (x, y) => boolTo<ulong >(x == 0));
                testBinOp<float,  Vector64< float >>(name, (x, y) => Simd.CompareEqualZero(x), (x, y) => boolTo<float >(x == 0));
                testBinOp<sbyte,  Vector64< sbyte >>(name, (x, y) => Simd.CompareEqualZero(x), (x, y) => boolTo<sbyte >(x == 0));
                testBinOp<byte,   Vector64< byte  >>(name, (x, y) => Simd.CompareEqualZero(x), (x, y) => boolTo<byte  >(x == 0));
                testBinOp<short,  Vector64< short >>(name, (x, y) => Simd.CompareEqualZero(x), (x, y) => boolTo<short >(x == 0));
                testBinOp<ushort, Vector64< ushort>>(name, (x, y) => Simd.CompareEqualZero(x), (x, y) => boolTo<ushort>(x == 0));
                testBinOp<int,    Vector64< int   >>(name, (x, y) => Simd.CompareEqualZero(x), (x, y) => boolTo<int   >(x == 0));
                testBinOp<uint,   Vector64< uint  >>(name, (x, y) => Simd.CompareEqualZero(x), (x, y) => boolTo<uint  >(x == 0));

                testThrowsTypeNotSupported<Vector128<Vector128<long> >>(name, (x, y) => Simd.CompareEqualZero(x));

                testThrowsTypeNotSupported<Vector64< long >>(name, (x, y) => Simd.CompareEqualZero(x));
                testThrowsTypeNotSupported<Vector64< ulong>>(name, (x, y) => Simd.CompareEqualZero(x));
                testThrowsTypeNotSupported<Vector64<double>>(name, (x, y) => Simd.CompareEqualZero(x));
            }
            else
            {
                testThrowsPlatformNotSupported<Vector64< float >>(name, (x, y) => Simd.CompareEqualZero(x));
                testThrowsPlatformNotSupported<Vector64< double>>(name, (x, y) => Simd.CompareEqualZero(x));
                testThrowsPlatformNotSupported<Vector64< sbyte >>(name, (x, y) => Simd.CompareEqualZero(x));
                testThrowsPlatformNotSupported<Vector64< byte  >>(name, (x, y) => Simd.CompareEqualZero(x));
                testThrowsPlatformNotSupported<Vector64< short >>(name, (x, y) => Simd.CompareEqualZero(x));
                testThrowsPlatformNotSupported<Vector64< ushort>>(name, (x, y) => Simd.CompareEqualZero(x));
                testThrowsPlatformNotSupported<Vector64< int   >>(name, (x, y) => Simd.CompareEqualZero(x));
                testThrowsPlatformNotSupported<Vector64< uint  >>(name, (x, y) => Simd.CompareEqualZero(x));
                testThrowsPlatformNotSupported<Vector64< long  >>(name, (x, y) => Simd.CompareEqualZero(x));
                testThrowsPlatformNotSupported<Vector64< ulong >>(name, (x, y) => Simd.CompareEqualZero(x));
                testThrowsPlatformNotSupported<Vector128<float >>(name, (x, y) => Simd.CompareEqualZero(x));
                testThrowsPlatformNotSupported<Vector128<double>>(name, (x, y) => Simd.CompareEqualZero(x));
                testThrowsPlatformNotSupported<Vector128<sbyte >>(name, (x, y) => Simd.CompareEqualZero(x));
                testThrowsPlatformNotSupported<Vector128<byte  >>(name, (x, y) => Simd.CompareEqualZero(x));
                testThrowsPlatformNotSupported<Vector128<short >>(name, (x, y) => Simd.CompareEqualZero(x));
                testThrowsPlatformNotSupported<Vector128<ushort>>(name, (x, y) => Simd.CompareEqualZero(x));
                testThrowsPlatformNotSupported<Vector128<int   >>(name, (x, y) => Simd.CompareEqualZero(x));
                testThrowsPlatformNotSupported<Vector128<uint  >>(name, (x, y) => Simd.CompareEqualZero(x));
                testThrowsPlatformNotSupported<Vector128<long  >>(name, (x, y) => Simd.CompareEqualZero(x));
                testThrowsPlatformNotSupported<Vector128<ulong >>(name, (x, y) => Simd.CompareEqualZero(x));
            }

            Console.WriteLine($"Test{name} passed");
        }

        static void TestCompareGreaterThan()
        {
            String name = "CompareGreaterThan";

            if (Simd.IsSupported)
            {
                testBinOp<float,  Vector128<float >>(name, (x, y) => Simd.CompareGreaterThan(x, y), (x, y) => boolTo<float >(x > y));
                testBinOp<double, Vector128<double>>(name, (x, y) => Simd.CompareGreaterThan(x, y), (x, y) => boolTo<double>(x > y));
                testBinOp<sbyte,  Vector128<sbyte >>(name, (x, y) => Simd.CompareGreaterThan(x, y), (x, y) => boolTo<sbyte >(x > y));
                testBinOp<byte,   Vector128<byte  >>(name, (x, y) => Simd.CompareGreaterThan(x, y), (x, y) => boolTo<byte  >(x > y));
                testBinOp<short,  Vector128<short >>(name, (x, y) => Simd.CompareGreaterThan(x, y), (x, y) => boolTo<short >(x > y));
                testBinOp<ushort, Vector128<ushort>>(name, (x, y) => Simd.CompareGreaterThan(x, y), (x, y) => boolTo<ushort>(x > y));
                testBinOp<int,    Vector128<int   >>(name, (x, y) => Simd.CompareGreaterThan(x, y), (x, y) => boolTo<int   >(x > y));
                testBinOp<uint,   Vector128<uint  >>(name, (x, y) => Simd.CompareGreaterThan(x, y), (x, y) => boolTo<uint  >(x > y));
                testBinOp<long,   Vector128<long  >>(name, (x, y) => Simd.CompareGreaterThan(x, y), (x, y) => boolTo<long  >(x > y));
                testBinOp<ulong,  Vector128<ulong >>(name, (x, y) => Simd.CompareGreaterThan(x, y), (x, y) => boolTo<ulong >(x > y));
                testBinOp<float,  Vector64< float >>(name, (x, y) => Simd.CompareGreaterThan(x, y), (x, y) => boolTo<float >(x > y));
                testBinOp<sbyte,  Vector64< sbyte >>(name, (x, y) => Simd.CompareGreaterThan(x, y), (x, y) => boolTo<sbyte >(x > y));
                testBinOp<byte,   Vector64< byte  >>(name, (x, y) => Simd.CompareGreaterThan(x, y), (x, y) => boolTo<byte  >(x > y));
                testBinOp<short,  Vector64< short >>(name, (x, y) => Simd.CompareGreaterThan(x, y), (x, y) => boolTo<short >(x > y));
                testBinOp<ushort, Vector64< ushort>>(name, (x, y) => Simd.CompareGreaterThan(x, y), (x, y) => boolTo<ushort>(x > y));
                testBinOp<int,    Vector64< int   >>(name, (x, y) => Simd.CompareGreaterThan(x, y), (x, y) => boolTo<int   >(x > y));
                testBinOp<uint,   Vector64< uint  >>(name, (x, y) => Simd.CompareGreaterThan(x, y), (x, y) => boolTo<uint  >(x > y));

                testThrowsTypeNotSupported<Vector128<Vector128<long> >>(name, (x, y) => Simd.CompareGreaterThan(x, y));

                testThrowsTypeNotSupported<Vector64< long >>(name, (x, y) => Simd.CompareGreaterThan(x, y));
                testThrowsTypeNotSupported<Vector64< ulong>>(name, (x, y) => Simd.CompareGreaterThan(x, y));
                testThrowsTypeNotSupported<Vector64<double>>(name, (x, y) => Simd.CompareGreaterThan(x, y));
            }
            else
            {
                testThrowsPlatformNotSupported<Vector64< float >>(name, (x, y) => Simd.CompareGreaterThan(x, y));
                testThrowsPlatformNotSupported<Vector64< double>>(name, (x, y) => Simd.CompareGreaterThan(x, y));
                testThrowsPlatformNotSupported<Vector64< sbyte >>(name, (x, y) => Simd.CompareGreaterThan(x, y));
                testThrowsPlatformNotSupported<Vector64< byte  >>(name, (x, y) => Simd.CompareGreaterThan(x, y));
                testThrowsPlatformNotSupported<Vector64< short >>(name, (x, y) => Simd.CompareGreaterThan(x, y));
                testThrowsPlatformNotSupported<Vector64< ushort>>(name, (x, y) => Simd.CompareGreaterThan(x, y));
                testThrowsPlatformNotSupported<Vector64< int   >>(name, (x, y) => Simd.CompareGreaterThan(x, y));
                testThrowsPlatformNotSupported<Vector64< uint  >>(name, (x, y) => Simd.CompareGreaterThan(x, y));
                testThrowsPlatformNotSupported<Vector64< long  >>(name, (x, y) => Simd.CompareGreaterThan(x, y));
                testThrowsPlatformNotSupported<Vector64< ulong >>(name, (x, y) => Simd.CompareGreaterThan(x, y));
                testThrowsPlatformNotSupported<Vector128<float >>(name, (x, y) => Simd.CompareGreaterThan(x, y));
                testThrowsPlatformNotSupported<Vector128<double>>(name, (x, y) => Simd.CompareGreaterThan(x, y));
                testThrowsPlatformNotSupported<Vector128<sbyte >>(name, (x, y) => Simd.CompareGreaterThan(x, y));
                testThrowsPlatformNotSupported<Vector128<byte  >>(name, (x, y) => Simd.CompareGreaterThan(x, y));
                testThrowsPlatformNotSupported<Vector128<short >>(name, (x, y) => Simd.CompareGreaterThan(x, y));
                testThrowsPlatformNotSupported<Vector128<ushort>>(name, (x, y) => Simd.CompareGreaterThan(x, y));
                testThrowsPlatformNotSupported<Vector128<int   >>(name, (x, y) => Simd.CompareGreaterThan(x, y));
                testThrowsPlatformNotSupported<Vector128<uint  >>(name, (x, y) => Simd.CompareGreaterThan(x, y));
                testThrowsPlatformNotSupported<Vector128<long  >>(name, (x, y) => Simd.CompareGreaterThan(x, y));
                testThrowsPlatformNotSupported<Vector128<ulong >>(name, (x, y) => Simd.CompareGreaterThan(x, y));
            }

            Console.WriteLine($"Test{name} passed");
        }

        static void TestCompareGreaterThanZero()
        {
            String name = "CompareGreaterThanZero";

            if (Simd.IsSupported)
            {
                testBinOp<float,  Vector128<float >>(name, (x, y) => Simd.CompareGreaterThanZero(x), (x, y) => boolTo<float >(x > 0));
                testBinOp<double, Vector128<double>>(name, (x, y) => Simd.CompareGreaterThanZero(x), (x, y) => boolTo<double>(x > 0));
                testBinOp<sbyte,  Vector128<sbyte >>(name, (x, y) => Simd.CompareGreaterThanZero(x), (x, y) => boolTo<sbyte >(x > 0));
                testBinOp<byte,   Vector128<byte  >>(name, (x, y) => Simd.CompareGreaterThanZero(x), (x, y) => boolTo<byte  >(x > 0));
                testBinOp<short,  Vector128<short >>(name, (x, y) => Simd.CompareGreaterThanZero(x), (x, y) => boolTo<short >(x > 0));
                testBinOp<ushort, Vector128<ushort>>(name, (x, y) => Simd.CompareGreaterThanZero(x), (x, y) => boolTo<ushort>(x > 0));
                testBinOp<int,    Vector128<int   >>(name, (x, y) => Simd.CompareGreaterThanZero(x), (x, y) => boolTo<int   >(x > 0));
                testBinOp<uint,   Vector128<uint  >>(name, (x, y) => Simd.CompareGreaterThanZero(x), (x, y) => boolTo<uint  >(x > 0));
                testBinOp<long,   Vector128<long  >>(name, (x, y) => Simd.CompareGreaterThanZero(x), (x, y) => boolTo<long  >(x > 0));
                testBinOp<ulong,  Vector128<ulong >>(name, (x, y) => Simd.CompareGreaterThanZero(x), (x, y) => boolTo<ulong >(x > 0));
                testBinOp<float,  Vector64< float >>(name, (x, y) => Simd.CompareGreaterThanZero(x), (x, y) => boolTo<float >(x > 0));
                testBinOp<sbyte,  Vector64< sbyte >>(name, (x, y) => Simd.CompareGreaterThanZero(x), (x, y) => boolTo<sbyte >(x > 0));
                testBinOp<byte,   Vector64< byte  >>(name, (x, y) => Simd.CompareGreaterThanZero(x), (x, y) => boolTo<byte  >(x > 0));
                testBinOp<short,  Vector64< short >>(name, (x, y) => Simd.CompareGreaterThanZero(x), (x, y) => boolTo<short >(x > 0));
                testBinOp<ushort, Vector64< ushort>>(name, (x, y) => Simd.CompareGreaterThanZero(x), (x, y) => boolTo<ushort>(x > 0));
                testBinOp<int,    Vector64< int   >>(name, (x, y) => Simd.CompareGreaterThanZero(x), (x, y) => boolTo<int   >(x > 0));
                testBinOp<uint,   Vector64< uint  >>(name, (x, y) => Simd.CompareGreaterThanZero(x), (x, y) => boolTo<uint  >(x > 0));

                testThrowsTypeNotSupported<Vector128<Vector128<long> >>(name, (x, y) => Simd.CompareGreaterThanZero(x));

                testThrowsTypeNotSupported<Vector64< long >>(name, (x, y) => Simd.CompareGreaterThanZero(x));
                testThrowsTypeNotSupported<Vector64< ulong>>(name, (x, y) => Simd.CompareGreaterThanZero(x));
                testThrowsTypeNotSupported<Vector64<double>>(name, (x, y) => Simd.CompareGreaterThanZero(x));
            }
            else
            {
                testThrowsPlatformNotSupported<Vector64< float >>(name, (x, y) => Simd.CompareGreaterThanZero(x));
                testThrowsPlatformNotSupported<Vector64< double>>(name, (x, y) => Simd.CompareGreaterThanZero(x));
                testThrowsPlatformNotSupported<Vector64< sbyte >>(name, (x, y) => Simd.CompareGreaterThanZero(x));
                testThrowsPlatformNotSupported<Vector64< byte  >>(name, (x, y) => Simd.CompareGreaterThanZero(x));
                testThrowsPlatformNotSupported<Vector64< short >>(name, (x, y) => Simd.CompareGreaterThanZero(x));
                testThrowsPlatformNotSupported<Vector64< ushort>>(name, (x, y) => Simd.CompareGreaterThanZero(x));
                testThrowsPlatformNotSupported<Vector64< int   >>(name, (x, y) => Simd.CompareGreaterThanZero(x));
                testThrowsPlatformNotSupported<Vector64< uint  >>(name, (x, y) => Simd.CompareGreaterThanZero(x));
                testThrowsPlatformNotSupported<Vector64< long  >>(name, (x, y) => Simd.CompareGreaterThanZero(x));
                testThrowsPlatformNotSupported<Vector64< ulong >>(name, (x, y) => Simd.CompareGreaterThanZero(x));
                testThrowsPlatformNotSupported<Vector128<float >>(name, (x, y) => Simd.CompareGreaterThanZero(x));
                testThrowsPlatformNotSupported<Vector128<double>>(name, (x, y) => Simd.CompareGreaterThanZero(x));
                testThrowsPlatformNotSupported<Vector128<sbyte >>(name, (x, y) => Simd.CompareGreaterThanZero(x));
                testThrowsPlatformNotSupported<Vector128<byte  >>(name, (x, y) => Simd.CompareGreaterThanZero(x));
                testThrowsPlatformNotSupported<Vector128<short >>(name, (x, y) => Simd.CompareGreaterThanZero(x));
                testThrowsPlatformNotSupported<Vector128<ushort>>(name, (x, y) => Simd.CompareGreaterThanZero(x));
                testThrowsPlatformNotSupported<Vector128<int   >>(name, (x, y) => Simd.CompareGreaterThanZero(x));
                testThrowsPlatformNotSupported<Vector128<uint  >>(name, (x, y) => Simd.CompareGreaterThanZero(x));
                testThrowsPlatformNotSupported<Vector128<long  >>(name, (x, y) => Simd.CompareGreaterThanZero(x));
                testThrowsPlatformNotSupported<Vector128<ulong >>(name, (x, y) => Simd.CompareGreaterThanZero(x));
            }

            Console.WriteLine($"Test{name} passed");
        }

        static void TestCompareGreaterThanOrEqual()
        {
            String name = "CompareGreaterThanOrEqual";

            if (Simd.IsSupported)
            {
                testBinOp<float,  Vector128<float >>(name, (x, y) => Simd.CompareGreaterThanOrEqual(x, y), (x, y) => boolTo<float >(x >= y));
                testBinOp<double, Vector128<double>>(name, (x, y) => Simd.CompareGreaterThanOrEqual(x, y), (x, y) => boolTo<double>(x >= y));
                testBinOp<sbyte,  Vector128<sbyte >>(name, (x, y) => Simd.CompareGreaterThanOrEqual(x, y), (x, y) => boolTo<sbyte >(x >= y));
                testBinOp<byte,   Vector128<byte  >>(name, (x, y) => Simd.CompareGreaterThanOrEqual(x, y), (x, y) => boolTo<byte  >(x >= y));
                testBinOp<short,  Vector128<short >>(name, (x, y) => Simd.CompareGreaterThanOrEqual(x, y), (x, y) => boolTo<short >(x >= y));
                testBinOp<ushort, Vector128<ushort>>(name, (x, y) => Simd.CompareGreaterThanOrEqual(x, y), (x, y) => boolTo<ushort>(x >= y));
                testBinOp<int,    Vector128<int   >>(name, (x, y) => Simd.CompareGreaterThanOrEqual(x, y), (x, y) => boolTo<int   >(x >= y));
                testBinOp<uint,   Vector128<uint  >>(name, (x, y) => Simd.CompareGreaterThanOrEqual(x, y), (x, y) => boolTo<uint  >(x >= y));
                testBinOp<long,   Vector128<long  >>(name, (x, y) => Simd.CompareGreaterThanOrEqual(x, y), (x, y) => boolTo<long  >(x >= y));
                testBinOp<ulong,  Vector128<ulong >>(name, (x, y) => Simd.CompareGreaterThanOrEqual(x, y), (x, y) => boolTo<ulong >(x >= y));
                testBinOp<float,  Vector64< float >>(name, (x, y) => Simd.CompareGreaterThanOrEqual(x, y), (x, y) => boolTo<float >(x >= y));
                testBinOp<sbyte,  Vector64< sbyte >>(name, (x, y) => Simd.CompareGreaterThanOrEqual(x, y), (x, y) => boolTo<sbyte >(x >= y));
                testBinOp<byte,   Vector64< byte  >>(name, (x, y) => Simd.CompareGreaterThanOrEqual(x, y), (x, y) => boolTo<byte  >(x >= y));
                testBinOp<short,  Vector64< short >>(name, (x, y) => Simd.CompareGreaterThanOrEqual(x, y), (x, y) => boolTo<short >(x >= y));
                testBinOp<ushort, Vector64< ushort>>(name, (x, y) => Simd.CompareGreaterThanOrEqual(x, y), (x, y) => boolTo<ushort>(x >= y));
                testBinOp<int,    Vector64< int   >>(name, (x, y) => Simd.CompareGreaterThanOrEqual(x, y), (x, y) => boolTo<int   >(x >= y));
                testBinOp<uint,   Vector64< uint  >>(name, (x, y) => Simd.CompareGreaterThanOrEqual(x, y), (x, y) => boolTo<uint  >(x >= y));

                testThrowsTypeNotSupported<Vector128<Vector128<long> >>(name, (x, y) => Simd.CompareGreaterThanOrEqual(x, y));

                testThrowsTypeNotSupported<Vector64< long >>(name, (x, y) => Simd.CompareGreaterThanOrEqual(x, y));
                testThrowsTypeNotSupported<Vector64< ulong>>(name, (x, y) => Simd.CompareGreaterThanOrEqual(x, y));
                testThrowsTypeNotSupported<Vector64<double>>(name, (x, y) => Simd.CompareGreaterThanOrEqual(x, y));
            }
            else
            {
                testThrowsPlatformNotSupported<Vector64< float >>(name, (x, y) => Simd.CompareGreaterThanOrEqual(x, y));
                testThrowsPlatformNotSupported<Vector64< double>>(name, (x, y) => Simd.CompareGreaterThanOrEqual(x, y));
                testThrowsPlatformNotSupported<Vector64< sbyte >>(name, (x, y) => Simd.CompareGreaterThanOrEqual(x, y));
                testThrowsPlatformNotSupported<Vector64< byte  >>(name, (x, y) => Simd.CompareGreaterThanOrEqual(x, y));
                testThrowsPlatformNotSupported<Vector64< short >>(name, (x, y) => Simd.CompareGreaterThanOrEqual(x, y));
                testThrowsPlatformNotSupported<Vector64< ushort>>(name, (x, y) => Simd.CompareGreaterThanOrEqual(x, y));
                testThrowsPlatformNotSupported<Vector64< int   >>(name, (x, y) => Simd.CompareGreaterThanOrEqual(x, y));
                testThrowsPlatformNotSupported<Vector64< uint  >>(name, (x, y) => Simd.CompareGreaterThanOrEqual(x, y));
                testThrowsPlatformNotSupported<Vector64< long  >>(name, (x, y) => Simd.CompareGreaterThanOrEqual(x, y));
                testThrowsPlatformNotSupported<Vector64< ulong >>(name, (x, y) => Simd.CompareGreaterThanOrEqual(x, y));
                testThrowsPlatformNotSupported<Vector128<float >>(name, (x, y) => Simd.CompareGreaterThanOrEqual(x, y));
                testThrowsPlatformNotSupported<Vector128<double>>(name, (x, y) => Simd.CompareGreaterThanOrEqual(x, y));
                testThrowsPlatformNotSupported<Vector128<sbyte >>(name, (x, y) => Simd.CompareGreaterThanOrEqual(x, y));
                testThrowsPlatformNotSupported<Vector128<byte  >>(name, (x, y) => Simd.CompareGreaterThanOrEqual(x, y));
                testThrowsPlatformNotSupported<Vector128<short >>(name, (x, y) => Simd.CompareGreaterThanOrEqual(x, y));
                testThrowsPlatformNotSupported<Vector128<ushort>>(name, (x, y) => Simd.CompareGreaterThanOrEqual(x, y));
                testThrowsPlatformNotSupported<Vector128<int   >>(name, (x, y) => Simd.CompareGreaterThanOrEqual(x, y));
                testThrowsPlatformNotSupported<Vector128<uint  >>(name, (x, y) => Simd.CompareGreaterThanOrEqual(x, y));
                testThrowsPlatformNotSupported<Vector128<long  >>(name, (x, y) => Simd.CompareGreaterThanOrEqual(x, y));
                testThrowsPlatformNotSupported<Vector128<ulong >>(name, (x, y) => Simd.CompareGreaterThanOrEqual(x, y));
            }

            Console.WriteLine($"Test{name} passed");
        }

        static void TestCompareGreaterThanOrEqualZero()
        {
            String name = "CompareGreaterThanOrEqualZero";

            if (Simd.IsSupported)
            {
                testBinOp<float,  Vector128<float >>(name, (x, y) => Simd.CompareGreaterThanOrEqualZero(x), (x, y) => boolTo<float >(x >= 0));
                testBinOp<double, Vector128<double>>(name, (x, y) => Simd.CompareGreaterThanOrEqualZero(x), (x, y) => boolTo<double>(x >= 0));
                testBinOp<sbyte,  Vector128<sbyte >>(name, (x, y) => Simd.CompareGreaterThanOrEqualZero(x), (x, y) => boolTo<sbyte >(x >= 0));
                testBinOp<byte,   Vector128<byte  >>(name, (x, y) => Simd.CompareGreaterThanOrEqualZero(x), (x, y) => boolTo<byte  >(x >= 0));
                testBinOp<short,  Vector128<short >>(name, (x, y) => Simd.CompareGreaterThanOrEqualZero(x), (x, y) => boolTo<short >(x >= 0));
                testBinOp<ushort, Vector128<ushort>>(name, (x, y) => Simd.CompareGreaterThanOrEqualZero(x), (x, y) => boolTo<ushort>(x >= 0));
                testBinOp<int,    Vector128<int   >>(name, (x, y) => Simd.CompareGreaterThanOrEqualZero(x), (x, y) => boolTo<int   >(x >= 0));
                testBinOp<uint,   Vector128<uint  >>(name, (x, y) => Simd.CompareGreaterThanOrEqualZero(x), (x, y) => boolTo<uint  >(x >= 0));
                testBinOp<long,   Vector128<long  >>(name, (x, y) => Simd.CompareGreaterThanOrEqualZero(x), (x, y) => boolTo<long  >(x >= 0));
                testBinOp<ulong,  Vector128<ulong >>(name, (x, y) => Simd.CompareGreaterThanOrEqualZero(x), (x, y) => boolTo<ulong >(x >= 0));
                testBinOp<float,  Vector64< float >>(name, (x, y) => Simd.CompareGreaterThanOrEqualZero(x), (x, y) => boolTo<float >(x >= 0));
                testBinOp<sbyte,  Vector64< sbyte >>(name, (x, y) => Simd.CompareGreaterThanOrEqualZero(x), (x, y) => boolTo<sbyte >(x >= 0));
                testBinOp<byte,   Vector64< byte  >>(name, (x, y) => Simd.CompareGreaterThanOrEqualZero(x), (x, y) => boolTo<byte  >(x >= 0));
                testBinOp<short,  Vector64< short >>(name, (x, y) => Simd.CompareGreaterThanOrEqualZero(x), (x, y) => boolTo<short >(x >= 0));
                testBinOp<ushort, Vector64< ushort>>(name, (x, y) => Simd.CompareGreaterThanOrEqualZero(x), (x, y) => boolTo<ushort>(x >= 0));
                testBinOp<int,    Vector64< int   >>(name, (x, y) => Simd.CompareGreaterThanOrEqualZero(x), (x, y) => boolTo<int   >(x >= 0));
                testBinOp<uint,   Vector64< uint  >>(name, (x, y) => Simd.CompareGreaterThanOrEqualZero(x), (x, y) => boolTo<uint  >(x >= 0));

                testThrowsTypeNotSupported<Vector128<Vector128<long> >>(name, (x, y) => Simd.CompareGreaterThanOrEqualZero(x));

                testThrowsTypeNotSupported<Vector64< long >>(name, (x, y) => Simd.CompareGreaterThanOrEqualZero(x));
                testThrowsTypeNotSupported<Vector64< ulong>>(name, (x, y) => Simd.CompareGreaterThanOrEqualZero(x));
                testThrowsTypeNotSupported<Vector64<double>>(name, (x, y) => Simd.CompareGreaterThanOrEqualZero(x));
            }
            else
            {
                testThrowsPlatformNotSupported<Vector64< float >>(name, (x, y) => Simd.CompareGreaterThanOrEqualZero(x));
                testThrowsPlatformNotSupported<Vector64< double>>(name, (x, y) => Simd.CompareGreaterThanOrEqualZero(x));
                testThrowsPlatformNotSupported<Vector64< sbyte >>(name, (x, y) => Simd.CompareGreaterThanOrEqualZero(x));
                testThrowsPlatformNotSupported<Vector64< byte  >>(name, (x, y) => Simd.CompareGreaterThanOrEqualZero(x));
                testThrowsPlatformNotSupported<Vector64< short >>(name, (x, y) => Simd.CompareGreaterThanOrEqualZero(x));
                testThrowsPlatformNotSupported<Vector64< ushort>>(name, (x, y) => Simd.CompareGreaterThanOrEqualZero(x));
                testThrowsPlatformNotSupported<Vector64< int   >>(name, (x, y) => Simd.CompareGreaterThanOrEqualZero(x));
                testThrowsPlatformNotSupported<Vector64< uint  >>(name, (x, y) => Simd.CompareGreaterThanOrEqualZero(x));
                testThrowsPlatformNotSupported<Vector64< long  >>(name, (x, y) => Simd.CompareGreaterThanOrEqualZero(x));
                testThrowsPlatformNotSupported<Vector64< ulong >>(name, (x, y) => Simd.CompareGreaterThanOrEqualZero(x));
                testThrowsPlatformNotSupported<Vector128<float >>(name, (x, y) => Simd.CompareGreaterThanOrEqualZero(x));
                testThrowsPlatformNotSupported<Vector128<double>>(name, (x, y) => Simd.CompareGreaterThanOrEqualZero(x));
                testThrowsPlatformNotSupported<Vector128<sbyte >>(name, (x, y) => Simd.CompareGreaterThanOrEqualZero(x));
                testThrowsPlatformNotSupported<Vector128<byte  >>(name, (x, y) => Simd.CompareGreaterThanOrEqualZero(x));
                testThrowsPlatformNotSupported<Vector128<short >>(name, (x, y) => Simd.CompareGreaterThanOrEqualZero(x));
                testThrowsPlatformNotSupported<Vector128<ushort>>(name, (x, y) => Simd.CompareGreaterThanOrEqualZero(x));
                testThrowsPlatformNotSupported<Vector128<int   >>(name, (x, y) => Simd.CompareGreaterThanOrEqualZero(x));
                testThrowsPlatformNotSupported<Vector128<uint  >>(name, (x, y) => Simd.CompareGreaterThanOrEqualZero(x));
                testThrowsPlatformNotSupported<Vector128<long  >>(name, (x, y) => Simd.CompareGreaterThanOrEqualZero(x));
                testThrowsPlatformNotSupported<Vector128<ulong >>(name, (x, y) => Simd.CompareGreaterThanOrEqualZero(x));
            }

            Console.WriteLine($"Test{name} passed");
        }

        static void TestCompareLessThanZero()
        {
            String name = "CompareLessThanZero";

            if (Simd.IsSupported)
            {
                testBinOp<float,  Vector128<float >>(name, (x, y) => Simd.CompareLessThanZero(x), (x, y) => boolTo<float >(x < 0));
                testBinOp<double, Vector128<double>>(name, (x, y) => Simd.CompareLessThanZero(x), (x, y) => boolTo<double>(x < 0));
                testBinOp<sbyte,  Vector128<sbyte >>(name, (x, y) => Simd.CompareLessThanZero(x), (x, y) => boolTo<sbyte >(x < 0));
                testBinOp<byte,   Vector128<byte  >>(name, (x, y) => Simd.CompareLessThanZero(x), (x, y) => boolTo<byte  >(x < 0));
                testBinOp<short,  Vector128<short >>(name, (x, y) => Simd.CompareLessThanZero(x), (x, y) => boolTo<short >(x < 0));
                testBinOp<ushort, Vector128<ushort>>(name, (x, y) => Simd.CompareLessThanZero(x), (x, y) => boolTo<ushort>(x < 0));
                testBinOp<int,    Vector128<int   >>(name, (x, y) => Simd.CompareLessThanZero(x), (x, y) => boolTo<int   >(x < 0));
                testBinOp<uint,   Vector128<uint  >>(name, (x, y) => Simd.CompareLessThanZero(x), (x, y) => boolTo<uint  >(x < 0));
                testBinOp<long,   Vector128<long  >>(name, (x, y) => Simd.CompareLessThanZero(x), (x, y) => boolTo<long  >(x < 0));
                testBinOp<ulong,  Vector128<ulong >>(name, (x, y) => Simd.CompareLessThanZero(x), (x, y) => boolTo<ulong >(x < 0));
                testBinOp<float,  Vector64< float >>(name, (x, y) => Simd.CompareLessThanZero(x), (x, y) => boolTo<float >(x < 0));
                testBinOp<sbyte,  Vector64< sbyte >>(name, (x, y) => Simd.CompareLessThanZero(x), (x, y) => boolTo<sbyte >(x < 0));
                testBinOp<byte,   Vector64< byte  >>(name, (x, y) => Simd.CompareLessThanZero(x), (x, y) => boolTo<byte  >(x < 0));
                testBinOp<short,  Vector64< short >>(name, (x, y) => Simd.CompareLessThanZero(x), (x, y) => boolTo<short >(x < 0));
                testBinOp<ushort, Vector64< ushort>>(name, (x, y) => Simd.CompareLessThanZero(x), (x, y) => boolTo<ushort>(x < 0));
                testBinOp<int,    Vector64< int   >>(name, (x, y) => Simd.CompareLessThanZero(x), (x, y) => boolTo<int   >(x < 0));
                testBinOp<uint,   Vector64< uint  >>(name, (x, y) => Simd.CompareLessThanZero(x), (x, y) => boolTo<uint  >(x < 0));

                testThrowsTypeNotSupported<Vector128<Vector128<long> >>(name, (x, y) => Simd.CompareLessThanZero(x));

                testThrowsTypeNotSupported<Vector64< long >>(name, (x, y) => Simd.CompareLessThanZero(x));
                testThrowsTypeNotSupported<Vector64< ulong>>(name, (x, y) => Simd.CompareLessThanZero(x));
                testThrowsTypeNotSupported<Vector64<double>>(name, (x, y) => Simd.CompareLessThanZero(x));
            }
            else
            {
                testThrowsPlatformNotSupported<Vector64< float >>(name, (x, y) => Simd.CompareLessThanZero(x));
                testThrowsPlatformNotSupported<Vector64< double>>(name, (x, y) => Simd.CompareLessThanZero(x));
                testThrowsPlatformNotSupported<Vector64< sbyte >>(name, (x, y) => Simd.CompareLessThanZero(x));
                testThrowsPlatformNotSupported<Vector64< byte  >>(name, (x, y) => Simd.CompareLessThanZero(x));
                testThrowsPlatformNotSupported<Vector64< short >>(name, (x, y) => Simd.CompareLessThanZero(x));
                testThrowsPlatformNotSupported<Vector64< ushort>>(name, (x, y) => Simd.CompareLessThanZero(x));
                testThrowsPlatformNotSupported<Vector64< int   >>(name, (x, y) => Simd.CompareLessThanZero(x));
                testThrowsPlatformNotSupported<Vector64< uint  >>(name, (x, y) => Simd.CompareLessThanZero(x));
                testThrowsPlatformNotSupported<Vector64< long  >>(name, (x, y) => Simd.CompareLessThanZero(x));
                testThrowsPlatformNotSupported<Vector64< ulong >>(name, (x, y) => Simd.CompareLessThanZero(x));
                testThrowsPlatformNotSupported<Vector128<float >>(name, (x, y) => Simd.CompareLessThanZero(x));
                testThrowsPlatformNotSupported<Vector128<double>>(name, (x, y) => Simd.CompareLessThanZero(x));
                testThrowsPlatformNotSupported<Vector128<sbyte >>(name, (x, y) => Simd.CompareLessThanZero(x));
                testThrowsPlatformNotSupported<Vector128<byte  >>(name, (x, y) => Simd.CompareLessThanZero(x));
                testThrowsPlatformNotSupported<Vector128<short >>(name, (x, y) => Simd.CompareLessThanZero(x));
                testThrowsPlatformNotSupported<Vector128<ushort>>(name, (x, y) => Simd.CompareLessThanZero(x));
                testThrowsPlatformNotSupported<Vector128<int   >>(name, (x, y) => Simd.CompareLessThanZero(x));
                testThrowsPlatformNotSupported<Vector128<uint  >>(name, (x, y) => Simd.CompareLessThanZero(x));
                testThrowsPlatformNotSupported<Vector128<long  >>(name, (x, y) => Simd.CompareLessThanZero(x));
                testThrowsPlatformNotSupported<Vector128<ulong >>(name, (x, y) => Simd.CompareLessThanZero(x));
            }

            Console.WriteLine($"Test{name} passed");
        }

        static void TestCompareLessThanOrEqualZero()
        {
            String name = "CompareLessThanOrEqualZero";

            if (Simd.IsSupported)
            {
                testBinOp<float,  Vector128<float >>(name, (x, y) => Simd.CompareLessThanOrEqualZero(x), (x, y) => boolTo<float >(x <= 0));
                testBinOp<double, Vector128<double>>(name, (x, y) => Simd.CompareLessThanOrEqualZero(x), (x, y) => boolTo<double>(x <= 0));
                testBinOp<sbyte,  Vector128<sbyte >>(name, (x, y) => Simd.CompareLessThanOrEqualZero(x), (x, y) => boolTo<sbyte >(x <= 0));
                testBinOp<byte,   Vector128<byte  >>(name, (x, y) => Simd.CompareLessThanOrEqualZero(x), (x, y) => boolTo<byte  >(x <= 0));
                testBinOp<short,  Vector128<short >>(name, (x, y) => Simd.CompareLessThanOrEqualZero(x), (x, y) => boolTo<short >(x <= 0));
                testBinOp<ushort, Vector128<ushort>>(name, (x, y) => Simd.CompareLessThanOrEqualZero(x), (x, y) => boolTo<ushort>(x <= 0));
                testBinOp<int,    Vector128<int   >>(name, (x, y) => Simd.CompareLessThanOrEqualZero(x), (x, y) => boolTo<int   >(x <= 0));
                testBinOp<uint,   Vector128<uint  >>(name, (x, y) => Simd.CompareLessThanOrEqualZero(x), (x, y) => boolTo<uint  >(x <= 0));
                testBinOp<long,   Vector128<long  >>(name, (x, y) => Simd.CompareLessThanOrEqualZero(x), (x, y) => boolTo<long  >(x <= 0));
                testBinOp<ulong,  Vector128<ulong >>(name, (x, y) => Simd.CompareLessThanOrEqualZero(x), (x, y) => boolTo<ulong >(x <= 0));
                testBinOp<float,  Vector64< float >>(name, (x, y) => Simd.CompareLessThanOrEqualZero(x), (x, y) => boolTo<float >(x <= 0));
                testBinOp<sbyte,  Vector64< sbyte >>(name, (x, y) => Simd.CompareLessThanOrEqualZero(x), (x, y) => boolTo<sbyte >(x <= 0));
                testBinOp<byte,   Vector64< byte  >>(name, (x, y) => Simd.CompareLessThanOrEqualZero(x), (x, y) => boolTo<byte  >(x <= 0));
                testBinOp<short,  Vector64< short >>(name, (x, y) => Simd.CompareLessThanOrEqualZero(x), (x, y) => boolTo<short >(x <= 0));
                testBinOp<ushort, Vector64< ushort>>(name, (x, y) => Simd.CompareLessThanOrEqualZero(x), (x, y) => boolTo<ushort>(x <= 0));
                testBinOp<int,    Vector64< int   >>(name, (x, y) => Simd.CompareLessThanOrEqualZero(x), (x, y) => boolTo<int   >(x <= 0));
                testBinOp<uint,   Vector64< uint  >>(name, (x, y) => Simd.CompareLessThanOrEqualZero(x), (x, y) => boolTo<uint  >(x <= 0));

                testThrowsTypeNotSupported<Vector128<Vector128<long> >>(name, (x, y) => Simd.CompareLessThanOrEqualZero(x));

                testThrowsTypeNotSupported<Vector64< long >>(name, (x, y) => Simd.CompareLessThanOrEqualZero(x));
                testThrowsTypeNotSupported<Vector64< ulong>>(name, (x, y) => Simd.CompareLessThanOrEqualZero(x));
                testThrowsTypeNotSupported<Vector64<double>>(name, (x, y) => Simd.CompareLessThanOrEqualZero(x));
            }
            else
            {
                testThrowsPlatformNotSupported<Vector64< float >>(name, (x, y) => Simd.CompareLessThanOrEqualZero(x));
                testThrowsPlatformNotSupported<Vector64< double>>(name, (x, y) => Simd.CompareLessThanOrEqualZero(x));
                testThrowsPlatformNotSupported<Vector64< sbyte >>(name, (x, y) => Simd.CompareLessThanOrEqualZero(x));
                testThrowsPlatformNotSupported<Vector64< byte  >>(name, (x, y) => Simd.CompareLessThanOrEqualZero(x));
                testThrowsPlatformNotSupported<Vector64< short >>(name, (x, y) => Simd.CompareLessThanOrEqualZero(x));
                testThrowsPlatformNotSupported<Vector64< ushort>>(name, (x, y) => Simd.CompareLessThanOrEqualZero(x));
                testThrowsPlatformNotSupported<Vector64< int   >>(name, (x, y) => Simd.CompareLessThanOrEqualZero(x));
                testThrowsPlatformNotSupported<Vector64< uint  >>(name, (x, y) => Simd.CompareLessThanOrEqualZero(x));
                testThrowsPlatformNotSupported<Vector64< long  >>(name, (x, y) => Simd.CompareLessThanOrEqualZero(x));
                testThrowsPlatformNotSupported<Vector64< ulong >>(name, (x, y) => Simd.CompareLessThanOrEqualZero(x));
                testThrowsPlatformNotSupported<Vector128<float >>(name, (x, y) => Simd.CompareLessThanOrEqualZero(x));
                testThrowsPlatformNotSupported<Vector128<double>>(name, (x, y) => Simd.CompareLessThanOrEqualZero(x));
                testThrowsPlatformNotSupported<Vector128<sbyte >>(name, (x, y) => Simd.CompareLessThanOrEqualZero(x));
                testThrowsPlatformNotSupported<Vector128<byte  >>(name, (x, y) => Simd.CompareLessThanOrEqualZero(x));
                testThrowsPlatformNotSupported<Vector128<short >>(name, (x, y) => Simd.CompareLessThanOrEqualZero(x));
                testThrowsPlatformNotSupported<Vector128<ushort>>(name, (x, y) => Simd.CompareLessThanOrEqualZero(x));
                testThrowsPlatformNotSupported<Vector128<int   >>(name, (x, y) => Simd.CompareLessThanOrEqualZero(x));
                testThrowsPlatformNotSupported<Vector128<uint  >>(name, (x, y) => Simd.CompareLessThanOrEqualZero(x));
                testThrowsPlatformNotSupported<Vector128<long  >>(name, (x, y) => Simd.CompareLessThanOrEqualZero(x));
                testThrowsPlatformNotSupported<Vector128<ulong >>(name, (x, y) => Simd.CompareLessThanOrEqualZero(x));
            }

            Console.WriteLine($"Test{name} passed");
        }

        static void TestCompareTest()
        {
            String name = "CompareTest";

            if (Simd.IsSupported)
            {
                testBinOp<float,  Vector128<float >>(name, (x, y) => Simd.CompareTest(x, y), (x, y) => boolTo<float >((bits(x) & bits(y)) != 0));
                testBinOp<double, Vector128<double>>(name, (x, y) => Simd.CompareTest(x, y), (x, y) => boolTo<double>((bits(x) & bits(y)) != 0));
                testBinOp<sbyte,  Vector128<sbyte >>(name, (x, y) => Simd.CompareTest(x, y), (x, y) => boolTo<sbyte >((    (x) &     (y)) != 0));
                testBinOp<byte,   Vector128<byte  >>(name, (x, y) => Simd.CompareTest(x, y), (x, y) => boolTo<byte  >((    (x) &     (y)) != 0));
                testBinOp<short,  Vector128<short >>(name, (x, y) => Simd.CompareTest(x, y), (x, y) => boolTo<short >((    (x) &     (y)) != 0));
                testBinOp<ushort, Vector128<ushort>>(name, (x, y) => Simd.CompareTest(x, y), (x, y) => boolTo<ushort>((    (x) &     (y)) != 0));
                testBinOp<int,    Vector128<int   >>(name, (x, y) => Simd.CompareTest(x, y), (x, y) => boolTo<int   >((    (x) &     (y)) != 0));
                testBinOp<uint,   Vector128<uint  >>(name, (x, y) => Simd.CompareTest(x, y), (x, y) => boolTo<uint  >((    (x) &     (y)) != 0));
                testBinOp<long,   Vector128<long  >>(name, (x, y) => Simd.CompareTest(x, y), (x, y) => boolTo<long  >((    (x) &     (y)) != 0));
                testBinOp<ulong,  Vector128<ulong >>(name, (x, y) => Simd.CompareTest(x, y), (x, y) => boolTo<ulong >((    (x) &     (y)) != 0));
                testBinOp<float,  Vector64< float >>(name, (x, y) => Simd.CompareTest(x, y), (x, y) => boolTo<float >((bits(x) & bits(y)) != 0));
                testBinOp<sbyte,  Vector64< sbyte >>(name, (x, y) => Simd.CompareTest(x, y), (x, y) => boolTo<sbyte >((    (x) &     (y)) != 0));
                testBinOp<byte,   Vector64< byte  >>(name, (x, y) => Simd.CompareTest(x, y), (x, y) => boolTo<byte  >((    (x) &     (y)) != 0));
                testBinOp<short,  Vector64< short >>(name, (x, y) => Simd.CompareTest(x, y), (x, y) => boolTo<short >((    (x) &     (y)) != 0));
                testBinOp<ushort, Vector64< ushort>>(name, (x, y) => Simd.CompareTest(x, y), (x, y) => boolTo<ushort>((    (x) &     (y)) != 0));
                testBinOp<int,    Vector64< int   >>(name, (x, y) => Simd.CompareTest(x, y), (x, y) => boolTo<int   >((    (x) &     (y)) != 0));
                testBinOp<uint,   Vector64< uint  >>(name, (x, y) => Simd.CompareTest(x, y), (x, y) => boolTo<uint  >((    (x) &     (y)) != 0));

                testThrowsTypeNotSupported<Vector128<Vector128<long> >>(name, (x, y) => Simd.CompareTest(x, y));

                testThrowsTypeNotSupported<Vector64< long >>(name, (x, y) => Simd.CompareTest(x, y));
                testThrowsTypeNotSupported<Vector64< ulong>>(name, (x, y) => Simd.CompareTest(x, y));
                testThrowsTypeNotSupported<Vector64<double>>(name, (x, y) => Simd.CompareTest(x, y));
            }
            else
            {
                testThrowsPlatformNotSupported<Vector64< float >>(name, (x, y) => Simd.CompareTest(x, y));
                testThrowsPlatformNotSupported<Vector64< double>>(name, (x, y) => Simd.CompareTest(x, y));
                testThrowsPlatformNotSupported<Vector64< sbyte >>(name, (x, y) => Simd.CompareTest(x, y));
                testThrowsPlatformNotSupported<Vector64< byte  >>(name, (x, y) => Simd.CompareTest(x, y));
                testThrowsPlatformNotSupported<Vector64< short >>(name, (x, y) => Simd.CompareTest(x, y));
                testThrowsPlatformNotSupported<Vector64< ushort>>(name, (x, y) => Simd.CompareTest(x, y));
                testThrowsPlatformNotSupported<Vector64< int   >>(name, (x, y) => Simd.CompareTest(x, y));
                testThrowsPlatformNotSupported<Vector64< uint  >>(name, (x, y) => Simd.CompareTest(x, y));
                testThrowsPlatformNotSupported<Vector64< long  >>(name, (x, y) => Simd.CompareTest(x, y));
                testThrowsPlatformNotSupported<Vector64< ulong >>(name, (x, y) => Simd.CompareTest(x, y));
                testThrowsPlatformNotSupported<Vector128<float >>(name, (x, y) => Simd.CompareTest(x, y));
                testThrowsPlatformNotSupported<Vector128<double>>(name, (x, y) => Simd.CompareTest(x, y));
                testThrowsPlatformNotSupported<Vector128<sbyte >>(name, (x, y) => Simd.CompareTest(x, y));
                testThrowsPlatformNotSupported<Vector128<byte  >>(name, (x, y) => Simd.CompareTest(x, y));
                testThrowsPlatformNotSupported<Vector128<short >>(name, (x, y) => Simd.CompareTest(x, y));
                testThrowsPlatformNotSupported<Vector128<ushort>>(name, (x, y) => Simd.CompareTest(x, y));
                testThrowsPlatformNotSupported<Vector128<int   >>(name, (x, y) => Simd.CompareTest(x, y));
                testThrowsPlatformNotSupported<Vector128<uint  >>(name, (x, y) => Simd.CompareTest(x, y));
                testThrowsPlatformNotSupported<Vector128<long  >>(name, (x, y) => Simd.CompareTest(x, y));
                testThrowsPlatformNotSupported<Vector128<ulong >>(name, (x, y) => Simd.CompareTest(x, y));
            }

            Console.WriteLine($"Test{name} passed");
        }

        static void TestDivide()
        {
            String name = "Divide";

            if (Simd.IsSupported)
            {
                testBinOp<float,  Vector128<float >>(name, (x, y) => Simd.Divide(x, y), (x, y) =>         (x / y));
                testBinOp<double, Vector128<double>>(name, (x, y) => Simd.Divide(x, y), (x, y) =>         (x / y));
                testBinOp<float,  Vector64< float >>(name, (x, y) => Simd.Divide(x, y), (x, y) =>         (x / y));
            }
            else
            {
                testThrowsPlatformNotSupported<Vector64< float >>(name, (x, y) => Simd.Divide(x, y));
                testThrowsPlatformNotSupported<Vector128<float >>(name, (x, y) => Simd.Divide(x, y));
                testThrowsPlatformNotSupported<Vector128<double>>(name, (x, y) => Simd.Divide(x, y));
            }

            Console.WriteLine($"Test{name} passed");
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        static T simdExtract<T>(Vector64<T> vector, byte index)
            where T : struct
        {
            return Simd.Extract<T>(vector, index);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        static T simdExtract<T>(Vector128<T> vector, byte index)
            where T : struct
        {
            return Simd.Extract<T>(vector, index);
        }

        static void TestExtract()
        {
            String name = "Extract";

            if (Simd.IsSupported)
            {
                testExtractOp<float,  Vector128<float >>(name, (x) => Simd.Extract(x, 0), (x) => x[ 0]);
                testExtractOp<float,  Vector128<float >>(name, (x) => Simd.Extract(x, 1), (x) => x[ 1]);
                testExtractOp<float,  Vector128<float >>(name, (x) => Simd.Extract(x, 2), (x) => x[ 2]);
                testExtractOp<float,  Vector128<float >>(name, (x) => Simd.Extract(x, 3), (x) => x[ 3]);
                testExtractOp<double, Vector128<double>>(name, (x) => Simd.Extract(x, 0), (x) => x[ 0]);
                testExtractOp<double, Vector128<double>>(name, (x) => Simd.Extract(x, 1), (x) => x[ 1]);
                testExtractOp<sbyte,  Vector128<sbyte >>(name, (x) => Simd.Extract(x, 0), (x) => x[ 0]);
                testExtractOp<sbyte,  Vector128<sbyte >>(name, (x) => Simd.Extract(x, 1), (x) => x[ 1]);
                testExtractOp<sbyte,  Vector128<sbyte >>(name, (x) => Simd.Extract(x, 2), (x) => x[ 2]);
                testExtractOp<sbyte,  Vector128<sbyte >>(name, (x) => Simd.Extract(x, 3), (x) => x[ 3]);
                testExtractOp<sbyte,  Vector128<sbyte >>(name, (x) => Simd.Extract(x, 4), (x) => x[ 4]);
                testExtractOp<sbyte,  Vector128<sbyte >>(name, (x) => Simd.Extract(x, 5), (x) => x[ 5]);
                testExtractOp<sbyte,  Vector128<sbyte >>(name, (x) => Simd.Extract(x, 6), (x) => x[ 6]);
                testExtractOp<sbyte,  Vector128<sbyte >>(name, (x) => Simd.Extract(x, 7), (x) => x[ 7]);
                testExtractOp<sbyte,  Vector128<sbyte >>(name, (x) => Simd.Extract(x, 8), (x) => x[ 8]);
                testExtractOp<sbyte,  Vector128<sbyte >>(name, (x) => Simd.Extract(x, 9), (x) => x[ 9]);
                testExtractOp<sbyte,  Vector128<sbyte >>(name, (x) => Simd.Extract(x,10), (x) => x[10]);
                testExtractOp<sbyte,  Vector128<sbyte >>(name, (x) => Simd.Extract(x,11), (x) => x[11]);
                testExtractOp<sbyte,  Vector128<sbyte >>(name, (x) => Simd.Extract(x,12), (x) => x[12]);
                testExtractOp<sbyte,  Vector128<sbyte >>(name, (x) => Simd.Extract(x,13), (x) => x[13]);
                testExtractOp<sbyte,  Vector128<sbyte >>(name, (x) => Simd.Extract(x,14), (x) => x[14]);
                testExtractOp<sbyte,  Vector128<sbyte >>(name, (x) => Simd.Extract(x,15), (x) => x[15]);
                testExtractOp<byte,   Vector128<byte  >>(name, (x) => Simd.Extract(x, 0), (x) => x[ 0]);
                testExtractOp<byte,   Vector128<byte  >>(name, (x) => Simd.Extract(x, 1), (x) => x[ 1]);
                testExtractOp<byte,   Vector128<byte  >>(name, (x) => Simd.Extract(x, 2), (x) => x[ 2]);
                testExtractOp<byte,   Vector128<byte  >>(name, (x) => Simd.Extract(x, 3), (x) => x[ 3]);
                testExtractOp<byte,   Vector128<byte  >>(name, (x) => Simd.Extract(x, 4), (x) => x[ 4]);
                testExtractOp<byte,   Vector128<byte  >>(name, (x) => Simd.Extract(x, 5), (x) => x[ 5]);
                testExtractOp<byte,   Vector128<byte  >>(name, (x) => Simd.Extract(x, 6), (x) => x[ 6]);
                testExtractOp<byte,   Vector128<byte  >>(name, (x) => Simd.Extract(x, 7), (x) => x[ 7]);
                testExtractOp<byte,   Vector128<byte  >>(name, (x) => Simd.Extract(x, 8), (x) => x[ 8]);
                testExtractOp<byte,   Vector128<byte  >>(name, (x) => Simd.Extract(x, 9), (x) => x[ 9]);
                testExtractOp<byte,   Vector128<byte  >>(name, (x) => Simd.Extract(x,10), (x) => x[10]);
                testExtractOp<byte,   Vector128<byte  >>(name, (x) => Simd.Extract(x,11), (x) => x[11]);
                testExtractOp<byte,   Vector128<byte  >>(name, (x) => Simd.Extract(x,12), (x) => x[12]);
                testExtractOp<byte,   Vector128<byte  >>(name, (x) => Simd.Extract(x,13), (x) => x[13]);
                testExtractOp<byte,   Vector128<byte  >>(name, (x) => Simd.Extract(x,14), (x) => x[14]);
                testExtractOp<byte,   Vector128<byte  >>(name, (x) => Simd.Extract(x,15), (x) => x[15]);
                testExtractOp<short,  Vector128<short >>(name, (x) => Simd.Extract(x, 0), (x) => x[ 0]);
                testExtractOp<short,  Vector128<short >>(name, (x) => Simd.Extract(x, 1), (x) => x[ 1]);
                testExtractOp<short,  Vector128<short >>(name, (x) => Simd.Extract(x, 2), (x) => x[ 2]);
                testExtractOp<short,  Vector128<short >>(name, (x) => Simd.Extract(x, 3), (x) => x[ 3]);
                testExtractOp<short,  Vector128<short >>(name, (x) => Simd.Extract(x, 4), (x) => x[ 4]);
                testExtractOp<short,  Vector128<short >>(name, (x) => Simd.Extract(x, 5), (x) => x[ 5]);
                testExtractOp<short,  Vector128<short >>(name, (x) => Simd.Extract(x, 6), (x) => x[ 6]);
                testExtractOp<short,  Vector128<short >>(name, (x) => Simd.Extract(x, 7), (x) => x[ 7]);
                testExtractOp<ushort, Vector128<ushort>>(name, (x) => Simd.Extract(x, 0), (x) => x[ 0]);
                testExtractOp<ushort, Vector128<ushort>>(name, (x) => Simd.Extract(x, 1), (x) => x[ 1]);
                testExtractOp<ushort, Vector128<ushort>>(name, (x) => Simd.Extract(x, 2), (x) => x[ 2]);
                testExtractOp<ushort, Vector128<ushort>>(name, (x) => Simd.Extract(x, 3), (x) => x[ 3]);
                testExtractOp<ushort, Vector128<ushort>>(name, (x) => Simd.Extract(x, 4), (x) => x[ 4]);
                testExtractOp<ushort, Vector128<ushort>>(name, (x) => Simd.Extract(x, 5), (x) => x[ 5]);
                testExtractOp<ushort, Vector128<ushort>>(name, (x) => Simd.Extract(x, 6), (x) => x[ 6]);
                testExtractOp<ushort, Vector128<ushort>>(name, (x) => Simd.Extract(x, 7), (x) => x[ 7]);
                testExtractOp<int,    Vector128<int   >>(name, (x) => Simd.Extract(x, 0), (x) => x[ 0]);
                testExtractOp<int,    Vector128<int   >>(name, (x) => Simd.Extract(x, 1), (x) => x[ 1]);
                testExtractOp<int,    Vector128<int   >>(name, (x) => Simd.Extract(x, 2), (x) => x[ 2]);
                testExtractOp<int,    Vector128<int   >>(name, (x) => Simd.Extract(x, 3), (x) => x[ 3]);
                testExtractOp<uint,   Vector128<uint  >>(name, (x) => Simd.Extract(x, 0), (x) => x[ 0]);
                testExtractOp<uint,   Vector128<uint  >>(name, (x) => Simd.Extract(x, 1), (x) => x[ 1]);
                testExtractOp<uint,   Vector128<uint  >>(name, (x) => Simd.Extract(x, 2), (x) => x[ 2]);
                testExtractOp<uint,   Vector128<uint  >>(name, (x) => Simd.Extract(x, 3), (x) => x[ 3]);
                testExtractOp<long,   Vector128<long  >>(name, (x) => Simd.Extract(x, 0), (x) => x[ 0]);
                testExtractOp<long,   Vector128<long  >>(name, (x) => Simd.Extract(x, 1), (x) => x[ 1]);
                testExtractOp<ulong,  Vector128<ulong >>(name, (x) => Simd.Extract(x, 0), (x) => x[ 0]);
                testExtractOp<ulong,  Vector128<ulong >>(name, (x) => Simd.Extract(x, 1), (x) => x[ 1]);
                testExtractOp<float,  Vector64< float >>(name, (x) => Simd.Extract(x, 0), (x) => x[ 0]);
                testExtractOp<float,  Vector64< float >>(name, (x) => Simd.Extract(x, 1), (x) => x[ 1]);
                testExtractOp<sbyte,  Vector64< sbyte >>(name, (x) => Simd.Extract(x, 0), (x) => x[ 0]);
                testExtractOp<sbyte,  Vector64< sbyte >>(name, (x) => Simd.Extract(x, 1), (x) => x[ 1]);
                testExtractOp<sbyte,  Vector64< sbyte >>(name, (x) => Simd.Extract(x, 2), (x) => x[ 2]);
                testExtractOp<sbyte,  Vector64< sbyte >>(name, (x) => Simd.Extract(x, 3), (x) => x[ 3]);
                testExtractOp<sbyte,  Vector64< sbyte >>(name, (x) => Simd.Extract(x, 4), (x) => x[ 4]);
                testExtractOp<sbyte,  Vector64< sbyte >>(name, (x) => Simd.Extract(x, 5), (x) => x[ 5]);
                testExtractOp<sbyte,  Vector64< sbyte >>(name, (x) => Simd.Extract(x, 6), (x) => x[ 6]);
                testExtractOp<sbyte,  Vector64< sbyte >>(name, (x) => Simd.Extract(x, 7), (x) => x[ 7]);
                testExtractOp<byte,   Vector64< byte  >>(name, (x) => Simd.Extract(x, 0), (x) => x[ 0]);
                testExtractOp<byte,   Vector64< byte  >>(name, (x) => Simd.Extract(x, 1), (x) => x[ 1]);
                testExtractOp<byte,   Vector64< byte  >>(name, (x) => Simd.Extract(x, 2), (x) => x[ 2]);
                testExtractOp<byte,   Vector64< byte  >>(name, (x) => Simd.Extract(x, 3), (x) => x[ 3]);
                testExtractOp<byte,   Vector64< byte  >>(name, (x) => Simd.Extract(x, 4), (x) => x[ 4]);
                testExtractOp<byte,   Vector64< byte  >>(name, (x) => Simd.Extract(x, 5), (x) => x[ 5]);
                testExtractOp<byte,   Vector64< byte  >>(name, (x) => Simd.Extract(x, 6), (x) => x[ 6]);
                testExtractOp<byte,   Vector64< byte  >>(name, (x) => Simd.Extract(x, 7), (x) => x[ 7]);
                testExtractOp<short,  Vector64< short >>(name, (x) => Simd.Extract(x, 0), (x) => x[ 0]);
                testExtractOp<short,  Vector64< short >>(name, (x) => Simd.Extract(x, 1), (x) => x[ 1]);
                testExtractOp<short,  Vector64< short >>(name, (x) => Simd.Extract(x, 2), (x) => x[ 2]);
                testExtractOp<short,  Vector64< short >>(name, (x) => Simd.Extract(x, 3), (x) => x[ 3]);
                testExtractOp<ushort, Vector64< ushort>>(name, (x) => Simd.Extract(x, 0), (x) => x[ 0]);
                testExtractOp<ushort, Vector64< ushort>>(name, (x) => Simd.Extract(x, 1), (x) => x[ 1]);
                testExtractOp<ushort, Vector64< ushort>>(name, (x) => Simd.Extract(x, 2), (x) => x[ 2]);
                testExtractOp<ushort, Vector64< ushort>>(name, (x) => Simd.Extract(x, 3), (x) => x[ 3]);
                testExtractOp<int,    Vector64< int   >>(name, (x) => Simd.Extract(x, 0), (x) => x[ 0]);
                testExtractOp<int,    Vector64< int   >>(name, (x) => Simd.Extract(x, 1), (x) => x[ 1]);
                testExtractOp<uint,   Vector64< uint  >>(name, (x) => Simd.Extract(x, 0), (x) => x[ 0]);
                testExtractOp<uint,   Vector64< uint  >>(name, (x) => Simd.Extract(x, 1), (x) => x[ 1]);

                // Test non-constant call
                testExtractOp<float,  Vector128<float >>(name, (x) => simdExtract(x, 0), (x) => x[ 0]);
                testExtractOp<float,  Vector128<float >>(name, (x) => simdExtract(x, 1), (x) => x[ 1]);
                testExtractOp<float,  Vector128<float >>(name, (x) => simdExtract(x, 2), (x) => x[ 2]);
                testExtractOp<float,  Vector128<float >>(name, (x) => simdExtract(x, 3), (x) => x[ 3]);
                testExtractOp<double, Vector128<double>>(name, (x) => simdExtract(x, 0), (x) => x[ 0]);
                testExtractOp<double, Vector128<double>>(name, (x) => simdExtract(x, 1), (x) => x[ 1]);
                testExtractOp<sbyte,  Vector128<sbyte >>(name, (x) => simdExtract(x, 0), (x) => x[ 0]);
                testExtractOp<sbyte,  Vector128<sbyte >>(name, (x) => simdExtract(x, 1), (x) => x[ 1]);
                testExtractOp<sbyte,  Vector128<sbyte >>(name, (x) => simdExtract(x, 2), (x) => x[ 2]);
                testExtractOp<sbyte,  Vector128<sbyte >>(name, (x) => simdExtract(x, 3), (x) => x[ 3]);
                testExtractOp<sbyte,  Vector128<sbyte >>(name, (x) => simdExtract(x, 4), (x) => x[ 4]);
                testExtractOp<sbyte,  Vector128<sbyte >>(name, (x) => simdExtract(x, 5), (x) => x[ 5]);
                testExtractOp<sbyte,  Vector128<sbyte >>(name, (x) => simdExtract(x, 6), (x) => x[ 6]);
                testExtractOp<sbyte,  Vector128<sbyte >>(name, (x) => simdExtract(x, 7), (x) => x[ 7]);
                testExtractOp<sbyte,  Vector128<sbyte >>(name, (x) => simdExtract(x, 8), (x) => x[ 8]);
                testExtractOp<sbyte,  Vector128<sbyte >>(name, (x) => simdExtract(x, 9), (x) => x[ 9]);
                testExtractOp<sbyte,  Vector128<sbyte >>(name, (x) => simdExtract(x,10), (x) => x[10]);
                testExtractOp<sbyte,  Vector128<sbyte >>(name, (x) => simdExtract(x,11), (x) => x[11]);
                testExtractOp<sbyte,  Vector128<sbyte >>(name, (x) => simdExtract(x,12), (x) => x[12]);
                testExtractOp<sbyte,  Vector128<sbyte >>(name, (x) => simdExtract(x,13), (x) => x[13]);
                testExtractOp<sbyte,  Vector128<sbyte >>(name, (x) => simdExtract(x,14), (x) => x[14]);
                testExtractOp<sbyte,  Vector128<sbyte >>(name, (x) => simdExtract(x,15), (x) => x[15]);
                testExtractOp<byte,   Vector128<byte  >>(name, (x) => simdExtract(x, 0), (x) => x[ 0]);
                testExtractOp<byte,   Vector128<byte  >>(name, (x) => simdExtract(x, 1), (x) => x[ 1]);
                testExtractOp<byte,   Vector128<byte  >>(name, (x) => simdExtract(x, 2), (x) => x[ 2]);
                testExtractOp<byte,   Vector128<byte  >>(name, (x) => simdExtract(x, 3), (x) => x[ 3]);
                testExtractOp<byte,   Vector128<byte  >>(name, (x) => simdExtract(x, 4), (x) => x[ 4]);
                testExtractOp<byte,   Vector128<byte  >>(name, (x) => simdExtract(x, 5), (x) => x[ 5]);
                testExtractOp<byte,   Vector128<byte  >>(name, (x) => simdExtract(x, 6), (x) => x[ 6]);
                testExtractOp<byte,   Vector128<byte  >>(name, (x) => simdExtract(x, 7), (x) => x[ 7]);
                testExtractOp<byte,   Vector128<byte  >>(name, (x) => simdExtract(x, 8), (x) => x[ 8]);
                testExtractOp<byte,   Vector128<byte  >>(name, (x) => simdExtract(x, 9), (x) => x[ 9]);
                testExtractOp<byte,   Vector128<byte  >>(name, (x) => simdExtract(x,10), (x) => x[10]);
                testExtractOp<byte,   Vector128<byte  >>(name, (x) => simdExtract(x,11), (x) => x[11]);
                testExtractOp<byte,   Vector128<byte  >>(name, (x) => simdExtract(x,12), (x) => x[12]);
                testExtractOp<byte,   Vector128<byte  >>(name, (x) => simdExtract(x,13), (x) => x[13]);
                testExtractOp<byte,   Vector128<byte  >>(name, (x) => simdExtract(x,14), (x) => x[14]);
                testExtractOp<byte,   Vector128<byte  >>(name, (x) => simdExtract(x,15), (x) => x[15]);
                testExtractOp<short,  Vector128<short >>(name, (x) => simdExtract(x, 0), (x) => x[ 0]);
                testExtractOp<short,  Vector128<short >>(name, (x) => simdExtract(x, 1), (x) => x[ 1]);
                testExtractOp<short,  Vector128<short >>(name, (x) => simdExtract(x, 2), (x) => x[ 2]);
                testExtractOp<short,  Vector128<short >>(name, (x) => simdExtract(x, 3), (x) => x[ 3]);
                testExtractOp<short,  Vector128<short >>(name, (x) => simdExtract(x, 4), (x) => x[ 4]);
                testExtractOp<short,  Vector128<short >>(name, (x) => simdExtract(x, 5), (x) => x[ 5]);
                testExtractOp<short,  Vector128<short >>(name, (x) => simdExtract(x, 6), (x) => x[ 6]);
                testExtractOp<short,  Vector128<short >>(name, (x) => simdExtract(x, 7), (x) => x[ 7]);
                testExtractOp<ushort, Vector128<ushort>>(name, (x) => simdExtract(x, 0), (x) => x[ 0]);
                testExtractOp<ushort, Vector128<ushort>>(name, (x) => simdExtract(x, 1), (x) => x[ 1]);
                testExtractOp<ushort, Vector128<ushort>>(name, (x) => simdExtract(x, 2), (x) => x[ 2]);
                testExtractOp<ushort, Vector128<ushort>>(name, (x) => simdExtract(x, 3), (x) => x[ 3]);
                testExtractOp<ushort, Vector128<ushort>>(name, (x) => simdExtract(x, 4), (x) => x[ 4]);
                testExtractOp<ushort, Vector128<ushort>>(name, (x) => simdExtract(x, 5), (x) => x[ 5]);
                testExtractOp<ushort, Vector128<ushort>>(name, (x) => simdExtract(x, 6), (x) => x[ 6]);
                testExtractOp<ushort, Vector128<ushort>>(name, (x) => simdExtract(x, 7), (x) => x[ 7]);
                testExtractOp<int,    Vector128<int   >>(name, (x) => simdExtract(x, 0), (x) => x[ 0]);
                testExtractOp<int,    Vector128<int   >>(name, (x) => simdExtract(x, 1), (x) => x[ 1]);
                testExtractOp<int,    Vector128<int   >>(name, (x) => simdExtract(x, 2), (x) => x[ 2]);
                testExtractOp<int,    Vector128<int   >>(name, (x) => simdExtract(x, 3), (x) => x[ 3]);
                testExtractOp<uint,   Vector128<uint  >>(name, (x) => simdExtract(x, 0), (x) => x[ 0]);
                testExtractOp<uint,   Vector128<uint  >>(name, (x) => simdExtract(x, 1), (x) => x[ 1]);
                testExtractOp<uint,   Vector128<uint  >>(name, (x) => simdExtract(x, 2), (x) => x[ 2]);
                testExtractOp<uint,   Vector128<uint  >>(name, (x) => simdExtract(x, 3), (x) => x[ 3]);
                testExtractOp<long,   Vector128<long  >>(name, (x) => simdExtract(x, 0), (x) => x[ 0]);
                testExtractOp<long,   Vector128<long  >>(name, (x) => simdExtract(x, 1), (x) => x[ 1]);
                testExtractOp<ulong,  Vector128<ulong >>(name, (x) => simdExtract(x, 0), (x) => x[ 0]);
                testExtractOp<ulong,  Vector128<ulong >>(name, (x) => simdExtract(x, 1), (x) => x[ 1]);
                testExtractOp<float,  Vector64< float >>(name, (x) => simdExtract(x, 0), (x) => x[ 0]);
                testExtractOp<float,  Vector64< float >>(name, (x) => simdExtract(x, 1), (x) => x[ 1]);
                testExtractOp<sbyte,  Vector64< sbyte >>(name, (x) => simdExtract(x, 0), (x) => x[ 0]);
                testExtractOp<sbyte,  Vector64< sbyte >>(name, (x) => simdExtract(x, 1), (x) => x[ 1]);
                testExtractOp<sbyte,  Vector64< sbyte >>(name, (x) => simdExtract(x, 2), (x) => x[ 2]);
                testExtractOp<sbyte,  Vector64< sbyte >>(name, (x) => simdExtract(x, 3), (x) => x[ 3]);
                testExtractOp<sbyte,  Vector64< sbyte >>(name, (x) => simdExtract(x, 4), (x) => x[ 4]);
                testExtractOp<sbyte,  Vector64< sbyte >>(name, (x) => simdExtract(x, 5), (x) => x[ 5]);
                testExtractOp<sbyte,  Vector64< sbyte >>(name, (x) => simdExtract(x, 6), (x) => x[ 6]);
                testExtractOp<sbyte,  Vector64< sbyte >>(name, (x) => simdExtract(x, 7), (x) => x[ 7]);
                testExtractOp<byte,   Vector64< byte  >>(name, (x) => simdExtract(x, 0), (x) => x[ 0]);
                testExtractOp<byte,   Vector64< byte  >>(name, (x) => simdExtract(x, 1), (x) => x[ 1]);
                testExtractOp<byte,   Vector64< byte  >>(name, (x) => simdExtract(x, 2), (x) => x[ 2]);
                testExtractOp<byte,   Vector64< byte  >>(name, (x) => simdExtract(x, 3), (x) => x[ 3]);
                testExtractOp<byte,   Vector64< byte  >>(name, (x) => simdExtract(x, 4), (x) => x[ 4]);
                testExtractOp<byte,   Vector64< byte  >>(name, (x) => simdExtract(x, 5), (x) => x[ 5]);
                testExtractOp<byte,   Vector64< byte  >>(name, (x) => simdExtract(x, 6), (x) => x[ 6]);
                testExtractOp<byte,   Vector64< byte  >>(name, (x) => simdExtract(x, 7), (x) => x[ 7]);
                testExtractOp<short,  Vector64< short >>(name, (x) => simdExtract(x, 0), (x) => x[ 0]);
                testExtractOp<short,  Vector64< short >>(name, (x) => simdExtract(x, 1), (x) => x[ 1]);
                testExtractOp<short,  Vector64< short >>(name, (x) => simdExtract(x, 2), (x) => x[ 2]);
                testExtractOp<short,  Vector64< short >>(name, (x) => simdExtract(x, 3), (x) => x[ 3]);
                testExtractOp<ushort, Vector64< ushort>>(name, (x) => simdExtract(x, 0), (x) => x[ 0]);
                testExtractOp<ushort, Vector64< ushort>>(name, (x) => simdExtract(x, 1), (x) => x[ 1]);
                testExtractOp<ushort, Vector64< ushort>>(name, (x) => simdExtract(x, 2), (x) => x[ 2]);
                testExtractOp<ushort, Vector64< ushort>>(name, (x) => simdExtract(x, 3), (x) => x[ 3]);
                testExtractOp<int,    Vector64< int   >>(name, (x) => simdExtract(x, 0), (x) => x[ 0]);
                testExtractOp<int,    Vector64< int   >>(name, (x) => simdExtract(x, 1), (x) => x[ 1]);
                testExtractOp<uint,   Vector64< uint  >>(name, (x) => simdExtract(x, 0), (x) => x[ 0]);
                testExtractOp<uint,   Vector64< uint  >>(name, (x) => simdExtract(x, 1), (x) => x[ 1]);

                testThrowsArgumentOutOfRangeException<float,  Vector128<float >>(name, (x, y) => Simd.Extract(x, 4));
                testThrowsArgumentOutOfRangeException<double, Vector128<double>>(name, (x, y) => Simd.Extract(x, 2));
                testThrowsArgumentOutOfRangeException<sbyte,  Vector128<sbyte >>(name, (x, y) => Simd.Extract(x,16));
                testThrowsArgumentOutOfRangeException<byte,   Vector128<byte  >>(name, (x, y) => Simd.Extract(x,16));
                testThrowsArgumentOutOfRangeException<short,  Vector128<short >>(name, (x, y) => Simd.Extract(x, 8));
                testThrowsArgumentOutOfRangeException<ushort, Vector128<ushort>>(name, (x, y) => Simd.Extract(x, 8));
                testThrowsArgumentOutOfRangeException<int,    Vector128<int   >>(name, (x, y) => Simd.Extract(x, 4));
                testThrowsArgumentOutOfRangeException<uint,   Vector128<uint  >>(name, (x, y) => Simd.Extract(x, 4));
                testThrowsArgumentOutOfRangeException<long,   Vector128<long  >>(name, (x, y) => Simd.Extract(x, 2));
                testThrowsArgumentOutOfRangeException<ulong,  Vector128<ulong >>(name, (x, y) => Simd.Extract(x, 2));
                testThrowsArgumentOutOfRangeException<float,  Vector64< float >>(name, (x, y) => Simd.Extract(x, 2));
                testThrowsArgumentOutOfRangeException<sbyte,  Vector64< sbyte >>(name, (x, y) => Simd.Extract(x, 8));
                testThrowsArgumentOutOfRangeException<byte,   Vector64< byte  >>(name, (x, y) => Simd.Extract(x, 8));
                testThrowsArgumentOutOfRangeException<short,  Vector64< short >>(name, (x, y) => Simd.Extract(x, 4));
                testThrowsArgumentOutOfRangeException<ushort, Vector64< ushort>>(name, (x, y) => Simd.Extract(x, 4));
                testThrowsArgumentOutOfRangeException<int,    Vector64< int   >>(name, (x, y) => Simd.Extract(x, 2));
                testThrowsArgumentOutOfRangeException<uint,   Vector64< uint  >>(name, (x, y) => Simd.Extract(x, 2));

                testThrowsTypeNotSupported<Vector64< long >>(name, (x, y) => { return Simd.Extract(x, 1) > 1 ? x : y; });
                testThrowsTypeNotSupported<Vector64< ulong>>(name, (x, y) => { return Simd.Extract(x, 1) > 1 ? x : y; });
                testThrowsTypeNotSupported<Vector64<double>>(name, (x, y) => { return Simd.Extract(x, 1) > 1 ? x : y; });
            }
            else
            {
                testThrowsPlatformNotSupported<Vector64<float>  , float >(name, (x, y) => Simd.Extract(x, 1));
                testThrowsPlatformNotSupported<Vector64<double> , double>(name, (x, y) => Simd.Extract(x, 1));
                testThrowsPlatformNotSupported<Vector64<sbyte>  , sbyte >(name, (x, y) => Simd.Extract(x, 1));
                testThrowsPlatformNotSupported<Vector64<byte>   , byte  >(name, (x, y) => Simd.Extract(x, 1));
                testThrowsPlatformNotSupported<Vector64<short>  , short >(name, (x, y) => Simd.Extract(x, 1));
                testThrowsPlatformNotSupported<Vector64<ushort> , ushort>(name, (x, y) => Simd.Extract(x, 1));
                testThrowsPlatformNotSupported<Vector64<int>    , int   >(name, (x, y) => Simd.Extract(x, 1));
                testThrowsPlatformNotSupported<Vector64<uint>   , uint  >(name, (x, y) => Simd.Extract(x, 1));
                testThrowsPlatformNotSupported<Vector64<long>   , long  >(name, (x, y) => Simd.Extract(x, 1));
                testThrowsPlatformNotSupported<Vector64<ulong>  , ulong >(name, (x, y) => Simd.Extract(x, 1));
                testThrowsPlatformNotSupported<Vector128<float> , float >(name, (x, y) => Simd.Extract(x, 1));
                testThrowsPlatformNotSupported<Vector128<double>, double>(name, (x, y) => Simd.Extract(x, 1));
                testThrowsPlatformNotSupported<Vector128<sbyte> , sbyte >(name, (x, y) => Simd.Extract(x, 1));
                testThrowsPlatformNotSupported<Vector128<byte>  , byte  >(name, (x, y) => Simd.Extract(x, 1));
                testThrowsPlatformNotSupported<Vector128<short> , short >(name, (x, y) => Simd.Extract(x, 1));
                testThrowsPlatformNotSupported<Vector128<ushort>, ushort>(name, (x, y) => Simd.Extract(x, 1));
                testThrowsPlatformNotSupported<Vector128<int>   , int   >(name, (x, y) => Simd.Extract(x, 1));
                testThrowsPlatformNotSupported<Vector128<uint>  , uint  >(name, (x, y) => Simd.Extract(x, 1));
                testThrowsPlatformNotSupported<Vector128<long>  , long  >(name, (x, y) => Simd.Extract(x, 1));
                testThrowsPlatformNotSupported<Vector128<ulong> , ulong >(name, (x, y) => Simd.Extract(x, 1));
            }

            Console.WriteLine($"Test{name} passed");
        }

        static void TestInsert()
        {
            String name = "Insert";

            if (Simd.IsSupported)
            {
                testPermuteOp<float,  Vector128<float >>(name, (x, y) => Simd.Insert(x, 1, (float )2), (i, x, y) => (float )(i != 1 ? x[i] : 2));
                testPermuteOp<double, Vector128<double>>(name, (x, y) => Simd.Insert(x, 1, (double)2), (i, x, y) => (double)(i != 1 ? x[i] : 2));
                testPermuteOp<sbyte,  Vector128<sbyte >>(name, (x, y) => Simd.Insert(x, 1, (sbyte )2), (i, x, y) => (sbyte )(i != 1 ? x[i] : 2));
                testPermuteOp<byte,   Vector128<byte  >>(name, (x, y) => Simd.Insert(x, 1, (byte  )2), (i, x, y) => (byte  )(i != 1 ? x[i] : 2));
                testPermuteOp<short,  Vector128<short >>(name, (x, y) => Simd.Insert(x, 1, (short )2), (i, x, y) => (short )(i != 1 ? x[i] : 2));
                testPermuteOp<ushort, Vector128<ushort>>(name, (x, y) => Simd.Insert(x, 1, (ushort)2), (i, x, y) => (ushort)(i != 1 ? x[i] : 2));
                testPermuteOp<int,    Vector128<int   >>(name, (x, y) => Simd.Insert(x, 1, (int   )2), (i, x, y) => (int   )(i != 1 ? x[i] : 2));
                testPermuteOp<uint,   Vector128<uint  >>(name, (x, y) => Simd.Insert(x, 1, (uint  )2), (i, x, y) => (uint  )(i != 1 ? x[i] : 2));
                testPermuteOp<long,   Vector128<long  >>(name, (x, y) => Simd.Insert(x, 1, (long  )2), (i, x, y) => (long  )(i != 1 ? x[i] : 2));
                testPermuteOp<ulong,  Vector128<ulong >>(name, (x, y) => Simd.Insert(x, 1, (ulong )2), (i, x, y) => (ulong )(i != 1 ? x[i] : 2));
                testPermuteOp<float,  Vector64< float >>(name, (x, y) => Simd.Insert(x, 1, (float )2), (i, x, y) => (float )(i != 1 ? x[i] : 2));
                testPermuteOp<sbyte,  Vector64< sbyte >>(name, (x, y) => Simd.Insert(x, 1, (sbyte )2), (i, x, y) => (sbyte )(i != 1 ? x[i] : 2));
                testPermuteOp<byte,   Vector64< byte  >>(name, (x, y) => Simd.Insert(x, 1, (byte  )2), (i, x, y) => (byte  )(i != 1 ? x[i] : 2));
                testPermuteOp<short,  Vector64< short >>(name, (x, y) => Simd.Insert(x, 1, (short )2), (i, x, y) => (short )(i != 1 ? x[i] : 2));
                testPermuteOp<ushort, Vector64< ushort>>(name, (x, y) => Simd.Insert(x, 1, (ushort)2), (i, x, y) => (ushort)(i != 1 ? x[i] : 2));
                testPermuteOp<int,    Vector64< int   >>(name, (x, y) => Simd.Insert(x, 1, (int   )2), (i, x, y) => (int   )(i != 1 ? x[i] : 2));
                testPermuteOp<uint,   Vector64< uint  >>(name, (x, y) => Simd.Insert(x, 1, (uint  )2), (i, x, y) => (uint  )(i != 1 ? x[i] : 2));

                testPermuteOp<float,  Vector128<float >>(name, (x, y) => Simd.Insert(x, 3, Simd.Extract(y, 1)), (i, x, y) => (float )(i != 3 ? x[i] : y[1]));
                testPermuteOp<double, Vector128<double>>(name, (x, y) => Simd.Insert(x, 0, Simd.Extract(y, 1)), (i, x, y) => (double)(i != 0 ? x[i] : y[1]));
                testPermuteOp<sbyte,  Vector128<sbyte >>(name, (x, y) => Simd.Insert(x, 9, Simd.Extract(y, 1)), (i, x, y) => (sbyte )(i != 9 ? x[i] : y[1]));
                testPermuteOp<byte,   Vector128<byte  >>(name, (x, y) => Simd.Insert(x, 9, Simd.Extract(y, 1)), (i, x, y) => (byte  )(i != 9 ? x[i] : y[1]));
                testPermuteOp<short,  Vector128<short >>(name, (x, y) => Simd.Insert(x, 5, Simd.Extract(y, 1)), (i, x, y) => (short )(i != 5 ? x[i] : y[1]));
                testPermuteOp<ushort, Vector128<ushort>>(name, (x, y) => Simd.Insert(x, 5, Simd.Extract(y, 1)), (i, x, y) => (ushort)(i != 5 ? x[i] : y[1]));
                testPermuteOp<int,    Vector128<int   >>(name, (x, y) => Simd.Insert(x, 2, Simd.Extract(y, 1)), (i, x, y) => (int   )(i != 2 ? x[i] : y[1]));
                testPermuteOp<uint,   Vector128<uint  >>(name, (x, y) => Simd.Insert(x, 2, Simd.Extract(y, 1)), (i, x, y) => (uint  )(i != 2 ? x[i] : y[1]));
                testPermuteOp<long,   Vector128<long  >>(name, (x, y) => Simd.Insert(x, 0, Simd.Extract(y, 1)), (i, x, y) => (long  )(i != 0 ? x[i] : y[1]));
                testPermuteOp<ulong,  Vector128<ulong >>(name, (x, y) => Simd.Insert(x, 0, Simd.Extract(y, 1)), (i, x, y) => (ulong )(i != 0 ? x[i] : y[1]));
                testPermuteOp<float,  Vector64< float >>(name, (x, y) => Simd.Insert(x, 0, Simd.Extract(y, 1)), (i, x, y) => (float )(i != 0 ? x[i] : y[1]));
                testPermuteOp<sbyte,  Vector64< sbyte >>(name, (x, y) => Simd.Insert(x, 7, Simd.Extract(y, 1)), (i, x, y) => (sbyte )(i != 7 ? x[i] : y[1]));
                testPermuteOp<byte,   Vector64< byte  >>(name, (x, y) => Simd.Insert(x, 7, Simd.Extract(y, 1)), (i, x, y) => (byte  )(i != 7 ? x[i] : y[1]));
                testPermuteOp<short,  Vector64< short >>(name, (x, y) => Simd.Insert(x, 2, Simd.Extract(y, 1)), (i, x, y) => (short )(i != 2 ? x[i] : y[1]));
                testPermuteOp<ushort, Vector64< ushort>>(name, (x, y) => Simd.Insert(x, 2, Simd.Extract(y, 1)), (i, x, y) => (ushort)(i != 2 ? x[i] : y[1]));
                testPermuteOp<int,    Vector64< int   >>(name, (x, y) => Simd.Insert(x, 0, Simd.Extract(y, 1)), (i, x, y) => (int   )(i != 0 ? x[i] : y[1]));
                testPermuteOp<uint,   Vector64< uint  >>(name, (x, y) => Simd.Insert(x, 0, Simd.Extract(y, 1)), (i, x, y) => (uint  )(i != 0 ? x[i] : y[1]));

                testThrowsArgumentOutOfRangeException<float,  Vector128<float >, Vector128<float >>(name, (x, y) => Simd.Insert(x, 4, (float )1));
                testThrowsArgumentOutOfRangeException<double, Vector128<double>, Vector128<double>>(name, (x, y) => Simd.Insert(x, 2, (double)1));
                testThrowsArgumentOutOfRangeException<sbyte,  Vector128<sbyte >, Vector128<sbyte >>(name, (x, y) => Simd.Insert(x,16, (sbyte )1));
                testThrowsArgumentOutOfRangeException<byte,   Vector128<byte  >, Vector128<byte  >>(name, (x, y) => Simd.Insert(x,16, (byte  )1));
                testThrowsArgumentOutOfRangeException<short,  Vector128<short >, Vector128<short >>(name, (x, y) => Simd.Insert(x, 8, (short )1));
                testThrowsArgumentOutOfRangeException<ushort, Vector128<ushort>, Vector128<ushort>>(name, (x, y) => Simd.Insert(x, 8, (ushort)1));
                testThrowsArgumentOutOfRangeException<int,    Vector128<int   >, Vector128<int   >>(name, (x, y) => Simd.Insert(x, 4, (int   )1));
                testThrowsArgumentOutOfRangeException<uint,   Vector128<uint  >, Vector128<uint  >>(name, (x, y) => Simd.Insert(x, 4, (uint  )1));
                testThrowsArgumentOutOfRangeException<long,   Vector128<long  >, Vector128<long  >>(name, (x, y) => Simd.Insert(x, 2, (long  )1));
                testThrowsArgumentOutOfRangeException<ulong,  Vector128<ulong >, Vector128<ulong >>(name, (x, y) => Simd.Insert(x, 2, (ulong )1));
                testThrowsArgumentOutOfRangeException<float,  Vector64< float >, Vector64< float >>(name, (x, y) => Simd.Insert(x, 2, (float )1));
                testThrowsArgumentOutOfRangeException<sbyte,  Vector64< sbyte >, Vector64< sbyte >>(name, (x, y) => Simd.Insert(x, 8, (sbyte )1));
                testThrowsArgumentOutOfRangeException<byte,   Vector64< byte  >, Vector64< byte  >>(name, (x, y) => Simd.Insert(x, 8, (byte  )1));
                testThrowsArgumentOutOfRangeException<short,  Vector64< short >, Vector64< short >>(name, (x, y) => Simd.Insert(x, 4, (short )1));
                testThrowsArgumentOutOfRangeException<ushort, Vector64< ushort>, Vector64< ushort>>(name, (x, y) => Simd.Insert(x, 4, (ushort)1));
                testThrowsArgumentOutOfRangeException<int,    Vector64< int   >, Vector64< int   >>(name, (x, y) => Simd.Insert(x, 2, (int   )1));
                testThrowsArgumentOutOfRangeException<uint,   Vector64< uint  >, Vector64< uint  >>(name, (x, y) => Simd.Insert(x, 2, (uint  )1));

                testThrowsTypeNotSupported<Vector128<bool >>(name, (x, y) => Simd.Insert(x, 1,      true));
                testThrowsTypeNotSupported<Vector64< long >>(name, (x, y) => Simd.Insert(x, 1, ( long )5));
                testThrowsTypeNotSupported<Vector64< ulong>>(name, (x, y) => Simd.Insert(x, 1, ( ulong)5));
                testThrowsTypeNotSupported<Vector64<double>>(name, (x, y) => Simd.Insert(x, 1, (double)5));
            }
            else
            {
                testThrowsPlatformNotSupported<Vector64< float >>(name, (x, y) => Simd.Insert(x, 1, (float )1));
                testThrowsPlatformNotSupported<Vector64< double>>(name, (x, y) => Simd.Insert(x, 1, (double)1));
                testThrowsPlatformNotSupported<Vector64< sbyte >>(name, (x, y) => Simd.Insert(x, 1, (sbyte )1));
                testThrowsPlatformNotSupported<Vector64< byte  >>(name, (x, y) => Simd.Insert(x, 1, (byte  )1));
                testThrowsPlatformNotSupported<Vector64< short >>(name, (x, y) => Simd.Insert(x, 1, (short )1));
                testThrowsPlatformNotSupported<Vector64< ushort>>(name, (x, y) => Simd.Insert(x, 1, (ushort)1));
                testThrowsPlatformNotSupported<Vector64< int   >>(name, (x, y) => Simd.Insert(x, 1, (int   )1));
                testThrowsPlatformNotSupported<Vector64< uint  >>(name, (x, y) => Simd.Insert(x, 1, (uint  )1));
                testThrowsPlatformNotSupported<Vector64< long  >>(name, (x, y) => Simd.Insert(x, 1, (long  )1));
                testThrowsPlatformNotSupported<Vector64< ulong >>(name, (x, y) => Simd.Insert(x, 1, (ulong )1));
                testThrowsPlatformNotSupported<Vector128<float >>(name, (x, y) => Simd.Insert(x, 1, (float )1));
                testThrowsPlatformNotSupported<Vector128<double>>(name, (x, y) => Simd.Insert(x, 1, (double)1));
                testThrowsPlatformNotSupported<Vector128<sbyte >>(name, (x, y) => Simd.Insert(x, 1, (sbyte )1));
                testThrowsPlatformNotSupported<Vector128<byte  >>(name, (x, y) => Simd.Insert(x, 1, (byte  )1));
                testThrowsPlatformNotSupported<Vector128<short >>(name, (x, y) => Simd.Insert(x, 1, (short )1));
                testThrowsPlatformNotSupported<Vector128<ushort>>(name, (x, y) => Simd.Insert(x, 1, (ushort)1));
                testThrowsPlatformNotSupported<Vector128<int   >>(name, (x, y) => Simd.Insert(x, 1, (int   )1));
                testThrowsPlatformNotSupported<Vector128<uint  >>(name, (x, y) => Simd.Insert(x, 1, (uint  )1));
                testThrowsPlatformNotSupported<Vector128<long  >>(name, (x, y) => Simd.Insert(x, 1, (long  )1));
                testThrowsPlatformNotSupported<Vector128<ulong >>(name, (x, y) => Simd.Insert(x, 1, (ulong )1));
            }

            Console.WriteLine($"Test{name} passed");
        }

        static void TestLeadingSignCount()
        {
            String name = "LeadingSignCount";

            if (Simd.IsSupported)
            {
                testBinOp<sbyte,  Vector128<sbyte >>(name, (x, y) => Simd.LeadingSignCount(x), (x, y) => leadingSign(x));
                testBinOp<short,  Vector128<short >>(name, (x, y) => Simd.LeadingSignCount(x), (x, y) => leadingSign(x));
                testBinOp<int,    Vector128<int   >>(name, (x, y) => Simd.LeadingSignCount(x), (x, y) => leadingSign(x));
                testBinOp<sbyte,  Vector64< sbyte >>(name, (x, y) => Simd.LeadingSignCount(x), (x, y) => leadingSign(x));
                testBinOp<short,  Vector64< short >>(name, (x, y) => Simd.LeadingSignCount(x), (x, y) => leadingSign(x));
                testBinOp<int,    Vector64< int   >>(name, (x, y) => Simd.LeadingSignCount(x), (x, y) => leadingSign(x));
            }
            else
            {
                testThrowsPlatformNotSupported<Vector64< sbyte >>(name, (x, y) => Simd.LeadingSignCount(x));
                testThrowsPlatformNotSupported<Vector64< short >>(name, (x, y) => Simd.LeadingSignCount(x));
                testThrowsPlatformNotSupported<Vector64< int   >>(name, (x, y) => Simd.LeadingSignCount(x));
                testThrowsPlatformNotSupported<Vector128<sbyte >>(name, (x, y) => Simd.LeadingSignCount(x));
                testThrowsPlatformNotSupported<Vector128<short >>(name, (x, y) => Simd.LeadingSignCount(x));
                testThrowsPlatformNotSupported<Vector128<int   >>(name, (x, y) => Simd.LeadingSignCount(x));
            }

            Console.WriteLine($"Test{name} passed");
        }

        static void TestLeadingZeroCount()
        {
            String name = "LeadingZeroCount";

            if (Simd.IsSupported)
            {
                testBinOp<sbyte,  Vector128<sbyte >>(name, (x, y) => Simd.LeadingZeroCount(x), (x, y) => leadingZero(x));
                testBinOp<byte,   Vector128<byte  >>(name, (x, y) => Simd.LeadingZeroCount(x), (x, y) => leadingZero(x));
                testBinOp<short,  Vector128<short >>(name, (x, y) => Simd.LeadingZeroCount(x), (x, y) => leadingZero(x));
                testBinOp<ushort, Vector128<ushort>>(name, (x, y) => Simd.LeadingZeroCount(x), (x, y) => leadingZero(x));
                testBinOp<int,    Vector128<int   >>(name, (x, y) => Simd.LeadingZeroCount(x), (x, y) => leadingZero(x));
                testBinOp<uint,   Vector128<uint  >>(name, (x, y) => Simd.LeadingZeroCount(x), (x, y) => leadingZero(x));
                testBinOp<sbyte,  Vector64< sbyte >>(name, (x, y) => Simd.LeadingZeroCount(x), (x, y) => leadingZero(x));
                testBinOp<byte,   Vector64< byte  >>(name, (x, y) => Simd.LeadingZeroCount(x), (x, y) => leadingZero(x));
                testBinOp<short,  Vector64< short >>(name, (x, y) => Simd.LeadingZeroCount(x), (x, y) => leadingZero(x));
                testBinOp<ushort, Vector64< ushort>>(name, (x, y) => Simd.LeadingZeroCount(x), (x, y) => leadingZero(x));
                testBinOp<int,    Vector64< int   >>(name, (x, y) => Simd.LeadingZeroCount(x), (x, y) => leadingZero(x));
                testBinOp<uint,   Vector64< uint  >>(name, (x, y) => Simd.LeadingZeroCount(x), (x, y) => leadingZero(x));
            }
            else
            {
                testThrowsPlatformNotSupported<Vector64< sbyte >>(name, (x, y) => Simd.LeadingZeroCount(x));
                testThrowsPlatformNotSupported<Vector64< byte  >>(name, (x, y) => Simd.LeadingZeroCount(x));
                testThrowsPlatformNotSupported<Vector64< short >>(name, (x, y) => Simd.LeadingZeroCount(x));
                testThrowsPlatformNotSupported<Vector64< ushort>>(name, (x, y) => Simd.LeadingZeroCount(x));
                testThrowsPlatformNotSupported<Vector64< int   >>(name, (x, y) => Simd.LeadingZeroCount(x));
                testThrowsPlatformNotSupported<Vector64< uint  >>(name, (x, y) => Simd.LeadingZeroCount(x));
                testThrowsPlatformNotSupported<Vector128<sbyte >>(name, (x, y) => Simd.LeadingZeroCount(x));
                testThrowsPlatformNotSupported<Vector128<byte  >>(name, (x, y) => Simd.LeadingZeroCount(x));
                testThrowsPlatformNotSupported<Vector128<short >>(name, (x, y) => Simd.LeadingZeroCount(x));
                testThrowsPlatformNotSupported<Vector128<ushort>>(name, (x, y) => Simd.LeadingZeroCount(x));
                testThrowsPlatformNotSupported<Vector128<int   >>(name, (x, y) => Simd.LeadingZeroCount(x));
                testThrowsPlatformNotSupported<Vector128<uint  >>(name, (x, y) => Simd.LeadingZeroCount(x));
            }

            Console.WriteLine($"Test{name} passed");
        }

        static void TestMax()
        {
            String name = "Max";

            if (Simd.IsSupported)
            {
                testBinOp<float,  Vector128<float >>(name, (x, y) => Simd.Max(x, y), (x, y) =>         ((x > y) ? x : y));
                testBinOp<double, Vector128<double>>(name, (x, y) => Simd.Max(x, y), (x, y) =>         ((x > y) ? x : y));
                testBinOp<sbyte,  Vector128<sbyte >>(name, (x, y) => Simd.Max(x, y), (x, y) => (sbyte) ((x > y) ? x : y));
                testBinOp<byte,   Vector128<byte  >>(name, (x, y) => Simd.Max(x, y), (x, y) => (byte)  ((x > y) ? x : y));
                testBinOp<short,  Vector128<short >>(name, (x, y) => Simd.Max(x, y), (x, y) => (short) ((x > y) ? x : y));
                testBinOp<ushort, Vector128<ushort>>(name, (x, y) => Simd.Max(x, y), (x, y) => (ushort)((x > y) ? x : y));
                testBinOp<int,    Vector128<int   >>(name, (x, y) => Simd.Max(x, y), (x, y) =>         ((x > y) ? x : y));
                testBinOp<uint,   Vector128<uint  >>(name, (x, y) => Simd.Max(x, y), (x, y) =>         ((x > y) ? x : y));
                testBinOp<float,  Vector64< float >>(name, (x, y) => Simd.Max(x, y), (x, y) =>         ((x > y) ? x : y));
                testBinOp<sbyte,  Vector64< sbyte >>(name, (x, y) => Simd.Max(x, y), (x, y) => (sbyte) ((x > y) ? x : y));
                testBinOp<byte,   Vector64< byte  >>(name, (x, y) => Simd.Max(x, y), (x, y) => (byte)  ((x > y) ? x : y));
                testBinOp<short,  Vector64< short >>(name, (x, y) => Simd.Max(x, y), (x, y) => (short) ((x > y) ? x : y));
                testBinOp<ushort, Vector64< ushort>>(name, (x, y) => Simd.Max(x, y), (x, y) => (ushort)((x > y) ? x : y));
                testBinOp<int,    Vector64< int   >>(name, (x, y) => Simd.Max(x, y), (x, y) =>         ((x > y) ? x : y));
                testBinOp<uint,   Vector64< uint  >>(name, (x, y) => Simd.Max(x, y), (x, y) =>         ((x > y) ? x : y));
            }
            else
            {
                testThrowsPlatformNotSupported<Vector64< float >>(name, (x, y) => Simd.Max(x, y));
                testThrowsPlatformNotSupported<Vector64< sbyte >>(name, (x, y) => Simd.Max(x, y));
                testThrowsPlatformNotSupported<Vector64< byte  >>(name, (x, y) => Simd.Max(x, y));
                testThrowsPlatformNotSupported<Vector64< short >>(name, (x, y) => Simd.Max(x, y));
                testThrowsPlatformNotSupported<Vector64< ushort>>(name, (x, y) => Simd.Max(x, y));
                testThrowsPlatformNotSupported<Vector64< int   >>(name, (x, y) => Simd.Max(x, y));
                testThrowsPlatformNotSupported<Vector64< uint  >>(name, (x, y) => Simd.Max(x, y));
                testThrowsPlatformNotSupported<Vector128<float >>(name, (x, y) => Simd.Max(x, y));
                testThrowsPlatformNotSupported<Vector128<double>>(name, (x, y) => Simd.Max(x, y));
                testThrowsPlatformNotSupported<Vector128<sbyte >>(name, (x, y) => Simd.Max(x, y));
                testThrowsPlatformNotSupported<Vector128<byte  >>(name, (x, y) => Simd.Max(x, y));
                testThrowsPlatformNotSupported<Vector128<short >>(name, (x, y) => Simd.Max(x, y));
                testThrowsPlatformNotSupported<Vector128<ushort>>(name, (x, y) => Simd.Max(x, y));
                testThrowsPlatformNotSupported<Vector128<int   >>(name, (x, y) => Simd.Max(x, y));
                testThrowsPlatformNotSupported<Vector128<uint  >>(name, (x, y) => Simd.Max(x, y));
            }

            Console.WriteLine($"Test{name} passed");
        }

        static void TestMin()
        {
            String name = "Min";

            if (Simd.IsSupported)
            {
                testBinOp<float,  Vector128<float >>(name, (x, y) => Simd.Min(x, y), (x, y) =>         ((x < y) ? x : y));
                testBinOp<double, Vector128<double>>(name, (x, y) => Simd.Min(x, y), (x, y) =>         ((x < y) ? x : y));
                testBinOp<sbyte,  Vector128<sbyte >>(name, (x, y) => Simd.Min(x, y), (x, y) => (sbyte) ((x < y) ? x : y));
                testBinOp<byte,   Vector128<byte  >>(name, (x, y) => Simd.Min(x, y), (x, y) => (byte)  ((x < y) ? x : y));
                testBinOp<short,  Vector128<short >>(name, (x, y) => Simd.Min(x, y), (x, y) => (short) ((x < y) ? x : y));
                testBinOp<ushort, Vector128<ushort>>(name, (x, y) => Simd.Min(x, y), (x, y) => (ushort)((x < y) ? x : y));
                testBinOp<int,    Vector128<int   >>(name, (x, y) => Simd.Min(x, y), (x, y) =>         ((x < y) ? x : y));
                testBinOp<uint,   Vector128<uint  >>(name, (x, y) => Simd.Min(x, y), (x, y) =>         ((x < y) ? x : y));
                testBinOp<float,  Vector64< float >>(name, (x, y) => Simd.Min(x, y), (x, y) =>         ((x < y) ? x : y));
                testBinOp<sbyte,  Vector64< sbyte >>(name, (x, y) => Simd.Min(x, y), (x, y) => (sbyte) ((x < y) ? x : y));
                testBinOp<byte,   Vector64< byte  >>(name, (x, y) => Simd.Min(x, y), (x, y) => (byte)  ((x < y) ? x : y));
                testBinOp<short,  Vector64< short >>(name, (x, y) => Simd.Min(x, y), (x, y) => (short) ((x < y) ? x : y));
                testBinOp<ushort, Vector64< ushort>>(name, (x, y) => Simd.Min(x, y), (x, y) => (ushort)((x < y) ? x : y));
                testBinOp<int,    Vector64< int   >>(name, (x, y) => Simd.Min(x, y), (x, y) =>         ((x < y) ? x : y));
                testBinOp<uint,   Vector64< uint  >>(name, (x, y) => Simd.Min(x, y), (x, y) =>         ((x < y) ? x : y));
            }
            else
            {
                testThrowsPlatformNotSupported<Vector64< float >>(name, (x, y) => Simd.Min(x, y));
                testThrowsPlatformNotSupported<Vector64< sbyte >>(name, (x, y) => Simd.Min(x, y));
                testThrowsPlatformNotSupported<Vector64< byte  >>(name, (x, y) => Simd.Min(x, y));
                testThrowsPlatformNotSupported<Vector64< short >>(name, (x, y) => Simd.Min(x, y));
                testThrowsPlatformNotSupported<Vector64< ushort>>(name, (x, y) => Simd.Min(x, y));
                testThrowsPlatformNotSupported<Vector64< int   >>(name, (x, y) => Simd.Min(x, y));
                testThrowsPlatformNotSupported<Vector64< uint  >>(name, (x, y) => Simd.Min(x, y));
                testThrowsPlatformNotSupported<Vector128<float >>(name, (x, y) => Simd.Min(x, y));
                testThrowsPlatformNotSupported<Vector128<double>>(name, (x, y) => Simd.Min(x, y));
                testThrowsPlatformNotSupported<Vector128<sbyte >>(name, (x, y) => Simd.Min(x, y));
                testThrowsPlatformNotSupported<Vector128<byte  >>(name, (x, y) => Simd.Min(x, y));
                testThrowsPlatformNotSupported<Vector128<short >>(name, (x, y) => Simd.Min(x, y));
                testThrowsPlatformNotSupported<Vector128<ushort>>(name, (x, y) => Simd.Min(x, y));
                testThrowsPlatformNotSupported<Vector128<int   >>(name, (x, y) => Simd.Min(x, y));
                testThrowsPlatformNotSupported<Vector128<uint  >>(name, (x, y) => Simd.Min(x, y));
            }

            Console.WriteLine($"Test{name} passed");
        }

        static void TestMultiply()
        {
            String name = "Multiply";

            if (Simd.IsSupported)
            {
                testBinOp<float,  Vector128<float >>(name, (x, y) => Simd.Multiply(x, y), (x, y) =>         (x * y));
                testBinOp<double, Vector128<double>>(name, (x, y) => Simd.Multiply(x, y), (x, y) =>         (x * y));
                testBinOp<sbyte,  Vector128<sbyte >>(name, (x, y) => Simd.Multiply(x, y), (x, y) => (sbyte) (x * y));
                testBinOp<byte,   Vector128<byte  >>(name, (x, y) => Simd.Multiply(x, y), (x, y) => (byte)  (x * y));
                testBinOp<short,  Vector128<short >>(name, (x, y) => Simd.Multiply(x, y), (x, y) => (short) (x * y));
                testBinOp<ushort, Vector128<ushort>>(name, (x, y) => Simd.Multiply(x, y), (x, y) => (ushort)(x * y));
                testBinOp<int,    Vector128<int   >>(name, (x, y) => Simd.Multiply(x, y), (x, y) =>         (x * y));
                testBinOp<uint,   Vector128<uint  >>(name, (x, y) => Simd.Multiply(x, y), (x, y) =>         (x * y));
                testBinOp<float,  Vector64< float >>(name, (x, y) => Simd.Multiply(x, y), (x, y) =>         (x * y));
                testBinOp<sbyte,  Vector64< sbyte >>(name, (x, y) => Simd.Multiply(x, y), (x, y) => (sbyte) (x * y));
                testBinOp<byte,   Vector64< byte  >>(name, (x, y) => Simd.Multiply(x, y), (x, y) => (byte)  (x * y));
                testBinOp<short,  Vector64< short >>(name, (x, y) => Simd.Multiply(x, y), (x, y) => (short) (x * y));
                testBinOp<ushort, Vector64< ushort>>(name, (x, y) => Simd.Multiply(x, y), (x, y) => (ushort)(x * y));
                testBinOp<int,    Vector64< int   >>(name, (x, y) => Simd.Multiply(x, y), (x, y) =>         (x * y));
                testBinOp<uint,   Vector64< uint  >>(name, (x, y) => Simd.Multiply(x, y), (x, y) =>         (x * y));
            }
            else
            {
                testThrowsPlatformNotSupported<Vector64< float >>(name, (x, y) => Simd.Multiply(x, y));
                testThrowsPlatformNotSupported<Vector64< sbyte >>(name, (x, y) => Simd.Multiply(x, y));
                testThrowsPlatformNotSupported<Vector64< byte  >>(name, (x, y) => Simd.Multiply(x, y));
                testThrowsPlatformNotSupported<Vector64< short >>(name, (x, y) => Simd.Multiply(x, y));
                testThrowsPlatformNotSupported<Vector64< ushort>>(name, (x, y) => Simd.Multiply(x, y));
                testThrowsPlatformNotSupported<Vector64< int   >>(name, (x, y) => Simd.Multiply(x, y));
                testThrowsPlatformNotSupported<Vector64< uint  >>(name, (x, y) => Simd.Multiply(x, y));
                testThrowsPlatformNotSupported<Vector128<float >>(name, (x, y) => Simd.Multiply(x, y));
                testThrowsPlatformNotSupported<Vector128<double>>(name, (x, y) => Simd.Multiply(x, y));
                testThrowsPlatformNotSupported<Vector128<sbyte >>(name, (x, y) => Simd.Multiply(x, y));
                testThrowsPlatformNotSupported<Vector128<byte  >>(name, (x, y) => Simd.Multiply(x, y));
                testThrowsPlatformNotSupported<Vector128<short >>(name, (x, y) => Simd.Multiply(x, y));
                testThrowsPlatformNotSupported<Vector128<ushort>>(name, (x, y) => Simd.Multiply(x, y));
                testThrowsPlatformNotSupported<Vector128<int   >>(name, (x, y) => Simd.Multiply(x, y));
                testThrowsPlatformNotSupported<Vector128<uint  >>(name, (x, y) => Simd.Multiply(x, y));
            }

            Console.WriteLine($"Test{name} passed");
        }

        static void TestNegate()
        {
            String name = "Negate";

            if (Simd.IsSupported)
            {
                testBinOp<float,  Vector128<float >>(name, (x, y) => Simd.Negate(x), (x, y) =>         (-x));
                testBinOp<double, Vector128<double>>(name, (x, y) => Simd.Negate(x), (x, y) =>         (-x));
                testBinOp<sbyte,  Vector128<sbyte >>(name, (x, y) => Simd.Negate(x), (x, y) => (sbyte) (-x));
                testBinOp<short,  Vector128<short >>(name, (x, y) => Simd.Negate(x), (x, y) => (short) (-x));
                testBinOp<int,    Vector128<int   >>(name, (x, y) => Simd.Negate(x), (x, y) =>         (-x));
                testBinOp<long,   Vector128<long  >>(name, (x, y) => Simd.Negate(x), (x, y) =>         (-x));
                testBinOp<float,  Vector64< float >>(name, (x, y) => Simd.Negate(x), (x, y) =>         (-x));
                testBinOp<sbyte,  Vector64< sbyte >>(name, (x, y) => Simd.Negate(x), (x, y) => (sbyte) (-x));
                testBinOp<short,  Vector64< short >>(name, (x, y) => Simd.Negate(x), (x, y) => (short) (-x));
                testBinOp<int,    Vector64< int   >>(name, (x, y) => Simd.Negate(x), (x, y) =>         (-x));
            }
            else
            {
                testThrowsPlatformNotSupported<Vector64< float >>(name, (x, y) => Simd.Negate(x));
                testThrowsPlatformNotSupported<Vector64< sbyte >>(name, (x, y) => Simd.Negate(x));
                testThrowsPlatformNotSupported<Vector64< short >>(name, (x, y) => Simd.Negate(x));
                testThrowsPlatformNotSupported<Vector64< int   >>(name, (x, y) => Simd.Negate(x));
                testThrowsPlatformNotSupported<Vector128<float >>(name, (x, y) => Simd.Negate(x));
                testThrowsPlatformNotSupported<Vector128<double>>(name, (x, y) => Simd.Negate(x));
                testThrowsPlatformNotSupported<Vector128<sbyte >>(name, (x, y) => Simd.Negate(x));
                testThrowsPlatformNotSupported<Vector128<short >>(name, (x, y) => Simd.Negate(x));
                testThrowsPlatformNotSupported<Vector128<int   >>(name, (x, y) => Simd.Negate(x));
                testThrowsPlatformNotSupported<Vector128<long  >>(name, (x, y) => Simd.Negate(x));
            }

            Console.WriteLine($"Test{name} passed");
        }

        static void TestNot()
        {
            String name = "Not";

            if (Simd.IsSupported)
            {
                testBinOp<float,  Vector128<float >>(name, (x, y) => Simd.Not(x), (x, y) => bitsToFloat (~bits(x)));
                testBinOp<double, Vector128<double>>(name, (x, y) => Simd.Not(x), (x, y) => bitsToDouble(~bits(x)));
                testBinOp<sbyte,  Vector128<sbyte >>(name, (x, y) => Simd.Not(x), (x, y) => (sbyte)     (~     x ));
                testBinOp<byte,   Vector128<byte  >>(name, (x, y) => Simd.Not(x), (x, y) => (byte)      (~     x ));
                testBinOp<short,  Vector128<short >>(name, (x, y) => Simd.Not(x), (x, y) => (short)     (~     x ));
                testBinOp<ushort, Vector128<ushort>>(name, (x, y) => Simd.Not(x), (x, y) => (ushort)    (~     x ));
                testBinOp<int,    Vector128<int   >>(name, (x, y) => Simd.Not(x), (x, y) =>             (~     x ));
                testBinOp<uint,   Vector128<uint  >>(name, (x, y) => Simd.Not(x), (x, y) =>             (~     x ));
                testBinOp<long,   Vector128<long  >>(name, (x, y) => Simd.Not(x), (x, y) =>             (~     x ));
                testBinOp<ulong,  Vector128<ulong >>(name, (x, y) => Simd.Not(x), (x, y) =>             (~     x ));
                testBinOp<float,  Vector64< float >>(name, (x, y) => Simd.Not(x), (x, y) => bitsToFloat (~bits(x)));
                testBinOp<sbyte,  Vector64< sbyte >>(name, (x, y) => Simd.Not(x), (x, y) => (sbyte)     (~     x ));
                testBinOp<byte,   Vector64< byte  >>(name, (x, y) => Simd.Not(x), (x, y) => (byte)      (~     x ));
                testBinOp<short,  Vector64< short >>(name, (x, y) => Simd.Not(x), (x, y) => (short)     (~     x ));
                testBinOp<ushort, Vector64< ushort>>(name, (x, y) => Simd.Not(x), (x, y) => (ushort)    (~     x ));
                testBinOp<int,    Vector64< int   >>(name, (x, y) => Simd.Not(x), (x, y) =>             (~     x ));
                testBinOp<uint,   Vector64< uint  >>(name, (x, y) => Simd.Not(x), (x, y) =>             (~     x ));

                testThrowsTypeNotSupported<Vector128<Vector128<long> >>(name, (x, y) => Simd.Not(x));

                testThrowsTypeNotSupported<Vector64< long  >>(name, (x, y) => Simd.Not(x));
                testThrowsTypeNotSupported<Vector64< ulong >>(name, (x, y) => Simd.Not(x));
                testThrowsTypeNotSupported<Vector64< double>>(name, (x, y) => Simd.Not(x));
            }
            else
            {
                testThrowsPlatformNotSupported<Vector64< float >>(name, (x, y) => Simd.Not(x));
                testThrowsPlatformNotSupported<Vector64< double>>(name, (x, y) => Simd.Not(x));
                testThrowsPlatformNotSupported<Vector64< sbyte >>(name, (x, y) => Simd.Not(x));
                testThrowsPlatformNotSupported<Vector64< byte  >>(name, (x, y) => Simd.Not(x));
                testThrowsPlatformNotSupported<Vector64< short >>(name, (x, y) => Simd.Not(x));
                testThrowsPlatformNotSupported<Vector64< ushort>>(name, (x, y) => Simd.Not(x));
                testThrowsPlatformNotSupported<Vector64< int   >>(name, (x, y) => Simd.Not(x));
                testThrowsPlatformNotSupported<Vector64< uint  >>(name, (x, y) => Simd.Not(x));
                testThrowsPlatformNotSupported<Vector64< long  >>(name, (x, y) => Simd.Not(x));
                testThrowsPlatformNotSupported<Vector64< ulong >>(name, (x, y) => Simd.Not(x));
                testThrowsPlatformNotSupported<Vector128<float >>(name, (x, y) => Simd.Not(x));
                testThrowsPlatformNotSupported<Vector128<double>>(name, (x, y) => Simd.Not(x));
                testThrowsPlatformNotSupported<Vector128<sbyte >>(name, (x, y) => Simd.Not(x));
                testThrowsPlatformNotSupported<Vector128<byte  >>(name, (x, y) => Simd.Not(x));
                testThrowsPlatformNotSupported<Vector128<short >>(name, (x, y) => Simd.Not(x));
                testThrowsPlatformNotSupported<Vector128<ushort>>(name, (x, y) => Simd.Not(x));
                testThrowsPlatformNotSupported<Vector128<int   >>(name, (x, y) => Simd.Not(x));
                testThrowsPlatformNotSupported<Vector128<uint  >>(name, (x, y) => Simd.Not(x));
                testThrowsPlatformNotSupported<Vector128<long  >>(name, (x, y) => Simd.Not(x));
                testThrowsPlatformNotSupported<Vector128<ulong >>(name, (x, y) => Simd.Not(x));
            }

            Console.WriteLine($"Test{name} passed");
        }

        static void TestOr()
        {
            String name = "Or";

            if (Simd.IsSupported)
            {
                testBinOp<float,  Vector128<float >>(name, (x, y) => Simd.Or(x, y), (x, y) => bitsToFloat (bits(x) | bits(y)));
                testBinOp<double, Vector128<double>>(name, (x, y) => Simd.Or(x, y), (x, y) => bitsToDouble(bits(x) | bits(y)));
                testBinOp<sbyte,  Vector128<sbyte >>(name, (x, y) => Simd.Or(x, y), (x, y) => (sbyte)     (     x  |      y ));
                testBinOp<byte,   Vector128<byte  >>(name, (x, y) => Simd.Or(x, y), (x, y) => (byte)      (     x  |      y ));
                testBinOp<short,  Vector128<short >>(name, (x, y) => Simd.Or(x, y), (x, y) => (short)     (     x  |      y ));
                testBinOp<ushort, Vector128<ushort>>(name, (x, y) => Simd.Or(x, y), (x, y) => (ushort)    (     x  |      y ));
                testBinOp<int,    Vector128<int   >>(name, (x, y) => Simd.Or(x, y), (x, y) =>             (     x  |      y ));
                testBinOp<uint,   Vector128<uint  >>(name, (x, y) => Simd.Or(x, y), (x, y) =>             (     x  |      y ));
                testBinOp<long,   Vector128<long  >>(name, (x, y) => Simd.Or(x, y), (x, y) =>             (     x  |      y ));
                testBinOp<ulong,  Vector128<ulong >>(name, (x, y) => Simd.Or(x, y), (x, y) =>             (     x  |      y ));
                testBinOp<float,  Vector64< float >>(name, (x, y) => Simd.Or(x, y), (x, y) => bitsToFloat (bits(x) | bits(y)));
                testBinOp<sbyte,  Vector64< sbyte >>(name, (x, y) => Simd.Or(x, y), (x, y) => (sbyte)     (     x  |      y ));
                testBinOp<byte,   Vector64< byte  >>(name, (x, y) => Simd.Or(x, y), (x, y) => (byte)      (     x  |      y ));
                testBinOp<short,  Vector64< short >>(name, (x, y) => Simd.Or(x, y), (x, y) => (short)     (     x  |      y ));
                testBinOp<ushort, Vector64< ushort>>(name, (x, y) => Simd.Or(x, y), (x, y) => (ushort)    (     x  |      y ));
                testBinOp<int,    Vector64< int   >>(name, (x, y) => Simd.Or(x, y), (x, y) =>             (     x  |      y ));
                testBinOp<uint,   Vector64< uint  >>(name, (x, y) => Simd.Or(x, y), (x, y) =>             (     x  |      y ));

                testThrowsTypeNotSupported<Vector128<Vector128<long> >>(name, (x, y) => Simd.Or(x, y));

                testThrowsTypeNotSupported<Vector64< long  >>(name, (x, y) => Simd.Or(x, y));
                testThrowsTypeNotSupported<Vector64< ulong >>(name, (x, y) => Simd.Or(x, y));
                testThrowsTypeNotSupported<Vector64< double>>(name, (x, y) => Simd.Or(x, y));
            }
            else
            {
                testThrowsPlatformNotSupported<Vector64< float >>(name, (x, y) => Simd.Or(x, y));
                testThrowsPlatformNotSupported<Vector64< double>>(name, (x, y) => Simd.Or(x, y));
                testThrowsPlatformNotSupported<Vector64< sbyte >>(name, (x, y) => Simd.Or(x, y));
                testThrowsPlatformNotSupported<Vector64< byte  >>(name, (x, y) => Simd.Or(x, y));
                testThrowsPlatformNotSupported<Vector64< short >>(name, (x, y) => Simd.Or(x, y));
                testThrowsPlatformNotSupported<Vector64< ushort>>(name, (x, y) => Simd.Or(x, y));
                testThrowsPlatformNotSupported<Vector64< int   >>(name, (x, y) => Simd.Or(x, y));
                testThrowsPlatformNotSupported<Vector64< uint  >>(name, (x, y) => Simd.Or(x, y));
                testThrowsPlatformNotSupported<Vector64< long  >>(name, (x, y) => Simd.Or(x, y));
                testThrowsPlatformNotSupported<Vector64< ulong >>(name, (x, y) => Simd.Or(x, y));
                testThrowsPlatformNotSupported<Vector128<float >>(name, (x, y) => Simd.Or(x, y));
                testThrowsPlatformNotSupported<Vector128<double>>(name, (x, y) => Simd.Or(x, y));
                testThrowsPlatformNotSupported<Vector128<sbyte >>(name, (x, y) => Simd.Or(x, y));
                testThrowsPlatformNotSupported<Vector128<byte  >>(name, (x, y) => Simd.Or(x, y));
                testThrowsPlatformNotSupported<Vector128<short >>(name, (x, y) => Simd.Or(x, y));
                testThrowsPlatformNotSupported<Vector128<ushort>>(name, (x, y) => Simd.Or(x, y));
                testThrowsPlatformNotSupported<Vector128<int   >>(name, (x, y) => Simd.Or(x, y));
                testThrowsPlatformNotSupported<Vector128<uint  >>(name, (x, y) => Simd.Or(x, y));
                testThrowsPlatformNotSupported<Vector128<long  >>(name, (x, y) => Simd.Or(x, y));
                testThrowsPlatformNotSupported<Vector128<ulong >>(name, (x, y) => Simd.Or(x, y));
            }

            Console.WriteLine($"Test{name} passed");
        }

        static void TestOrNot()
        {
            String name = "OrNot";

            if (Simd.IsSupported)
            {
                testBinOp<float,  Vector128<float >>(name, (x, y) => Simd.OrNot(x, y), (x, y) => bitsToFloat (bits(x) | ~bits(y)));
                testBinOp<double, Vector128<double>>(name, (x, y) => Simd.OrNot(x, y), (x, y) => bitsToDouble(bits(x) | ~bits(y)));
                testBinOp<sbyte,  Vector128<sbyte >>(name, (x, y) => Simd.OrNot(x, y), (x, y) => (sbyte)     (     x  | ~     y ));
                testBinOp<byte,   Vector128<byte  >>(name, (x, y) => Simd.OrNot(x, y), (x, y) => (byte)      (     x  | ~     y ));
                testBinOp<short,  Vector128<short >>(name, (x, y) => Simd.OrNot(x, y), (x, y) => (short)     (     x  | ~     y ));
                testBinOp<ushort, Vector128<ushort>>(name, (x, y) => Simd.OrNot(x, y), (x, y) => (ushort)    (     x  | ~     y ));
                testBinOp<int,    Vector128<int   >>(name, (x, y) => Simd.OrNot(x, y), (x, y) =>             (     x  | ~     y ));
                testBinOp<uint,   Vector128<uint  >>(name, (x, y) => Simd.OrNot(x, y), (x, y) =>             (     x  | ~     y ));
                testBinOp<long,   Vector128<long  >>(name, (x, y) => Simd.OrNot(x, y), (x, y) =>             (     x  | ~     y ));
                testBinOp<ulong,  Vector128<ulong >>(name, (x, y) => Simd.OrNot(x, y), (x, y) =>             (     x  | ~     y ));
                testBinOp<float,  Vector64< float >>(name, (x, y) => Simd.OrNot(x, y), (x, y) => bitsToFloat (bits(x) | ~bits(y)));
                testBinOp<sbyte,  Vector64< sbyte >>(name, (x, y) => Simd.OrNot(x, y), (x, y) => (sbyte)     (     x  | ~     y ));
                testBinOp<byte,   Vector64< byte  >>(name, (x, y) => Simd.OrNot(x, y), (x, y) => (byte)      (     x  | ~     y ));
                testBinOp<short,  Vector64< short >>(name, (x, y) => Simd.OrNot(x, y), (x, y) => (short)     (     x  | ~     y ));
                testBinOp<ushort, Vector64< ushort>>(name, (x, y) => Simd.OrNot(x, y), (x, y) => (ushort)    (     x  | ~     y ));
                testBinOp<int,    Vector64< int   >>(name, (x, y) => Simd.OrNot(x, y), (x, y) =>             (     x  | ~     y ));
                testBinOp<uint,   Vector64< uint  >>(name, (x, y) => Simd.OrNot(x, y), (x, y) =>             (     x  | ~     y ));

                testThrowsTypeNotSupported<Vector128<Vector128<long> >>(name, (x, y) => Simd.OrNot(x, y));

                testThrowsTypeNotSupported<Vector64< long  >>(name, (x, y) => Simd.OrNot(x, y));
                testThrowsTypeNotSupported<Vector64< ulong >>(name, (x, y) => Simd.OrNot(x, y));
                testThrowsTypeNotSupported<Vector64< double>>(name, (x, y) => Simd.OrNot(x, y));
            }
            else
            {
                testThrowsPlatformNotSupported<Vector64< float >>(name, (x, y) => Simd.OrNot(x, y));
                testThrowsPlatformNotSupported<Vector64< double>>(name, (x, y) => Simd.OrNot(x, y));
                testThrowsPlatformNotSupported<Vector64< sbyte >>(name, (x, y) => Simd.OrNot(x, y));
                testThrowsPlatformNotSupported<Vector64< byte  >>(name, (x, y) => Simd.OrNot(x, y));
                testThrowsPlatformNotSupported<Vector64< short >>(name, (x, y) => Simd.OrNot(x, y));
                testThrowsPlatformNotSupported<Vector64< ushort>>(name, (x, y) => Simd.OrNot(x, y));
                testThrowsPlatformNotSupported<Vector64< int   >>(name, (x, y) => Simd.OrNot(x, y));
                testThrowsPlatformNotSupported<Vector64< uint  >>(name, (x, y) => Simd.OrNot(x, y));
                testThrowsPlatformNotSupported<Vector64< long  >>(name, (x, y) => Simd.OrNot(x, y));
                testThrowsPlatformNotSupported<Vector64< ulong >>(name, (x, y) => Simd.OrNot(x, y));
                testThrowsPlatformNotSupported<Vector128<float >>(name, (x, y) => Simd.OrNot(x, y));
                testThrowsPlatformNotSupported<Vector128<double>>(name, (x, y) => Simd.OrNot(x, y));
                testThrowsPlatformNotSupported<Vector128<sbyte >>(name, (x, y) => Simd.OrNot(x, y));
                testThrowsPlatformNotSupported<Vector128<byte  >>(name, (x, y) => Simd.OrNot(x, y));
                testThrowsPlatformNotSupported<Vector128<short >>(name, (x, y) => Simd.OrNot(x, y));
                testThrowsPlatformNotSupported<Vector128<ushort>>(name, (x, y) => Simd.OrNot(x, y));
                testThrowsPlatformNotSupported<Vector128<int   >>(name, (x, y) => Simd.OrNot(x, y));
                testThrowsPlatformNotSupported<Vector128<uint  >>(name, (x, y) => Simd.OrNot(x, y));
                testThrowsPlatformNotSupported<Vector128<long  >>(name, (x, y) => Simd.OrNot(x, y));
                testThrowsPlatformNotSupported<Vector128<ulong >>(name, (x, y) => Simd.OrNot(x, y));
            }

            Console.WriteLine($"Test{name} passed");
        }

        static void TestPopCount()
        {
            String name = "PopCount";

            if (Simd.IsSupported)
            {
                testBinOp<sbyte,  Vector128<sbyte >>(name, (x, y) => Simd.PopCount(x), (x, y) => popCount(x));
                testBinOp<byte,   Vector128<byte  >>(name, (x, y) => Simd.PopCount(x), (x, y) => popCount(x));
                testBinOp<sbyte,  Vector64< sbyte >>(name, (x, y) => Simd.PopCount(x), (x, y) => popCount(x));
                testBinOp<byte,   Vector64< byte  >>(name, (x, y) => Simd.PopCount(x), (x, y) => popCount(x));
            }
            else
            {
                testThrowsPlatformNotSupported<Vector64< sbyte >>(name, (x, y) => Simd.PopCount(x));
                testThrowsPlatformNotSupported<Vector64< byte  >>(name, (x, y) => Simd.PopCount(x));
                testThrowsPlatformNotSupported<Vector128<sbyte >>(name, (x, y) => Simd.PopCount(x));
                testThrowsPlatformNotSupported<Vector128<byte  >>(name, (x, y) => Simd.PopCount(x));
            }

            Console.WriteLine($"Test{name} passed");
        }

        static void TestSetAllVector()
        {
            String name = "SetAllVector";

            if (Simd.IsSupported)
            {
                testBinOp<float,  Vector128<float >>(name, (x, y) => Simd.SetAllVector128((float )4), (x, y) => (float )4);
                testBinOp<double, Vector128<double>>(name, (x, y) => Simd.SetAllVector128((double)4), (x, y) => (double)4);
                testBinOp<sbyte,  Vector128<sbyte >>(name, (x, y) => Simd.SetAllVector128((sbyte )4), (x, y) => (sbyte )4);
                testBinOp<byte,   Vector128<byte  >>(name, (x, y) => Simd.SetAllVector128((byte  )4), (x, y) => (byte  )4);
                testBinOp<short,  Vector128<short >>(name, (x, y) => Simd.SetAllVector128((short )4), (x, y) => (short )4);
                testBinOp<ushort, Vector128<ushort>>(name, (x, y) => Simd.SetAllVector128((ushort)4), (x, y) => (ushort)4);
                testBinOp<int,    Vector128<int   >>(name, (x, y) => Simd.SetAllVector128((int   )4), (x, y) => (int   )4);
                testBinOp<uint,   Vector128<uint  >>(name, (x, y) => Simd.SetAllVector128((uint  )4), (x, y) => (uint  )4);
                testBinOp<long,   Vector128<long  >>(name, (x, y) => Simd.SetAllVector128((long  )4), (x, y) => (long  )4);
                testBinOp<ulong,  Vector128<ulong >>(name, (x, y) => Simd.SetAllVector128((ulong )4), (x, y) => (ulong )4);
                testBinOp<float,  Vector64< float >>(name, (x, y) => Simd.SetAllVector64( (float )4), (x, y) => (float )4);
                testBinOp<sbyte,  Vector64< sbyte >>(name, (x, y) => Simd.SetAllVector64( (sbyte )4), (x, y) => (sbyte )4);
                testBinOp<byte,   Vector64< byte  >>(name, (x, y) => Simd.SetAllVector64( (byte  )4), (x, y) => (byte  )4);
                testBinOp<short,  Vector64< short >>(name, (x, y) => Simd.SetAllVector64( (short )4), (x, y) => (short )4);
                testBinOp<ushort, Vector64< ushort>>(name, (x, y) => Simd.SetAllVector64( (ushort)4), (x, y) => (ushort)4);
                testBinOp<int,    Vector64< int   >>(name, (x, y) => Simd.SetAllVector64( (int   )4), (x, y) => (int   )4);
                testBinOp<uint,   Vector64< uint  >>(name, (x, y) => Simd.SetAllVector64( (uint  )4), (x, y) => (uint  )4);

                //testThrowsTypeNotSupported<Vector128<Vector128<long> >>(name, (x, y) => Simd.SetAllVector128(Simd.SetAllVector128((long  )5)));

                testThrowsTypeNotSupported<Vector64< long >>(name, (x, y) => Simd.SetAllVector64((long  )6));
                testThrowsTypeNotSupported<Vector64< ulong>>(name, (x, y) => Simd.SetAllVector64((ulong )6));
                testThrowsTypeNotSupported<Vector64<double>>(name, (x, y) => Simd.SetAllVector64((double)6));
            }
            else
            {
                testThrowsPlatformNotSupported<Vector64< float >>(name, (x, y) => Simd.SetAllVector64((float )7));
                testThrowsPlatformNotSupported<Vector64< sbyte >>(name, (x, y) => Simd.SetAllVector64((sbyte )7));
                testThrowsPlatformNotSupported<Vector64< byte  >>(name, (x, y) => Simd.SetAllVector64((byte  )7));
                testThrowsPlatformNotSupported<Vector64< short >>(name, (x, y) => Simd.SetAllVector64((short )7));
                testThrowsPlatformNotSupported<Vector64< ushort>>(name, (x, y) => Simd.SetAllVector64((ushort)7));
                testThrowsPlatformNotSupported<Vector64< int   >>(name, (x, y) => Simd.SetAllVector64((int   )7));
                testThrowsPlatformNotSupported<Vector64< uint  >>(name, (x, y) => Simd.SetAllVector64((uint  )7));
                testThrowsPlatformNotSupported<Vector64< long  >>(name, (x, y) => Simd.SetAllVector64((long  )7));
                testThrowsPlatformNotSupported<Vector64< ulong >>(name, (x, y) => Simd.SetAllVector64((ulong )7));
                testThrowsPlatformNotSupported<Vector64< double>>(name, (x, y) => Simd.SetAllVector64((double)7));

                testThrowsPlatformNotSupported<Vector128<float >>(name, (x, y) => Simd.SetAllVector128((float )8));
                testThrowsPlatformNotSupported<Vector128<double>>(name, (x, y) => Simd.SetAllVector128((double)8));
                testThrowsPlatformNotSupported<Vector128<sbyte >>(name, (x, y) => Simd.SetAllVector128((sbyte )8));
                testThrowsPlatformNotSupported<Vector128<byte  >>(name, (x, y) => Simd.SetAllVector128((byte  )8));
                testThrowsPlatformNotSupported<Vector128<short >>(name, (x, y) => Simd.SetAllVector128((short )8));
                testThrowsPlatformNotSupported<Vector128<ushort>>(name, (x, y) => Simd.SetAllVector128((ushort)8));
                testThrowsPlatformNotSupported<Vector128<int   >>(name, (x, y) => Simd.SetAllVector128((int   )8));
                testThrowsPlatformNotSupported<Vector128<uint  >>(name, (x, y) => Simd.SetAllVector128((uint  )8));
                testThrowsPlatformNotSupported<Vector128<long  >>(name, (x, y) => Simd.SetAllVector128((long  )8));
                testThrowsPlatformNotSupported<Vector128<ulong >>(name, (x, y) => Simd.SetAllVector128((ulong )8));
            }

            Console.WriteLine($"Test{name} passed");
        }

        static void TestSqrt()
        {
            String name = "Sqrt";

            if (Simd.IsSupported)
            {
                testBinOp<float,  Vector128<float >>(name, (x, y) => Simd.Sqrt(x), (x, y) =>         ((float) Math.Sqrt(x)));
                testBinOp<double, Vector128<double>>(name, (x, y) => Simd.Sqrt(x), (x, y) =>         (        Math.Sqrt(x)));
                testBinOp<float,  Vector64< float >>(name, (x, y) => Simd.Sqrt(x), (x, y) =>         ((float) Math.Sqrt(x)));
            }
            else
            {
                testThrowsPlatformNotSupported<Vector64< float >>(name, (x, y) => Simd.Sqrt(x));
                testThrowsPlatformNotSupported<Vector128<float >>(name, (x, y) => Simd.Sqrt(x));
                testThrowsPlatformNotSupported<Vector128<double>>(name, (x, y) => Simd.Sqrt(x));
            }

            Console.WriteLine($"Test{name} passed");
        }

        static void TestSubtract()
        {
            String name = "Subtract";

            if (Simd.IsSupported)
            {
                testBinOp<float,  Vector128<float >>(name, (x, y) => Simd.Subtract(x, y), (x, y) =>         (x - y));
                testBinOp<double, Vector128<double>>(name, (x, y) => Simd.Subtract(x, y), (x, y) =>         (x - y));
                testBinOp<sbyte,  Vector128<sbyte >>(name, (x, y) => Simd.Subtract(x, y), (x, y) => (sbyte) (x - y));
                testBinOp<byte,   Vector128<byte  >>(name, (x, y) => Simd.Subtract(x, y), (x, y) => (byte)  (x - y));
                testBinOp<short,  Vector128<short >>(name, (x, y) => Simd.Subtract(x, y), (x, y) => (short) (x - y));
                testBinOp<ushort, Vector128<ushort>>(name, (x, y) => Simd.Subtract(x, y), (x, y) => (ushort)(x - y));
                testBinOp<int,    Vector128<int   >>(name, (x, y) => Simd.Subtract(x, y), (x, y) =>         (x - y));
                testBinOp<uint,   Vector128<uint  >>(name, (x, y) => Simd.Subtract(x, y), (x, y) =>         (x - y));
                testBinOp<long,   Vector128<long  >>(name, (x, y) => Simd.Subtract(x, y), (x, y) =>         (x - y));
                testBinOp<ulong,  Vector128<ulong >>(name, (x, y) => Simd.Subtract(x, y), (x, y) =>         (x - y));
                testBinOp<float,  Vector64< float >>(name, (x, y) => Simd.Subtract(x, y), (x, y) =>         (x - y));
                testBinOp<sbyte,  Vector64< sbyte >>(name, (x, y) => Simd.Subtract(x, y), (x, y) => (sbyte) (x - y));
                testBinOp<byte,   Vector64< byte  >>(name, (x, y) => Simd.Subtract(x, y), (x, y) => (byte)  (x - y));
                testBinOp<short,  Vector64< short >>(name, (x, y) => Simd.Subtract(x, y), (x, y) => (short) (x - y));
                testBinOp<ushort, Vector64< ushort>>(name, (x, y) => Simd.Subtract(x, y), (x, y) => (ushort)(x - y));
                testBinOp<int,    Vector64< int   >>(name, (x, y) => Simd.Subtract(x, y), (x, y) =>         (x - y));
                testBinOp<uint,   Vector64< uint  >>(name, (x, y) => Simd.Subtract(x, y), (x, y) =>         (x - y));

                testThrowsTypeNotSupported<Vector128<Vector128<long> >>(name, (x, y) => Simd.Subtract(x, y));

                testThrowsTypeNotSupported<Vector64< long >>(name, (x, y) => Simd.Subtract(x, y));
                testThrowsTypeNotSupported<Vector64< ulong>>(name, (x, y) => Simd.Subtract(x, y));
                testThrowsTypeNotSupported<Vector64<double>>(name, (x, y) => Simd.Subtract(x, y));
            }
            else
            {
                testThrowsPlatformNotSupported<Vector64< float >>(name, (x, y) => Simd.Subtract(x, y));
                testThrowsPlatformNotSupported<Vector64< double>>(name, (x, y) => Simd.Subtract(x, y));
                testThrowsPlatformNotSupported<Vector64< sbyte >>(name, (x, y) => Simd.Subtract(x, y));
                testThrowsPlatformNotSupported<Vector64< byte  >>(name, (x, y) => Simd.Subtract(x, y));
                testThrowsPlatformNotSupported<Vector64< short >>(name, (x, y) => Simd.Subtract(x, y));
                testThrowsPlatformNotSupported<Vector64< ushort>>(name, (x, y) => Simd.Subtract(x, y));
                testThrowsPlatformNotSupported<Vector64< int   >>(name, (x, y) => Simd.Subtract(x, y));
                testThrowsPlatformNotSupported<Vector64< uint  >>(name, (x, y) => Simd.Subtract(x, y));
                testThrowsPlatformNotSupported<Vector64< long  >>(name, (x, y) => Simd.Subtract(x, y));
                testThrowsPlatformNotSupported<Vector64< ulong >>(name, (x, y) => Simd.Subtract(x, y));

                testThrowsPlatformNotSupported<Vector128<float >>(name, (x, y) => Simd.Subtract(x, y));
                testThrowsPlatformNotSupported<Vector128<double>>(name, (x, y) => Simd.Subtract(x, y));
                testThrowsPlatformNotSupported<Vector128<sbyte >>(name, (x, y) => Simd.Subtract(x, y));
                testThrowsPlatformNotSupported<Vector128<byte  >>(name, (x, y) => Simd.Subtract(x, y));
                testThrowsPlatformNotSupported<Vector128<short >>(name, (x, y) => Simd.Subtract(x, y));
                testThrowsPlatformNotSupported<Vector128<ushort>>(name, (x, y) => Simd.Subtract(x, y));
                testThrowsPlatformNotSupported<Vector128<int   >>(name, (x, y) => Simd.Subtract(x, y));
                testThrowsPlatformNotSupported<Vector128<uint  >>(name, (x, y) => Simd.Subtract(x, y));
                testThrowsPlatformNotSupported<Vector128<long  >>(name, (x, y) => Simd.Subtract(x, y));
                testThrowsPlatformNotSupported<Vector128<ulong >>(name, (x, y) => Simd.Subtract(x, y));
            }

            Console.WriteLine($"Test{name} passed");
        }

        static void TestXor()
        {
            String name = "Xor";

            if (Simd.IsSupported)
            {
                testBinOp<float,  Vector128<float >>(name, (x, y) => Simd.Xor(x, y), (x, y) => bitsToFloat (bits(x) ^ bits(y)));
                testBinOp<double, Vector128<double>>(name, (x, y) => Simd.Xor(x, y), (x, y) => bitsToDouble(bits(x) ^ bits(y)));
                testBinOp<sbyte,  Vector128<sbyte >>(name, (x, y) => Simd.Xor(x, y), (x, y) => (sbyte)     (     x  ^      y ));
                testBinOp<byte,   Vector128<byte  >>(name, (x, y) => Simd.Xor(x, y), (x, y) => (byte)      (     x  ^      y ));
                testBinOp<short,  Vector128<short >>(name, (x, y) => Simd.Xor(x, y), (x, y) => (short)     (     x  ^      y ));
                testBinOp<ushort, Vector128<ushort>>(name, (x, y) => Simd.Xor(x, y), (x, y) => (ushort)    (     x  ^      y ));
                testBinOp<int,    Vector128<int   >>(name, (x, y) => Simd.Xor(x, y), (x, y) =>             (     x  ^      y ));
                testBinOp<uint,   Vector128<uint  >>(name, (x, y) => Simd.Xor(x, y), (x, y) =>             (     x  ^      y ));
                testBinOp<long,   Vector128<long  >>(name, (x, y) => Simd.Xor(x, y), (x, y) =>             (     x  ^      y ));
                testBinOp<ulong,  Vector128<ulong >>(name, (x, y) => Simd.Xor(x, y), (x, y) =>             (     x  ^      y ));
                testBinOp<float,  Vector64< float >>(name, (x, y) => Simd.Xor(x, y), (x, y) => bitsToFloat (bits(x) ^ bits(y)));
                testBinOp<sbyte,  Vector64< sbyte >>(name, (x, y) => Simd.Xor(x, y), (x, y) => (sbyte)     (     x  ^      y ));
                testBinOp<byte,   Vector64< byte  >>(name, (x, y) => Simd.Xor(x, y), (x, y) => (byte)      (     x  ^      y ));
                testBinOp<short,  Vector64< short >>(name, (x, y) => Simd.Xor(x, y), (x, y) => (short)     (     x  ^      y ));
                testBinOp<ushort, Vector64< ushort>>(name, (x, y) => Simd.Xor(x, y), (x, y) => (ushort)    (     x  ^      y ));
                testBinOp<int,    Vector64< int   >>(name, (x, y) => Simd.Xor(x, y), (x, y) =>             (     x  ^      y ));
                testBinOp<uint,   Vector64< uint  >>(name, (x, y) => Simd.Xor(x, y), (x, y) =>             (     x  ^      y ));

                testThrowsTypeNotSupported<Vector128<Vector128<long> >>(name, (x, y) => Simd.Xor(x, y));

                testThrowsTypeNotSupported<Vector64< long  >>(name, (x, y) => Simd.Xor(x, y));
                testThrowsTypeNotSupported<Vector64< ulong >>(name, (x, y) => Simd.Xor(x, y));
                testThrowsTypeNotSupported<Vector64< double>>(name, (x, y) => Simd.Xor(x, y));
            }
            else
            {
                testThrowsPlatformNotSupported<Vector64< float >>(name, (x, y) => Simd.Xor(x, y));
                testThrowsPlatformNotSupported<Vector64< double>>(name, (x, y) => Simd.Xor(x, y));
                testThrowsPlatformNotSupported<Vector64< sbyte >>(name, (x, y) => Simd.Xor(x, y));
                testThrowsPlatformNotSupported<Vector64< byte  >>(name, (x, y) => Simd.Xor(x, y));
                testThrowsPlatformNotSupported<Vector64< short >>(name, (x, y) => Simd.Xor(x, y));
                testThrowsPlatformNotSupported<Vector64< ushort>>(name, (x, y) => Simd.Xor(x, y));
                testThrowsPlatformNotSupported<Vector64< int   >>(name, (x, y) => Simd.Xor(x, y));
                testThrowsPlatformNotSupported<Vector64< uint  >>(name, (x, y) => Simd.Xor(x, y));
                testThrowsPlatformNotSupported<Vector64< long  >>(name, (x, y) => Simd.Xor(x, y));
                testThrowsPlatformNotSupported<Vector64< ulong >>(name, (x, y) => Simd.Xor(x, y));
                testThrowsPlatformNotSupported<Vector128<float >>(name, (x, y) => Simd.Xor(x, y));
                testThrowsPlatformNotSupported<Vector128<double>>(name, (x, y) => Simd.Xor(x, y));
                testThrowsPlatformNotSupported<Vector128<sbyte >>(name, (x, y) => Simd.Xor(x, y));
                testThrowsPlatformNotSupported<Vector128<byte  >>(name, (x, y) => Simd.Xor(x, y));
                testThrowsPlatformNotSupported<Vector128<short >>(name, (x, y) => Simd.Xor(x, y));
                testThrowsPlatformNotSupported<Vector128<ushort>>(name, (x, y) => Simd.Xor(x, y));
                testThrowsPlatformNotSupported<Vector128<int   >>(name, (x, y) => Simd.Xor(x, y));
                testThrowsPlatformNotSupported<Vector128<uint  >>(name, (x, y) => Simd.Xor(x, y));
                testThrowsPlatformNotSupported<Vector128<long  >>(name, (x, y) => Simd.Xor(x, y));
                testThrowsPlatformNotSupported<Vector128<ulong >>(name, (x, y) => Simd.Xor(x, y));
            }

            Console.WriteLine($"Test{name} passed");
        }

        static void initializeDataSetDefault()
        {
            DataSet<float,  Vector64< float >>.setData(new float[]  { 1, -5 },                                                    new float[] { 22, -1 });
            DataSet<sbyte,  Vector64< sbyte >>.setData(new sbyte[]  { 1, -5, 100, 0, 7, 8, -2, -9 },                              new sbyte[] { 22, -1, -50, 0, 7, 5, 3, -33 });
            DataSet<byte,   Vector64< byte  >>.setData(new byte[]   { 1,  5, 100, 0, 7, 8,  2,  9 },                              new byte[]  { 22,  1,  50, 0, 7, 5, 3,  33 });
            DataSet<short,  Vector64< short >>.setData(new short[]  { 1, -5, 100, 0 },                                            new short[] { 22, -1, -50, 0 });
            DataSet<ushort, Vector64< ushort>>.setData(new ushort[] { 1,  5, 100, 0 },                                            new ushort[]{ 22,  1,  50, 0 });
            DataSet<int,    Vector64< int   >>.setData(new int[]    { 1, -5 },                                                    new int[]   { 22, -1 });
            DataSet<uint,   Vector64< uint  >>.setData(new uint[]   { 1,  5 },                                                    new uint[]  { 22,  1 });
            DataSet<float,  Vector128<float >>.setData(new float[]  { 1, -5, 100, 0 },                                            new float[] { 22, -1, -50, 0 });
            DataSet<double, Vector128<double>>.setData(new double[] { 1, -5 },                                                    new double[]{ 22, -1 });
            DataSet<sbyte,  Vector128<sbyte >>.setData(new sbyte[]  { 1, -5, 100, 0, 7, 8, -2, -9, 1, -5, 100, 0, 7, 8, -2, -9 }, new sbyte[] { 22, -1, -50, 0, 7, 5, 3, -33, -17, 4, 100, 120, 27, 6, -2, -6 });
            DataSet<byte,   Vector128<byte  >>.setData(new byte[]   { 1,  5, 100, 0, 7, 8,  2,  9, 1,  5, 100, 0, 7, 8,  2,  9 }, new byte[]  { 22,  1,  50, 0, 7, 5, 3,  33,  17, 4, 100, 120, 27, 6,  2,  6 });
            DataSet<short,  Vector128<short >>.setData(new short[]  { 1, -5, 100, 0, 7, 8, -2, -9 },                              new short[] { 22, -1, -50, 0, 7, 5, 3, -33 });
            DataSet<ushort, Vector128<ushort>>.setData(new ushort[] { 1,  5, 100, 0, 7, 8,  2,  9 },                              new ushort[]{ 22,  1,  50, 0, 7, 5, 3,  33 });
            DataSet<int,    Vector128<int   >>.setData(new int[]    { 1, -5, 100, 0 },                                            new int[]   { 22, -1, -50, 0 });
            DataSet<uint,   Vector128<uint  >>.setData(new uint[]   { 1,  5, 100, 0 },                                            new uint[]  { 22,  1,  50, 0 });
            DataSet<long,   Vector128<long  >>.setData(new long[]   { 1, -5 },                                                    new long[]  { 22, -1 });
            DataSet<ulong,  Vector128<ulong >>.setData(new ulong[]  { 1,  5 },                                                    new ulong[] { 22,  1 });

            Console.WriteLine("Using default data set");
        }

        static void initializeDataSetCompare()
        {
            DataSet<float,  Vector64< float >>.setData(new float[]  { 1, 0 },                                                    new float[] { 1, 17 });
            DataSet<sbyte,  Vector64< sbyte >>.setData(new sbyte[]  { 1, 0, 100, 0, 7, 8, -2, -9 },                              new sbyte[] { 1, 17, -50, 0, 7, 5, 3, -33 });
            DataSet<byte,   Vector64< byte  >>.setData(new byte[]   { 1, 0, 100, 0, 7, 8,  2,  9 },                              new byte[]  { 1, 17,  50, 0, 7, 5, 3,  33 });
            DataSet<short,  Vector64< short >>.setData(new short[]  { 1, 0, 100, 0 },                                            new short[] { 1, 17, -50, 0 });
            DataSet<ushort, Vector64< ushort>>.setData(new ushort[] { 1, 0, 100, 0 },                                            new ushort[]{ 1, 17,  50, 0 });
            DataSet<int,    Vector64< int   >>.setData(new int[]    { 1, 0 },                                                    new int[]   { 1, 17 });
            DataSet<uint,   Vector64< uint  >>.setData(new uint[]   { 1, 0 },                                                    new uint[]  { 1, 17 });
            DataSet<float,  Vector128<float >>.setData(new float[]  { 1, 0, 100, 0 },                                            new float[] { 1, 17, -50, 0 });
            DataSet<double, Vector128<double>>.setData(new double[] { 1, 0 },                                                    new double[]{ 1, 17 });
            DataSet<sbyte,  Vector128<sbyte >>.setData(new sbyte[]  { 1, 0, 100, 0, 7, 8, -2, -9, 1, -5, 100, 0, 7, 8, -2, -9 }, new sbyte[] { 1, 17, -50, 0, 7, 5, 3, -33, -17, 4, 100, 120, 27, 6, -2, -6 });
            DataSet<byte,   Vector128<byte  >>.setData(new byte[]   { 1, 0, 100, 0, 7, 8,  2,  9, 1,  5, 100, 0, 7, 8,  2,  9 }, new byte[]  { 1, 17,  50, 0, 7, 5, 3,  33,  17, 4, 100, 120, 27, 6,  2,  6 });
            DataSet<short,  Vector128<short >>.setData(new short[]  { 1, 0, 100, 0, 7, 8, -2, -9 },                              new short[] { 1, 17, -50, 0, 7, 5, 3, -33 });
            DataSet<ushort, Vector128<ushort>>.setData(new ushort[] { 1, 0, 100, 0, 7, 8,  2,  9 },                              new ushort[]{ 1, 17,  50, 0, 7, 5, 3,  33 });
            DataSet<int,    Vector128<int   >>.setData(new int[]    { 1, 0, 100, 0 },                                            new int[]   { 1, 17, -50, 0 });
            DataSet<uint,   Vector128<uint  >>.setData(new uint[]   { 1, 0, 100, 0 },                                            new uint[]  { 1, 17,  50, 0 });
            DataSet<long,   Vector128<long  >>.setData(new long[]   { 1, 0 },                                                    new long[]  { 1, 17 });
            DataSet<ulong,  Vector128<ulong >>.setData(new ulong[]  { 1, 0 },                                                    new ulong[] { 1, 17 });

            Console.WriteLine("Using compare data set");
        }

        static void ExecuteAllTests()
        {
            TestAbs();
            TestAdd();
            TestAnd();
            TestAndNot();
            TestBitwiseSelect();
            TestCompareEqual();
            TestCompareEqualZero();
            TestCompareGreaterThan();
            TestCompareGreaterThanZero();
            TestCompareGreaterThanOrEqual();
            TestCompareGreaterThanOrEqualZero();
            TestCompareLessThanZero();
            TestCompareLessThanOrEqualZero();
            TestCompareTest();
            TestDivide();
            TestExtract();
            TestInsert();
            TestLeadingSignCount();
            TestLeadingZeroCount();
            TestMax();
            TestMin();
            TestMultiply();
            TestNegate();
            TestNot();
            TestOr();
            TestOrNot();
            TestPopCount();
            TestSetAllVector();
            TestSqrt();
            TestSubtract();
            TestXor();
        }

        static int Main(string[] args)
        {
            Console.WriteLine($"System.Runtime.Intrinsics.Arm.Arm64.Simd.IsSupported = {Simd.IsSupported}");

            // Reflection call
            var issupported = "get_IsSupported";
            bool reflectedIsSupported = Convert.ToBoolean(typeof(Simd).GetMethod(issupported).Invoke(null, null));

            Debug.Assert(reflectedIsSupported == Simd.IsSupported, "Reflection result does not match");

            initializeDataSetDefault();

            ExecuteAllTests();

            initializeDataSetCompare();

            ExecuteAllTests();

            return 100;
        }
    }
}
