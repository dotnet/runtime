// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata.Ecma335;

using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;
using ILCompiler.ObjectWriter;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
{
    public sealed partial class RyuJitCompilation
    {
        private sealed class IncrementalCompilationSession : IDisposable
        {
            private readonly RyuJitCompilation _owner;
            private readonly IncrementalAssemblyBaseline _assemblyBaseline;
            private readonly IncrementalBodyUpdate[] _updates;
            private readonly List<MethodCodeNode> _candidates;
            private readonly Dictionary<MethodCodeNode, MethodBaseline> _methodBaselines;
            private readonly byte[] _configurationHash;
            private IncrementalObjectBaseline _objectBaseline;
            private IncrementalBodyUpdate _previousUpdate;
            private SessionState _state;

            private IncrementalCompilationSession(
                RyuJitCompilation owner,
                IncrementalAssemblyBaseline assemblyBaseline,
                IncrementalBodyUpdate[] updates,
                List<MethodCodeNode> candidates,
                Dictionary<MethodCodeNode, MethodBaseline> methodBaselines,
                byte[] configurationHash,
                IncrementalObjectLayout layout)
            {
                _owner = owner;
                _assemblyBaseline = assemblyBaseline;
                _updates = updates;
                _candidates = candidates;
                _methodBaselines = methodBaselines;
                _configurationHash = configurationHash;
                Layout = layout;
                _state = SessionState.Prepared;
            }

            internal IncrementalObjectLayout Layout { get; }

            internal static bool TryPrepare(
                RyuJitCompilation owner,
                string baselineObjectPath,
                IReadOnlyCollection<DependencyNodeCore<NodeFactory>> nodes,
                ObjectWritingOptions objectWritingOptions,
                ObjectDumper dumper,
                out IncrementalCompilationSession session,
                out string reason)
            {
                session = null;
                reason = null;
                if (objectWritingOptions != ObjectWritingOptions.GenerateUnwindInfo)
                {
                    reason = "unsupported-object-writing-options";
                    return false;
                }
                if (dumper is not null)
                {
                    reason = "object-dump-output-is-unsupported";
                    return false;
                }

                CompilerTypeSystemContext context = owner.TypeSystemContext;
                if (context.InputFilePaths.Count != 1)
                {
                    reason = "single-primary-input-required";
                    return false;
                }

                string inputSimpleName = null;
                string inputPath = null;
                foreach (KeyValuePair<string, string> input in context.InputFilePaths)
                {
                    inputSimpleName = input.Key;
                    inputPath = Path.GetFullPath(input.Value);
                }

                EcmaModule inputModule = context.GetModuleForSimpleName(inputSimpleName);
                if (!IncrementalAssemblyBaseline.TryCreate(
                    inputModule,
                    inputPath,
                    out IncrementalAssemblyBaseline assemblyBaseline,
                    out reason))
                {
                    return false;
                }
                if (!IncrementalCompilationFingerprint.TryCreate(
                    context,
                    owner.InstructionSetSupport,
                    owner._incrementalConfigurationDescription,
                    out byte[] configurationHash,
                    out reason))
                {
                    return false;
                }

                string baselineOutput = Path.GetFullPath(baselineObjectPath);
                string finalBaselineOutput = baselineOutput.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase) ?
                    baselineOutput.Substring(0, baselineOutput.Length - ".tmp".Length) :
                    baselineOutput;
                var updateObjects = new IncrementalBodyUpdate[owner._incrementalOptions.Updates.Length];
                var changedTokens = new HashSet<int>();
                for (int i = 0; i < updateObjects.Length; i++)
                {
                    IncrementalUpdateRequest request = owner._incrementalOptions.Updates[i];
                    string updatedAssembly = Path.GetFullPath(request.UpdatedAssemblyPath);
                    string outputObject = Path.GetFullPath(request.OutputObjectPath);
                    if (string.Equals(updatedAssembly, inputPath, StringComparison.OrdinalIgnoreCase))
                    {
                        reason = "updated-assembly-must-not-be-the-baseline-input";
                        return false;
                    }
                    if (string.Equals(outputObject, baselineOutput, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(outputObject, finalBaselineOutput, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(outputObject, inputPath, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(outputObject, updatedAssembly, StringComparison.OrdinalIgnoreCase))
                    {
                        reason = "incremental-output-path-collides-with-an-input-or-baseline-output";
                        return false;
                    }
                    if (File.Exists(outputObject))
                    {
                        reason = $"incremental-output-already-exists:{outputObject}";
                        return false;
                    }

                    try
                    {
                        if (!IncrementalBodyUpdate.TryCreate(
                            owner._incrementalBaseILProvider,
                            assemblyBaseline,
                            updatedAssembly,
                            allowUnchangedTarget: i != 0,
                            out IncrementalBodyUpdate update,
                            out reason))
                        {
                            return false;
                        }

                        updateObjects[i] = update;
                        foreach (int token in update.ChangedMethodTokens)
                            changedTokens.Add(token);
                    }
                    catch (BadImageFormatException)
                    {
                        reason = $"invalid-updated-assembly:{updatedAssembly}";
                        return false;
                    }
                }

                var candidateByToken = new Dictionary<int, MethodCodeNode>();
                foreach (DependencyNodeCore<NodeFactory> node in nodes)
                {
                    if (node is not MethodCodeNode methodCodeNode ||
                        methodCodeNode.Method.GetTypicalMethodDefinition() is not EcmaMethod ecmaMethod ||
                        ecmaMethod.Module != inputModule)
                    {
                        continue;
                    }

                    int token = MetadataTokens.GetToken(ecmaMethod.Handle);
                    if (!changedTokens.Contains(token))
                        continue;

                    if (!candidateByToken.TryAdd(token, methodCodeNode))
                    {
                        reason = $"changed-method-has-multiple-code-nodes:{token:X8}";
                        return false;
                    }
                }

                if (candidateByToken.Count != changedTokens.Count)
                {
                    reason = $"changed-method-node-count-mismatch:{changedTokens.Count}:{candidateByToken.Count}";
                    return false;
                }

                var candidates = new List<MethodCodeNode>(candidateByToken.Count);
                var baselines = new Dictionary<MethodCodeNode, MethodBaseline>(candidateByToken.Count);
                foreach (KeyValuePair<int, MethodCodeNode> pair in candidateByToken)
                {
                    MethodCodeNode candidate = pair.Value;
                    MethodDesc method = candidate.Method;
                    if (!candidate.Marked ||
                        method.HasInstantiation ||
                        method.OwningType.HasInstantiation ||
                        candidate.IsSpecialUnboxingThunk ||
                        candidate.HasConditionalStaticDependencies ||
                        candidate.EHInfo is not null ||
                        ((RyuJitNodeFactory)owner.NodeFactory).CanFoldMethodBodyForIncrementalCompilation(method))
                    {
                        reason = $"changed-method-node-is-not-eligible:{method}";
                        return false;
                    }

                    for (int i = 0; i < updateObjects.Length; i++)
                    {
                        if (updateObjects[i].IsChangedMethod(method) &&
                            !updateObjects[i].CanOverlayChangedMethod(method, out reason))
                        {
                            reason = $"{reason}:{method}";
                            return false;
                        }
                    }

                    if (!MethodBaseline.TryCapture(candidate, owner.NodeFactory, out MethodBaseline baseline, out reason))
                    {
                        reason = $"{reason}:{method}";
                        return false;
                    }

                    candidates.Add(candidate);
                    baselines.Add(candidate, baseline);
                }

                var objectNodes = new List<ObjectNode>(candidates.Count);
                foreach (MethodCodeNode candidate in candidates)
                    objectNodes.Add(candidate);

                session = new IncrementalCompilationSession(
                    owner,
                    assemblyBaseline,
                    updateObjects,
                    candidates,
                    baselines,
                    configurationHash,
                    new IncrementalObjectLayout(objectNodes));
                return true;
            }

            internal bool TryAttachBaseline(
                string baselineObjectPath,
                long emittedObjectLength,
                byte[] emittedObjectHash,
                out string reason)
            {
                if (_state != SessionState.Prepared)
                {
                    reason = "incremental-session-is-not-prepared";
                    return false;
                }

                if (!IncrementalObjectBaseline.TryOpenLocked(
                    baselineObjectPath,
                    Layout,
                    emittedObjectLength,
                    emittedObjectHash,
                    _assemblyBaseline.ImageHash,
                    _configurationHash,
                    out _objectBaseline,
                    out reason))
                {
                    return false;
                }

                _state = SessionState.Ready;
                return true;
            }

            internal IncrementalUpdateResult EmitUpdate(
                int updateIndex,
                out IncrementalStagedObject stagedObject)
            {
                stagedObject = null;
                if (_state != SessionState.Ready)
                {
                    return Failure("incremental-session-is-not-ready", 0, 0);
                }
                if ((uint)updateIndex >= (uint)_updates.Length)
                    return Failure("incremental-update-index-is-invalid", 0, 0);

                IncrementalBodyUpdate update = _updates[updateIndex];
                HashSet<int> dirtyTokens = IncrementalBodyUpdate.GetAffectedMethodTokens(
                    update.ChangedMethodTokens,
                    _previousUpdate?.ChangedMethodTokens);
                var dirtyMethods = new List<MethodCodeNode>();
                foreach (MethodCodeNode candidate in _candidates)
                {
                    if (update.TryGetTargetMethodToken(candidate.Method, out _, out int token) &&
                        dirtyTokens.Contains(token))
                    {
                        dirtyMethods.Add(candidate);
                    }
                }

                int expectedCount = dirtyTokens.Count;
                if (dirtyMethods.Count != expectedCount)
                {
                    return Failure(
                        $"changed-method-node-count-mismatch:{expectedCount}:{dirtyMethods.Count}",
                        update.ChangedMethodCount,
                        dirtyMethods.Count);
                }

                if (File.Exists(_owner._incrementalOptions.Updates[updateIndex].OutputObjectPath))
                {
                    return Failure(
                        "incremental-output-already-exists",
                        update.ChangedMethodCount,
                        dirtyMethods.Count);
                }

                if (!IncrementalCompilationFingerprint.TryCreate(
                    _owner.TypeSystemContext,
                    _owner.InstructionSetSupport,
                    _owner._incrementalConfigurationDescription,
                    out byte[] currentConfigurationHash,
                    out string reason))
                {
                    return Failure(reason, update.ChangedMethodCount, dirtyMethods.Count);
                }
                if (!System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                    _configurationHash,
                    currentConfigurationHash))
                {
                    return Failure(
                        "compilation-configuration-changed",
                        update.ChangedMethodCount,
                        dirtyMethods.Count);
                }

                _state = SessionState.Updating;
                try
                {
                    _owner._incrementalCurrentILProvider = update;
                    foreach (MethodCodeNode method in dirtyMethods)
                        method.ResetForIncrementalCompilation();

                    _owner.CompileSingleThreaded(dirtyMethods);
                    foreach (MethodCodeNode method in dirtyMethods)
                    {
                        if (!_methodBaselines[method].Matches(method, _owner.NodeFactory, out reason))
                        {
                            return PoisonedFailure(
                                $"incremental-method-state-changed:{method.Method}:{reason}",
                                update.ChangedMethodCount,
                                dirtyMethods.Count);
                        }
                    }

                    var objectNodes = new List<ObjectNode>(dirtyMethods.Count);
                    foreach (MethodCodeNode method in dirtyMethods)
                        objectNodes.Add(method);

                    if (!_objectBaseline.TryStagePatchedObject(
                        _owner._incrementalOptions.Updates[updateIndex].OutputObjectPath,
                        objectNodes,
                        _owner.NodeFactory,
                        _assemblyBaseline.ImageHash,
                        currentConfigurationHash,
                        out stagedObject,
                        out long patchedByteCount,
                        out reason))
                    {
                        return PoisonedFailure(
                            $"fast-object-patch-rejected:{reason}",
                            update.ChangedMethodCount,
                            dirtyMethods.Count);
                    }

                    _previousUpdate = update;
                    _state = SessionState.Ready;
                    return new IncrementalUpdateResult(
                        succeeded: true,
                        reason: null,
                        update.ChangedMethodCount,
                        dirtyMethods.Count,
                        patchedByteCount);
                }
                catch
                {
                    _state = SessionState.Poisoned;
                    throw;
                }
                finally
                {
                    _owner._incrementalCurrentILProvider = null;
                }
            }

            internal void Poison()
            {
                _owner._incrementalCurrentILProvider = null;
                if (_state != SessionState.Disposed)
                    _state = SessionState.Poisoned;
            }

            public void Dispose()
            {
                _owner._incrementalCurrentILProvider = null;
                _objectBaseline?.Dispose();
                _state = SessionState.Disposed;
            }

            private IncrementalUpdateResult Failure(
                string reason,
                int changedMethodCount,
                int recompiledMethodCount)
            {
                return new IncrementalUpdateResult(
                    succeeded: false,
                    reason,
                    changedMethodCount,
                    recompiledMethodCount,
                    patchedByteCount: 0);
            }

            private IncrementalUpdateResult PoisonedFailure(
                string reason,
                int changedMethodCount,
                int recompiledMethodCount)
            {
                _state = SessionState.Poisoned;
                return Failure(reason, changedMethodCount, recompiledMethodCount);
            }

            private enum SessionState
            {
                Prepared,
                Ready,
                Updating,
                Poisoned,
                Disposed,
            }
        }

        private sealed class MethodBaseline
        {
            private readonly IncrementalDependencyEntry[] _staticDependencies;
            private readonly IncrementalConditionalDependencyEntry[] _conditionalDependencies;
            private readonly IncrementalCodeState _codeState;

            private MethodBaseline(
                IncrementalDependencyEntry[] staticDependencies,
                IncrementalConditionalDependencyEntry[] conditionalDependencies,
                in IncrementalCodeState codeState)
            {
                _staticDependencies = staticDependencies;
                _conditionalDependencies = conditionalDependencies;
                _codeState = codeState;
            }

            internal static bool TryCapture(
                MethodCodeNode node,
                NodeFactory factory,
                out MethodBaseline baseline,
                out string reason)
            {
                baseline = null;
                if (!node.Marked)
                {
                    reason = "method-node-is-not-marked";
                    return false;
                }

                var staticDependencies = new List<IncrementalDependencyEntry>();
                foreach (DependencyNodeCore<NodeFactory>.DependencyListEntry dependency in
                    node.GetStaticDependencies(factory))
                {
                    if (!dependency.Node.Marked)
                    {
                        reason = $"unmarked-static-dependency:{staticDependencies.Count}";
                        return false;
                    }

                    staticDependencies.Add(new IncrementalDependencyEntry(
                        dependency.Node,
                        dependency.Reason,
                        dependency.Node.Marked));
                }

                var conditionalDependencies = new List<IncrementalConditionalDependencyEntry>();
                foreach (DependencyNodeCore<NodeFactory>.CombinedDependencyListEntry dependency in
                    node.GetConditionalStaticDependencies(factory))
                {
                    if (!dependency.Node.Marked ||
                        (dependency.OtherReasonNode is not null && !dependency.OtherReasonNode.Marked))
                    {
                        reason = $"unmarked-conditional-dependency:{conditionalDependencies.Count}";
                        return false;
                    }

                    conditionalDependencies.Add(new IncrementalConditionalDependencyEntry(
                        dependency.Node,
                        dependency.OtherReasonNode,
                        dependency.Reason,
                        dependency.Node.Marked,
                        dependency.OtherReasonNode?.Marked ?? true));
                }

                IncrementalCodeState state = new IncrementalCodeState(node);
                baseline = new MethodBaseline(
                    staticDependencies.ToArray(),
                    conditionalDependencies.ToArray(),
                    state);
                reason = null;
                return true;
            }

            internal bool Matches(MethodCodeNode node, NodeFactory factory, out string reason)
            {
                if (!node.Marked)
                {
                    reason = "method-node-is-not-marked";
                    return false;
                }

                var staticDependencies = new List<IncrementalDependencyEntry>();
                foreach (DependencyNodeCore<NodeFactory>.DependencyListEntry dependency in
                    node.GetStaticDependencies(factory))
                {
                    staticDependencies.Add(new IncrementalDependencyEntry(
                        dependency.Node,
                        dependency.Reason,
                        dependency.Node.Marked));
                }

                var conditionalDependencies = new List<IncrementalConditionalDependencyEntry>();
                foreach (DependencyNodeCore<NodeFactory>.CombinedDependencyListEntry dependency in
                    node.GetConditionalStaticDependencies(factory))
                {
                    conditionalDependencies.Add(new IncrementalConditionalDependencyEntry(
                        dependency.Node,
                        dependency.OtherReasonNode,
                        dependency.Reason,
                        dependency.Node.Marked,
                        dependency.OtherReasonNode?.Marked ?? true));
                }

                if (!IncrementalDependencyValidator.Matches(
                    _staticDependencies,
                    _conditionalDependencies,
                    staticDependencies,
                    conditionalDependencies,
                    out reason))
                {
                    return false;
                }

                return IncrementalCodeStateValidator.Matches(_codeState, node, out reason);
            }
        }
    }

    internal sealed class IncrementalCompilationException : Exception
    {
        internal IncrementalCompilationException(string reason)
            : base($"Incremental compilation requires a clean compilation: {reason}")
        {
            Reason = reason;
            HResult = IncrementalFailureContract.FailureHResult;
        }

        internal string Reason { get; }
    }
}
