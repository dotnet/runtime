// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace ComInterfaceGenerator.Tests
{
    [GeneratedComInterface]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("2c3f9903-b586-46b1-881b-adfce9af47b1")]
    public partial interface IComInterface1
    {
        int GetData();
        void SetData(int n);
    }
}
