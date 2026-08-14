// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using Antlr4.Runtime;

namespace ILAssembler;

#pragma warning disable CA1822 // Parser actions are invoked through the per-parser GrammarActions instance.
internal sealed partial class GrammarActions
{
    internal void SetAssemblyAttribute(CILParser.AsmAttrAnyContext context)
    {
        (AssemblyFlags value, AssemblyFlags mask) = context.Start.Text switch
        {
            "retargetable" => (AssemblyFlags.Retargetable, (AssemblyFlags)0),
            "windowsruntime" => (AssemblyFlags.WindowsRuntime, (AssemblyFlags)0),
            "noplatform" => (AssemblyFlags.NoPlatform, (AssemblyFlags)0),
            "legacy library" => ((AssemblyFlags)0, (AssemblyFlags)0),
            "cil" => (GetFlagForArch(ProcessorArchitecture.MSIL), AssemblyFlags.ArchitectureMask),
            "x86" => (GetFlagForArch(ProcessorArchitecture.X86), AssemblyFlags.ArchitectureMask),
            "amd64" => (GetFlagForArch(ProcessorArchitecture.Amd64), AssemblyFlags.ArchitectureMask),
            "arm" => (GetFlagForArch(ProcessorArchitecture.Arm), AssemblyFlags.ArchitectureMask),
            "arm64" => (GetFlagForArch((ProcessorArchitecture)6), AssemblyFlags.ArchitectureMask),
            _ => throw new UnreachableException()
        };
        context.Value = value;
        context.Mask = mask;
    }

    internal AssemblyFlags AddAssemblyAttribute(
        AssemblyFlags attributes,
        AssemblyFlags value,
        AssemblyFlags mask)
        => mask == 0 ? attributes | value : (attributes & ~mask) | value;

    internal AssemblyDefinitionValue CreateAssemblyDefinition(
        AssemblyFlags attributes,
        string name,
        ImmutableArray<AssemblyDeclarationValue> declarations)
        => new(attributes, name, declarations);

    internal AssemblyDeclarationValue CreateAssemblyHashAlgorithmDeclaration(IToken value)
        => new AssemblyHashAlgorithmDirectiveValue((AssemblyHashAlgorithm)ParseInt32(value));

    internal AssemblyDeclarationValue CreateAssemblySecurityDeclaration(
        SecurityDeclarationValue? value,
        IToken location)
        => new AssemblySecurityDirectiveValue(value, location);

    internal AssemblyDeclarationValue CreateAssemblyPublicKeyDeclaration(
        ImmutableArray<byte> value)
        => new AssemblyPublicKeyDirectiveValue(value);

    internal AssemblyDeclarationValue CreateAssemblyVersionDeclaration(
        int? major,
        int? minor,
        int? build,
        int? revision)
        => new AssemblyVersionDirectiveValue(new(
            major ?? 0,
            minor ?? 0,
            build ?? 0,
            revision ?? 0));

    internal AssemblyDeclarationValue CreateAssemblyLocaleDeclaration(string value)
        => new AssemblyLocaleDirectiveValue(value);

    internal AssemblyDeclarationValue CreateAssemblyLocaleDeclaration(
        ImmutableArray<byte> value)
        => new AssemblyLocaleDirectiveValue(Encoding.Unicode.GetString(value.AsSpan()));

    internal AssemblyDeclarationValue CreateAssemblyCustomAttributeDeclaration(
        CustomAttributeDeclarationValue? value,
        IToken location)
        => new AssemblyCustomAttributeDirectiveValue(value, location);

    private static AssemblyFlags GetFlagForArch(ProcessorArchitecture architecture)
        => (AssemblyFlags)((int)architecture << 4);

    private static (ProcessorArchitecture Architecture, AssemblyFlags Flags) GetArchAndFlags(
        AssemblyFlags flags)
    {
        ProcessorArchitecture architecture =
            (ProcessorArchitecture)(((int)flags & 0xF0) >> 4);
        return (architecture, flags & ~GetFlagForArch(architecture));
    }

