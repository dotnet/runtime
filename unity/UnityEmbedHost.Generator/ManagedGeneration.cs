// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace UnityEmbedHost.Generator;

static class ManagedGeneration
{
    public static void Run(GeneratorExecutionContext context, IMethodSymbol[] callbackMethods)
    {
        WriteHostStruct(context, callbackMethods);
        WriteCoreCLRHost(context, callbackMethods);
    }

    static void WriteHostStruct(GeneratorExecutionContext context, IMethodSymbol[] callbackMethods)
    {
        string sourceBegin = @"
// Auto-generated code

namespace Unity.CoreCLRHelpers;

unsafe partial struct HostStruct
{";

        var sb = new StringBuilder();

        sb.Append(sourceBegin);
        sb.AppendLine();


        foreach (var methodSymbol in callbackMethods)
        {
            sb.AppendLine($"    public delegate* unmanaged<{FormatMethodParameters(methodSymbol)}> {methodSymbol.Name};");
        }

        string sourceEnd = @"}";

        sb.Append(sourceEnd);
        context.AddSource($"GeneratedHostStruct.gen.cs",
            SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    static void WriteCoreCLRHost(GeneratorExecutionContext context, IMethodSymbol[] callbackMethods)
    {
        string sourceBegin = @"
// Auto-generated code

namespace Unity.CoreCLRHelpers;

static unsafe partial class CoreCLRHost
{
    static partial void InitHostStruct(HostStruct* functionStruct)
    {";

        var sb = new StringBuilder();

        sb.Append(sourceBegin);
        sb.AppendLine();


        foreach (var methodSymbol in callbackMethods)
        {
            sb.AppendLine($"       functionStruct->{methodSymbol.Name} = &{methodSymbol.Name}_native;");
        }

        sb.AppendLine("    }");
        sb.AppendLine();

        foreach (var methodSymbol in callbackMethods)
        {
            sb.AppendLine("    [System.Runtime.InteropServices.UnmanagedCallersOnly]");
            string signature = FormatMethodParametersForMethodSignature(methodSymbol);
            sb.AppendLine($"    static {methodSymbol.ReturnType} {methodSymbol.Name}_native({signature}) => {methodSymbol.Name}({FormatMethodParametersNames(methodSymbol)});");
            sb.AppendLine();
        }

        sb.Append("}");
        context.AddSource($"GeneratedCoreCLRHost.gen.cs",
            SourceText.From(sb.ToString(), Encoding.UTF8));
    }
    static string FormatMethodParametersForMethodSignature(IMethodSymbol methodSymbol) =>
        methodSymbol.Parameters.Select(p => $"{p.Type} {p.Name}")
            .AggregateWithCommaSpace();

    static string FormatMethodParametersNames(IMethodSymbol methodSymbol) =>
        methodSymbol.Parameters.Select(p => p.Name)
            .AggregateWithCommaSpace();


    static string FormatMethodParameters(IMethodSymbol methodSymbol)
        => $"{methodSymbol.Parameters.Select(p => p.Type.ToString()).AggregateWithCommaSpace()}, {methodSymbol.ReturnType}";
}
