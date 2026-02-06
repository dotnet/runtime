// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface]
    [Guid(IID)]
    internal partial interface IDerived : IGetAndSetInt
    {
        void SetName([MarshalUsing(typeof(Utf16StringMarshaller))] string name);

        [return: MarshalUsing(typeof(Utf16StringMarshaller))]
        string GetName();

        internal new const string IID = "7F0DB364-3C04-4487-9193-4BB05DC7B654";
    }
    [GeneratedComInterface]
    [Guid("D38D8B40-54A4-4685-B048-D04E215E6A93")]
    internal partial interface IDerivedBool : IBool
    {
        void SetName([MarshalUsing(typeof(Utf16StringMarshaller))] string name);

        [return: MarshalUsing(typeof(Utf16StringMarshaller))]
        string GetName();
    }

    [GeneratedComClass]
    internal partial class Derived : GetAndSetInt, IDerived
    {
        string _data = "hello";
        public string GetName() => _data;
        public void SetName(string name) => _data = name;
    }
}
