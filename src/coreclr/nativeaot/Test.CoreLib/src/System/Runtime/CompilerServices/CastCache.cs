namespace System.Runtime.CompilerServices
{
    internal enum CastResult
    {
        CannotCast = 0,
        CanCast = 1,
        MaybeCast = 2
    }

    // trivial implementation of the cast cache
    internal unsafe struct CastCache
    {
        public CastCache(int initialCacheSize, int maxCacheSize)
        {
        }

        internal CastResult TryGet(nuint source, nuint target)
        {
            return CastResult.MaybeCast;
        }

        internal void TrySet(nuint source, nuint target, bool result)
        {
        }
    }
}
