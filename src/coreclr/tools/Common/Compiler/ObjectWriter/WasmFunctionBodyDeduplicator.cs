// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysis.Wasm;
using ILCompiler.DependencyAnalysisFramework;
using Internal.JitInterface;

namespace ILCompiler.ObjectWriter
{
    internal interface IWasmFunctionBodyNode
    {
        // A shareable body must not encode its own table-slot or runtime-function identity.
        bool IsShareableWasmFunctionBody { get; }

        bool HasCompatibleWasmRuntimeMetadata(IWasmFunctionBodyNode other);
    }

    internal sealed class WasmFunctionBodyDeduplicator
    {
        private readonly Dictionary<int, List<ObjectNode>> _buckets = [];
        private readonly Dictionary<ObjectNode, ObjectNode> _canonicalBodies = [];

        public void Prepare(
            IReadOnlyCollection<DependencyNode> nodes,
            NodeFactory factory,
            Func<ObjectNode, bool> shouldSkip)
        {
            foreach (DependencyNode dependency in nodes)
            {
                if (dependency is not ObjectNode node
                    || node is not INodeWithTypeSignature signatureNode
                    || node is not IWasmFunctionBodyNode bodyNode
                    || !bodyNode.IsShareableWasmFunctionBody
                    || shouldSkip(node))
                {
                    continue;
                }

                ObjectNode.ObjectData data = node.GetData(factory);
                if (data.DefinedSymbols.Length != 1 || data.DefinedSymbols[0].Offset != 0)
                {
                    continue;
                }
                if (HasSelfRelocation(node, data.Relocs))
                {
                    continue;
                }

                int hashCode = GetHashCode(signatureNode, data);
                if (!_buckets.TryGetValue(hashCode, out List<ObjectNode> candidates))
                {
                    candidates = [];
                    _buckets.Add(hashCode, candidates);
                }

                ObjectNode canonical = null;
                foreach (ObjectNode candidate in candidates)
                {
                    if (AreEquivalent(factory, node, signatureNode, data, candidate))
                    {
                        canonical = candidate;
                        break;
                    }
                }

                if (canonical is null)
                {
                    candidates.Add(node);
                }
                else
                {
                    _canonicalBodies.Add(node, canonical);
                }
            }
        }

        public bool TryGetCanonicalBody(ObjectNode node, out ObjectNode canonical) =>
            _canonicalBodies.TryGetValue(node, out canonical);

        private static bool HasSelfRelocation(ObjectNode node, Relocation[] relocations)
        {
            if (relocations is null)
            {
                return false;
            }

            foreach (Relocation relocation in relocations)
            {
                if (ReferenceEquals(relocation.Target, node))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool AreEquivalent(
            NodeFactory factory,
            ObjectNode node,
            INodeWithTypeSignature signatureNode,
            ObjectNode.ObjectData data,
            ObjectNode candidate)
        {
            INodeWithTypeSignature candidateSignatureNode = (INodeWithTypeSignature)candidate;
            if (!WasmLowering.GetSignature(signatureNode).FuncType.Equals(
                WasmLowering.GetSignature(candidateSignatureNode).FuncType))
            {
                return false;
            }

            IWasmFunctionBodyNode bodyNode = (IWasmFunctionBodyNode)node;
            IWasmFunctionBodyNode candidateBodyNode = (IWasmFunctionBodyNode)candidate;
            if (!bodyNode.HasCompatibleWasmRuntimeMetadata(candidateBodyNode))
            {
                return false;
            }

            ObjectNode.ObjectData candidateData = candidate.GetData(factory);
            return data.Data.AsSpan().SequenceEqual(candidateData.Data)
                && RelocationsEqual(data.Relocs, candidateData.Relocs);
        }

        private static bool RelocationsEqual(Relocation[] left, Relocation[] right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }
            if (left is null || right is null || left.Length != right.Length)
            {
                return false;
            }

            for (int i = 0; i < left.Length; i++)
            {
                if (left[i].Offset != right[i].Offset
                    || left[i].RelocType != right[i].RelocType
                    || !ReferenceEquals(left[i].Target, right[i].Target))
                {
                    return false;
                }
            }

            return true;
        }

        private static int GetHashCode(INodeWithTypeSignature node, ObjectNode.ObjectData data)
        {
            HashCode hash = new HashCode();
            hash.Add(WasmLowering.GetSignature(node).FuncType);
            hash.AddBytes(data.Data);

            if (data.Relocs is not null)
            {
                foreach (Relocation relocation in data.Relocs)
                {
                    hash.Add(relocation.Offset);
                    hash.Add(relocation.RelocType);
                    hash.Add(RuntimeHelpers.GetHashCode(relocation.Target));
                }
            }

            return hash.ToHashCode();
        }
    }
}
