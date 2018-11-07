using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm.Arm64;

namespace Arm64intrisicsTest
{

    class Program
    {

        struct DataSet<TBaseType, TVectorType>
            where TBaseType : struct
            where TVectorType : new()
        {
            private static TVectorType _vectorX;
            private static TVectorType _vectorY;
            private static TVectorType _vectorZ;

            public static TVectorType vectorX { get { return _vectorX; }}
            public static TVectorType vectorY { get { return _vectorY; }}
            public static TVectorType vectorZ { get { return _vectorZ; }}

            public static TBaseType[] arrayX { get; private set; }
            public static TBaseType[] arrayY { get; private set; }
            public static TBaseType[] arrayZ { get; private set; }

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

            public static unsafe void setData(TBaseType[] x, TBaseType[] y, TBaseType[] z)
            {
                setData(x, y);
                arrayZ = z;

                GCHandle handleSrc = GCHandle.Alloc(z, GCHandleType.Pinned);

                try
                {
                    var ptrSrc = (byte*) handleSrc.AddrOfPinnedObject().ToPointer();

                    _vectorZ = Unsafe.Read<TVectorType>(ptrSrc);
                }
                finally
                {
                    handleSrc.Free();
                }

            }

        }

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

        static void testCryptoOp<TBaseType, TVectorType, TBaseReturnType, TVectorReturnType>(String testCaseDescription,
                                                      Func<TVectorType, TVectorType, TVectorType, TVectorReturnType> cryptoOp,
                                                      TBaseType[] check)
            where TBaseType : struct, IComparable
            where TVectorType : new()
            where TBaseReturnType : struct, IComparable
            where TVectorReturnType : new()
        {
            bool failed = false;
            try
            {
                var vX = DataSet<TBaseType, TVectorType>.vectorX;
                var vY = DataSet<TBaseType, TVectorType>.vectorY;
                var vZ = DataSet<TBaseType, TVectorType>.vectorZ;
                var vResult = cryptoOp(vX, vY, vZ);

                var result = writeVector<TBaseReturnType, TVectorReturnType>(vResult);
                //Console.WriteLine("res [{0}]", string.Join(", ", result));


                for (int i = 0; i < result.Length; i++)
                {

                    var expected = check[i];

                    if (result[i].CompareTo(expected) != 0)
                    {
                        if(!failed)
                        {
                            Console.WriteLine($"testCryptoOp<{typeof(TBaseType).Name}, {typeof(TVectorType).Name} >{testCaseDescription}: Check Failed");
                        }
                        Console.WriteLine($"check[{i}] : result[{i}] = {result[i]}, expected {expected}");
                        failed = true;
                    }
                }
            }
            catch
            {
                Console.WriteLine($"testCryptoOp<{typeof(TBaseType).Name}, {typeof(TVectorType).Name} >{testCaseDescription}: Unexpected exception");
                throw;
            }

            if (failed)
            {
                throw new Exception($"testCryptoOp<{typeof(TBaseType).Name}, {typeof(TVectorType).Name} >{testCaseDescription}: Failed");
            }
            else
            {
                Console.WriteLine($"testCryptoOp<{typeof(TBaseType).Name}, {typeof(TVectorType).Name} >{testCaseDescription}: Check Passed");
            }
        }

        static void testThrowsTypeNotSupported<TVectorType>(String testCaseDescription,
                                                                Func<TVectorType, TVectorType, TVectorType, TVectorType> cryptoOp)
            where TVectorType : new()
        {
            TVectorType v = new TVectorType();

            bool notSupported = false;

            try
            {
                cryptoOp(v,v,v);
            }
            catch (PlatformNotSupportedException)
            {
                notSupported = true;
            }
            finally
            {
                Debug.Assert(notSupported, $"{typeof(TVectorType).Name} {testCaseDescription}: Failed to throw PlatformNotSupportedException");
            }
        }

        static void testThrowsPlatformNotSupported<TVectorType>(String testCaseDescription,
                                                                Func<TVectorType, TVectorType, TVectorType, TVectorType> cryptoOp)
            where TVectorType : new()
        {
            testThrowsPlatformNotSupported<TVectorType, TVectorType>(testCaseDescription, cryptoOp);
        }

