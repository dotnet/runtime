// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Internal.IL;
using Internal.IL.Stubs;
using Internal.JitInterface;
using Internal.ReadyToRunConstants;
using Internal.TypeSystem;

using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysis.ReadyToRun;
using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
{
    public abstract class Compilation : ICompilation, IDisposable
    {
        protected readonly DependencyAnalyzerBase<NodeFactory> _dependencyGraph;
        protected readonly NodeFactory _nodeFactory;
        protected readonly Logger _logger;
        private readonly DevirtualizationManager _devirtualizationManager;
        protected ILCache _methodILCache;
        private readonly HashSet<ModuleDesc> _modulesBeingInstrumented;


        public NameMangler NameMangler => _nodeFactory.NameMangler;
        public NodeFactory NodeFactory => _nodeFactory;
        public CompilerTypeSystemContext TypeSystemContext => NodeFactory.TypeSystemContext;
        public Logger Logger => _logger;

        public InstructionSetSupport InstructionSetSupport { get; }

        protected Compilation(
            DependencyAnalyzerBase<NodeFactory> dependencyGraph,
            NodeFactory nodeFactory,
            IEnumerable<ICompilationRootProvider> compilationRoots,
            ILProvider ilProvider,
            DevirtualizationManager devirtualizationManager,
            IEnumerable<ModuleDesc> modulesBeingInstrumented,
            Logger logger,
            InstructionSetSupport instructionSetSupport)
        {
            InstructionSetSupport = instructionSetSupport;
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

        public abstract void Dispose();
        public abstract void Compile(string outputFileName);
        public abstract void WriteDependencyLog(string outputFileName);

        protected abstract void ComputeDependencyNodeDependencies(List<DependencyNodeCore<NodeFactory>> obj);

        public bool CanInline(MethodDesc caller, MethodDesc callee)
        {
            if (JitConfigProvider.Instance.HasFlag(CorJitFlag.CORJIT_FLAG_DEBUG_CODE))
            {
                // If the callee wants debuggable code, don't allow it to be inlined
                return false;
            }

            if (callee.IsNoInlining)
            {
                return false;
            }

            // Check to see if the method requires a security object.  This means they call demand and
            // shouldn't be inlined.
            if (callee.RequireSecObject)
            {
                return false;
            }

            // If the method is MethodImpl'd by another method within the same type, then we have
            // an issue that the importer will import the wrong body. In this case, we'll just
            // disallow inlining because getFunctionEntryPoint will do the right thing.
            if (callee.IsVirtual)
            {
                MethodDesc calleeMethodImpl = callee.OwningType.FindVirtualFunctionTargetMethodOnObjectType(callee);
                if (calleeMethodImpl != callee)
                {
                    return false;
                }
            }

            return NodeFactory.CompilationModuleGroup.CanInline(caller, callee);
        }

        public virtual MethodIL GetMethodIL(MethodDesc method)
        {
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

        public MethodDesc ResolveVirtualMethod(MethodDesc declMethod, TypeDesc implType, out CORINFO_DEVIRTUALIZATION_DETAIL devirtualizationDetail)
        {
            return _devirtualizationManager.ResolveVirtualMethod(declMethod, implType, out devirtualizationDetail);
        }

        public bool IsModuleInstrumented(ModuleDesc module)
        {
            return _modulesBeingInstrumented.Contains(module);
        }

        public sealed class ILCache : LockFreeReaderHashtable<MethodDesc, ILCache.MethodILData>
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
                    methodIL = PInvokeILEmitter.EmitIL(key);
                }

                return new MethodILData() { Method = key, MethodIL = methodIL };
            }

            public class MethodILData
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
            private readonly DeferredTillPhaseNode _deferredPhaseNode = new DeferredTillPhaseNode(1);

            public RootingServiceProvider(NodeFactory factory, RootAdder rootAdder)
            {
                _factory = factory;
                _rootAdder = rootAdder;
                _rootAdder(_deferredPhaseNode, "Deferred nodes");
            }

            public void AddCompilationRoot(MethodDesc method, bool rootMinimalDependencies, string reason)
            {
                MethodDesc canonMethod = method.GetCanonMethodTarget(CanonicalFormKind.Specific);
                if (_factory.CompilationModuleGroup.ContainsMethodBody(canonMethod, false))
                {
                    IMethodNode methodEntryPoint = _factory.CompiledMethodNode(canonMethod);

                    if (rootMinimalDependencies)
                    {
                        _deferredPhaseNode.AddDependency((DependencyNodeCore<NodeFactory>)methodEntryPoint);
                    }
                    else
                    {
                        _rootAdder(methodEntryPoint, reason);
                    }
                }
            }
        }
    }

    public interface ICompilation
    {
        void Compile(string outputFileName);
        void WriteDependencyLog(string outputFileName);
        void Dispose();
    }

    public sealed class ReadyToRunCodegenCompilation : Compilation
    {
        /// <summary>
        /// We only need one CorInfoImpl per thread, and we don't want to unnecessarily construct them
        /// because their construction takes a significant amount of time.
        /// </summary>
        private readonly ConditionalWeakTable<Thread, CorInfoImpl> _corInfoImpls;

        /// <summary>
        /// Input MSIL file names.
        /// </summary>
        private readonly IEnumerable<string> _inputFiles;

        private readonly string _compositeRootPath;

        private readonly bool _resilient;

        private readonly int _parallelism;

        private readonly bool _generateMapFile;
        private readonly bool _generateMapCsvFile;
        private readonly bool _generatePdbFile;
        private readonly string _pdbPath;
        private readonly bool _generatePerfMapFile;
        private readonly string _perfMapPath;
        private readonly int _perfMapFormatVersion;
        private readonly bool _generateProfileFile;
        private readonly Func<MethodDesc, string> _printReproInstructions;

        private readonly ProfileDataManager _profileData;
        private readonly ReadyToRunFileLayoutOptimizer _fileLayoutOptimizer;

        public ProfileDataManager ProfileData => _profileData;

        public ReadyToRunSymbolNodeFactory SymbolNodeFactory { get; }
        public ReadyToRunCompilationModuleGroupBase CompilationModuleGroup { get; }
        private readonly int _customPESectionAlignment;
        /// <summary>
        /// Determining whether a type's layout is fixed is a little expensive and the question can be asked many times
        /// for the same type during compilation so preserve the computed value.
        /// </summary>
        private ConcurrentDictionary<TypeDesc, bool> _computedFixedLayoutTypes = new ConcurrentDictionary<TypeDesc, bool>();

        internal ReadyToRunCodegenCompilation(
            DependencyAnalyzerBase<NodeFactory> dependencyGraph,
            NodeFactory nodeFactory,
            IEnumerable<ICompilationRootProvider> roots,
            ILProvider ilProvider,
            Logger logger,
            DevirtualizationManager devirtualizationManager,
            IEnumerable<string> inputFiles,
            string compositeRootPath,
            InstructionSetSupport instructionSetSupport,
            bool resilient,
            bool generateMapFile,
            bool generateMapCsvFile,
            bool generatePdbFile,
            Func<MethodDesc, string> printReproInstructions,
            string pdbPath,
            bool generatePerfMapFile,
            string perfMapPath,
            int perfMapFormatVersion,
            bool generateProfileFile,
            int parallelism,
            ProfileDataManager profileData,
            ReadyToRunMethodLayoutAlgorithm methodLayoutAlgorithm,
            ReadyToRunFileLayoutAlgorithm fileLayoutAlgorithm,
            int customPESectionAlignment,
            bool verifyTypeAndFieldLayout)
            : base(
                  dependencyGraph,
                  nodeFactory,
                  roots,
                  ilProvider,
                  devirtualizationManager,
                  modulesBeingInstrumented: nodeFactory.CompilationModuleGroup.CompilationModuleSet,
                  logger,
                  instructionSetSupport)
        {
            _resilient = resilient;
            _parallelism = parallelism;
            _generateMapFile = generateMapFile;
            _generateMapCsvFile = generateMapCsvFile;
            _generatePdbFile = generatePdbFile;
            _pdbPath = pdbPath;
            _generatePerfMapFile = generatePerfMapFile;
            _perfMapPath = perfMapPath;
            _perfMapFormatVersion = perfMapFormatVersion;
            _generateProfileFile = generateProfileFile;
            _customPESectionAlignment = customPESectionAlignment;
            SymbolNodeFactory = new ReadyToRunSymbolNodeFactory(nodeFactory, verifyTypeAndFieldLayout);
            if (nodeFactory.InstrumentationDataTable != null)
                nodeFactory.InstrumentationDataTable.Initialize(SymbolNodeFactory);
            _corInfoImpls = new ConditionalWeakTable<Thread, CorInfoImpl>();
            _inputFiles = inputFiles;
            _compositeRootPath = compositeRootPath;
            _printReproInstructions = printReproInstructions;
            CompilationModuleGroup = (ReadyToRunCompilationModuleGroupBase)nodeFactory.CompilationModuleGroup;

            // Generate baseline support specification for InstructionSetSupport. This will prevent usage of the generated
            // code if the runtime environment doesn't support the specified instruction set
            string instructionSetSupportString = ReadyToRunInstructionSetSupportSignature.ToInstructionSetSupportString(instructionSetSupport);
            ReadyToRunInstructionSetSupportSignature instructionSetSupportSig = new ReadyToRunInstructionSetSupportSignature(instructionSetSupportString);
            _dependencyGraph.AddRoot(new Import(NodeFactory.EagerImports, instructionSetSupportSig), "Baseline instruction set support");

            _profileData = profileData;

            _fileLayoutOptimizer = new ReadyToRunFileLayoutOptimizer(logger, methodLayoutAlgorithm, fileLayoutAlgorithm, profileData, _nodeFactory);
        }

        private readonly static string s_folderUpPrefix = ".." + Path.DirectorySeparatorChar;

        public override void Compile(string outputFile)
        {
            _dependencyGraph.ComputeMarkedNodes();
            var nodes = _dependencyGraph.MarkedNodeList;

            nodes = _fileLayoutOptimizer.ApplyProfilerGuidedMethodSort(nodes);

            using (PerfEventSource.StartStopEvents.EmittingEvents())
            {
                NodeFactory.SetMarkingComplete();
                ReadyToRunObjectWriter.EmitObject(
                    outputFile,
                    componentModule: null,
                    inputFiles: _inputFiles,
                    nodes,
                    NodeFactory,
                    generateMapFile: _generateMapFile,
                    generateMapCsvFile: _generateMapCsvFile,
                    generatePdbFile: _generatePdbFile,
                    pdbPath: _pdbPath,
                    generatePerfMapFile: _generatePerfMapFile,
                    perfMapPath: _perfMapPath,
                    perfMapFormatVersion: _perfMapFormatVersion,
                    generateProfileFile: _generateProfileFile,
                    callChainProfile: _profileData.CallChainProfile,
                    _customPESectionAlignment);
                CompilationModuleGroup moduleGroup = _nodeFactory.CompilationModuleGroup;

                if (moduleGroup.IsCompositeBuildMode)
                {
                    // In composite mode with standalone MSIL we rewrite all input MSIL assemblies to the
                    // output folder, adding a formal R2R header to them with forwarding information to
                    // the composite executable.
                    string outputDirectory = Path.GetDirectoryName(outputFile);
                    string ownerExecutableName = Path.GetFileName(outputFile);
                    foreach (string inputFile in _inputFiles)
                    {
                        string relativeMsilPath = Path.GetRelativePath(_compositeRootPath, inputFile);
                        if (relativeMsilPath == inputFile || relativeMsilPath.StartsWith(s_folderUpPrefix, StringComparison.Ordinal))
                        {
                            // Input file not under the composite root, emit to root output folder
                            relativeMsilPath = Path.GetFileName(inputFile);
                        }
                        string standaloneMsilOutputFile = Path.Combine(outputDirectory, relativeMsilPath);
                        RewriteComponentFile(inputFile: inputFile, outputFile: standaloneMsilOutputFile, ownerExecutableName: ownerExecutableName);
                    }
                }
            }
        }

        private void RewriteComponentFile(string inputFile, string outputFile, string ownerExecutableName)
        {
            EcmaModule inputModule = NodeFactory.TypeSystemContext.GetModuleFromPath(inputFile);

            Directory.CreateDirectory(Path.GetDirectoryName(outputFile));

            ReadyToRunFlags flags =
                ReadyToRunFlags.READYTORUN_FLAG_Component |
                ReadyToRunFlags.READYTORUN_FLAG_NonSharedPInvokeStubs;

            if (inputModule.IsPlatformNeutral)
            {
                flags |= ReadyToRunFlags.READYTORUN_FLAG_PlatformNeutralSource;
            }

            CopiedCorHeaderNode copiedCorHeader = new CopiedCorHeaderNode(inputModule);
            // Re-written components shouldn't have any additional diagnostic information - only information about the forwards.
            // Even with all of this, we might be modifying the image in a silly manner - adding a directory when if didn't have one.
            DebugDirectoryNode debugDirectory = new DebugDirectoryNode(inputModule, outputFile, shouldAddNiPdb: false, shouldGeneratePerfmap: false);
            NodeFactory componentFactory = new NodeFactory(
                _nodeFactory.TypeSystemContext,
                _nodeFactory.CompilationModuleGroup,
                null,
                _nodeFactory.NameMangler,
                copiedCorHeader,
                debugDirectory,
                win32Resources: new Win32Resources.ResourceData(inputModule),
                flags,
                _nodeFactory.ImageBase);

            IComparer<DependencyNodeCore<NodeFactory>> comparer = new SortableDependencyNode.ObjectNodeComparer(CompilerComparer.Instance);
            DependencyAnalyzerBase<NodeFactory> componentGraph = new DependencyAnalyzer<NoLogStrategy<NodeFactory>, NodeFactory>(componentFactory, comparer);

            componentGraph.AddRoot(componentFactory.Header, "Component module R2R header");
            OwnerCompositeExecutableNode ownerExecutableNode = new OwnerCompositeExecutableNode(_nodeFactory.Target, ownerExecutableName);
            componentGraph.AddRoot(ownerExecutableNode, "Owner composite executable name");
            componentGraph.AddRoot(copiedCorHeader, "Copied COR header");
            componentGraph.AddRoot(debugDirectory, "Debug directory");
            if (componentFactory.Win32ResourcesNode != null)
            {
                componentGraph.AddRoot(componentFactory.Win32ResourcesNode, "Win32 resources");
            }
            componentGraph.ComputeMarkedNodes();
            componentFactory.Header.Add(Internal.Runtime.ReadyToRunSectionType.OwnerCompositeExecutable, ownerExecutableNode, ownerExecutableNode);
            ReadyToRunObjectWriter.EmitObject(
                outputFile,
                componentModule: inputModule,
                inputFiles: new string[] { inputFile },
                componentGraph.MarkedNodeList,
                componentFactory,
                generateMapFile: false,
                generateMapCsvFile: false,
                generatePdbFile: false,
                pdbPath: null,
                generatePerfMapFile: false,
                perfMapPath: null,
                perfMapFormatVersion: _perfMapFormatVersion,
                generateProfileFile: false,
                _profileData.CallChainProfile,
                customPESectionAlignment: 0);
        }

        public override void WriteDependencyLog(string outputFileName)
        {
            using (FileStream dgmlOutput = new FileStream(outputFileName, FileMode.Create))
            {
                DgmlWriter.WriteDependencyGraphToStream(dgmlOutput, _dependencyGraph, _nodeFactory);
                dgmlOutput.Flush();
            }
        }

        private bool IsLayoutFixedInCurrentVersionBubbleInternal(TypeDesc type)
        {
            // Primitive types and enums have fixed layout
            if (type.IsPrimitive || type.IsEnum)
            {
                return true;
            }

            if (!(type is MetadataType defType))
            {
                // Non metadata backed types have layout defined in all version bubbles
                return true;
            }

            if (!NodeFactory.CompilationModuleGroup.VersionsWithModule(defType.Module))
            {
                // Valuetypes with non-versionable attribute are candidates for fixed layout. Reject the rest.
                return type is MetadataType metadataType && metadataType.IsNonVersionable();
            }

            // If the above condition passed, check that all instance fields have fixed layout as well. In particular,
            // it is important for generic types with non-versionable layout (e.g. Nullable<T>)
            foreach (var field in type.GetFields())
            {
                // Only instance fields matter here
                if (field.IsStatic)
                    continue;

                var fieldType = field.FieldType;
                if (!fieldType.IsValueType)
                    continue;

                if (!IsLayoutFixedInCurrentVersionBubble(fieldType))
                {
                    return false;
                }
            }

            return true;
        }

        public bool IsLayoutFixedInCurrentVersionBubble(TypeDesc type) =>
            _computedFixedLayoutTypes.GetOrAdd(type, (t) => IsLayoutFixedInCurrentVersionBubbleInternal(t));

        public bool IsInheritanceChainLayoutFixedInCurrentVersionBubble(TypeDesc type)
        {
            // This method is not expected to be called for value types
            Debug.Assert(!type.IsValueType);

            if (type.IsObject)
                return true;

            if (!IsLayoutFixedInCurrentVersionBubble(type))
            {
                return false;
            }

            type = type.BaseType;

            if (type != null)
            {
                // If there are multiple inexact compilation units in the layout of the type, then the exact offset
                // of a derived given field is unknown as there may or may not be alignment inserted between a type and its base
                if (CompilationModuleGroup.TypeLayoutCompilationUnits(type).HasMultipleInexactCompilationUnits)
                    return false;

                while (!type.IsObject && type != null)
                {
                    if (!IsLayoutFixedInCurrentVersionBubble(type))
                    {
                        return false;
                    }
                    type = type.BaseType;
                }
            }

            return true;
        }

        // Compilation is broken into phases which interact with dependency analysis
        // Phase 0: All compilations which are driven by our standard heuristics and dependency expansion model
        // Phase 1: A helper phase which works in tandem with the DeferredTillPhaseNode to gather work to be done in phase 2
        // Phase 2: A phase where all compilations are not allowed to add dependencies that can trigger further compilations.
        // The _finishedFirstCompilationRunInPhase2 variable works in concert some checking to ensure that we don't violate any of this model
        private bool _finishedFirstCompilationRunInPhase2 = false;

        protected override void ComputeDependencyNodeDependencies(List<DependencyNodeCore<NodeFactory>> obj)
        {
            using (PerfEventSource.StartStopEvents.JitEvents())
            {
                Action<DependencyNodeCore<NodeFactory>> compileOneMethod = (DependencyNodeCore<NodeFactory> dependency) =>
                {
                    MethodWithGCInfo methodCodeNodeNeedingCode = dependency as MethodWithGCInfo;
                    if (methodCodeNodeNeedingCode == null)
                    {
                        if (dependency is DeferredTillPhaseNode deferredPhaseNode)
                        {
                            if (Logger.IsVerbose)
                                _logger.Writer.WriteLine($"Moved to phase {_nodeFactory.CompilationCurrentPhase}");
                            deferredPhaseNode.NotifyCurrentPhase(_nodeFactory.CompilationCurrentPhase);
                            return;
                        }
                    }

                    Debug.Assert((_nodeFactory.CompilationCurrentPhase == 0) || ((_nodeFactory.CompilationCurrentPhase == 2) && !_finishedFirstCompilationRunInPhase2));

                    MethodDesc method = methodCodeNodeNeedingCode.Method;

                    if (Logger.IsVerbose)
                    {
                        string methodName = method.ToString();
                        Logger.Writer.WriteLine("Compiling " + methodName);
                    }

                    if (_printReproInstructions != null)
                    {
                        Logger.Writer.WriteLine($"Single method repro args:{_printReproInstructions(method)}");
                    }

                    try
                    {
                        using (PerfEventSource.StartStopEvents.JitMethodEvents())
                        {
                            // Create only 1 CorInfoImpl per thread.
                            // This allows SuperPMI to rely on non-reuse of handles in ObjectToHandle
                            CorInfoImpl corInfoImpl = _corInfoImpls.GetValue(Thread.CurrentThread, thread => new CorInfoImpl(this));
                            corInfoImpl.CompileMethod(methodCodeNodeNeedingCode, Logger);
                        }
                    }
                    catch (TypeSystemException ex)
                    {
                        // If compilation fails, don't emit code for this method. It will be Jitted at runtime
                        if (Logger.IsVerbose)
                            Logger.Writer.WriteLine($"Warning: Method `{method}` was not compiled because: {ex.Message}");
                    }
                    catch (RequiresRuntimeJitException ex)
                    {
                        if (Logger.IsVerbose)
                            Logger.Writer.WriteLine($"Info: Method `{method}` was not compiled because `{ex.Message}` requires runtime JIT");
                    }
                    catch (CodeGenerationFailedException ex) when (_resilient)
                    {
                        if (Logger.IsVerbose)
                            Logger.Writer.WriteLine($"Warning: Method `{method}` was not compiled because `{ex.Message}` requires runtime JIT");
                    }
                };

                // Use only main thread to compile if parallelism is 1. This allows SuperPMI to rely on non-reuse of handles in ObjectToHandle
                if (Logger.IsVerbose)
                    Logger.Writer.WriteLine($"Processing {obj.Count} dependencies");
                if (_parallelism == 1)
                {
                    foreach (var dependency in obj)
                        compileOneMethod(dependency);
                }
                else
                {
                    ParallelOptions options = new ParallelOptions
                    {
                        MaxDegreeOfParallelism = _parallelism
                    };

                    Parallel.ForEach(obj, options, compileOneMethod);
                }
            }

            if (_methodILCache.Count > 1000)
            {
                _methodILCache = new ILCache(_methodILCache.ILProvider, NodeFactory.CompilationModuleGroup);
            }

            if (_nodeFactory.CompilationCurrentPhase == 2)
            {
                _finishedFirstCompilationRunInPhase2 = true;
            }
        }

        public ISymbolNode GetFieldRvaData(FieldDesc field) => NodeFactory.CopiedFieldRva(field);

        public override void Dispose()
        {
            _corInfoImpls?.Clear();
        }
    }
}
