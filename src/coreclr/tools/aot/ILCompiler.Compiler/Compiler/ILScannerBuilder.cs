// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem;
using Internal.IL;

namespace ILCompiler
{
    public sealed class ILScannerBuilder
    {
        private readonly CompilerTypeSystemContext _context;
        private readonly CompilationModuleGroup _compilationGroup;
        private readonly NameMangler _nameMangler;
        private readonly ILProvider _ilProvider;
        private readonly PreinitializationManager _preinitializationManager;

        // These need to provide reasonable defaults so that the user can optionally skip
        // calling the Use/Configure methods and still get something reasonable back.
        private Logger _logger = Logger.Null;
        private DependencyTrackingLevel _dependencyTrackingLevel = DependencyTrackingLevel.None;
        private IEnumerable<ICompilationRootProvider> _compilationRoots = Array.Empty<ICompilationRootProvider>();
        private MetadataManager _metadataManager;
        private InteropStubManager _interopStubManager = new EmptyInteropStubManager();
        private int _parallelism = -1;

        internal ILScannerBuilder(CompilerTypeSystemContext context, CompilationModuleGroup compilationGroup, NameMangler mangler, ILProvider ilProvider, PreinitializationManager preinitializationManager)
        {
            _context = context;
            _compilationGroup = compilationGroup;
            _nameMangler = mangler;
            _metadataManager = new AnalysisBasedMetadataManager(context);
            _ilProvider = ilProvider;
            _preinitializationManager = preinitializationManager;
        }

        public ILScannerBuilder UseDependencyTracking(DependencyTrackingLevel trackingLevel)
        {
            _dependencyTrackingLevel = trackingLevel;
            return this;
        }

        public ILScannerBuilder UseCompilationRoots(IEnumerable<ICompilationRootProvider> compilationRoots)
        {
            _compilationRoots = compilationRoots;
            return this;
        }

        public ILScannerBuilder UseMetadataManager(MetadataManager metadataManager)
        {
            _metadataManager = metadataManager;
            return this;
        }

        public ILScannerBuilder UseInteropStubManager(InteropStubManager interopStubManager)
        {
            _interopStubManager = interopStubManager;
            return this;
        }

        public ILScannerBuilder UseParallelism(int parallelism)
        {
            _parallelism = parallelism;
            return this;
        }

        public ILScannerBuilder UseLogger(Logger logger)
        {
            _logger = logger;
            return this;
        }

        public IILScanner ToILScanner()
        {
            var nodeFactory = new ILScanNodeFactory(_context, _compilationGroup, _metadataManager, _interopStubManager, _nameMangler, _preinitializationManager);
            DependencyAnalyzerBase<NodeFactory> graph = _dependencyTrackingLevel.CreateDependencyGraph(nodeFactory);

            return new ILScanner(graph, nodeFactory, _compilationRoots, _ilProvider, new NullDebugInformationProvider(), _logger, _parallelism);
        }
    }
}
