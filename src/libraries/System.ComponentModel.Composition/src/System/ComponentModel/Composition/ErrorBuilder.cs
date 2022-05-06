// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Composition.Primitives;

namespace System.ComponentModel.Composition
{
    internal static class ErrorBuilder
    {
        public static CompositionError PreventedByExistingImport(ComposablePart part, ImportDefinition import)
        {
            return CompositionError.Create(
                CompositionErrorId.ImportEngine_PreventedByExistingImport,
                SR.ImportEngine_PreventedByExistingImport,
                import.ToElement().DisplayName,
                part.ToElement().DisplayName);
        }

        public static CompositionError InvalidStateForRecompposition(ComposablePart part)
        {
            return CompositionError.Create(
                CompositionErrorId.ImportEngine_InvalidStateForRecomposition,
                SR.ImportEngine_InvalidStateForRecomposition,
                part.ToElement().DisplayName);
        }

        public static CompositionError ComposeTookTooManyIterations(int maximumNumberOfCompositionIterations)
        {
            return CompositionError.Create(
                CompositionErrorId.ImportEngine_ComposeTookTooManyIterations,
                SR.ImportEngine_ComposeTookTooManyIterations,
                maximumNumberOfCompositionIterations);
        }

        public static CompositionError CreateImportCardinalityMismatch(ImportCardinalityMismatchException exception, ImportDefinition definition)
        {
            ArgumentNullException.ThrowIfNull(exception);
            ArgumentNullException.ThrowIfNull(definition);

            return CompositionError.Create(
                CompositionErrorId.ImportEngine_ImportCardinalityMismatch,
                exception.Message,
                definition.ToElement(),
                (Exception?)null);
        }

        public static CompositionError CreatePartCannotActivate(ComposablePart part, Exception innerException)
        {
            ArgumentNullException.ThrowIfNull(part);
            ArgumentNullException.ThrowIfNull(innerException);

            ICompositionElement element = part.ToElement();
            return CompositionError.Create(
                CompositionErrorId.ImportEngine_PartCannotActivate,
                element,
                innerException,
                SR.ImportEngine_PartCannotActivate,
                element.DisplayName);
        }

        public static CompositionError CreatePartCannotSetImport(ComposablePart part, ImportDefinition definition, Exception innerException)
        {
            ArgumentNullException.ThrowIfNull(part);
            ArgumentNullException.ThrowIfNull(definition);
            ArgumentNullException.ThrowIfNull(innerException);

            ICompositionElement element = definition.ToElement();
            return CompositionError.Create(
                CompositionErrorId.ImportEngine_PartCannotSetImport,
                element,
                innerException,
                SR.ImportEngine_PartCannotSetImport,
                element.DisplayName,
                part.ToElement().DisplayName);
        }

        public static CompositionError CreateCannotGetExportedValue(ComposablePart part, ExportDefinition definition, Exception innerException)
        {
            ArgumentNullException.ThrowIfNull(part);
            ArgumentNullException.ThrowIfNull(definition);
            ArgumentNullException.ThrowIfNull(innerException);

            ICompositionElement element = definition.ToElement();
            return CompositionError.Create(
                CompositionErrorId.ImportEngine_PartCannotGetExportedValue,
                element,
                innerException,
                SR.ImportEngine_PartCannotGetExportedValue,
                element.DisplayName,
                part.ToElement().DisplayName);
        }

        public static CompositionError CreatePartCycle(ComposablePart part)
        {
            ArgumentNullException.ThrowIfNull(part);

            ICompositionElement element = part.ToElement();
            return CompositionError.Create(
                CompositionErrorId.ImportEngine_PartCycle,
                element,
                SR.ImportEngine_PartCycle,
                element.DisplayName);
        }
    }
}
