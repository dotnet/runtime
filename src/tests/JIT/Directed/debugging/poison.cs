using System;
using System.Runtime.CompilerServices;

public class Program
{
    [SkipLocalsInit]
    public static unsafe int Main()
    {
        bool result = true;

        int poisoned;
        Unsafe.SkipInit(out poisoned);
        result &= VerifyPoison(&poisoned, sizeof(int));

        GCRef zeroed;
        Unsafe.SkipInit(out zeroed);
        result &= VerifyZero(Unsafe.AsPointer(ref zeroed), Unsafe.SizeOf<GCRef>());

        WithoutGCRef poisoned2;
        Unsafe.SkipInit(out poisoned2);
        result &= VerifyPoison(&poisoned2, sizeof(WithoutGCRef));

        return result ? 100 : 101;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe bool VerifyPoison(void* val, int size)
        => AllEq(new Span<byte>(val, size), 0xCD);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe bool VerifyZero(void* val, int size)
        => AllEq(new Span<byte>(val, size), 0);

    private static unsafe bool AllEq(Span<byte> span, byte byteVal)
    {
        foreach (byte b in span)
        {
            if (b != byteVal)
                return false;
        }

        return true;
    }

    private struct GCRef
    {
        public object ARef;
    }

    private struct WithoutGCRef
    {
        public double ADouble;
        public int ANumber;
        public float AFloat;
    }
}
