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

    enum WrapperVariant
    {
        /// <summary>
        /// Friendly wrapper that takes managed types and returns managed types rather than having to deal with IntPtrs.
        /// </summary>
        Friendly,

        /// <summary>
        /// A wrapper that has the same signature as the embedding api
        /// </summary>
        Raw,

        /// <summary>
        /// A wrapper that has the same signature as the friendly wrapper, but returns the result as a raw IntPtr
        /// This is helpful in a few situations
        /// 1) When you need to pass the result to another embedding api
        /// 2) When you need to assert a null return and don't want the conversion back to managed because it will throw or crash
        /// </summary>
        RawReturnOnly
    }

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

        var methodsToGenerateWrappersFor = MethodsToGenerateWrappersFor(callbackMethods).ToArray();
        foreach (var methodSymbol in methodsToGenerateWrappersFor)
        {
            sb.AppendLine($"    {FormatSignature(methodSymbol, WrapperVariant.Raw)};");
            sb.AppendLine();
        }

        // Now write default interface implementations for the friendly and raw return only variants
        foreach (var methodSymbol in methodsToGenerateWrappersFor)
        {
            AppendWrapperDefaultInterfaceImplementation(sb, methodSymbol, WrapperVariant.Friendly);
            AppendWrapperDefaultInterfaceImplementation(sb, methodSymbol, WrapperVariant.RawReturnOnly);
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
            AppendWrapperMethodImplementation(sb, methodSymbol, WrapperVariant.Raw, apiClassName, apiName);
        }

        sb.Append("}");
        context.AddSource(generatedFileName,
            SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    static string WrapperVariantPostFix(WrapperVariant wrapperVariant)
    {
        switch (wrapperVariant)
        {
            case WrapperVariant.Friendly:
                return string.Empty;
            case WrapperVariant.Raw:
                return "_raw";
            case WrapperVariant.RawReturnOnly:
                return "_raw_return_only";
            default:
                throw new ArgumentException($"Unhandled {nameof(WrapperVariant)} value of {wrapperVariant}");
        }
    }

    static void AppendWrapperDefaultInterfaceImplementation(StringBuilder sb, IMethodSymbol methodSymbol, WrapperVariant wrapperVariant)
    {
        sb.AppendLine($"    public {FormatSignature(methodSymbol, wrapperVariant)}");
        sb.AppendLine($"        => {FormatManagedCast(methodSymbol, wrapperVariant)}{methodSymbol.Name}_raw({FormatMethodParametersNamesForNiceManaged(methodSymbol, wrapperVariant, callingAnotherWrapper: true)}){FormatToManagedRepresentation(methodSymbol, wrapperVariant)};");
        sb.AppendLine();
    }

    static void AppendWrapperMethodImplementation(StringBuilder sb, IMethodSymbol methodSymbol, WrapperVariant wrapperVariant, string apiClassName, string apiName)
    {
        sb.AppendLine($"    public {FormatSignature(methodSymbol, wrapperVariant)}");
        sb.AppendLine($"        => {FormatManagedCast(methodSymbol, wrapperVariant)}{apiClassName}.{apiName}({FormatMethodParametersNamesForNiceManaged(methodSymbol, wrapperVariant, callingAnotherWrapper: false)}){FormatToManagedRepresentation(methodSymbol, wrapperVariant)};");
        sb.AppendLine();
    }

    static string FormatSignature(IMethodSymbol methodSymbol, WrapperVariant wrapperVariant)
    {
        string signature = FormatMethodParametersForManagedWrapperMethodSignature(methodSymbol, wrapperVariant);
        var methodName = $"{methodSymbol.Name}{WrapperVariantPostFix(wrapperVariant)}";
        return $"{ManagedWrapperReturnType(methodSymbol, wrapperVariant)} {methodName}({signature})";
    }

    static IEnumerable<IMethodSymbol> MethodsToGenerateWrappersFor(IMethodSymbol[] callbackMethods)
        => callbackMethods.Where(m => m.ManagedWrapperOptions() != ManagedWrapperOptions.Exclude);

    static string FormatMethodParametersForManagedWrapperMethodSignature(IMethodSymbol methodSymbol, WrapperVariant wrapperVariant) =>
        methodSymbol.Parameters
            .Where(p =>  p.ManagedWrapperOptions() != ManagedWrapperOptions.Exclude)
            .Select(p => $"{ManagedWrapperType(p, wrapperVariant)} {p.Name}")
            .AggregateWithCommaSpace();

    static string ManagedWrapperReturnType(IMethodSymbol methodSymbol, WrapperVariant wrapperVariant)
        => ManagedWrapperType(methodSymbol.ReturnType, methodSymbol.GetReturnTypeAttributes(),
            wrapperVariant == WrapperVariant.RawReturnOnly ? WrapperVariant.Raw : wrapperVariant);

    static string ManagedWrapperType(IParameterSymbol parameterSymbol, WrapperVariant wrapperVariant)
        => ManagedWrapperType(parameterSymbol.Type, parameterSymbol.GetAttributes(), wrapperVariant);

    static string ManagedWrapperType(ITypeSymbol typeSymbol, ImmutableArray<AttributeData> providerAttributes, WrapperVariant wrapperVariant)
    {
        if (providerAttributes.ManagedWrapperOptions() == ManagedWrapperOptions.AsIs || wrapperVariant == WrapperVariant.Raw)
            return typeSymbol.ToString();

        if (providerAttributes.ManagedWrapperOptions() == ManagedWrapperOptions.Custom)
            return providerAttributes.ManagedWrapperOptionsValue<string>(1)!;

        switch (typeSymbol.NativeWrapperTypeFor(providerAttributes))
        {
            case "MonoException*":
                return "Exception";
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

    static string FormatMethodParametersNamesForNiceManaged(IMethodSymbol methodSymbol, WrapperVariant wrapperVariant, bool callingAnotherWrapper)
    {
        var parameters = callingAnotherWrapper
            ? methodSymbol.Parameters.Where(p => p.ManagedWrapperOptions() != ManagedWrapperOptions.Exclude)
            : methodSymbol.Parameters;
        return parameters
            .Select(p => FormatToNativeRepresentation(p, wrapperVariant))
            .AggregateWithCommaSpace();
    }

    static string FormatToNativeRepresentation(IParameterSymbol parameterSymbol, WrapperVariant wrapperVariant)
    {
        var managedWrapperOptions = parameterSymbol.ManagedWrapperOptions();
        if (managedWrapperOptions == ManagedWrapperOptions.Exclude)
        {
            if (parameterSymbol.Type.Name == "IntPtr")
                return "nint.Zero";

            return "null";
        }

        if (managedWrapperOptions == ManagedWrapperOptions.AsIs || wrapperVariant == WrapperVariant.Raw)
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

    static string FormatToManagedRepresentation(IMethodSymbol methodSymbol, WrapperVariant wrapperVariant)
    {
        if (methodSymbol.ManagedWrapperOptionsForReturnType() == ManagedWrapperOptions.AsIs || wrapperVariant == WrapperVariant.Raw || wrapperVariant == WrapperVariant.RawReturnOnly)
            return string.Empty;

        switch (methodSymbol.NativeWrapperTypeForReturnType())
        {
            case "MonoObject*":
            case "MonoArray*":
            case "MonoReflectionMethod*":
            case "MonoReflectionField*":
            case "MonoException*":
                return ".ToManagedRepresentation()";
            case "MonoClass*":
                return ".TypeFromHandleIntPtr()";
        }

        return string.Empty;
    }

    private static string FormatManagedCast(IMethodSymbol methodSymbol, WrapperVariant wrapperVariant)
    {
        if (wrapperVariant == WrapperVariant.Raw || wrapperVariant == WrapperVariant.RawReturnOnly)
            return string.Empty;

        switch (methodSymbol.NativeWrapperTypeForReturnType())
        {
            case "MonoException*":
                return "(Exception)";
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
