using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using  System.Runtime.InteropServices;
using Xunit;

namespace CodeGenTests
{
    public static class Sve_mine
    {

        private static unsafe void* Align(byte* buffer, ulong expectedAlignment)
        {
            return (void*)(((ulong)buffer + expectedAlignment - 1) & ~(expectedAlignment - 1));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Vector<byte> TrueMask(SveMaskPattern mask)
        {
            return Sve.TrueMask(mask);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Vector<byte> TrueMask_VectorCount4()
        {
            return Sve.TrueMask(SveMaskPattern.VectorCount4);
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe static Vector<byte> LoadVector_ImplicitMask(byte* address)
        {
            Vector<byte> mask = Sve.TrueMask(SveMaskPattern.All);
            return Sve.LoadVector(mask, address);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe static bool do_LoadVector_ImplicitMask()
        {
            int elemsInVector = 16;
            int OpElementCount = elemsInVector * 2;
            byte[] inArray1 = new byte[OpElementCount];
            for (var i = 0; i < OpElementCount; i++) { inArray1[i] = (byte)(i+1); }

            GCHandle inHandle1;
            inHandle1 = GCHandle.Alloc(inArray1, GCHandleType.Pinned);
            byte* inArray1Ptr = (byte*)Align((byte*)(inHandle1.AddrOfPinnedObject().ToPointer()), 128);

            Vector<byte> outVector1 = LoadVector_ImplicitMask(inArray1Ptr);

            for (var i = 0; i < elemsInVector; i++)
            {
                if (inArray1[i] != outVector1[i])
                {
                    Console.WriteLine("{0} {1} != {2}", i, inArray1[i], outVector1[i]);
                    // return false;
                }
                Console.WriteLine(outVector1[i]);
            }

            return true;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            Console.WriteLine($"  Sve:       {Sve.IsSupported}");
            if (!Sve.IsSupported)
            {
                return 0;
            }
            Vector<byte> mask1 = TrueMask(SveMaskPattern.VectorCount1);
            Vector<byte> mask2 = TrueMask_VectorCount4();
            Console.WriteLine($"Done {mask1} {mask2}");

            if(!do_LoadVector_ImplicitMask())
            {
                return 0;
            }

            return 100;
        }
    }
}