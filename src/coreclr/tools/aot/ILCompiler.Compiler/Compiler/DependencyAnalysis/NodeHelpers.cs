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
    internal class NodeHelpers
    {
        internal static void ModuleConstructorCall(ref DependencyList dependencyList, NodeFactory factory, TypeDesc type, string reason)
        {
            dependencyList ??= new DependencyList();
            if (type is not MetadataType mdType)
                return;
            var module = mdType.Module;
            if (module.GetGlobalModuleType().GetStaticConstructor() is MethodDesc moduleCctor)
            {
                dependencyList.Add(factory.MethodEntrypoint(moduleCctor), reason);
            }
            if(module is EcmaModule ecmaModule) {
                PEMemoryBlock resourceDirectory = ecmaModule.PEReader.GetSectionData(ecmaModule.PEReader.PEHeaders.CorHeader.ResourcesDirectory.RelativeVirtualAddress);

                foreach (var resourceHandle in ecmaModule.MetadataReader.ManifestResources)
                {
                    ManifestResource resource = ecmaModule.MetadataReader.GetManifestResource(resourceHandle);

                    // Don't try to process linked resources or resources in other assemblies
                    if (!resource.Implementation.IsNil)
                    {
                        continue;
                    }

                    string resourceName = ecmaModule.MetadataReader.GetString(resource.Name);
                    if (resourceName == "ILLink.Descriptors.xml")
                    {
                        dependencyList.Add(factory.AssemblyEmbeddedInfo(ecmaModule), "Embedded descriptor file");
                    }
                }
            }
        }
    }
}
