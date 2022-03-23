// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Optional features supported by custom type marshallers.
    /// </summary>
    [Flags]
#if LIBRARYIMPORT_GENERATOR_TEST
    public
#else
    internal
#endif
    enum CustomTypeMarshallerFeatures
    {
        /// <summary>
        /// No optional features supported
        /// </summary>
        None = 0,
        /// <summary>
        /// The marshaller owns unmanaged resources that must be freed
        /// </summary>
        UnmanagedResources = 0x1,
        /// <summary>
        /// The marshaller can use a caller-allocated buffer instead of allocating in some scenarios
        /// </summary>
        CallerAllocatedBuffer = 0x2,
        /// <summary>
        /// The marshaller uses the two-stage marshalling design for its <see cref="CustomTypeMarshallerKind"/> instead of the one-stage design.
        /// </summary>
        TwoStageMarshalling = 0x4
    }
}
