// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

using DependencyList = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.DependencyList;

namespace ILCompiler.DependencyAnalysis
{
    internal class ModuleUseBasedDependencyAlgorithm
    {
        internal static void AddDependenciesDueToModuleUse(ref DependencyList dependencyList, NodeFactory factory, ModuleDesc module)
        {
            dependencyList ??= new DependencyList();
            if (module.GetGlobalModuleType().GetStaticConstructor() is MethodDesc moduleCctor)
            {
                dependencyList.Add(factory.MethodEntrypoint(moduleCctor), "Module with a static constructor");
            }
            factory.MetadataManager.GetDependenciesDueToModuleUse(ref dependencyList, factory, module);
        }
    }
}
