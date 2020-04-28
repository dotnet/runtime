using System.Runtime.CompilerServices;
using static System.Runtime.InteropServices.MemoryMarshal;

namespace System.Numerics
{
    internal static partial class BigIntegerCalculator
    {
        private static unsafe ref uint NullRef => ref Unsafe.AsRef<uint>(null);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Span<uint> ZeroMem(Span<uint> memory)
        {
            memory.Clear();
            return memory;
        }

        public static int Compare(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right)
        {
            if (left.Length < right.Length)
                return -1;
            if (left.Length > right.Length)
                return 1;

            for (int i = left.Length - 1; i >= 0; i--)
            {
                uint leftElement = Unsafe.Add(ref GetReference(left), i);
                uint rightElement = Unsafe.Add(ref GetReference(right), i);
                if (leftElement < rightElement)
                    return -1;
                if (leftElement > rightElement)
                    return 1;
            }

            return 0;
        }
    }
}