// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace ComWrappersTests
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;

    using TestLibrary;

    class Program
    {
        class MyComWrappers : ComWrappers
        {
            protected unsafe override ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
            {
                throw new NotImplementedException();
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
