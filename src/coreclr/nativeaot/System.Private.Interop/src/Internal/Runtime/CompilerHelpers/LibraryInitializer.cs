// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Internal.Runtime.Augments;

namespace Internal.Runtime.CompilerHelpers
{
    /// <summary>
    /// Container class to run specific class constructors in a defined order. Since we can't
    /// directly invoke class constructors in C#, they're renamed Initialize.
    /// </summary>
    public static class LibraryInitializer
    {
        public static void InitializeLibrary()
        {
            RuntimeAugments.InitializeInteropLookups(new RuntimeInteropData());
        }
    }
}
