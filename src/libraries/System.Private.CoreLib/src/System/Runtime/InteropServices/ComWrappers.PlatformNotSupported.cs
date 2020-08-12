// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices
{
    [Flags]
    public enum CreateComInterfaceFlags
    {
        None = 0,
        CallerDefinedIUnknown = 1,
        TrackerSupport = 2,
    }

    [Flags]
    public enum CreateObjectFlags
    {
        None = 0,
        TrackerObject = 1,
        UniqueInstance = 2,
    }

    [SupportedOSPlatform("windows")]
    [CLSCompliant(false)]
    public abstract class ComWrappers
    {
        public struct ComInterfaceEntry
        {
            public Guid IID;
            public IntPtr Vtable;
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

        protected abstract unsafe ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count);

        public object GetOrCreateObjectForComInstance(IntPtr externalComObject, CreateObjectFlags flags)
        {
            throw new PlatformNotSupportedException();
        }

        protected abstract object? CreateObject(IntPtr externalComObject, CreateObjectFlags flags);

        public object GetOrRegisterObjectForComInstance(IntPtr externalComObject, CreateObjectFlags flags, object wrapper)
        {
            throw new PlatformNotSupportedException();
        }

        protected abstract void ReleaseObjects(IEnumerable objects);

        public static void RegisterForTrackerSupport(ComWrappers instance)
        {
            throw new PlatformNotSupportedException();
        }

        public static void RegisterForMarshalling(ComWrappers instance)
        {
            throw new PlatformNotSupportedException();
        }

        protected static void GetIUnknownImpl(out IntPtr fpQueryInterface, out IntPtr fpAddRef, out IntPtr fpRelease)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
