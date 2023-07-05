// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.ComponentModel.Composition.ReflectionModel;
using System.Reflection;
using Microsoft.Internal;

namespace System.Composition.Diagnostics
{
    internal static class CompositionTrace
    {
        internal static void PartDefinitionResurrected(ComposablePartDefinition definition)
        {
            ArgumentNullException.ThrowIfNull(definition);

            if (CompositionTraceSource.CanWriteInformation)
            {
                CompositionTraceSource.WriteInformation(CompositionTraceId.Rejection_DefinitionResurrected,
                    SR.CompositionTrace_Rejection_DefinitionResurrected,
                    definition.GetDisplayName());
            }
        }

        internal static void PartDefinitionRejected(ComposablePartDefinition definition, ChangeRejectedException exception)
        {
            ArgumentNullException.ThrowIfNull(definition);
            ArgumentNullException.ThrowIfNull(exception);

            if (CompositionTraceSource.CanWriteWarning)
            {
                CompositionTraceSource.WriteWarning(CompositionTraceId.Rejection_DefinitionRejected,
                    SR.CompositionTrace_Rejection_DefinitionRejected,
                    definition.GetDisplayName(),
                    exception.Message);
            }
        }

        internal static void AssemblyLoadFailed(DirectoryCatalog catalog, string fileName, Exception exception)
        {
            ArgumentNullException.ThrowIfNull(catalog);
            ArgumentNullException.ThrowIfNull(fileName);
            ArgumentNullException.ThrowIfNull(exception);

            if (fileName.Length == 0)
            {
                throw new ArgumentException(SR.Format(SR.ArgumentException_EmptyString, nameof(fileName)), nameof(fileName));
            }

            if (CompositionTraceSource.CanWriteWarning)
            {
                CompositionTraceSource.WriteWarning(CompositionTraceId.Discovery_AssemblyLoadFailed,
                    SR.CompositionTrace_Discovery_AssemblyLoadFailed,
                    catalog.GetDisplayName(),
                    fileName,
                    exception.Message);
            }
        }

        internal static void DefinitionMarkedWithPartNotDiscoverableAttribute(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            if (CompositionTraceSource.CanWriteInformation)
            {
                CompositionTraceSource.WriteInformation(CompositionTraceId.Discovery_DefinitionMarkedWithPartNotDiscoverableAttribute,
                    SR.CompositionTrace_Discovery_DefinitionMarkedWithPartNotDiscoverableAttribute,
                    type.GetDisplayName());
            }
        }

        internal static void DefinitionMismatchedExportArity(Type type, MemberInfo member)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(member);

            if (CompositionTraceSource.CanWriteInformation)
            {
                CompositionTraceSource.WriteInformation(CompositionTraceId.Discovery_DefinitionMismatchedExportArity,
                    SR.CompositionTrace_Discovery_DefinitionMismatchedExportArity,
                    type.GetDisplayName(), member.GetDisplayName());
            }
        }

        internal static void DefinitionContainsNoExports(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            if (CompositionTraceSource.CanWriteInformation)
            {
                CompositionTraceSource.WriteInformation(CompositionTraceId.Discovery_DefinitionContainsNoExports,
                    SR.CompositionTrace_Discovery_DefinitionContainsNoExports,
                    type.GetDisplayName());
            }
        }

        internal static void MemberMarkedWithMultipleImportAndImportMany(ReflectionItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            if (CompositionTraceSource.CanWriteError)
            {
                CompositionTraceSource.WriteError(CompositionTraceId.Discovery_MemberMarkedWithMultipleImportAndImportMany,
                    SR.CompositionTrace_Discovery_MemberMarkedWithMultipleImportAndImportMany,
                    item.GetDisplayName());
            }
        }
    }
}
