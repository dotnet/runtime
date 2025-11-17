// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using SharedTypes.ComInterfaces;

namespace NativeExports.ComInterfaceGenerator
{
    public static unsafe class UniqueMarshalling
    {
        // Call from another assembly to get a ptr to make an RCW
        [UnmanagedCallersOnly(EntryPoint = "new_unique_marshalling")]
        public static void* CreateComObject()
        {
            StrategyBasedComWrappers wrappers = new();
            var myObject = new SharedTypes.ComInterfaces.UniqueMarshalling();
            nint ptr = wrappers.GetOrCreateComInterfaceForObject(myObject, CreateComInterfaceFlags.None);

            return (void*)ptr;
        }
    }
}
