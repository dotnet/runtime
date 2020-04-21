// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace CrossBoundaryLayout
{
    public class B_A : A
    {
        public byte _bVal;
    }

    public class B_A_byte : AGeneric<byte>
    {
        public byte _bVal;
    }

    public class B_A_D : AGeneric<ByteStruct>
    {
        public byte _bVal;
    }

    public class B_A_Generic<T> : A
    {
        public T _bVal;
    }

    public class B_A_byte_Generic<T> : AGeneric<byte>
    {
        public T _bVal;
    }

    public class B_A_D_Generic<T> : AGeneric<ByteStruct>
    {
        public T _bVal;
    }

    public class B_ABoring_byte : ABoringGeneric<byte>
    {
        public byte _bVal;
    }

    public class B_ABoring_D : ABoringGeneric<ByteStruct>
    {
        public byte _bVal;
    }

    public class BTest
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
                    Console.WriteLine("B a._aVal");
                }
            }

            {
                var a2 = (AGeneric<byte>)Activator.CreateInstance(typeof(AGeneric<byte>));
                a2._aVal = 1;
                if (1 != (byte)typeof(AGeneric<byte>).GetField("_aVal").GetValue(a2))
                {
                    failure++;
                    Console.WriteLine("B a2_aVal");
                }
            }

            {
                var a3 = (AGeneric<ByteStruct>)Activator.CreateInstance(typeof(AGeneric<ByteStruct>));
                a3._aVal._dVal = 1;
                if (1 != ((ByteStruct)typeof(AGeneric<ByteStruct>).GetField("_aVal").GetValue(a3))._dVal)
                {
                    failure++;
                    Console.WriteLine("B a3_aVal");
                }
            }

            {
                var a4 = (ABoringGeneric<byte>)Activator.CreateInstance(typeof(ABoringGeneric<byte>));
                a4._aVal = 1;
                if (1 != (byte)typeof(ABoringGeneric<byte>).GetField("_aVal").GetValue(a4))
                {
                    failure++;
                    Console.WriteLine("B a4_aVal");
                }
            }

            {
                var a5 = (ABoringGeneric<ByteStruct>)Activator.CreateInstance(typeof(ABoringGeneric<ByteStruct>));
                a5._aVal = 1;
                if (1 != (byte)typeof(ABoringGeneric<ByteStruct>).GetField("_aVal").GetValue(a5))
                {
                    failure++;
                    Console.WriteLine("B a5_aVal");
                }
            }


            {
                var b = (B_A)Activator.CreateInstance(typeof(B_A));
                b._bVal = 1;
                if (1 != (byte)typeof(B_A).GetField("_bVal").GetValue(b))
                {
                    failure++;
                    Console.WriteLine("B b._bVal");
                }
            }

            {
                var b2 = (B_A_byte)Activator.CreateInstance(typeof(B_A_byte));
                b2._bVal = 1;
                if (1 != (byte)typeof(B_A_byte).GetField("_bVal").GetValue(b2))
                {
                    failure++;
                    Console.WriteLine("B b2._bVal");
                }
            }

            {
                var b3 = (B_A_D)Activator.CreateInstance(typeof(B_A_D));
                b3._bVal = 1;
                if (1 != (byte)typeof(B_A_D).GetField("_bVal").GetValue(b3))
                {
                    failure++;
                    Console.WriteLine("B b3._bVal");
                }
            }

            {
                var b4 = (B_A_Generic<byte>)Activator.CreateInstance(typeof(B_A_Generic<byte>));
                b4._bVal = 1;
                if (1 != (byte)typeof(B_A_Generic<byte>).GetField("_bVal").GetValue(b4))
                {
                    failure++;
                    Console.WriteLine("B b4._bVal");
                }
            }

            {
                var b5 = (B_A_byte_Generic<byte>)Activator.CreateInstance(typeof(B_A_byte_Generic<byte>));
                b5._bVal = 1;
                if (1 != (byte)typeof(B_A_byte_Generic<byte>).GetField("_bVal").GetValue(b5))
                {
                    failure++;
                    Console.WriteLine("B b5._bVal");
                }
            }

            {
                var b6 = (B_A_D_Generic<byte>)Activator.CreateInstance(typeof(B_A_D_Generic<byte>));
                b6._bVal = 1;
                if (1 != (byte)typeof(B_A_D_Generic<byte>).GetField("_bVal").GetValue(b6))
                {
                    failure++;
                    Console.WriteLine("B b6._bVal");
                }
            }

            {
                var b7 = (B_A_Generic<ByteStruct>)Activator.CreateInstance(typeof(B_A_Generic<ByteStruct>));
                b7._bVal._dVal = 1;
                if (1 != ((ByteStruct)typeof(B_A_Generic<ByteStruct>).GetField("_bVal").GetValue(b7))._dVal)
                {
                    failure++;
                    Console.WriteLine("B b7._bVal");
                }
            }

            {
                var b8 = (B_A_byte_Generic<ByteStruct>)Activator.CreateInstance(typeof(B_A_byte_Generic<ByteStruct>));
                b8._bVal._dVal = 1;
                if (1 != ((ByteStruct)typeof(B_A_byte_Generic<ByteStruct>).GetField("_bVal").GetValue(b8))._dVal)
                {
                    failure++;
                    Console.WriteLine("B b8._bVal");
                }
            }

            {
                var b9 = (B_A_D_Generic<ByteStruct>)Activator.CreateInstance(typeof(B_A_D_Generic<ByteStruct>));
                b9._bVal._dVal = 1;
                if (1 != ((ByteStruct)typeof(B_A_D_Generic<ByteStruct>).GetField("_bVal").GetValue(b9))._dVal)
                {
                    failure++;
                    Console.WriteLine("B b9._bVal");
                }
            }

            {
                var b10 = (B_ABoring_byte)Activator.CreateInstance(typeof(B_ABoring_byte));
                b10._bVal = 1;
                if (1 != (byte)typeof(B_ABoring_byte).GetField("_bVal").GetValue(b10))
                {
                    failure++;
                    Console.WriteLine("B b10._bVal");
                }
            }

            {
                var b11 = (B_ABoring_D)Activator.CreateInstance(typeof(B_ABoring_D));
                b11._bVal = 1;
                if (1 != (byte)typeof(B_ABoring_D).GetField("_bVal").GetValue(b11))
                {
                    failure++;
                    Console.WriteLine("B b11._bVal");
                }
            }

            return failure;
        }
    }
}