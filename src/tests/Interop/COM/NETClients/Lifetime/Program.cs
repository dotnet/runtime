// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace NetClient
{
    using System;
    using System.Threading;
    using System.Runtime.InteropServices;

    using TestLibrary;
    using Server.Contract;
    using Server.Contract.Servers;

    unsafe class Program
    {
        static delegate* unmanaged<int> GetAllocationCount;

        // Initialize for all tests
        static void Initialize()
        {
            var inst = new TrackMyLifetimeTesting();
            GetAllocationCount = (delegate* unmanaged<int>)inst.GetAllocationCountCallback();
        }

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

        static void Validate_COMServer_CleanUp()
        {
            Console.WriteLine($"Calling {nameof(Validate_COMServer_CleanUp)}...");

            int allocated = 0;
            allocated += AllocateInstances(1);
            allocated += AllocateInstances(2);
            allocated += AllocateInstances(3);
            Assert.AreNotEqual(0, GetAllocationCount());

            ForceGC();

            Assert.AreEqual(0, GetAllocationCount());
        }

        static void Validate_COMServer_DisableEagerCleanUp()
        {
            Console.WriteLine($"Calling {nameof(Validate_COMServer_DisableEagerCleanUp)}...");
            Assert.AreEqual(0, GetAllocationCount());

            Thread.CurrentThread.DisableComObjectEagerCleanup();

            int allocated = 0;
            allocated += AllocateInstances(1);
            allocated += AllocateInstances(2);
            allocated += AllocateInstances(3);
            Assert.AreNotEqual(0, GetAllocationCount());

            ForceGC();

            Assert.AreNotEqual(0, GetAllocationCount());

            Marshal.CleanupUnusedObjectsInCurrentContext();

            ForceGC();

            Assert.AreEqual(0, GetAllocationCount());
            Assert.IsFalse(Marshal.AreComObjectsAvailableForCleanup());
        }

        [STAThread]
        static int Main(string[] doNotUse)
        {
            // RegFree COM is not supported on Windows Nano
            if (Utilities.IsWindowsNanoServer)
            {
                return 100;
            }

            try
            {
                // Initialization for all future tests
                Initialize();
                Assert.IsTrue(GetAllocationCount != null);

                Validate_COMServer_CleanUp();
                Validate_COMServer_DisableEagerCleanUp();
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
