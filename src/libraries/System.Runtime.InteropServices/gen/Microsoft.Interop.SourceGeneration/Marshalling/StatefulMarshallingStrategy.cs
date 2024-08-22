// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Microsoft.Interop.SyntaxFactoryExtensions;

namespace Microsoft.Interop
{
    internal sealed class StatefulValueMarshalling(TypePositionInfo info, StubCodeContext stubContext, ManagedTypeInfo marshallerType, ManagedTypeInfo unmanagedType, MarshallerShape shape) : ICustomTypeMarshallingStrategy
    {
        internal const string MarshallerIdentifier = "marshaller";

        public ManagedTypeInfo NativeType => unmanagedType;

        public bool UsesNativeIdentifier => true;

        public TypePositionInfo TypeInfo => info;

        public StubCodeContext CodeContext => stubContext;

        public IEnumerable<StatementSyntax> GenerateCleanupCallerAllocatedResourcesStatements(StubIdentifierContext context)
        {
            if (MarshallerHelpers.GetCleanupStage(info, stubContext) is not StubIdentifierContext.Stage.CleanupCallerAllocated)
                yield break;

            if (!shape.HasFlag(MarshallerShape.Free))
                yield break;

            // <marshaller>.Free();
            yield return MethodInvocationStatement(
                        IdentifierName(context.GetAdditionalIdentifier(info, MarshallerIdentifier)),
                        IdentifierName(ShapeMemberNames.Free));
        }

        public IEnumerable<StatementSyntax> GenerateCleanupCalleeAllocatedResourcesStatements(StubIdentifierContext context)
        {
            if (MarshallerHelpers.GetCleanupStage(info, stubContext) is not StubIdentifierContext.Stage.CleanupCalleeAllocated)
                yield break;

            if (!shape.HasFlag(MarshallerShape.Free))
                yield break;

            // <marshaller>.Free();
            yield return MethodInvocationStatement(
                        IdentifierName(context.GetAdditionalIdentifier(info, MarshallerIdentifier)),
                        IdentifierName(ShapeMemberNames.Free));
        }

        public IEnumerable<StatementSyntax> GenerateGuaranteedUnmarshalStatements(StubIdentifierContext context)
        {
            if (!shape.HasFlag(MarshallerShape.GuaranteedUnmarshal))
                yield break;

            (string managedIdentifier, _) = context.GetIdentifiers(info);

            // <managedIdentifier> = <marshaller>.ToManagedFinally();
            yield return AssignmentStatement(
                            IdentifierName(managedIdentifier),
                            MethodInvocation(
                                IdentifierName(context.GetAdditionalIdentifier(info, MarshallerIdentifier)),
                                IdentifierName(ShapeMemberNames.Value.Stateful.ToManagedFinally)));
        }

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(StubIdentifierContext context)
        {
            if (!shape.HasFlag(MarshallerShape.ToUnmanaged))
                yield break;

            (string managedIdentifier, _) = context.GetIdentifiers(info);

            // <marshaller>.FromManaged(<managedIdentifier>);
            yield return MethodInvocationStatement(
                        IdentifierName(context.GetAdditionalIdentifier(info, MarshallerIdentifier)),
                        IdentifierName(ShapeMemberNames.Value.Stateful.FromManaged),
                        Argument(IdentifierName(managedIdentifier)));
        }

