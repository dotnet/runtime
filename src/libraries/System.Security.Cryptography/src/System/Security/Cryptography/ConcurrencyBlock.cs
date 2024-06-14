// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace System.Security.Cryptography
{
    internal struct ConcurrencyBlock
    {
        private int _count;

        internal static Scope Enter(ref ConcurrencyBlock block)
        {
            int count = Interlocked.Increment(ref block._count);

            if (count != 1)
            {
                Interlocked.Decrement(ref block._count);
                throw new CryptographicException(SR.Cryptography_ConcurrentUseNotSupported);
            }

            return new Scope(ref block._count);
        }

        internal ref struct Scope
        {
            private ref int _parentCount;

            internal Scope(ref int parentCount)
            {
                _parentCount = ref parentCount;
            }

            internal void Dispose()
            {
                Interlocked.Decrement(ref _parentCount);
            }
        }
    }
}
