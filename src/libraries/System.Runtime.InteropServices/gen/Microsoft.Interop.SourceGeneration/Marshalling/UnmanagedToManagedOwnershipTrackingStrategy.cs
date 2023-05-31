// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    /// <summary>
    /// Marshalling strategy that introduces a variable to hold the initial value of the provided <see cref="TypePositionInfo"/> and a variable to track if the original value has been replaced.
    /// </summary>
    /// <seealso cref="CleanupOwnedOriginalValueMarshalling" />
    internal sealed class UnmanagedToManagedOwnershipTrackingStrategy : ICustomTypeMarshallingStrategy
    {
        private readonly ICustomTypeMarshallingStrategy _innerMarshaller;

        public UnmanagedToManagedOwnershipTrackingStrategy(ICustomTypeMarshallingStrategy innerMarshaller)
        {
            _innerMarshaller = innerMarshaller;
        }

        public ManagedTypeInfo AsNativeType(TypePositionInfo info) => _innerMarshaller.AsNativeType(info);

        public IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateCleanupStatements(info, context);

        public IEnumerable<StatementSyntax> GenerateGuaranteedUnmarshalStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateGuaranteedUnmarshalStatements(info, context);
        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            foreach (StatementSyntax statement in _innerMarshaller.GenerateMarshalStatements(info, context))
            {
                yield return statement;
            }

            // Now that we've set the new value to pass to the caller on the <native> identifier, we need to make sure that we free the old one.
            // The caller will not see the old one any more, so it won't be able to free it.

            // <ownOriginalValue> = true;
            yield return ExpressionStatement(
                AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(context.GetAdditionalIdentifier(info, OwnershipTrackingHelpers.OwnOriginalValueIdentifier)),
                    LiteralExpression(SyntaxKind.TrueLiteralExpression)));
        }

        public IEnumerable<StatementSyntax> GenerateNotifyForSuccessfulInvokeStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateNotifyForSuccessfulInvokeStatements(info, context);
        public IEnumerable<StatementSyntax> GeneratePinnedMarshalStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GeneratePinnedMarshalStatements(info, context);

        public IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GeneratePinStatements(info, context);
        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context)
        {
            foreach (StatementSyntax statement in _innerMarshaller.GenerateSetupStatements(info, context))
            {
                yield return statement;
            }

            // bool <ownOriginalValue> = false;
            yield return LocalDeclarationStatement(
                VariableDeclaration(
                    PredefinedType(Token(SyntaxKind.BoolKeyword)),
                    SingletonSeparatedList(
                        VariableDeclarator(
                            Identifier(context.GetAdditionalIdentifier(info, OwnershipTrackingHelpers.OwnOriginalValueIdentifier)),
                            null,
                            EqualsValueClause(
                                LiteralExpression(SyntaxKind.FalseLiteralExpression))))));

            yield return OwnershipTrackingHelpers.DeclareOriginalValueIdentifier(info, context, AsNativeType(info));
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalCaptureStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateUnmarshalCaptureStatements(info, context);

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateUnmarshalStatements(info, context);
        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.UsesNativeIdentifier(info, context);
    }

    /// <summary>
    /// Marshalling strategy that uses the tracking variables introduced by <see cref="UnmanagedToManagedOwnershipTrackingStrategy"/> to cleanup the original value if the original value is owned
    /// in the <see cref="StubCodeContext.Stage.Cleanup"/> stage.
    /// </summary>
    internal sealed class CleanupOwnedOriginalValueMarshalling : ICustomTypeMarshallingStrategy
    {
        private readonly ICustomTypeMarshallingStrategy _innerMarshaller;

        public CleanupOwnedOriginalValueMarshalling(ICustomTypeMarshallingStrategy innerMarshaller)
        {
            _innerMarshaller = innerMarshaller;
        }

        public ManagedTypeInfo AsNativeType(TypePositionInfo info) => _innerMarshaller.AsNativeType(info);

        public IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context)
        {
            // if (<ownOriginalValue>)
            // {
            //     <cleanup>
            // }
            yield return IfStatement(
                IdentifierName(context.GetAdditionalIdentifier(info, OwnershipTrackingHelpers.OwnOriginalValueIdentifier)),
                Block(_innerMarshaller.GenerateCleanupStatements(info, new OwnedValueCodeContext(context))));
        }

        public IEnumerable<StatementSyntax> GenerateGuaranteedUnmarshalStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateGuaranteedUnmarshalStatements(info, context);
        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateMarshalStatements(info, context);

        public IEnumerable<StatementSyntax> GenerateNotifyForSuccessfulInvokeStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateNotifyForSuccessfulInvokeStatements(info, context);
        public IEnumerable<StatementSyntax> GeneratePinnedMarshalStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GeneratePinnedMarshalStatements(info, context);

        public IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GeneratePinStatements(info, context);
        public IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateSetupStatements(info, context);

        public IEnumerable<StatementSyntax> GenerateUnmarshalCaptureStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateUnmarshalCaptureStatements(info, context);

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.GenerateUnmarshalStatements(info, context);
        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => _innerMarshaller.UsesNativeIdentifier(info, context);
    }

    /// <summary>
    /// Marshalling strategy to cache the initial value of a given <see cref="TypePositionInfo"/> in a local variable and cleanup that value in the cleanup stage.
    /// Useful in scenarios where the value is always owned in all code-paths that reach the <see cref="StubCodeContext.Stage.Cleanup"/> stage, so additional ownership tracking is extraneous.
    /// </summary>
    internal sealed class FreeAlwaysOwnedOriginalValueGenerator : IMarshallingGenerator
    {
        private readonly IMarshallingGenerator _inner;

        public FreeAlwaysOwnedOriginalValueGenerator(IMarshallingGenerator inner)
        {
            _inner = inner;
        }

        public ManagedTypeInfo AsNativeType(TypePositionInfo info) => _inner.AsNativeType(info);
        public IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext context)
        {
            if (context.CurrentStage == StubCodeContext.Stage.Setup)
            {
                return GenerateSetupStatements();
            }

            if (context.CurrentStage == StubCodeContext.Stage.Cleanup)
            {
                return GenerateStatementsFromInner(new OwnedValueCodeContext(context));
            }

            return GenerateStatementsFromInner(context);

            IEnumerable<StatementSyntax> GenerateSetupStatements()
            {
                foreach (var statement in GenerateStatementsFromInner(context))
                {
                    yield return statement;
                }

                yield return OwnershipTrackingHelpers.DeclareOriginalValueIdentifier(info, context, AsNativeType(info));
            }

            IEnumerable<StatementSyntax> GenerateStatementsFromInner(StubCodeContext contextForStage)
            {
                return _inner.Generate(info, contextForStage);
            }
        }

        public SignatureBehavior GetNativeSignatureBehavior(TypePositionInfo info) => _inner.GetNativeSignatureBehavior(info);
        public ValueBoundaryBehavior GetValueBoundaryBehavior(TypePositionInfo info, StubCodeContext context) => _inner.GetValueBoundaryBehavior(info, context);
        public bool IsSupported(TargetFramework target, Version version) => _inner.IsSupported(target, version);
        public bool SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, StubCodeContext context) => _inner.SupportsByValueMarshalKind(marshalKind, context);
        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => _inner.UsesNativeIdentifier(info, context);
    }