        public IEnumerable<StatementSyntax> GeneratePinnedMarshalStatements(StubIdentifierContext context)
        {
            if (!shape.HasFlag(MarshallerShape.ToUnmanaged) && !shape.HasFlag(MarshallerShape.CallerAllocatedBuffer))
                yield break;

            (_, string nativeIdentifier) = context.GetIdentifiers(info);

            // <nativeIdentifier> = <marshaller>.ToUnmanaged();
            yield return AssignmentStatement(
                            IdentifierName(nativeIdentifier),
                            MethodInvocation(
                                IdentifierName(context.GetAdditionalIdentifier(info, MarshallerIdentifier)),
                                IdentifierName(ShapeMemberNames.Value.Stateful.ToUnmanaged)));
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(StubIdentifierContext context)
        {
            if (!shape.HasFlag(MarshallerShape.ToManaged))
                yield break;

            (string managedIdentifier, _) = context.GetIdentifiers(info);

            // <managedIdentifier> = <marshaller>.ToManaged();
            yield return AssignmentStatement(
                            IdentifierName(managedIdentifier),
                            MethodInvocation(
                                IdentifierName(context.GetAdditionalIdentifier(info, MarshallerIdentifier)),
                                IdentifierName(ShapeMemberNames.Value.Stateful.ToManaged)));
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalCaptureStatements(StubIdentifierContext context)
        {
            if (!shape.HasFlag(MarshallerShape.ToManaged) && !shape.HasFlag(MarshallerShape.GuaranteedUnmarshal))
                yield break;

            (_, string nativeIdentifier) = context.GetIdentifiers(info);

            // <marshaller>.FromUnmanaged(<nativeIdentifier>);
            yield return MethodInvocationStatement(
                        IdentifierName(context.GetAdditionalIdentifier(info, MarshallerIdentifier)),
                        IdentifierName(ShapeMemberNames.Value.Stateful.FromUnmanaged),
                        Argument(IdentifierName(nativeIdentifier)));
        }

        public IEnumerable<StatementSyntax> GenerateSetupStatements(StubIdentifierContext context)
        {
            // <marshaller> = new();
            LocalDeclarationStatementSyntax declaration = Declare(
                marshallerType.Syntax,
                context.GetAdditionalIdentifier(info, MarshallerIdentifier),
                ImplicitObjectCreationExpression(ArgumentList(), initializer: null));

            // For byref-like marshaller types, we'll mark them as scoped.
            // Byref-like types can capture references, so by default the compiler has to worry that
            // they could enable those references to escape the current stack frame.
            // In particular, this can interact poorly with the caller-allocated-buffer marshalling
            // support and make the simple `marshaller.FromManaged(managed, stackalloc X[i])` expression
            // illegal. Mark the marshaller type as scoped so the compiler knows that it won't escape.
            if (marshallerType is ValueTypeInfo { IsByRefLike: true })
            {
                declaration = declaration.AddModifiers(Token(SyntaxKind.ScopedKeyword));
            }

            yield return declaration;
        }

        public IEnumerable<StatementSyntax> GeneratePinStatements(StubIdentifierContext context)
        {
            if (!shape.HasFlag(MarshallerShape.StatefulPinnableReference))
                yield break;

            string unusedIdentifier = context.GetAdditionalIdentifier(info, "unused");
            // fixed(void* <unused> = <marshaller>) ;
            yield return FixedStatement(
                VariableDeclaration(
                    TypeSyntaxes.VoidStar,
                    SingletonSeparatedList(
                        VariableDeclarator(unusedIdentifier)
                            .WithInitializer(EqualsValueClause(IdentifierName(context.GetAdditionalIdentifier(info, MarshallerIdentifier)))))),
                EmptyStatement());
        }

        public IEnumerable<StatementSyntax> GenerateNotifyForSuccessfulInvokeStatements(StubIdentifierContext context)
        {
            if (!shape.HasFlag(MarshallerShape.OnInvoked))
                yield break;

            // <marshaller>.OnInvoked();
            yield return MethodInvocationStatement(
                        IdentifierName(context.GetAdditionalIdentifier(info, MarshallerIdentifier)),
                        IdentifierName(ShapeMemberNames.Value.Stateful.OnInvoked));
        }

        public static string GetMarshallerIdentifier(TypePositionInfo info, StubIdentifierContext context)
        {
            return context.GetAdditionalIdentifier(info, MarshallerIdentifier);
        }
    }

    /// <summary>
    /// Marshaller that enables support for a stackalloc constructor variant on a native type.
    /// </summary>
    internal sealed class StatefulCallerAllocatedBufferMarshalling(ICustomTypeMarshallingStrategy innerMarshaller, TypeSyntax marshallerType, TypeSyntax bufferElementType) : ICustomTypeMarshallingStrategy
    {
        public ManagedTypeInfo NativeType => innerMarshaller.NativeType;

        public IEnumerable<StatementSyntax> GenerateCleanupCallerAllocatedResourcesStatements(StubIdentifierContext context)
        {
            return innerMarshaller.GenerateCleanupCallerAllocatedResourcesStatements(context);
        }

        public IEnumerable<StatementSyntax> GenerateCleanupCalleeAllocatedResourcesStatements(StubIdentifierContext context)
        {
            return innerMarshaller.GenerateCleanupCalleeAllocatedResourcesStatements(context);
        }

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(StubIdentifierContext context)
        {
            if (MarshallerHelpers.CanUseCallerAllocatedBuffer(TypeInfo, CodeContext))
            {
                return GenerateCallerAllocatedBufferMarshalStatements();
            }

            return innerMarshaller.GenerateMarshalStatements(context);

            IEnumerable<StatementSyntax> GenerateCallerAllocatedBufferMarshalStatements()
            {
                (string managedIdentifier, _) = context.GetIdentifiers(TypeInfo);

                // <marshaller>.FromManaged(<managedIdentifier>, stackalloc <bufferElementType>[<marshallerType>.BufferSize]);
                yield return MethodInvocationStatement(
                                IdentifierName(context.GetAdditionalIdentifier(TypeInfo, StatefulValueMarshalling.MarshallerIdentifier)),
                                IdentifierName(ShapeMemberNames.Value.Stateful.FromManaged),
                                Argument(IdentifierName(managedIdentifier)),
                                Argument(StackAllocArrayCreationExpression(
                                    ArrayType(
                                        bufferElementType,
                                        SingletonList(ArrayRankSpecifier(SingletonSeparatedList<ExpressionSyntax>(
                                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                marshallerType,
                                                IdentifierName(ShapeMemberNames.BufferSize)))))))));
            }
        }

        public IEnumerable<StatementSyntax> GeneratePinnedMarshalStatements(StubIdentifierContext context)
        {
            return innerMarshaller.GeneratePinnedMarshalStatements(context);
        }

        public IEnumerable<StatementSyntax> GeneratePinStatements(StubIdentifierContext context)
        {
            return innerMarshaller.GeneratePinStatements(context);
        }

        public IEnumerable<StatementSyntax> GenerateSetupStatements(StubIdentifierContext context)
        {
            return innerMarshaller.GenerateSetupStatements(context);
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalCaptureStatements(StubIdentifierContext context)
        {
            return innerMarshaller.GenerateUnmarshalCaptureStatements(context);
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(StubIdentifierContext context)
        {
            return innerMarshaller.GenerateUnmarshalStatements(context);
        }

        public bool UsesNativeIdentifier => innerMarshaller.UsesNativeIdentifier;

        public TypePositionInfo TypeInfo => innerMarshaller.TypeInfo;

        public StubCodeContext CodeContext => innerMarshaller.CodeContext;

        public IEnumerable<StatementSyntax> GenerateGuaranteedUnmarshalStatements(StubIdentifierContext context) => innerMarshaller.GenerateGuaranteedUnmarshalStatements(context);
        public IEnumerable<StatementSyntax> GenerateNotifyForSuccessfulInvokeStatements(StubIdentifierContext context) => innerMarshaller.GenerateNotifyForSuccessfulInvokeStatements(context);
    }

    internal sealed class StatefulLinearCollectionSource(TypePositionInfo info, StubCodeContext codeContext) : IElementsMarshallingCollectionSource
    {
        public TypePositionInfo TypeInfo => info;

        public StubCodeContext CodeContext => codeContext;

        public InvocationExpressionSyntax GetUnmanagedValuesDestination(StubIdentifierContext context)
        {
            string marshaller = StatefulValueMarshalling.GetMarshallerIdentifier(info, context);

            // <marshaller>.GetUnmanagedValuesDestination()
            return MethodInvocation(
                    IdentifierName(marshaller),
                    IdentifierName(ShapeMemberNames.LinearCollection.Stateful.GetUnmanagedValuesDestination));
        }

        public InvocationExpressionSyntax GetManagedValuesSource(StubIdentifierContext context)
        {
            string marshaller = StatefulValueMarshalling.GetMarshallerIdentifier(info, context);

            // <marshaller>.GetManagedValuesSource()
            return MethodInvocation(
                    IdentifierName(marshaller),
                    IdentifierName(ShapeMemberNames.LinearCollection.Stateful.GetManagedValuesSource));
        }

        public InvocationExpressionSyntax GetUnmanagedValuesSource(StubIdentifierContext context)
        {
            string marshaller = StatefulValueMarshalling.GetMarshallerIdentifier(info, context);
            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);

            // <marshaller>.GetUnmanagedValuesSource(<numElements>)
            return MethodInvocation(
                    IdentifierName(marshaller),
                    IdentifierName(ShapeMemberNames.LinearCollection.Stateful.GetUnmanagedValuesSource),
                    Argument(IdentifierName(numElementsIdentifier)));
        }

        public InvocationExpressionSyntax GetManagedValuesDestination(StubIdentifierContext context)
        {
            string marshaller = StatefulValueMarshalling.GetMarshallerIdentifier(info, context);
            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(info, context);

            // <marshaller>.GetManagedValuesDestination(<numElements>)
            return MethodInvocation(
                    IdentifierName(marshaller),
                    IdentifierName(ShapeMemberNames.LinearCollection.Stateful.GetManagedValuesDestination),
                    Argument(IdentifierName(numElementsIdentifier)));
        }
    }

    /// <summary>
    /// Marshaller that enables support for marshalling elements of a collection via a native type that implements the LinearCollection marshalling spec.
    /// </summary>
    internal sealed class StatefulLinearCollectionMarshalling(
        ICustomTypeMarshallingStrategy innerMarshaller,
        MarshallerShape shape,
        CountInfo countInfo,
        bool castCountInfo,
        ElementsMarshalling elementsMarshalling,
        bool cleanupElements) : ICustomTypeMarshallingStrategy
    {
        public ManagedTypeInfo NativeType => innerMarshaller.NativeType;

        public IEnumerable<StatementSyntax> GenerateCleanupCallerAllocatedResourcesStatements(StubIdentifierContext context)
        {
            // We don't have anything to cleanup specifically related to this value, just the elements. We let the element marshaller decide whether to cleanup in callee or caller cleanup stage
            if (!cleanupElements)
                yield break;

            StatementSyntax elementCleanup = elementsMarshalling.GenerateElementCleanupStatement(context);

            if (!elementCleanup.IsKind(SyntaxKind.EmptyStatement))
            {
                yield return elementCleanup;
            }
        }

        public IEnumerable<StatementSyntax> GenerateCleanupCalleeAllocatedResourcesStatements(StubIdentifierContext context)
        {
            // We don't have anything to cleanup specifically related to this value, just the elements. We let the element marshaller decide whether to cleanup in callee or caller cleanup stage
            if (!cleanupElements)
                yield break;

            StatementSyntax elementCleanup = elementsMarshalling.GenerateElementCleanupStatement(context);

            if (!elementCleanup.IsKind(SyntaxKind.EmptyStatement))
            {
                yield return elementCleanup;
            }
        }

        public IEnumerable<StatementSyntax> GenerateGuaranteedUnmarshalStatements(StubIdentifierContext context) => innerMarshaller.GenerateGuaranteedUnmarshalStatements(context);

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(StubIdentifierContext context)
        {
            foreach (StatementSyntax statement in innerMarshaller.GenerateMarshalStatements(context))
            {
                yield return statement;
            }

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

            if (!shape.HasFlag(MarshallerShape.ToUnmanaged) && !shape.HasFlag(MarshallerShape.CallerAllocatedBuffer))
                yield break;

            yield return elementsMarshalling.GenerateMarshalStatement(context);
        }

        public IEnumerable<StatementSyntax> GenerateNotifyForSuccessfulInvokeStatements(StubIdentifierContext context) => innerMarshaller.GenerateNotifyForSuccessfulInvokeStatements(context);
        public IEnumerable<StatementSyntax> GeneratePinnedMarshalStatements(StubIdentifierContext context) => innerMarshaller.GeneratePinnedMarshalStatements(context);
        public IEnumerable<StatementSyntax> GeneratePinStatements(StubIdentifierContext context) => innerMarshaller.GeneratePinStatements(context);
        public IEnumerable<StatementSyntax> GenerateSetupStatements(StubIdentifierContext context)
        {
            foreach (StatementSyntax statement in innerMarshaller.GenerateSetupStatements(context))
            {
                yield return statement;
            }

            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(TypeInfo, context);
            // int <numElements>;
            yield return Declare(
                    PredefinedType(Token(SyntaxKind.IntKeyword)),
                    numElementsIdentifier,
                    initializeToDefault: false);

            var elementsSetup = elementsMarshalling.GenerateSetupStatement(context);
            if (elementsSetup is not EmptyStatementSyntax)
            {
                yield return elementsSetup;
            }
            // Use the numElements local to ensure the compiler doesn't give errors for using an uninitialized variable.
            // The value may be used in cleanup before it has been initialized, so this is not safe.
            yield return MarshallerHelpers.SkipInitOrDefaultInit(
                new TypePositionInfo(SpecialTypeInfo.Int32, NoMarshallingInfo.Instance)
                {
                    InstanceIdentifier = numElementsIdentifier
                }, context);
        }

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
                yield break;
            }

            if (!shape.HasFlag(MarshallerShape.ToManaged))
            {
                yield break;
            }

            string numElementsIdentifier = MarshallerHelpers.GetNumElementsIdentifier(TypeInfo, context);

            // <numElements> = <numElementsExpression>;
            yield return AssignmentStatement(IdentifierName(numElementsIdentifier), ElementsMarshalling.GenerateNumElementsExpression(countInfo, castCountInfo, CodeContext, context));

            yield return elementsMarshalling.GenerateUnmarshalStatement(context);

            foreach (StatementSyntax statement in innerMarshaller.GenerateUnmarshalStatements(context))
            {
                yield return statement;
            }
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalCaptureStatements(StubIdentifierContext context) => innerMarshaller.GenerateUnmarshalCaptureStatements(context);

        public bool UsesNativeIdentifier => true;

        public TypePositionInfo TypeInfo => innerMarshaller.TypeInfo;

        public StubCodeContext CodeContext => innerMarshaller.CodeContext;
    }

