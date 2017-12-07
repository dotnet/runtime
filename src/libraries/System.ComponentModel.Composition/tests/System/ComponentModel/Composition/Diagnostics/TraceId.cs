// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.Composition.Diagnostics
{
    // We need a public version of CompositionTraceId, so that the QA tests can access and verify the trace.
    public enum TraceId : ushort
    {
        Rejection_DefinitionRejected = CompositionTraceId.Rejection_DefinitionRejected,
        Rejection_DefinitionResurrected = CompositionTraceId.Rejection_DefinitionResurrected,

        Discovery_AssemblyLoadFailed = CompositionTraceId.Discovery_AssemblyLoadFailed,
        Discovery_DefinitionMarkedWithPartNotDiscoverableAttribute = CompositionTraceId.Discovery_DefinitionMarkedWithPartNotDiscoverableAttribute,
        Discovery_DefinitionMismatchedExportArity = CompositionTraceId.Discovery_DefinitionMismatchedExportArity,
        Discovery_DefinitionContainsNoExports = CompositionTraceId.Discovery_DefinitionContainsNoExports,
        Discovery_MemberMarkedWithMultipleImportAndImportMany = CompositionTraceId.Discovery_MemberMarkedWithMultipleImportAndImportMany,
    }
}
