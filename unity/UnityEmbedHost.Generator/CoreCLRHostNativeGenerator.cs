// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace UnityEmbedHost.Generator;

public class CoreCLRHostNativeGenerator
{

    public static void Run(GeneratorExecutionContext context, IMethodSymbol[] callbackMethods)
    {
        WriteCoreCLRHostNative(context, callbackMethods);
    }

    static void WriteCoreCLRHostNative(GeneratorExecutionContext context, IMethodSymbol[] callbackMethods)
    {
        string sourceBegin = @"
// Auto-generated code
using System;
using System.Runtime.InteropServices;

namespace Unity.CoreCLRHelpers;

static unsafe partial class CoreCLRHostNative
{";

        var sb = new StringBuilder();

        sb.Append(sourceBegin);
        sb.AppendLine();

        foreach (var methodSymbol in callbackMethods)
        {
            string signature = methodSymbol.FormatMethodParametersForMethodSignature();
            sb.AppendLine($"    [DllImport(\"coreclr\", EntryPoint = nameof({methodSymbol.NativeWrapperName()}), CallingConvention = CallingConvention.Cdecl)]");
            sb.AppendLine($"    public unsafe static extern {methodSymbol.ReturnType} {methodSymbol.NativeWrapperName()}({signature});");
            sb.AppendLine();
        }

        sb.Append("}");
        context.AddSource($"GeneratedCoreCLRHostNative.gen.cs",
            SourceText.From(sb.ToString(), Encoding.UTF8));
    }
}
