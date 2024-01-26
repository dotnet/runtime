// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;
using ILCompiler.ObjectWriter;
using ILLink.Shared;

using Internal.IL;
using Internal.IL.Stubs;
using Internal.TypeSystem;
using Internal.JitInterface;

namespace ILCompiler
{
    public sealed class RyuJitCompilation : Compilation
    {
        private readonly ConditionalWeakTable<Thread, CorInfoImpl> _corinfos = new ConditionalWeakTable<Thread, CorInfoImpl>();
        internal readonly RyuJitCompilationOptions _compilationOptions;
        private readonly ProfileDataManager _profileDataManager;
        private readonly MethodImportationErrorProvider _methodImportationErrorProvider;
        private readonly ReadOnlyFieldPolicy _readOnlyFieldPolicy;
        private readonly int _parallelism;

        public InstructionSetSupport InstructionSetSupport { get; }

        internal RyuJitCompilation(
            DependencyAnalyzerBase<NodeFactory> dependencyGraph,
            NodeFactory nodeFactory,
            IEnumerable<ICompilationRootProvider> roots,
            ILProvider ilProvider,
            DebugInformationProvider debugInformationProvider,
            Logger logger,
            DevirtualizationManager devirtualizationManager,
            IInliningPolicy inliningPolicy,
            InstructionSetSupport instructionSetSupport,
            ProfileDataManager profileDataManager,
            MethodImportationErrorProvider errorProvider,
            ReadOnlyFieldPolicy readOnlyFieldPolicy,
            RyuJitCompilationOptions options,
            int parallelism)
            : base(dependencyGraph, nodeFactory, roots, ilProvider, debugInformationProvider, devirtualizationManager, inliningPolicy, logger)
        {
            _compilationOptions = options;
            InstructionSetSupport = instructionSetSupport;

            _profileDataManager = profileDataManager;

            _methodImportationErrorProvider = errorProvider;

            _readOnlyFieldPolicy = readOnlyFieldPolicy;

            _parallelism = parallelism;
        }

        public ProfileDataManager ProfileData => _profileDataManager;

        public bool IsInitOnly(FieldDesc field) => _readOnlyFieldPolicy.IsReadOnly(field);

        public override IEETypeNode NecessaryTypeSymbolIfPossible(TypeDesc type)
        {
            // RyuJIT makes assumptions around the value of these symbols - in particular, it assumes
            // that type handles and type symbols have a 1:1 relationship. We therefore need to
            // make sure RyuJIT never sees a constructed and unconstructed type symbol for the
            // same type. If the type is constructable and we don't have whole program view
            // information proving that it isn't, give RyuJIT the constructed symbol even
            // though we just need the unconstructed one.
            // https://github.com/dotnet/runtimelab/issues/1128
            bool canPotentiallyConstruct = _devirtualizationManager == null
                ? true : _devirtualizationManager.CanConstructType(type);
            if (canPotentiallyConstruct)
                return _nodeFactory.MaximallyConstructableType(type);

            return _nodeFactory.NecessaryTypeSymbol(type);
        }

        public FrozenRuntimeTypeNode NecessaryRuntimeTypeIfPossible(TypeDesc type)
        {
            bool canPotentiallyConstruct = _devirtualizationManager == null
                ? true : _devirtualizationManager.CanConstructType(type);
            if (canPotentiallyConstruct)
                return _nodeFactory.SerializedMaximallyConstructableRuntimeTypeObject(type);

            return _nodeFactory.SerializedNecessaryRuntimeTypeObject(type);
        }

        protected override void CompileInternal(string outputFile, ObjectDumper dumper)
        {
            _dependencyGraph.ComputeMarkedNodes();
            var nodes = _dependencyGraph.MarkedNodeList;

            NodeFactory.SetMarkingComplete();

            ObjectWritingOptions options = default;
            if ((_compilationOptions & RyuJitCompilationOptions.UseDwarf5) != 0)
                options |= ObjectWritingOptions.UseDwarf5;

            if (_debugInformationProvider is not NullDebugInformationProvider)
                options |= ObjectWritingOptions.GenerateDebugInfo;

            if ((_compilationOptions & RyuJitCompilationOptions.ControlFlowGuardAnnotations) != 0)
                options |= ObjectWritingOptions.ControlFlowGuard;

            ObjectWriter.ObjectWriter.EmitObject(outputFile, nodes, NodeFactory, options, dumper, _logger);
        }

