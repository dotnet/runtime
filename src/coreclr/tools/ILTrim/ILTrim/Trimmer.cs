// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

using Internal.TypeSystem.Ecma;

using ILCompiler.DependencyAnalysisFramework;

using ILTrim.DependencyAnalysis;

namespace ILTrim
{
    public static class Trimmer
    {
        public static void TrimAssembly(PEReader pe, Stream output)
        {
            var mdReader = pe.GetMetadataReader();

            var context = new ILTrimTypeSystemContext();

            // TODO: we should set context.ReferenceFilePaths to a map of assembly simple name to file path
            //       and call ResolveAssembly instead to get an interned EcmaModule.
            //       Direct call to EcmaModule.Create creates an assembly out of thin air without registering
            //       it anywhere and once we deal with multiple assemblies that refer to each other, that's a problem.

            EcmaModule module = EcmaModule.Create(context, pe, containingAssembly: null);

            // TODO: need to context.SetSystemModule to get a usable type system context

            var factory = new NodeFactory();

            var analyzer = new DependencyAnalyzer<NoLogStrategy<NodeFactory>, NodeFactory>(factory, resultSorter: null);
            MethodDefinitionHandle entrypointToken = (MethodDefinitionHandle)MetadataTokens.Handle(pe.PEHeaders.CorHeader.EntryPointTokenOrRelativeVirtualAddress);
            analyzer.AddRoot(factory.MethodDefinition(module, entrypointToken), "Entrypoint");

            analyzer.ComputeMarkedNodes();

            var writers = ModuleWriter.CreateWriters(factory, analyzer.MarkedNodeList);
            writers[0].Save(output);
        }
    }
}
