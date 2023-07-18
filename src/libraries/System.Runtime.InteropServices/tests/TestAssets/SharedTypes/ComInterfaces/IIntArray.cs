// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
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
}
