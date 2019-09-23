// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.PortableExecutable;

using Internal.IL;
using Internal.JitInterface;
using Internal.TypeSystem;

using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysis.ReadyToRun;
using ILCompiler.DependencyAnalysisFramework;

namespace ILCompiler
{
    public abstract class Compilation : ICompilation
    {
        protected readonly DependencyAnalyzerBase<NodeFactory> _dependencyGraph;
        protected readonly NodeFactory _nodeFactory;
        protected readonly Logger _logger;
        private readonly DevirtualizationManager _devirtualizationManager;
        private ILCache _methodILCache;
        private readonly HashSet<ModuleDesc> _modulesBeingInstrumented;


        public NameMangler NameMangler => _nodeFactory.NameMangler;
        public NodeFactory NodeFactory => _nodeFactory;
        public CompilerTypeSystemContext TypeSystemContext => NodeFactory.TypeSystemContext;
        public Logger Logger => _logger;

        protected Compilation(
            DependencyAnalyzerBase<NodeFactory> dependencyGraph,
            NodeFactory nodeFactory,
            IEnumerable<ICompilationRootProvider> compilationRoots,
            ILProvider ilProvider,
            DevirtualizationManager devirtualizationManager,
            IEnumerable<ModuleDesc> modulesBeingInstrumented,
            Logger logger)
        {
            _dependencyGraph = dependencyGraph;
            _nodeFactory = nodeFactory;
            _logger = logger;
            _devirtualizationManager = devirtualizationManager;
            _modulesBeingInstrumented = new HashSet<ModuleDesc>(modulesBeingInstrumented);

            _dependencyGraph.ComputeDependencyRoutine += ComputeDependencyNodeDependencies;
            NodeFactory.AttachToDependencyGraph(_dependencyGraph);

            var rootingService = new RootingServiceProvider(nodeFactory, _dependencyGraph.AddRoot);
            foreach (var rootProvider in compilationRoots)
                rootProvider.AddCompilationRoots(rootingService);

            _methodILCache = new ILCache(ilProvider);
        }

        public abstract void Compile(string outputFileName);

        protected abstract void ComputeDependencyNodeDependencies(List<DependencyNodeCore<NodeFactory>> obj);

        public bool CanInline(MethodDesc caller, MethodDesc callee)
        {
            return NodeFactory.CompilationModuleGroup.CanInline(caller, callee);
        }

        public virtual MethodIL GetMethodIL(MethodDesc method)
        {
            // Flush the cache when it grows too big
            if (_methodILCache.Count > 1000)
                _methodILCache = new ILCache(_methodILCache.ILProvider);

            return _methodILCache.GetOrCreateValue(method).MethodIL;
        }

        public bool IsEffectivelySealed(TypeDesc type)
        {
            return _devirtualizationManager.IsEffectivelySealed(type);
        }

        public bool IsEffectivelySealed(MethodDesc method)
        {
            return _devirtualizationManager.IsEffectivelySealed(method);
        }

        public MethodDesc ResolveVirtualMethod(MethodDesc declMethod, TypeDesc implType)
        {
            return _devirtualizationManager.ResolveVirtualMethod(declMethod, implType);
        }

        public bool IsModuleInstrumented(ModuleDesc module)
        {
            return _modulesBeingInstrumented.Contains(module);
        }

        private sealed class ILCache : LockFreeReaderHashtable<MethodDesc, ILCache.MethodILData>
        {
            public ILProvider ILProvider { get; }

            public ILCache(ILProvider provider)
            {
                ILProvider = provider;
            }

            protected override int GetKeyHashCode(MethodDesc key)
            {
                return key.GetHashCode();
            }
            protected override int GetValueHashCode(MethodILData value)
            {
                return value.Method.GetHashCode();
            }
            protected override bool CompareKeyToValue(MethodDesc key, MethodILData value)
            {
                return Object.ReferenceEquals(key, value.Method);
            }
            protected override bool CompareValueToValue(MethodILData value1, MethodILData value2)
            {
                return Object.ReferenceEquals(value1.Method, value2.Method);
            }
            protected override MethodILData CreateValueFromKey(MethodDesc key)
            {
                return new MethodILData() { Method = key, MethodIL = ILProvider.GetMethodIL(key) };
            }

            internal class MethodILData
            {
                public MethodDesc Method;
                public MethodIL MethodIL;
            }
        }

        private delegate void RootAdder(object o, string reason);

        private class RootingServiceProvider : IRootingServiceProvider
        {
            private readonly NodeFactory _factory;
            private readonly RootAdder _rootAdder;

            public RootingServiceProvider(NodeFactory factory, RootAdder rootAdder)
            {
                _factory = factory;
                _rootAdder = rootAdder;
            }

            public void AddCompilationRoot(MethodDesc method, string reason)
            {
                MethodDesc canonMethod = method.GetCanonMethodTarget(CanonicalFormKind.Specific);
                IMethodNode methodEntryPoint = _factory.MethodEntrypoint(canonMethod);
                _rootAdder(methodEntryPoint, reason);
            }

