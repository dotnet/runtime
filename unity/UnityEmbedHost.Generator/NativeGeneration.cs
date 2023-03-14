// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.CodeAnalysis;

namespace UnityEmbedHost.Generator;

static class NativeGeneration
{
    // These names must match the attribute names defined in the embed host project
    const string NoNativeWrapperAttributeName = "NoNativeWrapperAttribute";
    public const string NativeWrapperTypeAttributeName = "NativeWrapperTypeAttribute";
    public const string NativeCallbackTypeAttributeName = "NativeCallbackTypeAttribute";

    static readonly DiagnosticDescriptor NativeDestinationNotFound = new (
        id: "EMBEDHOSTGEN001",
        title: $"Native file was missing",
        messageFormat: "Could not locate '{0}'",
        category: nameof(NativeGeneration),
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static void Run(GeneratorExecutionContext context, IMethodSymbol[] callbackMethods)
    {
        ReplaceNativeWrapperImplementations(context, callbackMethods);
    }

    static void ReplaceNativeWrapperImplementations(GeneratorExecutionContext context, IMethodSymbol[] callbackMethods)
    {
        var path = Path.Combine(GetCallingPath(context), "../../src/coreclr/vm/mono/mono_coreclr.cpp");

        if (!File.Exists(path))
        {
            context.ReportDiagnostic(Diagnostic.Create(NativeDestinationNotFound, Location.None, path));
            return;
        }

        var methodToWrapper = new Dictionary<string, string>();
        foreach (var method in callbackMethods)
        {
            // Some methods don't need a native wrapper.  Check for that before writing
            if (method.HasAttribute(NoNativeWrapperAttributeName))
                continue;
            methodToWrapper.Add(method.NativeWrapperName(), GenerateNativeWrapperMethod(method));
        }

        bool TryGetWrapperDeclarationLine(string line, Dictionary<string, string> wrappers, out string matchingWrapperName)
        {
            if (!IsMethodDeclarationLine(line))
            {
                matchingWrapperName = string.Empty;
                return false;
            }

            foreach (var wrapperName in wrappers.Keys)
            {
                if (line.Contains(wrapperName))
                {
                    matchingWrapperName = wrapperName;
                    return true;
                }
            }

            matchingWrapperName = string.Empty;
            return false;
        }

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
            if (TryGetWrapperDeclarationLine(line, methodToWrapper, out var match))
            {
                bool foundEndOfMethod = true;
                var temporaryBackup = new StringBuilder();

                while (lines[index] != "}")
                {
                    index++;

                    if (index == lines.Length)
                    {
                        // We hit the end of the file without finding the end of the method.  This bad
                        foundEndOfMethod = false;
                        break;
                    }
                    if (IsMethodDeclarationLine(lines[index]))
                    {
                        // We hit the next method without finding the end of the current one.  Also bad.
                        foundEndOfMethod = false;
                        break;
                    }
                }

                if (foundEndOfMethod)
                {
                    sb.Append(methodToWrapper[match]);
                    // Remove so that we know which have not been written
                    methodToWrapper.Remove(match);
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

        foreach (var unwritten in methodToWrapper.Keys)
        {
            sb.Append(methodToWrapper[unwritten]);
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

    private static string GenerateNativeWrapperMethod(IMethodSymbol method)
    {
        var sb = new StringBuilder();
        AppendNativeWrapperMethod(sb, method);
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

    private static void AppendNativeWrapperMethod(StringBuilder sb, IMethodSymbol method)
    {
        AppendAutoGeneratedComment(sb);
        sb.AppendLine(FormatNativeWrapperMethodSignature(method));
        sb.AppendLine("{");
        sb.AppendLine("    GCX_PREEMP(); // temporary until we sort out our GC thread model");
        sb.AppendLine($"    {FormatManagedCallbackCall(method)}");
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
