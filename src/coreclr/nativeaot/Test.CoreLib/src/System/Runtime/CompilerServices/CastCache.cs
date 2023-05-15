namespace System.Runtime.CompilerServices
{
    internal enum CastResult
    {
        CannotCast = 0,
        CanCast = 1,
        MaybeCast = 2
    }

    // trivial implementation of the cast cache
    internal static unsafe class CastCache
    {
        internal static CastResult TryGet(nuint source, nuint target)
        {
            return CastResult.MaybeCast;
        }

        internal static void TrySet(nuint source, nuint target, bool result)
        {
        }
    }
}
