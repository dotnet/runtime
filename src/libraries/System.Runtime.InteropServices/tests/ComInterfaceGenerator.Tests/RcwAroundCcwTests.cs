// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using SharedTypes.ComInterfaces;
using SharedTypes.ComInterfaces.MarshallingFails;
using Xunit;

namespace ComInterfaceGenerator.Tests
{
    public partial class RcwAroundCcwTests
    {
        static TInterface CreateWrapper<TClass, TInterface>() where TClass : TInterface, new()
        {
            var cw = new StrategyBasedComWrappers();
            var comPtr = cw.GetOrCreateComInterfaceForObject(new TClass(), CreateComInterfaceFlags.None);
            var comObject = cw.GetOrCreateObjectForComInstance(comPtr, CreateObjectFlags.None);
            var ifaceObject = (TInterface)comObject;
            return ifaceObject;
        }

        static bool SystemFindsComCalleeException() => PlatformDetection.IsWindows && PlatformDetection.IsNotNativeAot;

        [Fact]
        public void IInt()
        {
            var obj = CreateWrapper<IIntImpl, IInt>();
            obj.Set(1);
            Assert.Equal(1, obj.Get());
            var local = 4;
            obj.SwapRef(ref local);
            Assert.Equal(1, local);
            Assert.Equal(4, obj.Get());
            local = 2;
            obj.SetIn(in local);
            local = 0;
            obj.GetOut(out local);
            Assert.Equal(2, local);
        }

        [Fact]
        public void IDerived()
        {
            IDerived obj = CreateWrapper<Derived, IDerived>();
            obj.SetInt(1);
            Assert.Equal(1, obj.GetInt());
            obj.SetName("A");
            Assert.Equal("A", obj.GetName());
        }

        [Fact]
        public void IBool()
        {
            var obj = CreateWrapper<IBoolImpl, IBool>();
            Assert.False(obj.Get());
            obj.Set(true);
            Assert.True(obj.Get());
        }

        [Fact]
        public void IFloat()
        {
            var obj = CreateWrapper<IFloatImpl, IFloat>();
            obj.Set(2.71F);
            Assert.Equal(2.71F, obj.Get());
        }

        [Fact]
        public void IIntArray()
        {
            var obj = CreateWrapper<IIntArrayImpl, IIntArray>();
            int[] data = new int[] { 1, 2, 3 };
            int length = data.Length;
            obj.SetContents(data, length);
            Assert.Equal(data, obj.GetReturn(out int _));
            obj.GetOut(out var value);
            Assert.Equal(data, value);
            obj.SwapArray(ref data, data.Length);
            obj.PassIn(in data, data.Length);
            data = new int[] { 1, 2, 3 };
            obj.Double(data, data.Length);
            Assert.True(data is [2, 4, 6]);
        }

        [Fact]
        public void IArrayOfStatelessElements()
        {
            var obj = CreateWrapper<ArrayOfStatelessElements, IArrayOfStatelessElements>();
            var data = new StatelessType[10];

            // ByValueContentsOut should only free the returned values
            var oldFreeCount = StatelessTypeMarshaller.AllFreeCount;
            obj.MethodContentsOut(data, data.Length);
            Assert.Equal(oldFreeCount + 10, StatelessTypeMarshaller.AllFreeCount);

            // ByValueContentsOut should only free the elements after the call
            oldFreeCount = StatelessTypeMarshaller.AllFreeCount;
            obj.MethodContentsIn(data, data.Length);
            Assert.Equal(oldFreeCount + 10, StatelessTypeMarshaller.AllFreeCount);

            // ByValueContentsInOut should free elements in both directions
            oldFreeCount = StatelessTypeMarshaller.AllFreeCount;
            obj.MethodContentsInOut(data, data.Length);
            Assert.Equal(oldFreeCount + 20, StatelessTypeMarshaller.AllFreeCount);
        }

