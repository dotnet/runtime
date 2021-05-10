// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Collections.Immutable
{
    /// <summary>
    /// Object pooling utilities.
    /// </summary>
    internal static class SecureObjectPool
    {
        /// <summary>
        /// The ever-incrementing (and wrap-on-overflow) integer for owner id's.
        /// </summary>
        private static int s_poolUserIdCounter;

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
                result = Interlocked.Increment(ref s_poolUserIdCounter);
            }
            while (result == UnassignedId);

            return result;
        }
    }

    internal sealed class SecureObjectPool<T, TCaller>
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

        public bool TryTake(TCaller caller, out SecurePooledObject<T>? item)
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
            Requires.NotNullAllowStructs(newValue, nameof(newValue));
            var pooledObject = new SecurePooledObject<T>(newValue);
            pooledObject.Owner = caller.PoolUserId;
            return pooledObject;
        }
    }

    internal interface ISecurePooledObjectUser
    {
        int PoolUserId { get; }
    }

    internal sealed class SecurePooledObject<T>
    {
        private readonly T _value;
        private int _owner;

        internal SecurePooledObject(T newValue)
        {
            Requires.NotNullAllowStructs(newValue, nameof(newValue));
            _value = newValue;
        }

        /// <summary>
        /// Gets or sets the current owner of this recyclable object.
        /// </summary>
        internal int Owner
        {
            get { return _owner; }
            set { _owner = value; }
        }

        /// <summary>
        /// Returns the recyclable value if it hasn't been reclaimed already.
        /// </summary>
        /// <typeparam name="TCaller">The type of renter of the object.</typeparam>
        /// <param name="caller">The renter of the object.</param>
        /// <returns>The rented object.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if <paramref name="caller"/> is no longer the renter of the value.</exception>
        internal T Use<TCaller>(ref TCaller caller)
            where TCaller : struct, ISecurePooledObjectUser
        {
            if (!IsOwned(ref caller))
                Requires.FailObjectDisposed(caller);
            return _value;
        }

        internal bool TryUse<TCaller>(ref TCaller caller, [MaybeNullWhen(false)] out T value)
            where TCaller : struct, ISecurePooledObjectUser
        {
            if (IsOwned(ref caller))
            {
                value = _value;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsOwned<TCaller>(ref TCaller caller)
            where TCaller : struct, ISecurePooledObjectUser
        {
            return caller.PoolUserId == _owner;
        }
    }
}
