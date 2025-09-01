// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface(), Guid("0A52B77C-E08B-4274-A1F4-1A2BF2C07E60")]
    partial interface IStatefulCollectionBlittableElement
    {
        void Method(
            [MarshalUsing(CountElementName = nameof(size))] StatefulCollection<int> p,
            int size);
        void MethodIn(
            [MarshalUsing(CountElementName = nameof(size))] in StatefulCollection<int> pIn,
            in int size);
        void MethodRef(
            [MarshalUsing(CountElementName = nameof(size))] ref StatefulCollection<int> pRef,
            int size);
        void MethodOut(
            [MarshalUsing(CountElementName = nameof(size))] out StatefulCollection<int> pOut,
            out int size);
        [return: MarshalUsing(CountElementName = nameof(size))]
        StatefulCollection<int> Return(int size);
        [PreserveSig]
        [return: MarshalUsing(CountElementName = nameof(size))]
        StatefulCollection<int> ReturnPreserveSig(int size);
    }
}
