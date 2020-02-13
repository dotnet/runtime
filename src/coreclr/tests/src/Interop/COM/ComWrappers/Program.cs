// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace ComWrappersTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    using TestLibrary;

    class Program
    {
        //
        // Managed object with native wrapper definition.
        //
        [Guid("447BB9ED-DA48-4ABC-8963-5BB5C3E0AA09")]
        interface ITest
        {
            void SetValue(int i);
        }

        class Test : ITest
        {
            public static int InstanceCount = 0;

            private int value = -1;
            public Test() { InstanceCount++; }
            ~Test() { InstanceCount--; }

            public void SetValue(int i) => this.value = i;
            public int GetValue() => this.value;
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

        class MyComWrappers : ComWrappers
        {
            protected unsafe override ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
            {
                Assert.IsTrue(obj is Test);

                IntPtr fpQueryInteface = default;
                IntPtr fpAddRef = default;
                IntPtr fpRelease = default;
                ComWrappers.GetIUnknownImpl(out fpQueryInteface, out fpAddRef, out fpRelease);

                var vtbl = new ITestVtbl()
                {
                    IUnknownImpl = new IUnknownVtbl()
                    {
                        QueryInterface = fpQueryInteface,
                        AddRef = fpAddRef,
                        Release = fpRelease
                    },
                    SetValue = Marshal.GetFunctionPointerForDelegate(ITestVtbl.pSetValue)
                };
                var vtblRaw = RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(ITestVtbl), sizeof(ITestVtbl));
                Marshal.StructureToPtr(vtbl, vtblRaw, false);

                var entryRaw = (ComInterfaceEntry*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(ITestVtbl), sizeof(ComInterfaceEntry));
                entryRaw->IID = typeof(ITest).GUID;
                entryRaw->Vtable = vtblRaw;

                count = 1;
                return entryRaw;
            }

            protected override object CreateObject(IntPtr externalComObject, IntPtr agileObj, CreateObjectFlags flags)
            {
                Assert.AreNotEqual(agileObj, IntPtr.Zero);

                var iid = typeof(ITrackerObject).GUID;
                IntPtr iTestComObject;
                int hr = Marshal.QueryInterface(externalComObject, ref iid, out iTestComObject);
                Assert.AreEqual(hr, 0);

                return new ITrackerObjectWrapper(iTestComObject);
            }

            public static void ValidateIUnknownImpls()
            {
                Console.WriteLine($"Running {nameof(ValidateIUnknownImpls)}...");

                ComWrappers.GetIUnknownImpl(out IntPtr fpQueryInteface, out IntPtr fpAddRef, out IntPtr fpRelease);

                Assert.AreNotEqual(fpQueryInteface, IntPtr.Zero);
                Assert.AreNotEqual(fpAddRef, IntPtr.Zero);
                Assert.AreNotEqual(fpRelease, IntPtr.Zero);
            }
        }

        static void ValidateComInterfaceCreation()
        {
            Console.WriteLine($"Running {nameof(ValidateComInterfaceCreation)}...");

            var testObj = new Test();

            var wrappers = new MyComWrappers();

            // Allocate a wrapper for the object
            IntPtr comWrapper = wrappers.GetOrCreateComInterfaceForObject(testObj, CreateComInterfaceFlags.TrackerSupport);
            Assert.AreNotEqual(comWrapper, IntPtr.Zero);

            // Get a wrapper for an object and verify it is the same one.
            IntPtr comWrapperMaybe = wrappers.GetOrCreateComInterfaceForObject(testObj, CreateComInterfaceFlags.TrackerSupport);
            Assert.AreEqual(comWrapper, comWrapperMaybe);

            // Release the wrapper
            int count = Marshal.Release(comWrapper);
            Assert.AreEqual(count, 1);
            count = Marshal.Release(comWrapperMaybe);
            Assert.AreEqual(count, 0);

            // Create a new wrapper
            IntPtr comWrapperNew = wrappers.GetOrCreateComInterfaceForObject(testObj, CreateComInterfaceFlags.TrackerSupport);

            // Once a wrapper is created for a managed object it is always present
            Assert.AreEqual(comWrapperNew, comWrapper);

            // Release the new wrapper
            count = Marshal.Release(comWrapperNew);
            Assert.AreEqual(count, 0);
        }

        static void ValidateRuntimeTrackerScenario()
        {
            Console.WriteLine($"Running {nameof(ValidateRuntimeTrackerScenario)}...");

            var cw = new MyComWrappers();

            // Get an object from a tracker runtime.
            IntPtr trackerObjRaw = MockReferenceTrackerRuntime.CreateTrackerObject();

            // Create a managed wrapper for the native object.
            var trackerObj = (ITrackerObjectWrapper)cw.GetOrCreateObjectForComInstance(trackerObjRaw, CreateObjectFlags.TrackerObject);

            var testWrapperIds = new List<int>();
            for (int i = 0; i < 1000; ++i)
            {
                // Create a native wrapper for the managed object.
                IntPtr testWrapper = cw.GetOrCreateComInterfaceForObject(new Test(), CreateComInterfaceFlags.TrackerSupport);

                // Pass the managed object to the native object.
                int id = trackerObj.AddObjectRef(testWrapper);

                // Retain the managed object wrapper ptr.
                testWrapperIds.Add(id);
            }

            Assert.IsTrue(testWrapperIds.Count <= Test.InstanceCount);

            GC.Collect();
            GC.Collect();
            GC.Collect();
            GC.Collect();
            GC.Collect();

            Assert.IsTrue(testWrapperIds.Count <= Test.InstanceCount);

            // Remove the managed object ref from the native object.
            foreach (int id in testWrapperIds)
            {
                trackerObj.DropObjectRef(id);
            }

            testWrapperIds.Clear();

            GC.Collect();
            GC.Collect();
            GC.Collect();
            GC.Collect();
            GC.Collect();
        }

        static void ValidateIUnknownImpls()
            => MyComWrappers.ValidateIUnknownImpls();

        static void ValidateRegisterForReferenceTrackerHost()
        {
            Console.WriteLine($"Running {nameof(ValidateRegisterForReferenceTrackerHost)}...");

            var wrappers1 = new MyComWrappers();
            wrappers1.RegisterForReferenceTrackerHost();

            Assert.Throws<InvalidOperationException>(
                () =>
                {
                    wrappers1.RegisterForReferenceTrackerHost();
                }, "Should not be able to re-register for ReferenceTrackerHost");

            var wrappers2 = new MyComWrappers();
            Assert.Throws<InvalidOperationException>(
                () =>
                {
                    wrappers2.RegisterForReferenceTrackerHost();
                }, "Should not be able to reset for ReferenceTrackerHost");
        }

        static int Main(string[] doNotUse)
        {
            try
            {
                ValidateComInterfaceCreation();
                ValidateRuntimeTrackerScenario();
                ValidateIUnknownImpls();
                ValidateRegisterForReferenceTrackerHost();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Test Failure: {e}");
                return 101;
            }

            return 100;
        }
    }
}
