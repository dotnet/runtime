// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.CodeAnalysis;
using Unity.CoreCLRHelpers;

namespace UnityEmbedHost.Generator;

static class NativeGeneration
{
    // These names must match the attribute names defined in the embed host project
    public const string NativeFunctionAttributeName = "NativeFunctionAttribute";
    public const string NativeWrapperTypeAttributeName = "NativeWrapperTypeAttribute";
    public const string NativeCallbackTypeAttributeName = "NativeCallbackTypeAttribute";

    private const string CppFileName = "mono_coreclr.cpp";

    static readonly DiagnosticDescriptor NativeDestinationNotFound = new (
        id: "EMBEDHOSTGEN001",
        title: $"Native file was missing",
        messageFormat: "Could not locate '{0}'",
        category: nameof(NativeGeneration),
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    static readonly DiagnosticDescriptor CannotUseSignatureOnlyOnUndefinedFunction = new (
        id: "EMBEDHOSTGEN001",
        title: $"Cannot use {nameof(NativeFunctionOptions)}.{nameof(NativeFunctionOptions.SignatureOnly)} on a function that is not defined in {CppFileName}",
        messageFormat: "Could not locate function '{0}'",
        category: nameof(NativeGeneration),
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static void Run(GeneratorExecutionContext context, IMethodSymbol[] callbackMethods)
    {
        ReplaceNativeWrapperImplementations(context, callbackMethods);
    }

    static void ReplaceNativeWrapperImplementations(GeneratorExecutionContext context, IMethodSymbol[] callbackMethods)
    {
        var path = Path.Combine(GetCallingPath(context), $"../../src/coreclr/vm/mono/{CppFileName}");

        if (!File.Exists(path))
        {
            context.ReportDiagnostic(Diagnostic.Create(NativeDestinationNotFound, Location.None, path));
            return;
        }

        var nativeMethodsToWrite = callbackMethods.Where(m => m.NativeFunctionOptions() != NativeFunctionOptions.DoNotGenerate)
            .Select(m => (m.NativeWrapperName(), m))
            .ToList();

        bool TryGetWrapperDeclarationLine(string line, List<(string, IMethodSymbol)> wrappers, out (string, IMethodSymbol) match)
        {
            if (!IsMethodDeclarationLine(line))
            {
                match = default;
                return false;
            }

            foreach (var wrapperData in wrappers)
            {
                if (line.Contains(wrapperData.Item1))
                {
                    match = wrapperData;
                    return true;
                }
            }

            match = default;
            return false;
        }

        bool IsGCPreEmp(string line)
            => line.Contains("GCX_PREEMP();");

        bool IsMethodDeclarationLine(string line)
            => line.StartsWith("extern \"C\" EXPORT_API");

        bool IsHostStructDeclarationLine(string line)
            => line == "struct HostStruct";

        var lines = File.ReadAllLines(path);
        var sb = new StringBuilder();

        var generatedMessageTag = GeneratedMessagePrefix;

        for (int index = 0; index < lines.Length; index++)
        {
            string? line = lines[index];
            if (TryGetWrapperDeclarationLine(line, nativeMethodsToWrite, out var match))
            {
                bool foundEndOfMethod = true;
                var temporaryBackup = new StringBuilder();
                var currentBody = new StringBuilder();
                var hasGcxPreEmp = false;
                var inBody = false;

                while (lines[index] != "}")
                {
                    index++;

                    if (index == lines.Length)
                    {
                        // We hit the end of the file without finding the end of the method.  This bad
                        foundEndOfMethod = false;
                        break;
                    }

                    if (IsGCPreEmp(lines[index]))
                        hasGcxPreEmp = true;

                    if (inBody && lines[index] != "}")
                        currentBody.AppendLine(lines[index]);
                    else if (lines[index] == "{")
                        inBody = true;

                    if (IsMethodDeclarationLine(lines[index]))
                    {
                        // We hit the next method without finding the end of the current one.  Also bad.
                        foundEndOfMethod = false;
                        break;
                    }
                }

                if (foundEndOfMethod)
                {
                    if (match.Item2.NativeFunctionOptions() == NativeFunctionOptions.SignatureOnly)
                        AppendNativeWrapperMethodWithCurrentBody(sb, match.Item2, currentBody);
                    else
                        AppendNativeWrapperMethod(sb, match.Item2, hasGcxPreEmp);

                    // Remove so that we know which have not been written
                    nativeMethodsToWrite.Remove(match);
                }
                else
                {
                    // If we didn't locate the end of the method, then play it safe and don't replace the method
                    sb.Append(temporaryBackup);
                }
            }
            else if (IsHostStructDeclarationLine(line))
            {
                sb.Append(GenerateNativeHostStruct(callbackMethods));

                while (lines[index] != "};" && index < lines.Length)
                    index++;
            }
            else if (line.Contains(generatedMessageTag))
            {
                // Skip these.  Otherwise we accumulate the messages
                continue;
            }
            else
            {
                sb.AppendLine(line);
            }
        }

        foreach (var unwritten in nativeMethodsToWrite)
        {
            if (unwritten.m.NativeFunctionOptions() == NativeFunctionOptions.SignatureOnly)
            {
                context.ReportDiagnostic(Diagnostic.Create(CannotUseSignatureOnlyOnUndefinedFunction, Location.None, unwritten.Item1));
                continue;
            }

            if (unwritten.m.NativeFunctionOptions() == NativeFunctionOptions.DoNotGenerate)
                continue;

            AppendNativeWrapperMethod(sb, unwritten.m,
                // Better safe than sorry?  Control may need to be exposed
                includeGcxPreEmp: true);
            sb.AppendLine();
        }

        if (File.Exists(path))
            File.Delete(path);

        File.WriteAllText(path, sb.ToString());
    }

    private static string GenerateNativeHostStruct(IMethodSymbol[] callbackMethods)
    {
        var sb = new StringBuilder();
        AppendAutoGeneratedComment(sb);
        sb.AppendLine("struct HostStruct");
        sb.AppendLine("{");
        foreach (var method in callbackMethods)
        {
            sb.AppendLine($"    {FormatNativeStructMember(method)};");
        }

        sb.AppendLine("};");
        return sb.ToString();
    }

    private static string GeneratedMessagePrefix => $"Generated by {typeof(CallbacksGenerator).Assembly.GetName().Name}";

    private static void AppendAutoGeneratedComment(StringBuilder sb, string? message = null)
    {
        sb.Append($"// {GeneratedMessagePrefix} - Commit these changes");
        if (!string.IsNullOrEmpty(message))
            sb.Append($" - {message}");

        sb.AppendLine();
    }

    private static void AppendNativeWrapperMethod(StringBuilder sb, IMethodSymbol method, bool includeGcxPreEmp)
    {
        AppendAutoGeneratedComment(sb);
        sb.AppendLine(FormatNativeWrapperMethodSignature(method));
        sb.AppendLine("{");
        if (includeGcxPreEmp)
            sb.AppendLine("    GCX_PREEMP(); // temporary until we sort out our GC thread model");
        sb.AppendLine($"    {FormatManagedCallbackCall(method)}");
        sb.AppendLine("}");
    }

    private static void AppendNativeWrapperMethodWithCurrentBody(StringBuilder sb, IMethodSymbol method, StringBuilder currentBody)
    {
        AppendAutoGeneratedComment(sb);
        sb.AppendLine(FormatNativeWrapperMethodSignature(method));
        sb.AppendLine("{");
        sb.Append(currentBody);
        sb.AppendLine("}");
    }

    static string FormatManagedCallbackCall(IMethodSymbol methodSymbol)
    {
        string cast = string.Empty;
        var callbackReturnType = methodSymbol.NativeCallbackTypeForReturnType();
        var wrapperReturnType = methodSymbol.NativeWrapperTypeForReturnType();
        if (callbackReturnType != wrapperReturnType)
            cast = $"({wrapperReturnType})";

        return $"return {cast}g_HostStruct->{methodSymbol.Name}({methodSymbol.Parameters.Select(p => p.Name).AggregateWithCommaSpace()});";
    }

    static string FormatNativeWrapperMethodSignature(IMethodSymbol methodSymbol)
    {
        var formattedParams = methodSymbol.Parameters
            .Select(p => $"{p.NativeWrapperTypeFor()} {p.Name}")
            .AggregateWithCommaSpace();
        return $"extern \"C\" EXPORT_API {methodSymbol.NativeWrapperTypeForReturnType()} EXPORT_CC {methodSymbol.NativeWrapperName()}({formattedParams})";
    }

    static string FormatNativeStructMember(IMethodSymbol method)
    {
        var formattedParameters =  method.Parameters.Select(p => $"{p.NativeCallbackType()} {p.Name}")
            .AggregateWithCommaSpace();
        return $"{method.NativeCallbackTypeForReturnType()} (*{method.NativeCallbackName()})({formattedParameters})";
    }

    static string GetCallingPath(GeneratorExecutionContext context)
    {
        if (!context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.projectdir", out var result))
            throw new ArgumentException("Failed to locate the path to the source generator");
        return result;
    }
}
