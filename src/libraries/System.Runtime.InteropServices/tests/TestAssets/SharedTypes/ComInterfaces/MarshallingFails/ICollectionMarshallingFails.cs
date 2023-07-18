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
        public int[] Get();
        public void Set(
            [MarshalUsing(ConstantElementCount = 10)]
            [MarshalUsing(typeof(ThrowOn4thElementMarshalled), ElementIndirectionDepth = 1)]
            int[] value);
    }

    [GeneratedComClass]
    internal partial class ICollectionMarshallingFailsImpl : ICollectionMarshallingFails
    {
        int[] _data = new[] { 1, 2, 3 };
        public int[] Get() => _data;
        public void Set(int[] value) => _data = value;
    }
}
