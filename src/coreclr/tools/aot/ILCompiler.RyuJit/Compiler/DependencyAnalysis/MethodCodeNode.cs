// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Internal.IL;
using Internal.Text;
using Internal.TypeSystem;

using CombinedDependencyList = System.Collections.Generic.List<ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.CombinedDependencyListEntry>;

namespace ILCompiler.DependencyAnalysis
{
    [DebuggerTypeProxy(typeof(MethodCodeNodeDebugView))]
    public class MethodCodeNode : ObjectNode, IMethodBodyNode, INodeWithCodeInfo, INodeWithDebugInfo, ISymbolDefinitionNode, ISpecialUnboxThunkNode
    {
        private MethodDesc _method;
        private ObjectData _methodCode;
        private FrameInfo[] _frameInfos;
        private byte[] _gcInfo;
        private MethodExceptionHandlingInfoNode _ehInfo;
        private DebugLocInfo[] _debugLocInfos;
        private DebugVarInfo[] _debugVarInfos;
        private DebugEHClauseInfo[] _debugEHClauseInfos;
        private DependencyList _nonRelocationDependencies;
        private bool _isFoldable;
        private MethodDebugInformation _debugInfo;
        private TypeDesc[] _localTypes;

        public MethodCodeNode(MethodDesc method)
        {
            Debug.Assert(!method.IsAbstract);
            Debug.Assert(!method.IsGenericMethodDefinition && !method.OwningType.IsGenericDefinition);
            Debug.Assert(method.GetCanonMethodTarget(CanonicalFormKind.Specific) == method);
            _method = method;
        }

        public void SetCode(ObjectData data, bool isFoldable)
        {
            Debug.Assert(_methodCode == null);
            _methodCode = data;
            _isFoldable = isFoldable;
        }

        public MethodDesc Method =>  _method;

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override ObjectNodeSection Section
        {
            get
            {
                return _method.Context.Target.IsWindows ?
                    (_isFoldable ? ObjectNodeSection.FoldableManagedCodeWindowsContentSection : ObjectNodeSection.ManagedCodeWindowsContentSection) :
                    (_isFoldable ? ObjectNodeSection.FoldableManagedCodeUnixContentSection : ObjectNodeSection.ManagedCodeUnixContentSection);
            }
        }
        
        public override bool StaticDependenciesAreComputed => _methodCode != null;

