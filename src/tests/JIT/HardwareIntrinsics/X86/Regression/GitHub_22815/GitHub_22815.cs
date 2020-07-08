using System;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;

namespace GitHub_22815
{
    class Program
    {
        const int Pass = 100;
        const int Fail = 0;

        static int Main(string[] args)
        {
            bool result = true;
            if (Avx2.IsSupported)
            {
                result = test128((byte)1) && test128((sbyte)1) && test128((short)1) &&
                         test128((ushort)1) && test128((int)1) && test128((uint)1) && 
                         test128((long)1) && test128((ulong)1) &&
                         test256((byte)1) && test256((sbyte)1) && test256((short)1) &&
                         test256((ushort)1) && test256((int)1) && test256((uint)1) && 
                         test256((long)1) && test256((ulong)1);
            }
            return result ? Pass : Fail;
        }

        static unsafe bool test128(byte v)
        {
            var vec = Avx2.BroadcastScalarToVector128(&v);
            for (int i = 0; i < Vector128<byte>.Count; i++)
            {
                if (vec.GetElement(i) != v)
                {
                    return false;
                }
            }
            return true;
        }

        static unsafe bool test128(sbyte v)
        {
            var vec = Avx2.BroadcastScalarToVector128(&v);
            for (int i = 0; i < Vector128<sbyte>.Count; i++)
            {
                if (vec.GetElement(i) != v)
                {
                    return false;
                }
            }
            return true;
        }

        static unsafe bool test128(short v)
        {
            var vec = Avx2.BroadcastScalarToVector128(&v);
            for (int i = 0; i < Vector128<short>.Count; i++)
            {
                if (vec.GetElement(i) != v)
                {
                    return false;
                }
            }
            return true;
        }

        static unsafe bool test128(ushort v)
        {
            var vec = Avx2.BroadcastScalarToVector128(&v);
            for (int i = 0; i < Vector128<ushort>.Count; i++)
            {
                if (vec.GetElement(i) != v)
                {
                    return false;
                }
            }
            return true;
        }

        static unsafe bool test128(int v)
        {
            var vec = Avx2.BroadcastScalarToVector128(&v);
            for (int i = 0; i < Vector128<int>.Count; i++)
            {
                if (vec.GetElement(i) != v)
                {
                    return false;
                }
            }
            return true;
        }
        
        static unsafe bool test128(uint v)
        {
            var vec = Avx2.BroadcastScalarToVector128(&v);
            for (int i = 0; i < Vector128<uint>.Count; i++)
            {
                if (vec.GetElement(i) != v)
                {
                    return false;
                }
            }
            return true;
        }

        static unsafe bool test128(long v)
        {
            var vec = Avx2.BroadcastScalarToVector128(&v);
            for (int i = 0; i < Vector128<long>.Count; i++)
            {
                if (vec.GetElement(i) != v)
                {
                    return false;
                }
            }
            return true;
        }

        static unsafe bool test128(ulong v)
        {
            var vec = Avx2.BroadcastScalarToVector128(&v);
            for (int i = 0; i < Vector128<ulong>.Count; i++)
            {
                if (vec.GetElement(i) != v)
                {
                    return false;
                }
            }
            return true;
        }

        static unsafe bool test256(byte v)
        {
            var vec = Avx2.BroadcastScalarToVector256(&v);
            for (int i = 0; i < Vector256<byte>.Count; i++)
            {
                if (vec.GetElement(i) != v)
                {
                    return false;
                }
            }
            return true;
        }

        static unsafe bool test256(sbyte v)
        {
            var vec = Avx2.BroadcastScalarToVector256(&v);
            for (int i = 0; i < Vector256<sbyte>.Count; i++)
            {
                if (vec.GetElement(i) != v)
                {
                    return false;
                }
            }
            return true;
        }

        static unsafe bool test256(short v)
        {
            var vec = Avx2.BroadcastScalarToVector256(&v);
            for (int i = 0; i < Vector256<short>.Count; i++)
            {
                if (vec.GetElement(i) != v)
                {
                    return false;
                }
            }
            return true;
        }

        static unsafe bool test256(ushort v)
        {
            var vec = Avx2.BroadcastScalarToVector256(&v);
            for (int i = 0; i < Vector256<ushort>.Count; i++)
            {
                if (vec.GetElement(i) != v)
                {
                    return false;
                }
            }
            return true;
        }

        static unsafe bool test256(int v)
        {
            var vec = Avx2.BroadcastScalarToVector256(&v);
            for (int i = 0; i < Vector256<int>.Count; i++)
            {
                if (vec.GetElement(i) != v)
                {
                    return false;
                }
            }
            return true;
        }
        
        static unsafe bool test256(uint v)
        {
            var vec = Avx2.BroadcastScalarToVector256(&v);
            for (int i = 0; i < Vector256<uint>.Count; i++)
            {
                if (vec.GetElement(i) != v)
                {
                    return false;
                }
            }
            return true;
        }

        static unsafe bool test256(long v)
        {
            var vec = Avx2.BroadcastScalarToVector256(&v);
            for (int i = 0; i < Vector256<long>.Count; i++)
            {
                if (vec.GetElement(i) != v)
                {
                    return false;
                }
            }
            return true;
        }

        static unsafe bool test256(ulong v)
        {
            var vec = Avx2.BroadcastScalarToVector256(&v);
            for (int i = 0; i < Vector256<ulong>.Count; i++)
            {
                if (vec.GetElement(i) != v)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
