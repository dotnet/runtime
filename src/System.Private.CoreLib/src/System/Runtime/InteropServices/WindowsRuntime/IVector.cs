// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

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
        T GetAt(uint index);
        uint Size { get; }
        IReadOnlyList<T> GetView();  // Really an IVectorView<T>.
        bool IndexOf(T value, out uint index);
        void SetAt(uint index, T value);
        void InsertAt(uint index, T value);
        void RemoveAt(uint index);
        void Append(T value);
        void RemoveAtEnd();
        void Clear();
        uint GetMany(uint startIndex, [Out] T[] items);
        void ReplaceAll(T[] items);
    }

    // Same as IVector - the only difference is that GetView returns IVectorView<T>
    [ComImport]
    [Guid("913337e9-11a1-4345-a3a2-4e7f956e222d")]
    [WindowsRuntimeImport]
    internal interface IVector_Raw<T> : IIterable<T>
    {
        T GetAt(uint index);
        uint Size { get; }
        IVectorView<T> GetView();
        bool IndexOf(T value, out uint index);
        void SetAt(uint index, T value);
        void InsertAt(uint index, T value);
        void RemoveAt(uint index);
        void Append(T value);
        void RemoveAtEnd();
        void Clear();
    }

    [ComImport]
    [Guid("bbe1fa4c-b0e3-4583-baef-1f1b2e483e56")]
    [WindowsRuntimeImport]
    internal interface IVectorView<T> : IIterable<T>
    {
        T GetAt(uint index);
        uint Size { get; }
        bool IndexOf(T value, out uint index);
        uint GetMany(uint startIndex, [Out] T[] items);
    }

    [ComImport]
    [Guid("393de7de-6fd0-4c0d-bb71-47244a113e93")]
    [WindowsRuntimeImport]
    internal interface IBindableVector : IBindableIterable
    {
        object? GetAt(uint index);
        uint Size { get; }
        IBindableVectorView GetView();
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
        object? GetAt(uint index);
        uint Size { get; }
        bool IndexOf(object value, out uint index);
    }
}
