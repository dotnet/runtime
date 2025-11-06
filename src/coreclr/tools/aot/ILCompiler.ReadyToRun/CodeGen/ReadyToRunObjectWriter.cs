// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using ILCompiler.DependencyAnalysis.ReadyToRun;
using ILCompiler.DependencyAnalysisFramework;
using ILCompiler.Diagnostics;
using ILCompiler.ObjectWriter;
using ILCompiler.PEWriter;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using ObjectData = ILCompiler.DependencyAnalysis.ObjectNode.ObjectData;

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
        private readonly IReadOnlyCollection<DependencyNode> _nodes;

        /// <summary>
        /// Set to non-null when generating symbol info or profile info.
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

        public ReadyToRunObjectWriter(
            string objectFilePath,
            EcmaModule componentModule,
            IEnumerable<string> inputFiles,
            IReadOnlyCollection<DependencyNode> nodes,
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

        public void EmitReadyToRunObjects(ReadyToRunContainerFormat format, Logger logger)
        {
            bool succeeded = false;

            try
            {
                var stopwatch = Stopwatch.StartNew();

                Debug.Assert(format == ReadyToRunContainerFormat.PE);

                int? timeDateStamp;

                if (_nodeFactory.CompilationModuleGroup.IsCompositeBuildMode && _componentModule == null)
                {
                    timeDateStamp = null;
                }
                else
                {
                    PEReader inputPeReader = (_componentModule != null ? _componentModule.PEReader : _nodeFactory.CompilationModuleGroup.CompilationModuleSet.First().PEReader);
                    timeDateStamp = inputPeReader.PEHeaders.CoffHeader.TimeDateStamp;
                }

                PEObjectWriter objectWriter = new(_nodeFactory, ObjectWritingOptions.None, _outputInfoBuilder, _objectFilePath, _customPESectionAlignment, timeDateStamp);

                if (_nodeFactory.CompilationModuleGroup.IsCompositeBuildMode && _componentModule == null)
                {
                    objectWriter.AddExportedSymbol("RTR_HEADER");
                }

                using FileStream stream = new FileStream(_objectFilePath, FileMode.Create);
                objectWriter.EmitObject(stream, _nodes, dumper: null, logger);

                if (_outputInfoBuilder is not null)
                {
                    foreach (MethodWithGCInfo methodNode in _nodeFactory.EnumerateCompiledMethods())
                        _outputInfoBuilder.AddMethod(methodNode, methodNode);
                }

                if (_mapFileBuilder != null)
                {
                    _mapFileBuilder.SetFileSize(stream.Length);
                }

                if (_outputInfoBuilder is not null)
                {
                    foreach (string inputFile in _inputFiles)
                    {
                        _outputInfoBuilder.AddInputModule(_nodeFactory.TypeSystemContext.GetModuleFromPath(inputFile));
                    }
                }

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

                stopwatch.Stop();
                if (logger.IsVerbose)
                    logger.LogMessage($"Done writing object file in {stopwatch.Elapsed}");
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

        public static void EmitObject(
            string objectFilePath,
            EcmaModule componentModule,
            IEnumerable<string> inputFiles,
            IReadOnlyCollection<DependencyNode> nodes,
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
            ReadyToRunContainerFormat format,
            int customPESectionAlignment,
            Logger logger)
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

            objectWriter.EmitReadyToRunObjects(format, logger);
        }
    }
}
