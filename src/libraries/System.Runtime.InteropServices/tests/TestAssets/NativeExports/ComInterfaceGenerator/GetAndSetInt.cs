// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.InteropServices.ObjectiveC;
using System.Text;
using System.Threading.Tasks;
using SharedTypes.ComInterfaces;
using static System.Runtime.InteropServices.ComWrappers;

namespace NativeExports.ComInterfaceGenerator
{
    public static unsafe class GetAndSetInt
    {
        // Call from another assembly to get a ptr to make an RCW
        [UnmanagedCallersOnly(EntryPoint = "new_get_and_set_int")]
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
            static void* s_comInterface1VTable
            {
                get
                {
                    if (MyComWrapper._s_comInterface1VTable != null)
                        return _s_comInterface1VTable;
                    void** vtable = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(GetAndSetInt), sizeof(void*) * 5);
                    GetIUnknownImpl(out var fpQueryInterface, out var fpAddReference, out var fpRelease);
                    vtable[0] = (void*)fpQueryInterface;
                    vtable[1] = (void*)fpAddReference;
                    vtable[2] = (void*)fpRelease;
                    vtable[3] = (delegate* unmanaged<void*, int*, int>)&ImplementingObject.ABI.GetInt;
                    vtable[4] = (delegate* unmanaged<void*, int, int>)&ImplementingObject.ABI.SetInt;
                    _s_comInterface1VTable = vtable;
                    return _s_comInterface1VTable;
                }
            }
            protected override ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
            {
                if (obj is ImplementingObject)
                {
                    ComInterfaceEntry* comInterfaceEntry = (ComInterfaceEntry*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(ImplementingObject), sizeof(ComInterfaceEntry));
                    comInterfaceEntry->IID = new Guid(IGetAndSetInt._guid);
                    comInterfaceEntry->Vtable = (nint)s_comInterface1VTable;
                    count = 1;
                    return comInterfaceEntry;
                }
                count = 0;
                return null;
            }

            protected override object? CreateObject(nint externalComObject, CreateObjectFlags flags) => throw new NotImplementedException();
            protected override void ReleaseObjects(IEnumerable objects) => throw new NotImplementedException();
        }

        class ImplementingObject : IGetAndSetInt
        {
            int _data = 0;

            int IGetAndSetInt.GetInt()
            {
                return _data;
            }
            void IGetAndSetInt.SetInt(int x)
            {
                _data = x;
            }

            // Provides function pointers in the COM format to use in COM VTables
            public static class ABI
            {

                [UnmanagedCallersOnly]
                public static int GetInt(void* @this, int* value)
                {
                    try
                    {
                        *value = ComInterfaceDispatch.GetInstance<IGetAndSetInt>((ComInterfaceDispatch*)@this).GetInt();
                        return 0;
                    }
                    catch (Exception e)
                    {
                        return e.HResult;
                    }
                }

                [UnmanagedCallersOnly]
                public static int SetInt(void* @this, int newValue)
                {
                    try
                    {
                        ComInterfaceDispatch.GetInstance<IGetAndSetInt>((ComInterfaceDispatch*)@this).SetInt(newValue);
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
