// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This type is for the COM source generator and implements part of the COM-specific interactions.
// This API need to be exposed to implement the COM source generator in one form or another.

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// IUnknown interaction strategy.
    /// </summary>
    [CLSCompliant(false)]
    public unsafe interface IIUnknownStrategy
    {
        /// <summary>
        /// Create an instance pointer that represents the provided IUnknown instance.
        /// </summary>
        /// <param name="unknown">The IUnknown instance.</param>
        /// <returns>A pointer representing the unmanaged instance.</returns>
        /// <remarks>
        /// This method is used to create an instance pointer that can be used to interact with the other members of this interface.
        /// For example, this method can return an IAgileReference instance for the provided IUnknown instance
        /// that can be used in the QueryInterface and Release methods to enable creating thread-local instance pointers to us
        /// through the IAgileReference APIs instead of directly calling QueryInterface on the IUnknown.
        /// </remarks>
        public void* CreateInstancePointer(void* unknown);

        /// <summary>
        /// Perform a QueryInterface() for an IID on the unmanaged instance.
        /// </summary>
        /// <param name="instancePtr">A pointer representing the unmanaged instance.</param>
        /// <param name="iid">The IID (Interface ID) to query for.</param>
        /// <param name="ppObj">The resulting interface</param>
        /// <returns>Returns an HRESULT represents the success of the operation</returns>
        /// <seealso cref="Marshal.QueryInterface(nint, ref Guid, out nint)"/>
        public int QueryInterface(void* instancePtr, in Guid iid, out void* ppObj);

        /// <summary>
        /// Perform a Release() call on the supplied unmanaged instance.
        /// </summary>
        /// <param name="instancePtr">A pointer representing the unmanaged instance.</param>
        /// <returns>The current reference count.</returns>
        /// <seealso cref="Marshal.Release(nint)"/>
        public int Release(void* instancePtr);
    }
}
