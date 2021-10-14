// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

using Internal.TypeSystem.Ecma;

using ILCompiler.DependencyAnalysisFramework;

using ILTrim.DependencyAnalysis;
using System.Linq;

namespace ILTrim
{
    public static class Trimmer
    {
        public static void TrimAssembly(string inputPath, IReadOnlyList<string> additionalTrimPaths, string outputDir, IReadOnlyList<string> referencePaths)
        {
            var context = new ILTrimTypeSystemContext();

            Dictionary<string, string> references = new();
            foreach (var path in additionalTrimPaths.Concat(referencePaths))
            {
                var simpleName = Path.GetFileNameWithoutExtension (path);
                references.Add (simpleName, path);
            }
            context.ReferenceFilePaths = references;

            // Get an interned EcmaModule. Direct call to EcmaModule.Create creates an assembly out of thin air without
            // registering it anywhere and once we deal with multiple assemblies that refer to each other, that's a problem.
            EcmaModule module = context.GetModuleFromPath (inputPath);

            EcmaModule corelib = context.GetModuleForSimpleName("System.Private.CoreLib");
            context.SetSystemModule(corelib);

            var factory = new NodeFactory(additionalTrimPaths.Select(p => Path.GetFileNameWithoutExtension(p)));

            var analyzer = new DependencyAnalyzer<NoLogStrategy<NodeFactory>, NodeFactory>(factory, resultSorter: null);
            MethodDefinitionHandle entrypointToken = (MethodDefinitionHandle)MetadataTokens.Handle(module.PEReader.PEHeaders.CorHeader.EntryPointTokenOrRelativeVirtualAddress);
            analyzer.AddRoot(factory.MethodDefinition(module, entrypointToken), "Entrypoint");

            analyzer.ComputeMarkedNodes();

            var writers = ModuleWriter.CreateWriters(factory, analyzer.MarkedNodeList);
            foreach (var writer in writers)
            {
                var ext = writer.AssemblyName == "test" ? ".exe" : ".dll";
                string outputPath = Path.Combine(outputDir, writer.AssemblyName + ext);
                using var outputStream = File.OpenWrite(outputPath);
                writer.Save(outputStream);
            }
        }
    }
}
