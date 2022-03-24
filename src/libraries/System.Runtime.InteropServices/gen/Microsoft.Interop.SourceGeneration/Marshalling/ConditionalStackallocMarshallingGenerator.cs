// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    public abstract class ConditionalStackallocMarshallingGenerator : IMarshallingGenerator
    {
        protected static string GetAllocationMarkerIdentifier(TypePositionInfo info, StubCodeContext context) => context.GetAdditionalIdentifier(info, "allocated");

        private static string GetByteLengthIdentifier(TypePositionInfo info, StubCodeContext context) => context.GetAdditionalIdentifier(info, "bytelen");

        private static string GetStackAllocIdentifier(TypePositionInfo info, StubCodeContext context) => context.GetAdditionalIdentifier(info, "stackptr");

        protected static bool UsesConditionalStackAlloc(TypePositionInfo info, StubCodeContext context)
        {
            return context.SingleFrameSpansNativeContext
                && (!info.IsByRef || info.RefKind == RefKind.In)
                && !info.IsManagedReturnPosition
                && context.AdditionalTemporaryStateLivesAcrossStages;
        }

        protected static bool TryGenerateSetupSyntax(TypePositionInfo info, StubCodeContext context, out StatementSyntax statement)
        {
            statement = EmptyStatement();

            if (!UsesConditionalStackAlloc(info, context))
                return false;

            string allocationMarkerIdentifier = GetAllocationMarkerIdentifier(info, context);

            // bool <allocationMarker> = false;
            statement = LocalDeclarationStatement(
                VariableDeclaration(
                    PredefinedType(Token(SyntaxKind.BoolKeyword)),
                    SingletonSeparatedList(
                        VariableDeclarator(allocationMarkerIdentifier)
                            .WithInitializer(EqualsValueClause(LiteralExpression(SyntaxKind.FalseLiteralExpression))))));
            return true;
        }

        protected IEnumerable<StatementSyntax> GenerateConditionalAllocationSyntax(
            TypePositionInfo info,
            StubCodeContext context,
            int stackallocMaxSize)
        {
            (_, string nativeIdentifier) = context.GetIdentifiers(info);

            string allocationMarkerIdentifier = GetAllocationMarkerIdentifier(info, context);
            string byteLenIdentifier = GetByteLengthIdentifier(info, context);
            string stackAllocPtrIdentifier = GetStackAllocIdentifier(info, context);
            // <native> = <allocationExpression>;
            ExpressionStatementSyntax allocationStatement = ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(nativeIdentifier),
                    GenerateAllocationExpression(info, context, Identifier(byteLenIdentifier), out bool allocationRequiresByteLength)));

            // int <byteLenIdentifier> = <byteLengthExpression>;
            LocalDeclarationStatementSyntax byteLenAssignment = LocalDeclarationStatement(
                VariableDeclaration(
                    PredefinedType(Token(SyntaxKind.IntKeyword)),
                    SingletonSeparatedList(
                        VariableDeclarator(byteLenIdentifier)
                            .WithInitializer(EqualsValueClause(
                                GenerateByteLengthCalculationExpression(info, context))))));

            if (!UsesConditionalStackAlloc(info, context))
            {
                List<StatementSyntax> statements = new List<StatementSyntax>();
                if (allocationRequiresByteLength)
                {
                    statements.Add(byteLenAssignment);
                }
                statements.Add(allocationStatement);
                yield return ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(nativeIdentifier),
                    LiteralExpression(SyntaxKind.NullLiteralExpression)));
                yield return IfStatement(
                    GenerateNullCheckExpression(info, context),
                    Block(statements));
                yield break;
            }

            // Code block for stackalloc if number of bytes is below threshold size
            BlockSyntax marshalOnStack = Block(
                // byte* <stackAllocPtr> = stackalloc byte[<byteLen>];
                LocalDeclarationStatement(
                    VariableDeclaration(
                        PointerType(PredefinedType(Token(SyntaxKind.ByteKeyword))),
                        SingletonSeparatedList(
                            VariableDeclarator(stackAllocPtrIdentifier)
                                .WithInitializer(EqualsValueClause(
                                    StackAllocArrayCreationExpression(
                                        ArrayType(
                                            PredefinedType(Token(SyntaxKind.ByteKeyword)),
                                            SingletonList(
                                                ArrayRankSpecifier(SingletonSeparatedList<ExpressionSyntax>(
                                                    IdentifierName(byteLenIdentifier))))))))))),
                GenerateStackallocOnlyValueMarshalling(info, context, Identifier(byteLenIdentifier), Identifier(stackAllocPtrIdentifier)),
                // <nativeIdentifier> = <stackAllocPtr>;
                ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(nativeIdentifier),
                        CastExpression(
                            AsNativeType(info),
                            IdentifierName(stackAllocPtrIdentifier)))));

            //   if (<byteLen> > <StackAllocBytesThreshold>)
            //   {
            //       <allocationStatement>;
            //       <allocationMarker> = true;
            //   }
            //   else
            //   {
            //       byte* <stackAllocPtr> = stackalloc byte[<byteLen>];
            //       <marshalValueOnStackStatement>;
            //       <native> = (<nativeType>)<stackAllocPtr>;
            //   }
            IfStatementSyntax allocBlock = IfStatement(
                BinaryExpression(
                    SyntaxKind.GreaterThanExpression,
                    IdentifierName(byteLenIdentifier),
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(stackallocMaxSize))),
                Block(
                    allocationStatement,
                    ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(allocationMarkerIdentifier),
                            LiteralExpression(SyntaxKind.TrueLiteralExpression)))),
                ElseClause(marshalOnStack));

            yield return IfStatement(
                GenerateNullCheckExpression(info, context),
                Block(byteLenAssignment, allocBlock));
        }

        protected StatementSyntax GenerateConditionalAllocationFreeSyntax(
            TypePositionInfo info,
            StubCodeContext context)
        {
            string allocationMarkerIdentifier = GetAllocationMarkerIdentifier(info, context);
            if (!UsesConditionalStackAlloc(info, context))
            {
                return ExpressionStatement(GenerateFreeExpression(info, context));
            }
            else
            {
                // if (<allocationMarkerIdentifier>)
                // {
                //     <freeExpression>;
                // }
                return IfStatement(
                    IdentifierName(allocationMarkerIdentifier),
                    Block(ExpressionStatement(GenerateFreeExpression(info, context))));
            }
        }

        /// <summary>
        /// Generate an expression that allocates memory for the native representation of the object.
        /// </summary>
        /// <param name="info">Object to marshal</param>
        /// <param name="context">Code generation context</param>
        /// <param name="byteLengthIdentifier">An identifier that represents how many bytes must be allocated.</param>
        /// <param name="allocationRequiresByteLength">If the allocation expression uses <paramref name="byteLengthIdentifier"/>, true; otherwise false.</param>
        /// <returns>An expression that allocates memory for the native representation of the object.</returns>
        protected abstract ExpressionSyntax GenerateAllocationExpression(
            TypePositionInfo info,
            StubCodeContext context,
            SyntaxToken byteLengthIdentifier,
            out bool allocationRequiresByteLength);

        /// <summary>
        /// Generates an expression that represents the number of bytes that need to be allocated.
        /// </summary>
        /// <param name="info">Object to marshal</param>
        /// <param name="context">Code generation context</param>
        /// <returns>An expression that results in the number of bytes to allocate as a C# int.</returns>
        protected abstract ExpressionSyntax GenerateByteLengthCalculationExpression(
            TypePositionInfo info,
            StubCodeContext context);

        /// <summary>
        /// Generate a statement that is only executed when memory is stack allocated.
        /// </summary>
        /// <param name="info">Object to marshal</param>
        /// <param name="context">Code generation context</param>
        /// <param name="byteLengthIdentifier">An identifier that represents the number of bytes allocated.</param>
        /// <param name="stackAllocPtrIdentifier">An identifier that represents a pointer to the stack allocated memory (of type byte*).</param>
        /// <returns>A statement that is only executed when memory is stack allocated.</returns>
        protected abstract StatementSyntax GenerateStackallocOnlyValueMarshalling(
            TypePositionInfo info,
            StubCodeContext context,
            SyntaxToken byteLengthIdentifier,
            SyntaxToken stackAllocPtrIdentifier);

        /// <summary>
        /// Generate code to free native allocated memory used during marshalling.
        /// </summary>
        /// <param name="info">Object to marshal</param>
        /// <param name="context">Code generation context</param>
        /// <returns>An expression that frees allocated memory.</returns>
        protected abstract ExpressionSyntax GenerateFreeExpression(
            TypePositionInfo info,
            StubCodeContext context);

        /// <summary>
        /// Generate code to check if the managed value is not null.
        /// </summary>
        /// <param name="info">Object to marshal</param>
        /// <param name="context">Code generation context</param>
        /// <returns>An expression that checks if the managed value is not null.</returns>
        protected virtual ExpressionSyntax GenerateNullCheckExpression(
            TypePositionInfo info,
            StubCodeContext context)
        {
            return BinaryExpression(
                    SyntaxKind.NotEqualsExpression,
                    IdentifierName(context.GetIdentifiers(info).managed),
                    LiteralExpression(SyntaxKind.NullLiteralExpression));
        }

        /// <inheritdoc/>
        public virtual bool IsSupported(TargetFramework target, Version version)
        {
            return target is TargetFramework.Net && version.Major >= 6;
        }

        /// <inheritdoc/>
        public abstract TypeSyntax AsNativeType(TypePositionInfo info);

        /// <inheritdoc/>
        public abstract SignatureBehavior GetNativeSignatureBehavior(TypePositionInfo info);

        /// <inheritdoc/>
        public abstract ValueBoundaryBehavior GetValueBoundaryBehavior(TypePositionInfo info, StubCodeContext context);

        /// <inheritdoc/>
        public abstract IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext context);

        /// <inheritdoc/>
        public abstract bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context);

        /// <inheritdoc />
        public abstract bool SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, StubCodeContext context);
    }
}
