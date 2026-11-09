// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysis.ReadyToRun;
using ILCompiler.DependencyAnalysisFramework;

using Internal.IL;
using Internal.JitInterface;
using Internal.TypeSystem;

namespace ILCompiler;

internal readonly struct DeferredWasmMethodFinalizationInfo(int level, bool isEager = false)
{
    public int Level { get; } = level;
    public bool IsEager { get; } = isEager;
}

// #DeferredWasmCallFinalization
//
// Own the post-codegen processing of deferred calls. The JIT generates each
// method before dependency analysis quiesces; the finalization plan orders
// rewriting and activation-fixup minimization, not another compilation pass.
//
// Finalize the startup/helper eager closure first, then relevel and finalize
// the remaining methods. Publish direct-call eligibility only after an entire
// level is finalized: callees at lower levels have complete activation
// guarantees, while calls within a recursive component remain indirect.
//
// _pendingMethods retains raw debug data and import-time activation tokens
// until rewriting. _finalizedMethods records the level and eager status used
// by call selection. _targets interns deferred call nodes across JIT workers,
// which also populate distinct MethodData entries. Registration and
// finalization are serialized around those workers. After finalization,
// promote eager requirements, commit import placement, and
// revisit method dependencies to publish the chosen calls and imports.
// See #DeferredWasmCallRewriting and #PrecodeFixupMinimization.
internal sealed class DeferredWasmCallProcessor
{
    private const int DefaultMaxDirectCallActivationDepth = 4;

    private readonly ReadyToRunCodegenCompilation _compilation;
    private readonly DependencyAnalyzerBase<NodeFactory> _dependencyGraph;
    private readonly DeferredWasmCallRewriter _rewriter;
    private readonly Dictionary<MethodDesc, DeferredWasmMethodFinalizationInfo> _finalizedMethods = new();
    private readonly HashSet<MethodDesc> _startupEagerMethods;
    private readonly ConcurrentDictionary<(MethodWithToken Method, WasmTypeNode Signature), DeferredWasmCallNode> _targets = new();
    private readonly Dictionary<MethodWithGCInfo, MethodData> _pendingMethods = new();
    private readonly HashSet<Import> _fixupScratch = new();
    private bool _finalizingEagerMethods;
    private bool _policyFrozen;

    private sealed class MethodData
    {
        internal OffsetMapping[] Locations;
        internal NativeVarInfo[] Variables;
        internal Dictionary<MethodWithGCInfo, DelayLoadMethodImport> ActivationImports;
    }

    private NodeFactory NodeFactory => _compilation.NodeFactory;
    private Logger Logger => _compilation.Logger;

    internal DeferredWasmCallProcessor(
        ReadyToRunCodegenCompilation compilation, DependencyAnalyzerBase<NodeFactory> dependencyGraph)
    {
        _compilation = compilation;
        _dependencyGraph = dependencyGraph;
        _rewriter = new DeferredWasmCallRewriter(compilation);
        _startupEagerMethods = GetStartupEagerMethods();
        _dependencyGraph.DependencyAnalysisQuiesced += FinalizeCalls;
        foreach (MethodDesc method in _startupEagerMethods)
        {
            _dependencyGraph.AddRoot(NodeFactory.CompiledMethodNode(method), "Native-facing startup method");
        }
    }

    // Register methods before their JIT workers publish the data needed for
    // deferred finalization. New methods after policy selection indicate a
    // missed dependency, but can still be rewritten using the frozen policy.
    internal void QueueMethods(List<DependencyNodeCore<NodeFactory>> dependencies)
    {
        foreach (DependencyNodeCore<NodeFactory> dependency in dependencies)
        {
            if (dependency is MethodWithGCInfo method)
            {
                Debug.Assert(!_policyFrozen,
                    $"Method {method.Method} requires compilation after the deferred Wasm call policy was frozen.");
                _pendingMethods.TryAdd(method, new MethodData());
            }
        }
    }

