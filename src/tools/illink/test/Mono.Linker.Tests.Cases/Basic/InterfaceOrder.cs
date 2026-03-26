// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic
{
    [Kept]
    class InterfaceOrder
    {
        [Kept]
        static void Main()
        {
            I1 c1 = new C1();
            c1.Method();
            I2 c2 = new C2();
            c2.Method();
        }
        [Kept]
        interface I1
        {
            [Kept]
            void Method();
        }

        [Kept]
        interface I2
        {
            [Kept]
            void Method();
        }

        [Kept]
        [KeptMember(".ctor()")]
        [KeptInterface(typeof(I1))]
        [KeptInterface(typeof(I2))]
        class C1 : I1, I2
        {
            [Kept]
            public void Method() { }
        }

        [Kept]
        [KeptMember(".ctor()")]
        [KeptInterface(typeof(I1))]
        [KeptInterface(typeof(I2))]
        class C2 : I2, I1
        {
            [Kept]
            public void Method() { }
        }

    }
}
