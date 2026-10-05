// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.Metadata.Ecma;

/// <summary>
/// Serializes reflection metadata as independent ECMA-335 metadata roots.
/// </summary>
public static class EcmaMetadataTransform
{
    /// <summary>
    /// Generates concatenated metadata roots for the modules selected by the metadata policy.
    /// Optionally writes metadata-only DLLs to <paramref name="outputDirectory"/>.
    /// </summary>
    /// <remarks>
    /// Includes reflection metadata only, not the additional records used exclusively for stack traces.
    /// </remarks>
    public static byte[] Run<TPolicy>(TPolicy policy, IEnumerable<ModuleDesc> modules, string outputDirectory = null)
        where TPolicy : struct, IMetadataPolicy
    {
        var sortedModules = new List<EcmaModule>();
        foreach (ModuleDesc module in modules)
            sortedModules.Add((EcmaModule)module);
        sortedModules.Sort(TypeSystemComparer.Instance.Compare);

        if (outputDirectory is not null)
            Directory.CreateDirectory(outputDirectory);

        var result = new BlobBuilder();
        foreach (EcmaModule module in sortedModules)
        {
            var transform = new Transform<TPolicy>(module, policy);
            if (!transform.HasMetadata)
                continue;

            var metadataRoot = transform.Generate();
            if (outputDirectory is null)
            {
                var blob = new BlobBuilder();
                metadataRoot.Serialize(blob, methodBodyStreamRva: 0, mappedFieldDataStreamRva: 0);
                blob.WriteContentTo(result);
            }
            else
            {
                var pe = new ManagedPEBuilder(
                    new PEHeaderBuilder(imageCharacteristics: Characteristics.ExecutableImage | Characteristics.Dll),
                    metadataRoot,
                    ilStream: new BlobBuilder(),
                    deterministicIdProvider: static _ => default);
                var peBlob = new BlobBuilder();
                pe.Serialize(peBlob);
                // SRM serialization consumes the heap builders. Extract the metadata from
                // this serialization instead of attempting to serialize the root again.
                using var reader = new PEReader(peBlob.ToImmutableArray());
                result.WriteBytes(reader.GetMetadata().GetContent());
                AssemblyNameInfo name = module.Assembly.GetName();
                string directory = outputDirectory;
                if (!string.IsNullOrEmpty(name.CultureName))
                {
                    directory = Path.Combine(directory, name.CultureName);
                    Directory.CreateDirectory(directory);
                }
                using FileStream stream = File.Create(Path.Combine(directory, name.Name + ".dll"));
                peBlob.WriteContentTo(stream);
            }
        }

        return result.ToArray();
    }
}