    // Retain unencoded debug offsets and the original activation tokens until
    // call rewriting has established the final layout and activation paths.
    internal void InitializeMethodData(
        MethodWithGCInfo method, OffsetMapping[] locations, NativeVarInfo[] variables,
        Dictionary<MethodWithGCInfo, DelayLoadMethodImport> activationImports)
    {
        MethodData data = _pendingMethods[method];
        data.Locations = locations;
        data.Variables = variables;
        data.ActivationImports = activationImports;
    }

    // Preserve the ordinary import when it cannot identify the same compiled
    // target; otherwise share a deferred node for this token and call signature.
    internal ISymbolNode GetCall(MethodWithToken method, ISymbolNode fallback, WasmTypeNode signature = null)
    {
        if (fallback is not DelayLoadMethodImport { MethodCodeNode: not null } import ||
            import.MethodCodeNode.Method != method.Method.GetCanonMethodTarget(CanonicalFormKind.Specific))
        {
            return fallback;
        }

        return _targets.GetOrAdd(
            (method, signature), static (key, fallback) => new DeferredWasmCallNode(fallback, key.Signature), import);
    }

    // Require finalized activation guarantees and bound non-eager activation
    // depth. Eager methods are prepared at module startup instead.
    internal bool CanUseDirectCall(MethodDesc method, out DeferredWasmMethodFinalizationInfo finalizationInfo)
    {
        finalizationInfo = default;
        return NodeFactory.Target.Architecture == TargetArchitecture.Wasm32 &&
            _finalizedMethods.TryGetValue(method, out finalizationInfo) &&
            (finalizationInfo.IsEager || _finalizingEagerMethods ||
                (!method.IsNoInlining && !method.IsNoOptimization &&
                    // Include the caller and the level-zero leaf in the activation depth.
                    (finalizationInfo.Level < DefaultMaxDirectCallActivationDepth - 1 ||
                        IsDirectCallDepthExempt(method.OwningType))));
    }

    // Build the post-codegen schedule once. Later quiescence notifications only
    // finalize newly discovered methods and publish their chosen dependencies.
    private void FinalizeCalls()
    {
        if (!_policyFrozen)
        {
            var methods = new List<MethodWithGCInfo>();
            foreach (MethodWithGCInfo method in _pendingMethods.Keys)
            {
                if (!method.IsEmpty)
                {
                    methods.Add(method);
                }
            }

            DeferredWasmCallFinalizationPlan plan = DeferredWasmCallFinalizationPlan.Create(methods, NodeFactory);
            FinalizePlan(plan);
            _policyFrozen = true;
            NodeFactory.CompleteDeferredImportPlacement();
        }
        foreach (MethodWithGCInfo method in _pendingMethods.Keys)
        {
            if (_pendingMethods[method] is not null)
            {
                FinalizeMethod(method);
            }
            _dependencyGraph.RevisitStaticDependencies(method);
        }
        _pendingMethods.Clear();
    }

