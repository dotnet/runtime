// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysis.Wasm;
using ILCompiler.DependencyAnalysisFramework;
using Internal.JitInterface;

namespace ILCompiler.ObjectWriter
{
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
                    || (node is INodeWithFunclets nodeWithFunclets && nodeWithFunclets.GetFuncletKinds().Length > 0)
                    || shouldSkip(node))
                {
                    continue;
                }

                ObjectNode.ObjectData data = node.GetData(factory);
                if (data.DefinedSymbols.Length != 1 || data.DefinedSymbols[0].Offset != 0)
                {
                    continue;
                }
                if (HasTableIndexSelfRelocation(node, data.Relocs))
                {
                    continue;
                }

                int hashCode = GetHashCode(node, signatureNode, data);
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
                    Debug.Assert(SortableDependencyNode.CompareImpl(canonical, node, CompilerComparer.Instance) < 0);
                    _canonicalBodies.Add(node, canonical);
                }
            }
        }

        public bool TryGetCanonicalBody(ObjectNode node, out ObjectNode canonical) =>
            _canonicalBodies.TryGetValue(node, out canonical);

        private static bool HasTableIndexSelfRelocation(ObjectNode node, Relocation[] relocations)
        {
            if (relocations is null)
            {
                return false;
            }

            foreach (Relocation relocation in relocations)
            {
                bool exposesTableIndex = relocation.RelocType switch
                {
                    RelocType.IMAGE_REL_BASED_WASM32_TABLE or
                    RelocType.IMAGE_REL_BASED_WASM64_TABLE or
                    RelocType.WASM_TABLE_INDEX_SLEB or
                    RelocType.WASM_TABLE_INDEX_I32 or
                    RelocType.WASM_TABLE_INDEX_I64 or
                    RelocType.WASM_TABLE_INDEX_REL_I32 or
                    RelocType.WASM_MEMORY_ADDR_REL_SLEB => true,

                    RelocType.IMAGE_REL_BASED_ABSOLUTE or
                    RelocType.IMAGE_REL_BASED_HIGHLOW or
                    RelocType.IMAGE_REL_BASED_THUMB_MOV32 or
                    RelocType.IMAGE_REL_BASED_DIR64 or
                    RelocType.IMAGE_REL_BASED_ADDR32NB or
                    RelocType.IMAGE_REL_BASED_REL32 or
                    RelocType.IMAGE_REL_BASED_THUMB_BRANCH24 or
                    RelocType.IMAGE_REL_BASED_THUMB_MOV32_PCREL or
                    RelocType.IMAGE_REL_BASED_ARM64_BRANCH26 or
                    RelocType.IMAGE_REL_BASED_LOONGARCH64_PC or
                    RelocType.IMAGE_REL_BASED_LOONGARCH64_JIR or
                    RelocType.IMAGE_REL_BASED_RISCV64_CALL_PLT or
                    RelocType.IMAGE_REL_BASED_RISCV64_PCREL_I or
                    RelocType.IMAGE_REL_BASED_RISCV64_PCREL_S or
                    RelocType.IMAGE_REL_BASED_RELPTR32 or
                    RelocType.IMAGE_REL_SECTION or
                    RelocType.IMAGE_REL_BASED_ARM64_PAGEBASE_REL21 or
                    RelocType.IMAGE_REL_BASED_ARM64_PAGEOFFSET_12A or
                    RelocType.IMAGE_REL_BASED_ARM64_PAGEOFFSET_12L or
                    RelocType.WASM_FUNCTION_INDEX_LEB or
                    RelocType.WASM_MEMORY_ADDR_LEB or
                    RelocType.WASM_MEMORY_ADDR_SLEB or
                    RelocType.WASM_TYPE_INDEX_LEB or
                    RelocType.WASM_GLOBAL_INDEX_LEB or
                    RelocType.WASM_MEMORY_ADDR_REL_LEB or
                    RelocType.WASM_CLR_RESTORE_CONTEXT_EXCEPTION_TAG_LEB or
                    RelocType.WASM_METHOD_RELATIVE_VIRTUAL_IP_I32 or
                    RelocType.WASM_ASYNC_RESUME_INFO_DELTA_ULEB or
                    RelocType.IMAGE_REL_SECREL or
                    RelocType.IMAGE_REL_TLSGD or
                    RelocType.IMAGE_REL_TPOFF or
                    RelocType.IMAGE_REL_AARCH64_TLSDESC_ADR_PAGE21 or
                    RelocType.IMAGE_REL_AARCH64_TLSDESC_LD64_LO12 or
                    RelocType.IMAGE_REL_AARCH64_TLSDESC_ADD_LO12 or
                    RelocType.IMAGE_REL_AARCH64_TLSDESC_CALL or
                    RelocType.IMAGE_REL_AARCH64_TLSLE_ADD_TPREL_HI12 or
                    RelocType.IMAGE_REL_AARCH64_TLSLE_ADD_TPREL_LO12_NC or
                    RelocType.IMAGE_REL_ARM_PREL31 or
                    RelocType.IMAGE_REL_ARM_JUMP24 or
                    RelocType.IMAGE_REL_ARM64_TLS_SECREL_HIGH12A or
                    RelocType.IMAGE_REL_ARM64_TLS_SECREL_LOW12A or
                    RelocType.IMAGE_REL_SYMBOL_SIZE or
                    RelocType.IMAGE_REL_FILE_ABSOLUTE or
                    RelocType.IMAGE_REL_FILE_CHECKSUM_CALLBACK => false,

                    _ => throw new InvalidOperationException(
                        $"Unhandled relocation type {relocation.RelocType} in Wasm function-body deduplication."),
                };

                if (exposesTableIndex && TargetsSelf(node, relocation.Target))
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

            ObjectNode.ObjectData candidateData = candidate.GetData(factory);
            return data.Data.AsSpan().SequenceEqual(candidateData.Data)
                && RelocationsEqual(node, data.Relocs, candidate, candidateData.Relocs);
        }

        private static bool RelocationsEqual(
            ObjectNode leftNode,
            Relocation[] left,
            ObjectNode rightNode,
            Relocation[] right)
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
                    || left[i].RelocType != right[i].RelocType)
                {
                    return false;
                }

                bool leftTargetsSelf = TargetsSelf(leftNode, left[i].Target);
                bool rightTargetsSelf = TargetsSelf(rightNode, right[i].Target);
                if (leftTargetsSelf != rightTargetsSelf
                    || (!leftTargetsSelf && !ReferenceEquals(left[i].Target, right[i].Target)))
                {
                    return false;
                }
            }

            return true;
        }

        private static int GetHashCode(
            ObjectNode objectNode,
            INodeWithTypeSignature node,
            ObjectNode.ObjectData data)
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
                    bool targetsSelf = TargetsSelf(objectNode, relocation.Target);
                    hash.Add(targetsSelf);
                    if (!targetsSelf)
                    {
                        hash.Add(RuntimeHelpers.GetHashCode(relocation.Target));
                    }
                }
            }

            return hash.ToHashCode();
        }

        private static bool TargetsSelf(ObjectNode node, ISymbolNode target)
        {
            if (ReferenceEquals(target, node))
            {
                return true;
            }

            return node is IMethodNode methodNode
                && target is IMethodNode targetMethodNode
                && targetMethodNode.Method == methodNode.Method;
        }
    }
}
