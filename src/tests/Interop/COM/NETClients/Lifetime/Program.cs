// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
namespace NetClient
{
    using System;
    using System.Threading;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    using TestLibrary;
    using Xunit;
    using Server.Contract;
    using Server.Contract.Servers;

    public unsafe class Program
    {
        static delegate* unmanaged<int> GetAllocationCount;
        static ITrackMyLifetimeTesting? s_agileInstance;
        static Exception? s_callbackException;

        [DllImport("COMNativeServer", EntryPoint = "InvokeCallbackOnNativeThread")]
        private static extern int InvokeCallbackOnNativeThread(delegate* unmanaged<void> callback);

        // Initialize for all tests
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Initialize()
        {
            var inst = new TrackMyLifetimeTesting();
            GetAllocationCount = (delegate* unmanaged<int>)inst.GetAllocationCountCallback();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AllocateInstances(int a)
        {
            var insts = new object[a];
            for (int i = 0; i < a; ++i)
            {
                insts[i] = new TrackMyLifetimeTesting();
            }
            return a;
        }

        static void ForceGC()
        {
            for (int i = 0; i < 3; ++i)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        [UnmanagedCallersOnly]
        static void InvokeObjectFromNativeThread()
        {
            try
            {
                s_agileInstance!.Method();
            }
            catch (Exception e)
            {
                s_callbackException = e;
            }
        }

        static void Validate_COMServer_CleanUp()
        {
            Console.WriteLine($"Calling {nameof(Validate_COMServer_CleanUp)}...");

            int allocated = 0;
            allocated += AllocateInstances(1);
            allocated += AllocateInstances(2);
            allocated += AllocateInstances(3);
            Assert.NotEqual(0, GetAllocationCount());

            ForceGC();

            Assert.Equal(0, GetAllocationCount());
        }

        static void Validate_COMServer_DisableEagerCleanUp()
        {
            Console.WriteLine($"Calling {nameof(Validate_COMServer_DisableEagerCleanUp)}...");
            Assert.Equal(0, GetAllocationCount());

            Thread.CurrentThread.DisableComObjectEagerCleanup();

            int allocated = 0;
            allocated += AllocateInstances(1);
            allocated += AllocateInstances(2);
            allocated += AllocateInstances(3);
            Assert.NotEqual(0, GetAllocationCount());

            ForceGC();

            Assert.NotEqual(0, GetAllocationCount());

            Marshal.CleanupUnusedObjectsInCurrentContext();

            ForceGC();

            Assert.Equal(0, GetAllocationCount());
            Assert.False(Marshal.AreComObjectsAvailableForCleanup());
        }

        static void Validate_COMServer_CallOnNativeThread()
        {
            Console.WriteLine($"Calling {nameof(Validate_COMServer_CallOnNativeThread)}...");

            // Need agile instance since the object will be used on a different thread
            // than the creating thread and we're on an STA thread.
            s_agileInstance = CreateAgileInstance();
            try
            {
                s_agileInstance.Method();

                // Create a fresh native thread for each callback so the COM call runs before that thread
                // has initialized the CLR's OLE TLS state.
                for (int i = 0; i < 10; i++)
                {
                    s_callbackException = null;

                    Marshal.ThrowExceptionForHR(InvokeCallbackOnNativeThread(&InvokeObjectFromNativeThread));

                    Assert.True(s_callbackException is null, s_callbackException?.ToString());
                }
            }
            finally
            {
                s_agileInstance = null;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static ITrackMyLifetimeTesting CreateAgileInstance()
                => new TrackMyLifetimeTesting().CreateAgileInstance();
        }

        const int TestFailed = 101;
        const int TestPassed = 100;

        static int RunOnSTAThread(Action action)
        {
            int result = TestFailed;

            Thread staThread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Test Failure: {e}");
                    result = TestFailed;
                }
                result = TestPassed;
            });

            staThread.SetApartmentState(ApartmentState.STA);
            staThread.Start();
            staThread.Join();

            return result;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            // RegFree COM and STA apartments are not supported on Windows Nano
            if (Utilities.IsWindowsNanoServer)
            {
                return TestPassed;
            }

            // Run the test on a new STA thread since Nano Server doesn't support the STA
            // and as a result, the main application thread can't be made STA with the STAThread attribute
            int result = RunOnSTAThread(() =>
            {
                // Initialization for all future tests
                Initialize();
                ForceGC();
                Assert.True(GetAllocationCount != null);

                Validate_COMServer_CleanUp();
                Validate_COMServer_CallOnNativeThread();
            });
            if (result != TestPassed)
            {
                return result;
            }

            return RunOnSTAThread(() =>
            {
                // Initialization for all future tests
                Initialize();
                ForceGC();
                Assert.True(GetAllocationCount != null);

                // Manipulating the eager cleanup state cannot be changed once set,
                // so we need to run this test on a separate thread after the first
                // test validates that cleanup is working as expected with eager cleanup enabled.
                Validate_COMServer_DisableEagerCleanUp();
            });
        }
    }
}
