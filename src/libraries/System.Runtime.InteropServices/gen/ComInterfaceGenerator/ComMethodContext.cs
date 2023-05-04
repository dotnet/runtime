// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
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
        /// </summary>
        private sealed record ComMethodContext(ComInterfaceContext DeclaringInterface, ComMethodInfo MethodInfo, int Index, IncrementalMethodStubGenerationContext GenerationContext)
        {
            public GeneratedMethodContextBase ManagedToUnmanagedStub
            {
                get
                {
                    if (GenerationContext.VtableIndexData.Direction is not (MarshalDirection.ManagedToUnmanaged or MarshalDirection.Bidirectional))
                    {
                        return (GeneratedMethodContextBase)new SkippedStubContext(DeclaringInterface.Info.Type);
                    }
                    var (methodStub, diagnostics) = VirtualMethodPointerStubGenerator.GenerateManagedToNativeStub(GenerationContext);
                    return new GeneratedStubCodeContext(GenerationContext.TypeKeyOwner, GenerationContext.ContainingSyntaxContext, new(methodStub), new(diagnostics));
                }
            }

            public Diagnostic? Diagnostic => MethodInfo.Diagnostic;

            public GeneratedMethodContextBase NativeToManagedStub
            {
                get
                {
                    if (GenerationContext.VtableIndexData.Direction is not (MarshalDirection.UnmanagedToManaged or MarshalDirection.Bidirectional))
                    {
                        return (GeneratedMethodContextBase)new SkippedStubContext(GenerationContext.OriginalDefiningType);
                    }
                    var (methodStub, diagnostics) = VirtualMethodPointerStubGenerator.GenerateNativeToManagedStub(GenerationContext);
                    return new GeneratedStubCodeContext(GenerationContext.OriginalDefiningType, GenerationContext.ContainingSyntaxContext, new(methodStub), new(diagnostics));
                }
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
        }
    }
}
