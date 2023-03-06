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

    [GeneratedComInterface]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IComInterface2
    {
        int Method1();
    }

    public class Tests
    {
#pragma warning disable xUnit1004 // Test methods should not be skipped
        [Fact(Skip = "Not Implemented Yet")]
#pragma warning restore xUnit1004 // Test methods should not be skipped
        public unsafe void UseGeneratedComInterface()
        {
            throw new NotImplementedException("Not Implemented");
        }

    }
}

