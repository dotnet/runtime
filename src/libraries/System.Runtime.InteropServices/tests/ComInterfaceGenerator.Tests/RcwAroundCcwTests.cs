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
using static ComInterfaceGenerator.Tests.ComInterfaces;

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

        [Fact]
        public void IBool()
        {
            var obj = CreateWrapper<IBoolImpl, IBool>();
            obj.Set(true);
            Assert.True(obj.Get());
        }

        [Fact]
        public void IFloat()
        {
            var obj = CreateWrapper<IFloatImpl, IFloat>();
            obj.Set(2.71F);
            _ = obj.Get();
        }

        [Fact]
        public void IIntArray()
        {
            var obj = CreateWrapper<IIntArrayImpl, IIntArray>();
            int[] data = new int[] { 1, 2, 3 };
            int length = data.Length;
            obj.Set(data, length);
            _ = obj.Get(out int _);
            _ = obj.Get2(out var _);
        }

        [Fact]
        public void IJaggedIntArray()
        {
            int[][] expectedData = new int[][] { new int[] { 1, 2, 3 }, new int[] { 4, 5 }, new int[] { 6, 7, 8, 9 } };
            int[] expectedWidths = new int[] { 3, 2, 4 };
            int expectedLength = expectedData.Length;

            var obj = CreateWrapper<IJaggedIntArrayImpl, IJaggedIntArray>();

            obj.Set(expectedData, expectedWidths, expectedLength);
            _ = obj.Get(out int[] actualWidths, out int actualLength);
            _ = obj.Get2(out var _, out var _);
        }

        [Fact]
        public void IInterface()
        {
            var iint = CreateWrapper<IIntImpl, IInt>();
            var obj = CreateWrapper<IInterfaceImpl, IInterface>();
            obj.Set(iint);
            _ = obj.Get();
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/87139")]
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

        [ActiveIssue("https://github.com/dotnet/runtime/issues/87139")]
        [Fact]
        public void IJaggedIntArrayMarshallingFails()
        {
            var obj = CreateWrapper<IJaggedIntArrayMarshallingFailsImpl, IJaggedIntArrayMarshallingFails>();

            Assert.Throws<ArgumentException>(() =>
            _ = obj.Get(out _, out _)
            );
            Assert.Throws<ArgumentException>(() =>
            _ = obj.Get2(out _, out _)
            );
            Assert.Throws<ArgumentException>(() =>
                obj.Set(new int[][] { new int[] { 1 }, new int[] { 1, 2, 3 }, new int[] { 2, 3, 4, 5 } }, new int[] {1, 3, 4}, 3)
            );
        }
    }

    public static partial class ComInterfaces
    {
        [GeneratedComInterface]
        [Guid("EE6D1F2A-3418-4317-A87C-35488F6546AB")]
        internal partial interface IInt
        {
            public int Get();
            public void Set(int value);
        }

        [GeneratedComClass]
        internal partial class IIntImpl : IInt
        {
            int _data;
            public int Get() => _data;
            public void Set(int value) => _data = value;
        }

        [GeneratedComInterface]
        [Guid("5A9D3ED6-CC17-4FB9-8F82-0070489B7213")]
        internal partial interface IBool
        {
            [return: MarshalAs(UnmanagedType.I1)]
            bool Get();
            void Set([MarshalAs(UnmanagedType.I1)] bool value);
        }

        [GeneratedComClass]
        internal partial class IBoolImpl : IBool
        {
            bool _data;
            public bool Get() => _data;
            public void Set(bool value) => _data = value;
        }

        [GeneratedComInterface]
        [Guid("9FA4A8A9-2D8F-48A8-B6FB-B44B5F1B9FB6")]
        internal partial interface IFloat
        {
            float Get();
            void Set(float value);
        }

        [GeneratedComClass]
        internal partial class IFloatImpl : IFloat
        {
            float _data;
            public float Get() => _data;
            public void Set(float value) => _data = value;
        }

        [GeneratedComInterface]
        [Guid("9FA4A8A9-3D8F-48A8-B6FB-B45B5F1B9FB6")]
        internal partial interface IIntArray
        {
            [return: MarshalUsing(CountElementName = nameof(size))]
            int[] Get(out int size);
            int Get2([MarshalUsing(CountElementName = MarshalUsingAttribute.ReturnsCountValue)] out int[] array);
            void Set([MarshalUsing(CountElementName = nameof(size))] int[] array, int size);
        }

        [GeneratedComClass]
        internal partial class IIntArrayImpl : IIntArray
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
        internal partial interface IJaggedIntArray
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

        [CustomMarshaller(typeof(int), MarshalMode.ElementIn | MarshalMode.ElementOut | MarshalMode.Default, typeof(ThrowOn4thElementMarshalled))]
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

        [GeneratedComInterface]
        [Guid("A4857395-06FB-4A6E-81DB-35461BE999C5")]
        internal partial interface ICollectionMarshallingFails
        {
            [return: MarshalUsing(ConstantElementCount = 10)]
            [return: MarshalUsing(typeof(ThrowOn4thElementMarshalled), ElementIndirectionDepth = 1)]
            public int[] Get();
            public void Set(
                [MarshalUsing(ConstantElementCount = 10)]
                [MarshalUsing(typeof(ThrowOn4thElementMarshalled), ElementIndirectionDepth = 1)]
                int[] value);
        }

        [GeneratedComClass]
        public partial class ICollectionMarshallingFailsImpl : ICollectionMarshallingFails
        {
            int[] _data = new[] { 1, 2, 3 };
            public int[] Get() => _data;
            public void Set(int[] value) => _data = value;
        }

        [GeneratedComInterface]
        [Guid("9FA4A8A9-3D8F-48A8-B6FB-B45B5F1B9FB6")]
        internal partial interface IJaggedIntArrayMarshallingFails
        {
            [return: MarshalUsing(CountElementName = nameof(length)),
                MarshalUsing(ElementIndirectionDepth = 1, CountElementName = nameof(widths)),
                MarshalUsing(typeof(ThrowOn4thElementMarshalled), ElementIndirectionDepth =2)]
            int[][] Get(
                [MarshalUsing(CountElementName = nameof(length))]
                out int[] widths,
                out int length);

            int Get2(
                [MarshalUsing(CountElementName = MarshalUsingAttribute.ReturnsCountValue),
                MarshalUsing(ElementIndirectionDepth = 1, CountElementName = nameof(widths)),
                MarshalUsing(typeof(ThrowOn4thElementMarshalled), ElementIndirectionDepth =2)]
                out int[][] array,
                [MarshalUsing(CountElementName = MarshalUsingAttribute.ReturnsCountValue)]
                out int[] widths);

            void Set(
                [MarshalUsing(CountElementName = nameof(length)),
                MarshalUsing(ElementIndirectionDepth = 1, CountElementName = nameof(widths)),
                MarshalUsing(typeof(ThrowOn4thElementMarshalled), ElementIndirectionDepth =2)]
                int[][] array,
                [MarshalUsing(CountElementName = nameof(length))]
                int[] widths,
                int length);
        }

        [GeneratedComClass]
        internal partial class IJaggedIntArrayMarshallingFailsImpl : IJaggedIntArrayMarshallingFails
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

        [GeneratedComInterface]
        [Guid("A4857398-06FB-4A6E-81DB-35461BE999C5")]
        internal partial interface IInterface
        {
            public IInt Get();
            public void Set(IInt value);
        }

        [GeneratedComClass]
        public partial  class IInterfaceImpl: IInterface
        {
            IInt _data = new IIntImpl();
            IInt IInterface.Get() => _data;
            void IInterface.Set(IInt value)
            {
                int x = value.Get();
                value.Set(x);
                _data = value;
            }
        }
    }
}
