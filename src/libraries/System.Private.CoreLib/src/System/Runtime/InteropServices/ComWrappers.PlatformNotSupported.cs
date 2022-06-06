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

        public nint GetOrCreateComInterfaceForObject(object instance, CreateComInterfaceFlags flags)
        {
            throw new PlatformNotSupportedException();
        }

        public object GetOrCreateObjectForComInstance(nint externalComObject, CreateObjectFlags flags)
        {
            throw new PlatformNotSupportedException();
        }

        public object GetOrRegisterObjectForComInstance(nint externalComObject, CreateObjectFlags flags, object wrapper)
        {
            throw new PlatformNotSupportedException();
        }

        public object GetOrRegisterObjectForComInstance(nint externalComObject, CreateObjectFlags flags, object wrapper, nint inner)
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

        protected static void GetIUnknownImpl(out nint fpQueryInterface, out nint fpAddRef, out nint fpRelease)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