    // Establish eager activation first, then finalize the remaining methods
    // relative to that available frontier. Neither stage regenerates JIT code.
    private void FinalizePlan(DeferredWasmCallFinalizationPlan plan)
    {
        var methodNodes = new List<MethodWithGCInfo>();
        var managedHelpers = new List<MethodWithGCInfo>();
        var finalizedMethods = new HashSet<MethodDesc>();
        var eagerRoots = new HashSet<MethodDesc>(_startupEagerMethods);

        for (int level = 0; level < plan.LevelCount; level++)
        {
            IReadOnlyList<MethodDesc> methodsAtLevel = plan.GetMethodsAtLevel(level);
            foreach (MethodDesc method in methodsAtLevel)
            {
                MethodWithGCInfo methodNode = NodeFactory.CompiledMethodNode(method);
                methodNodes.Add(methodNode);
                if (methodNode.IsJitHelper)
                {
                    managedHelpers.Add(methodNode);
                    eagerRoots.Add(method);
                }
            }
        }

        _compilation.EnsureDirectManagedHelpersInitialized(managedHelpers);

        DeferredWasmCallFinalizationPlan eagerPlan = plan.CreateEagerPlan(eagerRoots);
        _finalizingEagerMethods = true;
        FinalizeMethods(eagerPlan, isEager: true);
        _finalizingEagerMethods = false;

        // Only successfully finalized eager methods precede non-eager level zero.
        var finalizedEagerMethods = new HashSet<MethodDesc>(finalizedMethods);
        DeferredWasmCallFinalizationPlan nonEagerPlan = plan.CreateNonEagerPlan(finalizedEagerMethods);
        FinalizeMethods(nonEagerPlan, isEager: false);

        _compilation.ValidateManagedHelpers(managedHelpers);
        PromoteModuleEagerFixups(methodNodes, finalizedMethods, finalizedEagerMethods);

        // Publish a level only after every member has complete code and fixups,
        // preventing direct activation edges within a recursive component.
        void FinalizeMethods(DeferredWasmCallFinalizationPlan stagePlan, bool isEager)
        {
            for (int level = 0; level < stagePlan.LevelCount; level++)
            {
                IReadOnlyList<MethodDesc> methodsAtLevel = stagePlan.GetMethodsAtLevel(level);
                if (methodsAtLevel.Count == 0)
                {
                    continue;
                }

                var methodsToFinalize = new List<MethodWithGCInfo>(methodsAtLevel.Count);
                foreach (MethodDesc method in methodsAtLevel)
                {
                    MethodWithGCInfo methodNode = NodeFactory.CompiledMethodNode(method);
                    if (_pendingMethods[methodNode] is not null)
                    {
                        methodsToFinalize.Add(methodNode);
                    }
                }

                string stage = isEager ? "eager" : "non-eager";
                if (Logger.IsVerbose)
                {
                    Logger.Writer.WriteLine($"Finalizing deferred Wasm calls {stage} level {level} with {methodsAtLevel.Count} methods");
                }

                foreach (MethodWithGCInfo method in methodsToFinalize)
                {
                    FinalizeMethod(method);
                }

                int finalizedCount = 0;
                foreach (MethodWithGCInfo method in methodsToFinalize)
                {
                    if (!method.IsEmpty)
                    {
                        finalizedMethods.Add(method.Method);
                        _finalizedMethods.Add(method.Method, new DeferredWasmMethodFinalizationInfo(level, isEager));
                        finalizedCount++;
                    }
                }

                if (Logger.IsVerbose)
                {
                    Logger.Writer.WriteLine($"Finalized deferred Wasm calls {stage} level {level}: {finalizedCount} finalized methods");
                }
            }
        }
    }

    // Rewrite before minimizing fixups, then release raw per-method data while
    // retaining the pending key for dependency publication.
    private void FinalizeMethod(MethodWithGCInfo method)
    {
        if (!method.IsEmpty)
        {
            MethodData data = _pendingMethods[method];
            _rewriter.Rewrite(method, data.Locations, data.Variables, data.ActivationImports);
            method.FinalizePrecodeFixups(scratch: _fixupScratch);
        }
        _pendingMethods[method] = null;
    }

    // These native-facing CoreLib entrypoints must be ready before ordinary
    // managed calls can establish activation through their precodes.
    private HashSet<MethodDesc> GetStartupEagerMethods()
    {
        var methods = new HashSet<MethodDesc>();
        AddMethod("System"u8, "AppContext"u8, "Setup");
        AddMethod("System.Diagnostics.Tracing"u8, "EventSource"u8, "InitializeDefaultEventSources");
        AddMethod("System"u8, "Environment"u8, "InitializeCommandLineArgs");
        AddMethod("System"u8, "Environment"u8, "CallEntryPoint");
        AddMethod("System"u8, "StartupHookProvider"u8, "ManagedStartup");
        return methods;

        void AddMethod(ReadOnlySpan<byte> namespaceName, ReadOnlySpan<byte> typeName, string methodName)
        {
            MetadataType type = _compilation.TypeSystemContext.SystemModule.GetKnownType(namespaceName, typeName);
            MethodDesc foundMethod = null;
            foreach (MethodDesc method in type.GetMethods())
            {
                if (method.GetName() == methodName && method.IsUnmanagedCallersOnly)
                {
                    Debug.Assert(foundMethod is null);
                    foundMethod = method;
                }
            }

            Debug.Assert(foundMethod is not null);
            if (_compilation.CompilationModuleGroup.ContainsMethodBody(foundMethod, unboxingStub: false))
            {
                methods.Add(foundMethod);
            }
        }
    }

