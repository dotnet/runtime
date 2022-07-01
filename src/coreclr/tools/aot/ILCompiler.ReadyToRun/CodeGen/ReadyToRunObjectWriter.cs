// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

using ILCompiler.DependencyAnalysis.ReadyToRun;
using ILCompiler.DependencyAnalysisFramework;
using ILCompiler.Diagnostics;
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
        /// Compilation input files. Input files are emitted as perfmap entries and used
        /// to calculate the output GUID of the ReadyToRun executable for symbol indexation.
        /// </summary>
        private readonly IEnumerable<string> _inputFiles;

        /// <summary>
        /// Nodes to emit into the output executable as collected by the dependency analysis.
        /// </summary>
        private readonly IEnumerable<DependencyNode> _nodes;

        /// <summary>
        /// Set to non-null when the executable generator should output a map or symbol file.
        /// </summary>
        private readonly OutputInfoBuilder _outputInfoBuilder;

        /// <summary>
        /// Set to non-null when the executable generator should output a map file.
        /// </summary>
        private readonly MapFileBuilder _mapFileBuilder;

        /// <summary>
        /// Set to non-null when generating symbol info (PDB / PerfMap).
        /// </summary>
        private readonly SymbolFileBuilder _symbolFileBuilder;

        /// <summary>
        /// Set to non-null when generating callchain profile info.
        /// </summary>
        private readonly ProfileFileBuilder _profileFileBuilder;

        /// <summary>
        /// True when the map file builder should emit a textual map file
        /// </summary>
        private bool _generateMapFile;

        /// <summary>
        /// True when the map file builder should emit a CSV formatted map file
        /// </summary>
        private bool _generateMapCsvFile;

        /// <summary>
        /// True when the map file builder should emit a PDB symbol file (only supported on Windows)
        /// </summary>
        private bool _generatePdbFile;

        /// <summary>
        /// Explicit specification of the output PDB path
        /// </summary>
        private string _pdbPath;

        /// <summary>
        /// True when the map file builder should emit a PerfMap file
        /// </summary>
        private bool _generatePerfMapFile;

        /// <summary>
        /// Explicit specification of the output PerfMap path
        /// </summary>
        private string _perfMapPath;

        /// <summary>
        /// Requested version of the perfmap file format
        /// </summary>
        private int _perfMapFormatVersion;

        /// <summary>
        /// If non-zero, the PE file will be laid out such that it can naturally be mapped with a higher alignment than 4KB.
        /// This is used to support loading via large pages on Linux.
        /// </summary>
        private readonly int _customPESectionAlignment;

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

        public ReadyToRunObjectWriter(
            string objectFilePath,
            EcmaModule componentModule,
            IEnumerable<string> inputFiles,
            IEnumerable<DependencyNode> nodes,
            NodeFactory factory,
            bool generateMapFile,
            bool generateMapCsvFile,
            bool generatePdbFile,
            string pdbPath,
            bool generatePerfMapFile,
            string perfMapPath,
            int perfMapFormatVersion,
            bool generateProfileFile,
            CallChainProfile callChainProfile,
            int customPESectionAlignment)
        {
            _objectFilePath = objectFilePath;
            _componentModule = componentModule;
            _inputFiles = inputFiles;
            _nodes = nodes;
            _nodeFactory = factory;
            _customPESectionAlignment = customPESectionAlignment;
            _generateMapFile = generateMapFile;
            _generateMapCsvFile = generateMapCsvFile;
            _generatePdbFile = generatePdbFile;
            _pdbPath = pdbPath;
            _generatePerfMapFile = generatePerfMapFile;
            _perfMapPath = perfMapPath;
            _perfMapFormatVersion = perfMapFormatVersion;

            bool generateMap = (generateMapFile || generateMapCsvFile);
            bool generateSymbols = (generatePdbFile || generatePerfMapFile);

            if (generateMap || generateSymbols || generateProfileFile)
            {
                _outputInfoBuilder = new OutputInfoBuilder();

                if (generateMap)
                {
                    _mapFileBuilder = new MapFileBuilder(_outputInfoBuilder);
                }

                if (generateSymbols)
                {
                    _symbolFileBuilder = new SymbolFileBuilder(_outputInfoBuilder, _nodeFactory.Target);
                }

                if (generateProfileFile)
                {
                    _profileFileBuilder = new ProfileFileBuilder(_outputInfoBuilder, callChainProfile, _nodeFactory.Target);
                }
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
                int? timeDateStamp;
                ISymbolNode r2rHeaderExportSymbol;
                Func<IEnumerable<Blob>, BlobContentId> peIdProvider = null;

                if (_nodeFactory.CompilationModuleGroup.IsCompositeBuildMode && _componentModule == null)
                {
                    headerBuilder = PEHeaderProvider.Create(Subsystem.Unknown, _nodeFactory.Target, _nodeFactory.ImageBase);
                    peIdProvider = new Func<IEnumerable<Blob>, BlobContentId>(content => BlobContentId.FromHash(CryptographicHashProvider.ComputeSourceHash(content)));
                    timeDateStamp = null;
                    r2rHeaderExportSymbol = _nodeFactory.Header;
                }
                else
                {
                    PEReader inputPeReader = (_componentModule != null ? _componentModule.PEReader : _nodeFactory.CompilationModuleGroup.CompilationModuleSet.First().PEReader);
                    headerBuilder = PEHeaderProvider.Create(inputPeReader.PEHeaders.PEHeader.Subsystem, _nodeFactory.Target, _nodeFactory.ImageBase);
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
                    _customPESectionAlignment,
                    peIdProvider);

                NativeDebugDirectoryEntryNode nativeDebugDirectoryEntryNode = null;
                PerfMapDebugDirectoryEntryNode perfMapDebugDirectoryEntryNode = null;
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

                    if (node is PerfMapDebugDirectoryEntryNode pmdeNode)
                    {
                        // There should be only one PerfMapDebugDirectoryEntryNode.
                        Debug.Assert(perfMapDebugDirectoryEntryNode is null);
                        perfMapDebugDirectoryEntryNode = pmdeNode;
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

                    EmitObjectData(r2rPeBuilder, nodeContents, nodeIndex, name, node.Section);
                    lastWrittenObjectNode = node;

                    if (_outputInfoBuilder != null && node is MethodWithGCInfo methodNode)
                    {
                        _outputInfoBuilder.AddMethod(methodNode, nodeContents.DefinedSymbols[0]);
                    }
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

                if (_outputInfoBuilder != null)
                {
                    foreach (string inputFile in _inputFiles)
                    {
                        _outputInfoBuilder.AddInputModule(_nodeFactory.TypeSystemContext.GetModuleFromPath(inputFile));
                    }
                }

                using (var peStream = File.Create(_objectFilePath))
                {
                    r2rPeBuilder.Write(peStream, timeDateStamp);

                    if (_mapFileBuilder != null)
                    {
                        _mapFileBuilder.SetFileSize(peStream.Length);
                    }

                    if (nativeDebugDirectoryEntryNode is not null)
                    {
                        Debug.Assert(_generatePdbFile);
                        // Compute hash of the output image and store that in the native DebugDirectory entry
                        using (var hashAlgorithm = SHA256.Create())
                        {
                            peStream.Seek(0, SeekOrigin.Begin);
                            byte[] hash = hashAlgorithm.ComputeHash(peStream);
                            byte[] rsdsEntry = nativeDebugDirectoryEntryNode.GenerateRSDSEntryData(hash);

                            int offsetToUpdate = r2rPeBuilder.GetSymbolFilePosition(nativeDebugDirectoryEntryNode);
                            peStream.Seek(offsetToUpdate, SeekOrigin.Begin);
                            peStream.Write(rsdsEntry);
                        }
                    }

                    if (perfMapDebugDirectoryEntryNode is not null)
                    {
                        Debug.Assert(_generatePerfMapFile && _outputInfoBuilder is not null && _outputInfoBuilder.EnumerateInputAssemblies().Any());
                        byte[] perfmapSig = PerfMapWriter.PerfMapV1SignatureHelper(_outputInfoBuilder.EnumerateInputAssemblies(), _nodeFactory.Target);
                        byte[] perfMapEntry = perfMapDebugDirectoryEntryNode.GeneratePerfMapEntryData(perfmapSig, _perfMapFormatVersion);

                        int offsetToUpdate = r2rPeBuilder.GetSymbolFilePosition(perfMapDebugDirectoryEntryNode);
                        peStream.Seek(offsetToUpdate, SeekOrigin.Begin);
                        peStream.Write(perfMapEntry);
                    }
                }

                if (_outputInfoBuilder != null)
                {
                    r2rPeBuilder.AddSections(_outputInfoBuilder);

                    if (_generateMapFile)
                    {
                        string mapFileName = Path.ChangeExtension(_objectFilePath, ".map");
                        _mapFileBuilder.SaveMap(mapFileName);
                    }

                    if (_generateMapCsvFile)
                    {
                        string nodeStatsCsvFileName = Path.ChangeExtension(_objectFilePath, ".nodestats.csv");
                        string mapCsvFileName = Path.ChangeExtension(_objectFilePath, ".map.csv");
                        _mapFileBuilder.SaveCsv(nodeStatsCsvFileName, mapCsvFileName);
                    }

                    if (_generatePdbFile)
                    {
                        string path = _pdbPath;
                        if (string.IsNullOrEmpty(path))
                        {
                            path = Path.GetDirectoryName(_objectFilePath);
                        }
                        _symbolFileBuilder.SavePdb(path, _objectFilePath);
                    }

                    if (_generatePerfMapFile)
                    {
                        string path = _perfMapPath;
                        if (string.IsNullOrEmpty(path))
                        {
                            path = Path.GetDirectoryName(_objectFilePath);
                        }
                        _symbolFileBuilder.SavePerfMap(path, _perfMapFormatVersion, _objectFilePath);
                    }

                    if (_profileFileBuilder != null)
                    {
                        string path = Path.ChangeExtension(_objectFilePath, ".profile");
                        _profileFileBuilder.SaveProfile(path);
                    }
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
        private void EmitObjectData(R2RPEBuilder r2rPeBuilder, ObjectData data, int nodeIndex, string name, ObjectNodeSection section)
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
                else
                {
                    _previouslyWrittenNodeNames.Add(symbolName, new NodeInfo(definedSymbol, nodeIndex, symbolIndex));
                }
            }
#endif

            r2rPeBuilder.AddObjectData(data, section, name, _outputInfoBuilder);
        }

        public static void EmitObject(
            string objectFilePath,
            EcmaModule componentModule,
            IEnumerable<string> inputFiles,
            IEnumerable<DependencyNode> nodes,
            NodeFactory factory,
            bool generateMapFile,
            bool generateMapCsvFile,
            bool generatePdbFile,
            string pdbPath,
            bool generatePerfMapFile,
            string perfMapPath,
            int perfMapFormatVersion,
            bool generateProfileFile,
            CallChainProfile callChainProfile,
            int customPESectionAlignment)
        {
            Console.WriteLine($@"Emitting R2R PE file: {objectFilePath}");
            ReadyToRunObjectWriter objectWriter = new ReadyToRunObjectWriter(
                objectFilePath,
                componentModule,
                inputFiles,
                nodes,
                factory,
                generateMapFile: generateMapFile,
                generateMapCsvFile: generateMapCsvFile,
                generatePdbFile: generatePdbFile,
                pdbPath: pdbPath,
                generatePerfMapFile: generatePerfMapFile,
                perfMapPath: perfMapPath,
                perfMapFormatVersion: perfMapFormatVersion,
                generateProfileFile: generateProfileFile,
                callChainProfile,
                customPESectionAlignment);
            objectWriter.EmitPortableExecutable();
        }
    }
}
