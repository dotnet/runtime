// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Buffers
{
    /// <summary>
    /// Contains factory methods to create <see cref="BoundedMemory{T}"/> instances.
    /// </summary>
    public static partial class BoundedMemory
    {
        /// <summary>
        /// Allocates a new <see cref="BoundedMemory{T}"/> region which is immediately preceded by
        /// or immediately followed by a poison (MEM_NOACCESS) page. If <paramref name="placement"/>
        /// is <see cref="PoisonPagePlacement.Before"/>, then attempting to read the memory
        /// immediately before the returned <see cref="BoundedMemory{T}"/> will result in an AV.
        /// If <paramref name="placement"/> is <see cref="PoisonPagePlacement.After"/>, then
        /// attempting to read the memory immediately after the returned <see cref="BoundedMemory{T}"/>
        /// will result in AV.
        /// </summary>
        /// <remarks>
        /// The newly-allocated memory will be populated with random data.
        /// </remarks>
        public static BoundedMemory<T> Allocate<T>(int elementCount, PoisonPagePlacement placement = PoisonPagePlacement.After) where T : unmanaged
        {
            if (elementCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(elementCount));
            }
            if (placement != PoisonPagePlacement.Before && placement != PoisonPagePlacement.After)
            {
                throw new ArgumentOutOfRangeException(nameof(placement));
            }

            var retVal = AllocateWithoutDataPopulation<T>(elementCount, placement);
            FillRandom(MemoryMarshal.AsBytes(retVal.Span));
            return retVal;
        }

        /// <summary>
        /// Similar to <see cref="Allocate(int, PoisonPagePlacement)"/>, but populates the allocated
        /// native memory block from existing data rather than using random data.
        /// </summary>
        public static BoundedMemory<T> AllocateFromExistingData<T>(ReadOnlySpan<T> data, PoisonPagePlacement placement = PoisonPagePlacement.After) where T : unmanaged
        {
            if (placement != PoisonPagePlacement.Before && placement != PoisonPagePlacement.After)
            {
                throw new ArgumentOutOfRangeException(nameof(placement));
            }

            var retVal = AllocateWithoutDataPopulation<T>(data.Length, placement);
            data.CopyTo(retVal.Span);
            return retVal;
        }

        /// <summary>
        /// Similar to <see cref="Allocate(int, PoisonPagePlacement)"/>, but populates the allocated
        /// native memory block from existing data rather than using random data.
        /// </summary>
        public static BoundedMemory<T> AllocateFromExistingData<T>(T[] data, PoisonPagePlacement placement = PoisonPagePlacement.After) where T : unmanaged
        {
            return AllocateFromExistingData(new ReadOnlySpan<T>(data), placement);
        }

        private static void FillRandom(Span<byte> buffer)
        {
            // Loop over a Random instance manually since Random.NextBytes(Span<byte>) doesn't
            // exist on all platforms we target.

            Random random = new Random(); // doesn't need to be cryptographically strong

            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = (byte)random.Next();
            }
        }
    }
}
