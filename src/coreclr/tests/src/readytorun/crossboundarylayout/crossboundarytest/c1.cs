// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace CrossBoundaryLayout
{
    public class A1BoringGeneric<T>
    {
        public byte _aVal;
    }

    public class B1_A : A
    {
        public byte _bVal;
    }

    public class B1_A_byte : AGeneric<byte>
    {
        public byte _bVal;
    }

    public class B1_A_D : AGeneric<ByteStruct>
    {
        public byte _bVal;
    }

    public class B1_A_Generic<T> : A
    {
        public T _bVal;
    }

    public class B1_A_byte_Generic<T> : AGeneric<byte>
    {
        public T _bVal;
    }

    public class B1_A_D_Generic<T> : AGeneric<ByteStruct>
    {
        public T _bVal;
    }

    public class B1_ABoring_byte : ABoringGeneric<byte>
    {
        public byte _bVal;
    }

    public class B1_ABoring_D : ABoringGeneric<ByteStruct>
    {
        public byte _bVal;
    }

    public class B1_A1Boring_byte : A1BoringGeneric<byte>
    {
        public byte _bVal;
    }

    public class B1_A1Boring_D : A1BoringGeneric<ByteStruct>
    {
        public byte _bVal;
    }

    class C1_B_A : B1_A
    {
        public byte _cVal;
    }

    class C1_B_A_byte : B1_A_byte
    {
        public byte _cVal;
    }

    class C1_B_A_D : B1_A_D
    {
        public byte _cVal;
    }

    class C1_B_A_Generic_byte : B1_A_Generic<byte>
    {
        public byte _cVal;
    }

    class C1_B_A_Generic_D : B1_A_Generic<ByteStruct>
    {
        public byte _cVal;
    }

    class C1_B_A_byte_Generic_byte : B1_A_byte_Generic<byte>
    {
        public byte _cVal;
    }

    class C1_B_A_byte_Generic_D : B1_A_byte_Generic<ByteStruct>
    {
        public byte _cVal;
    }

    class C1_B_A_D_Generic_byte : B1_A_D_Generic<byte>
    {
        public byte _cVal;
    }

    class C1_B_A_D_Generic_D : B1_A_D_Generic<ByteStruct>
    {
        public byte _cVal;
    }

    public class C1Test
    {
        public static int Test()
        {
            int failure = 0;
            {
                var a = (A)Activator.CreateInstance(typeof(A));
                a._aVal = 1;
                if (1 != (byte)typeof(A).GetField("_aVal").GetValue(a))
                {
                    failure++;
                    Console.WriteLine("C1 a._aVal");
                }
            }

            {
                var a2 = (AGeneric<byte>)Activator.CreateInstance(typeof(AGeneric<byte>));
                a2._aVal = 1;
                if (1 != (byte)typeof(AGeneric<byte>).GetField("_aVal").GetValue(a2))
                {
                    failure++;
                    Console.WriteLine("C1 a2_aVal");
                }
            }

            {
                var a3 = (AGeneric<ByteStruct>)Activator.CreateInstance(typeof(AGeneric<ByteStruct>));
                a3._aVal._dVal = 1;
                if (1 != ((ByteStruct)typeof(AGeneric<ByteStruct>).GetField("_aVal").GetValue(a3))._dVal)
                {
                    failure++;
                    Console.WriteLine("C1 a3_aVal");
                }
            }

            {
                var a4 = (ABoringGeneric<byte>)Activator.CreateInstance(typeof(ABoringGeneric<byte>));
                a4._aVal = 1;
                if (1 != (byte)typeof(ABoringGeneric<byte>).GetField("_aVal").GetValue(a4))
                {
                    failure++;
                    Console.WriteLine("C1 a4_aVal");
                }
            }

            {
                var a5 = (ABoringGeneric<ByteStruct>)Activator.CreateInstance(typeof(ABoringGeneric<ByteStruct>));
                a5._aVal = 1;
                if (1 != (byte)typeof(ABoringGeneric<ByteStruct>).GetField("_aVal").GetValue(a5))
                {
                    failure++;
                    Console.WriteLine("C1 a5_aVal");
                }
            }

            {
                var a6 = (A1BoringGeneric<byte>)Activator.CreateInstance(typeof(A1BoringGeneric<byte>));
                a6._aVal = 1;
                if (1 != (byte)typeof(A1BoringGeneric<byte>).GetField("_aVal").GetValue(a6))
                {
                    failure++;
                    Console.WriteLine("C1 a6_aVal");
                }
            }

            {
                var a7 = (A1BoringGeneric<ByteStruct>)Activator.CreateInstance(typeof(A1BoringGeneric<ByteStruct>));
                a7._aVal = 1;
                if (1 != (byte)typeof(A1BoringGeneric<ByteStruct>).GetField("_aVal").GetValue(a7))
                {
                    failure++;
                    Console.WriteLine("C1 a7_aVal");
                }
            }

            {
                var b = (B1_A)Activator.CreateInstance(typeof(B1_A));
                b._bVal = 1;
                if (1 != (byte)typeof(B1_A).GetField("_bVal").GetValue(b))
                {
                    failure++;
                    Console.WriteLine("C1 b._bVal");
                }
            }

            {
                var b2 = (B1_A_byte)Activator.CreateInstance(typeof(B1_A_byte));
                b2._bVal = 1;
                if (1 != (byte)typeof(B1_A_byte).GetField("_bVal").GetValue(b2))
                {
                    failure++;
                    Console.WriteLine("C1 b2._bVal");
                }
            }

            {
                var b3 = (B1_A_D)Activator.CreateInstance(typeof(B1_A_D));
                b3._bVal = 1;
                if (1 != (byte)typeof(B1_A_D).GetField("_bVal").GetValue(b3))
                {
                    failure++;
                    Console.WriteLine("C1 b3._bVal");
                }
            }

            {
                var b4 = (B1_A_Generic<byte>)Activator.CreateInstance(typeof(B1_A_Generic<byte>));
                b4._bVal = 1;
                if (1 != (byte)typeof(B1_A_Generic<byte>).GetField("_bVal").GetValue(b4))
                {
                    failure++;
                    Console.WriteLine("C1 b4._bVal");
                }
            }

            {
                var b5 = (B1_A_byte_Generic<byte>)Activator.CreateInstance(typeof(B1_A_byte_Generic<byte>));
                b5._bVal = 1;
                if (1 != (byte)typeof(B1_A_byte_Generic<byte>).GetField("_bVal").GetValue(b5))
                {
                    failure++;
                    Console.WriteLine("C1 b5._bVal");
                }
            }

            {
                var b6 = (B1_A_D_Generic<byte>)Activator.CreateInstance(typeof(B1_A_D_Generic<byte>));
                b6._bVal = 1;
                if (1 != (byte)typeof(B1_A_D_Generic<byte>).GetField("_bVal").GetValue(b6))
                {
                    failure++;
                    Console.WriteLine("C1 b6._bVal");
                }
            }

            {
                var b7 = (B1_A_Generic<ByteStruct>)Activator.CreateInstance(typeof(B1_A_Generic<ByteStruct>));
                b7._bVal._dVal = 1;
                if (1 != ((ByteStruct)typeof(B1_A_Generic<ByteStruct>).GetField("_bVal").GetValue(b7))._dVal)
                {
                    failure++;
                    Console.WriteLine("C1 b7._bVal");
                }
            }

            {
                var b8 = (B1_A_byte_Generic<ByteStruct>)Activator.CreateInstance(typeof(B1_A_byte_Generic<ByteStruct>));
                b8._bVal._dVal = 1;
                if (1 != ((ByteStruct)typeof(B1_A_byte_Generic<ByteStruct>).GetField("_bVal").GetValue(b8))._dVal)
                {
                    failure++;
                    Console.WriteLine("C1 b8._bVal");
                }
            }

            {
                var b9 = (B1_A_D_Generic<ByteStruct>)Activator.CreateInstance(typeof(B1_A_D_Generic<ByteStruct>));
                b9._bVal._dVal = 1;
                if (1 != ((ByteStruct)typeof(B1_A_D_Generic<ByteStruct>).GetField("_bVal").GetValue(b9))._dVal)
                {
                    failure++;
                    Console.WriteLine("C1 b9._bVal");
                }
            }

            {
                var b10 = (B1_ABoring_byte)Activator.CreateInstance(typeof(B1_ABoring_byte));
                b10._bVal = 1;
                if (1 != (byte)typeof(B1_ABoring_byte).GetField("_bVal").GetValue(b10))
                {
                    failure++;
                    Console.WriteLine("C1 b10._bVal");
                }
            }

            {
                var b11 = (B1_ABoring_D)Activator.CreateInstance(typeof(B1_ABoring_D));
                b11._bVal = 1;
                if (1 != (byte)typeof(B1_ABoring_D).GetField("_bVal").GetValue(b11))
                {
                    failure++;
                    Console.WriteLine("C1 b11._bVal");
                }
            }

            {
                var b12 = (B1_A1Boring_byte)Activator.CreateInstance(typeof(B1_A1Boring_byte));
                b12._bVal = 1;
                if (1 != (byte)typeof(B1_A1Boring_byte).GetField("_bVal").GetValue(b12))
                {
                    failure++;
                    Console.WriteLine("C1 b12._bVal");
                }
            }

            {
                var b13 = (B1_A1Boring_D)Activator.CreateInstance(typeof(B1_A1Boring_D));
                b13._bVal = 1;
                if (1 != (byte)typeof(B1_A1Boring_D).GetField("_bVal").GetValue(b13))
                {
                    failure++;
                    Console.WriteLine("C1 b13._bVal");
                }
            }

            {
                var c = (C1_B_A)Activator.CreateInstance(typeof(C1_B_A));
                c._cVal = 1;
                if (1 != (byte)typeof(C1_B_A).GetField("_cVal").GetValue(c))
                {
                    failure++;
                    Console.WriteLine("C1 c._cVal");
                }
            }

            {
                var c2 = (C1_B_A_byte)Activator.CreateInstance(typeof(C1_B_A_byte));
                c2._cVal = 1;
                if (1 != (byte)typeof(C1_B_A_byte).GetField("_cVal").GetValue(c2))
                {
                    failure++;
                    Console.WriteLine("C1 c2._cVal");
                }
            }

            {
                var c3 = (C1_B_A_D)Activator.CreateInstance(typeof(C1_B_A_D));
                c3._cVal = 1;
                if (1 != (byte)typeof(C1_B_A_D).GetField("_cVal").GetValue(c3))
                {
                    failure++;
                    Console.WriteLine("C1 c3._cVal");
                }
            }

            {
                var c4 = (C1_B_A_Generic_byte)Activator.CreateInstance(typeof(C1_B_A_Generic_byte));
                c4._cVal = 1;
                if (1 != (byte)typeof(C1_B_A_Generic_byte).GetField("_cVal").GetValue(c4))
                {
                    failure++;
                    Console.WriteLine("C1 c4._cVal");
                }
            }

            {
                var c5 = (C1_B_A_byte_Generic_byte)Activator.CreateInstance(typeof(C1_B_A_byte_Generic_byte));
                c5._cVal = 1;
                if (1 != (byte)typeof(C1_B_A_byte_Generic_byte).GetField("_cVal").GetValue(c5))
                {
                    failure++;
                    Console.WriteLine("C1 c5._cVal");
                }
            }

            {
                var c6 = (C1_B_A_D_Generic_byte)Activator.CreateInstance(typeof(C1_B_A_D_Generic_byte));
                c6._cVal = 1;
                if (1 != (byte)typeof(C1_B_A_D_Generic_byte).GetField("_cVal").GetValue(c6))
                {
                    failure++;
                    Console.WriteLine("C1 c6._cVal");
                }
            }

            {
                var c7 = (C1_B_A_Generic_D)Activator.CreateInstance(typeof(C1_B_A_Generic_D));
                c7._cVal = 1;
                if (1 != (byte)typeof(C1_B_A_Generic_D).GetField("_cVal").GetValue(c7))
                {
                    failure++;
                    Console.WriteLine("C1 c7._cVal");
                }
            }

            {
                var c8 = (C1_B_A_byte_Generic_D)Activator.CreateInstance(typeof(C1_B_A_byte_Generic_D));
                c8._cVal = 1;
                if (1 != (byte)typeof(C1_B_A_byte_Generic_D).GetField("_cVal").GetValue(c8))
                {
                    failure++;
                    Console.WriteLine("C1 c8._cVal");
                }
            }

            {
                var c9 = (C1_B_A_D_Generic_D)Activator.CreateInstance(typeof(C1_B_A_D_Generic_D));
                c9._cVal = 1;
                if (1 != (byte)typeof(C1_B_A_D_Generic_D).GetField("_cVal").GetValue(c9))
                {
                    failure++;
                    Console.WriteLine("C1 c9._cVal");
                }
            }

            return failure;
        }
    }
}