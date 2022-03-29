// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;

using Internal.IL;

namespace ILCompiler
{
    public abstract partial class CompilationBuilder
    {
        protected readonly CompilerTypeSystemContext _context;
        protected readonly CompilationModuleGroup _compilationGroup;
        protected readonly NameMangler _nameMangler;

        // These need to provide reasonable defaults so that the user can optionally skip
        // calling the Use/Configure methods and still get something reasonable back.
        protected Logger _logger = Logger.Null;
        private DependencyTrackingLevel _dependencyTrackingLevel = DependencyTrackingLevel.None;
        protected IEnumerable<ICompilationRootProvider> _compilationRoots = Array.Empty<ICompilationRootProvider>();
        protected OptimizationMode _optimizationMode = OptimizationMode.None;
        protected int _parallelism = -1;

        public CompilationBuilder(CompilerTypeSystemContext context, CompilationModuleGroup compilationGroup, NameMangler nameMangler)
        {
            _context = context;
            _compilationGroup = compilationGroup;
            _nameMangler = nameMangler;
            InitializePartial();
        }

        // Partial class specific initialization
        partial void InitializePartial();

        public CompilationBuilder UseLogger(Logger logger)
        {
            _logger = logger;
            return this;
        }

        public CompilationBuilder UseParallelism(int parallelism)
        {
            _parallelism = parallelism;
            return this;
        }

        public CompilationBuilder UseCompilationUnitPrefix(string prefix)
        {
            _nameMangler.CompilationUnitPrefix = prefix;
            return this;
        }

        public CompilationBuilder UseDependencyTracking(DependencyTrackingLevel trackingLevel)
        {
            _dependencyTrackingLevel = trackingLevel;
            return this;
        }

        public CompilationBuilder UseCompilationRoots(IEnumerable<ICompilationRootProvider> compilationRoots)
        {
            _compilationRoots = compilationRoots;
            return this;
        }

        public CompilationBuilder UseOptimizationMode(OptimizationMode mode)
        {
            _optimizationMode = mode;
            return this;
        }

        public abstract CompilationBuilder UseBackendOptions(IEnumerable<string> options);

        public abstract CompilationBuilder UseILProvider(ILProvider ilProvider);

        protected abstract ILProvider GetILProvider();

        protected DependencyAnalyzerBase<NodeFactory> CreateDependencyGraph(NodeFactory factory, IComparer<DependencyNodeCore<NodeFactory>> comparer = null)
        {
            return _dependencyTrackingLevel.CreateDependencyGraph(factory, comparer);
        }

        public abstract ICompilation ToCompilation();
    }

    /// <summary>
    /// Represents the level of optimizations performed by the compiler.
    /// </summary>
    public enum OptimizationMode
    {
        /// <summary>
        /// Do not optimize.
        /// </summary>
        None,

        /// <summary>
        /// Minimize code space.
        /// </summary>
        PreferSize,

        /// <summary>
        /// Generate blended code. (E.g. favor size for rarely executed code such as class constructors.)
        /// </summary>
        Blended,

        /// <summary>
        /// Maximize execution speed.
        /// </summary>
        PreferSpeed,
    }
}
