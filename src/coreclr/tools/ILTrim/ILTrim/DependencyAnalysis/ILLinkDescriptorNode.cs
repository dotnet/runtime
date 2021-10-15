// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Xml;

using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;

namespace ILTrim.DependencyAnalysis
{
    /// <summary>
    /// Represents an IL Link descriptor based root.
    /// </summary>
    public class ILLinkDescriptorNode : DependencyNodeCore<NodeFactory>
    {
        private readonly EcmaModule _module;

        public ILLinkDescriptorNode(EcmaModule module)
        {
            _module = module;
        }

        protected override string GetName(NodeFactory factory)
        {
            return $"Roots from {_module} ILLink.Descriptors.xml";
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

                    return DescriptorReader.GetDependencies(_module.Context, XmlReader.Create(ms), _module, new Dictionary<string, bool>(), factory);
                }
            }

            return null;
        }

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory) => null;
        public override bool HasConditionalStaticDependencies => false;
        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory) => null;

        private class DescriptorReader : ILCompiler.ProcessLinkerXmlBase
        {
            private readonly NodeFactory _factory;
            private DependencyList _dependencies = new DependencyList();

            public static DependencyList GetDependencies(TypeSystemContext context, XmlReader reader, ModuleDesc owningModule,
                IReadOnlyDictionary<string, bool> featureSwitchValues, NodeFactory factory)
            {
                var rdr = new DescriptorReader(context, reader, owningModule, featureSwitchValues, factory);
                rdr.ProcessXml();
                return rdr._dependencies;
            }

            private DescriptorReader(TypeSystemContext context, XmlReader reader, ModuleDesc owningModule,
                IReadOnlyDictionary<string, bool> featureSwitchValues, NodeFactory factory)
                : base(context, reader, owningModule, featureSwitchValues)
            {
                _factory = factory;
            }

            protected override void ProcessType(ModuleDesc assembly)
            {
                if (ShouldProcessElement())
                {
                    string typeName = _reader.GetAttribute("fullname");

                    List<TypeDesc> types = new List<TypeDesc>();
                    bool hasContent = false;
                    if (typeName.Contains('*'))
                    {
                        typeName = typeName.Replace("*", ".*");
                        var regex = new System.Text.RegularExpressions.Regex(typeName);

                        foreach (var type in assembly.GetAllTypes())
                        {
                            if (regex.IsMatch(type.GetFullName()))
                                types.Add(type);
                        }

                        if (!_reader.IsEmptyElement)
                            _reader.Skip();
                    }
                    else
                    {
                        TypeDesc type = CustomAttributeTypeNameParser.GetTypeByCustomAttributeTypeName(assembly, typeName, throwIfNotFound: false);
                        if (type == null)
                        {
                            _reader.Skip();
                            return;
                        }

                        if (!_reader.IsEmptyElement)
                        {
                            _reader.Read();

                            while (_reader.IsStartElement())
                            {
                                hasContent = true;
                                if (_reader.Name == "method")
                                {
                                    ProcessMethod(type);
                                }
                                else if (_reader.Name == "field")
                                {
                                    ProcessField(type);
                                }

                                _reader.Skip();
                            }
                        }

                        types.Add(type);
                    }

                    foreach (var type in types)
                    {
                        var ecmaType = (EcmaType)type;
                        if (_factory.IsModuleTrimmed(ecmaType.EcmaModule))
                        {
                            _dependencies.Add(_factory.TypeDefinition(ecmaType.EcmaModule, ecmaType.Handle),
                                "Type rooted by descriptor");
                            _dependencies.Add(_factory.ConstructedType(ecmaType), "Type rooted by descriptor");
                        }

                        string preserve = _reader.GetAttribute("preserve");

                        bool preserveMethods = false;
                        bool preserveFields = false;

                        if ((!hasContent && preserve != "nothing") || (preserve == "all"))
                        {
                            preserveFields = true;
                            preserveMethods = true;
                        }
                        else if (preserve == "fields")
                        {
                            preserveFields = true;
                        }
                        else if (preserve == "methods")
                        {
                            preserveMethods = true;
                        }

                        if (preserveFields)
                        {
                            foreach (FieldDesc field in type.GetFields())
                            {
                                ProcessField(field);
                            }
                        }

                        if (preserveMethods)
                        {
                            foreach (MethodDesc method in type.GetMethods())
                            {
                                ProcessMethod(method);
                            }
                        }
                    }
                }

                _reader.Skip();
            }

            protected override void ProcessField(FieldDesc field)
            {
                var ecmaField = (EcmaField)field;
                if (_factory.IsModuleTrimmed(ecmaField.Module))
                    _dependencies.Add(_factory.FieldDefinition(ecmaField.Module, ecmaField.Handle),
                        "Field rooted by descriptor");
            }

            protected override void ProcessMethod(MethodDesc method)
            {
                var ecmaMethod = (EcmaMethod)method;
                if (_factory.IsModuleTrimmed(ecmaMethod.Module))
                    _dependencies.Add(_factory.MethodDefinition(ecmaMethod.Module, ecmaMethod.Handle),
                        "Method rooted by descriptor");
            }
        }
    }
}
