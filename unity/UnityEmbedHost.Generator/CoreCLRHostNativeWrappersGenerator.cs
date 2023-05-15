// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Unity.CoreCLRHelpers;

namespace UnityEmbedHost.Generator;

public class CoreCLRHostNativeWrappersGenerator
{
    public const string ManagedWrapperOptionsAttributeName = "ManagedWrapperOptionsAttribute";

    public static void Run(GeneratorExecutionContext context, IMethodSymbol[] callbackMethods)
    {
        WriteCoreCLRHostNativeWrappers(context, callbackMethods);
        WriteCoreCLRHostWrappers(context, callbackMethods);
        WriteICoreCLRHostAdapter(context, callbackMethods);
    }

    static void WriteCoreCLRHostNativeWrappers(GeneratorExecutionContext context, IMethodSymbol[] callbackMethods)
    {
        WriteCoreCLRHostNativeWrappers(context, callbackMethods,
            "CoreCLRHostNativeWrappers",
            "CoreCLRHostNative",
            "GeneratedCoreCLRHostNativeWrappers.gen.cs",
            useNativeName: true);
    }

    static void WriteCoreCLRHostWrappers(GeneratorExecutionContext context, IMethodSymbol[] callbackMethods)
    {
        WriteCoreCLRHostNativeWrappers(context, callbackMethods,
            "CoreCLRHostWrappers",
            "CoreCLRHost",
            "GeneratedCoreCLRHostWrappers.gen.cs",
            useNativeName: false);
    }

