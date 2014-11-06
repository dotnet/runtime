// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Validation;

namespace System.Collections.Immutable
{
    /// <summary>
    /// Object pooling utilities.
    /// </summary>
    internal class SecureObjectPool
    {
        /// <summary>
        /// The ever-incrementing (and wrap-on-overflow) integer for owner id's.
        /// </summary>
        private static int poolUserIdCounter;

        /// <summary>
        /// The ID reserved for unassigned objects.
        /// </summary>
        internal const int UnassignedId = -1;

        /// <summary>
        /// Returns a new ID.
        /// </summary>
        internal static int NewId()
        {
            int result = Interlocked.Increment(ref poolUserIdCounter);
            if (result == UnassignedId)
            {
                result = Interlocked.Increment(ref poolUserIdCounter);
            }

            return result;
        }
    }

    internal class SecureObjectPool<T, TCaller>
        where TCaller : ISecurePooledObjectUser
    {
        private AllocFreeConcurrentStack<SecurePooledObject<T>> pool = new AllocFreeConcurrentStack<SecurePooledObject<T>>();

        public void TryAdd(TCaller caller, SecurePooledObject<T> item)
        {
            lock (item)
            {
                // Only allow the caller to recycle this object if it is the current owner.
                if (caller.PoolUserId == item.Owner)
                {
                    item.Owner = SecureObjectPool.UnassignedId;
                    this.pool.TryAdd(item);
                }
            }
        }

        public bool TryTake(TCaller caller, out SecurePooledObject<T> item)
        {
            if (caller.PoolUserId != SecureObjectPool.UnassignedId && this.pool.TryTake(out item))
            {
                item.Owner = caller.PoolUserId;
                return true;
            }
            else
            {
                item = null;
                return false;
            }
        }

        public SecurePooledObject<T> PrepNew(TCaller caller, T newValue)
        {
            Requires.NotNullAllowStructs(newValue, "newValue");
            var pooledObject = new SecurePooledObject<T>(newValue);
            pooledObject.Owner = caller.PoolUserId;
            return pooledObject;
        }
    }

    internal interface ISecurePooledObjectUser
    {
        int PoolUserId { get; }
    }

    internal class SecurePooledObject<T>
    {
        private readonly T value;
        private int owner;

        internal SecurePooledObject(T newValue)
        {
            Requires.NotNullAllowStructs(newValue, "newValue");
            this.value = newValue;
        }

        internal int Owner
        {
            get
            {
                lock (this)
                {
                    return this.owner;
                }
            }

            set
            {
                lock (this)
                {
                    this.owner = value;
                }
            }
        }

        internal SecurePooledObjectUser Use<TCaller>(TCaller caller)
            where TCaller : ISecurePooledObjectUser
        {
            this.ThrowDisposedIfNotOwned(caller);
            return new SecurePooledObjectUser(this);
        }

        internal void ThrowDisposedIfNotOwned<TCaller>(TCaller caller)
            where TCaller : ISecurePooledObjectUser
        {
            if (caller.PoolUserId != this.owner)
            {
                throw new ObjectDisposedException(caller.GetType().FullName);
            }
        }

        internal struct SecurePooledObjectUser : IDisposable
        {
            private readonly SecurePooledObject<T> value;

            internal SecurePooledObjectUser(SecurePooledObject<T> value)
            {
                this.value = value;
                Monitor.Enter(value);
            }

            internal T Value
            {
                get { return this.value.value; }
            }

            public void Dispose()
            {
                Monitor.Exit(value);
            }
        }
    }
}
