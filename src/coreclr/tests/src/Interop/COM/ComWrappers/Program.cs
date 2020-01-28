// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace ComWrappersTests
{
    using System;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    using TestLibrary;

    class Program
    {
        [Guid("447BB9ED-DA48-4ABC-8963-5BB5C3E0AA09")]
        interface ITest
        {
            void SetValue(int i);
        }

        class Test : ITest
        {
            private int value = -1;
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

            protected override object CreateObject(IntPtr externalComObject, CreateObjectFlags flags)
            {
                throw new NotImplementedException();
            }

            public static void ValidateIUnknownImpls()
            {
                ComWrappers.GetIUnknownImpl(out IntPtr fpQueryInteface, out IntPtr fpAddRef, out IntPtr fpRelease);

                Assert.AreNotEqual(fpQueryInteface, IntPtr.Zero);
                Assert.AreNotEqual(fpAddRef, IntPtr.Zero);
                Assert.AreNotEqual(fpRelease, IntPtr.Zero);
            }
        }

        static void ValidateComInterfaceCreation()
        {
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
            Assert.AreNotEqual(comWrapper, IntPtr.Zero);
            Assert.AreNotEqual(comWrapperNew, comWrapper);

            // Release the new wrapper
            count = Marshal.Release(comWrapperNew);
            Assert.AreEqual(count, 0);
        }

        static void ValidateIUnknownImpls()
            => MyComWrappers.ValidateIUnknownImpls();

        static void ValidateRegisterForReferenceTrackerHost()
        {
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
