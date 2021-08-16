// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;

namespace System.Reflection.Internal
{
    /// <summary>
    /// The usage is:
    ///        var inst = PooledStringBuilder.GetInstance();
    ///        var sb = inst.builder;
    ///        ... Do Stuff...
    ///        ... sb.ToString() ...
    ///        inst.Free();
    /// </summary>
    internal sealed class PooledStringBuilder
    {
        public readonly StringBuilder Builder = new StringBuilder();
        private readonly ObjectPool<PooledStringBuilder> _pool;

        private PooledStringBuilder(ObjectPool<PooledStringBuilder> pool)
        {
            Debug.Assert(pool != null);
            _pool = pool;
        }

        public int Length
        {
            get { return this.Builder.Length; }
        }

        public void Free()
        {
            var builder = this.Builder;

            // do not store builders that are too large.
            if (builder.Capacity <= 1024)
            {
                builder.Clear();
                _pool.Free(this);
            }
        }

        public string ToStringAndFree()
        {
            string result = this.Builder.ToString();
            this.Free();

            return result;
        }

        // global pool
        private static readonly ObjectPool<PooledStringBuilder> s_poolInstance = CreatePool();

        // if someone needs to create a private pool;
        public static ObjectPool<PooledStringBuilder> CreatePool()
        {
            ObjectPool<PooledStringBuilder>? pool = null;
            pool = new ObjectPool<PooledStringBuilder>(() => new PooledStringBuilder(pool!), 32);
            return pool;
        }

        public static PooledStringBuilder GetInstance()
        {
            var builder = s_poolInstance.Allocate();
            Debug.Assert(builder.Builder.Length == 0);
            return builder;
        }
    }
}
