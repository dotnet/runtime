// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
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
        public ByValueMarshalKindSupport SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, TypePositionInfo info, StubCodeContext context, out GeneratorDiagnostic? diagnostic)
            => _inner.SupportsByValueMarshalKind(marshalKind, info, context, out diagnostic);
        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => _inner.UsesNativeIdentifier(info, context);
    }

#pragma warning disable SA1400 // Access modifier should be declared https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3659
    file sealed record OwnedValueCodeContext : StubCodeContext
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
    file static class OwnershipTrackingHelpers
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
