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
                            IdentifierName(ShapeMemberNames.Value.ToManaged)))));
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
    internal class CustomNativeTypeWithToFromNativeValueContext : StubCodeContext
    {
        public CustomNativeTypeWithToFromNativeValueContext(StubCodeContext parentContext)
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
    /// Marshaller that enables support of ToNativeValue/FromNativeValue methods on a native type.
    /// </summary>
    internal sealed class CustomNativeTypeWithToFromNativeValueMarshalling : ICustomNativeTypeMarshallingStrategy
    {
        private readonly ICustomNativeTypeMarshallingStrategy _innerMarshaller;
        private readonly TypeSyntax _nativeValueTypeSyntax;

        public CustomNativeTypeWithToFromNativeValueMarshalling(ICustomNativeTypeMarshallingStrategy innerMarshaller, TypeSyntax nativeValueTypeSyntax)
        {
            _innerMarshaller = innerMarshaller;
            _nativeValueTypeSyntax = nativeValueTypeSyntax;
        }

        public TypeSyntax AsNativeType(TypePositionInfo info)
        {
            return _nativeValueTypeSyntax;
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context)
        {
            return true;
        }

        public IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context)
        {
            var subContext = new CustomNativeTypeWithToFromNativeValueContext(context);

            // When temporary state does not live across stages, the marshaller state is uninitialized
            // in any stage other than Marshal and Unmarshal. So, we need to reinitialize it here in Cleanup
            // from the native value so we can safely run any cleanup functionality in the marshaller.
            if (!context.AdditionalTemporaryStateLivesAcrossStages)
            {
                yield return GenerateFromNativeValueInvocation(info, context, subContext);
            }

            foreach (StatementSyntax statement in _innerMarshaller.GenerateCleanupStatements(info, subContext))
            {
                yield return statement;
            }
        }

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context, IEnumerable<ArgumentSyntax> nativeTypeConstructorArguments)
        {
            var subContext = new CustomNativeTypeWithToFromNativeValueContext(context);
            foreach (StatementSyntax statement in _innerMarshaller.GenerateMarshalStatements(info, subContext, nativeTypeConstructorArguments))
            {
                yield return statement;
            }

            // <nativeIdentifier> = <marshalerIdentifier>.ToNativeValue();
            yield return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(context.GetIdentifiers(info).native),
                    InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName(subContext.GetIdentifiers(info).native),
                            IdentifierName(ShapeMemberNames.Value.ToNativeValue)),
                        ArgumentList())));
        }

        private static StatementSyntax GenerateFromNativeValueInvocation(TypePositionInfo info, StubCodeContext context, CustomNativeTypeWithToFromNativeValueContext subContext)
        {
            // <marshalerIdentifier>.FromNativeValue(<nativeIdentifier>);
            return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(subContext.GetIdentifiers(info).native),
                        IdentifierName(ShapeMemberNames.Value.FromNativeValue)),
                    ArgumentList(SingletonSeparatedList(Argument(IdentifierName(context.GetIdentifiers(info).native))))));
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            var subContext = new CustomNativeTypeWithToFromNativeValueContext(context);

            if (info.IsManagedReturnPosition || (info.IsByRef && info.RefKind != RefKind.In))
            {
                yield return GenerateFromNativeValueInvocation(info, context, subContext);
            }

            foreach (StatementSyntax statement in _innerMarshaller.GenerateUnmarshalStatements(info, subContext))
            {
                yield return statement;
            }
        }

        public IEnumerable<ArgumentSyntax> GetNativeTypeConstructorArguments(TypePositionInfo info, StubCodeContext context)
        {
            var subContext = new CustomNativeTypeWithToFromNativeValueContext(context);
            return _innerMarshaller.GetNativeTypeConstructorArguments(info, subContext);
        }

        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context)
        {
            var subContext = new CustomNativeTypeWithToFromNativeValueContext(context);
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
            var subContext = new CustomNativeTypeWithToFromNativeValueContext(context);
            return _innerMarshaller.GeneratePinStatements(info, subContext);
        }
    }

    /// <summary>
    /// Marshaller that enables support for a stackalloc constructor variant on a native type.
    /// </summary>
    internal sealed class StackallocOptimizationMarshalling : ICustomNativeTypeMarshallingStrategy
    {
        private readonly ICustomNativeTypeMarshallingStrategy _innerMarshaller;
        private readonly TypeSyntax _bufferElementType;
        private readonly int _bufferSize;

        public StackallocOptimizationMarshalling(ICustomNativeTypeMarshallingStrategy innerMarshaller, TypeSyntax bufferElementType, int bufferSize)
        {
            _innerMarshaller = innerMarshaller;
            _bufferElementType = bufferElementType;
            _bufferSize = bufferSize;
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
                // <bufferElementType>* <managedIdentifier>__stackptr = stackalloc <bufferElementType>[<_bufferSize>];
                yield return LocalDeclarationStatement(
                VariableDeclaration(
                    PointerType(_bufferElementType),
                    SingletonSeparatedList(
                        VariableDeclarator(GetStackAllocPointerIdentifier(info, context))
                            .WithInitializer(EqualsValueClause(
                                StackAllocArrayCreationExpression(
                                        ArrayType(
                                            _bufferElementType,
                                            SingletonList(ArrayRankSpecifier(SingletonSeparatedList<ExpressionSyntax>(
                                                LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(_bufferSize))
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
                                _bufferElementType))))
                    .WithArgumentList(
                        ArgumentList(SeparatedList(new ArgumentSyntax[]
                        {
                            Argument(IdentifierName(GetStackAllocPointerIdentifier(info, context))),
                            Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(_bufferSize)))
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
                            IdentifierName(ShapeMemberNames.Value.FreeNative))));
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
    /// Marshaller that calls the GetPinnableReference method on the marshaller value and enables support for the ToNativeValue and FromNativeValue methods.
    /// </summary>
    internal sealed class PinnableMarshallerTypeMarshalling : ICustomNativeTypeMarshallingStrategy
    {
        private readonly ICustomNativeTypeMarshallingStrategy _innerMarshaller;
        private readonly TypeSyntax _nativeValueType;

        public PinnableMarshallerTypeMarshalling(ICustomNativeTypeMarshallingStrategy innerMarshaller, TypeSyntax nativeValueType)
        {
            _innerMarshaller = innerMarshaller;
            _nativeValueType = nativeValueType;
        }

        private static bool CanPinMarshaller(TypePositionInfo info, StubCodeContext context)
        {
            return context.SingleFrameSpansNativeContext && !info.IsManagedReturnPosition && !info.IsByRef || info.RefKind == RefKind.In;
        }

        public TypeSyntax AsNativeType(TypePositionInfo info)
        {
            return _nativeValueType;
        }

        public IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context)
        {
            var subContext = new CustomNativeTypeWithToFromNativeValueContext(context);

            if (!context.AdditionalTemporaryStateLivesAcrossStages)
            {
                // <marshalerIdentifier>.Value = <nativeIdentifier>;
                yield return GenerateFromNativeValueInvocation(info, context, subContext);
            }

            foreach (StatementSyntax statement in _innerMarshaller.GenerateCleanupStatements(info, subContext))
            {
                yield return statement;
            }
        }

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context, IEnumerable<ArgumentSyntax> nativeTypeConstructorArguments)
        {
            var subContext = new CustomNativeTypeWithToFromNativeValueContext(context);
            foreach (StatementSyntax statement in _innerMarshaller.GenerateMarshalStatements(info, subContext, nativeTypeConstructorArguments))
            {
                yield return statement;
            }

            if (!CanPinMarshaller(info, context))
                yield return GenerateToNativeValueInvocation(info, context, subContext);
        }

        private static StatementSyntax GenerateToNativeValueInvocation(TypePositionInfo info, StubCodeContext context, CustomNativeTypeWithToFromNativeValueContext subContext)
        {
            // <nativeIdentifier> = <marshalerIdentifier>.ToNativeValue();
            return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(context.GetIdentifiers(info).native),
                    InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName(subContext.GetIdentifiers(info).native),
                            IdentifierName(ShapeMemberNames.Value.ToNativeValue)),
                        ArgumentList())));
        }

        public IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubCodeContext context)
        {
            // fixed (<_nativeTypeSyntax> <ignoredIdentifier> = &<marshalerIdentifier>)
            //  <assignment to Value property>
            var subContext = new CustomNativeTypeWithToFromNativeValueContext(context);
            yield return FixedStatement(
                VariableDeclaration(
                _nativeValueType,
                SingletonSeparatedList(
                    VariableDeclarator(Identifier(context.GetAdditionalIdentifier(info, "ignored")))
                        .WithInitializer(EqualsValueClause(
                            IdentifierName(subContext.GetIdentifiers(info).native))))),
                GenerateToNativeValueInvocation(info, context, subContext));
        }

        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context)
        {
            var subContext = new CustomNativeTypeWithToFromNativeValueContext(context);
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

        private static StatementSyntax GenerateFromNativeValueInvocation(TypePositionInfo info, StubCodeContext context, CustomNativeTypeWithToFromNativeValueContext subContext)
        {
            // <marshalerIdentifier>.FromNativeValue(<nativeIdentifier>);
            return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(subContext.GetIdentifiers(info).native),
                        IdentifierName(ShapeMemberNames.Value.FromNativeValue)),
                    ArgumentList(SingletonSeparatedList(Argument(IdentifierName(context.GetIdentifiers(info).native))))));
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            var subContext = new CustomNativeTypeWithToFromNativeValueContext(context);

            if (info.IsManagedReturnPosition || (info.IsByRef && info.RefKind != RefKind.In))
            {
                // <marshalerIdentifier>.Value = <nativeIdentifier>;
                yield return GenerateFromNativeValueInvocation(info, context, subContext);
            }

            foreach (StatementSyntax statement in _innerMarshaller.GenerateUnmarshalStatements(info, subContext))
            {
                yield return statement;
            }
        }

        public IEnumerable<ArgumentSyntax> GetNativeTypeConstructorArguments(TypePositionInfo info, StubCodeContext context)
        {
            var subContext = new CustomNativeTypeWithToFromNativeValueContext(context);
            return _innerMarshaller.GetNativeTypeConstructorArguments(info, subContext);
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context)
        {
            return _innerMarshaller.UsesNativeIdentifier(info, context);
        }
    }

    /// <summary>
    /// Marshaller that enables support for native types with the constructor variants that take a sizeOfElement int parameter.
    /// </summary>
    internal sealed class SizeOfElementMarshalling : ICustomNativeTypeMarshallingStrategy
    {
        private readonly ICustomNativeTypeMarshallingStrategy _innerMarshaller;
        private readonly ExpressionSyntax _sizeOfElementExpression;

        public SizeOfElementMarshalling(ICustomNativeTypeMarshallingStrategy innerMarshaller, ExpressionSyntax sizeOfElementExpression)
        {
            _innerMarshaller = innerMarshaller;
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
    /// Marshaller that enables support for marshalling blittable elements of a collection via a native type that implements the LinearCollection marshalling spec.
    /// </summary>
    internal sealed class LinearCollectionWithBlittableElementsMarshalling : ICustomNativeTypeMarshallingStrategy
    {
        private readonly ICustomNativeTypeMarshallingStrategy _innerMarshaller;
        private readonly TypeSyntax _elementType;
        private readonly ExpressionSyntax _numElementsExpression;

        public LinearCollectionWithBlittableElementsMarshalling(ICustomNativeTypeMarshallingStrategy innerMarshaller, TypeSyntax elementType, ExpressionSyntax numElementsExpression)
        {
            _innerMarshaller = innerMarshaller;
            _elementType = elementType;
            _numElementsExpression = numElementsExpression;
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

            // <nativeIdentifier>.GetManagedValuesSource().CopyTo(MemoryMarshal.Cast<byte, <elementType>>(<nativeIdentifier>.GetNativeValuesDestination()));
            yield return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName(nativeIdentifier),
                                IdentifierName(ShapeMemberNames.LinearCollection.GetManagedValuesSource)),
                        ArgumentList()),
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
                                InvocationExpression(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName(nativeIdentifier),
                                        IdentifierName(ShapeMemberNames.LinearCollection.GetNativeValuesDestination)),
                                    ArgumentList()))))));
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
            string numElementsIdentifier = context.GetAdditionalIdentifier(info, "numElements");
            yield return LocalDeclarationStatement(
                VariableDeclaration(
                    PredefinedType(Token(SyntaxKind.IntKeyword)),
                    SingletonSeparatedList(
                        VariableDeclarator(numElementsIdentifier).WithInitializer(EqualsValueClause(_numElementsExpression)))));
            // MemoryMarshal.Cast<byte, <elementType>>(<nativeIdentifier>.GetNativeValuesSource(<numElements>)).CopyTo(<nativeIdentifier>.GetManagedValuesDestination(<numElements>));
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
                                    InvocationExpression(
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName(nativeIdentifier),
                                            IdentifierName(ShapeMemberNames.LinearCollection.GetNativeValuesSource)),
                                        ArgumentList(SingletonSeparatedList(Argument(IdentifierName(numElementsIdentifier))))))),
                        IdentifierName("CopyTo")))
                .AddArgumentListArguments(
                    Argument(
                        InvocationExpression(
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName(nativeIdentifier),
                                            IdentifierName(ShapeMemberNames.LinearCollection.GetManagedValuesDestination)),
                                        ArgumentList(SingletonSeparatedList(Argument(IdentifierName(numElementsIdentifier))))))));

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
    /// Marshaller that enables support for marshalling non-blittable elements of a collection via a native type that implements the LinearCollection marshalling spec.
    /// </summary>
    internal sealed class LinearCollectionWithNonBlittableElementsMarshalling : ICustomNativeTypeMarshallingStrategy
    {
        private readonly ICustomNativeTypeMarshallingStrategy _innerMarshaller;
        private readonly IMarshallingGenerator _elementMarshaller;
        private readonly TypePositionInfo _elementInfo;
        private readonly ExpressionSyntax _numElementsExpression;

        public LinearCollectionWithNonBlittableElementsMarshalling(ICustomNativeTypeMarshallingStrategy innerMarshaller,
            IMarshallingGenerator elementMarshaller,
            TypePositionInfo elementInfo,
            ExpressionSyntax numElementsExpression)
        {
            _innerMarshaller = innerMarshaller;
            _elementMarshaller = elementMarshaller;
            _elementInfo = elementInfo;
            _numElementsExpression = numElementsExpression;
        }

        private LocalDeclarationStatementSyntax GenerateNativeValuesDestinationDeclaration(TypePositionInfo info, StubCodeContext context)
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
                            Argument(
                                InvocationExpression(
                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName(nativeIdentifier),
                                        IdentifierName(ShapeMemberNames.LinearCollection.GetNativeValuesDestination)),
                                    ArgumentList()))))))));
        }

        private LocalDeclarationStatementSyntax GenerateNativeValuesSourceDeclaration(TypePositionInfo info, StubCodeContext context, string numElementsIdentifier)
        {
            string nativeIdentifier = context.GetIdentifiers(info).native;
            string nativeSpanIdentifier = MarshallerHelpers.GetNativeSpanIdentifier(info, context);
            return LocalDeclarationStatement(VariableDeclaration(
                GenericName(
                    Identifier(TypeNames.System_ReadOnlySpan),
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
                            Argument(
                                InvocationExpression(
                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName(nativeIdentifier),
                                        IdentifierName(ShapeMemberNames.LinearCollection.GetNativeValuesSource)),
                                    ArgumentList(SingletonSeparatedList(Argument(IdentifierName(numElementsIdentifier))))))))))));
        }

        private LocalDeclarationStatementSyntax GeneratedManagedValuesSourceDeclaration(TypePositionInfo info, StubCodeContext context)
        {
            string nativeIdentifier = context.GetIdentifiers(info).native;
            string managedSpanIdentifier = MarshallerHelpers.GetManagedSpanIdentifier(info, context);
            return LocalDeclarationStatement(VariableDeclaration(
                GenericName(
                    Identifier(TypeNames.System_ReadOnlySpan),
                    TypeArgumentList(
                        SingletonSeparatedList(_elementInfo.ManagedType.Syntax))
                ),
                SingletonSeparatedList(
                    VariableDeclarator(Identifier(managedSpanIdentifier))
                    .WithInitializer(EqualsValueClause(
                        InvocationExpression(
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName(nativeIdentifier),
                                IdentifierName(ShapeMemberNames.LinearCollection.GetManagedValuesSource)),
                            ArgumentList()))))));
        }

        private LocalDeclarationStatementSyntax GeneratedManagedValuesDestinationDeclaration(TypePositionInfo info, StubCodeContext context, string numElementsIdentifier)
        {
            string nativeIdentifier = context.GetIdentifiers(info).native;
            string managedSpanIdentifier = MarshallerHelpers.GetManagedSpanIdentifier(info, context);
            return LocalDeclarationStatement(VariableDeclaration(
                GenericName(
                    Identifier(TypeNames.System_Span),
                    TypeArgumentList(
                        SingletonSeparatedList(_elementInfo.ManagedType.Syntax))
                ),
                SingletonSeparatedList(
                    VariableDeclarator(Identifier(managedSpanIdentifier))
                    .WithInitializer(EqualsValueClause(
                        InvocationExpression(
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName(nativeIdentifier),
                                IdentifierName(ShapeMemberNames.LinearCollection.GetManagedValuesDestination)),
                            ArgumentList(SingletonSeparatedList(Argument(IdentifierName(numElementsIdentifier))))))))));
        }

        private StatementSyntax GenerateContentsMarshallingStatement(TypePositionInfo info, StubCodeContext context, ExpressionSyntax lengthExpression)
        {
            string managedSpanIdentifier = MarshallerHelpers.GetManagedSpanIdentifier(info, context);
            string nativeSpanIdentifier = MarshallerHelpers.GetNativeSpanIdentifier(info, context);
            var elementSetupSubContext = new LinearCollectionElementMarshallingCodeContext(
                StubCodeContext.Stage.Setup,
                managedSpanIdentifier,
                nativeSpanIdentifier,
                context);
            var elementSubContext = new LinearCollectionElementMarshallingCodeContext(
                context.CurrentStage,
                managedSpanIdentifier,
                nativeSpanIdentifier,
                context);

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
                return MarshallerHelpers.GetForLoop(lengthExpression, elementSubContext.IndexerIdentifier)
                                    .WithStatement(marshallingStatement);
            }
            return EmptyStatement();
        }

        public TypeSyntax AsNativeType(TypePositionInfo info)
        {
            return _innerMarshaller.AsNativeType(info);
        }

        public IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context)
        {
            StatementSyntax contentsCleanupStatements = GenerateContentsMarshallingStatement(info, context,
                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName(MarshallerHelpers.GetNativeSpanIdentifier(info, context)),
                                    IdentifierName("Length")));

            if (!contentsCleanupStatements.IsKind(SyntaxKind.EmptyStatement))
            {
                yield return Block(
                    GenerateNativeValuesDestinationDeclaration(info, context),
                    contentsCleanupStatements);
            }

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
                // <nativeIdentifier>.GetNativeValuesDestination().Clear();
                string nativeIdentifier = context.GetIdentifiers(info).native;
                yield return ExpressionStatement(
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            InvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName(nativeIdentifier),
                                    IdentifierName(ShapeMemberNames.LinearCollection.GetNativeValuesDestination)),
                                ArgumentList()),
                            IdentifierName("Clear"))));
                yield break;
            }

            yield return Block(
                GeneratedManagedValuesSourceDeclaration(info, context),
                GenerateNativeValuesDestinationDeclaration(info, context),
                GenerateContentsMarshallingStatement(info, context,
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(MarshallerHelpers.GetManagedSpanIdentifier(info, context)),
                        IdentifierName("Length"))));
        }

        public IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubCodeContext context)
        {
            return _innerMarshaller.GeneratePinStatements(info, context);
        }

        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context)
        {
            return _innerMarshaller.GenerateSetupStatements(info, context);
        }

        private StatementSyntax GenerateByValueUnmarshalStatement(TypePositionInfo info, StubCodeContext context)
        {
            // Use ManagedSource and NativeDestination spans for by-value marshalling since we're just marshalling back the contents,
            // not the array itself.
            // This code is ugly since we're now enforcing readonly safety with ReadOnlySpan for all other scenarios,
            // but this is an uncommon case so we don't want to design the API around enabling just it.
            var (_, nativeIdentifier) = context.GetIdentifiers(info);
            string numElementsIdentifier = context.GetAdditionalIdentifier(info, "numElements");
            // int <numElements> = <nativeIdentifier>.GetManagedValuesSource().Length;
            LocalDeclarationStatementSyntax numElementsDeclaration = LocalDeclarationStatement(
                VariableDeclaration(
                    PredefinedType(Token(SyntaxKind.IntKeyword)),
                    SingletonSeparatedList(
                        VariableDeclarator(numElementsIdentifier).WithInitializer(EqualsValueClause(
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                InvocationExpression(
                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName(nativeIdentifier),
                                        IdentifierName(ShapeMemberNames.LinearCollection.GetManagedValuesSource)),
                                    ArgumentList()),
                                IdentifierName("Length")))))));

            string managedSpanIdentifier = MarshallerHelpers.GetManagedSpanIdentifier(info, context);
            // Span<TElement> <managedSpan> = MemoryMarshal.CreateSpan(ref Unsafe.AsRef(ref <nativeIdentifier>.GetManagedValuesSource().GetPinnableReference(), <numElements>));
            LocalDeclarationStatementSyntax managedValuesDeclaration = LocalDeclarationStatement(VariableDeclaration(
                GenericName(
                    Identifier(TypeNames.System_Span),
                    TypeArgumentList(
                        SingletonSeparatedList(_elementInfo.ManagedType.Syntax))
                ),
                SingletonSeparatedList(VariableDeclarator(managedSpanIdentifier).WithInitializer(EqualsValueClause(
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            ParseName(TypeNames.System_Runtime_InteropServices_MemoryMarshal),
                            IdentifierName("CreateSpan")))
                    .WithArgumentList(
                        ArgumentList(
                            SeparatedList(
                                new[]
                                {
                                    Argument(
                                        InvocationExpression(
                                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                ParseName(TypeNames.System_Runtime_CompilerServices_Unsafe),
                                                IdentifierName("AsRef")),
                                            ArgumentList(SingletonSeparatedList(
                                                Argument(
                                                    InvocationExpression(
                                                        MemberAccessExpression(
                                                            SyntaxKind.SimpleMemberAccessExpression,
                                                            InvocationExpression(
                                                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                                    IdentifierName(nativeIdentifier),
                                                                    IdentifierName(ShapeMemberNames.LinearCollection.GetManagedValuesSource)),
                                                                ArgumentList()),
                                                            IdentifierName("GetPinnableReference")),
                                                            ArgumentList()))
                                                .WithRefKindKeyword(
                                                    Token(SyntaxKind.InKeyword))))))
                                    .WithRefKindKeyword(
                                        Token(SyntaxKind.RefKeyword)),
                                    Argument(
                                        IdentifierName(numElementsIdentifier))
                                }))))))));

            LocalDeclarationStatementSyntax nativeValuesDeclaration = GenerateNativeValuesDestinationDeclaration(info, context);

            return Block(numElementsDeclaration,
                managedValuesDeclaration,
                nativeValuesDeclaration,
                GenerateContentsMarshallingStatement(info, context,
                    IdentifierName(numElementsIdentifier)));
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            string numElementsIdentifier = context.GetAdditionalIdentifier(info, "numElements");
            if (!info.IsByRef && info.ByValueContentsMarshalKind.HasFlag(ByValueContentsMarshalKind.Out))
            {
                yield return GenerateByValueUnmarshalStatement(info, context);
            }
            else
            {
                yield return Block(LocalDeclarationStatement(
                    VariableDeclaration(
                        PredefinedType(Token(SyntaxKind.IntKeyword)),
                        SingletonSeparatedList(
                            VariableDeclarator(numElementsIdentifier).WithInitializer(EqualsValueClause(_numElementsExpression))))),
                    GeneratedManagedValuesDestinationDeclaration(info, context, numElementsIdentifier),
                    GenerateNativeValuesSourceDeclaration(info, context, numElementsIdentifier),
                    GenerateContentsMarshallingStatement(info, context,
                        IdentifierName(numElementsIdentifier)));
            }

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

            public override SyntaxNode? VisitArgument(ArgumentSyntax node)
            {
                if (node.Expression.ToString() == _nativeIdentifier)
                {
                    return node.WithExpression(
                        CastExpression(_nativeType, node.Expression));
                }
                return node;
            }
        }
    }
}
