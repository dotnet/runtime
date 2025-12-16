// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using SharedTypes.ComInterfaces;

namespace NativeExports.ComInterfaceGenerator
{
    public static unsafe class UniqueMarshalling
    {
        private static void* s_cachedPtr = null;
        // To handle the final release of the cached pointer
        private static ComObject s_cachedObject = null;

        // Call from another assembly to get a ptr to make an RCW
        [UnmanagedCallersOnly(EntryPoint = "new_unique_marshalling")]
        public static void* CreateComObject()
        {
            if (s_cachedPtr == null)
            {
                StrategyBasedComWrappers wrappers = new();
                var myObject = new SharedTypes.ComInterfaces.UniqueMarshalling();
                nint ptr = wrappers.GetOrCreateComInterfaceForObject(myObject, CreateComInterfaceFlags.None);
                s_cachedObject = (ComObject)wrappers.GetOrCreateObjectForComInstance(ptr, CreateObjectFlags.None);
                s_cachedPtr = (void*)ptr;
            }

            // AddRef before returning - caller will Release
            Marshal.AddRef((nint)s_cachedPtr);
            return s_cachedPtr;
        }
    }
}
