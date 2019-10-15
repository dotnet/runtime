// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// Padding.cs
//
//
// Helper structs for padding over CPU cache lines to avoid false sharing.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Runtime.InteropServices;
using Internal;

namespace System.Threading.Tasks.Dataflow.Internal
{
    /// <summary>Value type that contains single Int64 value padded on both sides.</summary>
    [StructLayout(LayoutKind.Explicit, Size = 2 * PaddingHelpers.CACHE_LINE_SIZE)]
    internal struct PaddedInt64
    {
        [FieldOffset(PaddingHelpers.CACHE_LINE_SIZE)]
        internal long Value;
    }
}
