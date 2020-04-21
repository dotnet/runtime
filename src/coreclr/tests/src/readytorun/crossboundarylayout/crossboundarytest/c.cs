// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace CrossBoundaryLayout
{
    class C_B_A : B_A
    {
        public byte _cVal;
    }

    class C_B_A_byte : B_A_byte
    {
        public byte _cVal;
    }

    class C_B_A_D : B_A_D
    {
        public byte _cVal;
    }

    class C_B_A_Generic_byte : B_A_Generic<byte>
    {
        public byte _cVal;
    }

    class C_B_A_Generic_D : B_A_Generic<ByteStruct>
    {
        public byte _cVal;
    }

    class C_B_A_byte_Generic_byte : B_A_byte_Generic<byte>
    {
        public byte _cVal;
    }

    class C_B_A_byte_Generic_D : B_A_byte_Generic<ByteStruct>
    {
        public byte _cVal;
    }

    class C_B_A_D_Generic_byte : B_A_D_Generic<byte>
    {
        public byte _cVal;
    }

    class C_B_A_D_Generic_D : B_A_D_Generic<ByteStruct>
    {
        public byte _cVal;
    }

    public class CTest
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
                    Console.WriteLine("C a._aVal");
                }
            }

            {
                var a2 = (AGeneric<byte>)Activator.CreateInstance(typeof(AGeneric<byte>));
                a2._aVal = 1;
                if (1 != (byte)typeof(AGeneric<byte>).GetField("_aVal").GetValue(a2))
                {
                    failure++;
                    Console.WriteLine("C a2_aVal");
                }
            }

            {
                var a3 = (AGeneric<ByteStruct>)Activator.CreateInstance(typeof(AGeneric<ByteStruct>));
                a3._aVal._dVal = 1;
                if (1 != ((ByteStruct)typeof(AGeneric<ByteStruct>).GetField("_aVal").GetValue(a3))._dVal)
                {
                    failure++;
                    Console.WriteLine("C a3_aVal");
                }
            }

            {
                var a4 = (ABoringGeneric<byte>)Activator.CreateInstance(typeof(ABoringGeneric<byte>));
                a4._aVal = 1;
                if (1 != (byte)typeof(ABoringGeneric<byte>).GetField("_aVal").GetValue(a4))
                {
                    failure++;
                    Console.WriteLine("C a4_aVal");
                }
            }

            {
                var a5 = (ABoringGeneric<ByteStruct>)Activator.CreateInstance(typeof(ABoringGeneric<ByteStruct>));
                a5._aVal = 1;
                if (1 != (byte)typeof(ABoringGeneric<ByteStruct>).GetField("_aVal").GetValue(a5))
                {
                    failure++;
                    Console.WriteLine("C a5_aVal");
                }
            }

            {
                var b = (B_A)Activator.CreateInstance(typeof(B_A));
                b._bVal = 1;
                if (1 != (byte)typeof(B_A).GetField("_bVal").GetValue(b))
                {
                    failure++;
                    Console.WriteLine("C b._bVal");
                }
            }

            {
                var b2 = (B_A_byte)Activator.CreateInstance(typeof(B_A_byte));
                b2._bVal = 1;
                if (1 != (byte)typeof(B_A_byte).GetField("_bVal").GetValue(b2))
                {
                    failure++;
                    Console.WriteLine("C b2._bVal");
                }
            }

            {
                var b3 = (B_A_D)Activator.CreateInstance(typeof(B_A_D));
                b3._bVal = 1;
                if (1 != (byte)typeof(B_A_D).GetField("_bVal").GetValue(b3))
                {
                    failure++;
                    Console.WriteLine("C b3._bVal");
                }
            }

            {
                var b4 = (B_A_Generic<byte>)Activator.CreateInstance(typeof(B_A_Generic<byte>));
                b4._bVal = 1;
                if (1 != (byte)typeof(B_A_Generic<byte>).GetField("_bVal").GetValue(b4))
                {
                    failure++;
                    Console.WriteLine("C b4._bVal");
                }
            }

            {
                var b5 = (B_A_byte_Generic<byte>)Activator.CreateInstance(typeof(B_A_byte_Generic<byte>));
                b5._bVal = 1;
                if (1 != (byte)typeof(B_A_byte_Generic<byte>).GetField("_bVal").GetValue(b5))
                {
                    failure++;
                    Console.WriteLine("C b5._bVal");
                }
            }

            {
                var b6 = (B_A_D_Generic<byte>)Activator.CreateInstance(typeof(B_A_D_Generic<byte>));
                b6._bVal = 1;
                if (1 != (byte)typeof(B_A_D_Generic<byte>).GetField("_bVal").GetValue(b6))
                {
                    failure++;
                    Console.WriteLine("C b6._bVal");
                }
            }

            {
                var b7 = (B_A_Generic<ByteStruct>)Activator.CreateInstance(typeof(B_A_Generic<ByteStruct>));
                b7._bVal._dVal = 1;
                if (1 != ((ByteStruct)typeof(B_A_Generic<ByteStruct>).GetField("_bVal").GetValue(b7))._dVal)
                {
                    failure++;
                    Console.WriteLine("C b7._bVal");
                }
            }

            {
                var b8 = (B_A_byte_Generic<ByteStruct>)Activator.CreateInstance(typeof(B_A_byte_Generic<ByteStruct>));
                b8._bVal._dVal = 1;
                if (1 != ((ByteStruct)typeof(B_A_byte_Generic<ByteStruct>).GetField("_bVal").GetValue(b8))._dVal)
                {
                    failure++;
                    Console.WriteLine("C b8._bVal");
                }
            }

            {
                var b9 = (B_A_D_Generic<ByteStruct>)Activator.CreateInstance(typeof(B_A_D_Generic<ByteStruct>));
                b9._bVal._dVal = 1;
                if (1 != ((ByteStruct)typeof(B_A_D_Generic<ByteStruct>).GetField("_bVal").GetValue(b9))._dVal)
                {
                    failure++;
                    Console.WriteLine("C b9._bVal");
                }
            }

            {
                var b10 = (B_ABoring_byte)Activator.CreateInstance(typeof(B_ABoring_byte));
                b10._bVal = 1;
                if (1 != (byte)typeof(B_ABoring_byte).GetField("_bVal").GetValue(b10))
                {
                    failure++;
                    Console.WriteLine("C b10._bVal");
                }
            }

            {
                var b11 = (B_ABoring_D)Activator.CreateInstance(typeof(B_ABoring_D));
                b11._bVal = 1;
                if (1 != (byte)typeof(B_ABoring_D).GetField("_bVal").GetValue(b11))
                {
                    failure++;
                    Console.WriteLine("C b11._bVal");
                }
            }


            {
                var c = (C_B_A)Activator.CreateInstance(typeof(C_B_A));
                c._cVal = 1;
                if (1 != (byte)typeof(C_B_A).GetField("_cVal").GetValue(c))
                {
                    failure++;
                    Console.WriteLine("C c._cVal");
                }
            }

            {
                var c2 = (C_B_A_byte)Activator.CreateInstance(typeof(C_B_A_byte));
                c2._cVal = 1;
                if (1 != (byte)typeof(C_B_A_byte).GetField("_cVal").GetValue(c2))
                {
                    failure++;
                    Console.WriteLine("C c2._cVal");
                }
            }

            {
                var c3 = (C_B_A_D)Activator.CreateInstance(typeof(C_B_A_D));
                c3._cVal = 1;
                if (1 != (byte)typeof(C_B_A_D).GetField("_cVal").GetValue(c3))
                {
                    failure++;
                    Console.WriteLine("C c3._cVal");
                }
            }

            {
                var c4 = (C_B_A_Generic_byte)Activator.CreateInstance(typeof(C_B_A_Generic_byte));
                c4._cVal = 1;
                if (1 != (byte)typeof(C_B_A_Generic_byte).GetField("_cVal").GetValue(c4))
                {
                    failure++;
                    Console.WriteLine("C c4._cVal");
                }
            }

            {
                var c5 = (C_B_A_byte_Generic_byte)Activator.CreateInstance(typeof(C_B_A_byte_Generic_byte));
                c5._cVal = 1;
                if (1 != (byte)typeof(C_B_A_byte_Generic_byte).GetField("_cVal").GetValue(c5))
                {
                    failure++;
                    Console.WriteLine("C c5._cVal");
                }
            }

            {
                var c6 = (C_B_A_D_Generic_byte)Activator.CreateInstance(typeof(C_B_A_D_Generic_byte));
                c6._cVal = 1;
                if (1 != (byte)typeof(C_B_A_D_Generic_byte).GetField("_cVal").GetValue(c6))
                {
                    failure++;
                    Console.WriteLine("C c6._cVal");
                }
            }

            {
                var c7 = (C_B_A_Generic_D)Activator.CreateInstance(typeof(C_B_A_Generic_D));
                c7._cVal = 1;
                if (1 != (byte)typeof(C_B_A_Generic_D).GetField("_cVal").GetValue(c7))
                {
                    failure++;
                    Console.WriteLine("C c7._cVal");
                }
            }

            {
                var c8 = (C_B_A_byte_Generic_D)Activator.CreateInstance(typeof(C_B_A_byte_Generic_D));
                c8._cVal = 1;
                if (1 != (byte)typeof(C_B_A_byte_Generic_D).GetField("_cVal").GetValue(c8))
                {
                    failure++;
                    Console.WriteLine("C c8._cVal");
                }
            }

            {
                var c9 = (C_B_A_D_Generic_D)Activator.CreateInstance(typeof(C_B_A_D_Generic_D));
                c9._cVal = 1;
                if (1 != (byte)typeof(C_B_A_D_Generic_D).GetField("_cVal").GetValue(c9))
                {
                    failure++;
                    Console.WriteLine("C c9._cVal");
                }
            }

            return failure;
        }
    }
}