        static void testThrowsPlatformNotSupported<TVectorType, TVectorTypeReturn>(String testCaseDescription,
                                                                Func<TVectorType, TVectorType, TVectorType, TVectorTypeReturn> cryptoOp)
            where TVectorType : new()
        {
            bool notSupported = false;

            try
            {
                TVectorType v = new TVectorType();
                cryptoOp(v,v,v);
            }
            catch (PlatformNotSupportedException) // TODO-Fixme check for Type not supported exception
            {
                notSupported = true;
            }
            finally
            {
                Debug.Assert(notSupported, $"{typeof(TVectorType).Name} {testCaseDescription}: Failed to throw TypeNotSupportedException");
            }
        }


        static void TestAes()
        {
            String name = "Aes";

            if (Aes.IsSupported)
            {
                testCryptoOp<byte,  Vector128<byte>, byte, Vector128<byte> >(name, (x, y, z) => Aes.Encrypt(x, y), aesEncRes);
                testCryptoOp<byte,  Vector128<byte>, byte, Vector128<byte> >(name, (x, y, z) => Aes.Decrypt(x, y), aesDecRes);
                testCryptoOp<byte,  Vector128<byte>, byte, Vector128<byte> >(name, (x, y, z) => Aes.MixColumns(x), aesMixRes );
                testCryptoOp<byte,  Vector128<byte>, byte, Vector128<byte> >(name, (x, y, z) => Aes.InverseMixColumns(x), aesInvMixRes );

            }
            else
            {
                testThrowsPlatformNotSupported<Vector128<byte> , Vector128<byte>  >(name, (x, y, z) => Aes.Encrypt(x,y));
                testThrowsPlatformNotSupported<Vector128<byte> , Vector128<byte>  >(name, (x, y, z) => Aes.Decrypt(x,y));
                testThrowsPlatformNotSupported<Vector128<byte> , Vector128<byte>  >(name, (x, y, z) => Aes.MixColumns(x));
                testThrowsPlatformNotSupported<Vector128<byte> , Vector128<byte>  >(name, (x, y, z) => Aes.InverseMixColumns(x));
            }
        }

        static void TestSha256()
        {
            String name = "Sha256";
            if (Sha256.IsSupported)
            {
                testCryptoOp<uint,  Vector128<uint>, uint, Vector128<uint> >(name, (x, y, z) => Sha256.HashLower(x, y, z), sha256low);
                testCryptoOp<uint,  Vector128<uint>, uint, Vector128<uint> >(name, (x, y, z) => Sha256.HashUpper(x, y, z), sha256high);
                testCryptoOp<uint,  Vector128<uint>, uint, Vector128<uint> >(name, (x, y, z) => Sha256.SchedulePart1(x, y), sha256su1Res);
                testCryptoOp<uint,  Vector128<uint>, uint, Vector128<uint> >(name, (x, y, z) => Sha256.SchedulePart2(x, y, z), sha256su2Res);
            }
            else
            {
                testThrowsPlatformNotSupported<Vector128<uint>, Vector128<uint> >(name, (x, y, z) => Sha256.HashLower(x, y, z));
                testThrowsPlatformNotSupported<Vector128<uint>, Vector128<uint> >(name, (x, y, z) => Sha256.HashUpper(x, y, z));
                testThrowsPlatformNotSupported<Vector128<uint>, Vector128<uint> >(name, (x, y, z) => Sha256.SchedulePart1(x, y));
                testThrowsPlatformNotSupported<Vector128<uint>, Vector128<uint> >(name, (x, y, z) => Sha256.SchedulePart2(x, y, z));
            }
        }

