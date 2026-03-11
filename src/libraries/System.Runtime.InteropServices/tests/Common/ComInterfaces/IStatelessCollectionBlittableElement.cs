// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface(), Guid("0A52B77C-E08B-4274-A1F4-1A2BF2C07E60")]
    partial interface IStatelessCollectionBlittableElement
    {
        void Method(
            [MarshalUsing(CountElementName = nameof(size))] StatelessCollection<int> p,
            int size);
        void MethodIn(
            [MarshalUsing(CountElementName = nameof(size))] in StatelessCollection<int> pIn,
            in int size);
        void MethodRef(
            [MarshalUsing(CountElementName = nameof(size))] ref StatelessCollection<int> pRef,
            int size);
        void MethodOut(
            [MarshalUsing(CountElementName = nameof(size))] out StatelessCollection<int> pOut,
            out int size);
        [return: MarshalUsing(CountElementName = nameof(size))]
        StatelessCollection<int> Return(int size);
        [PreserveSig]
        [return: MarshalUsing(CountElementName = nameof(size))]
        StatelessCollection<int> ReturnPreserveSig(int size);
    }
}
