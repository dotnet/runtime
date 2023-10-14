// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// COM interface marshaller using a StrategyBasedComWrappers instance
    /// that will only create unique native object wrappers (RCW).
    /// </summary>
    /// <remarks>
    /// This marshaller will always pass the <see cref="CreateObjectFlags.Unwrap"/> and <see cref="CreateObjectFlags.UniqueInstance"/> flags
    /// to <see cref="ComWrappers.GetOrCreateObjectForComInstance(IntPtr, CreateObjectFlags)"/>.
    /// </remarks>
    /// <typeparam name="T">The managed type that represents a COM interface type</typeparam>
    [UnsupportedOSPlatform("android")]
    [UnsupportedOSPlatform("browser")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [CLSCompliant(false)]
    [CustomMarshaller(typeof(CustomMarshallerAttribute.GenericPlaceholder), MarshalMode.Default, typeof(UniqueComInterfaceMarshaller<>))]
    public static unsafe class UniqueComInterfaceMarshaller<T>
    {
        /// <summary>
        /// Convert a managed object to a COM interface pointer for the COM interface represented by <typeparamref name="T"/>.
        /// </summary>
        /// <param name="managed">The managed object</param>
        /// <returns>The COM interface pointer</returns>
        public static void* ConvertToUnmanaged(T? managed)
        {
            if (managed == null)
            {
                return null;
            }
            if (!ComWrappers.TryGetComInstance(managed, out nint unknown))
            {
                unknown = StrategyBasedComWrappers.DefaultMarshallingInstance.GetOrCreateComInterfaceForObject(managed, CreateComInterfaceFlags.None);
            }
            return ComInterfaceMarshaller<T>.CastIUnknownToInterfaceType(unknown);
        }


        /// <summary>
        /// Convert a COM interface pointer to a managed object.
        /// </summary>
        /// <param name="unmanaged">The COM interface pointer</param>
        /// <returns>A managed object that represents the passed in COM interface pointer.</returns>
        /// <remarks>
        /// If the passed in COM interface pointer wraps a managed object, this method returns the underlying object.
        /// </remarks>
        public static T? ConvertToManaged(void* unmanaged)
        {
            if (unmanaged == null)
            {
                return default;
            }
            return (T)StrategyBasedComWrappers.DefaultMarshallingInstance.GetOrCreateObjectForComInstance((nint)unmanaged, CreateObjectFlags.UniqueInstance);
        }


        /// <summary>
        /// Release a reference to the COM interface pointer.
        /// </summary>
        /// <param name="unmanaged">A COM interface pointer.</param>
        public static void Free(void* unmanaged)
        {
            if (unmanaged != null)
            {
                Marshal.Release((nint)unmanaged);
            }
        }
    }
}
