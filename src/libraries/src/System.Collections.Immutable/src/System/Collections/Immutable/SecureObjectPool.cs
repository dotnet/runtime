// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
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
            int result;
            do
            {
                result = Interlocked.Increment(ref poolUserIdCounter);
            }
            while (result == UnassignedId);

            return result;
        }
    }

    internal class SecureObjectPool<T, TCaller>
        where TCaller : ISecurePooledObjectUser
    {
        public void TryAdd(TCaller caller, SecurePooledObject<T> item)
        {
            // Only allow the caller to recycle this object if it is the current owner.
            if (caller.PoolUserId == item.Owner)
            {
                item.Owner = SecureObjectPool.UnassignedId;
                AllocFreeConcurrentStack<SecurePooledObject<T>>.TryAdd(item);
            }
        }

        public bool TryTake(TCaller caller, out SecurePooledObject<T> item)
        {
            if (caller.PoolUserId != SecureObjectPool.UnassignedId && AllocFreeConcurrentStack<SecurePooledObject<T>>.TryTake(out item))
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

        /// <summary>
        /// Gets or sets the current owner of this recyclable object.
        /// </summary>
        internal int Owner
        {
            get { return this.owner; }
            set { this.owner = value; }
        }

        /// <summary>
        /// Returns the recyclable value if it hasn't been reclaimed already.
        /// </summary>
        /// <typeparam name="TCaller">The type of renter of the object.</typeparam>
        /// <param name="caller">The renter of the object.</param>
        /// <returns>The rented object.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if <paramref name="caller"/> is no longer the renter of the value.</exception>
        internal T Use<TCaller>(TCaller caller)
            where TCaller : ISecurePooledObjectUser
        {
            this.ThrowDisposedIfNotOwned(caller);
            return this.value;
        }

        internal void ThrowDisposedIfNotOwned<TCaller>(TCaller caller)
            where TCaller : ISecurePooledObjectUser
        {
            if (caller.PoolUserId != this.owner)
            {
                throw new ObjectDisposedException(caller.GetType().FullName);
            }
        }
    }
}
