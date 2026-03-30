// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using static ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents an IL Link descriptor based root.
    /// </summary>
    internal class ILLinkDescriptorDependencyAnalyzer : ManifestResourceNode.IManifestResourceDependencyAnalyzer
    {
        private readonly EcmaModule _module;

        public ILLinkDescriptorDependencyAnalyzer(EcmaModule module)
        {
            _module = module;
        }

        public DependencyList GetDependencies(NodeFactory factory, Stream content)
        {
            return DescriptorReader.GetDependencies(
                _module.Context,
                content,
                _module,
                factory.Settings.FeatureSwitches,
                factory);
        }

        private class DescriptorReader : ProcessLinkerXmlBase
        {
            private readonly NodeFactory _factory;
            private DependencyList _dependencies = new DependencyList();

            public static DependencyList GetDependencies(TypeSystemContext context, Stream content, EcmaModule owningModule,
                IReadOnlyDictionary<string, bool> featureSwitchValues, NodeFactory factory)
            {
                var rdr = new DescriptorReader(context, content, owningModule, featureSwitchValues, factory);
                rdr.ProcessXml(false);
                return rdr._dependencies;
            }

            private DescriptorReader(TypeSystemContext context, Stream content, EcmaModule owningModule,
                IReadOnlyDictionary<string, bool> featureSwitchValues, NodeFactory factory)
                : base(factory.Logger, context, content, default(ManifestResource), owningModule, "descriptor", featureSwitchValues)
            {
                _factory = factory;
            }

            protected override void ProcessAssembly(ModuleDesc assembly, System.Xml.XPath.XPathNavigator nav, bool warnOnUnresolvedTypes)
            {
                ProcessTypes(assembly, nav, warnOnUnresolvedTypes);
            }

            protected override void ProcessType(TypeDesc type, System.Xml.XPath.XPathNavigator nav)
            {
                var ecmaType = (EcmaType)type;
                if (_factory.IsModuleTrimmed(ecmaType.Module))
                {
                    _dependencies.Add(_factory.TypeDefinition(ecmaType.Module, ecmaType.Handle),
                        "Type rooted by descriptor");
                    _dependencies.Add(_factory.ConstructedType(ecmaType), "Type rooted by descriptor");

                    ProcessTypeChildren(ecmaType, nav);
                }
            }

            protected override void ProcessField(TypeDesc type, FieldDesc field, System.Xml.XPath.XPathNavigator nav)
            {
                var ecmaField = (EcmaField)field;
                if (_factory.IsModuleTrimmed(ecmaField.Module))
                    _dependencies.Add(_factory.FieldDefinition(ecmaField.Module, ecmaField.Handle),
                        "Field rooted by descriptor");
            }

            protected override void ProcessMethod(TypeDesc type, MethodDesc method, System.Xml.XPath.XPathNavigator nav, object customData)
            {
                var ecmaMethod = (EcmaMethod)method;
                if (_factory.IsModuleTrimmed(ecmaMethod.Module))
                    _dependencies.Add(_factory.MethodDefinition(ecmaMethod.Module, ecmaMethod.Handle),
                        "Method rooted by descriptor");
            }

            protected override MethodDesc? GetMethod(TypeDesc type, string signature)
            {
                foreach (MethodDesc meth in type.GetAllMethods())
                {
                    if (signature == GetMethodSignature(meth, false))
                        return meth;
                }
                return null;
            }
        }
    }
}