    // Move the eager closure's activation requirements to module startup, then
    // remove those cells from every method's precode. See #PrecodeFixupMinimization.
    private void PromoteModuleEagerFixups(
        List<MethodWithGCInfo> methods,
        HashSet<MethodDesc> finalizedMethods,
        HashSet<MethodDesc> finalizedEagerMethods)
    {
        var eagerMethodClosure = new HashSet<MethodWithGCInfo>();
        var methodsToVisit = new Stack<MethodWithGCInfo>();
        var eagerFixups = new HashSet<Import>();

        foreach (MethodDesc method in finalizedEagerMethods)
        {
            MethodWithGCInfo methodNode = NodeFactory.CompiledMethodNode(method);
            Debug.Assert(!methodNode.IsEmpty);

            MethodWithToken methodWithToken = new MethodWithToken(
                method,
                NodeFactory.Resolver.GetModuleTokenForMethod(method, allowDynamicallyCreatedReference: true, throwIfNotFound: true),
                constrainedType: null,
                unboxing: false,
                genericContextObject: null);
            _dependencyGraph.AddRoot(
                _compilation.SymbolNodeFactory.EagerReadyToRunMethodEntry(methodWithToken),
                "Eager-stage method is already activated");

            methodsToVisit.Push(methodNode);
        }

        while (methodsToVisit.TryPop(out MethodWithGCInfo method))
        {
            if (!eagerMethodClosure.Add(method))
            {
                continue;
            }

            foreach (Import fixup in method.GetGuaranteedPrecodeFixups(_fixupScratch))
            {
                eagerFixups.Add(fixup);
                // Eager helper entries also prepare their bodies, even when the
                // generated call does not contribute a direct-call edge.
                if (fixup.Signature is MethodFixupSignature { IsReadyToRunEntry: true } signature)
                {
                    MethodDesc target = signature.Method.GetCanonMethodTarget(CanonicalFormKind.Specific);
                    if (finalizedMethods.Contains(target))
                    {
                        methodsToVisit.Push(NodeFactory.CompiledMethodNode(target));
                    }
                }
            }

            for (int i = 0; i < method.CodeRelocations.Length; i++)
            {
                if (method.GetDirectCallTarget(i) is MethodWithGCInfo target)
                {
                    methodsToVisit.Push(target);
                }
            }
        }

        foreach (Import fixup in eagerFixups)
        {
            if (!fixup.Table.IsEager)
            {
                ImportSectionNode table = fixup.Signature switch
                {
                    MethodFixupSignature { RequiresEagerActivation: true } or
                        ResumptionStubEntryPointSignature or StoreMultiCallableAddrOfCodeSignature => NodeFactory.EagerMethodImports,
                    _ when fixup.Table == NodeFactory.StringImports => NodeFactory.EagerStringImports,
                    _ => NodeFactory.EagerImports,
                };
                fixup.PromoteToEager(table);
            }

            _dependencyGraph.AddRoot(fixup, "Eager method precode fixup is module-eager");
        }

        foreach (MethodWithGCInfo method in methods)
        {
            if (!method.IsEmpty)
            {
                method.ApplyModuleEagerFixups(eagerFixups);
                Debug.Assert(!eagerMethodClosure.Contains(method) || method.Fixups.Count == 0);
            }
        }
    }

    // Keep common primitive and collection operations eligible even when their
    // position in the call graph exceeds the general activation-depth bound.
    internal static bool IsDirectCallDepthExempt(TypeDesc type)
    {
        if (type.IsPrimitive || type.IsObject || type.IsString)
        {
            return true;
        }

        // Generic collection and enumeration interfaces inherit these non-generic interfaces.
        foreach (DefType interfaceType in type.GetTypeDefinition().RuntimeInterfaces)
        {
            if (interfaceType is MetadataType metadataType &&
                metadataType.Module == type.Context.SystemModule &&
                metadataType.Namespace == "System.Collections"u8 &&
                (metadataType.Name == "IEnumerable"u8 || metadataType.Name == "IEnumerator"u8))
            {
                return true;
            }
        }

        return false;
    }
}
