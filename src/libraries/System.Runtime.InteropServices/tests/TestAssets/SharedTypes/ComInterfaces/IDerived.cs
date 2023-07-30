// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface]
    [Guid(_guid)]
    internal partial interface IDerived : IGetAndSetInt
    {
        void SetName([MarshalUsing(typeof(Utf16StringMarshaller))] string name);

        [return: MarshalUsing(typeof(Utf16StringMarshaller))]
        string GetName();
        [return: MarshalUsing(typeof(Utf16StringMarshaller))]
        string asdfasdf(
            [MarshalUsing(CountElementName = nameof(size2), ElementIndirectionDepth = 2),
            MarshalUsing(CountElementName = nameof(size11), ElementIndirectionDepth = 1),
            MarshalUsing(CountElementName =nameof(size), ElementIndirectionDepth = 0)]
            ref int[][][] asdf,
            [MarshalUsing(CountElementName = nameof(size1), ElementIndirectionDepth = 1)]
            [MarshalUsing(CountElementName = nameof(size0))]
            ref int[][] size2,
            [MarshalUsing(CountElementName = nameof(size))]
            ref int[] size1,
            [MarshalUsing(CountElementName = nameof(size0))]
            ref int[] size11,
            ref int size,
            ref int size0);

        internal new const string _guid = "7F0DB364-3C04-4487-9193-4BB05DC7B654";
    }

    [GeneratedComClass]
    internal partial class Derived : GetAndSetInt, IDerived
    {
        string _data = "hello";
        public string GetName() => _data;
        [return: MarshalUsing(typeof(Utf16StringMarshaller))]
        public string asdfasdf([MarshalUsing(CountElementName = "size2", ElementIndirectionDepth = 2), MarshalUsing(CountElementName = "size11", ElementIndirectionDepth = 1), MarshalUsing(CountElementName = "size", ElementIndirectionDepth = 0)] int[][][] asdf, [MarshalUsing(CountElementName = "size1", ElementIndirectionDepth = 1), MarshalUsing(CountElementName = "size0")] int[][] size2, [MarshalUsing(CountElementName = "size")] int[] size1, [MarshalUsing(CountElementName = "size0")] int[] size11, int size, int size0) => throw new NotImplementedException();
        public void SetName(string name) => _data = name;
        [return: MarshalUsing(typeof(Utf16StringMarshaller))]
        public string asdfasdf([MarshalUsing(CountElementName = "size2", ElementIndirectionDepth = 2), MarshalUsing(CountElementName = "size11", ElementIndirectionDepth = 1), MarshalUsing(CountElementName = "size", ElementIndirectionDepth = 0)] ref int[][][] asdf, [MarshalUsing(CountElementName = "size1", ElementIndirectionDepth = 1), MarshalUsing(CountElementName = "size0")] ref int[][] size2, [MarshalUsing(CountElementName = "size")] ref int[] size1, [MarshalUsing(CountElementName = "size0")] ref int[] size11, ref int size, ref int size0) => throw new NotImplementedException();
    }
}
