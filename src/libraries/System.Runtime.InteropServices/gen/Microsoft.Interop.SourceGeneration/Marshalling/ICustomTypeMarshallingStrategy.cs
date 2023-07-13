// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Interop
{
    /// <summary>
    /// Provides methods for generating code for each stage of the marshalling pipeline.
    /// The stages are outlined in runtime/docs/design/libraries/LibraryImportGenerator/Pipeline.md
    /// </summary>
    internal interface IMarshallingStagesGenerator
    {
        IEnumerable<StatementSyntax> GenerateCleanupStatements(TypePositionInfo info, StubCodeContext context);

        IEnumerable<StatementSyntax> GenerateGuaranteedUnmarshalStatements(TypePositionInfo info, StubCodeContext context);

        /// <summary>
        /// Conversion of managed data to native data
        /// </summary>
        IEnumerable<StatementSyntax> GenerateMarshalStatements(TypePositionInfo info, StubCodeContext context);

        IEnumerable<StatementSyntax> GenerateNotifyForSuccessfulInvokeStatements(TypePositionInfo info, StubCodeContext context);

        IEnumerable<StatementSyntax> GeneratePinnedMarshalStatements(TypePositionInfo info, StubCodeContext context);

        IEnumerable<StatementSyntax> GeneratePinStatements(TypePositionInfo info, StubCodeContext context);

        /// <summary>
        /// Initialization that happens before marshalling any data
        /// </summary>
        IEnumerable<StatementSyntax> GenerateSetupStatements(TypePositionInfo info, StubCodeContext context);

        /// <summary>
        /// Generate statements to capture any out parameters or return values into local variables that can be used in the cleanup stage if marshalling fails
        /// </summary>
        IEnumerable<StatementSyntax> GenerateUnmarshalCaptureStatements(TypePositionInfo info, StubCodeContext context);

        /// <summary>
        /// Generate statements to unmarshal the native data to managed representations and store them in a local variable which gets assigned to any out parameters
        /// </summary>
        IEnumerable<StatementSyntax> GenerateUnmarshalStatements(TypePositionInfo info, StubCodeContext context);
    }

    /// <summary>
    /// The base interface for implementing various aspects of the custom native type and collection marshalling specs.
    /// </summary>
    internal interface ICustomTypeMarshallingStrategy : IMarshallingStagesGenerator
    {
        ManagedTypeInfo AsNativeType(TypePositionInfo info);

        IEnumerable<StatementSyntax> GenerateAssignParameterIn(TypePositionInfo info, StubCodeContext context);

        IEnumerable<StatementSyntax> GenerateAssignParameterOut(TypePositionInfo info, StubCodeContext context);

        bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context);
    }
}
