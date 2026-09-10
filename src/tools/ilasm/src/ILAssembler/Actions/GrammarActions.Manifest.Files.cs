// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using Antlr4.Runtime;

namespace ILAssembler;

#pragma warning disable CA1822 // Parser actions are invoked through the per-parser GrammarActions instance.
internal sealed partial class GrammarActions
{
    internal bool ParseFileAttribute(IToken token)
    {
        Debug.Assert(token.Text == "nometadata");
        return false;
    }

    internal bool ParseFileEntry(IToken token)
    {
        Debug.Assert(token.Text == ".entrypoint");
        return true;
    }

    internal void AddFileAttribute(
        CILParser.FileDeclarationBuilder builder,
        bool hasMetadata)
        => builder.HasMetadata &= hasMetadata;

    internal void SetFileName(CILParser.FileDeclarationBuilder builder, string name)
        => builder.Name = name;

    internal void AddFileEntry(
        CILParser.FileDeclarationBuilder builder,
        bool isEntryPoint)
        => builder.IsEntryPoint |= isEntryPoint;

    internal void SetFileHash(
        CILParser.FileDeclarationBuilder builder,
        ImmutableArray<byte> hash)
        => builder.Hash = hash;

    internal void EndFileDeclaration(
        CILParser.FileDeclContext context,
        CILParser.FileDeclarationBuilder builder,
        int initialSyntaxErrorCount)
    {
        context.HasSyntaxError =
            HasSyntaxErrorsSince(initialSyntaxErrorCount) ||
            context.exception is not null;
        context.Value = context.HasSyntaxError
            ? null
            : new FileDeclarationValue(
                builder.Name,
                builder.HasMetadata,
                builder.IsEntryPoint,
                builder.Hash);
    }

    private EntityRegistry.FileEntity MaterializeFileDeclaration(FileDeclarationValue declaration)
    {
        BlobBuilder? hash = declaration.Hash is { } value ? CreateManifestBlob(value) : null;
        EntityRegistry.FileEntity entity =
            _entityRegistry.GetOrCreateFile(declaration.Name, declaration.HasMetadata, hash);
        if (declaration.IsEntryPoint)
        {
            _entityRegistry.EntryPoint = entity;
        }

        return entity;
    }

    internal EntityRegistry.FileEntity MaterializeFileDeclaration(
        CILParser.FileDeclContext context)
    {
        Debug.Assert(context.Value is not null);
        FileDeclarationValue declaration =
            context.Value ?? new(string.Empty, HasMetadata: true, IsEntryPoint: false, Hash: null);
        return MaterializeFileDeclaration(declaration);
    }
}
