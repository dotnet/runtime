// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection.PortableExecutable;

using ILCompiler.DependencyAnalysis.ReadyToRun;
using ILCompiler.DependencyAnalysisFramework;
using ILCompiler.PEWriter;
using ObjectData = ILCompiler.DependencyAnalysis.ObjectNode.ObjectData;

using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Object writer using R2RPEReader to directly emit Windows Portable Executable binaries
    /// </summary>
    internal class ReadyToRunObjectWriter
    {
        // Nodefactory for which ObjectWriter is instantiated for.
        private readonly ReadyToRunCodegenNodeFactory _nodeFactory;
        private readonly string _objectFilePath;
        private readonly IEnumerable<DependencyNode> _nodes;
        private readonly PEReader _inputPeReader;

#if DEBUG
        private struct NodeInfo
        {
            public readonly ISymbolNode Node;
            public readonly int NodeIndex;
            public readonly int SymbolIndex;

            public NodeInfo(ISymbolNode node, int nodeIndex, int symbolIndex)
            {
                Node = node;
                NodeIndex = nodeIndex;
                SymbolIndex = symbolIndex;
            }
        }

        Dictionary<string, NodeInfo> _previouslyWrittenNodeNames = new Dictionary<string, NodeInfo>();
#endif

        public ReadyToRunObjectWriter(PEReader inputPeReader, string objectFilePath, IEnumerable<DependencyNode> nodes, ReadyToRunCodegenNodeFactory factory)
        {
            _objectFilePath = objectFilePath;
            _nodes = nodes;
            _nodeFactory = factory;
            _inputPeReader = inputPeReader;
        }

        public void EmitPortableExecutable()
        {
            bool succeeded = false;

            FileStream mapFileStream = null;
            TextWriter mapFile = null;

            try
            {
                string mapFileName = Path.ChangeExtension(_objectFilePath, ".map");
                mapFileStream = new FileStream(mapFileName, FileMode.Create, FileAccess.Write);
                mapFile = new StreamWriter(mapFileStream);

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                mapFile.WriteLine($@"R2R object emission started: {DateTime.Now}");

                R2RPEBuilder r2rPeBuilder = new R2RPEBuilder(
                    _nodeFactory.Target,
                    _inputPeReader,
                    GetRuntimeFunctionsTable);

                int nodeIndex = -1;
                foreach (var depNode in _nodes)
                {
                    ++nodeIndex;
                    ObjectNode node = depNode as ObjectNode;

                    if (node == null)
                    {
                        continue;
                    }

                    if (node.ShouldSkipEmittingObjectNode(_nodeFactory))
                        continue;

                    ObjectData nodeContents = node.GetData(_nodeFactory);

                    string name = null;

                    if (mapFile != null)
                    {
                        name = depNode.GetType().ToString();
                        int firstGeneric = name.IndexOf('[');
                        if (firstGeneric < 0)
                        {
                            firstGeneric = name.Length;
                        }
                        int lastDot = name.LastIndexOf('.', firstGeneric - 1, firstGeneric);
                        if (lastDot > 0)
                        {
                            name = name.Substring(lastDot + 1);
                        }
                    }

                    EmitObjectData(r2rPeBuilder, nodeContents, nodeIndex, name, node.Section, mapFile);
                }

                r2rPeBuilder.SetCorHeader(_nodeFactory.CopiedCorHeaderNode, _nodeFactory.CopiedCorHeaderNode.Size);

                if (_nodeFactory.Win32ResourcesNode != null)
                {
                    Debug.Assert(_nodeFactory.Win32ResourcesNode.Size != 0);
                    r2rPeBuilder.SetWin32Resources(_nodeFactory.Win32ResourcesNode, _nodeFactory.Win32ResourcesNode.Size);
                }

                using (var peStream = File.Create(_objectFilePath))
                {
                    r2rPeBuilder.Write(peStream);
                }

                mapFile.WriteLine($@"R2R object emission finished: {DateTime.Now}, {stopwatch.ElapsedMilliseconds} msecs");
                mapFile.Flush();
                mapFileStream.Flush();

                succeeded = true;
            }
            finally
            {
                if (mapFile != null)
                {
                    mapFile.Dispose();
                }
                if (mapFileStream != null)
                {
                    mapFileStream.Dispose();
                }
                if (!succeeded)
                {
                    // If there was an exception while generating the OBJ file, make sure we don't leave the unfinished
                    // object file around.
                    try
                    {
                        File.Delete(_objectFilePath);
                    }
                    catch
                    {
                    }
                }
            }
        }

        /// <summary>
        /// Update the PE header directories by setting up the exception directory to point to the runtime functions table.
        /// This is needed for RtlLookupFunctionEntry / RtlLookupFunctionTable to work.
        /// </summary>
        /// <param name="builder">PE header directory builder can be used to override RVA's / sizes of any of the directories</param>
        private RuntimeFunctionsTableNode GetRuntimeFunctionsTable() => _nodeFactory.RuntimeFunctionsTable;

        /// <summary>
        /// Emit a single ObjectData into the proper section of the output R2R PE executable.
        /// </summary>
        /// <param name="r2rPeBuilder">R2R PE builder to output object data to</param>
        /// <param name="data">ObjectData blob to emit</param>
        /// <param name="nodeIndex">Logical index of the emitted node for diagnostic purposes</param>
        /// <param name="name">Textual representation of the ObjecData blob in the map file</param>
        /// <param name="section">Section to emit the blob into</param>
        /// <param name="mapFile">Map file output stream</param>
        private void EmitObjectData(R2RPEBuilder r2rPeBuilder, ObjectData data, int nodeIndex, string name, ObjectNodeSection section, TextWriter mapFile)
        {
#if DEBUG
            for (int symbolIndex = 0; symbolIndex < data.DefinedSymbols.Length; symbolIndex++)
            {
                ISymbolNode definedSymbol = data.DefinedSymbols[symbolIndex];
                NodeInfo alreadyWrittenSymbol;
                string symbolName = definedSymbol.GetMangledName(_nodeFactory.NameMangler);
                if (_previouslyWrittenNodeNames.TryGetValue(symbolName, out alreadyWrittenSymbol))
                {
                    Console.WriteLine($@"Duplicate symbol - 1st occurrence: [{alreadyWrittenSymbol.NodeIndex}:{alreadyWrittenSymbol.SymbolIndex}], {alreadyWrittenSymbol.Node.GetMangledName(_nodeFactory.NameMangler)}");
                    Console.WriteLine($@"Duplicate symbol - 2nd occurrence: [{nodeIndex}:{symbolIndex}], {definedSymbol.GetMangledName(_nodeFactory.NameMangler)}");
                    Debug.Fail("Duplicate node name emitted to file",
                    $"Symbol {definedSymbol.GetMangledName(_nodeFactory.NameMangler)} has already been written to the output object file {_objectFilePath} with symbol {alreadyWrittenSymbol}");
                }
                _previouslyWrittenNodeNames.Add(symbolName, new NodeInfo(definedSymbol, nodeIndex, symbolIndex));
            }
#endif

            r2rPeBuilder.AddObjectData(data, section, name, mapFile);
        }

        public static void EmitObject(PEReader inputPeReader, string objectFilePath, IEnumerable<DependencyNode> nodes, ReadyToRunCodegenNodeFactory factory)
        {
            Console.WriteLine($@"Emitting R2R PE file: {objectFilePath}");
            ReadyToRunObjectWriter objectWriter = new ReadyToRunObjectWriter(inputPeReader, objectFilePath, nodes, factory);
            objectWriter.EmitPortableExecutable();
        }
    }
}
