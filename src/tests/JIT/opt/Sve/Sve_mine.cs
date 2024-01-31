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
using Xunit;

namespace CodeGenTests
{
    public static class Sve_mine
    {
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

            return 100;
        }
    }
}