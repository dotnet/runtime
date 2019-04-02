// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace System.Buffers
{
    /// <summary>
    /// Owner of Memory<typeparamref name="T"/> that is responsible for disposing the underlying memory appropriately.
    /// </summary>
    public interface IMemoryOwner<T> : IDisposable
    {
        /// <summary>
        /// Returns a Memory<typeparamref name="T"/>.
        /// </summary>
        Memory<T> Memory { get; }
    }
}
