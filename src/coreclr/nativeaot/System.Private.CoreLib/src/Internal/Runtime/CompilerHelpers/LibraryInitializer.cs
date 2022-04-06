// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using System.Runtime.CompilerServices;

using Debug = System.Diagnostics.Debug;

namespace Internal.Runtime.CompilerHelpers
{
    /// <summary>
    /// Container class to run specific class constructors in a defined order. Since we can't
    /// directly invoke class constructors in C#, they're renamed Initialize.
    /// </summary>
    internal static class LibraryInitializer
    {
        public static void InitializeLibrary()
        {
            PreallocatedOutOfMemoryException.Initialize();
            ClassConstructorRunner.Initialize();
            TypeLoaderExports.Initialize();
        }
    }
}
