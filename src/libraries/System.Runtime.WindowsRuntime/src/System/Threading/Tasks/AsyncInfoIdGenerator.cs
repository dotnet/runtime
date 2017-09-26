// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace System.Threading.Tasks
{
    /// <summary>
    /// Reusable component to generate unique IDs for ann the different implementations of IAsyncInfo in this assembly.
    /// </summary>
    internal static class AsyncInfoIdGenerator
    {
        /// <summary>
        /// We will never generate this Id, so this value can be used as an invalid, uninitialised or a <em>no-Id</em> value.
        /// </summary>
        internal const UInt32 InvalidId = Int32.MaxValue;


        /// <summary>
        /// We want to avoid ending up with the same ID as a Windows-implemented async info.
        /// At the same time we want to be reproducible. So we use a random generator with a fixed seed.
        /// </summary>
        private static Random s_idGenerator = new Random(19830118);


        /// <summary>
        /// Generate a unique ID that can be used for an IAsyncInfo object.
        /// The returned value will never be equal to <code>AsyncInfoIdGenerator.InvalidId</code>.
        /// </summary>
        /// <returns>A new unique IAsyncInfo Id.</returns>
        internal static UInt32 CreateNext()
        {
            lock (s_idGenerator)
            {
                Int32 newId = s_idGenerator.Next(1, (Int32)InvalidId);  // Valid IDs will be larger than zero and smaller than InvalidId
                return unchecked((UInt32)newId);
            }
        }


        /// <summary>
        /// Initialises the specified <code>id</code> to a unique Id-value that can be used for an IAsyncInfo object under the
        /// assumption that another thread may also attempt to initialise <code>id</code>. The thread that changes <code>id</code>
        /// first from <code>AsyncInfoIdGenerator.InvalidId</code> to another value wins and all other threads will respect that
        /// choice and leave <code>id</code> unchanged. The method returns the Id that was agreed upon by the race.
        /// </summary>
        /// <param name="id">The IAsyncInfo ID to initialise.</param>
        /// <returns>The unique value to which the specified reference target was initialised.</returns>
        internal static UInt32 EnsureInitializedThreadsafe(ref UInt32 id)
        {
            if (id != InvalidId)
                return id;

            UInt32 newId = CreateNext();

            // There is no overload of Interlocked.CompareExchange that accepts an UInt32.
            // We apply some pointer tricks to pass the arguments to the overload that takes an Int32.
            // In clear-text, the following unsafe/fixed statement does this:
            //     UInt32 asyncIdVal = Interlocked.CompareExchange(ref id, newId, InvalidId);
            //     if (asyncIdVal == InvalidId)
            //         return newId;

            unsafe
            {
                fixed (UInt32* idPtr = &id)
                {
                    UInt32 asyncIdVal = unchecked((UInt32)Interlocked.CompareExchange(ref *(Int32*)idPtr, (Int32)newId, (Int32)InvalidId));
                    if (asyncIdVal == InvalidId)
                        return newId;

                    return asyncIdVal;
                }
            }
        }
    }  // class AsyncInfoIdGenerator
}  // namespace

// AsyncInfoIdGenerator.cs
