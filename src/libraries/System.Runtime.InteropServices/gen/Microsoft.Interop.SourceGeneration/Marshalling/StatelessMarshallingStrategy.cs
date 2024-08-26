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
    /// <summary>
    /// Stateless marshalling support for a type that has a custom unmanaged type.
    /// </summary>
    internal sealed class StatelessValueMarshalling(TypePositionInfo info, StubCodeContext codeContext, TypeSyntax marshallerTypeSyntax, ManagedTypeInfo unmanagedType, MarshallerShape shape) : ICustomTypeMarshallingStrategy
    {
        public ManagedTypeInfo NativeType => unmanagedType;

        public bool UsesNativeIdentifier => true;

        public TypePositionInfo TypeInfo => info;

        public StubCodeContext CodeContext => codeContext;

        public IEnumerable<StatementSyntax> GenerateCleanupCallerAllocatedResourcesStatements(StubIdentifierContext context) => Array.Empty<StatementSyntax>();

        public IEnumerable<StatementSyntax> GenerateCleanupCalleeAllocatedResourcesStatements(StubIdentifierContext context) => Array.Empty<StatementSyntax>();

        public IEnumerable<StatementSyntax> GenerateGuaranteedUnmarshalStatements(StubIdentifierContext context)
        {
            if (!shape.HasFlag(MarshallerShape.GuaranteedUnmarshal))
                yield break;

            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);

            // <managedIdentifier> = <marshallerType>.ConvertToManagedFinally(<nativeIdentifier>);
            yield return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(managedIdentifier),
                    InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            marshallerTypeSyntax,
                            IdentifierName(ShapeMemberNames.Value.Stateless.ConvertToManagedFinally)),
                        ArgumentList(SingletonSeparatedList(
                            Argument(IdentifierName(nativeIdentifier)))))));
        }

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(StubIdentifierContext context)
        {
            if (!shape.HasFlag(MarshallerShape.ToUnmanaged) && !shape.HasFlag(MarshallerShape.CallerAllocatedBuffer))
                yield break;

            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);

            // <marshallerType>.ConvertToUnmanaged(<managedIdentifier>)
            ExpressionSyntax convertToUnmanaged = InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            marshallerTypeSyntax,
                            IdentifierName(ShapeMemberNames.Value.Stateless.ConvertToUnmanaged)),
                        ArgumentList(SingletonSeparatedList(
                            Argument(IdentifierName(managedIdentifier)))));

            if (unmanagedType == SpecialTypeInfo.Void)
            {
                // To support our exception marshalling scenarios, we allow a "marshal to void" marshaller.
                // In this case, we don't assign the result to the native identifier because there is no native identifier.
                // <convertToUnmanaged>
                yield return ExpressionStatement(convertToUnmanaged);
                yield break;
            }

            // <nativeIdentifier> = <convertToUnmanaged>;
            var assignment = AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(nativeIdentifier),
                    convertToUnmanaged);


            if (unmanagedType is PointerTypeInfo pointer)
            {
                var rewriter = new PointerNativeTypeAssignmentRewriter(assignment.Right.ToString(), (PointerTypeSyntax)pointer.Syntax);
                assignment = (AssignmentExpressionSyntax)rewriter.Visit(assignment);
            }
            yield return ExpressionStatement(assignment);
        }

        public IEnumerable<StatementSyntax> GeneratePinnedMarshalStatements(StubIdentifierContext context)
        {
            return Array.Empty<StatementSyntax>();
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(StubIdentifierContext context)
        {
            if (!shape.HasFlag(MarshallerShape.ToManaged))
                yield break;

            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);

            // <managedIdentifier> = <marshallerType>.ConvertToManaged(<nativeIdentifier>);
            yield return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(managedIdentifier),
                    InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            marshallerTypeSyntax,
                            IdentifierName(ShapeMemberNames.Value.Stateless.ConvertToManaged)),
                        ArgumentList(SingletonSeparatedList(
                            Argument(IdentifierName(nativeIdentifier)))))));
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalCaptureStatements(StubIdentifierContext context)
        {
            return Array.Empty<StatementSyntax>();
        }

        public IEnumerable<StatementSyntax> GenerateSetupStatements(StubIdentifierContext context)
        {
            return Array.Empty<StatementSyntax>();
        }

        public IEnumerable<StatementSyntax> GeneratePinStatements(StubIdentifierContext context)
        {
            return Array.Empty<StatementSyntax>();
        }

        public IEnumerable<StatementSyntax> GenerateNotifyForSuccessfulInvokeStatements(StubIdentifierContext context)
        {
            return Array.Empty<StatementSyntax>();
        }
    }

    /// <summary>
    /// Marshaller that enables support for a stackalloc constructor variant on a native type.
    /// </summary>
    internal sealed class StatelessCallerAllocatedBufferMarshalling(ICustomTypeMarshallingStrategy innerMarshaller, TypeSyntax marshallerType, TypeSyntax bufferElementType, bool isLinearCollectionMarshalling) : ICustomTypeMarshallingStrategy
    {
        public ManagedTypeInfo NativeType => innerMarshaller.NativeType;
        public TypePositionInfo TypeInfo => innerMarshaller.TypeInfo;
        public StubCodeContext CodeContext => innerMarshaller.CodeContext;
        public IEnumerable<StatementSyntax> GenerateCleanupCallerAllocatedResourcesStatements(StubIdentifierContext context) => innerMarshaller.GenerateCleanupCallerAllocatedResourcesStatements(context);
        public IEnumerable<StatementSyntax> GenerateCleanupCalleeAllocatedResourcesStatements(StubIdentifierContext context) => innerMarshaller.GenerateCleanupCalleeAllocatedResourcesStatements(context);
        public IEnumerable<StatementSyntax> GenerateGuaranteedUnmarshalStatements(StubIdentifierContext context) => innerMarshaller.GenerateGuaranteedUnmarshalStatements(context);

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(StubIdentifierContext context)
        {
            if (MarshallerHelpers.CanUseCallerAllocatedBuffer(TypeInfo, CodeContext))
            {
                return GenerateCallerAllocatedBufferMarshalStatements();
            }

            return innerMarshaller.GenerateMarshalStatements(context);

            IEnumerable<StatementSyntax> GenerateCallerAllocatedBufferMarshalStatements()
            {
                string bufferIdentifier = context.GetAdditionalIdentifier(TypeInfo, "buffer");

                // Span<bufferElementType> <bufferIdentifier> = stackalloc <bufferElementType>[<marshallerType>.BufferSize];
                yield return LocalDeclarationStatement(
                    VariableDeclaration(
                        GenericName(
                            Identifier(TypeNames.System_Span),
                            TypeArgumentList(
                                SingletonSeparatedList(bufferElementType))),
                        SingletonSeparatedList(
                            VariableDeclarator(bufferIdentifier)
                                .WithInitializer(EqualsValueClause(
                                    StackAllocArrayCreationExpression(
                                        ArrayType(
                                            bufferElementType,
                                            SingletonList(ArrayRankSpecifier(SingletonSeparatedList<ExpressionSyntax>(
                                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                    marshallerType,
                                                    IdentifierName(ShapeMemberNames.BufferSize))
                                            ))))))))));

                (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(TypeInfo);

                if (isLinearCollectionMarshalling)
                {
                    string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(TypeInfo, context);

                    // <nativeIdentifier> = <marshallerType>.AllocateContainerForUnmanagedElements(<managedIdentifier>, <bufferIdentifier>, out <numElements>);
                    yield return ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(nativeIdentifier),
                            InvocationExpression(
                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                    marshallerType,
                                    IdentifierName(ShapeMemberNames.LinearCollection.Stateless.AllocateContainerForUnmanagedElements)),
                                ArgumentList(SeparatedList(new ArgumentSyntax[]
                                {
                                Argument(IdentifierName(managedIdentifier)),
                                Argument(IdentifierName(bufferIdentifier)),
                                Argument(IdentifierName(numElementsIdentifier))
                                    .WithRefOrOutKeyword(Token(SyntaxKind.OutKeyword))
                                })))));

                    // Linear collections have additional marshalling required using the inner marshaller
                    foreach (StatementSyntax statement in innerMarshaller.GenerateMarshalStatements(context))
                    {
                        yield return statement;
                    }
                }
                else
                {
                    // <nativeIdentifier> = <marshallerType>.ConvertToUnmanaged(<managedIdentifier>, <nativeIdentifier>__buffer);
                    yield return ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(nativeIdentifier),
                            InvocationExpression(
                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                    marshallerType,
                                    IdentifierName(ShapeMemberNames.Value.Stateless.ConvertToUnmanaged)),
                                ArgumentList(SeparatedList(new ArgumentSyntax[]
                                {
                                    Argument(IdentifierName(managedIdentifier)),
                                    Argument(IdentifierName(bufferIdentifier))
                                })))));
                }
            }
        }

        public IEnumerable<StatementSyntax> GeneratePinnedMarshalStatements(StubIdentifierContext context) => innerMarshaller.GeneratePinnedMarshalStatements(context);
        public IEnumerable<StatementSyntax> GeneratePinStatements(StubIdentifierContext context) => innerMarshaller.GeneratePinStatements(context);
        public IEnumerable<StatementSyntax> GenerateSetupStatements(StubIdentifierContext context) => innerMarshaller.GenerateSetupStatements(context);
        public IEnumerable<StatementSyntax> GenerateUnmarshalCaptureStatements(StubIdentifierContext context) => innerMarshaller.GenerateUnmarshalCaptureStatements(context);
        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(StubIdentifierContext context) => innerMarshaller.GenerateUnmarshalStatements(context);
        public bool UsesNativeIdentifier => innerMarshaller.UsesNativeIdentifier;

        public IEnumerable<StatementSyntax> GenerateNotifyForSuccessfulInvokeStatements(StubIdentifierContext context) => innerMarshaller.GenerateNotifyForSuccessfulInvokeStatements(context);
    }

    internal sealed class StatelessFreeMarshalling(ICustomTypeMarshallingStrategy innerMarshaller, TypeSyntax marshallerType) : ICustomTypeMarshallingStrategy
    {
        public bool UsesNativeIdentifier => innerMarshaller.UsesNativeIdentifier;

        public TypePositionInfo TypeInfo => innerMarshaller.TypeInfo;

        public StubCodeContext CodeContext => innerMarshaller.CodeContext;

        public ManagedTypeInfo NativeType => innerMarshaller.NativeType;

        public IEnumerable<StatementSyntax> GenerateCleanupCallerAllocatedResourcesStatements(StubIdentifierContext context)
        {
            if (MarshallerHelpers.GetCleanupStage(TypeInfo, CodeContext) is not StubIdentifierContext.Stage.CleanupCallerAllocated)
                yield break;

            foreach (StatementSyntax statement in innerMarshaller.GenerateCleanupCallerAllocatedResourcesStatements(context))
            {
                yield return statement;
            }
            // <marshallerType>.Free(<nativeIdentifier>);
            yield return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        marshallerType,
                        IdentifierName(ShapeMemberNames.Free)),
                    ArgumentList(SingletonSeparatedList(
                        Argument(IdentifierName(context.GetIdentifiers(TypeInfo).native))))));
        }

        public IEnumerable<StatementSyntax> GenerateCleanupCalleeAllocatedResourcesStatements(StubIdentifierContext context)
        {
            if (MarshallerHelpers.GetCleanupStage(TypeInfo, CodeContext) is not StubIdentifierContext.Stage.CleanupCalleeAllocated)
                yield break;

            foreach (StatementSyntax statement in innerMarshaller.GenerateCleanupCalleeAllocatedResourcesStatements(context))
            {
                yield return statement;
            }
            // <marshallerType>.Free(<nativeIdentifier>);
            yield return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        marshallerType,
                        IdentifierName(ShapeMemberNames.Free)),
                    ArgumentList(SingletonSeparatedList(
                        Argument(IdentifierName(context.GetIdentifiers(TypeInfo).native))))));
        }

        public IEnumerable<StatementSyntax> GenerateGuaranteedUnmarshalStatements(StubIdentifierContext context) => innerMarshaller.GenerateGuaranteedUnmarshalStatements(context);
        public IEnumerable<StatementSyntax> GenerateMarshalStatements(StubIdentifierContext context) => innerMarshaller.GenerateMarshalStatements(context);
        public IEnumerable<StatementSyntax> GenerateNotifyForSuccessfulInvokeStatements(StubIdentifierContext context) => innerMarshaller.GenerateNotifyForSuccessfulInvokeStatements(context);
        public IEnumerable<StatementSyntax> GeneratePinnedMarshalStatements(StubIdentifierContext context) => innerMarshaller.GeneratePinnedMarshalStatements(context);
        public IEnumerable<StatementSyntax> GeneratePinStatements(StubIdentifierContext context) => innerMarshaller.GeneratePinStatements(context);
        public IEnumerable<StatementSyntax> GenerateSetupStatements(StubIdentifierContext context) => innerMarshaller.GenerateSetupStatements(context);
        public IEnumerable<StatementSyntax> GenerateUnmarshalCaptureStatements(StubIdentifierContext context) => innerMarshaller.GenerateUnmarshalCaptureStatements(context);
        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(StubIdentifierContext context) => innerMarshaller.GenerateUnmarshalStatements(context);
    }

    /// <summary>
    /// Marshaller type that enables allocating space for marshalling a linear collection using a marshaller that implements the LinearCollection marshalling spec.
    /// </summary>
    internal sealed class StatelessLinearCollectionSpaceAllocator(TypePositionInfo info, StubCodeContext codeContext, TypeSyntax marshallerTypeSyntax, ManagedTypeInfo unmanagedType, MarshallerShape shape, CountInfo countInfo, bool countInfoRequiresCast) : ICustomTypeMarshallingStrategy
    {
        public ManagedTypeInfo NativeType => unmanagedType;

        public TypePositionInfo TypeInfo => info;

        public StubCodeContext CodeContext => codeContext;

        public IEnumerable<StatementSyntax> GenerateCleanupCallerAllocatedResourcesStatements(StubIdentifierContext context)
        {
            // If we clean up in a different stage, we don't need to do anything here
            if (MarshallerHelpers.GetCleanupStage(TypeInfo, CodeContext) is not StubIdentifierContext.Stage.CleanupCallerAllocated)
                yield break;

            if (MarshallerHelpers.GetMarshalDirection(TypeInfo, CodeContext) != MarshalDirection.ManagedToUnmanaged)
            {
                // If we are marshalling from unmanaged to managed, we need to get the number of elements again.
                string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(TypeInfo, context);
                // <numElements> = <numElementsExpression>;
                yield return ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(numElementsIdentifier),
                        ElementsMarshalling.GenerateNumElementsExpression(countInfo, countInfoRequiresCast, CodeContext, context)));
            }
        }

        public IEnumerable<StatementSyntax> GenerateCleanupCalleeAllocatedResourcesStatements(StubIdentifierContext context)
        {
            if (MarshallerHelpers.GetCleanupStage(TypeInfo, CodeContext) is not StubIdentifierContext.Stage.CleanupCalleeAllocated)
                yield break;

            if (MarshallerHelpers.GetMarshalDirection(TypeInfo, CodeContext) == MarshalDirection.ManagedToUnmanaged)
            {
                yield return EmptyStatement();
                yield break;
            }

            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(TypeInfo, context);
            // <numElements> = <numElementsExpression>;
            yield return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(numElementsIdentifier),
                    ElementsMarshalling.GenerateNumElementsExpression(countInfo, countInfoRequiresCast, CodeContext, context)));
        }

        public IEnumerable<StatementSyntax> GenerateGuaranteedUnmarshalStatements(StubIdentifierContext context)
        {
            if (!shape.HasFlag(MarshallerShape.GuaranteedUnmarshal))
                yield break;

            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(TypeInfo);
            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(TypeInfo, context);

            yield return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(numElementsIdentifier),
                    ElementsMarshalling.GenerateNumElementsExpression(countInfo, countInfoRequiresCast, CodeContext, context)));

            // <managedIdentifier> = <marshallerType>.AllocateContainerForManagedElementsFinally(<nativeIdentifier>, <numElements>);
            yield return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(managedIdentifier),
                    InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            marshallerTypeSyntax,
                            IdentifierName(ShapeMemberNames.LinearCollection.Stateless.AllocateContainerForManagedElementsFinally)),
                        ArgumentList(SeparatedList(new ArgumentSyntax[]
                        {
                            Argument(IdentifierName(nativeIdentifier)),
                            Argument(IdentifierName(numElementsIdentifier))
                        })))));
        }

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(StubIdentifierContext context)
        {
            if (!shape.HasFlag(MarshallerShape.ToUnmanaged) && !shape.HasFlag(MarshallerShape.CallerAllocatedBuffer))
                yield break;

            if (shape.HasFlag(MarshallerShape.ToUnmanaged)
                && !(shape.HasFlag(MarshallerShape.CallerAllocatedBuffer)
                    && MarshallerHelpers.CanUseCallerAllocatedBuffer(TypeInfo, CodeContext)))
            {
                (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(TypeInfo);
                string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(TypeInfo, context);

                // <nativeIdentifier> = <marshallerType>.AllocateContainerForUnmanagedElements(<managedIdentifier>, out <numElements>);
                yield return ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(nativeIdentifier),
                        InvocationExpression(
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                marshallerTypeSyntax,
                                IdentifierName(ShapeMemberNames.LinearCollection.Stateless.AllocateContainerForUnmanagedElements)),
                            ArgumentList(SeparatedList(new ArgumentSyntax[]
                            {
                                Argument(IdentifierName(managedIdentifier)),
                                Argument(IdentifierName(numElementsIdentifier))
                                    .WithRefOrOutKeyword(Token(SyntaxKind.OutKeyword))
                            })))));
            }
        }

        public IEnumerable<StatementSyntax> GenerateNotifyForSuccessfulInvokeStatements(StubIdentifierContext context) => Array.Empty<StatementSyntax>();
        public IEnumerable<StatementSyntax> GeneratePinnedMarshalStatements(StubIdentifierContext context) => Array.Empty<StatementSyntax>();
        public IEnumerable<StatementSyntax> GeneratePinStatements(StubIdentifierContext context) => Array.Empty<StatementSyntax>();
        public IEnumerable<StatementSyntax> GenerateSetupStatements(StubIdentifierContext context)
        {
            // int <numElements>;
            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(TypeInfo, context);
            yield return LocalDeclarationStatement(
                VariableDeclaration(
                    PredefinedType(Token(SyntaxKind.IntKeyword)),
                    SingletonSeparatedList(
                        VariableDeclarator(numElementsIdentifier))));
            // Use the numElements local to ensure the compiler doesn't give errors for using an uninitialized variable.
            // The value may be used in cleanup before it has been initialized, so this is unsafe
            yield return MarshallerHelpers.SkipInitOrDefaultInit(
                new TypePositionInfo(SpecialTypeInfo.Int32, NoMarshallingInfo.Instance)
                {
                    InstanceIdentifier = numElementsIdentifier
                }, context);
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalCaptureStatements(StubIdentifierContext context) => Array.Empty<StatementSyntax>();

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(StubIdentifierContext context)
        {
            if (CodeContext.Direction == MarshalDirection.ManagedToUnmanaged && !TypeInfo.IsByRef && TypeInfo.ByValueContentsMarshalKind.HasFlag(ByValueContentsMarshalKind.Out))
            {
                yield break;
            }

            if (!shape.HasFlag(MarshallerShape.ToManaged))
                yield break;

            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(TypeInfo);
            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(TypeInfo, context);

            yield return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(numElementsIdentifier),
                    ElementsMarshalling.GenerateNumElementsExpression(countInfo, countInfoRequiresCast, CodeContext, context)));

            // <managedIdentifier> = <marshallerType>.AllocateContainerForManagedElements(<nativeIdentifier>, <numElements>);
            yield return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(managedIdentifier),
                    InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            marshallerTypeSyntax,
                            IdentifierName(ShapeMemberNames.LinearCollection.Stateless.AllocateContainerForManagedElements)),
                        ArgumentList(SeparatedList(new ArgumentSyntax[]
                        {
                            Argument(IdentifierName(nativeIdentifier)),
                            Argument(IdentifierName(numElementsIdentifier))
                        })))));
        }

        public bool UsesNativeIdentifier => true;
    }

    internal sealed class StatelessLinearCollectionSource(TypePositionInfo info, StubCodeContext codeContext, TypeSyntax marshallerTypeSyntax) : IElementsMarshallingCollectionSource
    {
        public TypePositionInfo TypeInfo => info;

        public StubCodeContext CodeContext => codeContext;

        public InvocationExpressionSyntax GetUnmanagedValuesDestination(StubIdentifierContext context)
        {
            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);
            string nativeIdentifier = context.GetIdentifiers(info).native;

            // <marshallerType>.GetUnmanagedValuesDestination(<nativeIdentifier>, <numElements>)
            return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    marshallerTypeSyntax,
                    IdentifierName(ShapeMemberNames.LinearCollection.Stateless.GetUnmanagedValuesDestination)),
                ArgumentList(SeparatedList(new ArgumentSyntax[]
                {
                    Argument(IdentifierName(nativeIdentifier)),
                    Argument(IdentifierName(numElementsIdentifier)),
                })));
        }

        public InvocationExpressionSyntax GetManagedValuesSource(StubIdentifierContext context)
        {
            // <marshallerType>.GetManagedValuesSource(<managedIdentifier>)
            string managedIdentifier = context.GetIdentifiers(info).managed;
            return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    marshallerTypeSyntax,
                    IdentifierName(ShapeMemberNames.LinearCollection.Stateless.GetManagedValuesSource)),
                ArgumentList(SingletonSeparatedList(
                    Argument(IdentifierName(managedIdentifier)))));
        }

        public InvocationExpressionSyntax GetUnmanagedValuesSource(StubIdentifierContext context)
        {
            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);
            string nativeIdentifier = context.GetIdentifiers(info).native;

            // <marshallerType>.GetUnmanagedValuesSource(<nativeIdentifier>, <numElements>)
            return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    marshallerTypeSyntax,
                    IdentifierName(ShapeMemberNames.LinearCollection.Stateless.GetUnmanagedValuesSource)),
                ArgumentList(SeparatedList(new ArgumentSyntax[]
                {
                        Argument(IdentifierName(nativeIdentifier)),
                        Argument(IdentifierName(numElementsIdentifier))
                })));
        }

        public InvocationExpressionSyntax GetManagedValuesDestination(StubIdentifierContext context)
        {
            string managedIdentifier = context.GetIdentifiers(info).managed;

            // <marshallerType>.GetManagedValuesDestination(<managedIdentifier>)
            return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    marshallerTypeSyntax,
                    IdentifierName(ShapeMemberNames.LinearCollection.Stateless.GetManagedValuesDestination)),
                ArgumentList(SingletonSeparatedList(Argument(IdentifierName(managedIdentifier)))));
        }
    }

    /// <summary>
    /// Marshaller that enables support for marshalling elements of a collection via a marshaller type that implements the contiguous collection marshalling shape.
    /// </summary>
    internal sealed class StatelessLinearCollectionMarshalling(
        ICustomTypeMarshallingStrategy spaceMarshallingStrategy,
        ElementsMarshalling elementsMarshalling,
        ManagedTypeInfo unmanagedType,
        MarshallerShape shape,
        CountInfo countInfo,
        bool castCountInfo,
        bool cleanupElementsAndSpace) : ICustomTypeMarshallingStrategy
    {
        public bool UsesNativeIdentifier => true;

        public TypePositionInfo TypeInfo => spaceMarshallingStrategy.TypeInfo;

        public StubCodeContext CodeContext => spaceMarshallingStrategy.CodeContext;

        public ManagedTypeInfo NativeType => unmanagedType;

        public IEnumerable<StatementSyntax> GenerateCleanupCallerAllocatedResourcesStatements(StubIdentifierContext context)
        {
            if (!cleanupElementsAndSpace)
            {
                yield break;
            }

            StatementSyntax elementCleanup = elementsMarshalling.GenerateElementCleanupStatement(context);

            if (!elementCleanup.IsKind(SyntaxKind.EmptyStatement))
            {
                // If we don't have the numElements variable still available from unmarshal or marshal stage, we need to reassign that again.
                // When marshalling from managed to unmanaged, we don't have a numElements variable because we can determine length
                // from the managed value.
                if (!CodeContext.AdditionalTemporaryStateLivesAcrossStages
                    && MarshallerHelpers.GetMarshalDirection(TypeInfo, CodeContext) != MarshalDirection.ManagedToUnmanaged)
                {
                    // <numElements> = <numElementsExpression>;
                    string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(TypeInfo, context);
                    yield return ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(numElementsIdentifier),
                            ElementsMarshalling.GenerateNumElementsExpression(countInfo, castCountInfo, CodeContext, context)));
                }
                yield return elementCleanup;
            }

            if (MarshallerHelpers.GetCleanupStage(TypeInfo, CodeContext) is StubIdentifierContext.Stage.CleanupCallerAllocated)
            {
                foreach (var statement in spaceMarshallingStrategy.GenerateCleanupCallerAllocatedResourcesStatements(context))
                {
                    yield return statement;
                }
            }
        }

        public IEnumerable<StatementSyntax> GenerateCleanupCalleeAllocatedResourcesStatements(StubIdentifierContext context)
        {
            if (!cleanupElementsAndSpace)
            {
                yield break;
            }
            StatementSyntax elementCleanup = elementsMarshalling.GenerateElementCleanupStatement(context);

            if (!elementCleanup.IsKind(SyntaxKind.EmptyStatement))
            {
                // If we don't have the numElements variable still available from unmarshal or marshal stage, we need to reassign that again
                if (!CodeContext.AdditionalTemporaryStateLivesAcrossStages)
                {
                    // <numElements> = <numElementsExpression>;
                    string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(TypeInfo, context);
                    yield return ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(numElementsIdentifier),
                            ElementsMarshalling.GenerateNumElementsExpression(countInfo, castCountInfo, CodeContext, context)));
                }
                yield return elementCleanup;
            }

            if (MarshallerHelpers.GetCleanupStage(TypeInfo, CodeContext) is StubIdentifierContext.Stage.CleanupCallerAllocated)
            {
                foreach (var statement in spaceMarshallingStrategy.GenerateCleanupCalleeAllocatedResourcesStatements(context))
                {
                    yield return statement;
                }
            }
        }

        public IEnumerable<StatementSyntax> GenerateGuaranteedUnmarshalStatements(StubIdentifierContext context) => spaceMarshallingStrategy.GenerateGuaranteedUnmarshalStatements(context);

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(StubIdentifierContext context)
        {
            if (CodeContext.Direction == MarshalDirection.ManagedToUnmanaged && !TypeInfo.IsByRef && TypeInfo.ByValueContentsMarshalKind == ByValueContentsMarshalKind.Out)
            {
                // If the parameter is marshalled by-value [Out], then we don't marshal the contents of the collection.
                // We do clear the span, so that if the invoke target doesn't fill it, we aren't left with undefined content.
                yield return elementsMarshalling.GenerateClearUnmanagedDestination(context);

                yield break;
            }

            if (CodeContext.Direction == MarshalDirection.UnmanagedToManaged && !TypeInfo.IsByRef && TypeInfo.ByValueContentsMarshalKind.HasFlag(ByValueContentsMarshalKind.Out))
            {
                // If the parameter is marshalled by-value [Out] or [In, Out], then we need to unmarshal the contents of the collection
                // into the passed-in collection value.
                yield return elementsMarshalling.GenerateUnmanagedToManagedByValueOutMarshalStatement(context);
                yield break;
            }

            foreach (var statement in spaceMarshallingStrategy.GenerateMarshalStatements(context))
            {
                yield return statement;
            }

            if (!shape.HasFlag(MarshallerShape.ToUnmanaged) && !shape.HasFlag(MarshallerShape.CallerAllocatedBuffer))
                yield break;

            yield return elementsMarshalling.GenerateMarshalStatement(context);
        }

        public IEnumerable<StatementSyntax> GenerateNotifyForSuccessfulInvokeStatements(StubIdentifierContext context) => spaceMarshallingStrategy.GenerateNotifyForSuccessfulInvokeStatements(context);
        public IEnumerable<StatementSyntax> GeneratePinnedMarshalStatements(StubIdentifierContext context) => spaceMarshallingStrategy.GeneratePinnedMarshalStatements(context);
        public IEnumerable<StatementSyntax> GeneratePinStatements(StubIdentifierContext context) => spaceMarshallingStrategy.GeneratePinStatements(context);

        public IEnumerable<StatementSyntax> GenerateSetupStatements(StubIdentifierContext context)
        {
            foreach (var s in spaceMarshallingStrategy.GenerateSetupStatements(context))
                yield return s;
            var elementsSetup = elementsMarshalling.GenerateSetupStatement(context);
            if (elementsSetup is not EmptyStatementSyntax)
            {
                yield return elementsSetup;
            }
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalCaptureStatements(StubIdentifierContext context) => Array.Empty<StatementSyntax>();

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(StubIdentifierContext context)
        {
            if (CodeContext.Direction == MarshalDirection.ManagedToUnmanaged && !TypeInfo.IsByRef && TypeInfo.ByValueContentsMarshalKind.HasFlag(ByValueContentsMarshalKind.Out))
            {
                // If the parameter is marshalled by-value [Out] or [In, Out], then we need to unmarshal the contents of the collection
                // into the passed-in collection value.
                yield return elementsMarshalling.GenerateManagedToUnmanagedByValueOutUnmarshalStatement(context);
                yield break;
            }

            if (CodeContext.Direction == MarshalDirection.UnmanagedToManaged && !TypeInfo.IsByRef && TypeInfo.ByValueContentsMarshalKind == ByValueContentsMarshalKind.Out)
            {
                // If the parameter is marshalled by-value [Out], then we don't marshal the contents of the collection.
                // We do clear the span, so that if the invoke target doesn't fill it, we aren't left with undefined content.
                yield return elementsMarshalling.GenerateClearManagedValuesDestination(context);
                foreach (var statement in spaceMarshallingStrategy.GenerateUnmarshalStatements(context))
                {
                    yield return statement;
                }
                yield break;
            }

            if (!shape.HasFlag(MarshallerShape.ToManaged))
            {
                yield break;
            }
            else
            {
                foreach (var statement in spaceMarshallingStrategy.GenerateUnmarshalStatements(context))
                {
                    yield return statement;
                }

                yield return elementsMarshalling.GenerateUnmarshalStatement(context);
            }
        }
    }
}
