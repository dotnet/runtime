// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace Microsoft.Diagnostics.Tools.Pgo
{
    internal sealed class MethodListTypeSystemContext : MetadataTypeSystemContext, IMetadataStringDecoderProvider, IDisposable
    {
        private readonly Dictionary<string, ModuleDesc> _modules = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<FileStream> _streams = new();
        private readonly List<PEReader> _readers = new();
        private MetadataStringDecoder _metadataStringDecoder;

        public MethodListTypeSystemContext(IReadOnlyList<string> references)
        {
            try
            {
                foreach (string reference in references)
                {
                    if (!File.Exists(reference))
                    {
                        throw new FileNotFoundException($"Unable to find reference '{reference}'.", reference);
                    }

                    FileStream stream = File.OpenRead(reference);
                    _streams.Add(stream);

                    var reader = new PEReader(stream);
                    _readers.Add(reader);

                    if (!reader.HasMetadata)
                    {
                        throw new BadImageFormatException($"Reference '{reference}' does not contain managed metadata.", reference);
                    }

                    EcmaModule module = EcmaModule.Create(this, reader, containingAssembly: null);
                    string simpleName = module.Assembly.GetName().Name;
                    if (!_modules.TryAdd(simpleName, module))
                    {
                        throw new InvalidDataException($"Multiple references define assembly '{simpleName}'.");
                    }
                }

                if (!_modules.TryGetValue("System.Private.CoreLib", out ModuleDesc systemModule))
                {
                    throw new InvalidDataException("A reference to System.Private.CoreLib is required.");
                }

                SetSystemModule(systemModule);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public override bool SupportsCanon => true;

        public override bool SupportsUniversalCanon => false;

        public bool HasAssembly(AssemblyNameInfo name)
        {
            return _modules.ContainsKey(name.Name);
        }

        public void Dispose()
        {
            foreach (PEReader reader in _readers)
            {
                reader.Dispose();
            }

            foreach (FileStream stream in _streams)
            {
                stream.Dispose();
            }
        }

        public MetadataStringDecoder GetMetadataStringDecoder()
        {
            return _metadataStringDecoder ??= new CachingMetadataStringDecoder(0x10000);
        }

        public override ModuleDesc ResolveAssembly(AssemblyNameInfo name, bool throwIfNotFound)
        {
            if (_modules.TryGetValue(name.Name, out ModuleDesc module))
            {
                return module;
            }

            if (throwIfNotFound)
            {
                throw new FileNotFoundException($"Unable to resolve assembly '{name.Name}'.");
            }

            return null;
        }
    }
}
