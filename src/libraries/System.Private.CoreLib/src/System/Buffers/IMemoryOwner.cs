// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Buffers
{
#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved (System.Buffers.MemoryPool{T}.Rent)
    /// <summary>Identifies the owner of a block of memory who is responsible for disposing of the underlying memory appropriately.</summary>
    /// <typeparam name="T">The type of elements to store in memory.</typeparam>
    /// <remarks>The <see cref="IMemoryOwner{T}" /> interface is used to define the owner responsible for the lifetime management of a <see cref="System.Memory{T}" /> buffer. An instance of the <see cref="IMemoryOwner{T}" /> interface is returned by the <see cref="System.Buffers.MemoryPool{T}.Rent" /> method.
    /// While a buffer can have multiple consumers, it can only have a single owner at any given time. The owner can:
    /// <list type="bullet">
    ///   <item>Create the buffer either directly or by calling a factory method.</item>
    ///   <item>Transfer ownership to another consumer. In this case, the previous owner should no longer use the buffer.</item>
    ///   <item>Destroy the buffer when it is no longer in use.</item>
    /// </list>
    /// Because the <see cref="IMemoryOwner{T}" /> object implements the <see cref="System.IDisposable" /> interface, you should call its <see cref="System.IDisposable.Dispose" /> method only after the memory buffer is no longer needed and you have destroyed it. You should not dispose of the <see cref="IMemoryOwner{T}" /> object while a reference to its memory is available. This means that the type in which <see cref="IMemoryOwner{T}" /> is declared should not have a <see cref="object.Finalize" /> method.</remarks>
    public interface IMemoryOwner<T> : IDisposable
#pragma warning restore CS1574
    {
        /// <summary>Gets the memory belonging to this owner.</summary>
        /// <value>The memory belonging to this owner.</value>
        Memory<T> Memory { get; }
    }
}
