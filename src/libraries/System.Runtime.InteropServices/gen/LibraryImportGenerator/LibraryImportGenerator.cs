// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

[assembly: System.Resources.NeutralResourcesLanguage("en-US")]

namespace Microsoft.Interop
{
    [Generator]
    public sealed class LibraryImportGenerator : IIncrementalGenerator
    {
        internal sealed record IncrementalStubGenerationContext(
            SignatureContext SignatureContext,
            ContainingSyntaxContext ContainingSyntaxContext,
            ContainingSyntax StubMethodSyntaxTemplate,
            string MethodName,
            MethodSignatureDiagnosticLocations DiagnosticLocation,
            SequenceEqualImmutableArray<string> ForwardedAttributes,
            LibraryImportData LibraryImportData,
            LibraryImportGeneratorOptions Options,
            EnvironmentFlags EnvironmentFlags);

        public static class StepNames
        {
            public const string CalculateStubInformation = nameof(CalculateStubInformation);
            public const string GenerateSingleStub = nameof(GenerateSingleStub);
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Collect all methods adorned with LibraryImportAttribute and filter out invalid ones
            // (diagnostics for invalid methods are reported by the analyzer)
            var methodsToGenerate = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    TypeNames.LibraryImportAttribute,
                    static (node, ct) => node is MethodDeclarationSyntax,
                    static (context, ct) => context.TargetSymbol is IMethodSymbol methodSymbol
                        ? new { Syntax = (MethodDeclarationSyntax)context.TargetNode, Symbol = methodSymbol }
                        : null)
                .Where(
                    static modelData => modelData is not null
                        && Analyzers.LibraryImportDiagnosticsAnalyzer.GetDiagnosticIfInvalidMethodForGeneration(modelData.Syntax, modelData.Symbol) is null);

            // Compute generator options
            IncrementalValueProvider<LibraryImportGeneratorOptions> stubOptions = context.AnalyzerConfigOptionsProvider
                .Select(static (options, ct) => new LibraryImportGeneratorOptions(options.GlobalOptions));

            IncrementalValueProvider<StubEnvironment> stubEnvironment = context.CreateStubEnvironmentProvider();

            IncrementalValuesProvider<string> generateSingleStub = methodsToGenerate
                .Combine(stubEnvironment)
                .Combine(stubOptions)
                .Select(static (data, ct) => new
                {
                    data.Left.Left.Syntax,
                    data.Left.Left.Symbol,
                    Environment = data.Left.Right,
                    Options = data.Right,
                })
                .Select(
                    static (data, ct) => CalculateStubInformation(data.Syntax, data.Symbol, data.Environment, data.Options, ct)
                )
                .WithTrackingName(StepNames.CalculateStubInformation)
                .Combine(stubOptions)
                .Select(
                    static (data, ct) => GenerateSource(data.Left, data.Right)
                )
                .WithComparer(StringComparer.Ordinal)
                .WithTrackingName(StepNames.GenerateSingleStub);

            context.RegisterConcatenatedOutputs(generateSingleStub, "LibraryImports.g.cs");
        }

        private static List<string> GenerateForwardedAttributes(AttributeData? suppressGCTransitionAttribute, AttributeData? unmanagedCallConvAttribute, AttributeData? defaultDllImportSearchPathsAttribute, AttributeData? wasmImportLinkageAttribute, AttributeData? stackTraceHiddenAttribute, AttributeData? debuggerHiddenAttribute)
        {
            const string CallConvsField = "CallConvs";
            // Manually rehydrate the forwarded attributes with fully qualified types so we don't have to worry about any using directives.
            List<string> attributes = new();

            if (suppressGCTransitionAttribute is not null)
            {
                attributes.Add(TypeNames.GlobalAlias + TypeNames.SuppressGCTransitionAttribute);
            }

            if (stackTraceHiddenAttribute is not null)
            {
                attributes.Add(TypeNames.GlobalAlias + TypeNames.System_Diagnostics_StackTraceHiddenAttribute);
            }

            if (debuggerHiddenAttribute is not null)
            {
                attributes.Add(TypeNames.GlobalAlias + TypeNames.System_Diagnostics_DebuggerHiddenAttribute);
            }

            if (unmanagedCallConvAttribute is not null)
            {
                string unmanagedCallConv = TypeNames.GlobalAlias + TypeNames.UnmanagedCallConvAttribute;
                foreach (KeyValuePair<string, TypedConstant> arg in unmanagedCallConvAttribute.NamedArguments)
                {
                    if (arg.Key == CallConvsField)
                    {
                        var callConvs = new List<string>(arg.Value.Values.Length);
                        foreach (TypedConstant callConv in arg.Value.Values)
                        {
                            callConvs.Add($"typeof({((ITypeSymbol)callConv.Value!).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})");
                        }

                        string initializer = callConvs.Count == 0 ? "{ }" : $"{{ {string.Join(", ", callConvs)} }}";
                        unmanagedCallConv += $"({CallConvsField} = new {TypeNames.GlobalAlias}{TypeNames.System_Type}[] {initializer})";
                    }
                }
                attributes.Add(unmanagedCallConv);
            }
            if (defaultDllImportSearchPathsAttribute is not null)
            {
                string searchPaths = ((int)defaultDllImportSearchPathsAttribute.ConstructorArguments[0].Value!).ToString(CultureInfo.InvariantCulture);
                attributes.Add($"{TypeNames.GlobalAlias}{TypeNames.DefaultDllImportSearchPathsAttribute}(({TypeNames.GlobalAlias}{TypeNames.DllImportSearchPath}){searchPaths})");
            }
            if (wasmImportLinkageAttribute is not null)
            {
                attributes.Add(TypeNames.GlobalAlias + TypeNames.WasmImportLinkageAttribute);
            }
            return attributes;
        }

