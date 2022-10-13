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
                    initializations.Add(MarshallerHelpers.SkipInitOrDefaultInit(info, context));
                }

                // Declare variables for parameters
                AppendVariableDeclarations(variables, marshaller, context, initializeToDefault: initializeDeclarations);
            }

            // Stub return is not the same as invoke return
            if (!marshallers.IsManagedVoidReturn && !marshallers.ManagedNativeSameReturn)
            {
                // Declare variables for stub return value
                AppendVariableDeclarations(variables, marshallers.ManagedReturnMarshaller, context, initializeToDefault: initializeDeclarations);
            }

            if (!marshallers.IsManagedVoidReturn)
            {
                // Declare variables for invoke return value
                AppendVariableDeclarations(variables, marshallers.NativeReturnMarshaller, context, initializeToDefault: initializeDeclarations);
            }

            return new VariableDeclarations
            {
                Initializations = initializations.ToImmutable(),
                Variables = variables.ToImmutable()
            };

            static void AppendVariableDeclarations(ImmutableArray<LocalDeclarationStatementSyntax>.Builder statementsToUpdate, BoundGenerator marshaller, StubCodeContext context, bool initializeToDefault)
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
