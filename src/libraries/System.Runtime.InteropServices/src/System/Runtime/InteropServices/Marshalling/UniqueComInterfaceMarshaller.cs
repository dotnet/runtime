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
    [UnsupportedOSPlatform("android")]
    [UnsupportedOSPlatform("browser")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [CLSCompliant(false)]
    [CustomMarshaller(typeof(CustomMarshallerAttribute.GenericPlaceholder), MarshalMode.Default, typeof(UniqueComInterfaceMarshaller<>))]
    public static unsafe class UniqueComInterfaceMarshaller<T>
    {
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

        public static T? ConvertToManaged(void* unmanaged)
        {
            if (unmanaged == null)
            {
                return default;
            }
            return (T)StrategyBasedComWrappers.DefaultMarshallingInstance.GetOrCreateObjectForComInstance((nint)unmanaged, CreateObjectFlags.Unwrap | CreateObjectFlags.UniqueInstance);
        }
    }
}
