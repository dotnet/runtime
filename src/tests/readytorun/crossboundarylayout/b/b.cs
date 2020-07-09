// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace CrossBoundaryLayout
{
    public struct ByteStructB
    {
        public byte _bsVal;
    }

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

    public class B_A_BS : AGeneric<ByteStructB>
    {
        public byte _bVal;
    }

    public class B_A_E : AGeneric<ByteStructE>
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

    public class B_A_BS_Generic<T> : AGeneric<ByteStructB>
    {
        public T _bVal;
    }

    public class B_A_E_Generic<T> : AGeneric<ByteStructE>
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

    public class B_ABoring_BS : ABoringGeneric<ByteStructB>
    {
        public byte _bVal;
    }

    public class B_ABoring_E : ABoringGeneric<ByteStructE>
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
                    ATest.ReportTestFailure("B a_aVal", a, ref failure);
                }
            }

            {
                var a2 = (AGeneric<byte>)Activator.CreateInstance(typeof(AGeneric<byte>));
                a2._aVal = 1;
                if (1 != (byte)typeof(AGeneric<byte>).GetField("_aVal").GetValue(a2))
                {
                    ATest.ReportTestFailure("B a2_aVal", a2, ref failure);
                }
            }

            {
                var a3 = (AGeneric<ByteStruct>)Activator.CreateInstance(typeof(AGeneric<ByteStruct>));
                a3._aVal._dVal = 1;
                if (1 != ((ByteStruct)typeof(AGeneric<ByteStruct>).GetField("_aVal").GetValue(a3))._dVal)
                {
                    ATest.ReportTestFailure("B a3_aVal", a3, ref failure);
                }
            }

            {
                var a4 = (ABoringGeneric<byte>)Activator.CreateInstance(typeof(ABoringGeneric<byte>));
                a4._aVal = 1;
                if (1 != (byte)typeof(ABoringGeneric<byte>).GetField("_aVal").GetValue(a4))
                {
                    ATest.ReportTestFailure("B a4_aVal", a4, ref failure);
                }
            }

            {
                var a5 = (ABoringGeneric<ByteStruct>)Activator.CreateInstance(typeof(ABoringGeneric<ByteStruct>));
                a5._aVal = 1;
                if (1 != (byte)typeof(ABoringGeneric<ByteStruct>).GetField("_aVal").GetValue(a5))
                {
                    ATest.ReportTestFailure("B a5_aVal", a5, ref failure);
                }
            }


            {
                var b = (B_A)Activator.CreateInstance(typeof(B_A));
                b._bVal = 1;
                if (1 != (byte)typeof(B_A).GetField("_bVal").GetValue(b))
                {
                    ATest.ReportTestFailure("B b_bVal", b, ref failure);
                }
            }

            {
                var b2 = (B_A_byte)Activator.CreateInstance(typeof(B_A_byte));
                b2._bVal = 1;
                if (1 != (byte)typeof(B_A_byte).GetField("_bVal").GetValue(b2))
                {
                    ATest.ReportTestFailure("B b2_bVal", b2, ref failure);
                }
            }

            {
                var b3 = (B_A_D)Activator.CreateInstance(typeof(B_A_D));
                b3._bVal = 1;
                if (1 != (byte)typeof(B_A_D).GetField("_bVal").GetValue(b3))
                {
                    ATest.ReportTestFailure("B b3_bVal", b3, ref failure);
                }
            }

            {
                var b4 = (B_A_Generic<byte>)Activator.CreateInstance(typeof(B_A_Generic<byte>));
                b4._bVal = 1;
                if (1 != (byte)typeof(B_A_Generic<byte>).GetField("_bVal").GetValue(b4))
                {
                    ATest.ReportTestFailure("B b4_bVal", b4, ref failure);
                }
            }

            {
                var b5 = (B_A_byte_Generic<byte>)Activator.CreateInstance(typeof(B_A_byte_Generic<byte>));
                b5._bVal = 1;
                if (1 != (byte)typeof(B_A_byte_Generic<byte>).GetField("_bVal").GetValue(b5))
                {
                    ATest.ReportTestFailure("B b5_bVal", b5, ref failure);
                }
            }

            {
                var b6 = (B_A_D_Generic<byte>)Activator.CreateInstance(typeof(B_A_D_Generic<byte>));
                b6._bVal = 1;
                if (1 != (byte)typeof(B_A_D_Generic<byte>).GetField("_bVal").GetValue(b6))
                {
                    ATest.ReportTestFailure("B b6_bVal", b6, ref failure);
                }
            }

            {
                var b7 = (B_A_Generic<ByteStruct>)Activator.CreateInstance(typeof(B_A_Generic<ByteStruct>));
                b7._bVal._dVal = 1;
                if (1 != ((ByteStruct)typeof(B_A_Generic<ByteStruct>).GetField("_bVal").GetValue(b7))._dVal)
                {
                    ATest.ReportTestFailure("B b7_bVal", b7, ref failure);
                }
            }

            {
                var b8 = (B_A_byte_Generic<ByteStruct>)Activator.CreateInstance(typeof(B_A_byte_Generic<ByteStruct>));
                b8._bVal._dVal = 1;
                if (1 != ((ByteStruct)typeof(B_A_byte_Generic<ByteStruct>).GetField("_bVal").GetValue(b8))._dVal)
                {
                    ATest.ReportTestFailure("B b8_bVal", b8, ref failure);
                }
            }

            {
                var b9 = (B_A_D_Generic<ByteStruct>)Activator.CreateInstance(typeof(B_A_D_Generic<ByteStruct>));
                b9._bVal._dVal = 1;
                if (1 != ((ByteStruct)typeof(B_A_D_Generic<ByteStruct>).GetField("_bVal").GetValue(b9))._dVal)
                {
                    ATest.ReportTestFailure("B b9_bVal", b9, ref failure);
                }
            }

            {
                var b10 = (B_ABoring_byte)Activator.CreateInstance(typeof(B_ABoring_byte));
                b10._bVal = 1;
                if (1 != (byte)typeof(B_ABoring_byte).GetField("_bVal").GetValue(b10))
                {
                    ATest.ReportTestFailure("B b10_bVal", b10, ref failure);
                }
            }

            {
                var b11 = (B_ABoring_D)Activator.CreateInstance(typeof(B_ABoring_D));
                b11._bVal = 1;
                if (1 != (byte)typeof(B_ABoring_D).GetField("_bVal").GetValue(b11))
                {
                    ATest.ReportTestFailure("B b11_bVal", b11, ref failure);
                }
            }

            {
                var b12 = (B_A_BS)Activator.CreateInstance(typeof(B_A_BS));
                b12._bVal = 1;
                if (1 != (byte)typeof(B_A_BS).GetField("_bVal").GetValue(b12))
                {
                    ATest.ReportTestFailure("B b12_bVal", b12, ref failure);
                }
            }

            {
                var b13 = (B_A_E)Activator.CreateInstance(typeof(B_A_E));
                b13._bVal = 1;
                if (1 != (byte)typeof(B_A_E).GetField("_bVal").GetValue(b13))
                {
                    ATest.ReportTestFailure("B b12_bVal", b13, ref failure);
                }
            }

            {
                var b14 = (B_ABoring_BS)Activator.CreateInstance(typeof(B_ABoring_BS));
                b14._bVal = 1;
                if (1 != (byte)typeof(B_ABoring_BS).GetField("_bVal").GetValue(b14))
                {
                    ATest.ReportTestFailure("B b12_bVal", b14, ref failure);
                }
            }

            {
                var b15 = (B_ABoring_E)Activator.CreateInstance(typeof(B_ABoring_E));
                b15._bVal = 1;
                if (1 != (byte)typeof(B_ABoring_E).GetField("_bVal").GetValue(b15))
                {
                    ATest.ReportTestFailure("B b12_bVal", b15, ref failure);
                }
            }

            return failure;
        }
    }
}
