// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Xunit;

namespace ComInterfaceGenerator.Tests
{

    [GeneratedComInterface]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IComInterface1
    {
        void Method();
    }

    public class ImplComInterface1 : IComInterface1
    {
        public int Data = 0;
        public void Method() { Data = 1; }
    }

    public class Tests
    {
        [Fact]
        public unsafe void UseGeneratedComInterface()
        {
            ImplComInterface1 x = new();


        }

    }
}

