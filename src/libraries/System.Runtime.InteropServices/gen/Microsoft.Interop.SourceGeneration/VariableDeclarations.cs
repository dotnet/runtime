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
        public static VariableDeclarations GenerateDeclarationsForManagedToUnmanaged(BoundGenerators marshallers, StubCodeContext context, bool initializeDeclarations)
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
            if (!marshallers.IsManagedVoidReturn)
            {
                // Declare variables for stub return value
                AppendVariableDeclarations(variables, marshallers.ManagedReturnMarshaller, context, initializeToDefault: initializeDeclarations);
            }

            if (!marshallers.IsUnmanagedVoidReturn && !marshallers.ManagedNativeSameReturn)
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
                        marshaller.Generator.AsNativeType(marshaller.TypeInfo).Syntax,
                        native,
                        initializeToDefault));
                }
            }
        }

        public static VariableDeclarations GenerateDeclarationsForUnmanagedToManaged(BoundGenerators marshallers, StubCodeContext context, bool initializeDeclarations)
        {
            ImmutableArray<StatementSyntax>.Builder initializations = ImmutableArray.CreateBuilder<StatementSyntax>();
            ImmutableArray<LocalDeclarationStatementSyntax>.Builder variables = ImmutableArray.CreateBuilder<LocalDeclarationStatementSyntax>();

            foreach (BoundGenerator marshaller in marshallers.NativeParameterMarshallers)
            {
                TypePositionInfo info = marshaller.TypeInfo;
                if (info.IsNativeReturnPosition || info.IsManagedReturnPosition)
                    continue;

                // Declare variables for parameters
                AppendVariableDeclarations(variables, marshaller, context, initializeToDefault: initializeDeclarations);
            }

            if (!marshallers.IsManagedVoidReturn)
            {
                // Declare variables for stub return value
                AppendVariableDeclarations(variables, marshallers.ManagedReturnMarshaller, context, initializeToDefault: initializeDeclarations);
            }

            if (!marshallers.IsUnmanagedVoidReturn && !marshallers.ManagedNativeSameReturn)
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
                if (marshaller.TypeInfo.IsNativeReturnPosition)
                {
                    bool nativeReturnUsesNativeIdentifier = marshaller.Generator.UsesNativeIdentifier(marshaller.TypeInfo, context);

                    // Always initialize the return value.
                    statementsToUpdate.Add(MarshallerHelpers.Declare(
                        marshaller.TypeInfo.ManagedType.Syntax,
                        managed,
                        initializeToDefault || !nativeReturnUsesNativeIdentifier));

                    if (nativeReturnUsesNativeIdentifier)
                    {
                        statementsToUpdate.Add(MarshallerHelpers.Declare(
                            marshaller.Generator.AsNativeType(marshaller.TypeInfo).Syntax,
                            native,
                            initializeToDefault: true));
                    }
                }
                else
                {
                    ValueBoundaryBehavior boundaryBehavior = marshaller.Generator.GetValueBoundaryBehavior(marshaller.TypeInfo, context);

                    // Declare variable with native type for parameter
                    // if the marshaller uses the native identifier and the signature uses a different identifier
                    // than the native identifier.
                    if (marshaller.Generator.UsesNativeIdentifier(marshaller.TypeInfo, context)
                        && boundaryBehavior is not
                            (ValueBoundaryBehavior.NativeIdentifier or ValueBoundaryBehavior.CastNativeIdentifier))
                    {
                        TypeSyntax localType = marshaller.Generator.AsNativeType(marshaller.TypeInfo).Syntax;
                        if (boundaryBehavior != ValueBoundaryBehavior.AddressOfNativeIdentifier)
                        {
                            statementsToUpdate.Add(MarshallerHelpers.Declare(
                                localType,
                                native,
                                false));
                        }
                        else
                        {
                            // To simplify propogating back the value to the "byref" parameter,
                            // we'll just declare the native identifier as a ref to its type.
                            // The rest of the code we generate will work as expected, and we don't need
                            // to manually propogate back the updated values after the call.
                            statementsToUpdate.Add(MarshallerHelpers.Declare(
                                RefType(localType),
                                native,
                                marshaller.Generator.GenerateNativeByRefInitialization(marshaller.TypeInfo, context)));
                        }
                    }

                    if (boundaryBehavior != ValueBoundaryBehavior.ManagedIdentifier)
                    {
                        statementsToUpdate.Add(MarshallerHelpers.Declare(
                            marshaller.TypeInfo.ManagedType.Syntax,
                            managed,
                            initializeToDefault));
                    }
                }
            }
        }
    }
}
