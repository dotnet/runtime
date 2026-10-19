// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using ILCompiler.DependencyAnalysis;

using Internal.IL.Stubs;
using Internal.JitInterface;
using Internal.TypeSystem;

namespace ILCompiler
{
    public sealed class MethodBodyDeduplicator : IObjectDataDeduplicator
    {
        private readonly bool _genericsOnly;
        private int _previousHashCount;

        public MethodBodyDeduplicator(bool genericsOnly)
        {
            _genericsOnly = genericsOnly;
        }

        public bool CanFold(MethodDesc method)
        {
            if (!_genericsOnly || method.HasInstantiation || method.OwningType.HasInstantiation)
                return true;

            if (method.GetTypicalMethodDefinition() is GetFieldHelperMethodOverride)
                return true;

            return false;
        }

        public void DeduplicatePass(NodeFactory factory, Dictionary<ISymbolNode, ISymbolNode> previousSymbolRemapping, Dictionary<ISymbolNode, ISymbolNode> symbolRemapping)
        {
            var methodHash = new HashSet<MethodInternKey>(_previousHashCount, new MethodInternComparer(factory, previousSymbolRemapping, _genericsOnly));

            foreach (IMethodBodyNode body in factory.MetadataManager.GetCompiledMethodBodies())
            {
                if (!CanFold(body.Method))
                    continue;

                // We don't track special unboxing thunks as virtual method use related so ignore them
                if (body is ISpecialUnboxThunkNode unboxThunk && unboxThunk.IsSpecialUnboxingThunk)
                    continue;

                // Bodies that are visible from outside should not be folded because we don't know
                // if they're address taken.
                if (!factory.GetSymbolAlternateName(body, out _).IsNull)
                    continue;

                var key = new MethodInternKey(body, factory, previousSymbolRemapping);
                if (methodHash.TryGetValue(key, out MethodInternKey found))
                {
                    symbolRemapping.TryAdd(body, found.Method);
                }
                else
                {
                    methodHash.Add(key);
                }
            }

            _previousHashCount = methodHash.Count;
        }

        private sealed class MethodInternKey
        {
            public IMethodBodyNode Method { get; }
            public int HashCode { get; }

            public MethodInternKey(IMethodBodyNode node, NodeFactory factory, Dictionary<ISymbolNode, ISymbolNode> interner)
            {
                ObjectNode.ObjectData data = ((ObjectNode)node).GetData(factory, relocsOnly: false);

                var hashCode = default(HashCode);
                hashCode.AddBytes(data.Data);

                if (factory.Target.IsWasm)
                    hashCode.Add(WasmLowering.GetSignature(node.Method).FuncType);

                // Hash relocations consistently with MethodInternComparer.AreSame so that bodies that only differ
                // in their relocation targets (e.g. instantiations over different struct state machines)
                // don't all end up in the same bucket.
                foreach (Relocation reloc in data.Relocs)
                {
                    hashCode.Add((int)reloc.RelocType);
                    hashCode.Add(reloc.Offset);

                    ISymbolNode target = reloc.Target;
                    if (target is MethodReadOnlyDataNode rodata)
                    {
                        // AreSame considers read-only data nodes with the same contents equal
                        hashCode.AddBytes(rodata.GetData(factory, relocsOnly: false).Data);
                    }
                    else
                    {
                        // AreSame considers targets equal if one got interned into the other. Interned
                        // targets are never interning targets themselves, so hashing the target this
                        // maps to is consistent with that.
                        if (interner != null && interner.TryGetValue(target, out ISymbolNode internedTarget))
                            target = internedTarget;

                        hashCode.Add(RuntimeHelpers.GetHashCode(target));
                    }
                }

                var nodeWithCodeInfo = (INodeWithCodeInfo)node;

                hashCode.AddBytes(nodeWithCodeInfo.GCInfo);

                foreach (FrameInfo fi in nodeWithCodeInfo.FrameInfos)
                    hashCode.Add(fi.GetHashCode());

                ObjectNode.ObjectData ehData = nodeWithCodeInfo.EHInfo?.GetData(factory, relocsOnly: false);

                if (ehData is not null)
                    hashCode.AddBytes(ehData.Data);

                HashCode = hashCode.ToHashCode();
                Method = node;
            }
        }