        [Fact]
        public void IArrayOfStatelessElementsThrows()
        {
            var obj = CreateWrapper<ArrayOfStatelessElementsThrows, IArrayOfStatelessElements>();
            var data = new StatelessType[10];
            var oldFreeCount = StatelessTypeMarshaller.AllFreeCount;
            try
            {
                obj.MethodContentsOut(data, 10);
            }
            catch (Exception) { }
            Assert.Equal(oldFreeCount, StatelessTypeMarshaller.AllFreeCount);

            for (int i = 0; i < 10; i++)
            {
                data[i] = new StatelessType() { I = i };
            }

            oldFreeCount = StatelessTypeMarshaller.AllFreeCount;
            try
            {
                obj.MethodContentsIn(data, 10);
            }
            catch (Exception) { }
            Assert.Equal(oldFreeCount + 10, StatelessTypeMarshaller.AllFreeCount);

            oldFreeCount = StatelessTypeMarshaller.AllFreeCount;
            try
            {
                obj.MethodContentsInOut(data, 10);
            }
            catch (Exception) { }
            Assert.Equal(oldFreeCount + 10, StatelessTypeMarshaller.AllFreeCount);
        }

        [Fact]
        public void IJaggedIntArray()
        {
            int[][] data = new int[][] { new int[] { 1, 2, 3 }, new int[] { 4, 5 }, new int[] { 6, 7, 8, 9 } };
            int[] widths = new int[] { 3, 2, 4 };
            int length = data.Length;

            var obj = CreateWrapper<IJaggedIntArrayImpl, IJaggedIntArray>();

            obj.Set(data, widths, length);
            Assert.Equal(data, obj.Get(out _, out _));
            _ = obj.Get2(out var value, out _);
            Assert.Equal(data, value);
        }

        [Fact]
        public void IInterface()
        {
            var iint = CreateWrapper<IIntImpl, IInt>();
            var obj = CreateWrapper<IInterfaceImpl, IInterface>();
            obj.SetInt(iint);
            _ = obj.Get();
            obj.SwapRef(ref iint);
            obj.InInt(in iint);
            obj.GetOut(out var _);
        }

        [Fact]
        public void IStatefulFinallyMarshalling()
        {
            var obj = CreateWrapper<StatefulFinallyMarshalling, IStatefulFinallyMarshalling>();
            var data = new StatefulFinallyType() { i = -10 };
            obj.Method(data);
            obj.MethodIn(in data);
            obj.MethodOut(out _);
            obj.MethodRef(ref data);
            _ = obj.Return();
            _ = obj.ReturnPreserveSig();
        }

        [Fact]
        public void IStatelessFinallyMarshalling()
        {
            var obj = CreateWrapper<StatelessFinallyMarshalling, IStatelessFinallyMarshalling>();
            var data = new StatelessFinallyType() { I = -10 };
            obj.Method(data);
            obj.MethodIn(in data);
            obj.MethodOut(out _);
            obj.MethodRef(ref data);
            _ = obj.Return();
            _ = obj.ReturnPreserveSig();
        }

        [Fact]
        public void StatefulMarshalling()
        {
            var obj = CreateWrapper<StatefulMarshalling, IStatefulMarshalling>();
            var data = new StatefulType() { i = 42 };

            int preCallFreeCount = StatefulTypeMarshaller.FreeCount;
            obj.Method(data);
            Assert.Equal(42, data.i);
            Assert.Equal(preCallFreeCount + 2, StatefulTypeMarshaller.FreeCount);

            preCallFreeCount = StatefulTypeMarshaller.FreeCount;
            obj.MethodIn(in data);
            Assert.Equal(42, data.i);
            Assert.Equal(preCallFreeCount + 2, StatefulTypeMarshaller.FreeCount);

            var oldData = data;
            preCallFreeCount = StatefulTypeMarshaller.FreeCount;
            obj.MethodRef(ref data);
            Assert.Equal(preCallFreeCount + 2, StatefulTypeMarshaller.FreeCount);
            Assert.True(oldData == data); // We want reference equality here

            preCallFreeCount = StatefulTypeMarshaller.FreeCount;
            obj.MethodOut(out data);
            Assert.Equal(preCallFreeCount + 2, StatefulTypeMarshaller.FreeCount);
            Assert.Equal(1, data.i);

            preCallFreeCount = StatefulTypeMarshaller.FreeCount;
            Assert.Equal(1, obj.Return().i);
            Assert.Equal(preCallFreeCount + 2, StatefulTypeMarshaller.FreeCount);
            preCallFreeCount = StatefulTypeMarshaller.FreeCount;
            Assert.Equal(1, obj.ReturnPreserveSig().i);
            Assert.Equal(preCallFreeCount + 2, StatefulTypeMarshaller.FreeCount);
        }

