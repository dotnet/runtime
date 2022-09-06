// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem.Ecma;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Node that embedded xml files to find and root dependencies.
    /// </summary>
    public class EmbeddedTrimmingDescriptorNode : DependencyNodeCore<NodeFactory>
    {
        private readonly EcmaModule _module;

        public EmbeddedTrimmingDescriptorNode(EcmaModule module)
        {
            _module = module;
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            PEMemoryBlock resourceDirectory = _module.PEReader.GetSectionData(_module.PEReader.PEHeaders.CorHeader.ResourcesDirectory.RelativeVirtualAddress);

            foreach (var resourceHandle in _module.MetadataReader.ManifestResources)
            {
                ManifestResource resource = _module.MetadataReader.GetManifestResource(resourceHandle);

                // Don't try to process linked resources or resources in other assemblies
                if (!resource.Implementation.IsNil)
                {
                    continue;
                }

                string resourceName = _module.MetadataReader.GetString(resource.Name);
                if (resourceName == "ILLink.Descriptors.xml")
                {
                    BlobReader reader = resourceDirectory.GetReader((int)resource.Offset, resourceDirectory.Length - (int)resource.Offset);
                    int length = (int)reader.ReadUInt32();

                    UnmanagedMemoryStream ms;
                    unsafe
                    {
                        ms = new UnmanagedMemoryStream(reader.CurrentPointer, length);
                    }

                    var metadataManager = (UsageBasedMetadataManager)factory.MetadataManager;
                    return DescriptorMarker.GetDependencies(factory, ms, resource, _module, "resource " + resourceName + " in " + _module.ToString(), metadataManager.FeatureSwitches);
                }
            }
            return Array.Empty<DependencyListEntry>();
        }

        protected override string GetName(NodeFactory factory)
        {
            return $"Embedded descriptor from {_module}";
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => null;
    }
}