        public virtual void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.GetMangledMethodName(_method));
        }
        public int Offset => 0;
        public override bool IsShareable => _method is InstantiatedMethod || EETypeNode.IsTypeNodeShareable(_method.OwningType);

        public override bool HasConditionalStaticDependencies => CodeBasedDependencyAlgorithm.HasConditionalDependenciesDueToMethodCodePresence(_method);

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory)
        {
            CombinedDependencyList dependencies = null;
            CodeBasedDependencyAlgorithm.AddConditionalDependenciesDueToMethodCodePresence(ref dependencies, factory, _method);
            return dependencies ?? (IEnumerable<CombinedDependencyListEntry>)Array.Empty<CombinedDependencyListEntry>();
        }

        protected override DependencyList ComputeNonRelocationBasedDependencies(NodeFactory factory)
        {
            DependencyList dependencies = _nonRelocationDependencies != null ? new DependencyList(_nonRelocationDependencies) : null;

            TypeDesc owningType = _method.OwningType;
            if (factory.PreinitializationManager.HasEagerStaticConstructor(owningType))
            {
                if (dependencies == null)
                    dependencies = new DependencyList();
                dependencies.Add(factory.EagerCctorIndirection(owningType.GetStaticConstructor()), "Eager .cctor");
            }

            if (_ehInfo != null)
            {
                if (dependencies == null)
                    dependencies = new DependencyList();
                dependencies.Add(_ehInfo, "Exception handling information");
            }

            if (MethodAssociatedDataNode.MethodHasAssociatedData(factory, this))
            {
                dependencies = dependencies ?? new DependencyList();
                dependencies.Add(new DependencyListEntry(factory.MethodAssociatedData(this), "Method associated data"));
            }

            return dependencies;
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly)
        {
            return _methodCode;
        }

        public bool IsSpecialUnboxingThunk => ((CompilerTypeSystemContext)Method.Context).IsSpecialUnboxingThunk(_method);

        public ISymbolNode GetUnboxingThunkTarget(NodeFactory factory)
        {
            Debug.Assert(IsSpecialUnboxingThunk);

            MethodDesc nonUnboxingMethod = ((CompilerTypeSystemContext)Method.Context).GetTargetOfSpecialUnboxingThunk(_method);
            return factory.MethodEntrypoint(nonUnboxingMethod, false);
        }

        public FrameInfo[] FrameInfos => _frameInfos;
        public byte[] GCInfo => _gcInfo;
        public MethodExceptionHandlingInfoNode EHInfo => _ehInfo;

        public ISymbolNode GetAssociatedDataNode(NodeFactory factory)
        {
            if (MethodAssociatedDataNode.MethodHasAssociatedData(factory, this))
                return factory.MethodAssociatedData(this);

            return null;
        }

        public void InitializeFrameInfos(FrameInfo[] frameInfos)
        {
            Debug.Assert(_frameInfos == null);
            _frameInfos = frameInfos;
        }

        public void InitializeGCInfo(byte[] gcInfo)
        {
            Debug.Assert(_gcInfo == null);
            _gcInfo = gcInfo;
        }

        public void InitializeEHInfo(ObjectData ehInfo)
        {
            Debug.Assert(_ehInfo == null);
            if (ehInfo != null)
                _ehInfo = new MethodExceptionHandlingInfoNode(_method, ehInfo);
        }

        public DebugLocInfo[] DebugLocInfos => _debugLocInfos;
        public DebugVarInfo[] DebugVarInfos => _debugVarInfos;
        public DebugEHClauseInfo[] DebugEHClauseInfos => _debugEHClauseInfos;

        public bool IsStateMachineMoveNextMethod => _debugInfo.IsStateMachineMoveNextMethod;

        public void InitializeDebugLocInfos(DebugLocInfo[] debugLocInfos)
        {
            Debug.Assert(_debugLocInfos == null);
            _debugLocInfos = debugLocInfos;
        }

        public void InitializeDebugVarInfos(DebugVarInfo[] debugVarInfos)
        {
            Debug.Assert(_debugVarInfos == null);
            _debugVarInfos = debugVarInfos;
        }

        public void InitializeDebugInfo(MethodDebugInformation debugInfo)
        {
            Debug.Assert(_debugInfo == null);
            _debugInfo = debugInfo;
        }

        public void InitializeLocalTypes(TypeDesc[] localTypes)
        {
            Debug.Assert(_localTypes == null);
            _localTypes = localTypes;
        }

        public void InitializeDebugEHClauseInfos(DebugEHClauseInfo[] debugEHClauseInfos)
        {
            Debug.Assert(_debugEHClauseInfos == null);
            _debugEHClauseInfos = debugEHClauseInfos;
        }

        public IEnumerable<DebugVarInfoMetadata> GetDebugVars()
        {
            MethodSignature sig = _method.Signature;
            int offset = sig.IsStatic ? 0 : 1;

            var parameterNames = new string[sig.Length + offset];
            int i = 0;
            foreach (var paramName in _debugInfo.GetParameterNames())
            {
                parameterNames[i] = paramName;
                i++;
            }

            var localNames = new string[_localTypes.Length];

            foreach (var local in _debugInfo.GetLocalVariables())
            {
                if (!local.CompilerGenerated && local.Slot < localNames.Length)
                    localNames[local.Slot] = local.Name;
            }

            foreach (var varInfo in _debugVarInfos)
            {
                if (varInfo.VarNumber < parameterNames.Length)
                {
                    // This is a parameter
                    TypeDesc varType;
                    if (!sig.IsStatic && varInfo.VarNumber == 0)
                    {
                        varType = _method.OwningType.IsValueType ?
                            _method.OwningType.MakeByRefType() :
                            _method.OwningType;
                    }
                    else
                    {
                        varType = _method.Signature[(int)varInfo.VarNumber - offset];
                    }

                    string name = parameterNames[varInfo.VarNumber];
                    if (name == null)
                        continue;

                    yield return new DebugVarInfoMetadata(name, varType, isParameter: true, varInfo);
                }
                else
                {
                    // This is a local
                    int localNumber = (int)varInfo.VarNumber - sig.Length - offset;
                    string name = localNames[localNumber];
                    if (name == null)
                        continue;

                    yield return new DebugVarInfoMetadata(name, _localTypes[localNumber], isParameter: false, varInfo);
                }
            }
        }

        public void InitializeNonRelocationDependencies(DependencyList dependencies)
        {
            _nonRelocationDependencies = dependencies;
        }

        public IEnumerable<NativeSequencePoint> GetNativeSequencePoints()
        {
            var sequencePoints = new (string Document, int LineNumber)[_debugLocInfos.Length * 4 /* chosen empirically */];
            try
            {
                foreach (var sequencePoint in _debugInfo.GetSequencePoints())
                {
                    int offset = sequencePoint.Offset;
                    if (offset >= sequencePoints.Length)
                    {
                        int newLength = Math.Max(2 * sequencePoints.Length, sequencePoint.Offset + 1);
                        Array.Resize(ref sequencePoints, newLength);
                    }
                    sequencePoints[offset] = (sequencePoint.Document, sequencePoint.LineNumber);
                }
            }
            catch (BadImageFormatException)
            {
                // Roslyn had a bug where it was generating bad sequence points:
                // https://github.com/dotnet/roslyn/issues/20118
                // Do not crash the compiler.
                yield break;
            }

            int previousNativeOffset = -1;
            foreach (var nativeMapping in _debugLocInfos)
            {
                if (nativeMapping.NativeOffset == previousNativeOffset)
                    continue;

                if (nativeMapping.ILOffset < sequencePoints.Length)
                {
                    var sequencePoint = sequencePoints[nativeMapping.ILOffset];
                    if (sequencePoint.Document != null)
                    {
                        yield return new NativeSequencePoint(
                            nativeMapping.NativeOffset,
                            sequencePoint.Document,
                            sequencePoint.LineNumber);
                        previousNativeOffset = nativeMapping.NativeOffset;
                    }
                }
            }
        }

        public override int ClassCode => 788492407;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return comparer.Compare(_method, ((MethodCodeNode)other)._method);
        }

        public override string ToString()
        {
            return _method.ToString();
        }

        internal class MethodCodeNodeDebugView
        {
            private readonly MethodCodeNode _node;

            public MethodCodeNodeDebugView(MethodCodeNode node)
            {
                _node = node;
            }

            public MethodDesc Method => _node.Method;

            public string Disassembly
            {
                get
                {
                    var sb = new StringBuilder();
                    sb.Append("// ");
                    sb.AppendLine(_node.Method.ToString());
                    if (_node.StaticDependenciesAreComputed)
                    {
                        var d = Disassembler.Disassemble(
                            _node.Method.Context.Target.Architecture,
                            _node._methodCode.Data,
                            _node._methodCode.Relocs);
                        sb.Append(d);
                    }
                    else
                    {
                        sb.Append("// Not compiled yet.");
                    }

                    return sb.ToString();
                }
            }
        }
    }

    public readonly struct DebugLocInfo
    {
        public readonly int NativeOffset;
        public readonly int ILOffset;

        public DebugLocInfo(int nativeOffset, int ilOffset)
            => (NativeOffset, ILOffset) = (nativeOffset, ilOffset);
    }
}
