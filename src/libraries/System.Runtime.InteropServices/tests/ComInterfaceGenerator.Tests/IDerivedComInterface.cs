// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace ComInterfaceGenerator.Tests
{
    [GeneratedComInterface]
    [Guid("7F0DB364-3C04-4487-9193-4BB05DC7B654")]
    public partial interface IDerivedComInterface : IComInterface1
    {
        void SetName([MarshalUsing(typeof(Utf16StringMarshaller))] string name);

        [return:MarshalUsing(typeof(Utf16StringMarshaller))]
        string GetName();
    }
}