        [Fact]
        public void StatelessCallerAllocatedBufferMarshalling()
        {
            var obj = CreateWrapper<StatelessCallerAllocatedBufferMarshalling, IStatelessCallerAllocatedBufferMarshalling>();
            var data = new StatelessCallerAllocatedBufferType() { I = 42 };

            // ManagedToUnmanagedIn should use Caller Allocated Buffer and not allocate
            StatelessCallerAllocatedBufferTypeMarshaller.DisableAllocations();

            var numFreeCalls = StatelessCallerAllocatedBufferTypeMarshaller.FreeCount;
            obj.Method(data);
            Assert.Equal(1 + numFreeCalls, StatelessCallerAllocatedBufferTypeMarshaller.FreeCount);
            Assert.Equal(42, data.I);

            numFreeCalls = StatelessCallerAllocatedBufferTypeMarshaller.FreeCount;
            obj.MethodIn(in data);
            Assert.Equal(1 + numFreeCalls, StatelessCallerAllocatedBufferTypeMarshaller.FreeCount);
            Assert.Equal(42, data.I);

            // Other marshal modes will allocate
            StatelessCallerAllocatedBufferTypeMarshaller.EnableAllocations();

            numFreeCalls = StatelessCallerAllocatedBufferTypeMarshaller.FreeCount;
            obj.MethodRef(ref data);
            Assert.Equal(2 + numFreeCalls, StatelessCallerAllocatedBufferTypeMarshaller.FreeCount);
            StatelessCallerAllocatedBufferTypeMarshaller.AssertAllPointersFreed();
            Assert.Equal(200, data.I);

            numFreeCalls = StatelessCallerAllocatedBufferTypeMarshaller.FreeCount;
            obj.MethodOut(out data);
            Assert.Equal(1 + numFreeCalls, StatelessCallerAllocatedBufferTypeMarshaller.FreeCount);
            StatelessCallerAllocatedBufferTypeMarshaller.AssertAllPointersFreed();
            Assert.Equal(20, data.I);

            numFreeCalls = StatelessCallerAllocatedBufferTypeMarshaller.FreeCount;
            Assert.Equal(201, obj.Return().I);
            Assert.Equal(1 + numFreeCalls, StatelessCallerAllocatedBufferTypeMarshaller.FreeCount);

            numFreeCalls = StatelessCallerAllocatedBufferTypeMarshaller.FreeCount;
            Assert.Equal(202, obj.ReturnPreserveSig().I);
            Assert.Equal(1 + numFreeCalls, StatelessCallerAllocatedBufferTypeMarshaller.FreeCount);
        }

        [Fact]
        public void StatefulPinnedMarshalling()
        {
            var obj = CreateWrapper<StatefulPinnedMarshalling, IStatefulPinnedMarshalling>();
            var data = new StatefulPinnedType() { I = 4 };

            StatefulPinnedTypeMarshaller.ManagedToUnmanagedIn.DisableNonPinnedPath();

            obj.Method(data);
            Assert.Equal(4, data.I);

            obj.MethodIn(in data);
            Assert.Equal(4, data.I);

            StatefulPinnedTypeMarshaller.ManagedToUnmanagedIn.EnableNonPinnedPath();

            obj.MethodOut(out data);
            Assert.Equal(102, data.I);

            obj.MethodRef(ref data);
            Assert.Equal(103, data.I);

            data = obj.Return();
            Assert.Equal(104, data.I);
        }

        [Fact]
        public void ICollectionMarshallingFails()
        {
            Type hrExceptionType = SystemFindsComCalleeException() ? typeof(MarshallingFailureException) : typeof(Exception);

            var obj = CreateWrapper<ICollectionMarshallingFailsImpl, ICollectionMarshallingFails>();

            Assert.Throws<MarshallingFailureException>(() =>
                obj.Set(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0 }, 10)
            );

