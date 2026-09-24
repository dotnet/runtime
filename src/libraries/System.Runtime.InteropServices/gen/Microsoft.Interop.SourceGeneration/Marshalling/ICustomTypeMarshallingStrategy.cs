// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Interop
{
    /// <summary>
    /// The base interface for implementing various aspects of the custom native type and collection marshalling specs.
    /// </summary>
    internal interface ICustomTypeMarshallingStrategy
    {
        TypePositionInfo TypeInfo { get; }

        StubCodeContext CodeContext { get; }

        ManagedTypeInfo NativeType { get; }

        void GenerateCleanupCallerAllocatedResourcesStatements(IndentedTextWriter writer, StubIdentifierContext context);

        void GenerateCleanupCalleeAllocatedResourcesStatements(IndentedTextWriter writer, StubIdentifierContext context);

        void GenerateGuaranteedUnmarshalStatements(IndentedTextWriter writer, StubIdentifierContext context);

        void GenerateMarshalStatements(IndentedTextWriter writer, StubIdentifierContext context);

        void GenerateNotifyForSuccessfulInvokeStatements(IndentedTextWriter writer, StubIdentifierContext context);

        void GeneratePinnedMarshalStatements(IndentedTextWriter writer, StubIdentifierContext context);

        void GeneratePinStatements(IndentedTextWriter writer, StubIdentifierContext context);

        void GenerateSetupStatements(IndentedTextWriter writer, StubIdentifierContext context);

        void GenerateUnmarshalCaptureStatements(IndentedTextWriter writer, StubIdentifierContext context);

        void GenerateUnmarshalStatements(IndentedTextWriter writer, StubIdentifierContext context);

        bool UsesNativeIdentifier { get; }
    }
}
