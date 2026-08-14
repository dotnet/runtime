// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;

namespace ILAssembler;

#pragma warning disable CA1822 // Parser actions are invoked through the per-parser GrammarActions instance.
internal sealed partial class GrammarActions
{
    private readonly Stack<ManifestValueListFrame<CILParser.AssemblyRefDeclsContext>>
        _assemblyReferenceDeclarationFrames = new();

    internal object CreateAssemblyReferenceHeader(
        AssemblyFlags attributes,
        string name,
        string alias)
        => new AssemblyReferenceHeaderValue(attributes, name, alias);

    internal void BeginAssemblyReferenceDeclarations(CILParser.AssemblyRefDeclsContext context)
        => _assemblyReferenceDeclarationFrames.Push(new(context));

    internal void AddAssemblyReferenceDeclaration(
        CILParser.AssemblyRefDeclsContext context,
        object? value)
        => AddManifestValue(_assemblyReferenceDeclarationFrames, context, value);

    internal object EndAssemblyReferenceDeclarations(CILParser.AssemblyRefDeclsContext context)
        => EndManifestValues(_assemblyReferenceDeclarationFrames, context);

    internal object CreateAssemblyReference(object? header, object? declarations)
        => new AssemblyReferenceValue(
            GetAssemblyReferenceHeader(header),
            GetManifestValues(declarations));

    internal object CreateAssemblyReferenceHashDeclaration(ImmutableArray<byte> value)
        => new AssemblyReferenceHashDirectiveValue(value);

    internal object CreateAssemblyReferencePublicKeyTokenDeclaration(ImmutableArray<byte> value)
        => new AssemblyReferencePublicKeyTokenDirectiveValue(value);

    internal object? CreateAssemblyReferenceAutoDeclaration() => null;

    private EntityRegistry.AssemblyReferenceEntity MaterializeAssemblyReferenceHeader(
        AssemblyReferenceHeaderValue header)
    {
        (ProcessorArchitecture architecture, AssemblyFlags flags) =
            GetArchAndFlags(header.Attributes);
        return _entityRegistry.GetOrCreateAssemblyReference(
            header.Alias,
            assemblyReference =>
            {
                assemblyReference.Name = header.Name;
                assemblyReference.Flags = flags;
                assemblyReference.ProcessorArchitecture = architecture;
            });
    }

    private void MaterializeAssemblyReference(AssemblyReferenceValue reference)
    {
        EntityRegistry.AssemblyReferenceEntity entity =
            MaterializeAssemblyReferenceHeader(reference.Header);
        foreach (object declaration in reference.Declarations)
        {
            switch (declaration)
            {
                case AssemblyReferenceHashDirectiveValue hash:
                    entity.Hash = CreateManifestBlob(hash.Value);
                    break;
                case AssemblyReferencePublicKeyTokenDirectiveValue publicKeyToken:
                    entity.PublicKeyOrToken = CreateManifestBlob(publicKeyToken.Value);
                    entity.Flags &= ~AssemblyFlags.PublicKey;
                    break;
                default:
                    ApplyAssemblyOrReferenceDirective(entity, declaration);
                    break;
            }
        }
    }

    private static AssemblyReferenceHeaderValue GetAssemblyReferenceHeader(object? value)
        => value as AssemblyReferenceHeaderValue ?? new(0, string.Empty, string.Empty);

    public GrammarResult VisitAssemblyRefBlock(CILParser.AssemblyRefBlockContext context)
    {
        if (context.Value is AssemblyReferenceValue reference)
        {
            MaterializeAssemblyReference(reference);
        }

        return GrammarResult.SentinelValue.Result;
    }

}
