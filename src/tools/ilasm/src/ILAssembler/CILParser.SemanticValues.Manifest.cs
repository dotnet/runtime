// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using Antlr4.Runtime;

namespace ILAssembler;

public partial class CILParser
{
    public abstract record AssemblyDeclarationValue;

    public sealed record AssemblyDefinitionValue(
        AssemblyFlags Attributes,
        string Name,
        ImmutableArray<AssemblyDeclarationValue> Declarations);

    public sealed record AssemblyReferenceValue(
        AssemblyReferenceHeaderValue Header,
        ImmutableArray<AssemblyDeclarationValue> Declarations);

    public sealed record AssemblyReferenceHeaderValue(
        bool IsValid,
        AssemblyFlags Attributes,
        string Name,
        string Alias)
    {
        public static AssemblyReferenceHeaderValue Error { get; } =
            new(false, 0, string.Empty, string.Empty);
    }

    public sealed record AssemblyPublicKeyDirectiveValue(
        ImmutableArray<byte> Value) : AssemblyDeclarationValue;

    public sealed record AssemblyVersionDirectiveValue(
        Version Value) : AssemblyDeclarationValue;

    public sealed record AssemblyLocaleDirectiveValue(
        string Value) : AssemblyDeclarationValue;

    public sealed record AssemblyCustomAttributeDirectiveValue(
        CustomAttributeDeclarationValue? Value,
        IToken Location) : AssemblyDeclarationValue;

    public sealed record AssemblyHashAlgorithmDirectiveValue(
        AssemblyHashAlgorithm Value) : AssemblyDeclarationValue;

    public sealed record AssemblySecurityDirectiveValue(
        SecurityDeclarationValue? Value,
        IToken Location) : AssemblyDeclarationValue;

    public sealed record AssemblyReferenceHashDirectiveValue(
        ImmutableArray<byte> Value) : AssemblyDeclarationValue;

    public sealed record AssemblyReferencePublicKeyTokenDirectiveValue(
        ImmutableArray<byte> Value) : AssemblyDeclarationValue;

    public sealed record FileDeclarationValue(
        string Name,
        bool HasMetadata,
        bool IsEntryPoint,
        ImmutableArray<byte>? Hash);

    public sealed class FileDeclarationBuilder
    {
        internal string Name { get; set; } = string.Empty;

        internal bool HasMetadata { get; set; } = true;

        internal bool IsEntryPoint { get; set; }

        internal ImmutableArray<byte>? Hash { get; set; }
    }

    public abstract record ExportedTypeDeclarationValue;

    public sealed record ExportedTypeValue(
        ExportedTypeHeaderValue Header,
        ImmutableArray<ExportedTypeDeclarationValue> Declarations);

    public sealed record ExportedTypeHeaderValue(
        bool IsValid,
        TypeAttributes Attributes,
        string Name,
        IToken? Location)
    {
        public static ExportedTypeHeaderValue Error { get; } =
            new(false, 0, string.Empty, null);
    }

    public sealed record ExportedTypeFileDirectiveValue(
        string Name,
        IToken Location) : ExportedTypeDeclarationValue;

    public sealed record NestedExportedTypeDirectiveValue(
        TypeName Name,
        IToken Location) : ExportedTypeDeclarationValue;

    public sealed record ExportedTypeAssemblyDirectiveValue(
        string Name,
        IToken Location) : ExportedTypeDeclarationValue;

    public sealed record ExportedTypeMetadataTokenDirectiveValue(
        int Token,
        IToken Location) : ExportedTypeDeclarationValue;

    public sealed record ExportedTypeDefinitionIdDirectiveValue(
        int Value) : ExportedTypeDeclarationValue;

    public sealed record ExportedTypeCustomAttributeDirectiveValue(
        CustomAttributeDeclarationValue? Value,
        IToken Location) : ExportedTypeDeclarationValue;

    public abstract record ManifestResourceDeclarationValue;

    public sealed record ManifestResourceValue(
        ManifestResourceHeaderValue Header,
        ImmutableArray<ManifestResourceDeclarationValue> Declarations);

    public sealed record ManifestResourceHeaderValue(
        bool IsValid,
        ManifestResourceAttributes Attributes,
        string Name,
        string Alias,
        IToken? Location)
    {
        public static ManifestResourceHeaderValue Error { get; } =
            new(false, 0, string.Empty, string.Empty, null);
    }

    public sealed record ManifestResourceFileDirectiveValue(
        string Name,
        uint Offset,
        IToken Location) : ManifestResourceDeclarationValue;

    public sealed record ManifestResourceAssemblyDirectiveValue(
        string Name) : ManifestResourceDeclarationValue;

    public sealed record ManifestResourceCustomAttributeDirectiveValue(
        CustomAttributeDeclarationValue? Value,
        IToken Location) : ManifestResourceDeclarationValue;

    public sealed record RawVTableValue(ImmutableArray<byte> Value);

    public sealed record VTableFixupValue(int SlotCount, ushort Flags, string DataLabel);

    public abstract record TypedefDeclarationValue(string Alias)
    {
        public static TypedefDeclarationValue Error { get; } =
            new ErrorTypedefDeclarationValue();
    }

    public sealed record ErrorTypedefDeclarationValue()
        : TypedefDeclarationValue(string.Empty);

    public sealed record TypeSignatureTypedefDeclarationValue(
        TypeValue Type,
        string Alias) : TypedefDeclarationValue(Alias);

    public sealed record ClassTypedefDeclarationValue(
        ClassNameValue Type,
        string Alias) : TypedefDeclarationValue(Alias);

    public sealed record MemberTypedefDeclarationValue(
        MemberReferenceValue Member,
        string Alias) : TypedefDeclarationValue(Alias);

    public sealed record CustomAttributeTypedefDeclarationValue(
        CustomAttributeDescriptorValue Attribute,
        IToken Location,
        string Alias) : TypedefDeclarationValue(Alias);

    public abstract class TypedefEntry
    {
        public sealed class Type : TypedefEntry
        {
            internal Type(EntityRegistry.TypeEntity entity)
            {
                Entity = entity;
            }

            internal EntityRegistry.TypeEntity Entity { get; }
        }

        public sealed class TypeBlob : TypedefEntry
        {
            internal TypeBlob(BlobBuilder blob)
            {
                Blob = blob;
            }

            internal BlobBuilder Blob { get; }
        }

        public sealed class Member : TypedefEntry
        {
            internal Member(EntityRegistry.EntityBase entity)
            {
                Entity = entity;
            }

            internal EntityRegistry.EntityBase Entity { get; }
        }

        public sealed class CustomAttribute : TypedefEntry
        {
            internal CustomAttribute(
                EntityRegistry.EntityBase constructor,
                BlobBuilder value)
            {
                Constructor = constructor;
                Value = value;
            }

            internal EntityRegistry.EntityBase Constructor { get; }

            internal BlobBuilder Value { get; }
        }
    }
}
