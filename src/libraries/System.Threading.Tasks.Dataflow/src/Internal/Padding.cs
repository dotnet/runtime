// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// Padding.cs
//
//
// Helper structs for padding over CPU cache lines to avoid false sharing.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Runtime.InteropServices;

namespace System.Threading.Tasks.Dataflow.Internal
{
    /// <summary>A placeholder class for common padding constants and eventually routines.</summary>
    internal static class Padding
    {
        /// <summary>A size greater than or equal to the size of the most common CPU cache lines.</summary>
        internal const int CACHE_LINE_SIZE = 128;
    }

    /// <summary>Padding structure used to minimize false sharing in SingleProducerSingleConsumerQueue{T}.</summary>
    [StructLayout(LayoutKind.Explicit, Size = Padding.CACHE_LINE_SIZE - sizeof(Int32))] // Based on common case of 64-byte cache lines
    internal struct PaddingForInt32
    {
    }

    /// <summary>Value type that contains single Int64 value padded on both sides.</summary>
    [StructLayout(LayoutKind.Explicit, Size = 2 * Padding.CACHE_LINE_SIZE)]
    internal struct PaddedInt64
    {
        [FieldOffset(Padding.CACHE_LINE_SIZE)]
        internal Int64 Value;
    }
}
