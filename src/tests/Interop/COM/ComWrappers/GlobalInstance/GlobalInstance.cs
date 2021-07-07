// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ComWrappersTests.GlobalInstance
{
    using System;
    using System.Collections;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    using ComWrappersTests.Common;
    using TestLibrary;

    partial class Program
    {
        struct MarshalInterface
        {
            [DllImport(nameof(MockReferenceTrackerRuntime), EntryPoint="CreateTrackerObject_SkipTrackerRuntime")]
            [return: MarshalAs(UnmanagedType.IUnknown)]
            extern public static object CreateTrackerObjectAsIUnknown();

            [DllImport(nameof(MockReferenceTrackerRuntime), EntryPoint="CreateTrackerObject_SkipTrackerRuntime")]
            [return: MarshalAs(UnmanagedType.Interface)]
            extern public static FakeWrapper CreateTrackerObjectAsInterface();

            [DllImport(nameof(MockReferenceTrackerRuntime), EntryPoint="CreateTrackerObject_SkipTrackerRuntime")]
            [return: MarshalAs(UnmanagedType.Interface)]
            extern public static Test CreateTrackerObjectWrongType();

            [DllImport(nameof(MockReferenceTrackerRuntime))]
            extern public static int UpdateTestObjectAsIUnknown(
                [MarshalAs(UnmanagedType.IUnknown)] object testObj,
                int i,
                [MarshalAs(UnmanagedType.IUnknown)] out object ret);

            [DllImport(nameof(MockReferenceTrackerRuntime))]
            extern public static int UpdateTestObjectAsIDispatch(
                [MarshalAs(UnmanagedType.IDispatch)] object testObj,
                int i,
                [MarshalAs(UnmanagedType.IDispatch)] out object ret);

            [DllImport(nameof(MockReferenceTrackerRuntime))]
            extern public static int UpdateTestObjectAsInterface(
                [MarshalAs(UnmanagedType.Interface)] ITest testObj,
                int i,
                [Out, MarshalAs(UnmanagedType.Interface)] out ITest ret);
        }

        private const string ManagedServerTypeName = "ConsumeNETServerTesting";

        private const string IID_IDISPATCH = "00020400-0000-0000-C000-000000000046";
        private const string IID_IINSPECTABLE = "AF86E2E0-B12D-4c6a-9C5A-D7AA65101E90";
        class TestEx : Test
        {
            public readonly Guid[] Interfaces;
            public TestEx(params string[] iids)
            {
                Interfaces = new Guid[iids.Length];
                for (int i = 0; i < iids.Length; i++)
                    Interfaces[i] = Guid.Parse(iids[i]);
            }
        }

        class FakeWrapper
        {
            private delegate int _AddRef(IntPtr This);
            private delegate int _Release(IntPtr This);
            private struct IUnknownWrapperVtbl
            {
                public IntPtr QueryInterface;
                public _AddRef AddRef;
                public _Release Release;
            }

            private readonly IntPtr wrappedInstance;

            private readonly IUnknownWrapperVtbl vtable;

            public FakeWrapper(IntPtr instance)
            {
                this.wrappedInstance = instance;
                var inst = Marshal.PtrToStructure<VtblPtr>(instance);
                this.vtable = Marshal.PtrToStructure<IUnknownWrapperVtbl>(inst.Vtbl);
            }

            ~FakeWrapper()
            {
                if (this.wrappedInstance != IntPtr.Zero)
                {
                    this.vtable.Release(this.wrappedInstance);
                }
            }
        }

        class GlobalComWrappers : ComWrappers
        {
            public static GlobalComWrappers Instance = new GlobalComWrappers();

            public bool ReturnInvalid { get; set; }

            public object LastComputeVtablesObject { get; private set; } = null;

            protected unsafe override ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
            {
                LastComputeVtablesObject = obj;

                if (ReturnInvalid)
                {
                    count = -1;
                    return null;
                }

                if (obj is Test)
                {
                    return ComputeVtablesForTestObject((Test)obj, out count);
                }
                else if (string.Equals(ManagedServerTypeName, obj.GetType().Name))
                {
                    IntPtr fpQueryInterface = default;
                    IntPtr fpAddRef = default;
                    IntPtr fpRelease = default;
                    ComWrappers.GetIUnknownImpl(out fpQueryInterface, out fpAddRef, out fpRelease);

                    var vtbl = new IUnknownVtbl()
                    {
                        QueryInterface = fpQueryInterface,
                        AddRef = fpAddRef,
                        Release = fpRelease
                    };
                    var vtblRaw = RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(IUnknownVtbl), sizeof(IUnknownVtbl));
                    Marshal.StructureToPtr(vtbl, vtblRaw, false);

                    // Including interfaces to allow QI, but not actually returning a valid vtable, since it is not needed for the tests here.
                    var entryRaw = (ComInterfaceEntry*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(IUnknownVtbl), sizeof(ComInterfaceEntry));
                    entryRaw[0].IID = typeof(Server.Contract.IConsumeNETServer).GUID;
                    entryRaw[0].Vtable = vtblRaw;

                    count = 1;
                    return entryRaw;
                }

                count = -1;
                return null;
            }

            protected override object CreateObject(IntPtr externalComObject, CreateObjectFlags flag)
            {
                if (ReturnInvalid)
                    return null;

                Guid[] iids = {
                    typeof(ITrackerObject).GUID,
                    typeof(ITest).GUID,
                    typeof(Server.Contract.IDispatchTesting).GUID,
                    typeof(Server.Contract.IConsumeNETServer).GUID
                };

                for (var i = 0; i < iids.Length; i++)
                {
                    var iid = iids[i];
                    IntPtr comObject;
                    int hr = Marshal.QueryInterface(externalComObject, ref iid, out comObject);
                    if (hr == 0)
                        return new FakeWrapper(comObject);
                }

                return null;
            }

            public const int ReleaseObjectsCallAck = unchecked((int)-1);

            protected override void ReleaseObjects(IEnumerable objects)
            {
                throw new Exception() { HResult = ReleaseObjectsCallAck };
            }

            private unsafe ComInterfaceEntry* ComputeVtablesForTestObject(Test obj, out int count)
            {
                IntPtr fpQueryInterface = default;
                IntPtr fpAddRef = default;
                IntPtr fpRelease = default;
                ComWrappers.GetIUnknownImpl(out fpQueryInterface, out fpAddRef, out fpRelease);

                var iUnknownVtbl = new IUnknownVtbl()
                {
                    QueryInterface = fpQueryInterface,
                    AddRef = fpAddRef,
                    Release = fpRelease
                };

                var vtbl = new ITestVtbl()
                {
                    IUnknownImpl = iUnknownVtbl,
                    SetValue = Marshal.GetFunctionPointerForDelegate(ITestVtbl.pSetValue)
                };
                var vtblRaw = RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(ITestVtbl), sizeof(ITestVtbl));
                Marshal.StructureToPtr(vtbl, vtblRaw, false);

                int countLocal = obj is TestEx ? ((TestEx)obj).Interfaces.Length + 1 : 1;
                var entryRaw = (ComInterfaceEntry*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(ITestVtbl), sizeof(ComInterfaceEntry) * countLocal);
                entryRaw[0].IID = typeof(ITest).GUID;
                entryRaw[0].Vtable = vtblRaw;

                if (obj is TestEx)
                {
                    var iUnknownVtblRaw = RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(IUnknownVtbl), sizeof(IUnknownVtbl));
                    Marshal.StructureToPtr(iUnknownVtbl, iUnknownVtblRaw, false);

                    var testEx = (TestEx)obj;
                    for (int i = 1; i < testEx.Interfaces.Length + 1; i++)
                    {
                        // Including interfaces to allow QI, but not actually returning a valid vtable, since it is not needed for the tests here.
                        entryRaw[i].IID = testEx.Interfaces[i - 1];
                        entryRaw[i].Vtable = iUnknownVtblRaw;
                    }
                }

                count = countLocal;
                return entryRaw;
            }
        }

        private static void ValidateRegisterForMarshalling()
        {
            Console.WriteLine($"Running {nameof(ValidateRegisterForMarshalling)}...");

            var wrappers1 = GlobalComWrappers.Instance;
            ComWrappers.RegisterForMarshalling(wrappers1);
            Assert.Throws<InvalidOperationException>(
                () =>
                {
                    ComWrappers.RegisterForMarshalling(wrappers1);
                }, "Should not be able to re-register for global ComWrappers");

            var wrappers2 = new GlobalComWrappers();
            Assert.Throws<InvalidOperationException>(
                () =>
                {
                    ComWrappers.RegisterForMarshalling(wrappers2);
                }, "Should not be able to reset for global ComWrappers");
        }

        private static void ValidateRegisterForTrackerSupport()
        {
            Console.WriteLine($"Running {nameof(ValidateRegisterForTrackerSupport)}...");

            var wrappers1 = GlobalComWrappers.Instance;
            ComWrappers.RegisterForTrackerSupport(wrappers1);
            Assert.Throws<InvalidOperationException>(
                () =>
                {
                    ComWrappers.RegisterForTrackerSupport(wrappers1);
                }, "Should not be able to re-register for global ComWrappers");

            var wrappers2 = new GlobalComWrappers();
            Assert.Throws<InvalidOperationException>(
                () =>
                {
                    ComWrappers.RegisterForTrackerSupport(wrappers2);
                }, "Should not be able to reset for global ComWrappers");
        }

        private static void ValidateMarshalAPIs(bool validateUseRegistered)
        {
            string scenario = validateUseRegistered ? "use registered wrapper" : "fall back to runtime";
            Console.WriteLine($"Running {nameof(ValidateMarshalAPIs)}: {scenario}...");

            GlobalComWrappers registeredWrapper = GlobalComWrappers.Instance;
            registeredWrapper.ReturnInvalid = !validateUseRegistered;

            Console.WriteLine($" -- Validate Marshal.GetIUnknownForObject...");

            var testObj = new Test();
            IntPtr comWrapper1 = Marshal.GetIUnknownForObject(testObj);
            Assert.AreNotEqual(IntPtr.Zero, comWrapper1);
            Assert.AreEqual(testObj, registeredWrapper.LastComputeVtablesObject, "Registered ComWrappers instance should have been called");

            IntPtr comWrapper2 = Marshal.GetIUnknownForObject(testObj);
            Assert.AreEqual(comWrapper1, comWrapper2);

            Marshal.Release(comWrapper1);
            Marshal.Release(comWrapper2);

            Console.WriteLine($" -- Validate Marshal.GetIDispatchForObject...");

            Assert.Throws<InvalidCastException>(() => Marshal.GetIDispatchForObject(testObj));

            if (validateUseRegistered)
            {
                var dispatchObj = new TestEx(IID_IDISPATCH);
                IntPtr dispatchWrapper = Marshal.GetIDispatchForObject(dispatchObj);
                Assert.AreNotEqual(IntPtr.Zero, dispatchWrapper);
                Assert.AreEqual(dispatchObj, registeredWrapper.LastComputeVtablesObject, "Registered ComWrappers instance should have been called");

                Console.WriteLine($" -- Validate Marshal.GetIDispatchForObject != Marshal.GetIUnknownForObject...");
                IntPtr unknownWrapper = Marshal.GetIUnknownForObject(dispatchObj);
                Assert.AreNotEqual(IntPtr.Zero, unknownWrapper);
                Assert.AreNotEqual(unknownWrapper, dispatchWrapper);
            }

            Console.WriteLine($" -- Validate Marshal.GetObjectForIUnknown...");

            IntPtr trackerObjRaw = MockReferenceTrackerRuntime.CreateTrackerObject();
            object objWrapper1 = Marshal.GetObjectForIUnknown(trackerObjRaw);
            Assert.AreEqual(validateUseRegistered, objWrapper1 is FakeWrapper, $"GetObjectForIUnknown should{(validateUseRegistered ? string.Empty : "not")} have returned {nameof(FakeWrapper)} instance");
            object objWrapper2 = Marshal.GetObjectForIUnknown(trackerObjRaw);
            Assert.AreEqual(objWrapper1, objWrapper2);

            Console.WriteLine($" -- Validate Marshal.GetUniqueObjectForIUnknown...");

            object objWrapper3 = Marshal.GetUniqueObjectForIUnknown(trackerObjRaw);
            Assert.AreEqual(validateUseRegistered, objWrapper3 is FakeWrapper, $"GetObjectForIUnknown should{(validateUseRegistered ? string.Empty : "not")} have returned {nameof(FakeWrapper)} instance");

            Assert.AreNotEqual(objWrapper1, objWrapper3);

            Marshal.Release(trackerObjRaw);
        }

        private static void ValidatePInvokes(bool validateUseRegistered)
        {
            string scenario = validateUseRegistered ? "use registered wrapper" : "fall back to runtime";
            Console.WriteLine($"Running {nameof(ValidatePInvokes)}: {scenario}...");

            GlobalComWrappers.Instance.ReturnInvalid = !validateUseRegistered;

            Console.WriteLine($" -- Validate MarshalAs IUnknown...");
            ValidateInterfaceMarshaler<object>(MarshalInterface.UpdateTestObjectAsIUnknown, shouldSucceed: validateUseRegistered);
            object obj = MarshalInterface.CreateTrackerObjectAsIUnknown();
            Assert.AreEqual(validateUseRegistered, obj is FakeWrapper, $"Should{(validateUseRegistered ? string.Empty : "not")} have returned {nameof(FakeWrapper)} instance");

            if (validateUseRegistered)
            {
                Console.WriteLine($" -- Validate MarshalAs IDispatch...");
                ValidateInterfaceMarshaler<object>(MarshalInterface.UpdateTestObjectAsIDispatch, shouldSucceed: true, new TestEx(IID_IDISPATCH));
            }

            Console.WriteLine($" -- Validate MarshalAs Interface...");
            ValidateInterfaceMarshaler<ITest>(MarshalInterface.UpdateTestObjectAsInterface, shouldSucceed: true);

            if (validateUseRegistered)
            {
                Assert.Throws<InvalidCastException>(() => MarshalInterface.CreateTrackerObjectWrongType());

                FakeWrapper wrapper = MarshalInterface.CreateTrackerObjectAsInterface();
                Assert.IsNotNull(wrapper, $"Should have returned {nameof(FakeWrapper)} instance");
            }
        }

        private delegate int UpdateTestObject<T>(T testObj, int i, out T ret) where T : class;
        private static void ValidateInterfaceMarshaler<T>(UpdateTestObject<T> func, bool shouldSucceed, Test testObj = null) where T : class
        {
            const int E_NOINTERFACE = unchecked((int)0x80004002);
            int value = 10;

            if (testObj == null)
                testObj = new Test();

            T retObj;
            int hr = func(testObj as T, value, out retObj);
            Assert.AreEqual(testObj, GlobalComWrappers.Instance.LastComputeVtablesObject, "Registered ComWrappers instance should have been called");
            if (shouldSucceed)
            {
                Assert.IsTrue(retObj is Test);
                Assert.AreEqual(value, testObj.GetValue());
                Assert.AreEqual<object>(testObj, retObj);
            }
            else
            {
                Assert.AreEqual(E_NOINTERFACE, hr);
            }
        }

        private static void ValidateComActivation(bool validateUseRegistered)
        {
            string scenario = validateUseRegistered ? "use registered wrapper" : "fall back to runtime";
            Console.WriteLine($"Running {nameof(ValidateComActivation)}: {scenario}...");
            GlobalComWrappers.Instance.ReturnInvalid = !validateUseRegistered;

            Console.WriteLine($" -- Validate native server...");
            ValidateNativeServerActivation();

            Console.WriteLine($" -- Validate managed server...");
            ValidateManagedServerActivation();
        }

        private static void ValidateNativeServerActivation()
        {
            bool returnValid = !GlobalComWrappers.Instance.ReturnInvalid;

            Type t= Type.GetTypeFromCLSID(Guid.Parse(Server.Contract.Guids.DispatchTesting));
            var server = Activator.CreateInstance(t);
            Assert.AreEqual(returnValid, server is FakeWrapper, $"Should{(returnValid ? string.Empty : "not")} have returned {nameof(FakeWrapper)} instance");

            IntPtr ptr = Marshal.GetIUnknownForObject(server);
            var obj = Marshal.GetObjectForIUnknown(ptr);
            Assert.AreEqual(server, obj);
        }

        private static void ValidateManagedServerActivation()
        {
            bool returnValid = !GlobalComWrappers.Instance.ReturnInvalid;

            // Initialize CoreShim and hostpolicymock
            HostPolicyMock.Initialize(Environment.CurrentDirectory, null);
            Environment.SetEnvironmentVariable("CORESHIM_COMACT_ASSEMBLYNAME", "NETServer");
            Environment.SetEnvironmentVariable("CORESHIM_COMACT_TYPENAME", ManagedServerTypeName);

            using (HostPolicyMock.Mock_corehost_resolve_component_dependencies(0, string.Empty, string.Empty, string.Empty))
            {
                Type t = Type.GetTypeFromCLSID(Guid.Parse(Server.Contract.Guids.ConsumeNETServerTesting));
                var server = Activator.CreateInstance(t);
                Assert.AreEqual(returnValid, server is FakeWrapper, $"Should{(returnValid ? string.Empty : "not")} have returned {nameof(FakeWrapper)} instance");
                object serverUnwrapped = GlobalComWrappers.Instance.LastComputeVtablesObject;
                Assert.AreEqual(ManagedServerTypeName, serverUnwrapped.GetType().Name);

                IntPtr ptr = Marshal.GetIUnknownForObject(server);
                var obj = Marshal.GetObjectForIUnknown(ptr);
                Assert.AreEqual(server, obj);
                Assert.AreEqual(returnValid, obj is FakeWrapper, $"Should{(returnValid ? string.Empty : "not")} have returned {nameof(FakeWrapper)} instance");
                serverUnwrapped.GetType().GetMethod("NotEqualByRCW").Invoke(serverUnwrapped, new object[] { obj });
            }
        }

        private static void ValidateNotifyEndOfReferenceTrackingOnThread()
        {
            Console.WriteLine($"Running {nameof(ValidateNotifyEndOfReferenceTrackingOnThread)}...");

            // Make global instance return invalid object so that the Exception thrown by
            // GlobalComWrappers.ReleaseObjects is marshalled using the built-in system.
            GlobalComWrappers.Instance.ReturnInvalid = true;

            // Trigger the thread lifetime end API and verify the callback occurs.
            int hr = MockReferenceTrackerRuntime.Trigger_NotifyEndOfReferenceTrackingOnThread();
            Assert.AreEqual(GlobalComWrappers.ReleaseObjectsCallAck, hr);
        }
    }
}