    private void MaterializeAssemblyDefinition(AssemblyDefinitionValue definition)
    {
        string assemblyName = _options.AssemblyName ?? definition.Name;
        _entityRegistry.Assembly ??= new EntityRegistry.AssemblyEntity(assemblyName);
        EntityRegistry.AssemblyEntity assembly = _entityRegistry.Assembly;
        (assembly.ProcessorArchitecture, assembly.Flags) =
            GetArchAndFlags(definition.Attributes);

        foreach (AssemblyDeclarationValue declaration in definition.Declarations)
        {
            switch (declaration)
            {
                case AssemblyHashAlgorithmDirectiveValue hashAlgorithm:
                    assembly.HashAlgorithm = hashAlgorithm.Value;
                    break;
                case AssemblySecurityDirectiveValue security:
                    if (security.Value is { } securityValue &&
                        MaterializeSecurityDeclaration(securityValue, security.Location) is { } entity)
                    {
                        entity.Parent = assembly;
                    }
                    break;
                default:
                    ApplyAssemblyOrReferenceDirective(assembly, declaration);
                    break;
            }
        }

        if (_options.KeyFile is not null)
        {
            ApplyKeyFile(_options.KeyFile);
        }
    }

    private void ApplyAssemblyOrReferenceDirective(
        EntityRegistry.AssemblyOrRefEntity target,
        AssemblyDeclarationValue declaration)
    {
        switch (declaration)
        {
            case AssemblyPublicKeyDirectiveValue publicKey:
                // COMPAT: A reference's public key token wins regardless of declaration order.
                if (target is not EntityRegistry.AssemblyReferenceEntity assemblyReference ||
                    assemblyReference.PublicKeyOrToken is null ||
                    assemblyReference.Flags.HasFlag(AssemblyFlags.PublicKey))
                {
                    target.PublicKeyOrToken = CreateManifestBlob(publicKey.Value);
                    target.Flags |= AssemblyFlags.PublicKey;
                }
                break;
            case AssemblyVersionDirectiveValue version:
                target.Version = version.Value;
                break;
            case AssemblyLocaleDirectiveValue locale:
                target.Culture = locale.Value;
                break;
            case AssemblyCustomAttributeDirectiveValue customAttribute:
                MaterializeCustomAttributeDeclaration(
                    customAttribute.Value,
                    customAttribute.Location)?.Owner = target;
                break;
        }
    }

    private static BlobBuilder CreateManifestBlob(ImmutableArray<byte> value)
    {
        BlobBuilder blob = new(value.Length);
        blob.WriteBytes(value);
        return blob;
    }

    private void ApplyKeyFile(string keyFilePath)
    {
        if (_entityRegistry.Assembly is null)
        {
            return;
        }

        try
        {
            byte[] keyBytes = File.ReadAllBytes(keyFilePath);
            BlobBuilder blob = new(keyBytes.Length);
            blob.WriteBytes(keyBytes);
            _entityRegistry.Assembly.PublicKeyOrToken = blob;
            _entityRegistry.Assembly.Flags |= AssemblyFlags.PublicKey;
        }
        catch (Exception ex)
        {
            SourceText? firstDocument = _documents.Values.FirstOrDefault();
            Location location = firstDocument is not null
                ? new Location(new SourceSpan(0, 0), firstDocument)
                : new Location(new SourceSpan(0, 0), new SourceText(string.Empty, keyFilePath));
            _diagnostics.Add(new Diagnostic(
                DiagnosticIds.KeyFileError,
                DiagnosticSeverity.Error,
                $"Failed to read key file '{keyFilePath}': {ex.Message}",
                location));
        }
    }

    internal void MaterializeAssemblyDefinition(CILParser.AssemblyBlockContext context)
    {
        if (context.Value is AssemblyDefinitionValue definition)
        {
            MaterializeAssemblyDefinition(definition);
        }
    }
}
