// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices
{
    [UnsupportedOSPlatform("android")]
    [UnsupportedOSPlatform("browser")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [CLSCompliant(false)]
    public abstract class ComWrappers
    {
        public struct ComInterfaceEntry
        {
            public Guid IID;

            public IntPtr Vtable;
        }

        protected abstract unsafe ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count);

        protected abstract object? CreateObject(IntPtr externalComObject, CreateObjectFlags flags);

        protected virtual object? CreateObject(IntPtr externalComObject, CreateObjectFlags flags, object? userState, out CreatedWrapperFlags wrapperFlags)
        {
            throw new PlatformNotSupportedException();
        }

        protected internal abstract void ReleaseObjects(IEnumerable objects);

        public static unsafe bool TryGetComInstance(object obj, out IntPtr unknown)
        {
            unknown = default;
            return false;
        }

        public static unsafe bool TryGetObject(IntPtr unknown, [NotNullWhen(true)] out object? obj)
        {
            obj = default;
            return false;
        }

        public struct ComInterfaceDispatch
        {
            public IntPtr Vtable;
            public static unsafe T GetInstance<T>(ComInterfaceDispatch* dispatchPtr) where T : class
            {
                throw new PlatformNotSupportedException();
            }
        }

        public IntPtr GetOrCreateComInterfaceForObject(object instance, CreateComInterfaceFlags flags)
        {
            throw new PlatformNotSupportedException();
        }

        public object GetOrCreateObjectForComInstance(IntPtr externalComObject, CreateObjectFlags flags)
        {
            throw new PlatformNotSupportedException();
        }

        public object GetOrCreateObjectForComInstance(IntPtr externalComObject, CreateObjectFlags flags, object? userState)
        {
            throw new PlatformNotSupportedException();
        }

        public object GetOrRegisterObjectForComInstance(IntPtr externalComObject, CreateObjectFlags flags, object wrapper)
        {
            throw new PlatformNotSupportedException();
        }

        public object GetOrRegisterObjectForComInstance(IntPtr externalComObject, CreateObjectFlags flags, object wrapper, IntPtr inner)
        {
            throw new PlatformNotSupportedException();
        }

        public static void RegisterForTrackerSupport(ComWrappers instance)
        {
            throw new PlatformNotSupportedException();
        }

        [SupportedOSPlatform("windows")]
        public static void RegisterForMarshalling(ComWrappers instance)
        {
            throw new PlatformNotSupportedException();
        }

        public static void GetIUnknownImpl(out IntPtr fpQueryInterface, out IntPtr fpAddRef, out IntPtr fpRelease)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
