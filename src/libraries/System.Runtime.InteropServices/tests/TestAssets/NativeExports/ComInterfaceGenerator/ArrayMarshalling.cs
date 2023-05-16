// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;
using SharedTypes.ComInterfaces;
using static System.Runtime.InteropServices.ComWrappers;

namespace NativeExports.ComInterfaceGenerator
{

    public static unsafe class ArrayMarshalling
    {

        [UnmanagedCallersOnly(EntryPoint = "new_get_and_set_int_array")]
        public static void* CreateComObject()
        {
            MyComWrapper cw = new();
            var myObject = new ImplementingObject();
            nint ptr = cw.GetOrCreateComInterfaceForObject(myObject, CreateComInterfaceFlags.None);

            return (void*)ptr;
        }

        class MyComWrapper : ComWrappers
        {
            static void* _s_comInterface1VTable = null;
            static void* GetIntArrayVTable
            {
                get
                {
                    if (MyComWrapper._s_comInterface1VTable != null)
                        return _s_comInterface1VTable;
                    void** vtable = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(ImplementingObject), sizeof(void*) * 4);
                    GetIUnknownImpl(out var fpQueryInterface, out var fpAddReference, out var fpRelease);
                    vtable[0] = (void*)fpQueryInterface;
                    vtable[1] = (void*)fpAddReference;
                    vtable[2] = (void*)fpRelease;
                    vtable[3] = (delegate* unmanaged<void*, int**, int>)&ImplementingObject.ABI.GetInts;
                    _s_comInterface1VTable = vtable;
                    return _s_comInterface1VTable;
                }
            }
            protected override ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
            {
                if (obj is ImplementingObject)
                {
                    ComInterfaceEntry* comInterfaceEntry = (ComInterfaceEntry*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(ImplementingObject), sizeof(ComInterfaceEntry));
                    comInterfaceEntry->IID = new Guid(IGetIntArray._guid);
                    comInterfaceEntry->Vtable = (nint)GetIntArrayVTable;
                    count = 1;
                    return comInterfaceEntry;
                }
                count = 0;
                return null;
            }

            protected override object? CreateObject(nint externalComObject, CreateObjectFlags flags) => throw new NotImplementedException();
            protected override void ReleaseObjects(IEnumerable objects) => throw new NotImplementedException();
        }
        class ImplementingObject : IGetIntArray
        {
            int[] _data = new int[10] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            public int[] GetInts() => _data;

            public static class ABI
            {
                [UnmanagedCallersOnly]
                public static int GetInts(void* @this, int** values)
                {

                    try
                    {
                        int[] arr = ComInterfaceDispatch.GetInstance<IGetIntArray>((ComInterfaceDispatch*)@this).GetInts();
                        *values = (int*)Marshal.AllocCoTaskMem(sizeof(int) * arr.Length);
                        for (int i = 0; i < arr.Length; i++)
                        {
                            (*values)[i] = arr[i];
                        }
                        return 0;
                    }
                    catch (Exception e)
                    {
                        return e.HResult;
                    }
                }
            }
        }
    }
}
