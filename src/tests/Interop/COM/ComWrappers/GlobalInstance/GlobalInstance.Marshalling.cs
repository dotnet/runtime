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
                bool builtInComDisabled=false;
                var comConfig = AppContext.GetData("System.Runtime.InteropServices.BuiltInComInterop.IsSupported");
                if(comConfig != null && !bool.Parse(comConfig.ToString()))
                {
                    builtInComDisabled=true;
                }
                Console.WriteLine($"Built-in COM Disabled?: {builtInComDisabled}");


                // The first test registers a global ComWrappers instance for marshalling
                // Subsequents tests assume the global instance has already been registered.
                ValidateRegisterForMarshalling();

                ValidateMarshalAPIs(validateUseRegistered: true);
                if(!builtInComDisabled)
                {
                    ValidateMarshalAPIs(validateUseRegistered: false);
                }

                ValidatePInvokes(validateUseRegistered: true);
                if(!builtInComDisabled)
                {
                    ValidatePInvokes(validateUseRegistered: false);
                }

                if(!builtInComDisabled)
                {
                    // This calls ValidateNativeServerActivation which calls Marshal.GetTypeFromCLSID that is not supported
                    ValidateComActivation(validateUseRegistered: true);
                    ValidateComActivation(validateUseRegistered: false);
                }

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

