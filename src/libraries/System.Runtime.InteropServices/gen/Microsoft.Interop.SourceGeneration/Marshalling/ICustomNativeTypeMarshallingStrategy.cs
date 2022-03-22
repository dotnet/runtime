// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    /// <summary>
    /// The base interface for implementing various different aspects of the custom native type and collection marshalling specs.
    /// </summary>
    internal interface ICustomNativeTypeMarshallingStrategy
    {
        TypeSyntax AsNativeType(TypePositionInfo info);

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
        private readonly TypeSyntax _nativeTypeSyntax;

        public SimpleCustomNativeTypeMarshalling(TypeSyntax nativeTypeSyntax)
        {
            _nativeTypeSyntax = nativeTypeSyntax;
        }

        public TypeSyntax AsNativeType(TypePositionInfo info)
        {
            return _nativeTypeSyntax;
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

            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);
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
        public CustomNativeTypeWithValuePropertyStubContext(StubCodeContext parentContext)
        {
            ParentContext = parentContext;
            CurrentStage = parentContext.CurrentStage;
        }

        public override bool SingleFrameSpansNativeContext => ParentContext!.SingleFrameSpansNativeContext;

        public override bool AdditionalTemporaryStateLivesAcrossStages => ParentContext!.AdditionalTemporaryStateLivesAcrossStages;

        public override (string managed, string native) GetIdentifiers(TypePositionInfo info)
        {
            return (ParentContext!.GetIdentifiers(info).managed, MarshallerHelpers.GetMarshallerIdentifier(info, ParentContext));
        }
    }

    /// <summary>
    /// Marshaller that enables support of a Value property on a native type.
    /// </summary>
    internal sealed class CustomNativeTypeWithValuePropertyMarshalling : ICustomNativeTypeMarshallingStrategy
    {
        private readonly ICustomNativeTypeMarshallingStrategy _innerMarshaller;
        private readonly TypeSyntax _valuePropertyType;

        public CustomNativeTypeWithValuePropertyMarshalling(ICustomNativeTypeMarshallingStrategy innerMarshaller, TypeSyntax valuePropertyType)
        {
            _innerMarshaller = innerMarshaller;
            _valuePropertyType = valuePropertyType;
        }

        public TypeSyntax AsNativeType(TypePositionInfo info)
        {
            return _valuePropertyType;
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context)
        {
            return true;
        }

        public IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context)
        {
            var subContext = new CustomNativeTypeWithValuePropertyStubContext(context);

            // When temporary state does not live across stages, the marshaller state is uninitialized
            // in any stage other than Marshal and Unmarshal. So, we need to reinitialize it here in Cleanup
            // from the native value so we can safely run any cleanup functionality in the marshaller.
            if (!context.AdditionalTemporaryStateLivesAcrossStages)
            {
                yield return GenerateValuePropertyAssignment(info, context, subContext);
            }

            foreach (StatementSyntax statement in _innerMarshaller.GenerateCleanupStatements(info, subContext))
            {
                yield return statement;
            }
        }

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context, IEnumerable<ArgumentSyntax> nativeTypeConstructorArguments)
        {
            var subContext = new CustomNativeTypeWithValuePropertyStubContext(context);
            foreach (StatementSyntax statement in _innerMarshaller.GenerateMarshalStatements(info, subContext, nativeTypeConstructorArguments))
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

        private StatementSyntax GenerateValuePropertyAssignment(TypePositionInfo info, StubCodeContext context, CustomNativeTypeWithValuePropertyStubContext subContext)
        {
            // <marshalerIdentifier>.Value = <nativeIdentifier>;
            return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(subContext.GetIdentifiers(info).native),
                        IdentifierName(ManualTypeMarshallingHelper.ValuePropertyName)),
                    IdentifierName(context.GetIdentifiers(info).native)));
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            var subContext = new CustomNativeTypeWithValuePropertyStubContext(context);

            if (info.IsManagedReturnPosition || (info.IsByRef && info.RefKind != RefKind.In))
            {
                yield return GenerateValuePropertyAssignment(info, context, subContext);
            }

            foreach (StatementSyntax statement in _innerMarshaller.GenerateUnmarshalStatements(info, subContext))
            {
                yield return statement;
            }
        }

        public IEnumerable<ArgumentSyntax> GetNativeTypeConstructorArguments(TypePositionInfo info, StubCodeContext context)
        {
            var subContext = new CustomNativeTypeWithValuePropertyStubContext(context);
            return _innerMarshaller.GetNativeTypeConstructorArguments(info, subContext);
        }

        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context)
        {
            var subContext = new CustomNativeTypeWithValuePropertyStubContext(context);
            yield return LocalDeclarationStatement(
                VariableDeclaration(
                    _innerMarshaller.AsNativeType(info),
                    SingletonSeparatedList(
                        VariableDeclarator(subContext.GetIdentifiers(info).native)
                        .WithInitializer(EqualsValueClause(LiteralExpression(SyntaxKind.DefaultLiteralExpression))))));

            foreach (StatementSyntax statement in _innerMarshaller.GenerateSetupStatements(info, subContext))
            {
                yield return statement;
            }
        }

        public IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubCodeContext context)
        {
            var subContext = new CustomNativeTypeWithValuePropertyStubContext(context);
            return _innerMarshaller.GeneratePinStatements(info, subContext);
        }
    }

    /// <summary>
    /// Marshaller that enables support for a stackalloc constructor variant on a native type.
    /// </summary>
    internal sealed class StackallocOptimizationMarshalling : ICustomNativeTypeMarshallingStrategy
    {
        private readonly ICustomNativeTypeMarshallingStrategy _innerMarshaller;

        public StackallocOptimizationMarshalling(ICustomNativeTypeMarshallingStrategy innerMarshaller)
        {
            _innerMarshaller = innerMarshaller;
        }

        public TypeSyntax AsNativeType(TypePositionInfo info)
        {
            return _innerMarshaller.AsNativeType(info);
        }

        public IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context)
        {
            return _innerMarshaller.GenerateCleanupStatements(info, context);
        }

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context, IEnumerable<ArgumentSyntax> nativeTypeConstructorArguments)
        {
            if (StackAllocOptimizationValid(info, context))
            {
                // byte* <managedIdentifier>__stackptr = stackalloc byte[<_nativeLocalType>.BufferSize];
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
                                                    IdentifierName(ManualTypeMarshallingHelper.BufferSizeFieldName))
                                            ))))))))));
            }

            foreach (StatementSyntax statement in _innerMarshaller.GenerateMarshalStatements(info, context, nativeTypeConstructorArguments))
            {
                yield return statement;
            }
        }

        private static bool StackAllocOptimizationValid(TypePositionInfo info, StubCodeContext context)
        {
            return context.SingleFrameSpansNativeContext && (!info.IsByRef || info.RefKind == RefKind.In);
        }

        private static string GetStackAllocPointerIdentifier(TypePositionInfo info, StubCodeContext context)
        {
            return context.GetAdditionalIdentifier(info, "stackptr");
        }

        public IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubCodeContext context)
        {
            return _innerMarshaller.GeneratePinStatements(info, context);
        }

        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context)
        {
            return _innerMarshaller.GenerateSetupStatements(info, context);
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            return _innerMarshaller.GenerateUnmarshalStatements(info, context);
        }

        public IEnumerable<ArgumentSyntax> GetNativeTypeConstructorArguments(TypePositionInfo info, StubCodeContext context)
        {
            foreach (ArgumentSyntax arg in _innerMarshaller.GetNativeTypeConstructorArguments(info, context))
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
                                    IdentifierName(ManualTypeMarshallingHelper.BufferSizeFieldName)))
                        }))));
            }
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context)
        {
            return _innerMarshaller.UsesNativeIdentifier(info, context);
        }
    }

    /// <summary>
    /// Marshaller that enables support for a FreeNative method on a native type.
    /// </summary>
    internal sealed class FreeNativeCleanupStrategy : ICustomNativeTypeMarshallingStrategy
    {
        private readonly ICustomNativeTypeMarshallingStrategy _innerMarshaller;

        public FreeNativeCleanupStrategy(ICustomNativeTypeMarshallingStrategy innerMarshaller)
        {
            _innerMarshaller = innerMarshaller;
        }

        public TypeSyntax AsNativeType(TypePositionInfo info)
        {
            return _innerMarshaller.AsNativeType(info);
        }

        public IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context)
        {
            foreach (StatementSyntax statement in _innerMarshaller.GenerateCleanupStatements(info, context))
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
            return _innerMarshaller.GenerateMarshalStatements(info, context, nativeTypeConstructorArguments);
        }

        public IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubCodeContext context)
        {
            return _innerMarshaller.GeneratePinStatements(info, context);
        }

        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context)
        {
            return _innerMarshaller.GenerateSetupStatements(info, context);
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            return _innerMarshaller.GenerateUnmarshalStatements(info, context);
        }

        public IEnumerable<ArgumentSyntax> GetNativeTypeConstructorArguments(TypePositionInfo info, StubCodeContext context)
        {
            return _innerMarshaller.GetNativeTypeConstructorArguments(info, context);
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context)
        {
            return _innerMarshaller.UsesNativeIdentifier(info, context);
        }
    }

    /// <summary>
    /// Marshaller that calls the GetPinnableReference method on the marshaller value and enables support for the Value property.
    /// </summary>
    internal sealed class PinnableMarshallerTypeMarshalling : ICustomNativeTypeMarshallingStrategy
    {
        private readonly ICustomNativeTypeMarshallingStrategy _innerMarshaller;
        private readonly TypeSyntax _valuePropertyType;

        public PinnableMarshallerTypeMarshalling(ICustomNativeTypeMarshallingStrategy innerMarshaller, TypeSyntax valuePropertyType)
        {
            _innerMarshaller = innerMarshaller;
            _valuePropertyType = valuePropertyType;
        }

        private bool CanPinMarshaller(TypePositionInfo info, StubCodeContext context)
        {
            return context.SingleFrameSpansNativeContext && !info.IsManagedReturnPosition && !info.IsByRef || info.RefKind == RefKind.In;
        }

        public TypeSyntax AsNativeType(TypePositionInfo info)
        {
            return _valuePropertyType;
        }

        public IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context)
        {
            var subContext = new CustomNativeTypeWithValuePropertyStubContext(context);

            if (!context.AdditionalTemporaryStateLivesAcrossStages)
            {
                // <marshalerIdentifier>.Value = <nativeIdentifier>;
                yield return GenerateValuePropertyAssignment(info, context, subContext);
            }

            foreach (StatementSyntax statement in _innerMarshaller.GenerateCleanupStatements(info, subContext))
            {
                yield return statement;
            }
        }

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context, IEnumerable<ArgumentSyntax> nativeTypeConstructorArguments)
        {
            var subContext = new CustomNativeTypeWithValuePropertyStubContext(context);
            foreach (StatementSyntax statement in _innerMarshaller.GenerateMarshalStatements(info, subContext, nativeTypeConstructorArguments))
            {
                yield return statement;
            }

            if (!CanPinMarshaller(info, context))
                yield return GenerateNativeAssignmentFromValueProperty(info, context, subContext);
        }

        private static StatementSyntax GenerateNativeAssignmentFromValueProperty(TypePositionInfo info, StubCodeContext context, CustomNativeTypeWithValuePropertyStubContext subContext)
        {
            // <nativeIdentifier> = <marshalerIdentifier>.Value;
            return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(context.GetIdentifiers(info).native),
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(subContext.GetIdentifiers(info).native),
                        IdentifierName(ManualTypeMarshallingHelper.ValuePropertyName))));
        }

        public IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubCodeContext context)
        {
            // fixed (<_nativeTypeSyntax> <ignoredIdentifier> = &<marshalerIdentifier>)
            //  <assignment to Value property>
            var subContext = new CustomNativeTypeWithValuePropertyStubContext(context);
            yield return FixedStatement(
                VariableDeclaration(
                _valuePropertyType,
                SingletonSeparatedList(
                    VariableDeclarator(Identifier(context.GetAdditionalIdentifier(info, "ignored")))
                        .WithInitializer(EqualsValueClause(
                            IdentifierName(subContext.GetIdentifiers(info).native))))),
                GenerateNativeAssignmentFromValueProperty(info, context, subContext));
        }

        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context)
        {
            var subContext = new CustomNativeTypeWithValuePropertyStubContext(context);
            yield return LocalDeclarationStatement(
                VariableDeclaration(
                    _innerMarshaller.AsNativeType(info),
                    SingletonSeparatedList(
                        VariableDeclarator(subContext.GetIdentifiers(info).native)
                        .WithInitializer(EqualsValueClause(LiteralExpression(SyntaxKind.DefaultLiteralExpression))))));

            foreach (StatementSyntax statement in _innerMarshaller.GenerateSetupStatements(info, subContext))
            {
                yield return statement;
            }
        }

        private StatementSyntax GenerateValuePropertyAssignment(TypePositionInfo info, StubCodeContext context, CustomNativeTypeWithValuePropertyStubContext subContext)
        {
            // <marshalerIdentifier>.Value = <nativeIdentifier>;
            return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(subContext.GetIdentifiers(info).native),
                        IdentifierName(ManualTypeMarshallingHelper.ValuePropertyName)),
                    IdentifierName(context.GetIdentifiers(info).native)));
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            var subContext = new CustomNativeTypeWithValuePropertyStubContext(context);

            if (info.IsManagedReturnPosition || (info.IsByRef && info.RefKind != RefKind.In))
            {
                // <marshalerIdentifier>.Value = <nativeIdentifier>;
                yield return GenerateValuePropertyAssignment(info, context, subContext);
            }

            foreach (StatementSyntax statement in _innerMarshaller.GenerateUnmarshalStatements(info, subContext))
            {
                yield return statement;
            }
        }

        public IEnumerable<ArgumentSyntax> GetNativeTypeConstructorArguments(TypePositionInfo info, StubCodeContext context)
        {
            var subContext = new CustomNativeTypeWithValuePropertyStubContext(context);
            return _innerMarshaller.GetNativeTypeConstructorArguments(info, subContext);
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context)
        {
            return _innerMarshaller.UsesNativeIdentifier(info, context);
        }
    }

    /// <summary>
    /// Marshaller that enables support for native types with the constructor variants that take a sizeOfElement int parameter and that have a SetUnmarshalledCollectionLength method.
    /// </summary>
    internal sealed class NumElementsExpressionMarshalling : ICustomNativeTypeMarshallingStrategy
    {
        private readonly ICustomNativeTypeMarshallingStrategy _innerMarshaller;
        private readonly ExpressionSyntax _numElementsExpression;
        private readonly ExpressionSyntax _sizeOfElementExpression;

        public NumElementsExpressionMarshalling(ICustomNativeTypeMarshallingStrategy innerMarshaller, ExpressionSyntax numElementsExpression, ExpressionSyntax sizeOfElementExpression)
        {
            _innerMarshaller = innerMarshaller;
            _numElementsExpression = numElementsExpression;
            _sizeOfElementExpression = sizeOfElementExpression;
        }

        public TypeSyntax AsNativeType(TypePositionInfo info)
        {
            return _innerMarshaller.AsNativeType(info);
        }

        public IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context)
        {
            // When temporary state does not live across stages, the marshaller state is uninitialized
            // in any stage other than Marshal and Unmarshal. So, we need to reinitialize it here in Cleanup
            // from the native data so we can safely run any cleanup functionality in the marshaller.
            if (!context.AdditionalTemporaryStateLivesAcrossStages)
            {
                foreach (StatementSyntax statement in GenerateUnmarshallerCollectionInitialization(info, context))
                {
                    yield return statement;
                }
            }

            foreach (StatementSyntax statement in _innerMarshaller.GenerateCleanupStatements(info, context))
            {
                yield return statement;
            }
        }

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context, IEnumerable<ArgumentSyntax> nativeTypeConstructorArguments)
        {
            return _innerMarshaller.GenerateMarshalStatements(info, context, nativeTypeConstructorArguments);
        }

        public IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubCodeContext context)
        {
            return _innerMarshaller.GeneratePinStatements(info, context);
        }

        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context)
        {
            return _innerMarshaller.GenerateSetupStatements(info, context);
        }

        private IEnumerable<StatementSyntax> GenerateUnmarshallerCollectionInitialization(TypePositionInfo info, StubCodeContext context)
        {
            string marshalerIdentifier = MarshallerHelpers.GetMarshallerIdentifier(info, context);
            if (info.RefKind == RefKind.Out || info.IsManagedReturnPosition)
            {
                yield return ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(marshalerIdentifier),
                    ImplicitObjectCreationExpression().AddArgumentListArguments(Argument(_sizeOfElementExpression))));
            }

            if (info.IsByRef || !info.ByValueContentsMarshalKind.HasFlag(ByValueContentsMarshalKind.Out))
            {
                yield return ExpressionStatement(
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName(marshalerIdentifier),
                            IdentifierName(ManualTypeMarshallingHelper.SetUnmarshalledCollectionLengthMethodName)))
                    .AddArgumentListArguments(Argument(_numElementsExpression)));
            }
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            // To fulfill the generic contiguous collection marshaller design,
            // we need to emit code to initialize the collection marshaller with the size of native elements
            // and set the unmanaged collection length before we marshal back the native data.
            // This ensures that the marshaller object has enough state to successfully set up the ManagedValues
            // and NativeValueStorage spans when the actual collection value is unmarshalled from native to the marshaller.
            foreach (StatementSyntax statement in GenerateUnmarshallerCollectionInitialization(info, context))
            {
                yield return statement;
            }

            foreach (StatementSyntax statement in _innerMarshaller.GenerateUnmarshalStatements(info, context))
            {
                yield return statement;
            }
        }

        public IEnumerable<ArgumentSyntax> GetNativeTypeConstructorArguments(TypePositionInfo info, StubCodeContext context)
        {
            foreach (ArgumentSyntax arg in _innerMarshaller.GetNativeTypeConstructorArguments(info, context))
            {
                yield return arg;
            }
            yield return Argument(_sizeOfElementExpression);
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context)
        {
            return _innerMarshaller.UsesNativeIdentifier(info, context);
        }
    }

    /// <summary>
    /// Marshaller that enables support for marshalling blittable elements of a contiguous collection via a native type that implements the contiguous collection marshalling spec.
    /// </summary>
    internal sealed class ContiguousBlittableElementCollectionMarshalling : ICustomNativeTypeMarshallingStrategy
    {
        private readonly ICustomNativeTypeMarshallingStrategy _innerMarshaller;
        private readonly TypeSyntax _elementType;

        public ContiguousBlittableElementCollectionMarshalling(ICustomNativeTypeMarshallingStrategy innerMarshaller, TypeSyntax elementType)
        {
            _innerMarshaller = innerMarshaller;
            _elementType = elementType;
        }

        public TypeSyntax AsNativeType(TypePositionInfo info)
        {
            return _innerMarshaller.AsNativeType(info);
        }

        public IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context)
        {
            return _innerMarshaller.GenerateCleanupStatements(info, context);
        }

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context, IEnumerable<ArgumentSyntax> nativeTypeConstructorArguments)
        {
            string nativeIdentifier = context.GetIdentifiers(info).native;
            foreach (StatementSyntax statement in _innerMarshaller.GenerateMarshalStatements(info, context, nativeTypeConstructorArguments))
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
                                                _elementType
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
            return _innerMarshaller.GeneratePinStatements(info, context);
        }

        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context)
        {
            return _innerMarshaller.GenerateSetupStatements(info, context);
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
                                                    _elementType
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

            foreach (StatementSyntax statement in _innerMarshaller.GenerateUnmarshalStatements(info, context))
            {
                yield return statement;
            }
        }

        public IEnumerable<ArgumentSyntax> GetNativeTypeConstructorArguments(TypePositionInfo info, StubCodeContext context)
        {
            return _innerMarshaller.GetNativeTypeConstructorArguments(info, context);
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context)
        {
            return _innerMarshaller.UsesNativeIdentifier(info, context);
        }
    }

    /// <summary>
    /// Marshaller that enables support for marshalling non-blittable elements of a contiguous collection via a native type that implements the contiguous collection marshalling spec.
    /// </summary>
    internal sealed class ContiguousNonBlittableElementCollectionMarshalling : ICustomNativeTypeMarshallingStrategy
    {
        private readonly ICustomNativeTypeMarshallingStrategy _innerMarshaller;
        private readonly IMarshallingGenerator _elementMarshaller;
        private readonly TypePositionInfo _elementInfo;

        public ContiguousNonBlittableElementCollectionMarshalling(ICustomNativeTypeMarshallingStrategy innerMarshaller,
            IMarshallingGenerator elementMarshaller,
            TypePositionInfo elementInfo)
        {
            _innerMarshaller = innerMarshaller;
            _elementMarshaller = elementMarshaller;
            _elementInfo = elementInfo;
        }

        private LocalDeclarationStatementSyntax GenerateNativeSpanDeclaration(TypePositionInfo info, StubCodeContext context)
        {
            string nativeIdentifier = context.GetIdentifiers(info).native;
            string nativeSpanIdentifier = MarshallerHelpers.GetNativeSpanIdentifier(info, context);
            return LocalDeclarationStatement(VariableDeclaration(
                GenericName(
                    Identifier(TypeNames.System_Span),
                    TypeArgumentList(
                        SingletonSeparatedList(_elementMarshaller.AsNativeType(_elementInfo).GetCompatibleGenericTypeParameterSyntax()))
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
                                                _elementMarshaller.AsNativeType(_elementInfo).GetCompatibleGenericTypeParameterSyntax()
                                            })))))
                        .AddArgumentListArguments(
                            Argument(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName(nativeIdentifier),
                                IdentifierName(ManualTypeMarshallingHelper.NativeValueStoragePropertyName)))))))));
        }

        private StatementSyntax GenerateContentsMarshallingStatement(TypePositionInfo info, StubCodeContext context, bool useManagedSpanForLength)
        {
            string nativeIdentifier = context.GetIdentifiers(info).native;
            string nativeSpanIdentifier = MarshallerHelpers.GetNativeSpanIdentifier(info, context);
            var elementSetupSubContext = new ContiguousCollectionElementMarshallingCodeContext(
                StubCodeContext.Stage.Setup,
                nativeSpanIdentifier,
                context);
            var elementSubContext = new ContiguousCollectionElementMarshallingCodeContext(
                context.CurrentStage,
                nativeSpanIdentifier,
                context);

            string collectionIdentifierForLength = useManagedSpanForLength
                ? $"{nativeIdentifier}.{ManualTypeMarshallingHelper.ManagedValuesPropertyName}"
                : nativeSpanIdentifier;

            TypePositionInfo localElementInfo = _elementInfo with
            {
                InstanceIdentifier = info.InstanceIdentifier,
                RefKind = info.IsByRef ? info.RefKind : info.ByValueContentsMarshalKind.GetRefKindForByValueContentsKind(),
                ManagedIndex = info.ManagedIndex,
                NativeIndex = info.NativeIndex
            };

            List<StatementSyntax> elementStatements = _elementMarshaller.Generate(localElementInfo, elementSubContext).ToList();

            if (elementStatements.Any())
            {
                StatementSyntax marshallingStatement = Block(
                    List(_elementMarshaller.Generate(localElementInfo, elementSetupSubContext)
                        .Concat(elementStatements)));

                if (_elementMarshaller.AsNativeType(_elementInfo) is PointerTypeSyntax elementNativeType)
                {
                    PointerNativeTypeAssignmentRewriter rewriter = new(elementSubContext.GetIdentifiers(localElementInfo).native, elementNativeType);
                    marshallingStatement = (StatementSyntax)rewriter.Visit(marshallingStatement);
                }

                // Iterate through the elements of the native collection to unmarshal them
                return Block(
                    GenerateNativeSpanDeclaration(info, context),
                    MarshallerHelpers.GetForLoop(collectionIdentifierForLength, elementSubContext.IndexerIdentifier)
                                    .WithStatement(marshallingStatement));
            }
            return EmptyStatement();
        }

        public TypeSyntax AsNativeType(TypePositionInfo info)
        {
            return _innerMarshaller.AsNativeType(info);
        }

        public IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context)
        {
            yield return GenerateContentsMarshallingStatement(info, context, useManagedSpanForLength: false);
            foreach (StatementSyntax statement in _innerMarshaller.GenerateCleanupStatements(info, context))
            {
                yield return statement;
            }
        }

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context, IEnumerable<ArgumentSyntax> nativeTypeConstructorArguments)
        {
            foreach (StatementSyntax statement in _innerMarshaller.GenerateMarshalStatements(info, context, nativeTypeConstructorArguments))
            {
                yield return statement;
            }

            if (!info.IsByRef && info.ByValueContentsMarshalKind == ByValueContentsMarshalKind.Out)
            {
                // If the parameter is marshalled by-value [Out], then we don't marshal the contents of the collection.
                // We do clear the span, so that if the invoke target doesn't fill it, we aren't left with undefined content.
                // <nativeIdentifier>.NativeValueStorage.Clear();
                string nativeIdentifier = context.GetIdentifiers(info).native;
                yield return ExpressionStatement(
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName(nativeIdentifier),
                                IdentifierName(ManualTypeMarshallingHelper.NativeValueStoragePropertyName)),
                            IdentifierName("Clear"))));
                yield break;
            }

            yield return GenerateContentsMarshallingStatement(info, context, useManagedSpanForLength: true);
        }

        public IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubCodeContext context)
        {
            return _innerMarshaller.GeneratePinStatements(info, context);
        }

        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context)
        {
            return _innerMarshaller.GenerateSetupStatements(info, context);
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            yield return GenerateContentsMarshallingStatement(info, context, useManagedSpanForLength: false);
            foreach (StatementSyntax statement in _innerMarshaller.GenerateUnmarshalStatements(info, context))
            {
                yield return statement;
            }
        }

        public IEnumerable<ArgumentSyntax> GetNativeTypeConstructorArguments(TypePositionInfo info, StubCodeContext context)
        {
            return _innerMarshaller.GetNativeTypeConstructorArguments(info, context);
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context)
        {
            return _innerMarshaller.UsesNativeIdentifier(info, context);
        }

        /// <summary>
        /// Rewrite assignment expressions to the native identifier to cast to IntPtr.
        /// This handles the case where the native type of a non-blittable managed type is a pointer,
        /// which are unsupported in generic type parameters.
        /// </summary>
        private class PointerNativeTypeAssignmentRewriter : CSharpSyntaxRewriter
        {
            private readonly string _nativeIdentifier;
            private readonly PointerTypeSyntax _nativeType;

            public PointerNativeTypeAssignmentRewriter(string nativeIdentifier, PointerTypeSyntax nativeType)
            {
                _nativeIdentifier = nativeIdentifier;
                _nativeType = nativeType;
            }

            public override SyntaxNode VisitAssignmentExpression(AssignmentExpressionSyntax node)
            {
                if (node.Left.ToString() == _nativeIdentifier)
                {
                    return node.WithRight(
                        CastExpression(MarshallerHelpers.SystemIntPtrType, node.Right));
                }
                if (node.Right.ToString() == _nativeIdentifier)
                {
                    return node.WithRight(CastExpression(_nativeType, node.Right));
                }

                return node;
            }
        }
    }
}
