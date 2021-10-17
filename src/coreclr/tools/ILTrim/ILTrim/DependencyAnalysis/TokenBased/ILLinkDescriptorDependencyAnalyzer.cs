// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Xml;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using static ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILTrim.DependencyAnalysis.NodeFactory>;

namespace ILTrim.DependencyAnalysis
{
    /// <summary>
    /// Represents an IL Link descriptor based root.
    /// </summary>
    internal class ILLinkDescriptorDependencyAnalyzer : ManifestResourceNode.IManifestResourceDependencyAnalyzer
    {
        private readonly EcmaModule _module;
        private readonly IReadOnlyDictionary<string, bool> _featureSwitches;

        public ILLinkDescriptorDependencyAnalyzer(EcmaModule module, IReadOnlyDictionary<string, bool> featureSwitches)
        {
            _module = module;
            _featureSwitches = featureSwitches;
        }

        public DependencyList GetDependencies(NodeFactory factory, Stream content)
        {
            return DescriptorReader.GetDependencies(_module.Context, XmlReader.Create(content), _module, _featureSwitches, factory);
        }

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

                        ProcessTypeAttributes(!_reader.IsEmptyElement, type);

                        if (!_reader.IsEmptyElement)
                        {
                            _reader.Read();

                            while (_reader.IsStartElement())
                            {
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
                    }
                }

                _reader.Skip();
            }

            private void ProcessTypeAttributes(bool hasContent, TypeDesc type)
            {

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
