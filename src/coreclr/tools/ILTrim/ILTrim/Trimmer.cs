// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using ILCompiler.DependencyAnalysisFramework;

using ILTrim.DependencyAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace ILTrim
{
    public static class Trimmer
    {
        public static void TrimAssembly(
            string inputPath,
            IReadOnlyList<string> additionalTrimPaths,
            string outputDir,
            IReadOnlyList<string> referencePaths,
            TrimmerSettings settings = null)
        {
            var context = new ILTrimTypeSystemContext();
            settings = settings ?? new TrimmerSettings();

            Dictionary<string, string> references = new();
            foreach (var path in additionalTrimPaths.Concat(referencePaths ?? Enumerable.Empty<string>()))
            {
                var simpleName = Path.GetFileNameWithoutExtension(path);
                references.Add(simpleName, path);
            }
            context.ReferenceFilePaths = references;

            // Get an interned EcmaModule. Direct call to EcmaModule.Create creates an assembly out of thin air without
            // registering it anywhere and once we deal with multiple assemblies that refer to each other, that's a problem.
            EcmaModule module = context.GetModuleFromPath(inputPath);

            EcmaModule corelib = context.GetModuleForSimpleName("System.Private.CoreLib");
            context.SetSystemModule(corelib);

            var trimmedAssemblies = new List<string>(additionalTrimPaths.Select(p => Path.GetFileNameWithoutExtension(p)));
            trimmedAssemblies.Add(Path.GetFileNameWithoutExtension(inputPath));
            var factory = new NodeFactory(trimmedAssemblies, settings);

            DependencyAnalyzerBase<NodeFactory> analyzer = settings.LogStrategy switch
            {
                LogStrategy.None => new DependencyAnalyzer<NoLogStrategy<NodeFactory>, NodeFactory>(factory, resultSorter: null),
                LogStrategy.FirstMark => new DependencyAnalyzer<FirstMarkLogStrategy<NodeFactory>, NodeFactory>(factory, resultSorter: null),
                LogStrategy.FullGraph => new DependencyAnalyzer<FullGraphLogStrategy<NodeFactory>, NodeFactory>(factory, resultSorter: null),
                LogStrategy.EventSource => new DependencyAnalyzer<EventSourceLogStrategy<NodeFactory>, NodeFactory>(factory, resultSorter: null),
                _ => throw new ArgumentException("Invalid log strategy")
            };

            analyzer.ComputeDependencyRoutine += ComputeDependencyNodeDependencies;

            if (!settings.LibraryMode)
            {
                MethodDefinitionHandle entrypointToken = (MethodDefinitionHandle)MetadataTokens.Handle(module.PEReader.PEHeaders.CorHeader.EntryPointTokenOrRelativeVirtualAddress);

                analyzer.AddRoot(factory.MethodDefinition(module, entrypointToken), "Entrypoint");

            }
            else
            {
                int rootNumber = 1;
                List<string> methodNames = new List<string>();
                foreach (var methodHandle in module.MetadataReader.MethodDefinitions)
                {
                    var method = module.MetadataReader.GetMethodDefinition(methodHandle);
                    var type = module.MetadataReader.GetTypeDefinition(method.GetDeclaringType());
                    if (!type.IsNested && IsPublic(type.Attributes) && method.Attributes.IsPublic())
                    {
                        analyzer.AddRoot(factory.MethodDefinition(module, methodHandle), $"LibraryMode_{rootNumber++}");
                    }
                }
            }

            analyzer.AddRoot(factory.VirtualMethodUse(
                (EcmaMethod)context.GetWellKnownType(WellKnownType.Object).GetMethod("Finalize", null)),
                "Finalizer");

            analyzer.ComputeMarkedNodes();

            var writers = ModuleWriter.CreateWriters(factory, analyzer.MarkedNodeList);
            if (!File.Exists(outputDir))
                Directory.CreateDirectory(outputDir);
            RunForEach(writers, writer =>
            {
                var ext = writer.AssemblyName == "test" ? ".exe" : ".dll";
                string outputPath = Path.Combine(outputDir, writer.AssemblyName + ext);
                using var outputStream = File.OpenWrite(outputPath);
                writer.Save(outputStream);
            });

            if (settings.LogFile != null) {
                using var logStream = File.OpenWrite(settings.LogFile);
                DgmlWriter.WriteDependencyGraphToStream<NodeFactory>(logStream, analyzer, factory);
            }

            void ComputeDependencyNodeDependencies(List<DependencyNodeCore<NodeFactory>> nodesWithPendingDependencyCalculation) =>
                RunForEach(
                    nodesWithPendingDependencyCalculation.Cast<INodeWithDeferredDependencies>(),
                    node => node.ComputeDependencies(factory));

            void RunForEach<T>(IEnumerable<T> inputs, Action<T> action)
            {
#if !SINGLE_THREADED
                if (settings.MaxDegreeOfParallelism == 1)
#endif
                {
                    foreach (var input in inputs)
                        action(input);
                }
#if !SINGLE_THREADED
                else
                {
                    Parallel.ForEach(
                        inputs,
                        new() { MaxDegreeOfParallelism = settings.EffectiveDegreeOfParallelism },
                        action);
                }
#endif
            }
        }

        private static bool IsPublic(TypeAttributes typeAttributes) =>
            (typeAttributes & TypeAttributes.VisibilityMask) == TypeAttributes.Public;
    }
}