#pragma warning disable SA1400 // Access modifier should be declared https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3659
    sealed file record OwnedValueCodeContext : StubCodeContext
#pragma warning restore SA1400 // Access modifier should be declared
    {
        private readonly StubCodeContext _innerContext;

        public OwnedValueCodeContext(StubCodeContext innerContext)
        {
            _innerContext = innerContext;
            CurrentStage = innerContext.CurrentStage;
            Direction = innerContext.Direction;
        }

        public override bool SingleFrameSpansNativeContext => _innerContext.SingleFrameSpansNativeContext;

        public override bool AdditionalTemporaryStateLivesAcrossStages => _innerContext.AdditionalTemporaryStateLivesAcrossStages;

        public override (TargetFramework framework, Version version) GetTargetFramework() => _innerContext.GetTargetFramework();

        public override (string managed, string native) GetIdentifiers(TypePositionInfo info)
        {
            var (managed, _) = _innerContext.GetIdentifiers(info);
            return (managed, _innerContext.GetAdditionalIdentifier(info, OwnershipTrackingHelpers.OriginalValueIdentifier));
        }

        public override string GetAdditionalIdentifier(TypePositionInfo info, string name) => _innerContext.GetAdditionalIdentifier(info, name);
    }

#pragma warning disable SA1400 // Access modifier should be declared https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3659
    static file class OwnershipTrackingHelpers
#pragma warning restore SA1400 // Access modifier should be declared
    {
        public const string OwnOriginalValueIdentifier = "ownOriginal";
        public const string OriginalValueIdentifier = "original";

        public static StatementSyntax DeclareOriginalValueIdentifier(TypePositionInfo info, StubCodeContext context, ManagedTypeInfo nativeType)
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
