// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.InteropServices.Tests.Common;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public partial class GetObjectForIUnknownTests
    {
        public static IEnumerable<object[]> GetObjectForIUnknown_ComObject_TestData()
        {
            yield return new object[] { new ComImportObject() };

            yield return new object[] { new DualComObject() };
            yield return new object[] { new IUnknownComObject() };
            yield return new object[] { new IDispatchComObject() };
            yield return new object[] { new IInspectableComObject() };

            yield return new object[] { new NonDualComObject() };
            yield return new object[] { new AutoDispatchComObject() };
            yield return new object[] { new AutoDualComObject() };

            yield return new object[] { new NonDualComObjectEmpty() };
            yield return new object[] { new AutoDispatchComObjectEmpty() };
            yield return new object[] { new AutoDualComObjectEmpty() };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        [MemberData(nameof(GetObjectForIUnknown_ComObject_TestData))]
        public void GetObjectForIUnknown_ComObject_ReturnsExpected(object o)
        {
            GetObjectForIUnknown_ValidPointer_ReturnsExpected(o);
        }

        [ComImport]
        [ComVisible(true)]
        [Guid("20d5e748-3e41-414f-ba43-542c6c47bd21")]
        [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
        public interface ICallback
        {
            void M();
        }

        class Callback : ICallback
        {
            public void M() { }
        }

        [GeneratedComInterface]
        [Guid("20d5e748-3e41-414f-ba43-542c6c47bd21")]
        public partial interface ICallbackWrapper
        {
            void M();
        }

        // Notice this class doesn't implement IDispatch nor it is specified on
        // the Callback class. The user workaround would be to mark the
        // Callback class as ComVisible(true) and public. We wrap the input
        // to avoid the automatic detection the runtime does on COM objects.
        // This simulates the failure mode we are trying to detect.
        [GeneratedComClass]
        public partial class CallbackWrapper : ICallbackWrapper
        {
            private IntPtr _wrapper;
            public CallbackWrapper(IntPtr wrapper)
            {
                _wrapper = wrapper;
                Marshal.AddRef(_wrapper);
            }

            ~CallbackWrapper()
            {
                Marshal.Release(_wrapper);
            }

            public void M() => throw new NotImplementedException();
        }

        [UnmanagedCallersOnly]
        private static unsafe IntPtr WrapCallback(IntPtr p)
        {
            // See CallbackWrapper for why we wrap the input.
            var wrapper = new CallbackWrapper(p);
            return (IntPtr)ComInterfaceMarshaller<ICallbackWrapper>.ConvertToUnmanaged(wrapper);
        }

        private delegate ICallback WrapDelegate(ICallback cb);

        // This test is validating a niche case is detected where a class implementing COM "callback"
        // interface is marshalled but fails to indicate it is COM visible and thus IDispatch isn't
        // provided by the runtime. This most often occurs in out-of-proc COM scenarios.
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public unsafe void GetObjectForIUnknown_ComObject_MissingIDispatchOnTarget()
        {
            // Use a delegate to trigger COM interop marshalling.
            var fptr = Marshal.GetDelegateForFunctionPointer<WrapDelegate>((IntPtr)(delegate* unmanaged<IntPtr, IntPtr>)&WrapCallback);
            ICallback icb = fptr(new Callback());

            Exception ex = Assert.Throws<TargetException>(() => icb.M());
            Assert.Equal("COM target does not implement IDispatch.", ex.Message);
        }
    }
}
