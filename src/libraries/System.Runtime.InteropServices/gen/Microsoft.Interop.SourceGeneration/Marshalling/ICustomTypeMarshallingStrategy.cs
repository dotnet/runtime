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
        ManagedTypeInfo AsNativeType(TypePositionInfo info);

        IEnumerable<StatementSyntax> GenerateCleanupCallerAllocatedResourcesStatements(TypePositionInfo info, StubIdentifierContext context);

        IEnumerable<StatementSyntax> GenerateCleanupCalleeAllocatedResourcesStatements(TypePositionInfo info, StubIdentifierContext context);

        IEnumerable<StatementSyntax> GenerateGuaranteedUnmarshalStatements(TypePositionInfo info, StubIdentifierContext context);

        IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubIdentifierContext context);

        IEnumerable<StatementSyntax> GenerateNotifyForSuccessfulInvokeStatements(TypePositionInfo info, StubIdentifierContext context);

        IEnumerable<StatementSyntax> GeneratePinnedMarshalStatements(TypePositionInfo info, StubIdentifierContext context);

        IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubIdentifierContext context);

        IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubIdentifierContext context);

        IEnumerable<StatementSyntax> GenerateUnmarshalCaptureStatements(TypePositionInfo info, StubIdentifierContext context);

        IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubIdentifierContext context);

        bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context);
    }
}
