// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Reflection;

namespace ILAssembler;

#pragma warning disable CA1822 // Parser actions are invoked through the per-parser GrammarActions instance.
internal sealed partial class GrammarActions
{
    internal AssemblyReferenceHeaderValue CreateAssemblyReferenceHeader(
        AssemblyFlags attributes,
        string name,
        string alias)
        => new AssemblyReferenceHeaderValue(true, attributes, name, alias);

    internal AssemblyReferenceValue CreateAssemblyReference(
        AssemblyReferenceHeaderValue header,
        ImmutableArray<AssemblyDeclarationValue> declarations)
        => new AssemblyReferenceValue(
            header,
            declarations);

    internal AssemblyDeclarationValue CreateAssemblyReferenceHashDeclaration(
        ImmutableArray<byte> value)
        => new AssemblyReferenceHashDirectiveValue(value);

    internal AssemblyDeclarationValue CreateAssemblyReferencePublicKeyTokenDeclaration(
        ImmutableArray<byte> value)
        => new AssemblyReferencePublicKeyTokenDirectiveValue(value);

    internal AssemblyDeclarationValue? CreateAssemblyReferenceAutoDeclaration() => null;

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
        if (!reference.Header.IsValid)
        {
            return;
        }

        EntityRegistry.AssemblyReferenceEntity entity =
            MaterializeAssemblyReferenceHeader(reference.Header);
        foreach (AssemblyDeclarationValue declaration in reference.Declarations)
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

    internal void MaterializeAssemblyReference(CILParser.AssemblyRefBlockContext context)
    {
        if (context.Value is AssemblyReferenceValue reference)
        {
            MaterializeAssemblyReference(reference);
        }
    }

}
