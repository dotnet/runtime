// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ObjectiveC;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.ComWrappers;

namespace NativeExports;

public static unsafe class ComInterfaceGeneratorExports
{
    interface IComInterface1
    {
        public int GetData();

        public void SetData(int x);

        public static Guid IID = new Guid("2c3f9903-b586-46b1-881b-adfce9af47b1");
    }

    // Call from another assembly to get a ptr to make an RCW
    [UnmanagedCallersOnly(EntryPoint = "get_com_object")]
    public static void* CreateComObject()
    {
        MyComWrapper cw = new();
        var myObject = new MyObject();
        nint ptr = cw.GetOrCreateComInterfaceForObject(myObject, CreateComInterfaceFlags.None);

        return (void*)ptr;
    }

    class MyComWrapper : System.Runtime.InteropServices.ComWrappers
    {
        static void* _s_comInterface1VTable = null;
        static void* s_comInterface1VTable
        {
            get
            {
                if (MyComWrapper._s_comInterface1VTable != null)
                    return _s_comInterface1VTable;
                void** vtable = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(ComInterfaceGeneratorExports), sizeof(void*) * 5);
                GetIUnknownImpl(out var fpQueryInterface, out var fpAddReference, out var fpRelease);
                vtable[0] = (void*)fpQueryInterface;
                vtable[1] = (void*)fpAddReference;
                vtable[2] = (void*)fpRelease;
                vtable[3] = (delegate* unmanaged<void*, int*, int>)&MyObject.ABI.GetData;
                vtable[4] = (delegate* unmanaged<void*, int, int>)&MyObject.ABI.SetData;
                _s_comInterface1VTable = vtable;
                return _s_comInterface1VTable;
            }
        }
        protected override ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
        {
            if (obj is MyObject)
            {
                ComInterfaceEntry* comInterfaceEntry = (ComInterfaceEntry*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(MyObject), sizeof(ComInterfaceEntry));
                comInterfaceEntry->IID = IComInterface1.IID;
                comInterfaceEntry->Vtable = (nint)s_comInterface1VTable;
                count = 1;
                return comInterfaceEntry;
            }
            count = 0;
            return null;
        }
        protected override object CreateObject(nint ptr, CreateObjectFlags flags)
        {
            int hr = Marshal.QueryInterface(ptr, ref IComInterface1.IID, out IntPtr IComInterfaceImpl);
            if (hr != 0)
            {
                return null;
            }
            return new IComInterface1Impl(ptr);
        }

        protected override void ReleaseObjects(IEnumerable objects) { }
    }

    // Wrapper for calling CCWs from the ComInterfaceGenerator
    class IComInterface1Impl : IComInterface1
    {
        nint _ptr;

        public IComInterface1Impl(nint @this)
        {
            _ptr = @this;
        }

        int GetData(nint inst)
        {
            int value;
            int hr = ((delegate* unmanaged<nint, int*, int>)(*(*(void***)inst + 3)))(inst, &value);
            if (hr != 0)
            {
                Marshal.GetExceptionForHR(hr);
            }
            return value;
        }

        void SetData(nint inst, int newValue)
        {
            int hr = ((delegate* unmanaged<nint, int, int>)(*(*(void***)inst + 4)))(inst, newValue);
            if (hr != 0)
            {
                Marshal.GetExceptionForHR(hr);
            }
        }

        int IComInterface1.GetData() => GetData(_ptr);

        void IComInterface1.SetData(int newValue) => SetData(_ptr, newValue);
    }

    class MyObject : IComInterface1
    {
        int _data = 0;

        int IComInterface1.GetData()
        {
            return _data;
        }
        void IComInterface1.SetData(int x)
        {
            _data = x;
        }

        // Provides function pointers in the COM format to use in COM VTables
        public static class ABI
        {

            [UnmanagedCallersOnly]
            public static int GetData(void* @this, int* value)
            {
                try
                {
                    *value = ComInterfaceDispatch.GetInstance<IComInterface1>((ComInterfaceDispatch*)@this).GetData();
                    return 0;
                }
                catch (Exception e)
                {
                    return e.HResult;
                }
            }

            [UnmanagedCallersOnly]
            public static int SetData(void* @this, int newValue)
            {
                try
                {
                    ComInterfaceDispatch.GetInstance<IComInterface1>((ComInterfaceDispatch*)@this).SetData(newValue);
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
