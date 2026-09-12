// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    public readonly record struct VariableDeclarations(string Initializations, string Variables)
    {
        public static VariableDeclarations GenerateDeclarationsForManagedToUnmanaged(BoundGenerators marshallers, StubIdentifierContext context, bool initializeDeclarations)
        {
            var initializations = new IndentedTextWriter();
            var variables = new IndentedTextWriter();
            foreach (IBoundMarshallingGenerator marshaller in marshallers.NativeParameterMarshallers)
            {
                TypePositionInfo info = marshaller.TypeInfo;
                if (info.IsManagedReturnPosition)
                    continue;

                if (info.RefKind == RefKind.Out && !info.IsErrorHandlingPosition)
                {
                    initializations.WriteLine(MarshallerHelpers.DefaultInit(info, context));
                }

                AppendVariableDeclarations(marshaller);
            }

            if (!marshallers.IsManagedVoidReturn)
            {
                AppendVariableDeclarations(marshallers.ManagedReturnMarshaller);
            }
            if (!marshallers.IsUnmanagedVoidReturn && !marshallers.ManagedNativeSameReturn)
            {
                AppendVariableDeclarations(marshallers.NativeReturnMarshaller);
            }

            foreach (IBoundMarshallingGenerator errorMarshaller in marshallers.SignatureMarshallers)
            {
                TypePositionInfo errorInfo = errorMarshaller.TypeInfo;
                if (errorInfo is { IsErrorHandlingPosition: true, ManagedIndex: TypePositionInfo.ErrorIndex }
                    && !ReferenceEquals(errorMarshaller, marshallers.NativeReturnMarshaller))
                {
                    Declare(variables, errorInfo.ManagedType.FullTypeName, context.GetIdentifiers(errorInfo).managed, initializeDeclarations);
                }
            }
            return new VariableDeclarations(initializations.ToString(), variables.ToString());

            void AppendVariableDeclarations(IBoundMarshallingGenerator marshaller)
            {
                (string managed, string native) = context.GetIdentifiers(marshaller.TypeInfo);
                if (marshaller.TypeInfo.IsManagedReturnPosition || marshaller.TypeInfo.IsNativeReturnPosition)
                {
                    Declare(variables, marshaller.TypeInfo.ManagedType.FullTypeName, managed, initializeDeclarations);
                }
                if (marshaller.UsesNativeIdentifier)
                {
                    Declare(variables, marshaller.NativeType.FullTypeName, native, initializeDeclarations);
                }
            }
        }

        public static VariableDeclarations GenerateDeclarationsForUnmanagedToManaged(BoundGenerators marshallers, StubIdentifierContext context, bool initializeDeclarations)
        {
            var variables = new IndentedTextWriter();
            foreach (IBoundMarshallingGenerator marshaller in marshallers.NativeParameterMarshallers)
            {
                TypePositionInfo info = marshaller.TypeInfo;
                if (info.IsNativeReturnPosition || info.IsManagedReturnPosition)
                    continue;

                AppendVariableDeclarations(marshaller);
            }

            if (!marshallers.IsManagedVoidReturn)
            {
                AppendVariableDeclarations(marshallers.ManagedReturnMarshaller);
            }
            if (!marshallers.IsUnmanagedVoidReturn && !marshallers.ManagedNativeSameReturn)
            {
                AppendVariableDeclarations(marshallers.NativeReturnMarshaller);
            }
            return new VariableDeclarations("", variables.ToString());

            void AppendVariableDeclarations(IBoundMarshallingGenerator marshaller)
            {
                (string managed, string native) = context.GetIdentifiers(marshaller.TypeInfo);
                if (marshaller.TypeInfo.IsNativeReturnPosition)
                {
                    bool nativeReturnUsesNativeIdentifier = marshaller.UsesNativeIdentifier;
                    Declare(variables, marshaller.TypeInfo.ManagedType.FullTypeName, managed, initializeDeclarations || !nativeReturnUsesNativeIdentifier);
                    if (nativeReturnUsesNativeIdentifier)
                    {
                        Declare(variables, marshaller.NativeType.FullTypeName, native, initializeToDefault: true);
                    }
                }
                else
                {
                    ValueBoundaryBehavior boundaryBehavior = marshaller.ValueBoundaryBehavior;
                    if (marshaller.UsesNativeIdentifier
                        && boundaryBehavior is not (ValueBoundaryBehavior.NativeIdentifier or ValueBoundaryBehavior.CastNativeIdentifier))
                    {
                        string localType = marshaller.NativeType.FullTypeName;
                        if (boundaryBehavior != ValueBoundaryBehavior.AddressOfNativeIdentifier)
                        {
                            Declare(variables, localType, native, initializeToDefault: false);
                        }
                        else
                        {
                            // Alias the native parameter so unmarshalling also updates the caller's value.
                            variables.WriteLine($"ref {localType} {native} = {marshaller.GenerateNativeByRefInitialization(context)};");
                        }
                    }

                    // The exception's managed identifier is declared by the catch clause.
                    if (boundaryBehavior != ValueBoundaryBehavior.ManagedIdentifier && !marshaller.TypeInfo.IsErrorHandlingPosition)
                    {
                        Declare(variables, marshaller.TypeInfo.ManagedType.FullTypeName, managed, initializeDeclarations);
                    }
                }
            }
        }

        private static void Declare(IndentedTextWriter writer, string type, string identifier, bool initializeToDefault)
        {
            writer.WriteLine($"{type} {identifier}{(initializeToDefault ? " = default" : "")};");
        }
    }
}