    static void WriteICoreCLRHostAdapter(GeneratorExecutionContext context, IMethodSymbol[] callbackMethods)
    {
        string sourceBegin = @"
// Auto-generated code
using System;

namespace Unity.CoreCLRHelpers;
";

        const string className = "ICoreCLRHostWrapper";

        var sb = new StringBuilder();

        sb.Append(sourceBegin);
        sb.AppendLine($"unsafe partial interface {className}");
        sb.AppendLine("{");

        foreach (var methodSymbol in MethodsToGenerateWrappersFor(callbackMethods))
        {
            string signature = FormatMethodParametersForManagedWrapperMethodSignature(methodSymbol);
            sb.AppendLine($"    {ManagedWrapperType(methodSymbol.ReturnType, methodSymbol.GetReturnTypeAttributes())} {methodSymbol.Name}({signature});");
            sb.AppendLine();
        }

        sb.Append("}");
        context.AddSource($"Generated{className}.gen.cs",
            SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    static void WriteCoreCLRHostNativeWrappers(GeneratorExecutionContext context, IMethodSymbol[] callbackMethods, string thisClassName, string apiClassName, string generatedFileName,
        bool useNativeName)
    {
        string sourceBegin = @"
// Auto-generated code
using System;

namespace Unity.CoreCLRHelpers;
";

        var sb = new StringBuilder();

        sb.Append(sourceBegin);
        sb.AppendLine($"unsafe partial class {thisClassName}");
        sb.AppendLine("{");

        foreach (var methodSymbol in MethodsToGenerateWrappersFor(callbackMethods))
        {
            var apiName = useNativeName ? methodSymbol.NativeWrapperName() : methodSymbol.Name;
            string signature = FormatMethodParametersForManagedWrapperMethodSignature(methodSymbol);
            sb.AppendLine($"    public {ManagedWrapperType(methodSymbol.ReturnType, methodSymbol.GetReturnTypeAttributes())} {methodSymbol.Name}({signature})");
            sb.AppendLine($"        => {FormatManagedCast(methodSymbol)}{apiClassName}.{apiName}({FormatMethodParametersNamesForNiceManaged(methodSymbol)}){FormatToManagedRepresentation(methodSymbol)};");
            sb.AppendLine();
        }

        sb.Append("}");
        context.AddSource(generatedFileName,
            SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    static IEnumerable<IMethodSymbol> MethodsToGenerateWrappersFor(IMethodSymbol[] callbackMethods)
        => callbackMethods.Where(m => m.ManagedWrapperOptions() != ManagedWrapperOptions.Exclude);

    static string FormatMethodParametersForManagedWrapperMethodSignature(IMethodSymbol methodSymbol) =>
        methodSymbol.Parameters
            .Where(p =>  p.ManagedWrapperOptions() != ManagedWrapperOptions.Exclude)
            .Select(p => $"{ManagedWrapperType(p)} {p.Name}")
            .AggregateWithCommaSpace();

    static string ManagedWrapperType(IParameterSymbol parameterSymbol)
        => ManagedWrapperType(parameterSymbol.Type, parameterSymbol.GetAttributes());

    static string ManagedWrapperType(ITypeSymbol typeSymbol, ImmutableArray<AttributeData> providerAttributes)
    {
        if (providerAttributes.ManagedWrapperOptions() == ManagedWrapperOptions.AsIs)
            return typeSymbol.ToString();

        if (providerAttributes.ManagedWrapperOptions() == ManagedWrapperOptions.Custom)
            return providerAttributes.ManagedWrapperOptionsValue<string>(1)!;

        switch (typeSymbol.NativeWrapperTypeFor(providerAttributes))
        {
            case "MonoClass*":
            case "MonoType*":
                return "Type";
            case "MonoDomain*":
            case "MonoObject*":
                return "object";
            case "MonoArray*":
                return "Array";
            case "MonoMethod*":
                return "RuntimeMethodHandle";
            case "MonoClassField*":
                return "RuntimeFieldHandle";
            case "MonoReflectionMethod*":
                return "System.Reflection.MethodInfo";
            case "MonoReflectionField*":
                return "System.Reflection.FieldInfo";
        }

        return typeSymbol.ToString();
    }

    static string FormatMethodParametersNamesForNiceManaged(IMethodSymbol methodSymbol) =>
        methodSymbol.Parameters.Select(FormatToNativeRepresentation)
            .AggregateWithCommaSpace();

    static string FormatToNativeRepresentation(IParameterSymbol parameterSymbol)
    {
        var managedWrapperOptions = parameterSymbol.ManagedWrapperOptions();
        if (managedWrapperOptions == ManagedWrapperOptions.Exclude)
        {
            if (parameterSymbol.Type.Name == "IntPtr")
                return "nint.Zero";

            return "null";
        }

        if (managedWrapperOptions == ManagedWrapperOptions.AsIs)
            return parameterSymbol.Name;

        switch (parameterSymbol.NativeWrapperTypeFor())
        {
            case "MonoObject*":
            case "MonoArray*":
                return $"{parameterSymbol.Name}.ToNativeRepresentation()";
            case "MonoClass*":
            case "MonoType*":
                return $"{parameterSymbol.Name}.TypeHandleIntPtr()";
            case "MonoMethod*":
                return $"{parameterSymbol.Name}.MethodHandleIntPtr()";
            case "MonoClassField*":
                return $"{parameterSymbol.Name}.FieldHandleIntPtr()";
        }

        return parameterSymbol.Name;
    }

    static string FormatToManagedRepresentation(IMethodSymbol methodSymbol)
    {
        if (methodSymbol.ManagedWrapperOptionsForReturnType() == ManagedWrapperOptions.AsIs)
            return string.Empty;

        switch (methodSymbol.NativeWrapperTypeForReturnType())
        {
            case "MonoObject*":
            case "MonoArray*":
            case "MonoReflectionMethod*":
            case "MonoReflectionField*":
                return ".ToManagedRepresentation()";
            case "MonoClass*":
                return ".TypeFromHandleIntPtr()";
        }

        return string.Empty;
    }

    private static string FormatManagedCast(IMethodSymbol methodSymbol)
    {
        switch (methodSymbol.NativeWrapperTypeForReturnType())
        {
            case "MonoArray*":
                return "(Array)";
            case "MonoReflectionMethod*":
                return "(System.Reflection.MethodInfo)";
            case "MonoReflectionField*":
                return "(System.Reflection.FieldInfo)";
        }

        return string.Empty;
    }

}
