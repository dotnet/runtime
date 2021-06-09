using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

using static Microsoft.Interop.MarshallerHelpers;

namespace Microsoft.Interop
{
    internal sealed class Utf16StringMarshaller : ConditionalStackallocMarshallingGenerator
    {
        // [Compat] Equivalent of MAX_PATH on Windows to match built-in system
        // The assumption is file paths are the most common case for marshalling strings,
        // so the threshold for optimized allocation is based on that length.
        private const int StackAllocBytesThreshold = 260 * sizeof(ushort);

        private static readonly TypeSyntax NativeType = PointerType(PredefinedType(Token(SyntaxKind.UShortKeyword)));

        private static string PinnedIdentifier(string nativeIdentifier) => $"{nativeIdentifier}__pinned";

        public override ArgumentSyntax AsArgument(TypePositionInfo info, StubCodeContext context)
        {
            string identifier = context.GetIdentifiers(info).native;
            if (info.IsByRef)
            {
                // &<nativeIdentifier>
                return Argument(
                    PrefixUnaryExpression(
                        SyntaxKind.AddressOfExpression,
                        IdentifierName(identifier)));
            }
            else if (context.PinningSupported)
            {
                // (ushort*)<pinned>
                return Argument(
                    CastExpression(
                        AsNativeType(info),
                        IdentifierName(PinnedIdentifier(identifier))));
            }

            // <nativeIdentifier>
            return Argument(IdentifierName(identifier));
        }

        public override TypeSyntax AsNativeType(TypePositionInfo info)
        {
            // ushort*
            return NativeType;
        }

        public override ParameterSyntax AsParameter(TypePositionInfo info)
        {
            // ushort**
            // or
            // ushort*
            var type = info.IsByRef
                ? PointerType(AsNativeType(info))
                : AsNativeType(info);
            return Parameter(Identifier(info.InstanceIdentifier))
                .WithType(type);
        }

        public override IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext context)
        {
            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);
            if (context.PinningSupported && !info.IsByRef && !info.IsManagedReturnPosition)
            {
                if (context.CurrentStage == StubCodeContext.Stage.Pin)
                {
                    // fixed (char* <pinned> = <managedIdentifier>)
                    yield return FixedStatement(
                        VariableDeclaration(
                            PointerType(PredefinedType(Token(SyntaxKind.CharKeyword))),
                            SingletonSeparatedList(
                                VariableDeclarator(Identifier(PinnedIdentifier(nativeIdentifier)))
                                    .WithInitializer(EqualsValueClause(IdentifierName(managedIdentifier))))),
                        EmptyStatement());
                }

                yield break;
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
                    }
                    break;
                case StubCodeContext.Stage.Unmarshal:
                    if (info.IsManagedReturnPosition || (info.IsByRef && info.RefKind != RefKind.In))
                    {
                        // <managed> = <native> == null ? null : new string((char*)<native>);
                        yield return ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(managedIdentifier),
                                ConditionalExpression(
                                    BinaryExpression(
                                        SyntaxKind.EqualsExpression,
                                        IdentifierName(nativeIdentifier),
                                        LiteralExpression(SyntaxKind.DefaultLiteralExpression)),
                                    LiteralExpression(SyntaxKind.NullLiteralExpression),
                                    ObjectCreationExpression(
                                        PredefinedType(Token(SyntaxKind.StringKeyword)),
                                        ArgumentList(SingletonSeparatedList<ArgumentSyntax>(
                                            Argument(
                                                CastExpression(
                                                    PointerType(PredefinedType(Token(SyntaxKind.CharKeyword))),
                                                    IdentifierName(nativeIdentifier))))),
                                        initializer: null))));
                    }
                    break;
                case StubCodeContext.Stage.Cleanup:
                    yield return GenerateConditionalAllocationFreeSyntax(info ,context);

                    break;
            }
        }

        public override bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => true;
        
        public override bool SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, StubCodeContext context) => false;
        
        protected override ExpressionSyntax GenerateAllocationExpression(
            TypePositionInfo info,
            StubCodeContext context,
            SyntaxToken byteLengthIdentifier,
            out bool allocationRequiresByteLength)
        {
            allocationRequiresByteLength = false;
            return CastExpression(
                AsNativeType(info),
                StringMarshaller.AllocationExpression(CharEncoding.Utf16, context.GetIdentifiers(info).managed));
        }

        protected override ExpressionSyntax GenerateByteLengthCalculationExpression(TypePositionInfo info, StubCodeContext context)
        {
            // +1 for null terminator
            // *2 for number of bytes per char
            // int <byteLen> = (<managed>.Length + 1) * 2;
            return 
                BinaryExpression(
                    SyntaxKind.MultiplyExpression,
                    ParenthesizedExpression(
                        BinaryExpression(
                            SyntaxKind.AddExpression,
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName(context.GetIdentifiers(info).managed),
                                IdentifierName("Length")),
                            LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1)))),
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(2)));
        }

        protected override StatementSyntax GenerateStackallocOnlyValueMarshalling(
            TypePositionInfo info,
            StubCodeContext context,
            SyntaxToken byteLengthIdentifier,
            SyntaxToken stackAllocPtrIdentifier)
        {
            // ((ReadOnlySpan<char>)<managed>).CopyTo(new Span<char>(<stackAllocPtr>, <managed>.Length + 1));
            return                                 
                ExpressionStatement(
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            ParenthesizedExpression(
                            CastExpression(
                                GenericName(Identifier("System.ReadOnlySpan"),
                                    TypeArgumentList(SingletonSeparatedList<TypeSyntax>(
                                        PredefinedType(Token(SyntaxKind.CharKeyword))))),
                                IdentifierName(context.GetIdentifiers(info).managed))),
                            IdentifierName("CopyTo")),
                        ArgumentList(
                            SeparatedList(new [] {
                                Argument(
                                    ObjectCreationExpression(
                                        GenericName(Identifier("System.Span"),
                                            TypeArgumentList(SingletonSeparatedList<TypeSyntax>(
                                                PredefinedType(Token(SyntaxKind.CharKeyword))))),
                                        ArgumentList(
                                            SeparatedList(new []{
                                                Argument(IdentifierName(stackAllocPtrIdentifier)),
                                                Argument(IdentifierName(byteLengthIdentifier))})),
                                        initializer: null))}))));
        }

        protected override ExpressionSyntax GenerateFreeExpression(
            TypePositionInfo info,
            StubCodeContext context)
        {
            return StringMarshaller.FreeExpression(context.GetIdentifiers(info).native);
        }
    }
}
