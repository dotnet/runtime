// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;
using Microsoft.CodeAnalysis.Operations;
using static Microsoft.Interop.Analyzers.AnalyzerDiagnostics;

namespace Microsoft.Interop.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RuntimeComApiUsageWithSourceGeneratedComAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(RuntimeComApisDoNotSupportSourceGeneratedCom, CastsBetweenRuntimeComAndSourceGeneratedComNotSupported);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(context =>
            {
                INamedTypeSymbol? marshalType = context.Compilation.GetBestTypeByMetadataName(TypeNames.System_Runtime_InteropServices_Marshal);
                INamedTypeSymbol? generatedComClassAttribute = context.Compilation.GetBestTypeByMetadataName(TypeNames.GeneratedComClassAttribute);
                INamedTypeSymbol? generatedComInterfaceAttribute = context.Compilation.GetBestTypeByMetadataName(TypeNames.GeneratedComInterfaceAttribute);
                INamedTypeSymbol? comObjectType = context.Compilation.GetBestTypeByMetadataName(TypeNames.System_Runtime_InteropServices_Marshalling_ComObject);

                List<Func<ITypeSymbol, bool>> sourceGeneratedComRecognizers = new();
                if (generatedComClassAttribute is not null)
                {
                    sourceGeneratedComRecognizers.Add(type => type.GetAttributes().Any(attr => generatedComClassAttribute.Equals(attr.AttributeClass, SymbolEqualityComparer.Default)));
                }
                if (generatedComInterfaceAttribute is not null)
                {
                    sourceGeneratedComRecognizers.Add(type => type.GetAttributes().Any(attr => generatedComInterfaceAttribute.Equals(attr.AttributeClass, SymbolEqualityComparer.Default)));
                }
                if (comObjectType is not null)
                {
                    sourceGeneratedComRecognizers.Add(type => type.Equals(comObjectType, SymbolEqualityComparer.Default));
                }

                if (marshalType is null || sourceGeneratedComRecognizers.Count == 0)
                {
                    return;
                }

                var methodsOfInterest = new Dictionary<ISymbol, ImmutableArray<Func<IInvocationOperation, (ITypeSymbol, Location)?>>>(SymbolEqualityComparer.Default);

                var firstArgumentTypeLookup = CreateArgumentTypeLookup(0);

                var firstArgumentTypeLookupOnly = ImmutableArray.Create(firstArgumentTypeLookup);

                methodsOfInterest.Add(marshalType.GetMembers("SetComObjectData")[0], firstArgumentTypeLookupOnly);
                methodsOfInterest.Add(marshalType.GetMembers("GetComObjectData")[0], firstArgumentTypeLookupOnly);
                methodsOfInterest.Add(marshalType.GetMembers("ReleaseComObject")[0], firstArgumentTypeLookupOnly);
                methodsOfInterest.Add(marshalType.GetMembers("FinalReleaseComObject")[0], firstArgumentTypeLookupOnly);

                foreach (var createAggregatedObject in marshalType.GetMembers("CreateAggregatedObject"))
                {
                    if (createAggregatedObject is IMethodSymbol { IsGenericMethod: true })
                    {
                        methodsOfInterest.Add(createAggregatedObject, ImmutableArray.Create(CreateTypeArgumentTypeLookup(0), CreateArgumentTypeLookup(1)));
                    }
                    else
                    {
                        methodsOfInterest.Add(createAggregatedObject, ImmutableArray.Create(CreateArgumentTypeLookup(1)));
                    }
                }

                foreach (var createWrapperOfType in marshalType.GetMembers("CreateWrapperOfType"))
                {
                    if (createWrapperOfType is IMethodSymbol { IsGenericMethod: true })
                    {
                        methodsOfInterest.Add(createWrapperOfType, ImmutableArray.Create(CreateTypeArgumentTypeLookup(0), CreateTypeArgumentTypeLookup(1), firstArgumentTypeLookup));
                    }
                    else
                    {
                        methodsOfInterest.Add(createWrapperOfType, ImmutableArray.Create(firstArgumentTypeLookup, CreateTypeOfArgumentTypeLookup(1)));
                    }
                }

                methodsOfInterest.Add(marshalType.GetMembers("GetTypedObjectForIUnknown")[0], ImmutableArray.Create(CreateTypeOfArgumentTypeLookup(1)));
                methodsOfInterest.Add(marshalType.GetMembers("GetIUnknownForObject")[0], firstArgumentTypeLookupOnly);
                methodsOfInterest.Add(marshalType.GetMembers("GetIDispatchForObject")[0], firstArgumentTypeLookupOnly);

                foreach (var getComInterfaceForObject in marshalType.GetMembers("GetComInterfaceForObject"))
                {
                    if (getComInterfaceForObject is IMethodSymbol { IsGenericMethod: true })
                    {
                        methodsOfInterest.Add(getComInterfaceForObject, ImmutableArray.Create(CreateTypeArgumentTypeLookup(0), CreateTypeArgumentTypeLookup(1), firstArgumentTypeLookup));
                    }
                    else
                    {
                        methodsOfInterest.Add(getComInterfaceForObject, ImmutableArray.Create(CreateArgumentTypeLookup(0), CreateTypeOfArgumentTypeLookup(1)));
                    }
                }

                context.RegisterOperationAction(context =>
                {
                    var operation = (IInvocationOperation)context.Operation;

                    if (methodsOfInterest.TryGetValue(operation.TargetMethod.OriginalDefinition, out ImmutableArray<Func<IInvocationOperation, (ITypeSymbol, Location)?>> discoverers))
                    {
                        foreach (Func<IInvocationOperation, (ITypeSymbol, Location)?> discoverer in discoverers)
                        {
                            var typeInfo = discoverer(operation);
                            if (typeInfo is (ITypeSymbol targetType, Location diagnosticLocation))
                            {
                                foreach (var recognizer in sourceGeneratedComRecognizers)
                                {
                                    if (recognizer(targetType))
                                    {
                                        context.ReportDiagnostic(
                                            Diagnostic.Create(
                                                RuntimeComApisDoNotSupportSourceGeneratedCom,
                                                diagnosticLocation,
                                                operation.TargetMethod.ToMinimalDisplayString(operation.SemanticModel, operation.Syntax.SpanStart),
                                                targetType.ToMinimalDisplayString(operation.SemanticModel, operation.Syntax.SpanStart)));
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }, OperationKind.Invocation);

                var getObjectForIUnknown = marshalType.GetMembers("GetObjectForIUnknown")[0];

                context.RegisterOperationAction(context =>
                {
                    var operation = (IConversionOperation)context.Operation;

                    if (operation.Type is INamedTypeSymbol { IsComImport: true })
                    {
                        IOperation operand = operation.Operand;
                        if (operand is IConversionOperation { Type.SpecialType: SpecialType.System_Object } objConversion)
                        {
                            operand = objConversion.Operand;
                        }
                        if (operand.Type is null)
                        {
                            // Some operations like the "null" literal expression don't have a type.
                            // These expressions definitely aren't a source-generated COM type, so we can skip them.
                            return;
                        }
                        foreach (var recognizer in sourceGeneratedComRecognizers)
                        {
                            if (recognizer(operand.Type))
                            {
                                context.ReportDiagnostic(
                                    Diagnostic.Create(
                                        CastsBetweenRuntimeComAndSourceGeneratedComNotSupported,
                                        operation.Syntax.GetLocation()));
                                break;
                            }
                        }
                    }

                    foreach (var recognizer in sourceGeneratedComRecognizers)
                    {
                        if (recognizer(operation.Type))
                        {
                            IOperation operand = operation.Operand;
                            if (operand is IConversionOperation { Type.SpecialType: SpecialType.System_Object } objConversion)
                            {
                                operand = objConversion.Operand;
                            }
                            if (operand.Type is INamedTypeSymbol { IsComImport: true })
                            {
                                context.ReportDiagnostic(
                                    Diagnostic.Create(
                                        CastsBetweenRuntimeComAndSourceGeneratedComNotSupported,
                                            operation.Syntax.GetLocation()));
                                break;
                            }
                            else if (operand is IInvocationOperation invocation && invocation.TargetMethod.Equals(getObjectForIUnknown, SymbolEqualityComparer.Default))
                            {
                                context.ReportDiagnostic(
                                    Diagnostic.Create(
                                        CastsBetweenRuntimeComAndSourceGeneratedComNotSupported,
                                            operation.Syntax.GetLocation()));
                                break;
                            }
                        }
                    }
                }, OperationKind.Conversion);

                static Func<IInvocationOperation, (ITypeSymbol Type, Location location)?> CreateArgumentTypeLookup(int ordinal) => invocation => invocation.GetArgumentByOrdinal(ordinal).Value switch
                {
                    IConversionOperation conversion => (conversion.Operand.Type, conversion.Operand.Syntax.GetLocation()),
                    IOperation op => (op.Type, op.Syntax.GetLocation())
                };

                static Func<IInvocationOperation, (ITypeSymbol Type, Location location)?> CreateTypeArgumentTypeLookup(int ordinal) => invocation =>
                {
                    var type = invocation.TargetMethod.TypeArguments[ordinal];

                    var invocationSyntax = (InvocationExpressionSyntax)invocation.Syntax;
                    var expression = invocationSyntax.Expression;

                    Location? location = null;

                    if (expression.IsKind(SyntaxKind.SimpleMemberAccessExpression))
                    {
                        expression = ((MemberAccessExpressionSyntax)expression).Name;
                    }

                    if (expression.IsKind(SyntaxKind.GenericName))
                    {
                        location = ((GenericNameSyntax)expression).TypeArgumentList.Arguments[ordinal].GetLocation();
                    }

                    if (location is null)
                    {
                        // If we couldn't find the type argument in source, then it was inferred. Don't emit a warning for the inferred type argument.
                        // We'll emit a warning for the argument that was passed in instead.
                        return null;
                    }

                    return (type, location);
                };

                static Func<IInvocationOperation, (ITypeSymbol Type, Location location)?> CreateTypeOfArgumentTypeLookup(int ordinal) => invocation => invocation.GetArgumentByOrdinal(ordinal).Value switch
                {
                    ITypeOfOperation typeOf => (typeOf.TypeOperand, ((TypeOfExpressionSyntax)typeOf.Syntax).Type.GetLocation()),
                    _ => null
                };
            });
        }
    }
}
