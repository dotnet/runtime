using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using Xunit;

namespace GitHub_17435
{
    public class Program
    {
        const int Pass = 100;
        const int Fail = 0;

        [Fact]
        public static unsafe void Test()
        {

            if (Sse2.IsSupported)
            {
                (uint a, uint b) = Program.Repro();
                if ((a !=3) || (b != 6))
                {
                    Console.WriteLine($"FAILED {a}, {b}");
                    Assert.Fail("");
                }
                else
                {
                    Console.WriteLine("Passed");
                }
            }
            else
            {
                Console.WriteLine("SSE2 not supported");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe static (uint, uint) Repro()
        {
            uint* a = stackalloc uint[4];
            a[0] = 1;
            a[1] = 1;
            a[2] = 1;
            a[3] = 1;
            
            // Here we force populate the registers
            var b = a[0];
            var h = a[3];
            var c = a[1];
            
            // We operate the values in xmm0
            Vector128<uint> v = Sse2.LoadVector128(a);
            v = Sse2.Add(v, v);
            // We send to the memory the modified values
            Sse2.Store(a, v);
            
            // We return both sums (from registers and from memory)
            return (b + h + c, a[0]+a[1]+a[3]);
        }
    }
}
