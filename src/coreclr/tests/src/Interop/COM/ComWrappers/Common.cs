// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace ComWrappersTests.Common
{
    using System;
    using System.Runtime.InteropServices;

    //
    // Managed object with native wrapper definition.
    //
    [Guid("447BB9ED-DA48-4ABC-8963-5BB5C3E0AA09")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface ITest
    {
        void SetValue(int i);
    }

    class Test : ITest, ICustomQueryInterface
    {
        public static int InstanceCount = 0;

        private int value = -1;
        public Test() { InstanceCount++; }
        ~Test() { InstanceCount--; }

        public void SetValue(int i) => this.value = i;
        public int GetValue() => this.value;

        public bool EnableICustomQueryInterface { get; set; } = false;
        public Guid ICustomQueryInterface_GetInterfaceIID { get; set; }
        public IntPtr ICustomQueryInterface_GetInterfaceResult { get; set; }

        CustomQueryInterfaceResult ICustomQueryInterface.GetInterface(ref Guid iid, out IntPtr ppv)
        {
            ppv = IntPtr.Zero;
            if (!EnableICustomQueryInterface)
            {
                return CustomQueryInterfaceResult.NotHandled;
            }

            if (iid != ICustomQueryInterface_GetInterfaceIID)
            {
                return CustomQueryInterfaceResult.Failed;
            }

            ppv = this.ICustomQueryInterface_GetInterfaceResult;
            return CustomQueryInterfaceResult.Handled;
        }
    }

    public struct IUnknownVtbl
    {
        public IntPtr QueryInterface;
        public IntPtr AddRef;
        public IntPtr Release;
    }

    public struct ITestVtbl
    {
        public IUnknownVtbl IUnknownImpl;
        public IntPtr SetValue;

        public delegate int _SetValue(IntPtr thisPtr, int i);
        public static _SetValue pSetValue = new _SetValue(SetValueInternal);

        public static int SetValueInternal(IntPtr dispatchPtr, int i)
        {
            unsafe
            {
                try
                {
                    ComWrappers.ComInterfaceDispatch.GetInstance<ITest>((ComWrappers.ComInterfaceDispatch*)dispatchPtr).SetValue(i);
                }
                catch (Exception e)
                {
                    return e.HResult;
                }
            }
            return 0; // S_OK;
        }
    }

    //
    // Native interface defintion with managed wrapper for tracker object
    //
    struct MockReferenceTrackerRuntime
    {
        [DllImport(nameof(MockReferenceTrackerRuntime))]
        extern public static IntPtr CreateTrackerObject();

        [DllImport(nameof(MockReferenceTrackerRuntime))]
        extern public static void ReleaseAllTrackerObjects();

        [DllImport(nameof(MockReferenceTrackerRuntime))]
        extern public static int Trigger_NotifyEndOfReferenceTrackingOnThread();
    }

    [Guid("42951130-245C-485E-B60B-4ED4254256F8")]
    public interface ITrackerObject
    {
        int AddObjectRef(IntPtr obj);
        void DropObjectRef(int id);
    };

    public struct VtblPtr
    {
        public IntPtr Vtbl;
    }

    public class ITrackerObjectWrapper : ITrackerObject
    {
        private struct ITrackerObjectWrapperVtbl
        {
            public IntPtr QueryInterface;
            public _AddRef AddRef;
            public _Release Release;
            public _AddObjectRef AddObjectRef;
            public _DropObjectRef DropObjectRef;
        }

        private delegate int _AddRef(IntPtr This);
        private delegate int _Release(IntPtr This);
        private delegate int _AddObjectRef(IntPtr This, IntPtr obj, out int id);
        private delegate int _DropObjectRef(IntPtr This, int id);

        private readonly IntPtr instance;
        private readonly ITrackerObjectWrapperVtbl vtable;

        public ITrackerObjectWrapper(IntPtr instance)
        {
            var inst = Marshal.PtrToStructure<VtblPtr>(instance);
            this.vtable = Marshal.PtrToStructure<ITrackerObjectWrapperVtbl>(inst.Vtbl);
            this.instance = instance;
        }

        ~ITrackerObjectWrapper()
        {
            if (this.instance != IntPtr.Zero)
            {
                this.vtable.Release(this.instance);
            }
        }

        public int AddObjectRef(IntPtr obj)
        {
            int id;
            int hr = this.vtable.AddObjectRef(this.instance, obj, out id);
            if (hr != 0)
            {
                throw new COMException($"{nameof(AddObjectRef)}", hr);
            }

            return id;
        }

        public void DropObjectRef(int id)
        {
            int hr = this.vtable.DropObjectRef(this.instance, id);
            if (hr != 0)
            {
                throw new COMException($"{nameof(DropObjectRef)}", hr);
            }
        }
    }
}

