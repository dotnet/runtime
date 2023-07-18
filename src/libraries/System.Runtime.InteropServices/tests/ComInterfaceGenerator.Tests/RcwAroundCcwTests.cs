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
            var obj = CreateWrapper<Derived, IDerived>();
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
            obj.Set(data, length);
            Assert.Equal(data, obj.Get(out int _));
            obj.Get2(out var value);
            Assert.Equal(data, value);
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
        public void ICollectionMarshallingFails()
        {
            var obj = CreateWrapper<ICollectionMarshallingFailsImpl, ICollectionMarshallingFails>();

            Assert.Throws<ArgumentException>(() =>
                _ = obj.Get()
            );
            Assert.Throws<ArgumentException>(() =>
                obj.Set(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0 })
            );
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/88111")]
        [Fact]
        public void IJaggedArrayMarshallingFails()
        {
            var obj = CreateWrapper<IJaggedIntArrayMarshallingFailsImpl, IJaggedIntArrayMarshallingFails>();

            Assert.Throws<ArgumentException>(() =>
                _ = obj.Get(out _, out _)
            );
            var array = new int[][] { new int[] { 1, 2, 3 }, new int[] { 4, 5, }, new int[] { 6, 7, 8, 9 } };
            var length = 3;
            var widths = new int[] { 3, 2, 4 };
            Assert.Throws<ArgumentException>(() =>
                obj.Set(array, widths, length)
            );
        }

        [Fact]
        public void IStringArrayMarshallingFails()
        {
            var obj = CreateWrapper<IStringArrayMarshallingFailsImpl, IStringArrayMarshallingFails>();

            var strings = IStringArrayMarshallingFailsImpl.StartingStrings;

            // All of these will marshal either to COM or the CCW will marshal on the return
            Assert.Throws<ArgumentException>(() =>
            {
                obj.Param(strings);
            });
            Assert.Throws<ArgumentException>(() =>
            {
                obj.RefParam(ref strings);
            });
            Assert.Throws<ArgumentException>(() =>
            {
                obj.InParam(in strings);
            });
            Assert.Throws<ArgumentException>(() =>
            {
                obj.OutParam(out strings);
            });
            // https://github.com/dotnet/runtime/issues/87845
            //Assert.Throws<ArgumentException>(() =>
            //{
            //    obj.ByValueOutParam(strings);
            //});
            Assert.Throws<ArgumentException>(() =>
            {
                obj.ByValueInOutParam(strings);
            });
            Assert.Throws<ArgumentException>(() =>
            {
                _ = obj.ReturnValue();
            });
        }
    }
}
