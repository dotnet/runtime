// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Represents a union type.
    /// </summary>
    public interface IUnion
    {
        /// <summary>
        /// Gets the value of the union, or <see langword="null" />.
        /// </summary>
        object? Value { get; }
    }
}
