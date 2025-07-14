// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface]
    [Guid(IID)]
#pragma warning disable SYSLIB1230 // Specifying 'GeneratedComInterfaceAttribute' on an interface that has a base interface defined in another assembly is not supported
    internal partial interface IDerived : IGetAndSetInt
#pragma warning restore SYSLIB1230
    {
        void SetName([MarshalUsing(typeof(Utf16StringMarshaller))] string name);

        [return: MarshalUsing(typeof(Utf16StringMarshaller))]
        string GetName();

        internal new const string IID = "7F0DB364-3C04-4487-9193-4BB05DC7B654";
    }

    [GeneratedComClass]
    internal partial class Derived : GetAndSetInt, IDerived
    {
        string _data = "hello";
        public string GetName() => _data;
        public void SetName(string name) => _data = name;
    }
}
