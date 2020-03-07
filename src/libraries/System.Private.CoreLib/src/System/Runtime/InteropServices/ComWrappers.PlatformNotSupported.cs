// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;

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
        IgnoreCache = 2,
    }

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
            public IntPtr vftbl;

            public static unsafe T GetInstance<T>(ComInterfaceDispatch* dispatchPtr) where T : class
            {
                throw new PlatformNotSupportedException();
            }
        }

        public IntPtr GetOrCreateComInterfaceForObject(object instance, CreateComInterfaceFlags flags)
        {
            throw new PlatformNotSupportedException();
        }

        protected unsafe abstract ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count);

        public object GetOrCreateObjectForComInstance(IntPtr externalComObject, CreateObjectFlags flags)
        {
            throw new PlatformNotSupportedException();
        }

        protected abstract object CreateObject(IntPtr externalComObject, IntPtr agileObjectRef, CreateObjectFlags flags);

        protected virtual void ReleaseObjects(IEnumerable objects)
        {
            throw new PlatformNotSupportedException();
        }

        public void RegisterForReferenceTrackerHost()
        {
            throw new PlatformNotSupportedException();
        }
    }
}