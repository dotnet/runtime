// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
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
}
