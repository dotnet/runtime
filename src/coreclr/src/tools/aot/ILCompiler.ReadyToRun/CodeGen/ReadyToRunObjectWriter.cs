// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;

using ILCompiler.DependencyAnalysis.ReadyToRun;
using ILCompiler.DependencyAnalysisFramework;
using ILCompiler.PEWriter;
using ObjectData = ILCompiler.DependencyAnalysis.ObjectNode.ObjectData;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using System.Security.Cryptography;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Object writer using R2RPEReader to directly emit Windows Portable Executable binaries
    /// </summary>
    internal class ReadyToRunObjectWriter
    {
        /// <summary>
        /// Nodefactory for which ObjectWriter is instantiated for. 
        /// </summary>
        private readonly NodeFactory _nodeFactory;

        /// <summary>
        /// Output executable path.
        /// </summary>
        private readonly string _objectFilePath;

        /// <summary>
        /// Set to non-null when rewriting MSIL assemblies during composite R2R build;
        /// we basically publish the input assemblies into the composite build output folder
        /// using the same ReadyToRunObjectWriter as we're using for emitting the "actual"
        /// R2R executable, just in this special mode in which we emit a minimal R2R header
        /// with forwarding information pointing at the composite module with native code.
        /// </summary>
        private readonly EcmaModule _componentModule;

        /// <summary>
        /// Nodes to emit into the output executable as collected by the dependency analysis.
        /// </summary>
        private readonly IEnumerable<DependencyNode> _nodes;

        /// <summary>
        /// Set to non-null when the executable generator should output a map file.
        /// </summary>
        private readonly MapFileBuilder _mapFileBuilder;

        /// <summary>
        /// If non-null, the PE file will be laid out such that it can naturally be mapped with a higher alignment than 4KB
        /// This is used to support loading via large pages on Linux
        /// </summary>
        private readonly int? _customPESectionAlignment;


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

        public ReadyToRunObjectWriter(string objectFilePath, EcmaModule componentModule, IEnumerable<DependencyNode> nodes, NodeFactory factory, bool generateMapFile, int? customPESectionAlignment)
        {
            _objectFilePath = objectFilePath;
            _componentModule = componentModule;
            _nodes = nodes;
            _nodeFactory = factory;
            _customPESectionAlignment = customPESectionAlignment;

            if (generateMapFile)
            {
                _mapFileBuilder = new MapFileBuilder();
            }
        }

        public void EmitPortableExecutable()
        {
            bool succeeded = false;

            try
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                PEHeaderBuilder headerBuilder;
                int timeDateStamp;
                ISymbolNode r2rHeaderExportSymbol;

                if (_nodeFactory.CompilationModuleGroup.IsCompositeBuildMode && _componentModule == null)
                {
                    headerBuilder = PEHeaderProvider.Create(
                        imageCharacteristics: Characteristics.ExecutableImage | Characteristics.Dll,
                        dllCharacteristics: default(DllCharacteristics),
                        Subsystem.Unknown,
                        _nodeFactory.Target);
                    // TODO: generate a non-zero timestamp: https://github.com/dotnet/runtime/issues/32507
                    timeDateStamp = 0;
                    r2rHeaderExportSymbol = _nodeFactory.Header;
                }
                else
                {
                    PEReader inputPeReader = (_componentModule != null ? _componentModule.PEReader : _nodeFactory.CompilationModuleGroup.CompilationModuleSet.First().PEReader);
                    headerBuilder = PEHeaderProvider.Copy(inputPeReader.PEHeaders, _nodeFactory.Target);
                    timeDateStamp = inputPeReader.PEHeaders.CoffHeader.TimeDateStamp;
                    r2rHeaderExportSymbol = null;
                }

                Func<RuntimeFunctionsTableNode> getRuntimeFunctionsTable = null;
                if (_componentModule == null)
                {
                    getRuntimeFunctionsTable = GetRuntimeFunctionsTable;
                }
                R2RPEBuilder r2rPeBuilder = new R2RPEBuilder(
                    _nodeFactory.Target,
                    headerBuilder,
                    r2rHeaderExportSymbol,
                    Path.GetFileName(_objectFilePath),
                    getRuntimeFunctionsTable,
                    _customPESectionAlignment);

                NativeDebugDirectoryEntryNode nativeDebugDirectoryEntryNode = null;
                ISymbolDefinitionNode firstImportThunk = null;
                ISymbolDefinitionNode lastImportThunk = null;
                ObjectNode lastWrittenObjectNode = null;

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

                    if (node is NativeDebugDirectoryEntryNode nddeNode)
                    {
                        // There should be only one NativeDebugDirectoryEntry.
                        Debug.Assert(nativeDebugDirectoryEntryNode == null);
                        nativeDebugDirectoryEntryNode = nddeNode;
                    }

                    if (node is ImportThunk importThunkNode)
                    {
                        Debug.Assert(firstImportThunk == null || lastWrittenObjectNode is ImportThunk,
                            "All the import thunks must be in single contiguous run");

                        if (firstImportThunk == null)
                        {
                            firstImportThunk = importThunkNode;
                        }
                        lastImportThunk = importThunkNode;
                    }

                    string name = null;

                    if (_mapFileBuilder != null)
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

                    EmitObjectData(r2rPeBuilder, nodeContents, nodeIndex, name, node.Section, _mapFileBuilder);
                    lastWrittenObjectNode = node;
                }

                r2rPeBuilder.SetCorHeader(_nodeFactory.CopiedCorHeaderNode, _nodeFactory.CopiedCorHeaderNode.Size);
                r2rPeBuilder.SetDebugDirectory(_nodeFactory.DebugDirectoryNode, _nodeFactory.DebugDirectoryNode.Size);
                if (firstImportThunk != null)
                {
                    r2rPeBuilder.AddSymbolForRange(_nodeFactory.DelayLoadMethodCallThunks, firstImportThunk, lastImportThunk);
                }
                

                if (_nodeFactory.Win32ResourcesNode != null)
                {
                    Debug.Assert(_nodeFactory.Win32ResourcesNode.Size != 0);
                    r2rPeBuilder.SetWin32Resources(_nodeFactory.Win32ResourcesNode, _nodeFactory.Win32ResourcesNode.Size);
                }

                using (var peStream = File.Create(_objectFilePath))
                {
                    r2rPeBuilder.Write(peStream, timeDateStamp);

                    if (_mapFileBuilder != null)
                    {
                        _mapFileBuilder.SetFileSize(peStream.Length);
                    }

                    // Compute MD5 hash of the output image and store that in the native DebugDirectory entry
                    using (var md5Hash = MD5.Create())
                    {
                        peStream.Seek(0, SeekOrigin.Begin);
                        byte[] hash = md5Hash.ComputeHash(peStream);
                        byte[] rsdsEntry = nativeDebugDirectoryEntryNode.GenerateRSDSEntryData(hash);

                        int offsetToUpdate = r2rPeBuilder.GetSymbolFilePosition(nativeDebugDirectoryEntryNode);
                        peStream.Seek(offsetToUpdate, SeekOrigin.Begin);
                        peStream.Write(rsdsEntry);
                    }
                }

                if (_mapFileBuilder != null)
                {
                    r2rPeBuilder.AddSections(_mapFileBuilder);

                    string mapFileName = Path.ChangeExtension(_objectFilePath, ".map");
                    _mapFileBuilder.Save(mapFileName);
                }

                succeeded = true;
            }
            finally
            {
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
        private void EmitObjectData(R2RPEBuilder r2rPeBuilder, ObjectData data, int nodeIndex, string name, ObjectNodeSection section, MapFileBuilder mapFileBuilder)
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

            r2rPeBuilder.AddObjectData(data, section, name, mapFileBuilder);
        }

        public static void EmitObject(string objectFilePath, EcmaModule componentModule, IEnumerable<DependencyNode> nodes, NodeFactory factory, bool generateMapFile, int? customPESectionAlignment)
        {
            Console.WriteLine($@"Emitting R2R PE file: {objectFilePath}");
            ReadyToRunObjectWriter objectWriter = new ReadyToRunObjectWriter(objectFilePath, componentModule, nodes, factory, generateMapFile, customPESectionAlignment);
            objectWriter.EmitPortableExecutable();
        }
    }
}
