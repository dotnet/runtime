// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.IL;
using Internal.TypeSystem;

using DependencyList = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.DependencyList;
using CombinedDependencyList = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.CombinedDependencyList;
using ILCompiler.DependencyAnalysisFramework;


namespace ILCompiler.DependencyAnalysis
{
    public static class CodeBasedDependencyAlgorithm
    {
        public static void AddDependenciesDueToMethodCodePresence(IDependencySink<NodeFactory> dependencies, NodeFactory factory, MethodDesc method, MethodIL methodIL)
        {
            factory.MetadataManager.GetDependenciesDueToMethodCodePresence(dependencies, factory, method, methodIL);

            factory.InteropStubManager.AddDependenciesDueToMethodCodePresence(dependencies, factory, method);

            if (method.OwningType is MetadataType mdType)
                ModuleUseBasedDependencyAlgorithm.AddDependenciesDueToModuleUse(dependencies, factory, mdType.Module);

            if (method.IsIntrinsic)
            {
                if (method.OwningType is MetadataType owningType)
                {
                    string name = method.GetName();

                    switch (name)
                    {
                        // The general purpose code in Comparer/EqualityComparer Create method depends on the template
                        // type loader being able to load the necessary types at runtime.
                        case "Create":
                            if (method.IsSharedByGenericInstantiations
                                && owningType.Module == factory.TypeSystemContext.SystemModule
                                && owningType.Namespace == "System.Collections.Generic"u8)
                            {
                                TypeDesc[] templateDependencies = null;

                                if (owningType.Name == "Comparer`1"u8)
                                {
                                    templateDependencies = Internal.IL.Stubs.ComparerIntrinsics.GetPotentialComparersForType(
                                        owningType.Instantiation[0]);
                                }
                                else if (owningType.Name == "EqualityComparer`1"u8)
                                {
                                    templateDependencies = Internal.IL.Stubs.ComparerIntrinsics.GetPotentialEqualityComparersForType(
                                        owningType.Instantiation[0]);
                                }

                                if (templateDependencies != null)
                                {
                                    foreach (TypeDesc templateType in templateDependencies)
                                    {
                                        dependencies.Add(factory.NativeLayout.TemplateTypeLayout(templateType), "Generic comparer");
                                    }
                                }
                            }
                            break;
                    }
                }
            }
        }

        public static bool HasConditionalDependenciesDueToMethodCodePresence(MethodDesc method)
        {
            // NICE: would be nice if the metadata managed could decide this but we don't have a way to get at it
            return method.HasInstantiation || method.OwningType.HasInstantiation;
        }

        public static void AddConditionalDependenciesDueToMethodCodePresence(IConditionalDependencySink<NodeFactory> dependencies, NodeFactory factory, MethodDesc method)
        {
            factory.MetadataManager.GetConditionalDependenciesDueToMethodCodePresence(dependencies, factory, method);
        }
    }
}
