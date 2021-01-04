using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    internal class BlittableArrayMarshaller : ConditionalStackallocMarshallingGenerator
    {
        /// <summary>
        /// Stack-alloc threshold set to 256 bytes to enable small arrays to be passed on the stack.
        /// Number kept small to ensure that P/Invokes with a lot of small array parameters doesn't
        /// blow the stack since this is a new optimization in the code-generated interop.
        /// </summary>
        private const int StackAllocBytesThreshold = 0x200;
        private readonly ExpressionSyntax _numElementsExpr;

        public BlittableArrayMarshaller(ExpressionSyntax numElementsExpr)
        {
            _numElementsExpr = numElementsExpr;
        }

        private TypeSyntax GetElementTypeSyntax(TypePositionInfo info)
        {
            return ((IArrayTypeSymbol)info.ManagedType).ElementType.AsTypeSyntax();
        }

        public override TypeSyntax AsNativeType(TypePositionInfo info)
        {
            return PointerType(GetElementTypeSyntax(info));
        }

        public override ParameterSyntax AsParameter(TypePositionInfo info)
        {
            var type = info.IsByRef
                ? PointerType(AsNativeType(info))
                : AsNativeType(info);
            return Parameter(Identifier(info.InstanceIdentifier))
                .WithType(type);
        }

        public override ArgumentSyntax AsArgument(TypePositionInfo info, StubCodeContext context)
        {
            return info.IsByRef
                ? Argument(
                    PrefixUnaryExpression(
                        SyntaxKind.AddressOfExpression,
                        IdentifierName(context.GetIdentifiers(info).native)))
                : Argument(IdentifierName(context.GetIdentifiers(info).native));
        }

        public override IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext context)
        {
            var (managedIdentifer, nativeIdentifier) = context.GetIdentifiers(info);
            if (!info.IsByRef && !info.IsManagedReturnPosition && context.PinningSupported)
            {
                if (context.CurrentStage == StubCodeContext.Stage.Pin)
                {
                    // fixed (<nativeType> <nativeIdentifier> = &MemoryMarshal.GetArrayDataReference(<managedIdentifer>))
                    yield return FixedStatement(
                        VariableDeclaration(AsNativeType(info), SingletonSeparatedList(
                            VariableDeclarator(nativeIdentifier)
                                .WithInitializer(EqualsValueClause(
                                    PrefixUnaryExpression(SyntaxKind.AddressOfExpression,
                                        InvocationExpression(
                                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                ParseTypeName(TypeNames.System_Runtime_InteropServices_MemoryMarshal),
                                                IdentifierName("GetArrayDataReference")),
                                                ArgumentList(
                                                    SingletonSeparatedList(Argument(IdentifierName(managedIdentifer)))
                                                ))))))),
                        EmptyStatement());
                }
                yield break;
            }
            
            TypeSyntax spanElementTypeSyntax = GetElementTypeSyntax(info);
            if (spanElementTypeSyntax is PointerTypeSyntax)
            {
                // Pointers cannot be passed to generics, so use IntPtr for this case.
                spanElementTypeSyntax = ParseTypeName("System.IntPtr");
            }

            switch (context.CurrentStage)
            {
                case StubCodeContext.Stage.Setup:
                    if (TryGenerateSetupSyntax(info, context, out StatementSyntax conditionalAllocSetup))
                        yield return conditionalAllocSetup;

                    break;
                case StubCodeContext.Stage.Marshal:
                    if (info.RefKind != RefKind.Out)
                    {
                        foreach (var statement in GenerateConditionalAllocationSyntax(
                            info,
                            context,
                            StackAllocBytesThreshold))
                        {
                            yield return statement;
                        }

                        // new Span<T>(nativeIdentifier, managedIdentifier.Length)
                        var nativeSpan = ObjectCreationExpression(
                            GenericName(TypeNames.System_Span)
                            .WithTypeArgumentList(
                                TypeArgumentList(
                                    SingletonSeparatedList(spanElementTypeSyntax))))
                        .WithArgumentList(
                            ArgumentList(
                                SeparatedList(
                                    new []{
                                        Argument(
                                            CastExpression(
                                                PointerType(spanElementTypeSyntax),
                                                IdentifierName(nativeIdentifier))),
                                        Argument(
                                            MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                IdentifierName(managedIdentifer),
                                                IdentifierName("Length")))
                                    })));

                        // new Span<T>(managedIdentifier).CopyTo(<nativeSpan>);
                        yield return IfStatement(
                                BinaryExpression(SyntaxKind.NotEqualsExpression,
                                    IdentifierName(managedIdentifer),
                                    LiteralExpression(SyntaxKind.NullLiteralExpression)),
                                ExpressionStatement(
                                    InvocationExpression(
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                                ObjectCreationExpression(
                                                                GenericName(Identifier(TypeNames.System_Span),
                                                                    TypeArgumentList(
                                                                        SingletonSeparatedList(
                                                                            spanElementTypeSyntax))))
                                                            .WithArgumentList(
                                                                ArgumentList(SingletonSeparatedList(
                                                                    Argument(IdentifierName(managedIdentifer))))),
                                            IdentifierName("CopyTo")))
                                    .WithArgumentList(
                                        ArgumentList(
                                            SingletonSeparatedList(
                                                Argument(nativeSpan))))));
                    }
                    break;
                case StubCodeContext.Stage.Unmarshal:
                    if (info.IsManagedReturnPosition
                        || (info.IsByRef && info.RefKind != RefKind.In)
                        || info.ByValueContentsMarshalKind.HasFlag(ByValueContentsMarshalKind.Out))
                    {
                        // new Span<T>(nativeIdentifier, managedIdentifier.Length).CopyTo(managedIdentifier);
                        var unmarshalContentsStatement =
                            ExpressionStatement(
                                InvocationExpression(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        ObjectCreationExpression(
                                                    GenericName(Identifier(TypeNames.System_Span),
                                                        TypeArgumentList(
                                                            SingletonSeparatedList(
                                                                spanElementTypeSyntax))))
                                                .WithArgumentList(
                                                    ArgumentList(
                                                        SeparatedList(
                                                            new[]{
                                                                Argument(CastExpression(
                                                                    PointerType(spanElementTypeSyntax),
                                                                    IdentifierName(nativeIdentifier))),
                                                                Argument(
                                                                    MemberAccessExpression(
                                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                                        IdentifierName(managedIdentifer),
                                                                        IdentifierName("Length")))
                                                            }))),
                                        IdentifierName("CopyTo")))
                                .WithArgumentList(
                                    ArgumentList(
                                        SingletonSeparatedList(
                                            Argument(IdentifierName(managedIdentifer))))));

                        if (info.IsManagedReturnPosition || info.IsByRef)
                        {
                            yield return IfStatement(
                                BinaryExpression(SyntaxKind.NotEqualsExpression,
                                IdentifierName(nativeIdentifier),
                                LiteralExpression(SyntaxKind.NullLiteralExpression)),
                                Block(
                                    // <managedIdentifier> = new <managedElementType>[<numElementsExpression>];
                                    ExpressionStatement(
                                        AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                            IdentifierName(managedIdentifer),
                                            ArrayCreationExpression(
                                            ArrayType(GetElementTypeSyntax(info),
                                                SingletonList(ArrayRankSpecifier(
                                                    SingletonSeparatedList(_numElementsExpr))))))),
                                    unmarshalContentsStatement),
                                ElseClause(
                                    ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                        IdentifierName(managedIdentifer),
                                        LiteralExpression(SyntaxKind.NullLiteralExpression)))));
                        }
                        else
                        {
                            yield return IfStatement(
                                BinaryExpression(SyntaxKind.NotEqualsExpression,
                                    IdentifierName(managedIdentifer),
                                    LiteralExpression(SyntaxKind.NullLiteralExpression)),
                                unmarshalContentsStatement);
                        }

                    }
                    break;
                case StubCodeContext.Stage.Cleanup:
                    yield return GenerateConditionalAllocationFreeSyntax(info, context);
                    break;
            }
        }

        public override bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context)
        {
            return (info.IsByRef || info.IsManagedReturnPosition) || !context.PinningSupported;
        }

        protected override ExpressionSyntax GenerateAllocationExpression(TypePositionInfo info, StubCodeContext context, SyntaxToken byteLengthIdentifier, out bool allocationRequiresByteLength)
        {
            allocationRequiresByteLength = true;
            // (<nativeType>)Marshal.AllocCoTaskMem(<byteLengthIdentifier>)
            return CastExpression(AsNativeType(info),
                InvocationExpression(
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        ParseTypeName(TypeNames.System_Runtime_InteropServices_Marshal),
                        IdentifierName("AllocCoTaskMem")),
                    ArgumentList(SingletonSeparatedList(Argument(IdentifierName(byteLengthIdentifier))))));
        }

        protected override ExpressionSyntax GenerateByteLengthCalculationExpression(TypePositionInfo info, StubCodeContext context)
        {
            // checked(sizeof(<nativeElementType>) * <managedIdentifier>.Length)
            return CheckedExpression(SyntaxKind.CheckedExpression,
                BinaryExpression(SyntaxKind.MultiplyExpression,
                    SizeOfExpression(GetElementTypeSyntax(info)),
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(context.GetIdentifiers(info).managed),
                        IdentifierName("Length"))));
        }

        protected override StatementSyntax GenerateStackallocOnlyValueMarshalling(TypePositionInfo info, StubCodeContext context, SyntaxToken byteLengthIdentifier, SyntaxToken stackAllocPtrIdentifier)
        {
            return EmptyStatement();
        }

        protected override ExpressionSyntax GenerateFreeExpression(TypePositionInfo info, StubCodeContext context)
        {
            // Marshal.FreeCoTaskMem((IntPtr)<nativeIdentifier>)
            return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    ParseTypeName(TypeNames.System_Runtime_InteropServices_Marshal),
                    IdentifierName("FreeCoTaskMem")),
                ArgumentList(SingletonSeparatedList(
                    Argument(
                        CastExpression(
                            ParseTypeName("System.IntPtr"),
                            IdentifierName(context.GetIdentifiers(info).native))))));
        }

        public override bool SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, StubCodeContext context)
        {
            return !context.PinningSupported && marshalKind.HasFlag(ByValueContentsMarshalKind.Out);
        }
    }

}
