// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// a set of lightweight static helpers for lazy initialization.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

#nullable enable
namespace System.Threading
{
    /// <summary>
    /// Specifies how a <see cref="T:System.Threading.Lazy{T}"/> instance should synchronize access among multiple threads.
    /// </summary>
    public enum LazyThreadSafetyMode
    {
        /// <summary>
        /// This mode makes no guarantees around the thread-safety of the <see cref="T:System.Threading.Lazy{T}"/> instance.  If used from multiple threads, the behavior of the <see cref="T:System.Threading.Lazy{T}"/> is undefined.
        /// This mode should be used when a <see cref="T:System.Threading.Lazy{T}"/> is guaranteed to never be initialized from more than one thread simultaneously and high performance is crucial. 
        /// If valueFactory throws an exception when the <see cref="T:System.Threading.Lazy{T}"/> is initialized, the exception will be cached and returned on subsequent accesses to Value. Also, if valueFactory recursively
        /// accesses Value on this <see cref="T:System.Threading.Lazy{T}"/> instance, a <see cref="T:System.InvalidOperationException"/> will be thrown.
        /// </summary>
        None,

        /// <summary>
        /// When multiple threads attempt to simultaneously initialize a <see cref="T:System.Threading.Lazy{T}"/> instance, this mode allows each thread to execute the
        /// valueFactory but only the first thread to complete initialization will be allowed to set the final value of the  <see cref="T:System.Threading.Lazy{T}"/>.
        /// Once initialized successfully, any future calls to Value will return the cached result.  If valueFactory throws an exception on any thread, that exception will be
        /// propagated out of Value. If any thread executes valueFactory without throwing an exception and, therefore, successfully sets the value, that value will be returned on
        /// subsequent accesses to Value from any thread.  If no thread succeeds in setting the value, IsValueCreated will remain false and subsequent accesses to Value will result in
        /// the valueFactory delegate re-executing.  Also, if valueFactory recursively accesses Value on this  <see cref="T:System.Threading.Lazy{T}"/> instance, an exception will NOT be thrown.
        /// </summary>
        PublicationOnly,

        /// <summary>
        /// This mode uses locks to ensure that only a single thread can initialize a <see cref="T:System.Threading.Lazy{T}"/> instance in a thread-safe manner.  In general,
        /// taken if this mode is used in conjunction with a <see cref="T:System.Threading.Lazy{T}"/> valueFactory delegate that uses locks internally, a deadlock can occur if not
        /// handled carefully.  If valueFactory throws an exception when the<see cref="T:System.Threading.Lazy{T}"/> is initialized, the exception will be cached and returned on
        /// subsequent accesses to Value. Also, if valueFactory recursively accesses Value on this <see cref="T:System.Threading.Lazy{T}"/> instance, a  <see cref="T:System.InvalidOperationException"/> will be thrown.
        /// </summary>
        ExecutionAndPublication
    }
}
