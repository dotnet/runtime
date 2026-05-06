// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
                    ATest.ReportTestFailure("C a_aVal", a, ref failure);
                }
            }

            {
                var a2 = (AGeneric<byte>)Activator.CreateInstance(typeof(AGeneric<byte>));
                a2._aVal = 1;
                if (1 != (byte)typeof(AGeneric<byte>).GetField("_aVal").GetValue(a2))
                {
                    ATest.ReportTestFailure("C a2_aVal", a2, ref failure);
                }
            }

            {
                var a3 = (AGeneric<ByteStruct>)Activator.CreateInstance(typeof(AGeneric<ByteStruct>));
                a3._aVal._dVal = 1;
                if (1 != ((ByteStruct)typeof(AGeneric<ByteStruct>).GetField("_aVal").GetValue(a3))._dVal)
                {
                    ATest.ReportTestFailure("C a3_aVal", a3, ref failure);
                }
            }

            {
                var a4 = (ABoringGeneric<byte>)Activator.CreateInstance(typeof(ABoringGeneric<byte>));
                a4._aVal = 1;
                if (1 != (byte)typeof(ABoringGeneric<byte>).GetField("_aVal").GetValue(a4))
                {
                    ATest.ReportTestFailure("C a4_aVal", a4, ref failure);
                }
            }

            {
                var a5 = (ABoringGeneric<ByteStruct>)Activator.CreateInstance(typeof(ABoringGeneric<ByteStruct>));
                a5._aVal = 1;
                if (1 != (byte)typeof(ABoringGeneric<ByteStruct>).GetField("_aVal").GetValue(a5))
                {
                    ATest.ReportTestFailure("C a5_aVal", a5, ref failure);
                }
            }


            {
                var b = (B_A)Activator.CreateInstance(typeof(B_A));
                b._bVal = 1;
                if (1 != (byte)typeof(B_A).GetField("_bVal").GetValue(b))
                {
                    ATest.ReportTestFailure("C b_bVal", b, ref failure);
                }
            }

            {
                var b2 = (B_A_byte)Activator.CreateInstance(typeof(B_A_byte));
                b2._bVal = 1;
                if (1 != (byte)typeof(B_A_byte).GetField("_bVal").GetValue(b2))
                {
                    ATest.ReportTestFailure("C b2_bVal", b2, ref failure);
                }
            }

            {
                var b3 = (B_A_D)Activator.CreateInstance(typeof(B_A_D));
                b3._bVal = 1;
                if (1 != (byte)typeof(B_A_D).GetField("_bVal").GetValue(b3))
                {
                    ATest.ReportTestFailure("C b3_bVal", b3, ref failure);
                }
            }

            {
                var b4 = (B_A_Generic<byte>)Activator.CreateInstance(typeof(B_A_Generic<byte>));
                b4._bVal = 1;
                if (1 != (byte)typeof(B_A_Generic<byte>).GetField("_bVal").GetValue(b4))
                {
                    ATest.ReportTestFailure("C b4_bVal", b4, ref failure);
                }
            }

            {
                var b5 = (B_A_byte_Generic<byte>)Activator.CreateInstance(typeof(B_A_byte_Generic<byte>));
                b5._bVal = 1;
                if (1 != (byte)typeof(B_A_byte_Generic<byte>).GetField("_bVal").GetValue(b5))
                {
                    ATest.ReportTestFailure("C b5_bVal", b5, ref failure);
                }
            }

            {
                var b6 = (B_A_D_Generic<byte>)Activator.CreateInstance(typeof(B_A_D_Generic<byte>));
                b6._bVal = 1;
                if (1 != (byte)typeof(B_A_D_Generic<byte>).GetField("_bVal").GetValue(b6))
                {
                    ATest.ReportTestFailure("C b6_bVal", b6, ref failure);
                }
            }

            {
                var b7 = (B_A_Generic<ByteStruct>)Activator.CreateInstance(typeof(B_A_Generic<ByteStruct>));
                b7._bVal._dVal = 1;
                if (1 != ((ByteStruct)typeof(B_A_Generic<ByteStruct>).GetField("_bVal").GetValue(b7))._dVal)
                {
                    ATest.ReportTestFailure("C b7_bVal", b7, ref failure);
                }
            }

            {
                var b8 = (B_A_byte_Generic<ByteStruct>)Activator.CreateInstance(typeof(B_A_byte_Generic<ByteStruct>));
                b8._bVal._dVal = 1;
                if (1 != ((ByteStruct)typeof(B_A_byte_Generic<ByteStruct>).GetField("_bVal").GetValue(b8))._dVal)
                {
                    ATest.ReportTestFailure("C b8_bVal", b8, ref failure);
                }
            }

            {
                var b9 = (B_A_D_Generic<ByteStruct>)Activator.CreateInstance(typeof(B_A_D_Generic<ByteStruct>));
                b9._bVal._dVal = 1;
                if (1 != ((ByteStruct)typeof(B_A_D_Generic<ByteStruct>).GetField("_bVal").GetValue(b9))._dVal)
                {
                    ATest.ReportTestFailure("C b9_bVal", b9, ref failure);
                }
            }

            {
                var b10 = (B_ABoring_byte)Activator.CreateInstance(typeof(B_ABoring_byte));
                b10._bVal = 1;
                if (1 != (byte)typeof(B_ABoring_byte).GetField("_bVal").GetValue(b10))
                {
                    ATest.ReportTestFailure("C b10_bVal", b10, ref failure);
                }
            }

            {
                var b11 = (B_ABoring_D)Activator.CreateInstance(typeof(B_ABoring_D));
                b11._bVal = 1;
                if (1 != (byte)typeof(B_ABoring_D).GetField("_bVal").GetValue(b11))
                {
                    ATest.ReportTestFailure("C b11_bVal", b11, ref failure);
                }
            }

            {
                var c = (C_B_A)Activator.CreateInstance(typeof(C_B_A));
                c._cVal = 1;
                if (1 != (byte)typeof(C_B_A).GetField("_cVal").GetValue(c))
                {
                    ATest.ReportTestFailure("C c_bVal", c, ref failure);
                }
            }

            {
                var c2 = (C_B_A_byte)Activator.CreateInstance(typeof(C_B_A_byte));
                c2._cVal = 1;
                if (1 != (byte)typeof(C_B_A_byte).GetField("_cVal").GetValue(c2))
                {
                    ATest.ReportTestFailure("C c2_bVal", c2, ref failure);
                }
            }

            {
                var c3 = (C_B_A_D)Activator.CreateInstance(typeof(C_B_A_D));
                c3._cVal = 1;
                if (1 != (byte)typeof(C_B_A_D).GetField("_cVal").GetValue(c3))
                {
                    ATest.ReportTestFailure("C c3_bVal", c3, ref failure);
                }
            }

            {
                var c4 = (C_B_A_Generic_byte)Activator.CreateInstance(typeof(C_B_A_Generic_byte));
                c4._cVal = 1;
                if (1 != (byte)typeof(C_B_A_Generic_byte).GetField("_cVal").GetValue(c4))
                {
                    ATest.ReportTestFailure("C c4_bVal", c4, ref failure);
                }
            }

            {
                var c5 = (C_B_A_byte_Generic_byte)Activator.CreateInstance(typeof(C_B_A_byte_Generic_byte));
                c5._cVal = 1;
                if (1 != (byte)typeof(C_B_A_byte_Generic_byte).GetField("_cVal").GetValue(c5))
                {
                    ATest.ReportTestFailure("C c5_bVal", c5, ref failure);
                }
            }

            {
                var c6 = (C_B_A_D_Generic_byte)Activator.CreateInstance(typeof(C_B_A_D_Generic_byte));
                c6._cVal = 1;
                if (1 != (byte)typeof(C_B_A_D_Generic_byte).GetField("_cVal").GetValue(c6))
                {
                    ATest.ReportTestFailure("C c6_bVal", c6, ref failure);
                }
            }

            {
                var c7 = (C_B_A_Generic_D)Activator.CreateInstance(typeof(C_B_A_Generic_D));
                c7._cVal = 1;
                if (1 != (byte)typeof(C_B_A_Generic_D).GetField("_cVal").GetValue(c7))
                {
                    ATest.ReportTestFailure("C c7_bVal", c7, ref failure);
                }
            }

            {
                var c8 = (C_B_A_byte_Generic_D)Activator.CreateInstance(typeof(C_B_A_byte_Generic_D));
                c8._cVal = 1;
                if (1 != (byte)typeof(C_B_A_byte_Generic_D).GetField("_cVal").GetValue(c8))
                {
                    ATest.ReportTestFailure("C c8_bVal", c8, ref failure);
                }
            }

            {
                var c9 = (C_B_A_D_Generic_D)Activator.CreateInstance(typeof(C_B_A_D_Generic_D));
                c9._cVal = 1;
                if (1 != (byte)typeof(C_B_A_D_Generic_D).GetField("_cVal").GetValue(c9))
                {
                    ATest.ReportTestFailure("C c9_bVal", c9, ref failure);
                }
            }

            return failure;
        }
    }
}
