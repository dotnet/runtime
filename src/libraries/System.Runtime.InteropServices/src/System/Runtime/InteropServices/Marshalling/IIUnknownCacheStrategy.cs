// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This type is for the COM source generator and implements part of the COM-specific interactions.
// This API need to be exposed to implement the COM source generator in one form or another.

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// Unmanaged virtual method table look up strategy.
    /// </summary>
    [CLSCompliant(false)]
    public unsafe interface IIUnknownCacheStrategy
    {
        /// <summary>
        /// A structure that contains information about a COM interface and its virtual method table.
        /// </summary>
        public readonly struct TableInfo
        {
            /// <summary>
            /// The <c>this</c> pointer of the unmanaged interface.
            /// </summary>
            public void* ThisPtr { get; init; }
            /// <summary>
            /// A pointer to the virtual method table for this unmanaged interface.
            /// </summary>
            public void** Table { get; init; }
            /// <summary>
            /// The managed type that provides a managed projection of the unmanaged interface.
            /// </summary>
            public RuntimeTypeHandle ManagedType { get; init; }
        }

        /// <summary>
        /// Construct a <see cref="TableInfo"/> instance.
        /// </summary>
        /// <param name="handle">RuntimeTypeHandle instance</param>
        /// <param name="interfaceDetails">An <see cref="IIUnknownDerivedDetails"/> instance</param>
        /// <param name="ptr">Pointer to the instance to query</param>
        /// <returns>The constructed <see cref="TableInfo"/> instance for the provided information.</returns>
        TableInfo ConstructTableInfo(RuntimeTypeHandle handle, IIUnknownDerivedDetails interfaceDetails, void* ptr);

        /// <summary>
        /// Get associated <see cref="TableInfo"/>.
        /// </summary>
        /// <param name="handle">RuntimeTypeHandle instance</param>
        /// <param name="info">A <see cref="TableInfo"/> instance</param>
        /// <returns>True if found, otherwise false.</returns>
        bool TryGetTableInfo(RuntimeTypeHandle handle, out TableInfo info);

        /// <summary>
        /// Set associated <see cref="TableInfo"/>.
        /// </summary>
        /// <param name="handle">RuntimeTypeHandle instance</param>
        /// <param name="info">A <see cref="TableInfo"/> instance</param>
        /// <returns>True if set, otherwise false.</returns>
        bool TrySetTableInfo(RuntimeTypeHandle handle, TableInfo info);

        /// <summary>
        /// Clear the cache
        /// </summary>
        /// <param name="unknownStrategy">The <see cref="IIUnknownStrategy"/> to use for clearing</param>
        void Clear(IIUnknownStrategy unknownStrategy);
    }
}
