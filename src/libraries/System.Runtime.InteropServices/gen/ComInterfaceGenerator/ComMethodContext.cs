// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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

    /// <summary>
    /// Represents a method, its declaring interface, and its index in the interface's vtable.
    /// This type contains all information necessary to generate the corresponding methods in the ComInterfaceGenerator
    /// </summary>
    internal sealed class ComMethodContext : IEquatable<ComMethodContext>
    {
        /// <summary>
        /// A partially constructed <see cref="ComMethodContext"/> that does not have a <see cref="IncrementalMethodStubGenerationContext"/> generated for it yet.
        /// <see cref="Builder"/> can be constructed without a reference to an ISymbol, whereas the <see cref="IncrementalMethodStubGenerationContext"/> requires an ISymbol
        /// </summary>
        /// <param name="OriginalDeclaringInterface">
        /// The interface that originally declared the method in user code
        /// </param>
        /// <param name="MethodInfo">The basic information about the method.</param>
        /// <param name="Index">The vtable index for the method.</param>
        public sealed record Builder(ComInterfaceContext OriginalDeclaringInterface, ComMethodInfo MethodInfo, int Index);

        /// <summary>
        /// The fully-constructed immutable state for a <see cref="ComMethodContext"/>.
        /// </summary>
        private record struct State(
            ComInterfaceContext OriginalDeclaringInterface,
            ComInterfaceContext OwningInterface,
            ComMethodInfo MethodInfo,
            IncrementalMethodStubGenerationContext GenerationContext);

        private readonly State _state;

        /// <summary>
        /// Construct a full method context from the <paramref name="builder"/>, context, and additional information.
        /// </summary>
        /// <param name="builder">The partially constructed context</param>
        /// <param name="owningInterface">The final owning interface of this method context</param>
        /// <param name="generationContext">The generation context for this method</param>
        public ComMethodContext(Builder builder, ComInterfaceContext owningInterface, IncrementalMethodStubGenerationContext generationContext)
        {
            _state = new State(builder.OriginalDeclaringInterface, owningInterface, builder.MethodInfo, generationContext);
        }

        public override bool Equals(object obj) => obj is ComMethodContext other && Equals(other);

        public override int GetHashCode() => _state.GetHashCode();

        public bool Equals(ComMethodContext other) => _state.Equals(other);

        public ComInterfaceContext OriginalDeclaringInterface => _state.OriginalDeclaringInterface;

        public ComInterfaceContext OwningInterface => _state.OwningInterface;

        public ComMethodInfo MethodInfo => _state.MethodInfo;

        public IncrementalMethodStubGenerationContext GenerationContext => _state.GenerationContext;

        public bool IsInheritedMethod => OriginalDeclaringInterface != OwningInterface;

        private GeneratedMethodContextBase? _managedToUnmanagedStub;

        public GeneratedMethodContextBase ManagedToUnmanagedStub => _managedToUnmanagedStub ??= CreateManagedToUnmanagedStub();

        private GeneratedMethodContextBase CreateManagedToUnmanagedStub()
        {
            if (GenerationContext.VtableIndexData.Direction is not (MarshalDirection.ManagedToUnmanaged or MarshalDirection.Bidirectional))
            {
                return new SkippedStubContext(OriginalDeclaringInterface.Info.Type);
            }
            var (methodStub, diagnostics) = VirtualMethodPointerStubGenerator.GenerateManagedToNativeStub(GenerationContext, ComInterfaceGeneratorHelpers.GetGeneratorResolver);
            return new GeneratedStubCodeContext(GenerationContext.TypeKeyOwner, GenerationContext.ContainingSyntaxContext, new(methodStub), new(diagnostics));
        }

        private GeneratedMethodContextBase? _unmanagedToManagedStub;

        public GeneratedMethodContextBase UnmanagedToManagedStub => _unmanagedToManagedStub ??= CreateUnmanagedToManagedStub();

        private GeneratedMethodContextBase CreateUnmanagedToManagedStub()
        {
            if (GenerationContext.VtableIndexData.Direction is not (MarshalDirection.UnmanagedToManaged or MarshalDirection.Bidirectional))
            {
                return new SkippedStubContext(GenerationContext.OriginalDefiningType);
            }
            var (methodStub, diagnostics) = VirtualMethodPointerStubGenerator.GenerateNativeToManagedStub(GenerationContext, ComInterfaceGeneratorHelpers.GetGeneratorResolver);
            return new GeneratedStubCodeContext(GenerationContext.OriginalDefiningType, GenerationContext.ContainingSyntaxContext, new(methodStub), new(diagnostics));
        }

        private MethodDeclarationSyntax? _unreachableExceptionStub;

        public MethodDeclarationSyntax UnreachableExceptionStub => _unreachableExceptionStub ??= CreateUnreachableExceptionStub();

        private MethodDeclarationSyntax CreateUnreachableExceptionStub()
        {
            // DeclarationCopiedFromBaseDeclaration(<Arguments>) => throw new UnreachableException();
            return MethodInfo.Syntax
                .WithReturnType(GenerationContext.SignatureContext.StubReturnType)
                .WithModifiers(TokenList())
                .WithAttributeLists(List<AttributeListSyntax>())
                .WithExplicitInterfaceSpecifier(ExplicitInterfaceSpecifier(
                    ParseName(OriginalDeclaringInterface.Info.Type.FullTypeName)))
                .WithParameterList(ParameterList(SeparatedList(GenerationContext.SignatureContext.StubParameters)))
                .WithExpressionBody(ArrowExpressionClause(
                    ThrowExpression(
                        ObjectCreationExpression(
                            TypeSyntaxes.UnreachableException)
                            .WithArgumentList(ArgumentList()))));
        }

        private MethodDeclarationSyntax? _shadow;

        public MethodDeclarationSyntax Shadow => _shadow ??= GenerateShadow();

        private MethodDeclarationSyntax GenerateShadow()
        {
            // DeclarationCopiedFromBaseDeclaration(<Arguments>)
            //    => ((<baseInterfaceType>)this).<MethodName>(<Arguments>);
            var forwarder = new Forwarder();
            return MethodDeclaration(GenerationContext.SignatureContext.StubReturnType, MethodInfo.MethodName)
                .WithModifiers(TokenList(Token(SyntaxKind.NewKeyword)))
                .WithAttributeLists(List(GenerationContext.SignatureContext.AdditionalAttributes.Concat(MethodInfo.Attributes.Select(a => a.GenerateAttributeList()))))
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
        /// Returns a flat list of <see cref="Builder"/> and its owning interface that represents all declared methods and inherited methods.
        /// Guarantees the output will be sorted by order of interface input order, then by vtable order.
        /// </summary>
        public static List<(ComInterfaceContext OwningInterface, Builder Method)> CalculateAllMethods(IEnumerable<(ComInterfaceContext, SequenceEqualImmutableArray<ComMethodInfo>)> ifaceAndDeclaredMethods, CancellationToken _)
        {
            // Optimization : This step technically only needs a single interface inheritance hierarchy.
            // We can calculate all inheritance chains in a previous step and only pass a single inheritance chain to this method.
            // This way, when a single method changes, we would only need to recalculate this for the inheritance chain in which that method exists.

            var ifaceToDeclaredMethodsMap = ifaceAndDeclaredMethods.ToDictionary(static pair => pair.Item1, static pair => pair.Item2);
            var allMethodsCache = new Dictionary<ComInterfaceContext, ImmutableArray<Builder>>();
            var accumulator = new List<(ComInterfaceContext OwningInterface, Builder Method)>();
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
