// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Reflection;

namespace ManagedTest
{
    public interface IManagedTest
    {
        void Foo();
    }

    public class ManagedTest : IManagedTest
    {
        public void Foo()
        {
            Console.WriteLine("ManagedTest successfully run!");
        }
    }
}
