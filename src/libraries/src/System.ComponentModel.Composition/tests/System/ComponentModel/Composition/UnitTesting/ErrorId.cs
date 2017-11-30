// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.Composition
{
    // We need a public version of CompositionErrorId, so that the QA tests can access and verify the errors.
    public enum ErrorId : int
    {
        Unknown = CompositionErrorId.Unknown,
        InvalidExportMetadata = CompositionErrorId.InvalidExportMetadata,
        ImportNotSetOnPart = CompositionErrorId.ImportNotSetOnPart,
        ImportEngine_ComposeTookTooManyIterations = CompositionErrorId.ImportEngine_ComposeTookTooManyIterations,
        ImportEngine_ImportCardinalityMismatch = CompositionErrorId.ImportEngine_ImportCardinalityMismatch,        
        ImportEngine_PartCycle = CompositionErrorId.ImportEngine_PartCycle,
        ImportEngine_PartCannotSetImport = CompositionErrorId.ImportEngine_PartCannotSetImport,
        ImportEngine_PartCannotGetExportedValue = CompositionErrorId.ImportEngine_PartCannotGetExportedValue,
        ImportEngine_PartCannotActivate = CompositionErrorId.ImportEngine_PartCannotActivate,
        ImportEngine_PreventedByExistingImport = CompositionErrorId.ImportEngine_PreventedByExistingImport,
        ImportEngine_InvalidStateForRecomposition = CompositionErrorId.ImportEngine_InvalidStateForRecomposition,
        ReflectionModel_ImportThrewException = CompositionErrorId.ReflectionModel_ImportThrewException,
        ReflectionModel_ImportNotAssignableFromExport = CompositionErrorId.ReflectionModel_ImportNotAssignableFromExport,
        ReflectionModel_ImportCollectionNull = CompositionErrorId.ReflectionModel_ImportCollectionNull,
        ReflectionModel_ImportCollectionNotWritable = CompositionErrorId.ReflectionModel_ImportCollectionNotWritable,
        ReflectionModel_ImportCollectionConstructionThrewException = CompositionErrorId.ReflectionModel_ImportCollectionConstructionThrewException,
        ReflectionModel_ImportCollectionGetThrewException = CompositionErrorId.ReflectionModel_ImportCollectionGetThrewException,
        ReflectionModel_ImportCollectionIsReadOnlyThrewException = CompositionErrorId.ReflectionModel_ImportCollectionIsReadOnlyThrewException,
        ReflectionModel_ImportCollectionClearThrewException = CompositionErrorId.ReflectionModel_ImportCollectionClearThrewException,
        ReflectionModel_ImportCollectionAddThrewException = CompositionErrorId.ReflectionModel_ImportCollectionAddThrewException,
        ReflectionModel_ImportManyOnParameterCanOnlyBeAssigned = CompositionErrorId.ReflectionModel_ImportManyOnParameterCanOnlyBeAssigned,
    }
}
