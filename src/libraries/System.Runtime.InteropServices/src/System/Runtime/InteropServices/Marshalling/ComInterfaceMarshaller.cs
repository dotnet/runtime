// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// COM interface marshaller using a StrategyBasedComWrappers instance
    /// </summary>
    /// <remarks>
    /// This marshaller will always pass the <see cref="CreateObjectFlags.Unwrap"/> flag
    /// to <see cref="ComWrappers.GetOrCreateObjectForComInstance(IntPtr, CreateObjectFlags)"/>.
    /// </remarks>
    [UnsupportedOSPlatform("android")]
    [UnsupportedOSPlatform("browser")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [CLSCompliant(false)]
    [CustomMarshaller(typeof(CustomMarshallerAttribute.GenericPlaceholder), MarshalMode.Default, typeof(ComInterfaceMarshaller<>))]
    public static unsafe class ComInterfaceMarshaller<T>
    {
        private static readonly Guid? TargetInterfaceIID = StrategyBasedComWrappers.DefaultIUnknownInterfaceDetailsStrategy.GetIUnknownDerivedDetails(typeof(T).TypeHandle)?.Iid;

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

        public static T? ConvertToManaged(void* unmanaged)
        {
            if (unmanaged == null)
            {
                return default;
            }
            return (T)StrategyBasedComWrappers.DefaultMarshallingInstance.GetOrCreateObjectForComInstance((nint)unmanaged, CreateObjectFlags.Unwrap);
        }

        internal static void* CastIUnknownToInterfaceType(nint unknown)
        {
            if (TargetInterfaceIID is null)
            {
                // If the managed type isn't a GeneratedComInterface-attributed type, we'll marshal to an IUnknown*.
                return (void*)unknown;
            }
            Guid iid = TargetInterfaceIID.Value;
            if (Marshal.QueryInterface(unknown, ref iid, out nint interfacePointer) != 0)
            {
                Marshal.Release(unknown);
                throw new InvalidCastException($"Unable to cast the provided managed object to a COM interface with ID '{iid:B}'");
            }
            Marshal.Release(unknown);
            return (void*)interfacePointer;
        }
    }
}
