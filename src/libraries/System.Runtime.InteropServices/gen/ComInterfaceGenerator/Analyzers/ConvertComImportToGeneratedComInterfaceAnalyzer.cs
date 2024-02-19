// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;
using static Microsoft.Interop.Analyzers.AnalyzerDiagnostics;

namespace Microsoft.Interop.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ConvertComImportToGeneratedComInterfaceAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(ConvertToGeneratedComInterface);

        private static readonly HashSet<string> s_unsupportedTypeNames = new()
        {
            "global::System.Runtime.InteropServices.CriticalHandle",
            "global::System.Runtime.InteropServices.HandleRef",
            "global::System.Text.StringBuilder"
        };

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(context =>
            {
                INamedTypeSymbol? interfaceTypeAttribute = context.Compilation.GetBestTypeByMetadataName(TypeNames.InterfaceTypeAttribute)!;
                INamedTypeSymbol? generatedComInterfaceAttribute = context.Compilation.GetBestTypeByMetadataName(TypeNames.GeneratedComInterfaceAttribute);

                if (generatedComInterfaceAttribute is null)
                {
                    return;
                }

                TargetFrameworkSettings targetFramework = context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.GetTargetFrameworkSettings();
                var env = new StubEnvironment(
                    context.Compilation,
                    targetFramework.TargetFramework,
                    targetFramework.Version,
                    context.Compilation.SourceModule.GetAttributes().Any(attr => attr.AttributeClass.ToDisplayString() == TypeNames.System_Runtime_CompilerServices_SkipLocalsInitAttribute));

                context.RegisterSymbolAction(context =>
                {
                    INamedTypeSymbol type = (INamedTypeSymbol)context.Symbol;
                    AttributeData? interfaceTypeAttributeData = type.GetAttributes().FirstOrDefault(a => a.AttributeClass.Equals(interfaceTypeAttribute, SymbolEqualityComparer.Default));
                    if (type is not { TypeKind: TypeKind.Interface, IsComImport: true }
                        || interfaceTypeAttributeData is not { ConstructorArguments: [{ Value: (int)ComInterfaceType.InterfaceIsIUnknown }] })
                    {
                        return;
                    }

                    bool mayRequireAdditionalWork = false;
                    bool hasStrings = false;
                    if (type.DeclaringSyntaxReferences.Length > 1)
                    {
                        mayRequireAdditionalWork = true;
                    }

                    foreach (var method in type.GetMembers().OfType<IMethodSymbol>().Where(m => !m.IsStatic && m.IsAbstract))
                    {
                        // Ignore types with methods with unsupported returns
                        if (method.ReturnsByRef || method.ReturnsByRefReadonly)
                            return;
                        // Run a basic conversion check like we do for ConvertToLibraryImportAttributeAnalyzer to determine if there will be warnings after the fix.

                        // Use  the method signature to do some of the work the generator will do after conversion.
                        // If any diagnostics or failures to marshal are reported, then mark this diagnostic with a property signifying that it may require
                        // later user work.
                        GeneratorDiagnosticsBag diagnostics = new(new DiagnosticDescriptorProvider(), new MethodSignatureDiagnosticLocations((MethodDeclarationSyntax)method.DeclaringSyntaxReferences[0].GetSyntax()), SR.ResourceManager, typeof(FxResources.Microsoft.Interop.ComInterfaceGenerator.SR));
                        AttributeData comImportAttribute = type.GetAttributes().First(attr => attr.AttributeClass.ToDisplayString() == TypeNames.System_Runtime_InteropServices_ComImportAttribute);
                        SignatureContext targetSignatureContext = SignatureContext.Create(
                            method,
                            CreateComImportMarshallingInfoParser(env, diagnostics, method, comImportAttribute),
                            env,
                            typeof(ConvertComImportToGeneratedComInterfaceAnalyzer).Assembly);

                        var managedToUnmanagedFactory = ComInterfaceGeneratorHelpers.CreateGeneratorFactory(env, MarshalDirection.ManagedToUnmanaged);
                        var unmanagedToManagedFactory = ComInterfaceGeneratorHelpers.CreateGeneratorFactory(env, MarshalDirection.UnmanagedToManaged);

                        mayRequireAdditionalWork = diagnostics.Diagnostics.Any();
                        bool anyExplicitlyUnsupportedInfo = false;

                        var managedToNativeStubCodeContext = new ManagedToNativeStubCodeContext(env.TargetFramework, env.TargetFrameworkVersion, "return", "nativeReturn");
                        var nativeToManagedStubCodeContext = new NativeToManagedStubCodeContext(env.TargetFramework, env.TargetFrameworkVersion, "return", "nativeReturn");

                        var forwarder = new Forwarder();
                        // We don't actually need the bound generators. We just need them to be attempted to be bound to determine if the generator will be able to bind them.
                        BoundGenerators generators = BoundGenerators.Create(targetSignatureContext.ElementTypeInformation, new CallbackGeneratorFactory((info, context) =>
                        {
                            if (s_unsupportedTypeNames.Contains(info.ManagedType.FullTypeName))
                            {
                                anyExplicitlyUnsupportedInfo = true;
                                return ResolvedGenerator.Resolved(forwarder);
                            }
                            if (HasUnsupportedMarshalAsInfo(info))
                            {
                                anyExplicitlyUnsupportedInfo = true;
                                return ResolvedGenerator.Resolved(forwarder);
                            }
                            if (info.MarshallingAttributeInfo is TrackedMarshallingInfo(TrackedMarshallingInfoAnnotation.ExplicitlyUnsupported, _))
                            {
                                anyExplicitlyUnsupportedInfo = true;
                                return ResolvedGenerator.Resolved(forwarder);
                            }
                            if (info.MarshallingAttributeInfo is TrackedMarshallingInfo(TrackedMarshallingInfoAnnotation annotation, var inner))
                            {
                                if (annotation == TrackedMarshallingInfoAnnotation.String)
                                {
                                    hasStrings = true;
                                }
                                info = info with { MarshallingAttributeInfo = inner };
                            }
                            // Run both factories and collect any binding failures.
                            ResolvedGenerator unmanagedToManagedGenerator = unmanagedToManagedFactory.GeneratorFactory.Create(info, nativeToManagedStubCodeContext);
                            ResolvedGenerator managedToUnmanagedGenerator = managedToUnmanagedFactory.GeneratorFactory.Create(info, managedToNativeStubCodeContext);
                            return managedToUnmanagedGenerator with
                            {
                                Diagnostics = managedToUnmanagedGenerator.Diagnostics.AddRange(unmanagedToManagedGenerator.Diagnostics)
                            };
                        }), managedToNativeStubCodeContext, forwarder, out var generatorDiagnostics);

                        mayRequireAdditionalWork |= generatorDiagnostics.Any(diag => diag.IsFatal);

                        if (anyExplicitlyUnsupportedInfo)
                        {
                            // If we have any parameters/return value with an explicitly unsupported marshal type or marshalling info,
                            // don't offer the fix. The amount of work for the user to get to pairity would be too expensive.
                            return;
                        }
                    }

                    ImmutableDictionary<string, string>.Builder properties = ImmutableDictionary.CreateBuilder<string, string>();

                    properties.Add(AnalyzerDiagnostics.Metadata.MayRequireAdditionalWork, mayRequireAdditionalWork.ToString());
                    properties.Add(AnalyzerDiagnostics.Metadata.AddStringMarshalling, hasStrings.ToString());

                    context.ReportDiagnostic(type.CreateDiagnostic(ConvertToGeneratedComInterface, properties.ToImmutable(), type.Name));
                }, SymbolKind.NamedType);
            });
        }

        private static MarshallingInfoParser CreateComImportMarshallingInfoParser(StubEnvironment env, GeneratorDiagnosticsBag diagnostics, IMethodSymbol method, AttributeData unparsedAttributeData)
        {
            var defaultInfo = new DefaultMarshallingInfo(CharEncoding.Utf16, null);

            var useSiteAttributeParsers = ImmutableArray.Create<IUseSiteAttributeParser>(
                    new MarshalAsAttributeParser(env.Compilation, diagnostics, defaultInfo),
                    new MarshalUsingAttributeParser(env.Compilation, diagnostics));

            return new MarshallingInfoParser(
                diagnostics,
                new MethodSignatureElementInfoProvider(env.Compilation, diagnostics, method, useSiteAttributeParsers),
                useSiteAttributeParsers,
                ImmutableArray.Create<IMarshallingInfoAttributeParser>(
                    new MarshalAsAttributeParser(env.Compilation, diagnostics, defaultInfo),
                    new MarshalUsingAttributeParser(env.Compilation, diagnostics),
                    new NativeMarshallingAttributeParser(env.Compilation, diagnostics),
                    new ComInterfaceMarshallingInfoProvider(env.Compilation)),
                ImmutableArray.Create<ITypeBasedMarshallingInfoProvider>(
                    new SafeHandleMarshallingInfoProvider(env.Compilation, method.ContainingType),
                    new ExplicitlyUnsupportedMarshallingInfoProvider(), // We don't support arrays, so we don't include the array marshalling info provider. Instead, we include our "explicitly unsupported" provider.
                    new CharMarshallingInfoProvider(defaultInfo),
                    new TrackingStringMarshallingInfoProvider(new StringMarshallingInfoProvider(env.Compilation, diagnostics, unparsedAttributeData, defaultInfo)), // We need to mark when we see string types to ensure we offer a code-fix that adds the string marshalling info.
                    new BooleanMarshallingInfoProvider(),
                    new BlittableTypeMarshallingInfoProvider(env.Compilation)));
        }

        private static bool HasUnsupportedMarshalAsInfo(TypePositionInfo info)
        {
            if (info.MarshallingAttributeInfo is not MarshalAsInfo(UnmanagedType unmanagedType, _))
                return false;

            return !Enum.IsDefined(typeof(UnmanagedType), unmanagedType)
                || unmanagedType == UnmanagedType.CustomMarshaler
                || unmanagedType == UnmanagedType.Interface
                || unmanagedType == UnmanagedType.IDispatch
                || unmanagedType == UnmanagedType.IInspectable
                || unmanagedType == UnmanagedType.IUnknown
                || unmanagedType == UnmanagedType.SafeArray;
        }

        private sealed class CallbackGeneratorFactory : IMarshallingGeneratorFactory
        {
            private readonly Func<TypePositionInfo, StubCodeContext, ResolvedGenerator> _func;

            public CallbackGeneratorFactory(Func<TypePositionInfo, StubCodeContext, ResolvedGenerator> func)
            {
                _func = func;
            }

            public ResolvedGenerator Create(TypePositionInfo info, StubCodeContext context) => _func(info, context);
        }

        private enum TrackedMarshallingInfoAnnotation
        {
            ExplicitlyUnsupported,
            String
        }

        private sealed record TrackedMarshallingInfo(TrackedMarshallingInfoAnnotation TrackingAnnotation, MarshallingInfo InnerInfo) : MarshallingInfo;

        private sealed class TrackingStringMarshallingInfoProvider : ITypeBasedMarshallingInfoProvider
        {
            private readonly ITypeBasedMarshallingInfoProvider _stringMarshallingInfoProvider;

            public TrackingStringMarshallingInfoProvider(ITypeBasedMarshallingInfoProvider stringMarshallingInfoProvider)
            {
                _stringMarshallingInfoProvider = stringMarshallingInfoProvider;
            }

            public bool CanProvideMarshallingInfoForType(ITypeSymbol type) => type.SpecialType == SpecialType.System_String;
            public MarshallingInfo GetMarshallingInfo(ITypeSymbol type, int indirectionDepth, UseSiteAttributeProvider useSiteAttributes, GetMarshallingInfoCallback marshallingInfoCallback)
                => new TrackedMarshallingInfo(TrackedMarshallingInfoAnnotation.String, _stringMarshallingInfoProvider.GetMarshallingInfo(type, indirectionDepth, useSiteAttributes, marshallingInfoCallback));
        }

        private sealed class ExplicitlyUnsupportedMarshallingInfoProvider : ITypeBasedMarshallingInfoProvider
        {
            public bool CanProvideMarshallingInfoForType(ITypeSymbol type) => type is { TypeKind: TypeKind.Array or TypeKind.Delegate } or { SpecialType: SpecialType.System_Array or SpecialType.System_Object };
            public MarshallingInfo GetMarshallingInfo(ITypeSymbol type, int indirectionDepth, UseSiteAttributeProvider useSiteAttributes, GetMarshallingInfoCallback marshallingInfoCallback) => new TrackedMarshallingInfo(TrackedMarshallingInfoAnnotation.ExplicitlyUnsupported, NoMarshallingInfo.Instance);
        }
    }
}
