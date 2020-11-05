using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    internal abstract class ConditionalStackallocMarshallingGenerator : IMarshallingGenerator
    {
        protected static string GetAllocationMarkerIdentifier(string managedIdentifier) => $"{managedIdentifier}__allocated";

        private static string GetByteLengthIdentifier(string managedIdentifier) => $"{managedIdentifier}__bytelen";

        private static string GetStackAllocIdentifier(string managedIdentifier) => $"{managedIdentifier}__stackptr";

        protected bool UsesConditionalStackAlloc(TypePositionInfo info, StubCodeContext context)
        {
            return context.CanUseAdditionalTemporaryState
                && context.StackSpaceUsable
                && (!info.IsByRef || info.RefKind == RefKind.In)
                && !info.IsManagedReturnPosition;
        }

        protected bool TryGenerateSetupSyntax(TypePositionInfo info, StubCodeContext context, out StatementSyntax statement)
        {
            statement = EmptyStatement();

            if (!UsesConditionalStackAlloc(info, context))
                return false;

            string allocationMarkerIdentifier = GetAllocationMarkerIdentifier(context.GetIdentifiers(info).managed);

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
            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);
            
            string allocationMarkerIdentifier = GetAllocationMarkerIdentifier(managedIdentifier);
            string byteLenIdentifier = GetByteLengthIdentifier(managedIdentifier);
            string stackAllocPtrIdentifier = GetStackAllocIdentifier(managedIdentifier);
            // <native> = <allocationExpression>;
            var allocationStatement = ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(nativeIdentifier),
                    GenerateAllocationExpression(info, context, Identifier(byteLenIdentifier), out bool allocationRequiresByteLength)));

            // int <byteLenIdentifier> = <byteLengthExpression>;
            var byteLenAssignment = LocalDeclarationStatement(
                VariableDeclaration(
                    PredefinedType(Token(SyntaxKind.IntKeyword)),
                    SingletonSeparatedList<VariableDeclaratorSyntax>(
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
                    BinaryExpression(SyntaxKind.NotEqualsExpression,
                        IdentifierName(managedIdentifier),
                        LiteralExpression(SyntaxKind.NullLiteralExpression)),
                    Block(statements));
                yield break;
            }

            // Code block for stackalloc if number of bytes is below threshold size
            var marshalOnStack = Block(
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
                                            SingletonList<ArrayRankSpecifierSyntax>(
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
            var allocBlock = IfStatement(
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
                BinaryExpression(
                    SyntaxKind.EqualsExpression,
                    IdentifierName(managedIdentifier),
                    LiteralExpression(SyntaxKind.NullLiteralExpression)),
                Block(
                    ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(nativeIdentifier),
                            LiteralExpression(SyntaxKind.NullLiteralExpression)))),
                ElseClause(Block(byteLenAssignment, allocBlock)));
        }

        protected StatementSyntax GenerateConditionalAllocationFreeSyntax(
            TypePositionInfo info,
            StubCodeContext context)
        {
            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);
            string allocationMarkerIdentifier = GetAllocationMarkerIdentifier(managedIdentifier);
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

        /// <inheritdoc/>
        public abstract TypeSyntax AsNativeType(TypePositionInfo info);

        /// <inheritdoc/>
        public abstract ParameterSyntax AsParameter(TypePositionInfo info);

        /// <inheritdoc/>
        public abstract ArgumentSyntax AsArgument(TypePositionInfo info, StubCodeContext context);

        /// <inheritdoc/>
        public abstract IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext context);

        /// <inheritdoc/>
        public abstract bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context);
    }
}