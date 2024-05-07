// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using ILCompiler.DependencyAnalysis;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    internal sealed class ObjectDataInterner
    {
        private readonly NodeFactory _factory;

        private readonly Dictionary<ISymbolNode, ISymbolNode> _symbolRemapping;

        public ObjectDataInterner(NodeFactory factory)
        {
            Debug.Assert(factory.MarkingComplete);

            _factory = factory;

            var symbolRemapping = new Dictionary<ISymbolNode, ISymbolNode>();
            var methodHash = new HashSet<MethodInternKey>();

            foreach (IMethodBodyNode body in factory.MetadataManager.GetCompiledMethodBodies())
            {
                var key = new MethodInternKey(body, this);
                if (methodHash.TryGetValue(key, out MethodInternKey found))
                {
                    symbolRemapping.Add(body, found.Method);
                }
                else
                {
                    methodHash.Add(key);
                }
            }

            _symbolRemapping = symbolRemapping;
        }

        public ISymbolNode GetDeduplicatedSymbol(ISymbolNode original)
        {
            ISymbolNode target = original;
            if (target is ISymbolNodeWithLinkage symbolWithLinkage)
                target = symbolWithLinkage.NodeForLinkage(_factory);

            return _symbolRemapping.TryGetValue(target, out ISymbolNode result) ? result : original;
        }

        private sealed class MethodInternKey : IEquatable<MethodInternKey>
        {
            private readonly ObjectDataInterner _parent;
            private readonly IMethodBodyNode _node;
            private readonly int _hashCode;

            public IMethodBodyNode Method => _node;

            public MethodInternKey(IMethodBodyNode node, ObjectDataInterner parent)
            {
                ObjectNode.ObjectData data = ((ObjectNode)node).GetData(parent._factory, relocsOnly: false);

                var hashCode = default(HashCode);
                hashCode.AddBytes(data.Data);

                var nodeWithCodeInfo = (INodeWithCodeInfo)node;

                hashCode.AddBytes(nodeWithCodeInfo.GCInfo);

                foreach (FrameInfo fi in nodeWithCodeInfo.FrameInfos)
                    hashCode.Add(fi.GetHashCode());

                ObjectNode.ObjectData ehData = nodeWithCodeInfo.EHInfo?.GetData(parent._factory, relocsOnly: false);

                if (ehData is not null)
                    hashCode.AddBytes(ehData.Data);

                _parent = parent;
                _hashCode = hashCode.ToHashCode();
                _node = node;
            }

            public override bool Equals(object obj) => obj is MethodInternKey other && Equals(other);

            public override int GetHashCode() => _hashCode;

            private static bool AreSame(ReadOnlySpan<byte> o1, ReadOnlySpan<byte> o2) => o1.SequenceEqual(o2);

            private static bool AreSame(ObjectNode.ObjectData o1, ObjectNode.ObjectData o2)
            {
                if (AreSame(o1.Data, o2.Data) && o1.Relocs.Length == o2.Relocs.Length)
                {
                    for (int i = 0; i < o1.Relocs.Length; i++)
                    {
                        ref Relocation r1 = ref o1.Relocs[i];
                        ref Relocation r2 = ref o2.Relocs[i];
                        if (r1.RelocType != r2.RelocType
                            || r1.Offset != r2.Offset
                            // TODO: should be comparing target after folding to catch more sameness
                            || r1.Target != r2.Target)
                        {
                            return false;
                        }
                    }

                    return true;
                }

                return false;
            }

            public bool Equals(MethodInternKey other)
            {
                if (other._hashCode != _hashCode)
                    return false;

                ObjectNode.ObjectData o1data = ((ObjectNode)_node).GetData(_parent._factory, relocsOnly: false);
                ObjectNode.ObjectData o2data = ((ObjectNode)other._node).GetData(_parent._factory, relocsOnly: false);

                if (!AreSame(o1data, o2data))
                    return false;

                var o1codeinfo = (INodeWithCodeInfo)_node;
                var o2codeinfo = (INodeWithCodeInfo)other._node;
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

                if (o1eh == null || o2eh == null)
                    return false;

                return AreSame(o1eh.GetData(_parent._factory, relocsOnly: false), o2eh.GetData(_parent._factory, relocsOnly: false));
            }
        }
    }
}
