// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

        IEnumerable<StatementSyntax> GenerateCleanupCallerAllocatedResourcesStatements(StubIdentifierContext context);

        IEnumerable<StatementSyntax> GenerateCleanupCalleeAllocatedResourcesStatements(StubIdentifierContext context);

        IEnumerable<StatementSyntax> GenerateGuaranteedUnmarshalStatements(StubIdentifierContext context);

        IEnumerable<StatementSyntax> GenerateMarshalStatements(StubIdentifierContext context);

        IEnumerable<StatementSyntax> GenerateNotifyForSuccessfulInvokeStatements(StubIdentifierContext context);

        IEnumerable<StatementSyntax> GeneratePinnedMarshalStatements(StubIdentifierContext context);

        IEnumerable<StatementSyntax> GeneratePinStatements(StubIdentifierContext context);

        IEnumerable<StatementSyntax> GenerateSetupStatements(StubIdentifierContext context);

        IEnumerable<StatementSyntax> GenerateUnmarshalCaptureStatements(StubIdentifierContext context);

        IEnumerable<StatementSyntax> GenerateUnmarshalStatements(StubIdentifierContext context);

        bool UsesNativeIdentifier { get; }
    }
}
