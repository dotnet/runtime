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
        int[] GetReturn(out int size);
        int GetOut([MarshalUsing(CountElementName = MarshalUsingAttribute.ReturnsCountValue)] out int[] array);
        void SetContents([MarshalUsing(CountElementName = nameof(size))] int[] array, int size);
        void FillAscending([Out][MarshalUsing(CountElementName = nameof(size))] int[] array, int size);
        void Double([In, Out][MarshalUsing(CountElementName = nameof(size))] int[] array, int size);
        void PassIn([MarshalUsing(CountElementName = nameof(size))] in int[] array, int size);
        void SwapArray([MarshalUsing(CountElementName = nameof(size))] ref int[] array, int size);
    }

    [GeneratedComClass]
    internal partial class IIntArrayImpl : IIntArray
    {
        int[] _data;
        public int[] GetReturn(out int size)
        {
            size = _data.Length;
            return _data;
        }
        public int GetOut(out int[] array)
        {
            array = _data;
            return array.Length;
        }
        public void SetContents(int[] array, int size)
        {
            _data = new int[size];
            array.CopyTo(_data, 0);
        }

        public void FillAscending(int[] array, int size)
        {
            for (int i = 0; i < size; i++)
            {
                array[i] = i;
            }
        }
        public void Double(int[] array, int size)
        {
            for (int i = 0; i < size; i++)
            {
                array[i] = array[i] * 2;
            }

        }

        public void PassIn([MarshalUsing(CountElementName = "size")] in int[] array, int size)
        {
            _data = new int[size];
            array.CopyTo(_data, 0);
        }
        public void SwapArray([MarshalUsing(CountElementName = "size")] ref int[] array, int size)
        {
            var temp = _data;
            _data = array;
            array = temp;
        }
    }
}
