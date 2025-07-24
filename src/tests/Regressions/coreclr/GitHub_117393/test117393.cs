// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace Test117393
{
    class TestObject : ICustomQueryInterface
    {
        public CustomQueryInterfaceResult GetInterface(ref Guid iid, out IntPtr ppv)
        {
            // Induce NullReferenceException
            string s = null;
            Console.WriteLine(s.Length);
            ppv = IntPtr.Zero;
	        return CustomQueryInterfaceResult.Failed;
        }
    }

    class TestWrappers : ComWrappers
    {
        protected override unsafe ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
        {
            // Unknown type
            count = 0;
            return null;
        }

        protected override object? CreateObject(IntPtr externalComObject, CreateObjectFlags flags)
        {
            return null;
        }

        protected override void ReleaseObjects(IEnumerable objects)
        {
        }
    }

    public class Program
    {
        [DllImport("nativetest117393")]
        private static extern void TestFromNativeThread(IntPtr pUnknown);

        [Fact]
        public static void TestEntryPoint()
        {
            bool reportedUnhandledException = false;
            UnhandledExceptionEventHandler handler = (sender, e) =>
            {
                reportedUnhandledException = true;
            };

            var cw = new TestWrappers();
            TestObject obj = new TestObject();
            // Create a managed object wrapper for the Demo object.
            // Note the returned COM interface will always be for IUnknown.
            IntPtr ccwUnknown = cw.GetOrCreateComInterfaceForObject(obj, CreateComInterfaceFlags.None);
            AppDomain.CurrentDomain.UnhandledException += handler;
            TestFromNativeThread(ccwUnknown);
            AppDomain.CurrentDomain.UnhandledException -= handler;
            Marshal.Release(ccwUnknown);

            Assert.False(reportedUnhandledException, "There should be no unhandled exception on the secondary thread");
        }
    }
}