        private static string PrintGeneratedSource(
            IncrementalStubGenerationContext stub,
            ManagedToNativeStubGenerator stubGenerator)
        {
            var writer = new IndentedTextWriter();
            foreach (string attribute in stub.SignatureContext.AdditionalAttributes)
            {
                writer.WriteLine($"[{attribute}]");
            }

            ContainingSyntax userDeclaredMethod = stub.StubMethodSyntaxTemplate;
            writer.WriteLine($"{string.Join(" ", userDeclaredMethod.Modifiers)} {stub.SignatureContext.StubReturnType} {userDeclaredMethod.Identifier}({string.Join(", ", stub.SignatureContext.StubParameters)})");

            // Create stub function. The generated body performs unmanaged operations (pointers, fixed,
            // stackalloc, calling the extern local P/Invoke), so it is wrapped in an explicit unsafe block
            // rather than relying on an unsafe modifier on the containing type.
            using (writer.WriteBlock())
            {
                const string InnerPInvokeName = "__PInvoke";
                writer.WriteLine("unsafe");
                using (writer.WriteBlock())
                {
                    stubGenerator.GenerateStubStatements(writer, InnerPInvokeName);
                    writer.WriteLine("// Local P/Invoke");
                    WriteTargetDllImport(writer, stubGenerator, stub, InnerPInvokeName);
                }
            }
            return writer.ToString();
        }

        private static LibraryImportCompilationData? ProcessLibraryImportAttribute(AttributeData attrData)
        {
            // Found the LibraryImport, but it has an error so report the error.
            // This is most likely an issue with targeting an incorrect TFM.
            if (attrData.AttributeClass?.TypeKind is null or TypeKind.Error)
            {
                return null;
            }

            if (attrData.ConstructorArguments.Length == 0)
            {
                return null;
            }

            ImmutableDictionary<string, TypedConstant> namedArguments = ImmutableDictionary.CreateRange(attrData.NamedArguments);

            string? entryPoint = null;
            if (namedArguments.TryGetValue(nameof(LibraryImportCompilationData.EntryPoint), out TypedConstant entryPointValue))
            {
                if (entryPointValue.Value is not string)
                {
                    return null;
                }
                entryPoint = (string)entryPointValue.Value!;
            }

            return new LibraryImportCompilationData(attrData.ConstructorArguments[0].Value!.ToString())
            {
                EntryPoint = entryPoint,
            }.WithValuesFromNamedArguments(namedArguments);
        }

