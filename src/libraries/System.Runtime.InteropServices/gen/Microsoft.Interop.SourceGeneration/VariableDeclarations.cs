// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
                TypePositionInfo info = marshaller.TypeInfo;
                (string managed, string native) = context.GetIdentifiers(info);

                // Declare variable for return value
                if (info.IsManagedReturnPosition || info.IsNativeReturnPosition)
                {
                    statementsToUpdate.Add(MarshallerHelpers.Declare(
                        info.ManagedType.Syntax,
                        managed,
                        initializeToDefault));
                }

                // Declare variable with native type for parameter or return value
                if (marshaller.Generator.UsesNativeIdentifier(info, context))
                {
                    statementsToUpdate.Add(MarshallerHelpers.Declare(
                        marshaller.Generator.AsNativeType(info).Syntax,
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
                if (info.IsNativeReturnPosition)
                    continue;

                if (info.IsManagedReturnPosition)
                    continue;

                // Declare variables for parameters
                AppendVariableDeclarations(variables, marshaller, context, initializeToDefault: initializeDeclarations);

                //{
                //    // We need to use the 'out' value - This should be removed once the ownership behavior is fixed
                //    var boundaryBehavior = marshaller.Generator.GetValueBoundaryBehavior(info, context);
                //    if (marshaller.Generator.UsesNativeIdentifier(info, context)
                //        && boundaryBehavior is not
                //            (ValueBoundaryBehavior.NativeIdentifier or ValueBoundaryBehavior.CastNativeIdentifier))
                //    {
                //        if (MarshallerHelpers.MarshalsOutToLocal(info, context))
                //        {
                //            string outlocal = context.GetAdditionalIdentifier(info, "out");
                //            initializations.Add(MarshallerHelpers.CreateDiscardStatement(outlocal));
                //        }
                //    }
                //}
            }

            if (!marshallers.IsManagedVoidReturn)
            {
                // Declare variables for stub return value
                AppendVariableDeclarations(variables, marshallers.ManagedReturnMarshaller, context, initializeToDefault: initializeDeclarations);
                //{
                //    var marshaller = marshallers.ManagedReturnMarshaller;
                //    var info = marshaller.TypeInfo;
                //    // We need to use the 'out' value - This should be removed once the ownership behavior is fixed
                //    var boundaryBehavior = marshaller.Generator.GetValueBoundaryBehavior(info, context);
                //    if (marshaller.Generator.UsesNativeIdentifier(info, context)
                //        && boundaryBehavior is not
                //            (ValueBoundaryBehavior.NativeIdentifier or ValueBoundaryBehavior.CastNativeIdentifier))
                //    {
                //        if (MarshallerHelpers.MarshalsOutToLocal(info, context))
                //        {
                //            string outlocal = context.GetAdditionalIdentifier(info, "out");
                //            initializations.Add(MarshallerHelpers.CreateDiscardStatement(outlocal));
                //        }
                //    }
                //}
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
                var info = marshaller.TypeInfo;
                (string managed, string native) = context.GetIdentifiers(info);

                // Declare variable for return value
                if (info.IsNativeReturnPosition)
                {
                    bool nativeReturnUsesNativeIdentifier = marshaller.Generator.UsesNativeIdentifier(info, context);

                    // Always initialize the return value.
                    statementsToUpdate.Add(MarshallerHelpers.Declare(
                        info.ManagedType.Syntax,
                        managed,
                        initializeToDefault || !nativeReturnUsesNativeIdentifier));

                    if (nativeReturnUsesNativeIdentifier)
                    {
                        statementsToUpdate.Add(MarshallerHelpers.Declare(
                            marshaller.Generator.AsNativeType(info).Syntax,
                            native,
                            initializeToDefault: true));
                    }
                }
                else
                {
                    ValueBoundaryBehavior boundaryBehavior = marshaller.Generator.GetValueBoundaryBehavior(info, context);

                    // Declare variable with native type for parameter
                    // if the marshaller uses the native identifier and the signature uses a different identifier
                    // than the native identifier.
                    if (marshaller.Generator.UsesNativeIdentifier(info, context)
                        && boundaryBehavior is not
                            (ValueBoundaryBehavior.NativeIdentifier or ValueBoundaryBehavior.CastNativeIdentifier))
                    {
                        TypeSyntax localType = marshaller.Generator.AsNativeType(info).Syntax;
                        if (MarshallerHelpers.MarshalsOutToLocal(info, context)
                            && !info.ByValueContentsMarshalKind.HasFlag(ByValueContentsMarshalKind.Out))
                        {
                            string outlocal = context.GetAdditionalIdentifier(info, "out");
                            // <nativeType> __param_native_out;
                            statementsToUpdate.Add(MarshallerHelpers.Declare(
                                marshaller.Generator.AsNativeType(info).Syntax,
                                outlocal,
                                true));
                        }

                        if (boundaryBehavior is ValueBoundaryBehavior.AddressOfNativeIdentifier)
                        {
                            // To simplify propogating back the value to the "byref" parameter,
                            // we'll just declare the native identifier as a ref to its type.
                            // The rest of the code we generate will work as expected, and we don't need
                            // to manually propogate back the updated values after the call.
                            statementsToUpdate.Add(MarshallerHelpers.Declare(
                                localType,
                                native,
                                marshaller.Generator.GenerateNativeDereferencedInitialization(info, context)));
                        }
                    }

                    if (boundaryBehavior != ValueBoundaryBehavior.ManagedIdentifier)
                    {
                        statementsToUpdate.Add(MarshallerHelpers.Declare(
                            info.ManagedType.Syntax,
                            managed,
                            initializeToDefault));
                    }
                }
            }
        }
    }
}
