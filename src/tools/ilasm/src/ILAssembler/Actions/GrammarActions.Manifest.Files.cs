// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using Antlr4.Runtime;

namespace ILAssembler;

#pragma warning disable CA1822 // Parser actions are invoked through the per-parser GrammarActions instance.
internal sealed partial class GrammarActions
{
    private readonly Stack<FileDeclarationFrame> _fileDeclarationFrames = new();

    private sealed class FileDeclarationFrame
    {
        public FileDeclarationFrame(CILParser.FileDeclContext owner)
        {
            Owner = owner;
        }

        public CILParser.FileDeclContext Owner { get; }

        public string Name { get; set; } = string.Empty;

        public bool HasMetadata { get; set; } = true;

        public bool IsEntryPoint { get; set; }

        public ImmutableArray<byte>? Hash { get; set; }
    }

    internal void BeginFileDeclaration(CILParser.FileDeclContext context)
    {
        BeginSemanticRoot(context);
        _fileDeclarationFrames.Push(new(context));
    }

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

    internal void AddFileAttribute(CILParser.FileDeclContext context, bool hasMetadata)
    {
        if (TryGetFileDeclarationFrame(context) is { } frame)
        {
            frame.HasMetadata &= hasMetadata;
        }
    }

    internal void SetFileName(CILParser.FileDeclContext context, string name)
    {
        if (TryGetFileDeclarationFrame(context) is { } frame)
        {
            frame.Name = name;
        }
    }

    internal void AddFileEntry(CILParser.FileDeclContext context, bool isEntryPoint)
    {
        if (TryGetFileDeclarationFrame(context) is { } frame)
        {
            frame.IsEntryPoint |= isEntryPoint;
        }
    }

    internal void SetFileHash(CILParser.FileDeclContext context, ImmutableArray<byte> hash)
    {
        if (TryGetFileDeclarationFrame(context) is { } frame)
        {
            frame.Hash = hash;
        }
    }

    internal void EndFileDeclaration(CILParser.FileDeclContext context)
    {
        context.HasSyntaxError = EndSemanticRoot(context);
        FileDeclarationFrame? frame = TryGetFileDeclarationFrame(context);
        if (frame is null)
        {
            context.Value = null;
            return;
        }

        _fileDeclarationFrames.Pop();
        context.Value = context.HasSyntaxError
            ? null
            : new FileDeclarationValue(
                frame.Name,
                frame.HasMetadata,
                frame.IsEntryPoint,
                frame.Hash);
    }

    private FileDeclarationFrame? TryGetFileDeclarationFrame(CILParser.FileDeclContext context)
    {
        Debug.Assert(_fileDeclarationFrames.Count > 0);
        FileDeclarationFrame? frame =
            _fileDeclarationFrames.Count == 0 ? null : _fileDeclarationFrames.Peek();
        Debug.Assert(frame is null || ReferenceEquals(frame.Owner, context));
        return frame is not null && ReferenceEquals(frame.Owner, context) ? frame : null;
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
        Debug.Assert(context.Value is FileDeclarationValue);
        FileDeclarationValue declaration = context.Value as FileDeclarationValue
            ?? new(string.Empty, HasMetadata: true, IsEntryPoint: false, Hash: null);
        return MaterializeFileDeclaration(declaration);
    }
}
