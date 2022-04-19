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
        public static readonly string SpecifiedMethodIndexNoExplicitParameters = @"
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

        public static readonly string SpecifiedMethodIndexNoExplicitParametersNoImplicitThis = @"
using System.Runtime.InteropServices;

readonly record struct NoCasting {}
partial interface INativeAPI
{
    public static readonly NoCasting TypeKey = default;
    [VirtualMethodIndex(0, ImplicitThisParameter = false)]
    void Method();
}

// Try using the generated native interface
sealed class NativeAPI : IUnmanagedVirtualMethodTableProvider<NoCasting>, INativeAPI.Native
{
    public VirtualMethodTableInfo GetFunctionPointerForIndex(NoCasting typeKey) => throw null;
}
";

        public static readonly string SpecifiedMethodIndexNoExplicitParametersCallConvWithCallingConventions = @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

readonly record struct NoCasting {}
partial interface INativeAPI
{
    public static readonly NoCasting TypeKey = default;

    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    [VirtualMethodIndex(0)]
    void Method();
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl), typeof(CallConvMemberFunction) })]
    [VirtualMethodIndex(1)]
    void Method1();

    [SuppressGCTransition]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl), typeof(CallConvMemberFunction) })]
    [VirtualMethodIndex(2)]
    void Method2();

    [SuppressGCTransition]
    [UnmanagedCallConv]
    [VirtualMethodIndex(3)]
    void Method3();

    [SuppressGCTransition]
    [VirtualMethodIndex(4)]
    void Method4();
}

// Try using the generated native interface
sealed class NativeAPI : IUnmanagedVirtualMethodTableProvider<NoCasting>, INativeAPI.Native
{
    public VirtualMethodTableInfo GetFunctionPointerForIndex(NoCasting typeKey) => throw null;
}
";

    }
}
