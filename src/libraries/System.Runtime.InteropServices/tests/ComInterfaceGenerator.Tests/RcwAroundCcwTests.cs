// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SharedTypes.ComInterfaces;
using Xunit;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

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
        public void IGetAndSetInt()
        {
            var obj = CreateWrapper<GetAndSetInt, IGetAndSetInt>();
            obj.SetInt(0);
            _ = obj.GetInt();
        }


        [Fact]
        public void IDerived()
        {
            var obj = CreateWrapper<Derived, IDerived>();
            _ = obj.GetInt();
            obj.SetInt(0);
            obj.SetName("A");
            _ = obj.GetName();
        }

    }
    public static partial class ComInterfaces
    {
        [GeneratedComInterface]
        [Guid("EE6D1F2A-3418-4317-A87C-35488F6546AB")]
        internal interface IInt
        {
            public int Get();
            public void Set(int value);
        }

        [GeneratedComClass]
        internal class IIntImpl : IInt
        {
            int _data;
            public int Get() => _data;
            public void Set(int value) => _data = value;
        }

        [GeneratedComInterface]
        [Guid("5A9D3ED6-CC17-4FB9-8F82-0070489B7213")]
        internal interface IBool
        {
            [return: MarshalAs(UnmanagedType.I1)]
            bool Get();
            void Set([MarshalAs(UnmanagedType.I1)] bool value);
        }

        [GeneratedComClass]
        internal class IBoolImpl : IBool
        {
            bool _data;
            public bool Get() => _data;
            public void Set(bool value) => _data = value;
        }

        [GeneratedComInterface]
        [Guid("9FA4A8A9-2D8F-48A8-B6FB-B44B5F1B9FB6")]
        internal interface IFloat
        {
            float Get();
            void Set(float value);
        }

        [GeneratedComClass]
        internal class IFloatImpl : IFloat
        {
            float _data;
            public float Get() => _data;
            public void Set(float value) => _data = value;
        }

        [GeneratedComInterface]
        [Guid("9FA4A8A9-3D8F-48A8-B6FB-B45B5F1B9FB6")]
        internal interface IIntArray
        {
            [return: MarshalUsing(CountElementName = nameof(size))]
            int[] Get(out int size);
            int Get2([MarshalUsing(CountElementName = MarshalUsingAttribute.ReturnsCountValue)] out int[] array);
            void Set([MarshalUsing(CountElementName = nameof(size))] int[] array, int size);
        }

        [GeneratedComClass]
        internal class IIntArrayImpl : IIntArray
        {
            int[] _data;
            public int[] Get(out int size)
            {
                size = _data.Length;
                return _data;
            }
            public int Get2(out int[] array)
            {
                array = _data;
                return array.Length;
            }
            public void Set(int[] array, int size)
            {
                _data = array;
            }
        }

        [GeneratedComInterface]
        [Guid("9FA4A8A9-3D8F-48A8-B6FB-B45B5F1B9FB6")]
        internal interface IJaggedIntArray
        {
            [return: MarshalUsing(CountElementName = nameof(length)),
                MarshalUsing(ElementIndirectionDepth = 1, CountElementName = nameof(widths))]
            int[][] Get(
                [MarshalUsing(CountElementName = nameof(length))]
                out int[] widths,
                out int length);

            int Get2(
                [MarshalUsing(CountElementName = MarshalUsingAttribute.ReturnsCountValue),
                MarshalUsing(ElementIndirectionDepth = 1, CountElementName = nameof(widths))]
                out int[][] array,
                [MarshalUsing(CountElementName = MarshalUsingAttribute.ReturnsCountValue)]
                out int[] widths);

            void Set(
                [MarshalUsing(CountElementName = nameof(length)),
                MarshalUsing(ElementIndirectionDepth = 1, CountElementName = nameof(widths))]
                int[][] array,
                [MarshalUsing(CountElementName = nameof(length))]
                int[] widths,
                int length);
        }

        [GeneratedComClass]
        internal partial class IJaggedIntArrayImpl : IJaggedIntArray
        {
            int[][] _data = new int[][] { new int[] { 1, 2, 3 }, new int[] { 4, 5 }, new int[] { 6, 7, 8, 9 } };
            int[] _widths = new int[] { 3, 2, 4 };
            public int[][] Get(out int[] widths, out int length)
            {
                widths = _widths;
                length = _data.Length;
                return _data;
            }
            public int Get2(out int[][] array, out int[] widths)
            {
                array = _data;
                widths = _widths;
                return array.Length;
            }
            public void Set(int[][] array, int[] widths, int length)
            {
                _data = array;
                _widths = widths;
            }
        }
    }

    [CustomMarshaller(typeof(int), MarshalMode.Default, typeof(ThrowOn4thElementMarshalled))]
    internal static class ThrowOn4thElementMarshalled
    {
        static int _marshalledCount = 0;
        public static nint ConvertToUnmanaged(int managed)
        {
            if (_marshalledCount++ == 3)
            {
                _marshalledCount = 0;
                throw new ArgumentException("The element was the 4th element (with 0-based index 3)");
            }
            return managed;
        }

        public static int ConvertToManaged(nint unmanaged)
        {
            return (int)unmanaged;
        }
    }

    //internal partial interface
}
