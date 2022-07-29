// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ILCompiler.DependencyAnalysis;
using Internal.TypeSystem;

using DependencyList = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.DependencyList;
using CombinedDependencyList = System.Collections.Generic.List<ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.CombinedDependencyListEntry>;
using DependencyListEntry = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.DependencyListEntry;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.IO;
using System.Reflection.Metadata;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.Compiler.Compiler.DependencyAnalysis
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
