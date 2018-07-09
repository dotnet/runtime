// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Composition.Diagnostics
{
    // NOTE: Do not change the trace ids of values that have already shipped, 
    // these leak out to TraceListerners which could take a dependency on them.
    // This enum is a ushort deliberately, the maximum value of a trace id is 65535.
    internal enum CompositionTraceId : ushort
    {
        // Rejection

        Rejection_DefinitionRejected = 1,
        Rejection_DefinitionResurrected = 2,

        // Discovery

        Discovery_AssemblyLoadFailed = 3,
        Discovery_DefinitionMarkedWithPartNotDiscoverableAttribute = 4,
        Discovery_DefinitionMismatchedExportArity = 5,
        Discovery_DefinitionContainsNoExports = 6,
        Discovery_MemberMarkedWithMultipleImportAndImportMany = 7,

        // Registration

        Registration_ConstructorConventionOverridden = 101,
        Registration_TypeExportConventionOverridden = 102,
        Registration_MemberExportConventionOverridden = 103,
        Registration_MemberImportConventionOverridden = 104,
        Registration_PartCreationConventionOverridden = 105,
        Registration_MemberImportConventionMatchedTwice = 106,
        Registration_PartMetadataConventionOverridden = 107,
        Registration_ParameterImportConventionOverridden = 108,
        Registration_OnSatisfiedImportNotificationOverridden = 109
    }
}
