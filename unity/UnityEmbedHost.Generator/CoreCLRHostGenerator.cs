// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace UnityEmbedHost.Generator;

static class CoreCLRHostGenerator
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
            string signature = methodSymbol.FormatMethodParametersForMethodSignature();
            sb.AppendLine($"    static {methodSymbol.ReturnType} {methodSymbol.Name}_native({signature})");
            sb.AppendLine("    {");
            sb.AppendLine("        try");
            sb.AppendLine("        {");
            sb.AppendLine($"            return {methodSymbol.Name}({FormatMethodParametersNames(methodSymbol)});");
            sb.AppendLine("        }");
            sb.AppendLine("        catch (System.Exception e)");
            sb.AppendLine("        {");
            sb.AppendLine("            Log(e.ToString());");
            sb.AppendLine("            System.Environment.Exit(1);");
            sb.AppendLine("        }");
            sb.AppendLine("        return default;");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        sb.Append("}");
        context.AddSource($"GeneratedCoreCLRHost.gen.cs",
            SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    static string FormatMethodParametersNames(IMethodSymbol methodSymbol) =>
        methodSymbol.Parameters.Select(p => p.Name)
            .AggregateWithCommaSpace();

    static string FormatMethodParameters(IMethodSymbol methodSymbol)
    {
        if (methodSymbol.Parameters.Length == 0)
            return $"{methodSymbol.ReturnType}";
        return $"{methodSymbol.Parameters.Select(p => p.Type.ToString()).AggregateWithCommaSpace()}, {methodSymbol.ReturnType}";
    }
}
