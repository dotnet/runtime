// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SharedTypes.ComInterfaces;
using static System.Runtime.InteropServices.ComWrappers;

namespace NativeExports.ComInterfaceGenerator
{
    internal unsafe class GetPoint
    {
        [UnmanagedCallersOnly(EntryPoint = "create_point_provider")]
        public static void* CreatePointProvider()
        {
            MyComWrapper cw = new();
            var myObject = new ImplementingObject();
            nint ptr = cw.GetOrCreateComInterfaceForObject(myObject, CreateComInterfaceFlags.None);

            return (void*)ptr;
        }

        private sealed class MyComWrapper : ComWrappers
        {
            protected override ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
            {
                if (obj is ImplementingObject)
                {
                    ComInterfaceEntry* comInterfaceEntry = (ComInterfaceEntry*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(ImplementingObject), sizeof(ComInterfaceEntry));
                    comInterfaceEntry->IID = typeof(IPointProvider).GUID;
                    comInterfaceEntry->Vtable = (nint)ImplementingObject.ABI.VTable;
                    count = 1;
                    return comInterfaceEntry;
                }
                count = 0;
                return null;
            }

            protected override object? CreateObject(nint externalComObject, CreateObjectFlags flags) => throw new NotImplementedException();
            protected override void ReleaseObjects(IEnumerable objects) => throw new NotImplementedException();
        }

        private sealed class ImplementingObject : IPointProvider
        {
            private Point _point;

            public Point GetPoint() => _point;
            public void SetPoint(Point point) => _point = point;

            public static class ABI
            {
                static void* s_vtable = null;

                public static void* VTable
                {
                    get
                    {
                        if (s_vtable != null)
                            return s_vtable;
                        void** vtable = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(ImplementingObject), sizeof(void*) * 5);
                        GetIUnknownImpl(out var fpQueryInterface, out var fpAddReference, out var fpRelease);
                        vtable[0] = (void*)fpQueryInterface;
                        vtable[1] = (void*)fpAddReference;
                        vtable[2] = (void*)fpRelease;
                        vtable[3] = (delegate* unmanaged[MemberFunction]<ComInterfaceDispatch*, Point>)&ImplementingObject.ABI.GetPoint;
                        vtable[4] = (delegate* unmanaged[MemberFunction]<ComInterfaceDispatch*, Point, int>)&ImplementingObject.ABI.SetPoint;
                        s_vtable = vtable;
                        return s_vtable;
                    }
                }

                [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvMemberFunction) })]
                private static Point GetPoint(ComInterfaceDispatch* @this)
                {

                    try
                    {
                        return ComInterfaceDispatch.GetInstance<IPointProvider>(@this).GetPoint();
                    }
                    catch (Exception e)
                    {
                        _ = Marshal.GetHRForException(e);
                        return default;
                    }
                }

                [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvMemberFunction) })]
                private static int SetPoint(ComInterfaceDispatch* @this, Point point)
                {

                    try
                    {
                        ComInterfaceDispatch.GetInstance<IPointProvider>(@this).SetPoint(point);
                        return 0;
                    }
                    catch (Exception e)
                    {
                        return Marshal.GetHRForException(e);
                    }
                }
            }
        }
    }
}
