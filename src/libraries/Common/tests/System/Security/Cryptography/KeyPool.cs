// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;

namespace System.Security.Cryptography.Tests
{
    internal class KeyPool<TKey, TDiscriminator> where TKey : AsymmetricAlgorithm
    {
        private readonly List<(TDiscriminator Discriminator, ConcurrentStack<TKey> Pool)> _pools = new();
        private readonly IEqualityComparer<TDiscriminator> _comparer;

        internal KeyPool(IEqualityComparer<TDiscriminator>? comparer = null)
        {
            _comparer = comparer ?? EqualityComparer<TDiscriminator>.Default;
        }

        private ConcurrentStack<TKey> GetPool(TDiscriminator discriminator)
        {
            static ConcurrentStack<TKey>? FindPool(
                KeyPool<TKey, TDiscriminator> pool,
                TDiscriminator discriminator)
            {
                foreach ((TDiscriminator id, ConcurrentStack<TKey> value) in pool._pools)
                {
                    if (pool._comparer.Equals(discriminator, id))
                    {
                        return value;
                    }
                }

                return null;
            }

            ConcurrentStack<TKey>? pool = FindPool(this, discriminator);

            if (pool is null)
            {
                lock (_pools)
                {
                    pool = FindPool(this, discriminator);

                    if (pool is null)
                    {
                        pool = new ConcurrentStack<TKey>();

                        _pools.Add((discriminator, pool));
                    }
                }
            }

            return pool;
        }

        internal KeyPoolLease Rent(
            TDiscriminator discriminator,
            Func<TDiscriminator, TKey> creator)
        {
            ConcurrentStack<TKey> pool = GetPool(discriminator);

            if (!pool.TryPop(out TKey rented))
            {
                rented = creator(discriminator);
            }

            return new KeyPoolLease(rented, discriminator, this);
        }

        private void Return(TKey key, TDiscriminator discriminator)
        {
            GetPool(discriminator).Push(key);
        }

        internal class KeyPoolLease : IDisposable
        {
            private readonly KeyPool<TKey, TDiscriminator> _pool;
            private readonly TDiscriminator _discriminator;

            internal TKey Key { get; private set; }

            internal KeyPoolLease(
                TKey key,
                TDiscriminator discriminator,
                KeyPool<TKey, TDiscriminator> pool)
            {
                Key = key;
                _discriminator = discriminator;
                _pool = pool;
            }

            public void Dispose()
            {
                _pool.Return(Key, _discriminator);
                Key = null!;
            }
        }
    }
}
