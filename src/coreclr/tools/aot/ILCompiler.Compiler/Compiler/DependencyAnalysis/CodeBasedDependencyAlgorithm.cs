// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

using Internal.IL;
using Internal.Text;
using Internal.TypeSystem;

using ILCompiler.DependencyAnalysisFramework;

using DependencyList=ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.DependencyList;
using CombinedDependencyList=System.Collections.Generic.List<ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.CombinedDependencyListEntry>;
using DependencyListEntry=ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.DependencyListEntry;


namespace ILCompiler.DependencyAnalysis
{
    public static class CodeBasedDependencyAlgorithm
    {
        public static void AddDependenciesDueToMethodCodePresence(ref DependencyList dependencies, NodeFactory factory, MethodDesc method, MethodIL methodIL)
        {
            factory.MetadataManager.GetDependenciesDueToMethodCodePresence(ref dependencies, factory, method, methodIL);

            factory.InteropStubManager.AddDependenciesDueToPInvoke(ref dependencies, factory, method);

            if (method.OwningType is MetadataType mdType)
                ModuleUseBasedDependencyAlgorithm.AddDependenciesDueToModuleUse(ref dependencies, factory, mdType.Module);

            if (method.IsIntrinsic)
            {
                if (method.OwningType is MetadataType owningType)
                {
                    string name = method.Name;

                    switch (name)
                    {
                        // The general purpose code in Comparer/EqualityComparer Create method depends on the template
                        // type loader being able to load the necessary types at runtime.
                        case "Create":
                            if (method.IsSharedByGenericInstantiations
                                && owningType.Module == factory.TypeSystemContext.SystemModule
                                && owningType.Namespace == "System.Collections.Generic")
                            {
                                TypeDesc[] templateDependencies = null;

                                if (owningType.Name == "Comparer`1")
                                {
                                    templateDependencies = Internal.IL.Stubs.ComparerIntrinsics.GetPotentialComparersForType(
                                        owningType.Instantiation[0]);
                                }
                                else if (owningType.Name == "EqualityComparer`1")
                                {
                                    templateDependencies = Internal.IL.Stubs.ComparerIntrinsics.GetPotentialEqualityComparersForType(
                                        owningType.Instantiation[0]);
                                }

                                if (templateDependencies != null)
                                {
                                    dependencies = dependencies ?? new DependencyList();
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

        public static void AddConditionalDependenciesDueToMethodCodePresence(ref CombinedDependencyList dependencies, NodeFactory factory, MethodDesc method)
        {
            factory.MetadataManager.GetConditionalDependenciesDueToMethodCodePresence(ref dependencies, factory, method);
        }
    }
}
