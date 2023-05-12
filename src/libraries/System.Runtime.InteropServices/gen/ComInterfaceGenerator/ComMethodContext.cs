// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    public sealed partial class ComInterfaceGenerator
    {
        /// <summary>
        /// Represents a method, its declaring interface, and its index in the interface's vtable.
        /// This type contains all information necessary to generate the corresponding methods in the ComInterfaceGenerator
        /// </summary>
        private sealed record ComMethodContext(
            ComInterfaceContext DeclaringInterface,
            // TypeKeyOwner is also the interface that the code is being generated for.
            ComInterfaceContext TypeKeyOwner,
            ComMethodInfo MethodInfo,
            int Index,
            IncrementalMethodStubGenerationContext GenerationContext)
        {
            public bool IsInheritedMethod => DeclaringInterface != TypeKeyOwner;

            public sealed record Builder(ComInterfaceContext DeclaringInterface, ComMethodInfo MethodInfo, int Index);

            public GeneratedMethodContextBase ManagedToUnmanagedStub
            {
                get
                {
                    if (GenerationContext.VtableIndexData.Direction is not (MarshalDirection.ManagedToUnmanaged or MarshalDirection.Bidirectional))
                    {
                        return new SkippedStubContext(DeclaringInterface.Info.Type);
                    }
                    var (methodStub, diagnostics) = VirtualMethodPointerStubGenerator.GenerateManagedToNativeStub(GenerationContext);
                    return new GeneratedStubCodeContext(GenerationContext.TypeKeyOwner, GenerationContext.ContainingSyntaxContext, new(methodStub), new(diagnostics));
                }
            }

            public GeneratedMethodContextBase NativeToManagedStub
            {
                get
                {
                    if (GenerationContext.VtableIndexData.Direction is not (MarshalDirection.UnmanagedToManaged or MarshalDirection.Bidirectional))
                    {
                        return new SkippedStubContext(GenerationContext.OriginalDefiningType);
                    }
                    var (methodStub, diagnostics) = VirtualMethodPointerStubGenerator.GenerateNativeToManagedStub(GenerationContext);
                    return new GeneratedStubCodeContext(GenerationContext.OriginalDefiningType, GenerationContext.ContainingSyntaxContext, new(methodStub), new(diagnostics));
                }
            }

            public MethodDeclarationSyntax GenerateUnreachableExceptionStub()
            {
                // DeclarationCopiedFromBaseDeclaration(<Arguments>) => throw new UnreachableException("This method should not be reached");
                return MethodInfo.Syntax
                    .WithAttributeLists(List<AttributeListSyntax>())
                    .WithExplicitInterfaceSpecifier(ExplicitInterfaceSpecifier(
                        ParseName(DeclaringInterface.Info.Type.FullTypeName)))
                    .WithExpressionBody(ArrowExpressionClause(
                        ThrowExpression(
                            ObjectCreationExpression(
                                ParseTypeName(TypeNames.UnreachableException))
                                .WithArgumentList(ArgumentList()))));
            }

            public MethodDeclarationSyntax GenerateShadow()
            {
                // DeclarationCopiedFromBaseDeclaration(<Arguments>)
                // {
                //    return ((<baseInterfaceType>)this).<MethodName>(<Arguments>);
                // }
                // TODO: Copy full name of parameter types and attributes / attribute arguments for parameters
                return MethodInfo.Syntax
                    .WithModifiers(TokenList(Token(SyntaxKind.NewKeyword)))
                    .WithExpressionBody(
                        ArrowExpressionClause(
                            InvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    ParenthesizedExpression(
                                        CastExpression(DeclaringInterface.Info.Type.Syntax, IdentifierName("this"))),
                                    IdentifierName(MethodInfo.MethodName)),
                                ArgumentList(
                                    // TODO: RefKind keywords
                                    SeparatedList(MethodInfo.Parameters.Select(p =>
                                    Argument(IdentifierName(p.Name))))))));
            }

            /// <summary>
            /// Returns a flat list of <see cref="ComMethodContext.Builder"/> and it's type key owner that represents all declared methods, and inherited methods.
            /// Guarantees the output will be sorted by order of interface input order, then by vtable order.
            /// </summary>
            public static List<(ComInterfaceContext TypeKeyOwner, Builder Method)> CalculateAllMethods(IEnumerable<(ComInterfaceContext, SequenceEqualImmutableArray<ComMethodInfo>)> ifaceAndDeclaredMethods, CancellationToken _)
            {
            // opt : change this to only take in a hierarchy of interfaces. we calc that before and select ober that
                var ifaceToDeclaredMethodsMap = ifaceAndDeclaredMethods.ToDictionary(static pair => pair.Item1, static pair => pair.Item2);
                // Track insertion order
                var allMethodsCache = new Dictionary<ComInterfaceContext, ImmutableArray<Builder>>();

                List<(ComInterfaceContext TypeKeyOwner, Builder Method)> accumulator = new();
                foreach (var kvp in ifaceAndDeclaredMethods)
                {
                    var methods = AddMethods(kvp.Item1, kvp.Item2);
                    foreach (var method in methods)
                    {
                        accumulator.Add((kvp.Item1, method));
                    }
                }
                return accumulator;

                ImmutableArray<Builder> AddMethods(ComInterfaceContext iface, IEnumerable<ComMethodInfo> declaredMethods)
                {
                    if (allMethodsCache.TryGetValue(iface, out var cachedValue))
                    {
                        return cachedValue;
                    }

                    int startingIndex = 3;
                    List<Builder> methods = new();
                    // If we have a base interface, we should add the inherited methods to our list in vtable order
                    if (iface.Base is not null)
                    {
                        var baseComIface = iface.Base;
                        ImmutableArray<Builder> baseMethods;
                        if (!allMethodsCache.TryGetValue(baseComIface, out var pair))
                        {
                            baseMethods = AddMethods(baseComIface, ifaceToDeclaredMethodsMap[baseComIface]);
                        }
                        else
                        {
                            baseMethods = pair;
                        }
                        methods.AddRange(baseMethods);
                        startingIndex += baseMethods.Length;
                    }
                    // Then we append the declared methods in vtable order
                    foreach (var method in declaredMethods)
                    {
                        methods.Add(new Builder(iface, method, startingIndex++));
                    }
                    // Cache so we don't recalculate if many interfaces inherit from the same one
                    var imm = methods.ToImmutableArray();
                    allMethodsCache[iface] = imm;
                    return imm;
                }
            }
        }
    }
}
