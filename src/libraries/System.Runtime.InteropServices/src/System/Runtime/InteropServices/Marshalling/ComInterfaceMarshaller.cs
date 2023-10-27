// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// COM interface marshaller using a <see cref="StrategyBasedComWrappers" /> instance
    /// </summary>
    /// <remarks>
    /// This marshaller will always pass the <see cref="CreateObjectFlags.Unwrap"/> flag
    /// to <see cref="ComWrappers.GetOrCreateObjectForComInstance(IntPtr, CreateObjectFlags)"/>.
    /// </remarks>
    /// <typeparam name="T">The managed type that represents a COM interface type</typeparam>
    [UnsupportedOSPlatform("android")]
    [UnsupportedOSPlatform("browser")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [CLSCompliant(false)]
    [CustomMarshaller(typeof(CustomMarshallerAttribute.GenericPlaceholder), MarshalMode.Default, typeof(ComInterfaceMarshaller<>))]
    public static unsafe class ComInterfaceMarshaller<T>
    {
        private static readonly Guid? TargetInterfaceIID = StrategyBasedComWrappers.DefaultIUnknownInterfaceDetailsStrategy.GetIUnknownDerivedDetails(typeof(T).TypeHandle)?.Iid;

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
            return CastIUnknownToInterfaceType(unknown);
        }

        /// <summary>
        /// Convert a COM interface pointer to a managed object.
        /// </summary>
        /// <param name="unmanaged">The COM interface pointer</param>
        /// <remarks>
        /// If the passed in COM interface pointer wraps a managed object, this method returns the underlying object.
        /// </remarks>
        public static T? ConvertToManaged(void* unmanaged)
        {
            if (unmanaged == null)
            {
                return default;
            }
            return (T)StrategyBasedComWrappers.DefaultMarshallingInstance.GetOrCreateObjectForComInstance((nint)unmanaged, CreateObjectFlags.Unwrap);
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

        internal static void* CastIUnknownToInterfaceType(nint unknown)
        {
            if (TargetInterfaceIID is null)
            {
                // If the managed type isn't a GeneratedComInterface-attributed type, we'll marshal to an IUnknown*.
                return (void*)unknown;
            }
            if (Marshal.QueryInterface(unknown, in Nullable.GetValueRefOrDefaultRef(in TargetInterfaceIID), out nint interfacePointer) != 0)
            {
                Marshal.Release(unknown);
                throw new InvalidCastException($"Unable to cast the provided managed object to a COM interface with ID '{TargetInterfaceIID.GetValueOrDefault():B}'");
            }
            Marshal.Release(unknown);
            return (void*)interfacePointer;
        }
    }
}
