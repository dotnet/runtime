// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ComWrappersTests.GlobalInstance
{
    using System;

    using ComWrappersTests.Common;
    using TestLibrary;

    partial class Program
    {
        private static void ValidateNotRegisteredForTrackerSupport()
        {
            Console.WriteLine($"Running {nameof(ValidateNotRegisteredForTrackerSupport)}...");

            int hr = MockReferenceTrackerRuntime.Trigger_NotifyEndOfReferenceTrackingOnThread();
            Assert.AreNotEqual(GlobalComWrappers.ReleaseObjectsCallAck, hr);
        }

        static int Main(string[] doNotUse)
        {
            try
            {
                // The first test registers a global ComWrappers instance for marshalling
                // Subsequents tests assume the global instance has already been registered.
                ValidateRegisterForMarshalling();

                ValidateMarshalAPIs(validateUseRegistered: true);
                ValidateMarshalAPIs(validateUseRegistered: false);

                ValidatePInvokes(validateUseRegistered: true);
                ValidatePInvokes(validateUseRegistered: false);

                ValidateComActivation(validateUseRegistered: true);
                ValidateComActivation(validateUseRegistered: false);

                ValidateNotRegisteredForTrackerSupport();

                // Register a global ComWrappers instance for tracker support
                ValidateRegisterForTrackerSupport();

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

