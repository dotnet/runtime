// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic
{
    [Kept]
    class InstanceFields
    {
        [Kept]
        static void Main()
        {
            _ = new TypeWithSequentialLayout();
        }
    }

    [Kept]
    [KeptMember(".ctor()")]
    [StructLayout(LayoutKind.Sequential)]
    class TypeWithSequentialLayout
    {
        [Kept]
        int Field = 42;

        static int StaticField; // Must not assign value, otherwise .cctor would keep the field
    }
}
