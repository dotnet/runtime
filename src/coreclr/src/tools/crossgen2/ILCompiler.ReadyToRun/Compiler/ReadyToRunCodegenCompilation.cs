// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Threading;

using Internal.IL;
using Internal.IL.Stubs;
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

            _methodILCache = new ILCache(ilProvider, NodeFactory.CompilationModuleGroup);
        }

        public abstract void Compile(string outputFileName);
        public abstract void WriteDependencyLog(string outputFileName);

        protected abstract void ComputeDependencyNodeDependencies(List<DependencyNodeCore<NodeFactory>> obj);

        public bool CanInline(MethodDesc caller, MethodDesc callee)
        {
            // Check to see if the method requires a security object.  This means they call demand and
            // shouldn't be inlined.
            if (callee.RequireSecObject)
            {
                return false;
            }

            return NodeFactory.CompilationModuleGroup.CanInline(caller, callee);
        }

        public virtual MethodIL GetMethodIL(MethodDesc method)
        {
            // Flush the cache when it grows too big
            if (_methodILCache.Count > 1000)
                _methodILCache = new ILCache(_methodILCache.ILProvider, NodeFactory.CompilationModuleGroup);

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
            private readonly CompilationModuleGroup _compilationModuleGroup;

            public ILCache(ILProvider provider, CompilationModuleGroup compilationModuleGroup)
            {
                ILProvider = provider;
                _compilationModuleGroup = compilationModuleGroup;
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
                MethodIL methodIL = ILProvider.GetMethodIL(key);
                if (methodIL == null
                    && key.IsPInvoke
                    && _compilationModuleGroup.GeneratesPInvoke(key))
                {
                    // TODO: enable when IL Stubs are fixed to be non-shared
                    // methodIL = PInvokeILEmitter.EmitIL(key);
                }

                return new MethodILData() { Method = key, MethodIL = methodIL };
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
        void WriteDependencyLog(string outputFileName);
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
        }

        public override void Compile(string outputFile)
        {
            using (FileStream inputFile = File.OpenRead(_inputFilePath))
            {
                PEReader inputPeReader = new PEReader(inputFile);

                _dependencyGraph.ComputeMarkedNodes();
                var nodes = _dependencyGraph.MarkedNodeList;

                using (PerfEventSource.StartStopEvents.EmittingEvents())
                {
                    NodeFactory.SetMarkingComplete();
                    ReadyToRunObjectWriter.EmitObject(inputPeReader, outputFile, nodes, NodeFactory);
                }
            }
        }

        public override void WriteDependencyLog(string outputFileName)
        {
            using (FileStream dgmlOutput = new FileStream(outputFileName, FileMode.Create))
            {
                DgmlWriter.WriteDependencyGraphToStream(dgmlOutput, _dependencyGraph, _nodeFactory);
                dgmlOutput.Flush();
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
            using (PerfEventSource.StartStopEvents.JitEvents())
            {
                ConditionalWeakTable<Thread, CorInfoImpl> cwt = new ConditionalWeakTable<Thread, CorInfoImpl>();
                foreach (DependencyNodeCore<NodeFactory> dependency in obj)
                {
                    MethodWithGCInfo methodCodeNodeNeedingCode = dependency as MethodWithGCInfo;
                    MethodDesc method = methodCodeNodeNeedingCode.Method;

                    if (Logger.IsVerbose)
                    {
                        string methodName = method.ToString();
                        Logger.Writer.WriteLine("Compiling " + methodName);
                    }

                    try
                    {
                        using (PerfEventSource.StartStopEvents.JitMethodEvents())
                        {
                            CorInfoImpl corInfoImpl = cwt.GetValue(Thread.CurrentThread, thread => new CorInfoImpl(this, _jitConfigProvider));
                            corInfoImpl.CompileMethod(methodCodeNodeNeedingCode);
                        }
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
                }
            }
        }

        public ISymbolNode GetFieldRvaData(FieldDesc field) => NodeFactory.CopiedFieldRva(field);
    }
}
