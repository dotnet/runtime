// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