            public void AddCompilationRoot(TypeDesc type, string reason)
            {
                _rootAdder(_factory.ConstructedTypeSymbol(type), reason);
            }
        }
    }

    public interface ICompilation
    {
        void Compile(string outputFileName);
    }

    public sealed class ReadyToRunCodegenCompilation : Compilation
    {
        /// <summary>
        /// JIT configuration provider.
        /// </summary>
        private readonly JitConfigProvider _jitConfigProvider;

        /// <summary>
        /// Name of the compilation input MSIL file.
        /// </summary>
        private readonly string _inputFilePath;

        /// <summary>
        /// JIT interface implementation.
        /// </summary>
        private readonly CorInfoImpl _corInfo;

        private bool _resilient;

        public new ReadyToRunCodegenNodeFactory NodeFactory { get; }

        public ReadyToRunSymbolNodeFactory SymbolNodeFactory { get; }

        internal ReadyToRunCodegenCompilation(
            DependencyAnalyzerBase<NodeFactory> dependencyGraph,
            ReadyToRunCodegenNodeFactory nodeFactory,
            IEnumerable<ICompilationRootProvider> roots,
            ILProvider ilProvider,
            Logger logger,
            DevirtualizationManager devirtualizationManager,
            JitConfigProvider configProvider,
            string inputFilePath,
            IEnumerable<ModuleDesc> modulesBeingInstrumented,
            bool resilient)
            : base(dependencyGraph, nodeFactory, roots, ilProvider, devirtualizationManager, modulesBeingInstrumented, logger)
        {
            _resilient = resilient;
            NodeFactory = nodeFactory;
            SymbolNodeFactory = new ReadyToRunSymbolNodeFactory(nodeFactory);
            _jitConfigProvider = configProvider;

            _inputFilePath = inputFilePath;
            _corInfo = new CorInfoImpl(this, _jitConfigProvider);
        }

        public override void Compile(string outputFile)
        {
            using (FileStream inputFile = File.OpenRead(_inputFilePath))
            {
                PEReader inputPeReader = new PEReader(inputFile);

                _dependencyGraph.ComputeMarkedNodes();
                var nodes = _dependencyGraph.MarkedNodeList;

                PerfEventSource.Log.EmittingStart();
                NodeFactory.SetMarkingComplete();
                ReadyToRunObjectWriter.EmitObject(inputPeReader, outputFile, nodes, NodeFactory);
                PerfEventSource.Log.EmittingStop();
            }
        }

        internal bool IsInheritanceChainLayoutFixedInCurrentVersionBubble(TypeDesc type)
        {
            // TODO: implement
            return true;
        }

        public TypeDesc GetTypeOfRuntimeType()
        {
            return TypeSystemContext.SystemModule.GetKnownType("System", "RuntimeType");
        }

        protected override void ComputeDependencyNodeDependencies(List<DependencyNodeCore<NodeFactory>> obj)
        {
            foreach (DependencyNodeCore<NodeFactory> dependency in obj)
            {
                var methodCodeNodeNeedingCode = dependency as MethodWithGCInfo;
                if (methodCodeNodeNeedingCode == null)
                {
                    // To compute dependencies of the shadow method that tracks dictionary
                    // dependencies we need to ensure there is code for the canonical method body.
                    var dependencyMethod = (ShadowConcreteMethodNode)dependency;
                    methodCodeNodeNeedingCode = (MethodWithGCInfo)dependencyMethod.CanonicalMethodNode;
                }

                // We might have already compiled this method.
                if (methodCodeNodeNeedingCode.StaticDependenciesAreComputed)
                    continue;

                MethodDesc method = methodCodeNodeNeedingCode.Method;
                if (!NodeFactory.CompilationModuleGroup.ContainsMethodBody(method, unboxingStub: false))
                {
                    // Don't drill into methods defined outside of this version bubble
                    continue;
                }

                if (Logger.IsVerbose)
                {
                    string methodName = method.ToString();
                    Logger.Writer.WriteLine("Compiling " + methodName);
                }

                try
                {
                    PerfEventSource.Log.JitStart();
                    _corInfo.CompileMethod(methodCodeNodeNeedingCode);
                }
                catch (TypeSystemException ex)
                {
                    // If compilation fails, don't emit code for this method. It will be Jitted at runtime
                    Logger.Writer.WriteLine($"Warning: Method `{method}` was not compiled because: {ex.Message}");
                }
                catch (RequiresRuntimeJitException ex)
                {
                    Logger.Writer.WriteLine($"Info: Method `{method}` was not compiled because `{ex.Message}` requires runtime JIT");
                }
                catch (CodeGenerationFailedException ex) when (_resilient)
                {
                    Logger.Writer.WriteLine($"Warning: Method `{method}` was not compiled because `{ex.Message}` requires runtime JIT");
                }
                finally
                {
                    PerfEventSource.Log.JitStop();
                }
            }
        }

        public ISymbolNode GetFieldRvaData(FieldDesc field) => NodeFactory.CopiedFieldRva(field);
    }
}
