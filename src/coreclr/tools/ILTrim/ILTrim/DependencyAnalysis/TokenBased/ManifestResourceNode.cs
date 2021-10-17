// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

using Internal.TypeSystem.Ecma;

namespace ILTrim.DependencyAnalysis
{
    public class ManifestResourceNode : TokenBasedNode
    {
        private ManifestResourceHandle Handle => (ManifestResourceHandle)_handle;

        private readonly IManifestResourceDependencyAnalyzer _dependencyAnalyzer;
        private readonly bool _skipWritingResource;

        public ManifestResourceNode(EcmaModule module, ManifestResourceHandle handle)
            : base(module, handle)
        {
            MetadataReader reader = _module.MetadataReader;
            ManifestResource resource = reader.GetManifestResource(Handle);

            switch (reader.GetString(resource.Name))
            {
                case "ILLink.Descriptors.xml":
                    _dependencyAnalyzer = new ILLinkDescriptorDependencyAnalyzer(_module, new Dictionary<string, bool>());
                    _skipWritingResource = true;
                    break;

                default:
                    _dependencyAnalyzer = null;
                    _skipWritingResource = false;
                    break;
            }
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            MetadataReader reader = _module.MetadataReader;
            ManifestResource resource = reader.GetManifestResource(Handle);

            if (!resource.Implementation.IsNil)
            {
                DependencyList dependencies = new();
                dependencies.Add(factory.GetNodeForToken(_module, resource.Implementation), "Implementation of a manifest resource");
                return dependencies;
            }
            else if (_dependencyAnalyzer != null)
            {
                PEMemoryBlock resourceDirectory = _module.PEReader.GetSectionData(_module.PEReader.PEHeaders.CorHeader.ResourcesDirectory.RelativeVirtualAddress);
                BlobReader blobReader = resourceDirectory.GetReader((int)resource.Offset, _module.PEReader.PEHeaders.CorHeader.ResourcesDirectory.Size - (int)resource.Offset);
                int length = (int)blobReader.ReadUInt32();

                UnmanagedMemoryStream ms;
                unsafe
                {
                    ms = new UnmanagedMemoryStream(blobReader.CurrentPointer, length);
                }

                return _dependencyAnalyzer.GetDependencies(factory, ms);
            }
            else
            {
                return null;
            }
        }

        public override void BuildTokens(TokenMap.Builder builder)
        {
            if (_skipWritingResource)
                return;

            base.BuildTokens(builder);
        }

        public override void Write(ModuleWritingContext writeContext)
        {
            if (_skipWritingResource)
                return;

            base.Write(writeContext);
        }

        protected override EntityHandle WriteInternal(ModuleWritingContext writeContext)
        {
            MetadataReader reader = _module.MetadataReader;
            ManifestResource resource = reader.GetManifestResource(Handle);

            var builder = writeContext.MetadataBuilder;

            PEMemoryBlock resourceDirectory = _module.PEReader.GetSectionData(_module.PEReader.PEHeaders.CorHeader.ResourcesDirectory.RelativeVirtualAddress);
            BlobReader resourceReader = resourceDirectory.GetReader((int)resource.Offset, resourceDirectory.Length - (int)resource.Offset);
            int length = (int)resourceReader.ReadUInt32();

            BlobBuilder resourceBuilder = writeContext.ManagedResourceBuilder;
            uint offset = (uint)resourceBuilder.Count;
            resourceBuilder.WriteUInt32((uint)length);
            resourceBuilder.WriteBytes(resourceReader.ReadBytes(length));
            return writeContext.MetadataBuilder.AddManifestResource(
                resource.Attributes,
                builder.GetOrAddString(reader.GetString(resource.Name)),
                writeContext.TokenMap.MapToken(resource.Implementation),
                offset);
        }

        public override string ToString()
        {
            MetadataReader reader = _module.MetadataReader;
            return reader.GetString(reader.GetManifestResource(Handle).Name);
        }

        public interface IManifestResourceDependencyAnalyzer
        {
            DependencyList GetDependencies(NodeFactory factory, Stream content);
        }
    }
}
