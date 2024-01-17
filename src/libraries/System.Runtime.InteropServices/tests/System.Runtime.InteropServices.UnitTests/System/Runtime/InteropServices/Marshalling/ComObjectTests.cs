// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices.Tests
{
    public class ComObjectTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsPreciseGcSupported))]
        public void ComObject_FinalRelease()
        {
            var (ptr, weakReference) = CreateComObject();
            var comWrappers = new StrategyBasedComWrappers();
            var comObject = (Marshalling.ComObject)comWrappers.GetOrCreateObjectForComInterface((nint)ptr, CreateObjectFlags.UniqueInstance);
            Marshal.Release((nint)ptr);
            comObject.FinalRelease();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            // The underlying object should be collected as FinalRelease should have released all references to the managed object wrapper.
            Assert.False(weakReference.IsAlive);
            // FinalRelease should not crash if called a second time (it should be a no-op).
            comObject.FinalRelease();

            [MethodImpl(MethodImplOptions.NoInlining)]
            static (nint, WeakReference) CreateComObject()
            {
                var managedObject = new object();
                var managedObjectWrapper = new Common.ComWrappersImpl();
                IntPtr ptr = cw.GetOrCreateComInterfaceForObject(o, CreateComInterfaceFlags.None);
                var comWrappers = new StrategyBasedComWrappers();
                var ptr = Common.ComObject.Create();
                var comObject = (Marshalling.ComObject)comWrappers.GetOrCreateObjectForComInterface((nint)ptr, CreateObjectFlags.UniqueInstance);
                return ((nint)ptr, new WeakReference(comObject));
            }
        }
    }
}
