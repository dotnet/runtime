using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public unsafe class FieldListByteNodeTypeMismatchX86
{
    private static readonly byte* _addr = (byte*)Marshal.AllocHGlobal(20);
    private static int _result = 100;

    [Fact]
    public static int TestEntryPoint()
    {
        int sum = 0;
        Problem(&sum);

        return _result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Problem(int* sum)
    {
        // Just a loop with some computations so that we can use
        // callee-saved registers (which happen to be non-byteable ones).
        for (int i = 0; i < 10; i++)
        {
            var i1 = i ^ i;
            var i2 = i | i;
            var i3 = i & i;
            var i4 = i1 + i + i2 - i3;
            i4 = i2 ^ i4;

            *sum += i4;
        }

        Sx2x1 s;
        byte* j = Addr();

        int o1 = j[-2];
        int o2 = j[-1];
        s.Short = j[0];
        s.Byte = j[1];

        o1 = o1 + o1;
        o2 = o2 + o2;

        if (s.Byte != 1)
        {
            return;
        }

        // Here assertion propagation will make s.Byte into CNS_INT(TYP_INT), yet the RA must
        // still allocate a byteable register so that it can be saved to the stack correctly.
        Call(s, 0, 0, o2, o1, j);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte* Addr()
    {
        _addr[11] = 1;
        return _addr + 10;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Call(Sx2x1 s, int i3, int i4, int i5, int i6, byte* i1)
    {
        if (s.Byte != 1)
        {
            _result = -1;
        }
    }

    private struct Sx2x1
    {
        public short Short;
        public byte Byte;
    }
}
