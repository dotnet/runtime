// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface]
    [Guid("5A9D3ED6-CC17-4FB9-8F82-0070489B7213")]
    internal partial interface IBool
    {
        [return: MarshalAs(UnmanagedType.I1)]
        bool Get();
        void Set([MarshalAs(UnmanagedType.I1)] bool value);
    }

    [GeneratedComClass]
    internal partial class IBoolImpl : IBool
    {
        bool _data;
        public bool Get() => _data;
        public void Set(bool value) => _data = value;
    }
}
