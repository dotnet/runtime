// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices
{
    public abstract partial class ComWrappers
    {
        public partial struct ComInterfaceDispatch
        {
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
