// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces.MarshallingFails
{
    [GeneratedComInterface]
    [Guid("A4857395-06FB-4A6E-81DB-35461BE999C5")]
    internal partial interface ICollectionMarshallingFails
    {
        [return: MarshalUsing(ConstantElementCount = 10)]
        [return: MarshalUsing(typeof(ThrowOn4thElementMarshalled), ElementIndirectionDepth = 1)]
        public int[] GetConstSize();

        [return: MarshalUsing(CountElementName = nameof(size))]
        [return: MarshalUsing(typeof(ThrowOn4thElementMarshalled), ElementIndirectionDepth = 1)]
        public int[] Get(out int size);

        public void Set(
            [MarshalUsing(CountElementName = nameof(size))]
            [MarshalUsing(typeof(ThrowOn4thElementMarshalled), ElementIndirectionDepth = 1)]
            int[] value, int size);
    }

    [GeneratedComClass]
    internal partial class ICollectionMarshallingFailsImpl : ICollectionMarshallingFails
    {
        int[] _data = new[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        public int[] Get(out int size)
        {
            size = _data.Length;
            return _data;
        }

        [return: MarshalUsing(ConstantElementCount = 10), MarshalUsing(typeof(ThrowOn4thElementMarshalled), ElementIndirectionDepth = 1)]
        public int[] GetConstSize() => new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

        public void Set(int[] value, int size)
        {
            _data = new int[size];
            value.CopyTo(_data, 0);
        }
    }
}
