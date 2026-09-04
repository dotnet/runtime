// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using Debug = System.Diagnostics.Debug;
using EcmaAssembly = Internal.TypeSystem.Ecma.EcmaAssembly;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a reflectable module.
    /// </summary>
    /// <remarks>
    /// Only expected to be used during ILScanning when scanning for reflection.
    /// </remarks>
    internal sealed class ModuleMetadataNode : DependencyNodeCore<NodeFactory>
    {
        private readonly ModuleDesc _module;

        public ModuleMetadataNode(ModuleDesc module)
        {
            Debug.Assert(module is IAssemblyDesc, "Multi-module assemblies?");
            _module = module;
        }

        public ModuleDesc Module => _module;

        public override void AddStaticDependencies(DependencySink<NodeFactory> sink, NodeFactory factory)
        {
            DependencySink<NodeFactory> dependencies = sink;

            // Global module type always generates metadata because it's really convenient to
            // have something in an assembly that always generates metadata.
            dependencies.Add(factory.TypeMetadata(_module.GetGlobalModuleType()), "Global module type");
            if (_module is EcmaModule ecmaModule
                && ecmaModule.EntryPoint is MethodDesc entrypoint
                && !factory.MetadataManager.IsReflectionBlocked(entrypoint))
            {
                dependencies.Add(factory.ReflectedMethod(entrypoint), "Reflectable entrypoint");
            }

            EcmaAssembly ecmaAssembly = (EcmaAssembly)_module;

            foreach (EcmaModule satelliteModule in ((UsageBasedMetadataManager)factory.MetadataManager).GetSatelliteAssemblies(ecmaAssembly))
            {
                dependencies.Add(factory.ModuleMetadata(satelliteModule), "Satellite assembly");
            }
        }

        public override void AddConditionalDependencies(DependencySink<NodeFactory> sink, NodeFactory factory)
        {
            DependencySink<NodeFactory> dependencies = sink;
            CustomAttributeBasedDependencyAlgorithm.AddDependenciesDueToCustomAttributes(dependencies, factory, (EcmaAssembly)_module);
        }

        protected override string GetName(NodeFactory factory)
        {
            return "Reflectable module: " + ((IAssemblyDesc)_module).GetName().Name;
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => true;
        public override bool StaticDependenciesAreComputed => true;
        public override void SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, DependencySink<NodeFactory> sink, NodeFactory factory) { }
    }
}
