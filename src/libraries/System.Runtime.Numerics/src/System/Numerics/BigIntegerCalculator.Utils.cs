using System.Runtime.CompilerServices;

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
    }
}