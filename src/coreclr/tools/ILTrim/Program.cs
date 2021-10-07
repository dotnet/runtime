// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

using ILCompiler.DependencyAnalysisFramework;
using ILTrim.DependencyAnalysis;

namespace ILTrim
{
    class Program
    {
        static void Main(string[] args)
        {
            using var fs = File.OpenRead(@"C:\git\runtime2\artifacts\bin\repro\x64\Debug\repro.dll");
            using var pe = new PEReader(fs);
            var mdReader = pe.GetMetadataReader();

            var module = new EcmaModule(pe, mdReader);

            var factory = new NodeFactory();

            var analyzer = new DependencyAnalyzer<NoLogStrategy<NodeFactory>, NodeFactory>(factory, resultSorter: null);
            MethodDefinitionHandle entrypointToken = (MethodDefinitionHandle)MetadataTokens.Handle(pe.PEHeaders.CorHeader.EntryPointTokenOrRelativeVirtualAddress);
            analyzer.AddRoot(factory.MethodDefinition(module, entrypointToken), "Entrypoint");

            analyzer.ComputeMarkedNodes();

            var writers = ModuleWriter.CreateWriters(factory, analyzer.MarkedNodeList);

            writers[0].Save("c:\\temp\\out.exe");
        }
    }
}
