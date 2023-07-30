// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using SharedTypes.ComInterfaces;
using Xunit;

namespace ComInterfaceGenerator.Tests
{
    public partial class IDerivedTests
    {
        [Fact]
        public unsafe void DerivedInterfaceTypeProvidesBaseInterfaceUnmanagedToManagedMembers()
        {
            // Make sure that we have the correct derived and base types here.
            Assert.Contains(typeof(IGetAndSetInt), typeof(IDerived).GetInterfaces());

            IIUnknownDerivedDetails baseInterfaceDetails = StrategyBasedComWrappers.DefaultIUnknownInterfaceDetailsStrategy.GetIUnknownDerivedDetails(typeof(IGetAndSetInt).TypeHandle);
            IIUnknownDerivedDetails derivedInterfaceDetails = StrategyBasedComWrappers.DefaultIUnknownInterfaceDetailsStrategy.GetIUnknownDerivedDetails(typeof(IDerived).TypeHandle);

            var numBaseMethods = typeof(IGetAndSetInt).GetMethods().Length;

            var numPointersToCompare = 3 + numBaseMethods;

            var expected = new ReadOnlySpan<nint>(baseInterfaceDetails.ManagedVirtualMethodTable, numPointersToCompare);
            var actual = new ReadOnlySpan<nint>(derivedInterfaceDetails.ManagedVirtualMethodTable, numPointersToCompare);

            Assert.True(expected.SequenceEqual(actual));
        }

        [Fact]
        public unsafe void CallBaseInterfaceMethod_EnsureQiCalledOnce()
        {
            var cw = new SingleQIComWrapper();
            var derivedImpl = new DerivedImpl();
            var nativeObj = cw.GetOrCreateComInterfaceForObject(derivedImpl, CreateComInterfaceFlags.None);
            var obj = cw.GetOrCreateObjectForComInstance(nativeObj, CreateObjectFlags.None);
            IDerived iface = (IDerived)obj;

            Assert.Equal(3, iface.GetInt());
            iface.SetInt(5);
            Assert.Equal(5, iface.GetInt());

            Assert.Equal("myName", iface.GetName());
            iface.SetName("updated");
            Assert.Equal("updated", iface.GetName());

            var iUnknownStrategyProperty = typeof(ComObject).GetProperty("IUnknownStrategy", BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.NotNull(iUnknownStrategyProperty);

            var qiCallCountObj = iUnknownStrategyProperty!.GetValue(obj);
            var countQi = (SingleQIComWrapper.CountQI)qiCallCountObj;
            Assert.Equal(1, countQi.QiCallCount);
        }

        [GeneratedComClass]
        partial class DerivedImpl : IDerived
        {
            int data = 3;
            string myName = "myName";
            public void DoThingWithString(string name) => throw new NotImplementedException();

            public int GetInt() => data;

            public string GetName() => myName;
            [return: MarshalUsing(typeof(Utf16StringMarshaller))]
            public string asdfasdf([MarshalUsing(CountElementName = "size2", ElementIndirectionDepth = 2), MarshalUsing(CountElementName = "size11", ElementIndirectionDepth = 1), MarshalUsing(CountElementName = "size", ElementIndirectionDepth = 0)] int[][][] asdf, [MarshalUsing(CountElementName = "size1", ElementIndirectionDepth = 1), MarshalUsing(CountElementName = "size0")] int[][] size2, [MarshalUsing(CountElementName = "size")] int[] size1, [MarshalUsing(CountElementName = "size0")] int[] size11, int size, int size0) => throw new NotImplementedException();
            public void SetInt(int n) => data = n;

            public void SetName(string name) => myName = name;
            [return: MarshalUsing(typeof(Utf16StringMarshaller))]
            public string asdfasdf([MarshalUsing(CountElementName = "size2", ElementIndirectionDepth = 2), MarshalUsing(CountElementName = "size11", ElementIndirectionDepth = 1), MarshalUsing(CountElementName = "size", ElementIndirectionDepth = 0)] ref int[][][] asdf, [MarshalUsing(CountElementName = "size1", ElementIndirectionDepth = 1), MarshalUsing(CountElementName = "size0")] ref int[][] size2, [MarshalUsing(CountElementName = "size")] ref int[] size1, [MarshalUsing(CountElementName = "size0")] ref int[] size11, ref int size, ref int size0) => throw new NotImplementedException();
        }

        /// <summary>
        /// Used to ensure that QI is only called once when calling base methods on a derived COM interface
        /// </summary>
        class SingleQIComWrapper : StrategyBasedComWrappers
        {
            public class CountQI : IIUnknownStrategy
            {
                public CountQI(IIUnknownStrategy iUnknown) => _iUnknownStrategy = iUnknown;
                private IIUnknownStrategy _iUnknownStrategy;
                public int QiCallCount = 0;
                public unsafe void* CreateInstancePointer(void* unknown) => _iUnknownStrategy.CreateInstancePointer(unknown);
                public unsafe int QueryInterface(void* instancePtr, in Guid iid, out void* ppObj)
                {
                    QiCallCount++;
                    return _iUnknownStrategy.QueryInterface(instancePtr, in iid, out ppObj);
                }
                public unsafe int Release(void* instancePtr) => _iUnknownStrategy.Release(instancePtr);
            }

            protected override IIUnknownStrategy GetOrCreateIUnknownStrategy()
                => new CountQI(base.GetOrCreateIUnknownStrategy());
        }
    }
}
