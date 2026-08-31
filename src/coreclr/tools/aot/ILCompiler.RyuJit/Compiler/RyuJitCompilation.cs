// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
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
    public sealed partial class RyuJitCompilation : Compilation
    {
        private readonly ConditionalWeakTable<Thread, CorInfoImpl> _corinfos = new ConditionalWeakTable<Thread, CorInfoImpl>();
        internal readonly RyuJitCompilationOptions _compilationOptions;
        private readonly ProfileDataManager _profileDataManager;
        private readonly FileLayoutOptimizer _fileLayoutOptimizer;
        private readonly MethodImportationErrorProvider _methodImportationErrorProvider;
        private readonly ReadOnlyFieldPolicy _readOnlyFieldPolicy;
        private readonly int _parallelism;
        private readonly ILProvider _incrementalBaseILProvider;
        private ILProvider _incrementalCurrentILProvider;
        private bool _incrementalOutputsPublished;
        private readonly IncrementalCompilationOptions _incrementalOptions;
        private readonly string _incrementalConfigurationDescription;

        public InstructionSetSupport InstructionSetSupport { get; }

        internal RyuJitCompilation(
            DependencyAnalyzerBase<NodeFactory> dependencyGraph,
            NodeFactory nodeFactory,
            IEnumerable<ICompilationRootProvider> roots,
            ILProvider ilProvider,
            DebugInformationProvider debugInformationProvider,
            Logger logger,
            IInliningPolicy inliningPolicy,
            InstructionSetSupport instructionSetSupport,
            ProfileDataManager profileDataManager,
            MethodImportationErrorProvider errorProvider,
            ReadOnlyFieldPolicy readOnlyFieldPolicy,
            RyuJitCompilationOptions options,
            MethodLayoutAlgorithm methodLayoutAlgorithm,
            FileLayoutAlgorithm fileLayoutAlgorithm,
            int parallelism,
            string orderFile,
            IncrementalCompilationOptions incrementalOptions,
            string incrementalConfigurationDescription)
            : base(dependencyGraph, nodeFactory, roots, ilProvider, debugInformationProvider, inliningPolicy, logger)
        {
            _compilationOptions = options;
            InstructionSetSupport = instructionSetSupport;

            _profileDataManager = profileDataManager;

            _methodImportationErrorProvider = errorProvider;

            _readOnlyFieldPolicy = readOnlyFieldPolicy;

            _parallelism = parallelism;

            _fileLayoutOptimizer = new FileLayoutOptimizer(logger, methodLayoutAlgorithm, fileLayoutAlgorithm, profileDataManager, nodeFactory, orderFile);
            _incrementalBaseILProvider = new BaselineILProvider(this);
            _incrementalOptions = incrementalOptions;
            _incrementalConfigurationDescription = incrementalConfigurationDescription;
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
            return GetLdTokenHelperForType(type) switch
            {
                ReadyToRunHelperId.MetadataTypeHandle => _nodeFactory.MetadataTypeSymbol(type),
                ReadyToRunHelperId.TypeHandle => _nodeFactory.MaximallyConstructableType(type),
                ReadyToRunHelperId.NecessaryTypeHandle => _nodeFactory.NecessaryTypeSymbol(type),
                _ => throw new UnreachableException()
            };
        }

        public FrozenRuntimeTypeNode NecessaryRuntimeTypeIfPossible(TypeDesc type)
        {
            return GetLdTokenHelperForType(type) switch
            {
                ReadyToRunHelperId.TypeHandle or ReadyToRunHelperId.MetadataTypeHandle => _nodeFactory.SerializedMetadataRuntimeTypeObject(type),
                ReadyToRunHelperId.NecessaryTypeHandle => _nodeFactory.SerializedNecessaryRuntimeTypeObject(type),
                _ => throw new UnreachableException()
            };
        }

        protected override void CompileInternal(string outputFile, ObjectDumper dumper)
        {
            _dependencyGraph.ComputeMarkedNodes();
            var nodes = _dependencyGraph.MarkedNodeList;

            nodes = _fileLayoutOptimizer.ApplyProfilerGuidedMethodSort(nodes);

            NodeFactory.SetMarkingComplete();

            ObjectWritingOptions options = ObjectWritingOptions.GenerateUnwindInfo;
            if ((_compilationOptions & RyuJitCompilationOptions.UseDwarf5) != 0)
                options |= ObjectWritingOptions.UseDwarf5;

            if (_debugInformationProvider is not NullDebugInformationProvider)
                options |= ObjectWritingOptions.GenerateDebugInfo;

            if ((_compilationOptions & RyuJitCompilationOptions.ControlFlowGuardAnnotations) != 0)
                options |= ObjectWritingOptions.ControlFlowGuard;

            if (_incrementalOptions is null)
            {
                ObjectWriter.ObjectWriter.EmitObject(outputFile, nodes, NodeFactory, options, dumper, _logger);
                return;
            }

            if (!IncrementalCompilationSession.TryPrepare(
                this,
                outputFile,
                nodes,
                options,
                dumper,
                out IncrementalCompilationSession session,
                out string reason))
            {
                throw new IncrementalCompilationException(reason);
            }

            using (session)
            {
                var stagedObjects = new List<IncrementalStagedObject>();
                try
                {
                    EmitIncrementalObject(
                        outputFile,
                        nodes,
                        options,
                        dumper,
                        session.Layout,
                        out long emittedObjectLength,
                        out byte[] emittedObjectHash);

                    if (!session.TryAttachBaseline(
                        outputFile,
                        emittedObjectLength,
                        emittedObjectHash,
                        out reason))
                    {
                        throw new IncrementalCompilationException(reason);
                    }

                    for (int i = 0; i < _incrementalOptions.Updates.Length; i++)
                    {
                        IncrementalUpdateResult result = session.EmitUpdate(
                            i,
                            out IncrementalStagedObject stagedObject);
                        if (!result.Succeeded)
                            throw new IncrementalCompilationException(result.Reason);
                        stagedObjects.Add(stagedObject);

                        _logger.LogMessage(
                            $"Incremental update {i + 1} staged: " +
                            $"{result.ChangedMethodCount} changed definitions, " +
                            $"{result.RecompiledMethodCount} recompiled nodes, " +
                            $"{result.PatchedByteCount} patched bytes.");
                    }

                    TypeSystemContext.LogWarnings(_logger);
                    if (_logger.HasLoggedErrors)
                    {
                        throw new IncrementalCompilationException(
                            "compiler diagnostics prevent incremental output publication");
                    }

                    foreach (IncrementalStagedObject stagedObject in stagedObjects)
                    {
                        if (!stagedObject.TryPublish(out reason))
                        {
                            session.Poison();
                            throw new IncrementalCompilationException(
                                $"incremental-output-publication-failed:{reason}");
                        }
                    }

                    _incrementalOutputsPublished = true;
                    _logger.LogMessage("All incremental outputs were published.");
                    stagedObjects.Clear();
                }
                catch (IncrementalCompilationException ex)
                {
                    session.Poison();
                    string cleanupFailure = CleanupIncrementalOutputs(stagedObjects);
                    throw cleanupFailure is null ?
                        ex :
                        new IncrementalCompilationException(
                            IncrementalObjectBaseline.AppendFailure(ex.Reason, cleanupFailure));
                }
                catch (Exception ex)
                {
                    session.Poison();
                    string cleanupFailure = CleanupIncrementalOutputs(stagedObjects);
                    if (cleanupFailure is not null)
                    {
                        throw new AggregateException(
                            ex,
                            new IOException(cleanupFailure));
                    }

                    throw;
                }
            }
        }

        public override MethodIL GetMethodIL(MethodDesc method) =>
            _incrementalCurrentILProvider?.GetMethodIL(method) ?? base.GetMethodIL(method);

        private MethodIL GetBaselineMethodIL(MethodDesc method) => base.GetMethodIL(method);

        internal bool GetIncrementalOutputPublicationStatus() =>
            _incrementalOutputsPublished;

        private void EmitIncrementalObject(
            string outputFile,
            IReadOnlyCollection<DependencyNode> nodes,
            ObjectWritingOptions options,
            ObjectDumper dumper,
            IncrementalObjectLayout layout,
            out long objectLength,
            out byte[] objectHash)
        {
            object[] arguments =
            {
                outputFile,
                nodes,
                NodeFactory,
                options,
                dumper,
                _logger,
                new Action<object, int, long, object, bool>(layout.RecordNode),
                new Action<Func<int, long, int, long?>>(layout.Complete),
                null,
                null,
            };

            try
            {
                IncrementalObjectWriterAccess.EmitMethod.Invoke(null, arguments);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }

            objectLength = (long)arguments[8];
            objectHash = (byte[])arguments[9];
        }

        private static string CleanupIncrementalOutputs(
            IReadOnlyList<IncrementalStagedObject> stagedObjects)
        {
            string reason = null;
            foreach (IncrementalStagedObject stagedObject in stagedObjects)
            {
                if (!stagedObject.TryCleanup(out string cleanupFailure))
                    reason = IncrementalObjectBaseline.AppendFailure(reason, cleanupFailure);
            }

            return reason;
        }

        private sealed class BaselineILProvider : ILProvider
        {
            private readonly RyuJitCompilation _compilation;

            internal BaselineILProvider(RyuJitCompilation compilation)
            {
                _compilation = compilation;
            }

            public override MethodIL GetMethodIL(MethodDesc method) =>
                _compilation.GetBaselineMethodIL(method);
        }

        private static class IncrementalObjectWriterAccess
        {
            // ILCompiler.Compiler and ILCompiler.RyuJit compile overlapping linked sources, so
            // InternalsVisibleTo would make duplicate internal types ambiguous. Keep this
            // experiment-only boundary internal and fail loudly if the reflected seam changes.
            internal static readonly MethodInfo EmitMethod =
                typeof(ObjectWriter.ObjectWriter).GetMethod(
                    "EmitObjectForIncrementalCompilation",
                    BindingFlags.NonPublic | BindingFlags.Static) ??
                throw new MissingMethodException(
                    typeof(ObjectWriter.ObjectWriter).FullName,
                    "EmitObjectForIncrementalCompilation");
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
                    var dependencyMethod = (ShadowMethodNode)dependency;
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

                // Try to compile the method again, but with a throwing method body this time.
                MethodIL throwingIL = TypeSystemThrowingILEmitter.EmitIL(method, exception);
                corInfo.CompileMethod(methodCodeNodeNeedingCode, throwingIL);
            }
        }
    }

    [Flags]
    public enum RyuJitCompilationOptions
    {
        ControlFlowGuardAnnotations = 0x1,
        UseDwarf5 = 0x2,
        UseResilience = 0x4,
    }
}
