// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.Composition
{
    internal enum CompositionErrorId : int
    {
        Unknown = 0,
        InvalidExportMetadata,
        ImportNotSetOnPart,
        ImportEngine_ComposeTookTooManyIterations,
        ImportEngine_ImportCardinalityMismatch,
        ImportEngine_PartCycle,
        ImportEngine_PartCannotSetImport,
        ImportEngine_PartCannotGetExportedValue,
        ImportEngine_PartCannotActivate,
        ImportEngine_PreventedByExistingImport,
        ImportEngine_InvalidStateForRecomposition,
        ReflectionModel_ImportThrewException,
        ReflectionModel_ImportNotAssignableFromExport,        
        ReflectionModel_ImportCollectionNull,
        ReflectionModel_ImportCollectionNotWritable,
        ReflectionModel_ImportCollectionConstructionThrewException,
        ReflectionModel_ImportCollectionGetThrewException,
        ReflectionModel_ImportCollectionIsReadOnlyThrewException,
        ReflectionModel_ImportCollectionClearThrewException,
        ReflectionModel_ImportCollectionAddThrewException,
        ReflectionModel_ImportManyOnParameterCanOnlyBeAssigned,
    }
}