        protected override void ComputeDependencyNodeDependencies(List<DependencyNodeCore<NodeFactory>> obj)
        {
            // Determine the list of method we actually need to compile
            var methodsToCompile = new List<MethodCodeNode>();
            var canonicalMethodsToCompile = new HashSet<MethodDesc>();

            foreach (DependencyNodeCore<NodeFactory> dependency in obj)
            {
                var methodCodeNodeNeedingCode = dependency as MethodCodeNode;
                if (methodCodeNodeNeedingCode == null)
                {
                    // To compute dependencies of the shadow method that tracks dictionary
                    // dependencies we need to ensure there is code for the canonical method body.
                    var dependencyMethod = (ShadowConcreteMethodNode)dependency;
                    methodCodeNodeNeedingCode = (MethodCodeNode)dependencyMethod.CanonicalMethodNode;
                }

                // We might have already queued this method for compilation
                MethodDesc method = methodCodeNodeNeedingCode.Method;
                if (method.IsCanonicalMethod(CanonicalFormKind.Any)
                    && !canonicalMethodsToCompile.Add(method))
                {
                    continue;
                }

                methodsToCompile.Add(methodCodeNodeNeedingCode);
            }

            if (_parallelism == 1)
            {
                CompileSingleThreaded(methodsToCompile);
            }
            else
            {
                CompileMultiThreaded(methodsToCompile);
            }
        }
        private void CompileMultiThreaded(List<MethodCodeNode> methodsToCompile)
        {
            if (Logger.IsVerbose)
            {
                Logger.LogMessage($"Compiling {methodsToCompile.Count} methods...");
            }

            Parallel.ForEach(
                methodsToCompile,
                new ParallelOptions { MaxDegreeOfParallelism = _parallelism },
                CompileSingleMethod);
        }


        private void CompileSingleThreaded(List<MethodCodeNode> methodsToCompile)
        {
            CorInfoImpl corInfo = _corinfos.GetValue(Thread.CurrentThread, thread => new CorInfoImpl(this));

            foreach (MethodCodeNode methodCodeNodeNeedingCode in methodsToCompile)
            {
                if (Logger.IsVerbose)
                {
                    Logger.LogMessage($"Compiling {methodCodeNodeNeedingCode.Method}...");
                }

                CompileSingleMethod(corInfo, methodCodeNodeNeedingCode);
            }
        }

        private void CompileSingleMethod(MethodCodeNode methodCodeNodeNeedingCode)
        {
            CorInfoImpl corInfo = _corinfos.GetValue(Thread.CurrentThread, thread => new CorInfoImpl(this));
            CompileSingleMethod(corInfo, methodCodeNodeNeedingCode);
        }

        private void CompileSingleMethod(CorInfoImpl corInfo, MethodCodeNode methodCodeNodeNeedingCode)
        {
            MethodDesc method = methodCodeNodeNeedingCode.Method;

            TypeSystemException exception = _methodImportationErrorProvider.GetCompilationError(method);

            // If we previously failed to import the method, do not try to import it again and go
            // directly to the error path.
            if (exception == null)
            {
                try
                {
                    corInfo.CompileMethod(methodCodeNodeNeedingCode);
                }
                catch (TypeSystemException ex)
                {
                    exception = ex;
                }
            }

            if (exception != null)
            {
                // Try to compile the method again, but with a throwing method body this time.
                MethodIL throwingIL = TypeSystemThrowingILEmitter.EmitIL(method, exception);
                corInfo.CompileMethod(methodCodeNodeNeedingCode, throwingIL);

                if (exception is TypeSystemException.InvalidProgramException
                    && method.OwningType is MetadataType mdOwningType
                    && mdOwningType.HasCustomAttribute("System.Runtime.InteropServices", "ClassInterfaceAttribute"))
                {
                    Logger.LogWarning(method, DiagnosticId.COMInteropNotSupportedInFullAOT);
                }
                if ((_compilationOptions & RyuJitCompilationOptions.UseResilience) != 0)
                    Logger.LogMessage($"Method '{method}' will always throw because: {exception.Message}");
                else
                    Logger.LogError($"Method will always throw because: {exception.Message}", 1005, method, MessageSubCategory.AotAnalysis);
            }
        }
    }

    [Flags]
    public enum RyuJitCompilationOptions
    {
        MethodBodyFolding = 0x1,
        ControlFlowGuardAnnotations = 0x2,
        UseDwarf5 = 0x4,
        UseResilience = 0x8,
    }
}
