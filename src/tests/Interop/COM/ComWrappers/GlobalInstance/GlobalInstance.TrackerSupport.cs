// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ComWrappersTests.GlobalInstance
{
    using System;
    using System.Runtime.InteropServices;

    using ComWrappersTests.Common;
    using TestLibrary;

    partial class Program
    {
        private static void ValidateNotRegisteredForMarshalling()
        {
            Console.WriteLine($"Running {nameof(ValidateNotRegisteredForMarshalling)}...");

            var testObj = new Test();
            IntPtr comWrapper1 = Marshal.GetIUnknownForObject(testObj);
            Assert.IsNull(GlobalComWrappers.Instance.LastComputeVtablesObject, "ComWrappers instance should not have been called");

            IntPtr trackerObjRaw = MockReferenceTrackerRuntime.CreateTrackerObject();
            object objWrapper = Marshal.GetObjectForIUnknown(trackerObjRaw);
            Assert.IsFalse(objWrapper is FakeWrapper, $"ComWrappers instance should not have been called");
        }

        static int Main(string[] doNotUse)
        {
            try
            {
                // The first test registers a global ComWrappers instance for tracker support.
                // Subsequents tests assume the global instance has already been registered.
                ValidateRegisterForTrackerSupport();
#if Windows
                ValidateNotRegisteredForMarshalling();
#endif                

                IntPtr trackerObjRaw = MockReferenceTrackerRuntime.CreateTrackerObject();
                var trackerObj = GlobalComWrappers.Instance.GetOrCreateObjectForComInstance(trackerObjRaw, CreateObjectFlags.TrackerObject);
                Marshal.Release(trackerObjRaw);

                ValidateNotifyEndOfReferenceTrackingOnThread();
#if Windows
                // Register a global ComWrappers instance for marshalling.
                ValidateRegisterForMarshalling();

                ValidateMarshalAPIs(validateUseRegistered: true);
                ValidateMarshalAPIs(validateUseRegistered: false);

                ValidatePInvokes(validateUseRegistered: true);
                ValidatePInvokes(validateUseRegistered: false);

                ValidateComActivation(validateUseRegistered: true);
                ValidateComActivation(validateUseRegistered: false);
#endif
                ValidateNotifyEndOfReferenceTrackingOnThread();
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

