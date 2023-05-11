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
        public static void* ConvertToUnmanaged(T? managed)
        {
            if (managed == null)
            {
                return null;
            }
            nint unknown;
            if (!ComWrappers.TryGetComInstance(managed, out unknown))
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

        private static readonly Guid? TargetInterfaceIID;

        static ComInterfaceMarshaller()
        {
            if (StrategyBasedComWrappers.DefaultIUnknownInterfaceDetailsStrategy.GetIUnknownDerivedDetails(typeof(T).TypeHandle) is { } interfaceDetails)
            {
                TargetInterfaceIID = interfaceDetails.Iid;
            }
        }

        internal static void* CastIUnknownToInterfaceType(nint unknown)
        {
            if (TargetInterfaceIID is null)
            {
                return unknown;
            }
            if (Marshal.QueryInterface(unknown, ref TargetInterfaceIID, out nint interfacePointer) != 0)
            {
                throw new InvalidCastException($"Unable to cast the provided managed object to a COM interface with ID '{iid:B}'");
            }
            Marshal.Release(unknown);
            return (void*)interfacePointer;
        }
    }
}