    /// <summary>
    /// Marshaller that enables calling the Free method on a stateful marshaller.
    /// </summary>
    internal sealed class StatefulFreeMarshalling(ICustomTypeMarshallingStrategy innerMarshaller) : ICustomTypeMarshallingStrategy
    {
        public ManagedTypeInfo NativeType => innerMarshaller.NativeType;

        public IEnumerable<StatementSyntax> GenerateCleanupCallerAllocatedResourcesStatements(StubIdentifierContext context)
        {
            foreach (var statement in innerMarshaller.GenerateCleanupCallerAllocatedResourcesStatements(context))
            {
                yield return statement;
            }

            if (MarshallerHelpers.GetCleanupStage(TypeInfo, CodeContext) is not StubIdentifierContext.Stage.CleanupCallerAllocated)
                yield break;

            string marshaller = StatefulValueMarshalling.GetMarshallerIdentifier(TypeInfo, context);
            // <marshaller>.Free();
            yield return MethodInvocationStatement(
                        IdentifierName(marshaller),
                        IdentifierName(ShapeMemberNames.Free));
        }

        public IEnumerable<StatementSyntax> GenerateCleanupCalleeAllocatedResourcesStatements(StubIdentifierContext context)
        {
            foreach (var statement in innerMarshaller.GenerateCleanupCalleeAllocatedResourcesStatements(context))
            {
                yield return statement;
            }

            if (MarshallerHelpers.GetCleanupStage(TypeInfo, CodeContext) is not StubIdentifierContext.Stage.CleanupCalleeAllocated)
                yield break;

            string marshaller = StatefulValueMarshalling.GetMarshallerIdentifier(TypeInfo, context);
            // <marshaller>.Free();
            yield return MethodInvocationStatement(
                        IdentifierName(marshaller),
                        IdentifierName(ShapeMemberNames.Free));
        }

