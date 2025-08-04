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
        [GeneratedComInterface]
        [Guid("7F0DB364-3C04-4487-9193-4BB05DC7B654")]
        internal partial interface IDerivedFromSharedType2 : SharedTypes.ComInterfaces.IGetAndSetInt
        {
            int GetTwoTimesInt();
        }

        [GeneratedComInterface]
        [Guid("7F0DB364-3C04-4487-9194-4BB05DC7B654")]
#pragma warning disable SYSLIB1230 // Specifying 'GeneratedComInterfaceAttribute' on an interface that has a base interface defined in another assembly is not supported
        internal partial interface IDerivedFromSharedType : SharedTypes.ComInterfaces.IGetAndSetInt
#pragma warning restore SYSLIB1230
        {
            int GetIntPlusOne();
        }

        [GeneratedComClass]
        [Guid("7F0DB364-3C04-4487-9195-4BB05DC7B654")]
        internal partial class DerivedFromSharedTypeImpl : IDerivedFromSharedType, IDerivedFromSharedType2
        {
            int _value = 42;

            public int GetInt() => _value;
            public int GetIntPlusOne() => _value + 1;
            public int GetTwoTimesInt() => _value * 2;
            public void SetInt(int value) { _value = value; }
        }

        [Fact]
        public unsafe void TypesDerivedFromSharedTypeHaveCorrectVTableSize()
        {
            var managedSourceObject = new DerivedFromSharedTypeImpl();
            var cw = new StrategyBasedComWrappers();
            var nativeObj = cw.GetOrCreateComInterfaceForObject(managedSourceObject, CreateComInterfaceFlags.None);
            object managedObj = cw.GetOrCreateObjectForComInstance(nativeObj, CreateObjectFlags.None);
            IGetAndSetInt getAndSetInt = (IGetAndSetInt)managedObj;
            IDerivedFromSharedType derivedFromSharedType = (IDerivedFromSharedType)managedObj;
            IDerivedFromSharedType2 derivedFromSharedType2 = (IDerivedFromSharedType2)managedObj;

            Assert.Equal(42, getAndSetInt.GetInt());
            Assert.Equal(42, derivedFromSharedType.GetInt());
            Assert.Equal(42, derivedFromSharedType2.GetInt());

            getAndSetInt.SetInt(100);
            Assert.Equal(100, getAndSetInt.GetInt());
            Assert.Equal(101, derivedFromSharedType.GetIntPlusOne());
            Assert.Equal(200, derivedFromSharedType2.GetTwoTimesInt());
        }


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
            var derivedImpl = new Derived();
            var nativeObj = cw.GetOrCreateComInterfaceForObject(derivedImpl, CreateComInterfaceFlags.None);
            var obj = cw.GetOrCreateObjectForComInstance(nativeObj, CreateObjectFlags.None);
            IDerived iface = (IDerived)obj;

            Assert.Equal(0, iface.GetInt());
            iface.SetInt(5);
            Assert.Equal(5, iface.GetInt());

            Assert.Equal("hello", iface.GetName());
            iface.SetName("updated");
            Assert.Equal("updated", iface.GetName());

            var iUnknownStrategyProperty = typeof(ComObject).GetProperty("IUnknownStrategy", BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.NotNull(iUnknownStrategyProperty);

            var qiCallCountObj = iUnknownStrategyProperty!.GetValue(obj);
            var countQi = (SingleQIComWrapper.CountQI)qiCallCountObj;
            Assert.Equal(1, countQi.QiCallCount);
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
