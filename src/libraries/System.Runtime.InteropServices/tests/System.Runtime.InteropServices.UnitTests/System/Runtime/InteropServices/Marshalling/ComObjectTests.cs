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

        }

        [Fact]
        public void ComObject_FinalRelease_ThrowingCacheStrategy()
        {
            var (ptr, _) = CreateComObject();
            var comWrappers = new ThrowingClearComWrappers();
            var comObject = (Marshalling.ComObject)comWrappers.GetOrCreateObjectForComInterface((nint)ptr, CreateObjectFlags.UniqueInstance);
            Marshal.Release((nint)ptr);
            Assert.Throws<InvalidOperationException>(() => comObject.FinalRelease());
            GC.Collect();
            // The finalizer should not throw as the object should have suppressed finalization.
            GC.WaitForPendingFinalizers();

            // FinalRelease should not throw an exception if called a second time (it should be a no-op).
            comObject.FinalRelease();

            // We'll manually release again to ensure that we don't leak.
            Marshal.Release((nint)ptr);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Assert.False(weakReference.IsAlive);
        }

        sealed class ThrowingClearComWrappers : StrategyBasedComWrappers
        {
            protected override IIUnknownCacheStrategy CreateCacheStrategy()
             => new ThrowingClearCacheStrategy(base.CreateCacheStrategy());


            private sealed class ThrowingClearCacheStrategy(IIUnknownCacheStrategy inner) : IIUnknownCacheStrategy
            {
                IIUnknownCacheStrategy.TableInfo IIUnknownCacheStrategy.ConstructTableInfo(RuntimeTypeHandle handle, IIUnknownDerivedDetails details, void* ptr)
                    => inner.ConstructTableInfo(handle, details, ptr);

                bool IIUnknownCacheStrategy.TryGetTableInfo(RuntimeTypeHandle handle, out IIUnknownCacheStrategy.TableInfo info)
                    => inner.TryGetTableInfo(handle, out info);

                bool IIUnknownCacheStrategy.TrySetTableInfo(RuntimeTypeHandle handle, IIUnknownCacheStrategy.TableInfo info)
                    => inner.TrySetTableInfo(handle, info);

                void IIUnknownCacheStrategy.Clear(IIUnknownStrategy unknownStrategy)
                {
                    throw new InvalidOperationException();
                }
            }
        }

        [Fact]
        public void ComObject_FinalRelease_ThrowingIUnknownStrategy()
        {
            var (ptr, _) = CreateComObject();
            var comWrappers = new ThrowingReleaseComWrappers();
            var comObject = (Marshalling.ComObject)comWrappers.GetOrCreateObjectForComInterface((nint)ptr, CreateObjectFlags.UniqueInstance);
            Marshal.Release((nint)ptr);
            Assert.Throws<InvalidOperationException>(() => comObject.FinalRelease());
            GC.Collect();
            // The finalizer should not throw as the object should have suppressed finalization.
            GC.WaitForPendingFinalizers();

            // FinalRelease should not throw an exception if called a second time (it should be a no-op).
            comObject.FinalRelease();

            // We'll manually release again to ensure that we don't leak.
            Marshal.Release((nint)ptr);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Assert.False(weakReference.IsAlive);
        }

        sealed class ThrowingReleaseComWrappers : StrategyBasedComWrappers
        {
            protected override IIUnknownStrategy GetOrCreateIUnknownStrategy()
             => new ThrowingReleaseStrategy();


            private sealed class ThrowingReleaseStrategy : IIUnknownStrategy
            {
                void* IIUnknownStrategy.CreateInstancePointer(void* unknown) => unknown;

                unsafe int IIUnknownStrategy.QueryInterface(void* thisPtr, in Guid handle, out void* ppObj)
                    => unchecked((int)0x80004002); // E_NOINTERFACE

                unsafe int IIUnknownStrategy.Release(void* thisPtr)
                    => throw new InvalidOperationException();
            }
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        private static (nint ptr, WeakReference objRef) CreateComObject()
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