        public IEnumerable<StatementSyntax> GenerateGuaranteedUnmarshalStatements(StubIdentifierContext context) => innerMarshaller.GenerateGuaranteedUnmarshalStatements(context);

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(StubIdentifierContext context) => innerMarshaller.GenerateMarshalStatements(context);

        public IEnumerable<StatementSyntax> GenerateNotifyForSuccessfulInvokeStatements(StubIdentifierContext context) => innerMarshaller.GenerateNotifyForSuccessfulInvokeStatements(context);
        public IEnumerable<StatementSyntax> GeneratePinnedMarshalStatements(StubIdentifierContext context) => innerMarshaller.GeneratePinnedMarshalStatements(context);
        public IEnumerable<StatementSyntax> GeneratePinStatements(StubIdentifierContext context) => innerMarshaller.GeneratePinStatements(context);
        public IEnumerable<StatementSyntax> GenerateSetupStatements(StubIdentifierContext context) => innerMarshaller.GenerateSetupStatements(context);

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(StubIdentifierContext context) => innerMarshaller.GenerateUnmarshalStatements(context);

        public IEnumerable<StatementSyntax> GenerateUnmarshalCaptureStatements(StubIdentifierContext context) => innerMarshaller.GenerateUnmarshalCaptureStatements(context);

        public bool UsesNativeIdentifier => innerMarshaller.UsesNativeIdentifier;

        public TypePositionInfo TypeInfo => innerMarshaller.TypeInfo;

        public StubCodeContext CodeContext => innerMarshaller.CodeContext;
    }
}
