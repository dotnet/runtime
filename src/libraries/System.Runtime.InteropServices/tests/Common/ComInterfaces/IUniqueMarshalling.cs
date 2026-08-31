// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface]
    [Guid(IID)]
    [NativeMarshalling(typeof(UniqueComInterfaceMarshaller<IUniqueMarshalling>))]
    internal partial interface IUniqueMarshalling
    {
        int GetValue();
        void SetValue(int x);
        IUniqueMarshalling GetThis();

        public const string IID = "E11D5F3E-DD57-4E7E-A78C-F5F8B8E0A1F4";
    }

    [GeneratedComClass]
    internal partial class UniqueMarshalling : IUniqueMarshalling
    {
        int _data = 0;
        public int GetValue() => _data;
        public void SetValue(int x) => _data = x;
        public IUniqueMarshalling GetThis() => this;
    }
}
