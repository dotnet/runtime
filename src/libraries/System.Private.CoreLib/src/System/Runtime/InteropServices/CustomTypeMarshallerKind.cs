// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// The shape of a custom type marshaller for usage in source-generated interop scenarios.
    /// </summary>
    /// <remarks>
    /// <seealso cref="LibraryImportAttribute"/>
    /// </remarks>
    public enum CustomTypeMarshallerKind
    {
        /// <summary>
        /// This custom type marshaller represents a single value.
        /// </summary>
        Value,
        /// <summary>
        /// This custom type marshaller represents a container of values that are placed sequentially in memory.
        /// </summary>
        LinearCollection
    }
}
