// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Metadata.Ecma335;

namespace ILAssembler;

/// <summary>
/// Lowers "pseudo custom attributes" into the metadata flag bits and auxiliary table rows that they
/// represent, and suppresses the <c>CustomAttribute</c> row for the attributes that are not retained.
/// </summary>
/// <remarks>
/// This mirrors <c>RegMeta::DefineCustomAttribute</c> in <c>src/coreclr/md/compiler/custattr_emit.cpp</c>,
/// which the native IL assembler relies on for the same behavior. Attributes are matched on the
/// namespace and name of the declaring type of the constructor only; the assembly the constructor
/// resolves to is deliberately not considered, matching the native implementation.
/// </remarks>
internal static partial class PseudoCustomAttributes
{
    private static readonly Location s_unknownLocation = new(new SourceSpan(0, 0), new SourceText("", ""));

    public static void Lower(EntityRegistry registry, ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        var attributes = new List<EntityRegistry.CustomAttributeEntity>();
        foreach (var entity in registry.GetSeenEntities(TableIndex.CustomAttribute))
        {
            if (entity is EntityRegistry.CustomAttributeEntity attribute)
            {
                attributes.Add(attribute);
            }
        }

        if (attributes.Count == 0)
        {
            return;
        }

        var lowered = new List<EntityRegistry.CustomAttributeEntity>();

        // Attributes are processed in source order so that a later directive can override an
        // earlier one, matching the native emitter which applies each attribute as it is defined.
        foreach (var attribute in attributes)
        {
            if (attribute.Owner is null)
            {
                continue;
            }

            if (!TryGetAttributeTypeName(attribute.Constructor, out string @namespace, out string name))
            {
                continue;
            }

            KnownAttribute? known = TryFindKnownAttribute(attribute.Constructor, @namespace, name);
            if (known is null && !IsSecurityAttribute(@namespace, name))
            {
                continue;
            }

            var context = new LoweringContext(registry, diagnostics, attribute, @namespace, name);
            if (known is not null)
            {
                // The native emitter abandons the whole DefineCustomAttribute call when a known
                // attribute fails validation, so the row is never written regardless of KeepAttribute.
                if (!Apply(context, known) || !known.KeepAttribute)
                {
                    lowered.Add(attribute);
                }

                continue;
            }

            if (ApplySecurityAttribute(context, @namespace, name, out bool keepSecurityAttribute)
                && !keepSecurityAttribute)
            {
                lowered.Add(attribute);
            }
        }

        registry.RemoveCustomAttributes(lowered);
    }

    private readonly struct LoweringContext(
        EntityRegistry registry,
        ImmutableArray<Diagnostic>.Builder diagnostics,
        EntityRegistry.CustomAttributeEntity attribute,
        string @namespace,
        string name)
    {
        public EntityRegistry Registry { get; } = registry;
        public EntityRegistry.CustomAttributeEntity Attribute { get; } = attribute;

        /// <summary>
        /// The attribute target. References to members and types defined in this module are only
        /// resolved while metadata rows are written, which happens after this pass runs, so the
        /// owner recorded during parsing is resolved to the entity it designates here.
        /// </summary>
        public EntityRegistry.EntityBase Owner { get; } = ResolveOwner(registry, attribute.Owner!);

        public string AttributeName { get; } = @namespace.Length == 0 ? name : @namespace + "." + name;

        public bool Error(string id, string message)
        {
            diagnostics.Add(new Diagnostic(id, DiagnosticSeverity.Error, message, Attribute.Location ?? s_unknownLocation));
            return false;
        }

        public bool InvalidTarget() => Error(
            DiagnosticIds.PseudoCustomAttributeInvalidTarget,
            string.Format(DiagnosticMessageTemplates.PseudoCustomAttributeInvalidTarget, AttributeName));

        public bool InvalidValue() => Error(
            DiagnosticIds.PseudoCustomAttributeInvalidValue,
            string.Format(DiagnosticMessageTemplates.PseudoCustomAttributeInvalidValue, AttributeName));

        public bool InvalidBlob() => Error(
            DiagnosticIds.PseudoCustomAttributeInvalidBlob,
            string.Format(DiagnosticMessageTemplates.PseudoCustomAttributeInvalidBlob, AttributeName));

        public bool InvalidGuid() => Error(
            DiagnosticIds.PseudoCustomAttributeInvalidGuid,
            string.Format(DiagnosticMessageTemplates.PseudoCustomAttributeInvalidGuid, AttributeName));

        public bool UnknownArgument(string argumentName) => Error(
            DiagnosticIds.PseudoCustomAttributeUnknownArgument,
            string.Format(DiagnosticMessageTemplates.PseudoCustomAttributeUnknownArgument, AttributeName, argumentName));

        public bool RepeatedArgument(string argumentName) => Error(
            DiagnosticIds.PseudoCustomAttributeRepeatedArgument,
            string.Format(DiagnosticMessageTemplates.PseudoCustomAttributeRepeatedArgument, AttributeName, argumentName));
    }






}
