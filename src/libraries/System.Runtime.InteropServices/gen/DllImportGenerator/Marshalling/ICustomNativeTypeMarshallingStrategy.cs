using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    /// <summary>
    /// The base interface for implementing various different aspects of the custom native type and collection marshalling specs.
    /// </summary>
    interface ICustomNativeTypeMarshallingStrategy
    {
        TypeSyntax AsNativeType(TypePositionInfo info);

        ArgumentSyntax AsArgument(TypePositionInfo info, StubCodeContext context);

        IEnumerable<ArgumentSyntax> GetNativeTypeConstructorArguments(TypePositionInfo info, StubCodeContext context);

        IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context, IEnumerable<ArgumentSyntax> nativeTypeConstructorArguments);

        IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context);

        IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context);

        IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubCodeContext context);

        IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubCodeContext context);

        bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context);
    }

    /// <summary>
    /// Marshalling support for a type that has a custom native type.
    /// </summary>
    internal sealed class SimpleCustomNativeTypeMarshalling : ICustomNativeTypeMarshallingStrategy
    {
        private readonly TypeSyntax nativeTypeSyntax;

        public SimpleCustomNativeTypeMarshalling(TypeSyntax nativeTypeSyntax)
        {
            this.nativeTypeSyntax = nativeTypeSyntax;
        }

        public ArgumentSyntax AsArgument(TypePositionInfo info, StubCodeContext context)
        {
            string identifier = context.GetIdentifiers(info).native;
            if (info.IsByRef)
            {
                return Argument(
                    PrefixUnaryExpression(
                        SyntaxKind.AddressOfExpression,
                        IdentifierName(identifier)));
            }

            return Argument(IdentifierName(identifier));
        }

        public TypeSyntax AsNativeType(TypePositionInfo info)
        {
            return nativeTypeSyntax;
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context)
        {
            return true;
        }

        public IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context)
        {
            return Array.Empty<StatementSyntax>();
        }

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context, IEnumerable<ArgumentSyntax> nativeTypeConstructorArguments)
        {
            // <nativeIdentifier> = new(<arguments>);
            yield return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(context.GetIdentifiers(info).native),
                    ImplicitObjectCreationExpression()
                        .WithArgumentList(ArgumentList(SeparatedList(nativeTypeConstructorArguments)))));
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            // If the current element is being marshalled by-value [Out], then don't call the ToManaged method and do the assignment.
            // The assignment will end up being a no-op and will not be observed.
            if (!info.IsByRef && info.ByValueContentsMarshalKind.HasFlag(ByValueContentsMarshalKind.Out))
            {
                yield break;
            }

            var (managedIdentifier, nativeIdentifier) = context.GetIdentifiers(info);
            // <managedIdentifier> = <marshalerIdentifier>.ToManaged();
            yield return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(managedIdentifier),
                    InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName(nativeIdentifier),
                            IdentifierName(ManualTypeMarshallingHelper.ToManagedMethodName)))));
        }

        public IEnumerable<ArgumentSyntax> GetNativeTypeConstructorArguments(TypePositionInfo info, StubCodeContext context)
        {
            yield return Argument(IdentifierName(context.GetIdentifiers(info).managed));
        }

        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context)
        {
            return Array.Empty<StatementSyntax>();
        }

        public IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubCodeContext context)
        {
            return Array.Empty<StatementSyntax>();
        }
    }

    /// <summary>
    /// A context that redefines the 'native' identifier for a TypePositionInfo to be the marshaller identifier.
    /// </summary>
    internal class CustomNativeTypeWithValuePropertyStubContext : StubCodeContext
    {
        private readonly StubCodeContext parentContext;

        public CustomNativeTypeWithValuePropertyStubContext(StubCodeContext parentContext)
        {
            this.parentContext = parentContext;
            CurrentStage = parentContext.CurrentStage;
        }

        public override bool PinningSupported => parentContext.PinningSupported;

        public override bool StackSpaceUsable => parentContext.StackSpaceUsable;

        public override bool CanUseAdditionalTemporaryState => parentContext.CanUseAdditionalTemporaryState;

        public override TypePositionInfo? GetTypePositionInfoForManagedIndex(int index)
        {
            return parentContext.GetTypePositionInfoForManagedIndex(index);
        }

        public override (string managed, string native) GetIdentifiers(TypePositionInfo info)
        {
            return (parentContext.GetIdentifiers(info).managed, MarshallerHelpers.GetMarshallerIdentifier(info, parentContext));
        }
    }

    /// <summary>
    /// Marshaller that enables support of a Value property on a native type.
    /// </summary>
    internal sealed class CustomNativeTypeWithValuePropertyMarshalling : ICustomNativeTypeMarshallingStrategy
    {
        private readonly ICustomNativeTypeMarshallingStrategy innerMarshaller;
        private readonly TypeSyntax valuePropertyType;

        public CustomNativeTypeWithValuePropertyMarshalling(ICustomNativeTypeMarshallingStrategy innerMarshaller, TypeSyntax valuePropertyType)
        {
            this.innerMarshaller = innerMarshaller;
            this.valuePropertyType = valuePropertyType;
        }

        public ArgumentSyntax AsArgument(TypePositionInfo info, StubCodeContext context)
        {
            string identifier = context.GetIdentifiers(info).native;
            if (info.IsByRef)
            {
                return Argument(
                    PrefixUnaryExpression(
                        SyntaxKind.AddressOfExpression,
                        IdentifierName(identifier)));
            }

            return Argument(IdentifierName(identifier));
        }

        public TypeSyntax AsNativeType(TypePositionInfo info)
        {
            return valuePropertyType;
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context)
        {
            return true;
        }

        public IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context)
        {
            var subContext = new CustomNativeTypeWithValuePropertyStubContext(context);
            return innerMarshaller.GenerateCleanupStatements(info, subContext);
        }

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context, IEnumerable<ArgumentSyntax> nativeTypeConstructorArguments)
        {
            var subContext = new CustomNativeTypeWithValuePropertyStubContext(context);
            foreach (var statement in innerMarshaller.GenerateMarshalStatements(info, subContext, nativeTypeConstructorArguments))
            {
                yield return statement;
            }

            // <nativeIdentifier> = <marshalerIdentifier>.Value;
            yield return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(context.GetIdentifiers(info).native),
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(subContext.GetIdentifiers(info).native),
                        IdentifierName(ManualTypeMarshallingHelper.ValuePropertyName))));
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            var subContext = new CustomNativeTypeWithValuePropertyStubContext(context);

            // <marshalerIdentifier>.Value = <nativeIdentifier>;
            yield return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(subContext.GetIdentifiers(info).native),
                        IdentifierName(ManualTypeMarshallingHelper.ValuePropertyName)),
                    IdentifierName(context.GetIdentifiers(info).native)));

            foreach (var statement in innerMarshaller.GenerateUnmarshalStatements(info, subContext))
            {
                yield return statement;
            }
        }

        public IEnumerable<ArgumentSyntax> GetNativeTypeConstructorArguments(TypePositionInfo info, StubCodeContext context)
        {
            var subContext = new CustomNativeTypeWithValuePropertyStubContext(context);
            return innerMarshaller.GetNativeTypeConstructorArguments(info, subContext);
        }

        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context)
        {
            var subContext = new CustomNativeTypeWithValuePropertyStubContext(context);
            yield return LocalDeclarationStatement(
                VariableDeclaration(
                    innerMarshaller.AsNativeType(info),
                    SingletonSeparatedList(
                        VariableDeclarator(subContext.GetIdentifiers(info).native)
                        .WithInitializer(EqualsValueClause(LiteralExpression(SyntaxKind.DefaultLiteralExpression))))));

            foreach (var statement in innerMarshaller.GenerateSetupStatements(info, subContext))
            {
                yield return statement;
            }
        }

        public IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubCodeContext context)
        {
            var subContext = new CustomNativeTypeWithValuePropertyStubContext(context);
            return innerMarshaller.GeneratePinStatements(info, subContext);
        }
    }

    /// <summary>
    /// Marshaller that enables support for a stackalloc constructor variant on a native type.
    /// </summary>
    internal sealed class StackallocOptimizationMarshalling : ICustomNativeTypeMarshallingStrategy
    {
        private readonly ICustomNativeTypeMarshallingStrategy innerMarshaller;

        public StackallocOptimizationMarshalling(ICustomNativeTypeMarshallingStrategy innerMarshaller)
        {
            this.innerMarshaller = innerMarshaller;
        }

        public ArgumentSyntax AsArgument(TypePositionInfo info, StubCodeContext context)
        {
            return innerMarshaller.AsArgument(info, context);
        }

        public TypeSyntax AsNativeType(TypePositionInfo info)
        {
            return innerMarshaller.AsNativeType(info);
        }

        public IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context)
        {
            return innerMarshaller.GenerateCleanupStatements(info, context);
        }

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context, IEnumerable<ArgumentSyntax> nativeTypeConstructorArguments)
        {
            if (StackAllocOptimizationValid(info, context))
            {
                // byte* <managedIdentifier>__stackptr = stackalloc byte[<_nativeLocalType>.StackBufferSize];
                yield return LocalDeclarationStatement(
                VariableDeclaration(
                    PointerType(PredefinedType(Token(SyntaxKind.ByteKeyword))),
                    SingletonSeparatedList(
                        VariableDeclarator(GetStackAllocPointerIdentifier(info, context))
                            .WithInitializer(EqualsValueClause(
                                StackAllocArrayCreationExpression(
                                        ArrayType(
                                            PredefinedType(Token(SyntaxKind.ByteKeyword)),
                                            SingletonList(ArrayRankSpecifier(SingletonSeparatedList<ExpressionSyntax>(
                                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                    AsNativeType(info),
                                                    IdentifierName(ManualTypeMarshallingHelper.StackBufferSizeFieldName))
                                            ))))))))));
            }

            foreach (var statement in innerMarshaller.GenerateMarshalStatements(info, context, nativeTypeConstructorArguments))
            {
                yield return statement;
            }
        }

        private static bool StackAllocOptimizationValid(TypePositionInfo info, StubCodeContext context)
        {
            return context.StackSpaceUsable && context.PinningSupported && (!info.IsByRef || info.RefKind == RefKind.In);
        }

        private static string GetStackAllocPointerIdentifier(TypePositionInfo info, StubCodeContext context)
        {
            return $"{context.GetIdentifiers(info).managed}__stackptr";
        }

        public IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubCodeContext context)
        {
            return innerMarshaller.GeneratePinStatements(info, context);
        }

        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context)
        {
            return innerMarshaller.GenerateSetupStatements(info, context);
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            return innerMarshaller.GenerateUnmarshalStatements(info, context);
        }

        public IEnumerable<ArgumentSyntax> GetNativeTypeConstructorArguments(TypePositionInfo info, StubCodeContext context)
        {
            foreach (var arg in innerMarshaller.GetNativeTypeConstructorArguments(info, context))
            {
                yield return arg;
            }
            if (StackAllocOptimizationValid(info, context))
            {
                yield return Argument(
                    ObjectCreationExpression(
                        GenericName(Identifier(TypeNames.System_Span),
                            TypeArgumentList(SingletonSeparatedList<TypeSyntax>(
                                PredefinedType(Token(SyntaxKind.ByteKeyword))))))
                    .WithArgumentList(
                        ArgumentList(SeparatedList(new ArgumentSyntax[]
                        {
                            Argument(IdentifierName(GetStackAllocPointerIdentifier(info, context))),
                            Argument(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                    AsNativeType(info),
                                    IdentifierName(ManualTypeMarshallingHelper.StackBufferSizeFieldName)))
                        }))));
            }
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context)
        {
            return innerMarshaller.UsesNativeIdentifier(info, context);
        }
    }

    /// <summary>
    /// Marshaller that enables support for a FreeNative method on a native type.
    /// </summary>
    internal sealed class FreeNativeCleanupStrategy : ICustomNativeTypeMarshallingStrategy
    {
        private readonly ICustomNativeTypeMarshallingStrategy innerMarshaller;

        public FreeNativeCleanupStrategy(ICustomNativeTypeMarshallingStrategy innerMarshaller)
        {
            this.innerMarshaller = innerMarshaller;
        }

        public ArgumentSyntax AsArgument(TypePositionInfo info, StubCodeContext context)
        {
            return innerMarshaller.AsArgument(info, context);
        }

        public TypeSyntax AsNativeType(TypePositionInfo info)
        {
            return innerMarshaller.AsNativeType(info);
        }

        public IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context)
        {
            foreach (var statement in innerMarshaller.GenerateCleanupStatements(info, context))
            {
                yield return statement;
            }

            // <nativeIdentifier>.FreeNative();
            yield return ExpressionStatement(
                InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName(context.GetIdentifiers(info).native),
                            IdentifierName(ManualTypeMarshallingHelper.FreeNativeMethodName))));
        }

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context, IEnumerable<ArgumentSyntax> nativeTypeConstructorArguments)
        {
            return innerMarshaller.GenerateMarshalStatements(info, context, nativeTypeConstructorArguments);
        }

        public IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubCodeContext context)
        {
            return innerMarshaller.GeneratePinStatements(info, context);
        }

        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context)
        {
            return innerMarshaller.GenerateSetupStatements(info, context);
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            return innerMarshaller.GenerateUnmarshalStatements(info, context);
        }

        public IEnumerable<ArgumentSyntax> GetNativeTypeConstructorArguments(TypePositionInfo info, StubCodeContext context)
        {
            return innerMarshaller.GetNativeTypeConstructorArguments(info, context);
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context)
        {
            return innerMarshaller.UsesNativeIdentifier(info, context);
        }
    }

    /// <summary>
    /// Marshaller that enables support for a GetPinnableReference method on a native type, with a Value property fallback.
    /// </summary>
    internal sealed class PinnableMarshallerTypeMarshalling : ICustomNativeTypeMarshallingStrategy
    {
        private readonly ICustomNativeTypeMarshallingStrategy innerMarshaller;
        private readonly TypeSyntax valuePropertyType;

        public PinnableMarshallerTypeMarshalling(ICustomNativeTypeMarshallingStrategy innerMarshaller, TypeSyntax valuePropertyType)
        {
            this.innerMarshaller = innerMarshaller;
            this.valuePropertyType = valuePropertyType;
        }

        private bool CanPinMarshaller(TypePositionInfo info, StubCodeContext context)
        {
            return context.PinningSupported && !info.IsManagedReturnPosition && !info.IsByRef || info.RefKind == RefKind.In;
        }

        public ArgumentSyntax AsArgument(TypePositionInfo info, StubCodeContext context)
        {
            return innerMarshaller.AsArgument(info, context);
        }

        public TypeSyntax AsNativeType(TypePositionInfo info)
        {
            return valuePropertyType;
        }

        public IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context)
        {
            var subContext = new CustomNativeTypeWithValuePropertyStubContext(context);
            return innerMarshaller.GenerateCleanupStatements(info, subContext);
        }

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context, IEnumerable<ArgumentSyntax> nativeTypeConstructorArguments)
        {
            var subContext = new CustomNativeTypeWithValuePropertyStubContext(context);
            foreach (var statement in innerMarshaller.GenerateMarshalStatements(info, subContext, nativeTypeConstructorArguments))
            {
                yield return statement;
            }

            if (!CanPinMarshaller(info, context))
            {
                // <nativeIdentifier> = <marshalerIdentifier>.Value;
                yield return ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(context.GetIdentifiers(info).native),
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName(subContext.GetIdentifiers(info).native),
                            IdentifierName(ManualTypeMarshallingHelper.ValuePropertyName))));
            }
        }

        public IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubCodeContext context)
        {
            // fixed (<_nativeTypeSyntax> <nativeIdentifier> = &<marshalerIdentifier>)
            var subContext = new CustomNativeTypeWithValuePropertyStubContext(context);
            yield return FixedStatement(
                VariableDeclaration(
                valuePropertyType,
                SingletonSeparatedList(
                    VariableDeclarator(context.GetIdentifiers(info).native)
                        .WithInitializer(EqualsValueClause(
                            PrefixUnaryExpression(SyntaxKind.AddressOfExpression,
                                InvocationExpression(
                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName(subContext.GetIdentifiers(info).native),
                                        IdentifierName(ManualTypeMarshallingHelper.GetPinnableReferenceName)),
                                    ArgumentList())))))),
                EmptyStatement());
        }

        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context)
        {
            var subContext = new CustomNativeTypeWithValuePropertyStubContext(context);
            yield return LocalDeclarationStatement(
                VariableDeclaration(
                    innerMarshaller.AsNativeType(info),
                    SingletonSeparatedList(
                        VariableDeclarator(subContext.GetIdentifiers(info).native)
                        .WithInitializer(EqualsValueClause(LiteralExpression(SyntaxKind.DefaultLiteralExpression))))));

            foreach (var statement in innerMarshaller.GenerateSetupStatements(info, subContext))
            {
                yield return statement;
            }
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            var subContext = new CustomNativeTypeWithValuePropertyStubContext(context);

            if (!CanPinMarshaller(info, context))
            {
                // <marshalerIdentifier>.Value = <nativeIdentifier>;
                yield return ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName(subContext.GetIdentifiers(info).native),
                            IdentifierName(ManualTypeMarshallingHelper.ValuePropertyName)),
                        IdentifierName(context.GetIdentifiers(info).native)));
            }

            foreach (var statement in innerMarshaller.GenerateUnmarshalStatements(info, subContext))
            {
                yield return statement;
            }
        }

        public IEnumerable<ArgumentSyntax> GetNativeTypeConstructorArguments(TypePositionInfo info, StubCodeContext context)
        {
            return innerMarshaller.GetNativeTypeConstructorArguments(info, context);
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context)
        {
            if (CanPinMarshaller(info, context))
            {
                return false;
            }
            return innerMarshaller.UsesNativeIdentifier(info, context);
        }
    }

    /// <summary>
    /// Marshaller that enables support for native types with the constructor variants that take a sizeOfElement int parameter and that have a SetUnmarshalledCollectionLength method.
    /// </summary>
    internal sealed class NumElementsExpressionMarshalling : ICustomNativeTypeMarshallingStrategy
    {
        private readonly ICustomNativeTypeMarshallingStrategy innerMarshaller;
        private readonly ExpressionSyntax numElementsExpression;
        private readonly ExpressionSyntax sizeOfElementExpression;

        public NumElementsExpressionMarshalling(ICustomNativeTypeMarshallingStrategy innerMarshaller, ExpressionSyntax numElementsExpression, ExpressionSyntax sizeOfElementExpression)
        {
            this.innerMarshaller = innerMarshaller;
            this.numElementsExpression = numElementsExpression;
            this.sizeOfElementExpression = sizeOfElementExpression;
        }

        public ArgumentSyntax AsArgument(TypePositionInfo info, StubCodeContext context)
        {
            return innerMarshaller.AsArgument(info, context);
        }

        public TypeSyntax AsNativeType(TypePositionInfo info)
        {
            return innerMarshaller.AsNativeType(info);
        }

        public IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context)
        {
            return innerMarshaller.GenerateCleanupStatements(info, context);
        }

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context, IEnumerable<ArgumentSyntax> nativeTypeConstructorArguments)
        {
            return innerMarshaller.GenerateMarshalStatements(info, context, nativeTypeConstructorArguments);
        }

        public IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubCodeContext context)
        {
            return innerMarshaller.GeneratePinStatements(info, context);
        }

        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context)
        {
            return innerMarshaller.GenerateSetupStatements(info, context);
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            string marshalerIdentifier = MarshallerHelpers.GetMarshallerIdentifier(info, context);
            if (info.RefKind == RefKind.Out || info.IsManagedReturnPosition)
            {
                yield return ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(marshalerIdentifier),
                    ImplicitObjectCreationExpression().AddArgumentListArguments(Argument(sizeOfElementExpression))));
            }

            if (info.IsByRef || !info.ByValueContentsMarshalKind.HasFlag(ByValueContentsMarshalKind.Out))
            {
                yield return ExpressionStatement(
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName(marshalerIdentifier),
                            IdentifierName(ManualTypeMarshallingHelper.SetUnmarshalledCollectionLengthMethodName)))
                    .AddArgumentListArguments(Argument(numElementsExpression)));
            }

            foreach (var statement in innerMarshaller.GenerateUnmarshalStatements(info, context))
            {
                yield return statement;
            }
        }

        public IEnumerable<ArgumentSyntax> GetNativeTypeConstructorArguments(TypePositionInfo info, StubCodeContext context)
        {
            foreach (var arg in innerMarshaller.GetNativeTypeConstructorArguments(info, context))
            {
                yield return arg;
            }
            yield return Argument(sizeOfElementExpression);
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context)
        {
            return innerMarshaller.UsesNativeIdentifier(info, context);
        }
    }

    /// <summary>
    /// Marshaller that enables support for marshalling blittable elements of a contiguous collection via a native type that implements the contiguous collection marshalling spec.
    /// </summary>
    internal sealed class ContiguousBlittableElementCollectionMarshalling : ICustomNativeTypeMarshallingStrategy
    {
        private readonly ICustomNativeTypeMarshallingStrategy innerMarshaller;
        private readonly TypeSyntax elementType;

        public ContiguousBlittableElementCollectionMarshalling(ICustomNativeTypeMarshallingStrategy innerMarshaller, TypeSyntax elementType)
        {
            this.innerMarshaller = innerMarshaller;
            this.elementType = elementType;
        }

        public ArgumentSyntax AsArgument(TypePositionInfo info, StubCodeContext context)
        {
            return innerMarshaller.AsArgument(info, context);
        }

        public TypeSyntax AsNativeType(TypePositionInfo info)
        {
            return innerMarshaller.AsNativeType(info);
        }

        public IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context)
        {
            return innerMarshaller.GenerateCleanupStatements(info, context);
        }

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context, IEnumerable<ArgumentSyntax> nativeTypeConstructorArguments)
        {
            string nativeIdentifier = context.GetIdentifiers(info).native;
            foreach (var statement in innerMarshaller.GenerateMarshalStatements(info, context, nativeTypeConstructorArguments))
            {
                yield return statement;
            }

            if (!info.IsByRef && info.ByValueContentsMarshalKind == ByValueContentsMarshalKind.Out)
            {
                // If the parameter is marshalled by-value [Out], then we don't marshal the contents of the collection.
                yield break;
            }

            // <nativeIdentifier>.ManagedValues.CopyTo(MemoryMarshal.Cast<byte, <elementType>>(<nativeIdentifier>.NativeValueStorage));
            yield return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName(nativeIdentifier),
                            IdentifierName(ManualTypeMarshallingHelper.ManagedValuesPropertyName)),
                        IdentifierName("CopyTo")))
                .AddArgumentListArguments(
                    Argument(
                        InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                ParseTypeName(TypeNames.System_Runtime_InteropServices_MemoryMarshal),
                                GenericName(
                                    Identifier("Cast"))
                                .WithTypeArgumentList(
                                    TypeArgumentList(
                                        SeparatedList(
                                            new[]
                                            {
                                                PredefinedType(Token(SyntaxKind.ByteKeyword)),
                                                elementType
                                            })))))
                        .AddArgumentListArguments(
                            Argument(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName(nativeIdentifier),
                                    IdentifierName(ManualTypeMarshallingHelper.NativeValueStoragePropertyName)))))));
        }

        public IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubCodeContext context)
        {
            return innerMarshaller.GeneratePinStatements(info, context);
        }

        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context)
        {
            return innerMarshaller.GenerateSetupStatements(info, context);
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            string nativeIdentifier = context.GetIdentifiers(info).native;
            // MemoryMarshal.Cast<byte, <elementType>>(<nativeIdentifier>.NativeValueStorage).CopyTo(<nativeIdentifier>.ManagedValues);
            yield return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        InvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    ParseTypeName(TypeNames.System_Runtime_InteropServices_MemoryMarshal),
                                    GenericName(
                                        Identifier("Cast"))
                                    .WithTypeArgumentList(
                                        TypeArgumentList(
                                            SeparatedList(
                                                new[]
                                                {
                                                    PredefinedType(Token(SyntaxKind.ByteKeyword)),
                                                    elementType
                                                })))))
                            .AddArgumentListArguments(
                                Argument(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName(nativeIdentifier),
                                        IdentifierName(ManualTypeMarshallingHelper.NativeValueStoragePropertyName)))),
                        IdentifierName("CopyTo")))
                .AddArgumentListArguments(
                    Argument(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName(nativeIdentifier),
                            IdentifierName(ManualTypeMarshallingHelper.ManagedValuesPropertyName)))));

            foreach (var statement in innerMarshaller.GenerateUnmarshalStatements(info, context))
            {
                yield return statement;
            }
        }

        public IEnumerable<ArgumentSyntax> GetNativeTypeConstructorArguments(TypePositionInfo info, StubCodeContext context)
        {
            return innerMarshaller.GetNativeTypeConstructorArguments(info, context);
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context)
        {
            return innerMarshaller.UsesNativeIdentifier(info, context);
        }
    }

    /// <summary>
    /// Marshaller that enables support for marshalling non-blittable elements of a contiguous collection via a native type that implements the contiguous collection marshalling spec.
    /// </summary>
    internal sealed class ContiguousNonBlittableElementCollectionMarshalling : ICustomNativeTypeMarshallingStrategy
    {
        private const string IndexerIdentifier = "__i";

        private readonly ICustomNativeTypeMarshallingStrategy innerMarshaller;
        private readonly IMarshallingGenerator elementMarshaller;
        private readonly TypePositionInfo elementInfo;

        public ContiguousNonBlittableElementCollectionMarshalling(ICustomNativeTypeMarshallingStrategy innerMarshaller,
            IMarshallingGenerator elementMarshaller,
            TypePositionInfo elementInfo)
        {
            this.innerMarshaller = innerMarshaller;
            this.elementMarshaller = elementMarshaller;
            this.elementInfo = elementInfo;
        }

        private string GetNativeSpanIdentifier(TypePositionInfo info, StubCodeContext context)
        {
            return context.GetIdentifiers(info).managed + "__nativeSpan";
        }

        private LocalDeclarationStatementSyntax GenerateNativeSpanDeclaration(TypePositionInfo info, StubCodeContext context)
        {
            string nativeIdentifier = context.GetIdentifiers(info).native;
            string nativeSpanIdentifier = GetNativeSpanIdentifier(info, context);
            return LocalDeclarationStatement(VariableDeclaration(
                GenericName(
                    Identifier(TypeNames.System_Span),
                    TypeArgumentList(
                        SingletonSeparatedList(elementMarshaller.AsNativeType(elementInfo).GetCompatibleGenericTypeParameterSyntax()))
                ),
                SingletonSeparatedList(
                    VariableDeclarator(Identifier(nativeSpanIdentifier))
                    .WithInitializer(EqualsValueClause(
                        InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                ParseTypeName(TypeNames.System_Runtime_InteropServices_MemoryMarshal),
                                GenericName(
                                    Identifier("Cast"))
                                .WithTypeArgumentList(
                                    TypeArgumentList(
                                        SeparatedList(
                                            new[]
                                            {
                                                PredefinedType(Token(SyntaxKind.ByteKeyword)),
                                                elementMarshaller.AsNativeType(elementInfo).GetCompatibleGenericTypeParameterSyntax()
                                            })))))
                        .AddArgumentListArguments(
                            Argument(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName(nativeIdentifier),
                                IdentifierName(ManualTypeMarshallingHelper.NativeValueStoragePropertyName)))))))));
        }

        private StatementSyntax GenerateContentsMarshallingStatement(TypePositionInfo info, StubCodeContext context, bool useManagedSpanForLength)
        {
            string nativeIdentifier = context.GetIdentifiers(info).native;
            string nativeSpanIdentifier = GetNativeSpanIdentifier(info, context);
            var elementSubContext = new ContiguousCollectionElementMarshallingCodeContext(
                context.CurrentStage,
                IndexerIdentifier,
                nativeSpanIdentifier,
                context);

            string collectionIdentifierForLength = useManagedSpanForLength
                ? $"{nativeIdentifier}.{ManualTypeMarshallingHelper.ManagedValuesPropertyName}"
                : nativeSpanIdentifier;

            TypePositionInfo localElementInfo = elementInfo with
            {
                InstanceIdentifier = info.InstanceIdentifier,
                RefKind = info.IsByRef ? info.RefKind : info.ByValueContentsMarshalKind.GetRefKindForByValueContentsKind(),
                ManagedIndex = info.ManagedIndex,
                NativeIndex = info.NativeIndex
            };

            StatementSyntax marshallingStatement = Block(
                List(elementMarshaller.Generate(
                    localElementInfo,
                    elementSubContext)));

            if (elementMarshaller.AsNativeType(elementInfo) is PointerTypeSyntax)
            {
                PointerNativeTypeAssignmentRewriter rewriter = new(elementSubContext.GetIdentifiers(localElementInfo).native);
                marshallingStatement = (StatementSyntax)rewriter.Visit(marshallingStatement);
            }

            // Iterate through the elements of the native collection to unmarshal them
            return Block(
                GenerateNativeSpanDeclaration(info, context),
                MarshallerHelpers.GetForLoop(collectionIdentifierForLength, IndexerIdentifier)
                                .WithStatement(marshallingStatement));
        }

        public ArgumentSyntax AsArgument(TypePositionInfo info, StubCodeContext context)
        {
            return innerMarshaller.AsArgument(info, context);
        }

        public TypeSyntax AsNativeType(TypePositionInfo info)
        {
            return innerMarshaller.AsNativeType(info);
        }

        public IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context)
        {
            yield return GenerateContentsMarshallingStatement(info, context, useManagedSpanForLength: false);
            foreach (var statement in innerMarshaller.GenerateCleanupStatements(info, context))
            {
                yield return statement;
            }
        }

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context, IEnumerable<ArgumentSyntax> nativeTypeConstructorArguments)
        {
            foreach (var statement in innerMarshaller.GenerateMarshalStatements(info, context, nativeTypeConstructorArguments))
            {
                yield return statement;
            }
            
            if (!info.IsByRef && info.ByValueContentsMarshalKind == ByValueContentsMarshalKind.Out)
            {
                // If the parameter is marshalled by-value [Out], then we don't marshal the contents of the collection.
                yield break;
            }

            yield return GenerateContentsMarshallingStatement(info, context, useManagedSpanForLength: true);
        }

        public IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubCodeContext context)
        {
            return innerMarshaller.GeneratePinStatements(info, context);
        }

        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context)
        {
            return innerMarshaller.GenerateSetupStatements(info, context);
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            yield return GenerateContentsMarshallingStatement(info, context, useManagedSpanForLength: false);
            foreach (var statement in innerMarshaller.GenerateUnmarshalStatements(info, context))
            {
                yield return statement;
            }
        }

        public IEnumerable<ArgumentSyntax> GetNativeTypeConstructorArguments(TypePositionInfo info, StubCodeContext context)
        {
            return innerMarshaller.GetNativeTypeConstructorArguments(info, context);
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context)
        {
            return innerMarshaller.UsesNativeIdentifier(info, context);
        }

        /// <summary>
        /// Rewrite assignment expressions to the native identifier to cast to IntPtr.
        /// This handles the case where the native type of a non-blittable managed type is a pointer,
        /// which are unsupported in generic type parameters.
        /// </summary>
        private class PointerNativeTypeAssignmentRewriter : CSharpSyntaxRewriter
        {
            private readonly string nativeIdentifier;

            public PointerNativeTypeAssignmentRewriter(string nativeIdentifier)
            {
                this.nativeIdentifier = nativeIdentifier;
            }

            public override SyntaxNode VisitAssignmentExpression(AssignmentExpressionSyntax node)
            {
                if (node.Left.ToString() == nativeIdentifier)
                {
                    return node.WithRight(
                        CastExpression(MarshallerHelpers.SystemIntPtrType, node.Right));
                }

                return node;
            }
        }
    }
}
