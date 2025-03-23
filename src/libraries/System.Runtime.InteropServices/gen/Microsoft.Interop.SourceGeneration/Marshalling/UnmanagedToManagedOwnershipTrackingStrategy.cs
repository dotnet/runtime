// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    /// <summary>
    /// Marshalling strategy that introduces a variable to hold the initial value of the provided <see cref="TypePositionInfo"/> and a variable to track if the original value has been replaced.
    /// </summary>
    /// <seealso cref="CleanupOwnedOriginalValueMarshalling" />
    internal sealed class UnmanagedToManagedOwnershipTrackingStrategy(ICustomTypeMarshallingStrategy innerMarshaller) : ICustomTypeMarshallingStrategy
    {
        public ManagedTypeInfo NativeType => innerMarshaller.NativeType;

        public bool UsesNativeIdentifier => innerMarshaller.UsesNativeIdentifier;

        public TypePositionInfo TypeInfo => innerMarshaller.TypeInfo;

        public StubCodeContext CodeContext => innerMarshaller.CodeContext;

        public IEnumerable<StatementSyntax> GenerateCleanupCallerAllocatedResourcesStatements(StubIdentifierContext context) => innerMarshaller.GenerateCleanupCallerAllocatedResourcesStatements(context);
        public IEnumerable<StatementSyntax> GenerateCleanupCalleeAllocatedResourcesStatements(StubIdentifierContext context) => innerMarshaller.GenerateCleanupCalleeAllocatedResourcesStatements(context);

        public IEnumerable<StatementSyntax> GenerateGuaranteedUnmarshalStatements(StubIdentifierContext context) => innerMarshaller.GenerateGuaranteedUnmarshalStatements(context);
        public IEnumerable<StatementSyntax> GenerateMarshalStatements(StubIdentifierContext context)
        {
            foreach (StatementSyntax statement in innerMarshaller.GenerateMarshalStatements(context))
            {
                yield return statement;
            }

            // Now that we've set the new value to pass to the caller on the <native> identifier, we need to make sure that we free the old one.
            // The caller will not see the old one any more, so it won't be able to free it.

            // <ownOriginalValue> = true;
            yield return ExpressionStatement(
                AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(context.GetAdditionalIdentifier(TypeInfo, OwnershipTrackingHelpers.OwnOriginalValueIdentifier)),
                    LiteralExpression(SyntaxKind.TrueLiteralExpression)));
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

            // bool <ownOriginalValue> = false;
            yield return LocalDeclarationStatement(
                VariableDeclaration(
                    PredefinedType(Token(SyntaxKind.BoolKeyword)),
                    SingletonSeparatedList(
                        VariableDeclarator(
                            Identifier(context.GetAdditionalIdentifier(TypeInfo, OwnershipTrackingHelpers.OwnOriginalValueIdentifier)),
                            null,
                            EqualsValueClause(
                                LiteralExpression(SyntaxKind.FalseLiteralExpression))))));

            yield return OwnershipTrackingHelpers.DeclareOriginalValueIdentifier(TypeInfo, context, NativeType);
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalCaptureStatements(StubIdentifierContext context) => innerMarshaller.GenerateUnmarshalCaptureStatements(context);

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(StubIdentifierContext context) => innerMarshaller.GenerateUnmarshalStatements(context);
    }

    /// <summary>
    /// Marshalling strategy that uses the tracking variables introduced by <see cref="UnmanagedToManagedOwnershipTrackingStrategy"/> to cleanup the original value if the original value is owned
    /// in the <see cref="StubIdentifierContext.Stage.CleanupCallerAllocated"/> stage.
    /// </summary>
    internal sealed class CleanupOwnedOriginalValueMarshalling(ICustomTypeMarshallingStrategy innerMarshaller) : ICustomTypeMarshallingStrategy
    {
        public ManagedTypeInfo NativeType => innerMarshaller.NativeType;

        public bool UsesNativeIdentifier => innerMarshaller.UsesNativeIdentifier;

        public TypePositionInfo TypeInfo => innerMarshaller.TypeInfo;

        public StubCodeContext CodeContext => innerMarshaller.CodeContext;

        public IEnumerable<StatementSyntax> GenerateCleanupCallerAllocatedResourcesStatements(StubIdentifierContext context)
        {
            if (MarshallerHelpers.GetCleanupStage(TypeInfo, CodeContext) is not StubIdentifierContext.Stage.CleanupCallerAllocated)
                yield break;
            // if (<ownOriginalValue>)
            // {
            //     <cleanup>
            // }
            yield return IfStatement(
                IdentifierName(context.GetAdditionalIdentifier(TypeInfo, OwnershipTrackingHelpers.OwnOriginalValueIdentifier)),
                Block(innerMarshaller.GenerateCleanupCallerAllocatedResourcesStatements(new OwnedValueCodeContext(context))));
        }

        public IEnumerable<StatementSyntax> GenerateCleanupCalleeAllocatedResourcesStatements(StubIdentifierContext context)
        {
            if (MarshallerHelpers.GetCleanupStage(TypeInfo, CodeContext) is not StubIdentifierContext.Stage.CleanupCalleeAllocated)
                yield break;
            // if (<ownOriginalValue>)
            // {
            //     <cleanup>
            // }
            yield return IfStatement(
                IdentifierName(context.GetAdditionalIdentifier(TypeInfo, OwnershipTrackingHelpers.OwnOriginalValueIdentifier)),
                Block(innerMarshaller.GenerateCleanupCalleeAllocatedResourcesStatements(new OwnedValueCodeContext(context))));
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
    /// Marshalling strategy to cache the initial value of a given <see cref="TypePositionInfo"/> in a local variable and cleanup that value in the cleanup stage.
    /// Useful in scenarios where the value is always owned in all code-paths that reach the <see cref="StubIdentifierContext.Stage.CleanupCallerAllocated"/> stage, so additional ownership tracking is extraneous.
    /// </summary>
    internal sealed class FreeAlwaysOwnedOriginalValueGenerator(IBoundMarshallingGenerator inner) : IBoundMarshallingGenerator
    {
        public ManagedTypeInfo NativeType => inner.NativeType;

        public TypePositionInfo TypeInfo => inner.TypeInfo;

        public StubCodeContext CodeContext => inner.CodeContext;

        public SignatureBehavior NativeSignatureBehavior => inner.NativeSignatureBehavior;

        public bool UsesNativeIdentifier => inner.UsesNativeIdentifier;
        public ValueBoundaryBehavior ValueBoundaryBehavior => inner.ValueBoundaryBehavior;

        public IEnumerable<StatementSyntax> Generate(StubIdentifierContext context)
        {
            if (context.CurrentStage == StubIdentifierContext.Stage.Setup)
            {
                return GenerateSetupStatements();
            }

            if (context.CurrentStage == StubIdentifierContext.Stage.CleanupCallerAllocated)
            {
                return GenerateStatementsFromInner(new OwnedValueCodeContext(context));
            }

            return GenerateStatementsFromInner(context);

            IEnumerable<StatementSyntax> GenerateSetupStatements()
            {
                return [
                    ..GenerateStatementsFromInner(new OwnedValueCodeContext(context)),
                    OwnershipTrackingHelpers.DeclareOriginalValueIdentifier(inner.TypeInfo, context, NativeType)
                    ];
            }

            IEnumerable<StatementSyntax> GenerateStatementsFromInner(StubIdentifierContext contextForStage)
            {
                return inner.Generate(contextForStage);
            }
        }

        public ByValueMarshalKindSupport SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, out GeneratorDiagnostic? diagnostic)
            => inner.SupportsByValueMarshalKind(marshalKind, out diagnostic);
    }

    file sealed record OwnedValueCodeContext : StubIdentifierContext
    {
        private readonly StubIdentifierContext _innerContext;

        public OwnedValueCodeContext(StubIdentifierContext innerContext)
        {
            _innerContext = innerContext;
            CurrentStage = innerContext.CurrentStage;
        }

        public override (string managed, string native) GetIdentifiers(TypePositionInfo info)
        {
            var (managed, _) = _innerContext.GetIdentifiers(info);
            return (managed, _innerContext.GetAdditionalIdentifier(info, OwnershipTrackingHelpers.OriginalValueIdentifier));
        }

        public override string GetAdditionalIdentifier(TypePositionInfo info, string name) => _innerContext.GetAdditionalIdentifier(info, name);
    }

    file static class OwnershipTrackingHelpers
    {
        public const string OwnOriginalValueIdentifier = "ownOriginal";
        public const string OriginalValueIdentifier = "original";

        public static StatementSyntax DeclareOriginalValueIdentifier(TypePositionInfo info, StubIdentifierContext context, ManagedTypeInfo nativeType)
        {
            // <nativeType> <original> = <nativeValueIdentifier>;
            return LocalDeclarationStatement(
                VariableDeclaration(
                    nativeType.Syntax,
                    SingletonSeparatedList(
                        VariableDeclarator(
                            Identifier(context.GetAdditionalIdentifier(info, OriginalValueIdentifier)),
                            null,
                            EqualsValueClause(
                                IdentifierName(context.GetIdentifiers(info).native))))));
        }
    }
}