            Assert.Throws(hrExceptionType, () =>
                _ = obj.GetConstSize()
            );

            Assert.Throws(hrExceptionType, () =>
                _ = obj.Get(out _)
            );
        }

        [Fact]
        public void IJaggedArrayMarshallingFails()
        {
            Type hrExceptionType = SystemFindsComCalleeException() ? typeof(MarshallingFailureException) : typeof(Exception);
            var obj = CreateWrapper<IJaggedIntArrayMarshallingFailsImpl, IJaggedIntArrayMarshallingFails>();

            Assert.Throws(hrExceptionType, () =>
                _ = obj.GetConstSize()
            );

            Assert.Throws(hrExceptionType, () =>
                _ = obj.Get(out _, out _)
            );
            var array = new int[][] { new int[] { 1, 2, 3 }, new int[] { 4, 5, }, new int[] { 6, 7, 8, 9 } };
            var widths = new int[] { 3, 2, 4 };
            var length = 3;
            Assert.Throws<MarshallingFailureException>(() =>
                obj.Set(array, widths, length)
            );
        }

        [Fact]
        public void IStringArrayMarshallingFails()
        {
            var obj = CreateWrapper<IStringArrayMarshallingFailsImpl, IStringArrayMarshallingFails>();
            var hrException = SystemFindsComCalleeException() ? typeof(MarshallingFailureException) : typeof(Exception);

            var strings = IStringArrayMarshallingFailsImpl.StartingStrings;

            // All of these will marshal either to COM or the CCW will marshal on the return
            Assert.Throws<MarshallingFailureException>(() =>
            {
                obj.Param(strings);
            });
            Assert.Throws<MarshallingFailureException>(() =>
            {
                obj.RefParam(ref strings);
            });
            Assert.Throws<MarshallingFailureException>(() =>
            {
                obj.InParam(in strings);
            });
            Assert.Throws<MarshallingFailureException>(() =>
            {
                obj.ByValueInOutParam(strings);
            });
            Assert.Throws(hrException, () =>
            {
                obj.OutParam(out strings);
            });
            Assert.Throws(hrException, () =>
            {
                _ = obj.ReturnValue();
            });
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/87845")]
        [Fact]
        public void IStringArrayMarshallingFails_Failing()
        {
            var obj = CreateWrapper<IStringArrayMarshallingFailsImpl, IStringArrayMarshallingFails>();

            var strings = IStringArrayMarshallingFailsImpl.StartingStrings;
            Assert.Throws<MarshallingFailureException>(() =>
            {
                obj.ByValueOutParam(strings);
            });
        }

        [Fact]
        public unsafe void IHideWorksAsExpected()
        {
            IHide obj = CreateWrapper<HideBaseMethods, IHide3>();

            // IHide.SameMethod should be index 3
            Assert.Equal(3, obj.SameMethod());
            Assert.Equal(4, obj.DifferentMethod());

            IHide2 obj2 = (IHide2)obj;

            // IHide2.SameMethod should be index 5
            Assert.Equal(5, obj2.SameMethod());
            Assert.Equal(4, obj2.DifferentMethod());
            Assert.Equal(6, obj2.DifferentMethod2());

            IHide3 obj3 = (IHide3)obj;
            // IHide3.SameMethod should be index 7
            Assert.Equal(7, obj3.SameMethod());
            Assert.Equal(4, obj3.DifferentMethod());
            Assert.Equal(6, obj3.DifferentMethod2());
            Assert.Equal(8, obj3.DifferentMethod3());

            // Ensure each VTable method points to the correct method on HideBaseMethods
            for (int i = 3; i < 9; i++)
            {
                var (__this, __vtable_native) = ((global::System.Runtime.InteropServices.Marshalling.IUnmanagedVirtualMethodTableProvider)obj3).GetVirtualMethodTableInfoForKey(typeof(global::SharedTypes.ComInterfaces.IHide3));
                int __retVal;
                int __invokeRetVal;
                __invokeRetVal = ((delegate* unmanaged[MemberFunction]<void*, int*, int>)__vtable_native[i])(__this, &__retVal);
                Assert.Equal(i, __retVal);
            }
        }
    }
}
