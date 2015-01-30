﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

// Windows.Foundation.Collections.IVector`1 and IVectorView`1 cannot be referenced from managed
// code because they're hidden by the metadata adapter. We redeclare the interfaces manually
// to be able to talk to native WinRT objects.
namespace System.Runtime.InteropServices.WindowsRuntime
{
    [ComImport]
    [Guid("913337e9-11a1-4345-a3a2-4e7f956e222d")]
    [WindowsRuntimeImport]
    internal interface IVector<T> : IIterable<T>
    {
        [Pure]
        T GetAt(uint index);
        [Pure]
        uint Size { get; }
        [Pure]
        IReadOnlyList<T> GetView();  // Really an IVectorView<T>.
        [Pure]
        bool IndexOf(T value, out uint index);
        void SetAt(uint index, T value);
        void InsertAt(uint index, T value);
        void RemoveAt(uint index);
        void Append(T value);
        void RemoveAtEnd();
        void Clear();
        [Pure]
        uint GetMany(uint startIndex, [Out] T[] items);
        void ReplaceAll(T[] items);
    }

    // Same as IVector - the only difference is that GetView returns IVectorView<T>
    [ComImport]
    [Guid("913337e9-11a1-4345-a3a2-4e7f956e222d")]
    [WindowsRuntimeImport]
    internal interface IVector_Raw<T> : IIterable<T>
    {
        [Pure]
        T GetAt(uint index);
        [Pure]
        uint Size { get; }
        [Pure]
        IVectorView<T> GetView();
        [Pure]
        bool IndexOf(T value, out uint index);
        void SetAt(uint index, T value);
        void InsertAt(uint index, T value);
        void RemoveAt(uint index);
        void Append(T value);
        void RemoveAtEnd();
        void Clear();
        [Pure]
        uint GetMany(uint startIndex, [Out] T[] items);
        void ReplaceAll(T[] items);
    }

    [ComImport]
    [Guid("bbe1fa4c-b0e3-4583-baef-1f1b2e483e56")]
    [WindowsRuntimeImport]
    internal interface IVectorView<T> : IIterable<T>
    {
        [Pure]
        T GetAt(uint index);
        [Pure]
        uint Size { get; }
        [Pure]
        bool IndexOf(T value, out uint index);
        [Pure]
        uint GetMany(uint startIndex, [Out] T[] items);
    }

    [ComImport]
    [Guid("393de7de-6fd0-4c0d-bb71-47244a113e93")]
    [WindowsRuntimeImport]
    internal interface IBindableVector : IBindableIterable
    {
        [Pure]
        object GetAt(uint index);
        [Pure]
        uint Size { get; }
        [Pure]
        IBindableVectorView GetView();
        [Pure]
        bool IndexOf(object value, out uint index);
        void SetAt(uint index, object value);
        void InsertAt(uint index, object value);
        void RemoveAt(uint index);
        void Append(object value);
        void RemoveAtEnd();
        void Clear();
    }

    [ComImport]
    [Guid("346dd6e7-976e-4bc3-815d-ece243bc0f33")]
    [WindowsRuntimeImport]
    internal interface IBindableVectorView : IBindableIterable
    {
        [Pure]
        object GetAt(uint index);
        [Pure]
        uint Size { get; }
        [Pure]
        bool IndexOf(object value, out uint index);
    }
}
