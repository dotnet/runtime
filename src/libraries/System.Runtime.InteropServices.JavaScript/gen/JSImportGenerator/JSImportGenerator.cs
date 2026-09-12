// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGenerators;

[assembly: System.Resources.NeutralResourcesLanguage("en-US")]

namespace Microsoft.Interop.JavaScript
{
    [Generator]
    public sealed class JSImportGenerator : IIncrementalGenerator
    {
        internal sealed record IncrementalStubGenerationContext(
            JSSignatureContext SignatureContext,
            ContainingSyntaxContext ContainingSyntaxContext,
            DeclarationHeader StubMethodSyntaxTemplate,
            MethodSignatureDiagnosticLocations DiagnosticLocation,
            JSImportData JSImportData);

        public static class StepNames
        {
            public const string CalculateStubInformation = nameof(CalculateStubInformation);
            public const string GenerateSingleStub = nameof(GenerateSingleStub);
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Invalid declarations are diagnosed by the analyzer.
            var methodsToGenerate = context.SyntaxProvider
                .ForAttributeWithMetadataName(Constants.JSImportAttribute,
                    static (node, ct) => node is MethodDeclarationSyntax,
                    static (context, ct) => new { Syntax = (MethodDeclarationSyntax)context.TargetNode, Symbol = (IMethodSymbol)context.TargetSymbol })
                .Where(static data =>
                    JSInteropDiagnosticsAnalyzer.GetDiagnosticIfInvalidMethodForGeneration(
                        data.Syntax, data.Symbol,
                        GeneratorDiagnostics.InvalidImportAttributedMethodSignature,
                        GeneratorDiagnostics.InvalidImportAttributedMethodContainingTypeMissingModifiers,
                        requiresImplementation: false) is null);

            IncrementalValueProvider<StubEnvironment> stubEnvironment = context.CreateStubEnvironmentProvider();

            IncrementalValuesProvider<string> generateSingleStub = methodsToGenerate
                .Combine(stubEnvironment)
                .Select(static (data, ct) => CalculateStubInformation(data.Left.Syntax, data.Left.Symbol, data.Right, ct))
                .WithTrackingName(StepNames.CalculateStubInformation)
                .Select(static (data, ct) => GenerateSource(data))
                .WithComparer(StringComparer.Ordinal)
                .WithTrackingName(StepNames.GenerateSingleStub);

            context.RegisterConcatenatedOutputs(generateSingleStub, "JSImports.g.cs");
        }

        internal static JSImportData? ProcessJSImportAttribute(AttributeData attrData)
        {
            // This can occur when targeting an incompatible reference assembly.
            if (attrData.AttributeClass?.TypeKind is null or TypeKind.Error)
            {
                return null;
            }

            if (attrData.ConstructorArguments.Length == 1)
            {
                return new JSImportData(attrData.ConstructorArguments[0].Value!.ToString(), null);
            }
            if (attrData.ConstructorArguments.Length == 2)
            {
                return new JSImportData(attrData.ConstructorArguments[0].Value!.ToString(), attrData.ConstructorArguments[1].Value!.ToString());
            }
            return null;
        }

        private static IncrementalStubGenerationContext CalculateStubInformation(
            MethodDeclarationSyntax originalSyntax,
            IMethodSymbol symbol,
            StubEnvironment environment,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            AttributeData? jsImportAttr = null;
            foreach (AttributeData attr in symbol.GetAttributes())
            {
                if (attr.AttributeClass is not null
                    && attr.AttributeClass.ToDisplayString() == Constants.JSImportAttribute)
                {
                    jsImportAttr = attr;
                }
            }

            Debug.Assert(jsImportAttr is not null);

            var locations = new MethodSignatureDiagnosticLocations(originalSyntax);
            var generatorDiagnostics = new GeneratorDiagnosticsBag(new DescriptorProvider(), locations, SR.ResourceManager, typeof(FxResources.Microsoft.Interop.JavaScript.JSImportGenerator.SR));
            JSImportData jsImportData = ProcessJSImportAttribute(jsImportAttr!) ?? new JSImportData("INVALID_CSHARP_SYNTAX", null);
            var signatureContext = JSSignatureContext.Create(symbol, environment, generatorDiagnostics, ct);
            ContainingSyntaxContext containingTypeContext = originalSyntax.GetContainingSyntaxContext();
            DeclarationHeader methodTemplate = originalSyntax.GetDeclarationTemplate();

            return new IncrementalStubGenerationContext(signatureContext, containingTypeContext, methodTemplate, locations, jsImportData);
        }