        private sealed class MethodInternComparer : IEqualityComparer<MethodInternKey>
        {
            private readonly NodeFactory _factory;
            private readonly Dictionary<ISymbolNode, ISymbolNode> _interner;
            private readonly bool _genericsOnly;

            public MethodInternComparer(NodeFactory factory, Dictionary<ISymbolNode, ISymbolNode> interner, bool genericsOnly)
                => (_factory, _interner, _genericsOnly) = (factory, interner, genericsOnly);

            public int GetHashCode(MethodInternKey key) => key.HashCode;

            private static bool AreSame(ReadOnlySpan<byte> o1, ReadOnlySpan<byte> o2) => o1.SequenceEqual(o2);

            private bool AreSame(ObjectNode.ObjectData o1, ObjectNode.ObjectData o2)
            {
                if (AreSame(o1.Data, o2.Data) && o1.Relocs.Length == o2.Relocs.Length)
                {
                    for (int i = 0; i < o1.Relocs.Length; i++)
                    {
                        ref Relocation r1 = ref o1.Relocs[i];
                        ref Relocation r2 = ref o2.Relocs[i];
                        if (r1.RelocType != r2.RelocType
                            || r1.Offset != r2.Offset)
                        {
                            return false;
                        }

                        if (r1.Target != r2.Target)
                        {
                            if (r1.Target is MethodReadOnlyDataNode rodata1
                                && r2.Target is MethodReadOnlyDataNode rodata2
                                && AreSame(rodata1.GetData(_factory, relocsOnly: false), rodata2.GetData(_factory, relocsOnly: false)))
                            {
                                // We can consider same MethodReadOnlyDataNode the same.
                            }
                            else if (_interner != null &&
                                ((_interner.TryGetValue(r1.Target, out ISymbolNode t1) && r2.Target == t1)
                                || (_interner.TryGetValue(r2.Target, out ISymbolNode t2) && r1.Target == t2)))
                            {
                                // These got already interned
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }

                    return true;
                }

                return false;
            }

            public bool Equals(MethodInternKey a, MethodInternKey b)
            {
                if (a.HashCode != b.HashCode)
                    return false;

                if (_genericsOnly
                    && a.Method.Method.GetTypicalMethodDefinition() != b.Method.Method.GetTypicalMethodDefinition())
                    return false;

                // Identical WASM bodies can still have different types when some parameters are unused.
                if (_factory.Target.IsWasm
                    && !WasmLowering.GetSignature(a.Method.Method).FuncType.Equals(WasmLowering.GetSignature(b.Method.Method).FuncType))
                    return false;

                ObjectNode.ObjectData o1data = ((ObjectNode)a.Method).GetData(_factory, relocsOnly: false);
                ObjectNode.ObjectData o2data = ((ObjectNode)b.Method).GetData(_factory, relocsOnly: false);

                if (!AreSame(o1data, o2data))
                    return false;

                var o1codeinfo = (INodeWithCodeInfo)a.Method;
                var o2codeinfo = (INodeWithCodeInfo)b.Method;
                if (!AreSame(o1codeinfo.GCInfo, o2codeinfo.GCInfo))
                    return false;

                FrameInfo[] o1frames = o1codeinfo.FrameInfos;
                FrameInfo[] o2frames = o2codeinfo.FrameInfos;
                if (o1frames.Length != o2frames.Length)
                    return false;

                for (int i = 0; i < o1frames.Length; i++)
                {
                    if (!o1frames[i].Equals(o2frames[i]))
                        return false;
                }

                MethodExceptionHandlingInfoNode o1eh = o1codeinfo.EHInfo;
                MethodExceptionHandlingInfoNode o2eh = o2codeinfo.EHInfo;

                if (o1eh == o2eh)
                    return true;

                if (o1eh is null || o2eh is null)
                    return false;

                return AreSame(o1eh.GetData(_factory, relocsOnly: false), o2eh.GetData(_factory, relocsOnly: false));
            }
        }
    }
}