        private static IncrementalStubGenerationContext CalculateStubInformation(
            MethodDeclarationSyntax originalSyntax,
            IMethodSymbol symbol,
            StubEnvironment environment,
            LibraryImportGeneratorOptions options,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            INamedTypeSymbol? suppressGCTransitionAttrType = environment.SuppressGCTransitionAttrType;
            INamedTypeSymbol? unmanagedCallConvAttrType = environment.UnmanagedCallConvAttrType;
            INamedTypeSymbol? defaultDllImportSearchPathsAttrType = environment.DefaultDllImportSearchPathsAttrType;
            INamedTypeSymbol? wasmImportLinkageAttrType = environment.WasmImportLinkageAttrType;
            INamedTypeSymbol? stackTraceHiddenAttrType = environment.StackTraceHiddenAttrType;
            INamedTypeSymbol? debuggerHiddenAttrType = environment.DebuggerHiddenAttrType;
            // Get any attributes of interest on the method
            AttributeData? generatedDllImportAttr = null;
            AttributeData? suppressGCTransitionAttribute = null;
            AttributeData? unmanagedCallConvAttribute = null;
            AttributeData? defaultDllImportSearchPathsAttribute = null;
            AttributeData? wasmImportLinkageAttribute = null;
            AttributeData? stackTraceHiddenAttribute = null;
            AttributeData? debuggerHiddenAttribute = null;
            foreach (AttributeData attr in symbol.GetAttributes())
            {
                if (attr.AttributeClass is not null
                    && attr.AttributeClass.ToDisplayString() == TypeNames.LibraryImportAttribute)
                {
                    generatedDllImportAttr = attr;
                }
                else if (suppressGCTransitionAttrType is not null && SymbolEqualityComparer.Default.Equals(attr.AttributeClass, suppressGCTransitionAttrType))
                {
                    suppressGCTransitionAttribute = attr;
                }
                else if (unmanagedCallConvAttrType is not null && SymbolEqualityComparer.Default.Equals(attr.AttributeClass, unmanagedCallConvAttrType))
                {
                    unmanagedCallConvAttribute = attr;
                }
                else if (defaultDllImportSearchPathsAttrType is not null && SymbolEqualityComparer.Default.Equals(attr.AttributeClass, defaultDllImportSearchPathsAttrType))
                {
                    defaultDllImportSearchPathsAttribute = attr;
                }
                else if (wasmImportLinkageAttrType is not null && SymbolEqualityComparer.Default.Equals(attr.AttributeClass, wasmImportLinkageAttrType))
                {
                    wasmImportLinkageAttribute = attr;
                }
                else if (stackTraceHiddenAttrType is not null && SymbolEqualityComparer.Default.Equals(attr.AttributeClass, stackTraceHiddenAttrType))
                {
                    stackTraceHiddenAttribute = attr;
                }
                else if (debuggerHiddenAttrType is not null && SymbolEqualityComparer.Default.Equals(attr.AttributeClass, debuggerHiddenAttrType))
                {
                    debuggerHiddenAttribute = attr;
                }
            }

            Debug.Assert(generatedDllImportAttr is not null);

            var locations = new MethodSignatureDiagnosticLocations(originalSyntax);

            // Process the LibraryImport attribute
            LibraryImportCompilationData libraryImportData =
                ProcessLibraryImportAttribute(generatedDllImportAttr!) ??
                new LibraryImportCompilationData("INVALID_CSHARP_SYNTAX");

            // Create a diagnostics bag that discards all diagnostics.
            // Diagnostics are now reported by the analyzer, not the generator.
            var discardedDiagnostics = new GeneratorDiagnosticsBag(new DiagnosticDescriptorProvider(), locations, SR.ResourceManager, typeof(FxResources.Microsoft.Interop.LibraryImportGenerator.SR));
            ErrorHandlingInfo? errorHandlingInfo = ErrorHandlingInfoParser.Parse(symbol, environment, discardedDiagnostics);

            // Create the stub.
            var signatureContext = SignatureContext.Create(
                symbol,
                DefaultMarshallingInfoParser.Create(environment, discardedDiagnostics, symbol, libraryImportData, generatedDllImportAttr),
                environment,
                new CodeEmitOptions(SkipInit: true),
                typeof(LibraryImportGenerator).Assembly,
                errorHandlingInfo);

            var containingTypeContext = new ContainingSyntaxContext(originalSyntax);

            var methodSyntaxTemplate = new ContainingSyntax(originalSyntax.Modifiers, SyntaxKind.MethodDeclaration, originalSyntax.Identifier, originalSyntax.TypeParameterList);

            List<string> additionalAttributes = GenerateForwardedAttributes(suppressGCTransitionAttribute, unmanagedCallConvAttribute, defaultDllImportSearchPathsAttribute, wasmImportLinkageAttribute, stackTraceHiddenAttribute, debuggerHiddenAttribute);
            return new IncrementalStubGenerationContext(
                signatureContext,
                containingTypeContext,
                methodSyntaxTemplate,
                symbol.Name,
                locations,
                new SequenceEqualImmutableArray<string>(additionalAttributes.ToImmutableArray(), StringComparer.Ordinal),
                LibraryImportData.From(libraryImportData),
                options,
                environment.EnvironmentFlags);

        }

