// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

using ILCompiler.DependencyAnalysisFramework;

namespace ILCompiler.DependencyAnalysis
{
    internal static class ModuleUseBasedDependencyAlgorithm
    {
        internal static void AddDependenciesDueToModuleUse(IDependencySink<NodeFactory> dependencyList, NodeFactory factory, ModuleDesc module)
        {
            if (module.GetGlobalModuleType().GetStaticConstructor() is MethodDesc moduleCctor)
            {
                dependencyList.Add(factory.MethodEntrypoint(moduleCctor), "Module with a static constructor");
            }
            factory.MetadataManager.GetDependenciesDueToModuleUse(dependencyList, factory, module);
        }
    }
}
