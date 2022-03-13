using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

public class Program
{
    // 'vlu1' is source as well as destination and want to make sure that
    // we do not allocate same register to the src/dest. We need to mark the
    // src as 'delayFree'.
    static unsafe int Main()
    {
        if (Avx2.IsSupported)
        {
            int* values = stackalloc int[256];
            var vmsk = Vector256.Create(-1, -1, -1, 0, -1, -1, -1, 0);
            var vlu1 = Vector256.Create(0, 1, 2, 3, 4, 5, 6, 7);
            var vlu2 = Vector256.Create(7, 6, 5, 4, 3, 2, 1, 0);

            vlu1 = Avx2.GatherMaskVector256(vlu1, values, vlu1, vmsk, sizeof(int));
            vlu2 = Avx2.GatherMaskVector256(vlu2, values, vlu2, vmsk, sizeof(int));

            if (vlu1.GetElement(3) != 3 || vlu2.GetElement(3) != 4)
            {
                return 1;
            }
        }

        return 100;
    }
}