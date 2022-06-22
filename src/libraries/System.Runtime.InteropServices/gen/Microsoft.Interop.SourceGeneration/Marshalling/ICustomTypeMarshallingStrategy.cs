// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    /// <summary>
    /// The base interface for implementing various aspects of the custom native type and collection marshalling specs.
    /// </summary>
    internal interface ICustomTypeMarshallingStrategy
    {
        TypeSyntax AsNativeType(TypePositionInfo info);

        IEnumerable<ArgumentSyntax> GetNativeTypeConstructorArguments(TypePositionInfo info, StubCodeContext context);

        IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context, IEnumerable<ArgumentSyntax> nativeTypeConstructorArguments);

        IEnumerable<StatementSyntax> GeneratePinnedMarshalStatements(TypePositionInfo info, StubCodeContext context);

        IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context);

        IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context);

        IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubCodeContext context);

        IEnumerable<StatementSyntax> GenerateUnmarshalCaptureStatements(TypePositionInfo info, StubCodeContext context);

        IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubCodeContext context);

        bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context);
    }

    /// <summary>
    /// Stateless marshalling support for a type that has a custom unmanaged type.
    /// </summary>
    internal sealed class StatelessValueMarshalling : ICustomTypeMarshallingStrategy
    {
        private readonly TypeSyntax _marshallerTypeSyntax;
        private readonly TypeSyntax _nativeTypeSyntax;
        private readonly CustomTypeMarshallerFeatures _features;

        public StatelessValueMarshalling(TypeSyntax marshallerTypeSyntax, TypeSyntax nativeTypeSyntax, CustomTypeMarshallerFeatures features)
        {
            _marshallerTypeSyntax = marshallerTypeSyntax;
            _nativeTypeSyntax = nativeTypeSyntax;
            _features = features;
        }

        public TypeSyntax AsNativeType(TypePositionInfo info)
        {
            return _nativeTypeSyntax;
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => true;

        public IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context)
        {
            if (!_features.HasFlag(CustomTypeMarshallerFeatures.UnmanagedResources))
                yield break;

            // <managedIdentifier> = <marshallerType>.ConvertToManaged(<nativeIdentifier>);
            yield return ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        _marshallerTypeSyntax,
                        IdentifierName(ShapeMemberNames.Value.Stateless.Free)),
                    ArgumentList(SingletonSeparatedList(
                        Argument(IdentifierName(context.GetIdentifiers(info).native))))));
        }

        public IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context, IEnumerable<ArgumentSyntax> nativeTypeConstructorArguments)
        {
            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);

            // <nativeIdentifier> = <marshallerType>.ConvertToUnmanaged(<managedIdentifier>);
            yield return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(nativeIdentifier),
                    InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            _marshallerTypeSyntax,
                            IdentifierName(ShapeMemberNames.Value.Stateless.ConvertToUnmanaged)),
                        ArgumentList(SingletonSeparatedList(
                            Argument(IdentifierName(managedIdentifier)))))));
        }

        public IEnumerable<StatementSyntax> GeneratePinnedMarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            return Array.Empty<StatementSyntax>();
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubCodeContext context)
        {
            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);

            // <managedIdentifier> = <marshallerType>.ConvertToManaged(<nativeIdentifier>);
            yield return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(managedIdentifier),
                    InvocationExpression(
                        MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                            _marshallerTypeSyntax,
                            IdentifierName(ShapeMemberNames.Value.Stateless.ConvertToManaged)),
                        ArgumentList(SingletonSeparatedList(
                            Argument(IdentifierName(nativeIdentifier)))))));
        }

        public IEnumerable<StatementSyntax> GenerateUnmarshalCaptureStatements(TypePositionInfo info, StubCodeContext context)
        {
            return Array.Empty<StatementSyntax>();
        }

        public IEnumerable<ArgumentSyntax> GetNativeTypeConstructorArguments(TypePositionInfo info, StubCodeContext context)
        {
            return Array.Empty<ArgumentSyntax>();
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
}
