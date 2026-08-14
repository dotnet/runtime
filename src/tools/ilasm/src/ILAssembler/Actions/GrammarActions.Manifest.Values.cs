// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Antlr4.Runtime;

namespace ILAssembler;

internal sealed partial class GrammarActions
{
    private sealed class ManifestValueListFrame<TContext>
        where TContext : ParserRuleContext
    {
        public ManifestValueListFrame(TContext owner)
        {
            Owner = owner;
        }

        public TContext Owner { get; }

        public ImmutableArray<object>.Builder? Values { get; set; }
    }

    private sealed record AssemblyDefinitionValue(
        AssemblyFlags Attributes,
        string Name,
        ImmutableArray<object> Declarations);

    private sealed record AssemblyReferenceValue(
        AssemblyReferenceHeaderValue Header,
        ImmutableArray<object> Declarations);

    private sealed record AssemblyReferenceHeaderValue(
        AssemblyFlags Attributes,
        string Name,
        string Alias);

    private sealed record AssemblyPublicKeyDirectiveValue(ImmutableArray<byte> Value);

    private sealed record AssemblyVersionDirectiveValue(Version Value);

    private sealed record AssemblyLocaleDirectiveValue(string Value);

    private sealed record AssemblyCustomAttributeDirectiveValue(object? Value, IToken Location);

    private sealed record AssemblyHashAlgorithmDirectiveValue(AssemblyHashAlgorithm Value);

    private sealed record AssemblySecurityDirectiveValue(object? Value, IToken Location);

    private sealed record AssemblyReferenceHashDirectiveValue(ImmutableArray<byte> Value);

    private sealed record AssemblyReferencePublicKeyTokenDirectiveValue(ImmutableArray<byte> Value);

    private sealed record FileDeclarationValue(
        string Name,
        bool HasMetadata,
        bool IsEntryPoint,
        ImmutableArray<byte>? Hash);

    private sealed record ExportedTypeValue(
        ExportedTypeHeaderValue Header,
        ImmutableArray<object> Declarations);

    private sealed record ExportedTypeHeaderValue(
        TypeAttributes Attributes,
        string Name,
        IToken Location);

    private sealed record ExportedTypeFileDirectiveValue(string Name, IToken Location);

    private sealed record NestedExportedTypeDirectiveValue(TypeName Name, IToken Location);

    private sealed record ExportedTypeAssemblyDirectiveValue(string Name, IToken Location);

    private sealed record ExportedTypeMetadataTokenDirectiveValue(int Token, IToken Location);

    private sealed record ExportedTypeDefinitionIdDirectiveValue(int Value);

    private sealed record ExportedTypeCustomAttributeDirectiveValue(object? Value, IToken Location);

    private sealed record ManifestResourceValue(
        ManifestResourceHeaderValue Header,
        ImmutableArray<object> Declarations);

    private sealed record ManifestResourceHeaderValue(
        ManifestResourceAttributes Attributes,
        string Name,
        string Alias,
        IToken Location);

    private sealed record ManifestResourceFileDirectiveValue(
        string Name,
        uint Offset,
        IToken Location);

    private sealed record ManifestResourceAssemblyDirectiveValue(string Name);

    private sealed record ManifestResourceCustomAttributeDirectiveValue(
        object? Value,
        IToken Location);

    private sealed record RawVTableValue(ImmutableArray<byte> Value);

    private sealed record VTableFixupValue(int SlotCount, ushort Flags, string DataLabel);

    private abstract record TypedefDeclarationValue(string Alias);

    private sealed record TypeSignatureTypedefDeclarationValue(TypeValue Type, string Alias)
        : TypedefDeclarationValue(Alias);

    private sealed record ClassTypedefDeclarationValue(ClassNameValue Type, string Alias)
        : TypedefDeclarationValue(Alias);

    private sealed record MemberTypedefDeclarationValue(MemberReferenceValue Member, string Alias)
        : TypedefDeclarationValue(Alias);

    private sealed record CustomAttributeTypedefDeclarationValue(
        CustomAttributeDescriptorValue Attribute,
        IToken Location,
        string Alias)
        : TypedefDeclarationValue(Alias);

    private abstract record TypedefEntry
    {
        public sealed record Type(EntityRegistry.TypeEntity Entity) : TypedefEntry;

        public sealed record TypeBlob(System.Reflection.Metadata.BlobBuilder Blob) : TypedefEntry;

        public sealed record Member(EntityRegistry.EntityBase Entity) : TypedefEntry;

        public sealed record CustomAttribute(
            EntityRegistry.EntityBase Constructor,
            System.Reflection.Metadata.BlobBuilder Value) : TypedefEntry;
    }

    private static void AddManifestValue<TContext>(
        Stack<ManifestValueListFrame<TContext>> frames,
        TContext context,
        object? value)
        where TContext : ParserRuleContext
    {
        if (value is not null && TryGetManifestValueListFrame(frames, context) is { } frame)
        {
            (frame.Values ??= ImmutableArray.CreateBuilder<object>()).Add(value);
        }
    }

    private static ImmutableArray<object> EndManifestValues<TContext>(
        Stack<ManifestValueListFrame<TContext>> frames,
        TContext context)
        where TContext : ParserRuleContext
    {
        if (TryGetManifestValueListFrame(frames, context) is not { } frame)
        {
            return [];
        }

        frames.Pop();
        return frame.Values?.ToImmutable() ?? [];
    }

    private static ManifestValueListFrame<TContext>? TryGetManifestValueListFrame<TContext>(
        Stack<ManifestValueListFrame<TContext>> frames,
        TContext context)
        where TContext : ParserRuleContext
    {
        Debug.Assert(frames.Count > 0);
        ManifestValueListFrame<TContext>? frame = frames.Count == 0 ? null : frames.Peek();
        Debug.Assert(frame is null || ReferenceEquals(frame.Owner, context));
        return frame is not null && ReferenceEquals(frame.Owner, context) ? frame : null;
    }

    private static ImmutableArray<object> GetManifestValues(object? value)
        => value is ImmutableArray<object> values ? values : [];
}
