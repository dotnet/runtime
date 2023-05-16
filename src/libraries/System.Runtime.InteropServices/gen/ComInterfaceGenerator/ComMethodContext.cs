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
        /// <param name="OriginalDeclaringInterface">
        /// The interface that originally declared the method in user code
        /// </param>
        /// <param name="OwningInterface">
        /// The interface that this methods is being generated for (may be different that OriginalDeclaringInterface if it is an inherited method)
        /// </param>
        /// <param name="MethodInfo">The basic information about the method.</param>
        /// <param name="Index">The index on the interface vtable that points to this method</param>
        /// <param name="GenerationContext"></param>
        private sealed record ComMethodContext(
            ComInterfaceContext OriginalDeclaringInterface,
            ComInterfaceContext OwningInterface,
            ComMethodInfo MethodInfo,
            int Index,
            IncrementalMethodStubGenerationContext GenerationContext)
        {
            /// <summary>
            /// A partially constructed <see cref="ComMethodContext"/> that does not have a <see cref="IncrementalMethodStubGenerationContext"/> generated for it yet.
            /// <see cref="Builder"/> can be constructed without a reference to an ISymbol, whereas the <see cref="IncrementalMethodStubGenerationContext"/> requires an ISymbol
            /// </summary>
            public sealed record Builder(ComInterfaceContext OriginalDeclaringInterface, ComMethodInfo MethodInfo, int Index);

            public bool IsInheritedMethod => OriginalDeclaringInterface != OwningInterface;

            public GeneratedMethodContextBase GetManagedToUnmanagedStub()
            {
                if (GenerationContext.VtableIndexData.Direction is not (MarshalDirection.ManagedToUnmanaged or MarshalDirection.Bidirectional))
                {
                    return new SkippedStubContext(OriginalDeclaringInterface.Info.Type);
                }
                var (methodStub, diagnostics) = VirtualMethodPointerStubGenerator.GenerateManagedToNativeStub(GenerationContext);
                return new GeneratedStubCodeContext(GenerationContext.TypeKeyOwner, GenerationContext.ContainingSyntaxContext, new(methodStub), new(diagnostics));
            }

            public GeneratedMethodContextBase GetNativeToManagedStub()
            {
                if (GenerationContext.VtableIndexData.Direction is not (MarshalDirection.UnmanagedToManaged or MarshalDirection.Bidirectional))
                {
                    return new SkippedStubContext(GenerationContext.OriginalDefiningType);
                }
                var (methodStub, diagnostics) = VirtualMethodPointerStubGenerator.GenerateNativeToManagedStub(GenerationContext);
                return new GeneratedStubCodeContext(GenerationContext.OriginalDefiningType, GenerationContext.ContainingSyntaxContext, new(methodStub), new(diagnostics));
            }

            public MethodDeclarationSyntax GenerateUnreachableExceptionStub()
            {
                // DeclarationCopiedFromBaseDeclaration(<Arguments>) => throw new UnreachableException("This method should not be reached");
                return MethodInfo.Syntax
                    .WithModifiers(TokenList())
                    .WithAttributeLists(List<AttributeListSyntax>())
                    .WithExplicitInterfaceSpecifier(ExplicitInterfaceSpecifier(
                        ParseName(OriginalDeclaringInterface.Info.Type.FullTypeName)))
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
                var forwarder = new Forwarder();
                return MethodDeclaration(GenerationContext.SignatureContext.StubReturnType, MethodInfo.MethodName)
                    .WithModifiers(TokenList(Token(SyntaxKind.NewKeyword)))
                    .WithParameterList(ParameterList(SeparatedList(GenerationContext.SignatureContext.StubParameters)))
                    .WithExpressionBody(
                        ArrowExpressionClause(
                            InvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    ParenthesizedExpression(
                                        CastExpression(OriginalDeclaringInterface.Info.Type.Syntax, IdentifierName("this"))),
                                    IdentifierName(MethodInfo.MethodName)),
                                ArgumentList(
                                    SeparatedList(GenerationContext.SignatureContext.ManagedParameters.Select(p => forwarder.AsArgument(p, new ManagedStubCodeContext())))))))
                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
            }

            /// <summary>
            /// Returns a flat list of <see cref="ComMethodContext.Builder"/> and it's type key owner that represents all declared methods, and inherited methods.
            /// Guarantees the output will be sorted by order of interface input order, then by vtable order.
            /// </summary>
            public static List<(ComInterfaceContext TypeKeyOwner, Builder Method)> CalculateAllMethods(IEnumerable<(ComInterfaceContext, SequenceEqualImmutableArray<ComMethodInfo>)> ifaceAndDeclaredMethods, CancellationToken _)
            {
                // Optimization : This step technically only needs a single interface inheritance hierarchy.
                // We can calculate all inheritance chains in a previous step and only pass a single inheritance chain to this method.
                // This way, when a single method changes, we would only need to recalculate this for the inheritance chain in which that method exists.

                var ifaceToDeclaredMethodsMap = ifaceAndDeclaredMethods.ToDictionary(static pair => pair.Item1, static pair => pair.Item2);
                var allMethodsCache = new Dictionary<ComInterfaceContext, ImmutableArray<Builder>>();
                var accumulator = new List<(ComInterfaceContext TypeKeyOwner, Builder Method)>();
                foreach (var kvp in ifaceAndDeclaredMethods)
                {
                    var methods = AddMethods(kvp.Item1, kvp.Item2);
                    foreach (var method in methods)
                    {
                        accumulator.Add((kvp.Item1, method));
                    }
                }
                return accumulator;

                /// <summary>
                /// Adds methods to a cache and returns inherited and declared methods for the interface in vtable order
                /// </summary>
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