        private static string GenerateSource(IncrementalStubGenerationContext incrementalContext)
        {
            var diagnostics = new GeneratorDiagnosticsBag(new DescriptorProvider(), incrementalContext.DiagnosticLocation, SR.ResourceManager, typeof(FxResources.Microsoft.Interop.JavaScript.JSImportGenerator.SR));
            const int NumImplicitArguments = 2;
            ImmutableArray<TypePositionInfo> originalElementInfo = incrementalContext.SignatureContext.SignatureContext.ElementTypeInformation;
            ImmutableArray<TypePositionInfo>.Builder typeInfoBuilder = ImmutableArray.CreateBuilder<TypePositionInfo>(originalElementInfo.Length + NumImplicitArguments);

            TypePositionInfo nativeOnlyParameterTemplate = new TypePositionInfo(
                SpecialTypeInfo.Void,
                new JSMarshallingInfo(NoMarshallingInfo.Instance, new JSInvalidTypeInfo()))
            {
                ManagedIndex = TypePositionInfo.UnsetIndex,
            };

            typeInfoBuilder.Add(nativeOnlyParameterTemplate with
            {
                InstanceIdentifier = Constants.ArgumentException,
                NativeIndex = 0,
            });
            typeInfoBuilder.Add(nativeOnlyParameterTemplate with
            {
                InstanceIdentifier = Constants.ArgumentReturn,
                NativeIndex = 1,
            });

            bool hasReturn = false;
            foreach (TypePositionInfo info in originalElementInfo)
            {
                // The implicit arguments establish ambient state before any parameter is marshalled.
                TypePositionInfo updatedInfo = info with
                {
                    MarshallingAttributeInfo = info.MarshallingAttributeInfo is JSMarshallingInfo jsInfo
                        ? jsInfo.AddElementDependencies([typeInfoBuilder[0], typeInfoBuilder[1]])
                        : info.MarshallingAttributeInfo,
                };

                if (info.IsManagedReturnPosition)
                {
                    hasReturn = info.ManagedType != SpecialTypeInfo.Void;
                }

                typeInfoBuilder.Add(info.IsNativeReturnPosition
                    ? updatedInfo
                    : updatedInfo with { NativeIndex = updatedInfo.NativeIndex + NumImplicitArguments });
            }

            var stubGenerator = new ManagedToNativeStubGenerator(
                typeInfoBuilder.ToImmutable(),
                setLastError: false,
                diagnostics,
                new CompositeMarshallingGeneratorResolver(
                    new NoSpanAndTaskMixingResolver(),
                    new JSGeneratorResolver()),
                new CodeEmitOptions(SkipInit: true));

            var writer = new IndentedTextWriter();
            incrementalContext.ContainingSyntaxContext.WriteToWithUnsafeModifier(
                writer,
                (Context: incrementalContext, Generator: stubGenerator, HasReturn: hasReturn),
                static (writer, state) => WriteImport(writer, state.Context, state.Generator, state.HasReturn));
            return writer.ToString();
        }

        private static void WriteImport(
            IndentedTextWriter writer,
            IncrementalStubGenerationContext context,
            ManagedToNativeStubGenerator stubGenerator,
            bool hasReturn)
        {
            const string LocalFunctionName = "__InvokeJSFunction";
            SignatureContext signature = context.SignatureContext.SignatureContext;
            writer.WriteLine($"[{Constants.DebuggerNonUserCodeAttribute}]");
            writer.WriteLine($"[{Constants.SupportedOSPlatformAttribute}({CodeWriterHelpers.StringLiteral(Constants.BrowserPlatform)})]");
            writer.WriteLine($"{string.Join(" ", context.StubMethodSyntaxTemplate.Modifiers)} {signature.StubReturnType} {context.StubMethodSyntaxTemplate.Identifier}({string.Join(", ", signature.StubParameters.Select(static parameter => parameter.Declaration))})");
            using (writer.WriteBlock())
            {
                WriteBinding(writer, context.JSImportData, context.SignatureContext);
                writer.WriteLine();
                stubGenerator.GenerateStubBody(writer, LocalFunctionName);
                writer.WriteLine();
                WriteInvokeFunction(writer, LocalFunctionName, context.SignatureContext, stubGenerator.GenerateTargetMethodSignatureData(), hasReturn);
            }
            writer.WriteLine();
            writer.WriteLine($"static {Constants.JSFunctionSignatureGlobal} {context.SignatureContext.BindingName};");
        }

        private static void WriteBinding(IndentedTextWriter writer, JSImportData jsImportData, JSSignatureContext signature)
        {
            string functionName = CodeWriterHelpers.StringLiteral(jsImportData.FunctionName);
            string moduleName = jsImportData.ModuleName is null ? "null" : CodeWriterHelpers.StringLiteral(jsImportData.ModuleName);
            string signatures = SignatureBindingHelpers.CreateSignaturesArgument(signature.SignatureContext.ElementTypeInformation, StubCodeContext.DefaultManagedToNativeStub);
            writer.WriteLine($"if ({signature.BindingName} == null)");
            using (writer.WriteBlock())
            {
                writer.WriteLine($"{signature.BindingName} = {Constants.JSFunctionSignatureGlobal}.{Constants.BindJSFunctionMethod}({functionName}, {moduleName}, {signatures});");
            }
        }

        private static void WriteInvokeFunction(
            IndentedTextWriter writer,
            string functionName,
            JSSignatureContext signatureContext,
            GeneratedMethodSignature signature,
            bool hasReturn)
        {
            string arguments = "[" + string.Join(", ", signature.Parameters.Select(static parameter => parameter.Identifier)) + "]";
            writer.WriteLine($"[{Constants.DebuggerNonUserCodeAttribute}]");
            writer.WriteLine($"{(hasReturn ? Constants.JSMarshalerArgumentGlobal : "void")} {functionName}{signature.ParameterList}");
            using (writer.WriteBlock())
            {
                if (hasReturn)
                {
                    writer.WriteLine($"{Constants.SpanGlobal}<{Constants.JSMarshalerArgumentGlobal}> {Constants.ArgumentsBuffer} = {arguments};");
                    writer.WriteLine($"{Constants.JSFunctionSignatureGlobal}.InvokeJS({signatureContext.BindingName}, {Constants.ArgumentsBuffer});");
                    writer.WriteLine($"return {Constants.ArgumentsBuffer}[1];");
                }
                else
                {
                    writer.WriteLine($"{Constants.JSFunctionSignatureGlobal}.InvokeJS({signatureContext.BindingName}, {arguments});");
                }
            }
        }
    }
}
