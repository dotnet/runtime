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
/// Recognizes the metadata transforms supported by native ILAsm, using C# compiler behavior for
/// decoded argument values rather than legacy metadata-emitter validation. Attributes that only
/// require validation are left untouched. Matching uses the namespace and name of the constructor's
/// declaring type without requiring a particular assembly.
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
        var deferredFieldOffsets = new List<(LoweringContext Context, KnownAttribute Known)>();
        var fieldOffsetsWithDeferredConstructors = new List<(LoweringContext Context, KnownAttribute Known)>();

        // Preserve source order within each native emission phase.
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
            if (known is null)
            {
                continue;
            }

            var context = new LoweringContext(registry, diagnostics, attribute, @namespace, name);
            if (known.KeepOnInvalidTarget && (known.Targets & GetTarget(context.Owner)) == 0)
            {
                continue;
            }

            // COMPAT: Native ilasm applies attributes with unresolved local member references after
            // field attributes and explicit layout, even if they appear earlier in the source.
            if (known.Kind == KnownAttributeKind.FieldOffset && context.IsDeferred)
            {
                if (RequiresMemberReferenceResolution(registry, attribute.Owner))
                {
                    deferredFieldOffsets.Add((context, known));
                }
                else
                {
                    fieldOffsetsWithDeferredConstructors.Add((context, known));
                }
                continue;
            }

            if (!Apply(context, known) || !known.KeepAttribute)
            {
                lowered.Add(attribute);
            }
        }

        // Explicit owners are queued during parsing; field attributes with local constructors
        // are queued later, when native ilasm emits the field definitions.
        deferredFieldOffsets.AddRange(fieldOffsetsWithDeferredConstructors);
        foreach ((LoweringContext context, KnownAttribute known) in deferredFieldOffsets)
        {
            Apply(context, known);
            lowered.Add(context.Attribute);
        }

        registry.RemoveCustomAttributes(lowered);
    }

    private sealed class LoweringContext(
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

        public bool IsDeferred { get; } =
            RequiresMemberReferenceResolution(registry, attribute.Owner)
            || RequiresMemberReferenceResolution(registry, attribute.Constructor);

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

        public bool UnknownArgument(string argumentName) => Error(
            DiagnosticIds.PseudoCustomAttributeUnknownArgument,
            string.Format(DiagnosticMessageTemplates.PseudoCustomAttributeUnknownArgument, AttributeName, argumentName));

        public bool RepeatedArgument(string argumentName) => Error(
            DiagnosticIds.PseudoCustomAttributeRepeatedArgument,
            string.Format(DiagnosticMessageTemplates.PseudoCustomAttributeRepeatedArgument, AttributeName, argumentName));
    }
}