        private static string GenerateSource(
            IncrementalStubGenerationContext pinvokeStub,
            LibraryImportGeneratorOptions options)
        {
            if (options.GenerateForwarders)
            {
                return PrintForwarderStub(pinvokeStub.StubMethodSyntaxTemplate, pinvokeStub);
            }

            IMarshallingGeneratorResolver resolver = options.GenerateForwarders
                ? new ForwarderResolver()
                : DefaultMarshallingGeneratorResolver.Create(pinvokeStub.EnvironmentFlags, MarshalDirection.ManagedToUnmanaged, TypeNames.LibraryImportAttribute_ShortName, []);

            // Generate stub code
            // Note: Diagnostics are now reported by the analyzer, so we pass a discarding diagnostics bag
            var discardedDiagnostics = new GeneratorDiagnosticsBag(new DiagnosticDescriptorProvider(), pinvokeStub.DiagnosticLocation, SR.ResourceManager, typeof(FxResources.Microsoft.Interop.LibraryImportGenerator.SR));
            var stubGenerator = new ManagedToNativeStubGenerator(
                pinvokeStub.SignatureContext.ElementTypeInformation,
                pinvokeStub.LibraryImportData.SetLastError && !options.GenerateForwarders,
                discardedDiagnostics,
                resolver,
                new CodeEmitOptions(SkipInit: true));

            // Check if the generator should produce a forwarder stub - regular DllImport.
            // This is done if the stub doesn't contain any marshalling logic.
            if (stubGenerator.NoMarshallingRequired)
            {
                return PrintForwarderStub(pinvokeStub.StubMethodSyntaxTemplate, pinvokeStub);
            }

            return pinvokeStub.ContainingSyntaxContext.WrapMemberInContainingSyntax(PrintGeneratedSource(pinvokeStub, stubGenerator));
        }

        private static string PrintForwarderStub(ContainingSyntax userDeclaredMethod, IncrementalStubGenerationContext stub)
        {
            var writer = new IndentedTextWriter();
            ImmutableArray<string> modifiers = CodeWriterHelpers.AddModifier(userDeclaredMethod.Modifiers, "extern");
            writer.WriteLine($"[{CreateDllImportAttribute(stub.LibraryImportData, stub.MethodName, forwardSetLastError: true)}]");
            writer.WriteLine($"{string.Join(" ", modifiers)} {stub.SignatureContext.StubReturnType} {userDeclaredMethod.Identifier}({string.Join(", ", stub.SignatureContext.StubParameters)});");
            return stub.ContainingSyntaxContext.WrapMemberInContainingSyntax(writer.ToString());
        }

        private static void WriteTargetDllImport(
            IndentedTextWriter writer,
            ManagedToNativeStubGenerator stubGenerator,
            IncrementalStubGenerationContext stub,
            string stubTargetName)
        {
            GeneratedMethodSignature signature = stubGenerator.GenerateTargetMethodSignatureData();
            writer.WriteLine($"[{CreateDllImportAttribute(stub.LibraryImportData, stub.MethodName, forwardSetLastError: false)}]");

            if (!string.IsNullOrEmpty(signature.ReturnTypeAttributes))
            {
                writer.WriteLine($"[return: {signature.ReturnTypeAttributes}]");
            }

            if (!stub.ForwardedAttributes.Array.IsEmpty)
            {
                writer.WriteLine($"[{string.Join(", ", stub.ForwardedAttributes.Array)}]");
            }

            writer.WriteLine($"static extern unsafe {signature.ReturnType} {stubTargetName}{signature.ParameterList};");
        }

        private static string CreateDllImportAttribute(LibraryImportData target, string methodName, bool forwardSetLastError)
        {
            var arguments = new List<string>
            {
                CodeWriterHelpers.StringLiteral(target.ModuleName),
                $"{nameof(DllImportAttribute.EntryPoint)} = {CodeWriterHelpers.StringLiteral(target.EntryPoint ?? methodName)}",
                $"{nameof(DllImportAttribute.ExactSpelling)} = true"
            };

            // Forward the charset to either interop boundary so runtime-marshalled types use the requested encoding.
            if (target.IsUserDefined.HasFlag(InteropAttributeMember.StringMarshalling)
                && target.StringMarshalling == StringMarshalling.Utf16)
            {
                arguments.Add($"{nameof(DllImportAttribute.CharSet)} = {CreateEnumExpression(CharSet.Unicode)}");
            }

            if (forwardSetLastError && target.IsUserDefined.HasFlag(InteropAttributeMember.SetLastError))
            {
                arguments.Add($"{nameof(DllImportAttribute.SetLastError)} = {(target.SetLastError ? "true" : "false")}");
            }

            return $"{TypeNames.GlobalAlias}{TypeNames.DllImportAttribute}({string.Join(", ", arguments)})";
        }

        private static string CreateEnumExpression<T>(T value) where T : Enum
            => $"{TypeNames.GlobalAlias}{typeof(T).FullName}.{value}";
    }
}