        static void TestSha1()
        {
            String name = "Sha1";
            if (Sha1.IsSupported)
            {
                testCryptoOp<uint,  Vector128<uint>, uint, Vector128<uint> >(name, (x, y, z) => Sha1.HashChoose(x, 20, y), sha1cRes);
                testCryptoOp<uint,  Vector128<uint>, uint, Vector128<uint> >(name, (x, y, z) => Sha1.HashParity(x, 20, y), sha1pRes);
                testCryptoOp<uint,  Vector128<uint>, uint, Vector128<uint> >(name, (x, y, z) => Sha1.HashMajority(x, 20, y), sha1mRes);
                testCryptoOp<uint,  Vector128<uint>, uint, Vector128<uint> >(name, (x, y, z) => Sha1.SchedulePart1(x, y, z), sha1su1Res);
                testCryptoOp<uint,  Vector128<uint>, uint, Vector128<uint> >(name, (x, y, z) => Sha1.SchedulePart2(x, y), sha1su2Res);
                if(Sha1.FixedRotate(100) != 25)
                    throw new Exception("Sha1 FixedRotate failed.\n");
            }
            else
            {
                testThrowsPlatformNotSupported<Vector128<uint> , Vector128<uint>  >(name, (x, y, z) => Sha1.HashChoose(x, 20, y));
                testThrowsPlatformNotSupported<Vector128<uint> , Vector128<uint>  >(name, (x, y, z) => Sha1.HashParity(x, 20, y));
                testThrowsPlatformNotSupported<Vector128<uint> , Vector128<uint>  >(name, (x, y, z) => Sha1.HashMajority(x, 20, y));
                testThrowsPlatformNotSupported<Vector128<uint> , Vector128<uint>  >(name, (x, y, z) => Sha1.SchedulePart1(x, y, z));
                testThrowsPlatformNotSupported<Vector128<uint> , Vector128<uint>  >(name, (x, y, z) => Sha1.SchedulePart2(x, y));
            }
        }

        static void initializeDataSetDefault()
        {
            /// Data sets
            DataSet<byte, Vector128<byte> >.setData(new byte[]   { 1,  5, 100, 0, 7, 8,  2,  9, 1,  5, 100, 0, 7, 8,  2,  9 },
                                               new byte[]  { 22,  1,  50, 0, 7, 5, 3,  33,  17, 4, 100, 120, 27, 6,  2,  6 },
                                               new byte[]  { 1,  5,  10, 0, 17, 23, 14,  33,  15, 40, 0, 20, 22, 55,  12,  5 });
            DataSet<uint, Vector128<uint> >.setData(new uint[] {10, 44, 11, 81}, new uint[] {20, 41, 67, 59}, new uint[] {10, 20, 51, 96});
        }

        // Below result values are obtained by executing the corresponding GCC arm64 crypto intrinsics (defined in arm_neon.h)
        // with the same input dataset on ARM64 platform.

        static byte[] aesEncRes = new byte[] {240, 215, 99, 118, 99, 124, 99, 99, 202, 171, 177, 52, 156, 242, 124, 188};
        static byte[] aesDecRes = new byte[] {135, 215, 82, 238, 82, 48, 82, 193, 124, 243, 185, 251, 196, 09, 09, 82};
        static byte[] aesMixRes = new byte[] {105, 167, 204, 98, 29, 24, 16, 17, 105, 167, 204, 98, 29, 24, 16, 17};
        static byte[] aesInvMixRes = new byte[] {203, 158, 110, 91, 41, 60, 36, 53, 203, 158, 110, 91, 41, 60, 36, 53};
        static uint[] sha1cRes = new uint[] {2162335592, 464120, 1073745449, 1073741936};
        static uint[] sha1pRes = new uint[] {15831335, 2147977893, 3857, 2147483767};
        static uint[] sha1mRes = new uint[] {12230250, 382193, 1073744809, 1073741916};
        static uint[] sha1su1Res = new uint[] {11, 105, 44, 24};
        static uint[] sha1su2Res = new uint[] {70,222,96,46};
        static uint[] sha256low = new uint[] {3870443882, 98061066, 1597900421, 3536859796};
        static uint[] sha256high = new uint[] {2024066181, 3259295072, 1866655758, 692061599};
        static uint[] sha256su1Res = new uint[] {1477115919, 369279021, 2719236117, 671416403};
        static uint[] sha256su2Res = new uint[] {2089011, 3932271, 203417658, 2151313268};


        static void ExecuteAllTests()
        {
            TestAes();
            TestSha1();
            TestSha256();
        }

        static int Main(string[] args)
        {
            Console.WriteLine($"System.Runtime.Intrinsics.Arm.Arm64.Aes.IsSupported = {Aes.IsSupported}");
            Console.WriteLine($"System.Runtime.Intrinsics.Arm.Arm64.Sha1.IsSupported = {Sha1.IsSupported}");
            Console.WriteLine($"System.Runtime.Intrinsics.Arm.Arm64.Sha2.IsSupported = {Sha256.IsSupported}");
            initializeDataSetDefault();
            Console.WriteLine("Running tests");
            ExecuteAllTests();

            return 100;
        }
    }
}
