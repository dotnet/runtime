// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

using static Microsoft.Interop.MarshallerHelpers;

namespace Microsoft.Interop
{
    public sealed class Utf8StringMarshaller : ConditionalStackallocMarshallingGenerator
    {
        // [Compat] Equivalent of MAX_PATH on Windows to match built-in system
        // The assumption is file paths are the most common case for marshalling strings,
        // so the threshold for optimized allocation is based on that length.
        private const int StackAllocBytesThreshold = 260;

        // Conversion from a 2-byte 'char' in UTF-16 to bytes in UTF-8 has a maximum of 3 bytes per 'char'
        // Two bytes ('char') in UTF-16 can be either:
        //   - Code point in the Basic Multilingual Plane: all 16 bits are that of the code point
        //   - Part of a pair for a code point in the Supplementary Planes: 10 bits are that of the code point
        // In UTF-8, 3 bytes are need to represent the code point in first and 4 bytes in the second. Thus, the
        // maximum number of bytes per 'char' is 3.
        private const int MaxByteCountPerChar = 3;

        private static readonly TypeSyntax s_nativeType = PointerType(PredefinedType(Token(SyntaxKind.ByteKeyword)));
        private static readonly TypeSyntax s_utf8EncodingType = ParseTypeName("System.Text.Encoding.UTF8");

        public override SignatureBehavior GetNativeSignatureBehavior(TypePositionInfo info)
        {
            return info.IsByRef ? SignatureBehavior.PointerToNativeType : SignatureBehavior.NativeType;
        }

        public override ValueBoundaryBehavior GetValueBoundaryBehavior(TypePositionInfo info, StubCodeContext context)
        {
            return info.IsByRef ? ValueBoundaryBehavior.AddressOfNativeIdentifier : ValueBoundaryBehavior.NativeIdentifier;
        }

        public override TypeSyntax AsNativeType(TypePositionInfo info) => s_nativeType;

        public override IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext context)
        {
            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);
            switch (context.CurrentStage)
            {
                case StubCodeContext.Stage.Setup:
                    if (TryGenerateSetupSyntax(info, context, out StatementSyntax conditionalAllocSetup))
                        yield return conditionalAllocSetup;

                    break;
                case StubCodeContext.Stage.Marshal:
                    if (info.RefKind != RefKind.Out)
                    {
                        foreach (StatementSyntax statement in GenerateConditionalAllocationSyntax(
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
                        // <managedIdentifier> = Marshal.PtrToStringUTF8((IntPtr)<nativeIdentifier>);
                        yield return ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(managedIdentifier),
                                InvocationExpression(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        InteropServicesMarshalType,
                                        IdentifierName("PtrToStringUTF8")),
                                    ArgumentList(SingletonSeparatedList<ArgumentSyntax>(
                                        Argument(
                                            CastExpression(
                                                SystemIntPtrType,
                                                IdentifierName(nativeIdentifier))))))));
                    }
                    break;
                case StubCodeContext.Stage.Cleanup:
                    yield return GenerateConditionalAllocationFreeSyntax(info, context);
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
                StringMarshaller.AllocationExpression(CharEncoding.Utf8, context.GetIdentifiers(info).managed));
        }

        protected override ExpressionSyntax GenerateByteLengthCalculationExpression(TypePositionInfo info, StubCodeContext context)
        {
            // + 1 for number of characters in case left over high surrogate is ?
            // * <MaxByteCountPerChar> (3 for UTF-8)
            // +1 for null terminator
            // int <byteLen> = (<managed>.Length + 1) * 3 + 1;
            return BinaryExpression(
                SyntaxKind.AddExpression,
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
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(MaxByteCountPerChar))),
                LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1)));
        }

        protected override StatementSyntax GenerateStackallocOnlyValueMarshalling(
            TypePositionInfo info,
            StubCodeContext context,
            SyntaxToken byteLengthIdentifier,
            SyntaxToken stackAllocPtrIdentifier)
        {
            return Block(
                // <byteLen> = Encoding.UTF8.GetBytes(<managed>, new Span<byte>(<stackAllocPtr>, <byteLen>));
                ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(byteLengthIdentifier),
                        InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                s_utf8EncodingType,
                                IdentifierName("GetBytes")),
                            ArgumentList(
                                SeparatedList(new ArgumentSyntax[] {
                                    Argument(IdentifierName(context.GetIdentifiers(info).managed)),
                                    Argument(
                                        ObjectCreationExpression(
                                            GenericName(Identifier(TypeNames.System_Span),
                                                TypeArgumentList(SingletonSeparatedList<TypeSyntax>(
                                                    PredefinedType(Token(SyntaxKind.ByteKeyword))))),
                                            ArgumentList(
                                                SeparatedList(new ArgumentSyntax[]{
                                                    Argument(IdentifierName(stackAllocPtrIdentifier)),
                                                    Argument(IdentifierName(byteLengthIdentifier))})),
                                            initializer: null))}))))),
                // <stackAllocPtr>[<byteLen>] = 0;
                ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        ElementAccessExpression(
                            IdentifierName(stackAllocPtrIdentifier),
                            BracketedArgumentList(
                                SingletonSeparatedList<ArgumentSyntax>(
                                    Argument(IdentifierName(byteLengthIdentifier))))),
                        LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0)))));
        }

        protected override ExpressionSyntax GenerateFreeExpression(
            TypePositionInfo info,
            StubCodeContext context)
        {
            return StringMarshaller.FreeExpression(context.GetIdentifiers(info).native);
        }
    }
}
