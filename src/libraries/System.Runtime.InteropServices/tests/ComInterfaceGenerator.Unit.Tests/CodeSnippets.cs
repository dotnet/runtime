// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComInterfaceGenerator.Unit.Tests
{
    internal static class CodeSnippets
    {
        public static string SpecifiedMethodIndexNoExplicitParameters = @"
using System.Runtime.InteropServices;

readonly record struct NoCasting {}
partial interface INativeAPI
{
    public static readonly NoCasting TypeKey = default;
    [VirtualMethodIndex(0)]
    void Method();
}

// Try using the generated native interface
sealed class NativeAPI : IUnmanagedVirtualMethodTableProvider<NoCasting>, INativeAPI.Native
{
    public VirtualMethodTableInfo GetFunctionPointerForIndex(NoCasting typeKey) => throw null;
}
";

    }
}
