// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    public struct VariableDeclarations
    {
        public ImmutableArray<StatementSyntax> Initializations { get; init; }
        public ImmutableArray<LocalDeclarationStatementSyntax> Variables { get; init; }
        public static VariableDeclarations GenerateDeclarationsForManagedToNative(BoundGenerators marshallers, StubCodeContext context, bool initializeDeclarations)
        {
            ImmutableArray<StatementSyntax>.Builder initializations = ImmutableArray.CreateBuilder<StatementSyntax>();
            ImmutableArray<LocalDeclarationStatementSyntax>.Builder variables = ImmutableArray.CreateBuilder<LocalDeclarationStatementSyntax>();

            foreach (BoundGenerator marshaller in marshallers.NativeParameterMarshallers)
            {
                TypePositionInfo info = marshaller.TypeInfo;
                if (info.IsManagedReturnPosition)
                    continue;

                if (info.RefKind == RefKind.Out)
                {
                    (TargetFramework fmk, _) = context.GetTargetFramework();
                    if (info.ManagedType is not PointerTypeInfo
                        && info.ManagedType is not ValueTypeInfo { IsByRefLike: true }
                        && fmk is TargetFramework.Net)
                    {
                        // Use the Unsafe.SkipInit<T> API when available and
                        // managed type is usable as a generic parameter.
                        initializations.Add(ExpressionStatement(
                            InvocationExpression(
                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                    ParseName(TypeNames.System_Runtime_CompilerServices_Unsafe),
                                    IdentifierName("SkipInit")))
                            .WithArgumentList(
                                ArgumentList(SingletonSeparatedList(
                                    Argument(IdentifierName(info.InstanceIdentifier))
                                    .WithRefOrOutKeyword(Token(SyntaxKind.OutKeyword)))))));
                    }
                    else
                    {
                        // Assign out params to default
                        initializations.Add(ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(info.InstanceIdentifier),
                                LiteralExpression(
                                    SyntaxKind.DefaultLiteralExpression,
                                    Token(SyntaxKind.DefaultKeyword)))));
                    }
                }

                // Declare variables for parameters
                AppendVariableDeclations(variables, marshaller, context, initializeToDefault: initializeDeclarations);
            }

            // Stub return is not the same as invoke return
            if (!marshallers.IsManagedVoidReturn && !marshallers.ManagedNativeSameReturn)
            {
                // Declare variables for stub return value
                AppendVariableDeclations(variables, marshallers.ManagedReturnMarshaller, context, initializeToDefault: initializeDeclarations);
            }

            if (!marshallers.IsManagedVoidReturn)
            {
                // Declare variables for invoke return value
                AppendVariableDeclations(variables, marshallers.NativeReturnMarshaller, context, initializeToDefault: initializeDeclarations);
            }

            return new VariableDeclarations
            {
                Initializations = initializations.ToImmutable(),
                Variables = variables.ToImmutable()
            };

            static void AppendVariableDeclations(ImmutableArray<LocalDeclarationStatementSyntax>.Builder statementsToUpdate, BoundGenerator marshaller, StubCodeContext context, bool initializeToDefault)
            {
                (string managed, string native) = context.GetIdentifiers(marshaller.TypeInfo);

                // Declare variable for return value
                if (marshaller.TypeInfo.IsManagedReturnPosition || marshaller.TypeInfo.IsNativeReturnPosition)
                {
                    statementsToUpdate.Add(MarshallerHelpers.Declare(
                        marshaller.TypeInfo.ManagedType.Syntax,
                        managed,
                        false));
                }

                // Declare variable with native type for parameter or return value
                if (marshaller.Generator.UsesNativeIdentifier(marshaller.TypeInfo, context))
                {
                    statementsToUpdate.Add(MarshallerHelpers.Declare(
                        marshaller.Generator.AsNativeType(marshaller.TypeInfo),
                        native,
                        initializeToDefault));
                }
            }
        }
    }
}
