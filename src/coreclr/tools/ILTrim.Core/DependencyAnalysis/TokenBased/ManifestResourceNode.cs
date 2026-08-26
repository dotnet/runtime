// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

using Internal.TypeSystem.Ecma;

using CodeOptimizations = Mono.Linker.CodeOptimizations;

namespace ILCompiler.DependencyAnalysis
{
    public class ManifestResourceNode : TokenBasedNode
    {
        private ManifestResourceHandle Handle => (ManifestResourceHandle)_handle;

        private bool? _skipWritingResource;

        public ManifestResourceNode(EcmaModule module, ManifestResourceHandle handle)
            : base(module, handle) { }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            ManifestResource resource = _module.MetadataReader.GetManifestResource(Handle);

            _skipWritingResource = false;

            DependencyList dependencies = null;

            if (resource.Implementation.IsNil)
            {
                string resourceName = _module.MetadataReader.GetString(resource.Name);
                if (resourceName == "ILLink.Descriptors.xml")
                {
                    string assemblyName = _module.Assembly.GetName().Name;
                    _skipWritingResource = factory.Settings.Optimizations.IsEnabled(CodeOptimizations.RemoveDescriptors, assemblyName);

                    if (!factory.Settings.IgnoreDescriptors)
                    {
                        PEMemoryBlock resourceDirectory = _module.PEReader.GetSectionData(_module.PEReader.PEHeaders.CorHeader.ResourcesDirectory.RelativeVirtualAddress);
                        BlobReader reader = resourceDirectory.GetReader((int)resource.Offset, resourceDirectory.Length - (int)resource.Offset);
                        int length = (int)reader.ReadUInt32();

                        UnmanagedMemoryStream ms;
                        unsafe
                        {
                            ms = new UnmanagedMemoryStream(reader.CurrentPointer, length);
                        }

                        dependencies = DescriptorMarker.GetDependencies(factory.Logger, factory, ms, resource, _module, "resource " + resourceName + " in " + _module.ToString(), factory.Settings.FeatureSettings);
                    }
                }
            }
            else
            {
                dependencies = new();
                switch (resource.Implementation.Kind)
                {
                    case HandleKind.AssemblyReference:
                        var referencedAssembly = (EcmaAssembly)_module.GetObject(resource.Implementation);
                        dependencies.Add(factory.AssemblyReference(_module, referencedAssembly), "Implementation of a manifest resource");
                        break;
                    default:
                        // TODO: Handle AssemblyFile
                        throw new InvalidOperationException(resource.Implementation.Kind.ToString());
                }
            }

            CustomAttributeNode.AddDependenciesDueToCustomAttributes(ref dependencies, factory, _module, resource.GetCustomAttributes());
            return dependencies;
        }

        public override void BuildTokens(TokenMap.Builder builder)
        {
            Debug.Assert(_skipWritingResource.HasValue, "Should have called GetStaticDependencies before writing");
            if (!_skipWritingResource.Value)
                base.BuildTokens(builder);
        }

        public override void Write(ModuleWritingContext writeContext)
        {
            Debug.Assert(_skipWritingResource.HasValue, "Should have called GetStaticDependencies before writing");
            if (!_skipWritingResource.Value)
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
            EntityHandle implementation = resource.Implementation;
            if (implementation.Kind == HandleKind.AssemblyReference)
            {
                var assemblyRefNode = writeContext.Factory.AssemblyReference(
                    _module,
                    (EcmaAssembly)_module.GetObject(implementation));
                Debug.Assert(assemblyRefNode.TargetToken.HasValue);
                implementation = (EntityHandle)assemblyRefNode.TargetToken.Value;
            }
            else
            {
                implementation = writeContext.TokenMap.MapToken(implementation);
            }

            return writeContext.MetadataBuilder.AddManifestResource(
                resource.Attributes,
                builder.GetOrAddString(reader.GetString(resource.Name)),
                implementation,
                offset);
        }

        public override string ToString()
        {
            MetadataReader reader = _module.MetadataReader;
            return reader.GetString(reader.GetManifestResource(Handle).Name);
        }
    }
}
