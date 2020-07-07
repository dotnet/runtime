// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata.Ecma335;

using Internal.NativeFormat;
using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// Stores information about what methods got inlined into other methods.
    /// </summary>
    public class InliningInfoNode : HeaderTableNode
    {
        private readonly EcmaModule _module;

        public InliningInfoNode(TargetDetails target, EcmaModule module)
            : base(target)
        {
            _module = module;
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append("__ReadyToRunInliningInfoTable__");
            sb.Append(_module.Assembly.GetName().Name);
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            // This node does not trigger generation of other nodes.
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });

            Dictionary<EcmaMethod, HashSet<EcmaMethod>> inlineeToInliners = new Dictionary<EcmaMethod, HashSet<EcmaMethod>>();

            // Build a map from inlinee to the list of inliners
            // We are only interested in the generic definitions of these.
            foreach (MethodWithGCInfo methodNode in factory.EnumerateCompiledMethods(_module, CompiledMethodCategory.All))
            {
                MethodDesc[] inlinees = methodNode.InlinedMethods;
                MethodDesc inliner = methodNode.Method;
                EcmaMethod inlinerDefinition = (EcmaMethod)inliner.GetTypicalMethodDefinition();

                // Only encode inlining info for inliners within the active module
                Debug.Assert(inlinerDefinition.Module == _module);

                foreach (MethodDesc inlinee in inlinees)
                {
                    MethodDesc inlineeDefinition = inlinee.GetTypicalMethodDefinition();
                    if (!(inlineeDefinition is EcmaMethod ecmaInlineeDefinition))
                    {
                        // We don't record non-ECMA methods because they don't have tokens that
                        // diagnostic tools could reason about anyway.
                        continue;
                    }

                    if (!factory.CompilationModuleGroup.VersionsWithMethodBody(inlinee))
                    {
                        // We cannot record inlining info across version bubble as cross-bubble assemblies
                        // are not guaranteed to preserve token values. Only non-versionable methods may be
                        // inlined across the version bubble.
                        Debug.Assert(inlinee.IsNonVersionable());
                        continue;
                    }

                    if (!inlineeToInliners.TryGetValue(ecmaInlineeDefinition, out HashSet<EcmaMethod> inliners))
                    {
                        inliners = new HashSet<EcmaMethod>();
                        inlineeToInliners.Add(ecmaInlineeDefinition, inliners);
                    }
                    inliners.Add((EcmaMethod)inlinerDefinition);
                }
            }

            // Serialize the map as a hash table
            NativeWriter writer = new NativeWriter();
            Section section = writer.NewSection();

            VertexHashtable hashtable = new VertexHashtable();
            section.Place(hashtable);

            foreach (var inlineeWithInliners in inlineeToInliners)
            {
                EcmaMethod inlinee = inlineeWithInliners.Key;
                int inlineeRid = MetadataTokens.GetRowNumber(inlinee.Handle);
                int hashCode = ReadyToRunHashCode.ModuleNameHashCode(inlinee.Module);
                hashCode ^= inlineeRid;

                // Format of the sequence:
                // Inlinee RID with flag in the lowest bit
                // - if flag is set, followed by module ID
                // Followed by inliner RIDs deltas with flag in the lowest bit
                // - if flag is set, followed by module ID

                var sig = new VertexSequence();

                bool isForeignInlinee = inlinee.Module != _module;
                sig.Append(new UnsignedConstant((uint)(inlineeRid << 1 | (isForeignInlinee ? 1 : 0))));
                if (isForeignInlinee)
                {
                    sig.Append(new UnsignedConstant((uint)factory.ManifestMetadataTable.ModuleToIndex(inlinee.Module)));
                }

                List<EcmaMethod> sortedInliners = new List<EcmaMethod>(inlineeWithInliners.Value);
                sortedInliners.MergeSort((a, b) =>
                {
                    if (a == b)
                        return 0;

                    int aRid = MetadataTokens.GetRowNumber(a.Handle);
                    int bRid = MetadataTokens.GetRowNumber(b.Handle);
                    if (aRid < bRid)
                        return -1;
                    else if (aRid > bRid)
                        return 1;

                    int result = a.Module.CompareTo(b.Module);
                    Debug.Assert(result != 0);
                    return result;
                });

                int baseRid = 0;
                foreach (EcmaMethod inliner in sortedInliners)
                {
                    int inlinerRid = MetadataTokens.GetRowNumber(inliner.Handle);
                    int ridDelta = inlinerRid - baseRid;
                    baseRid = inlinerRid;
                    Debug.Assert(ridDelta >= 0);
                    bool isForeignInliner = inliner.Module != _module;
                    sig.Append(new UnsignedConstant((uint)(ridDelta << 1 | (isForeignInliner ? 1 : 0))));
                    if (isForeignInliner)
                    {
                        sig.Append(new UnsignedConstant((uint)factory.ManifestMetadataTable.ModuleToIndex(inliner.Module)));
                    }
                }

                hashtable.Append((uint)hashCode, section.Place(sig));
            }

            MemoryStream writerContent = new MemoryStream();
            writer.Save(writerContent);

            return new ObjectData(
                data: writerContent.ToArray(),
                relocs: null,
                alignment: 8,
                definedSymbols: new ISymbolDefinitionNode[] { this });
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            InliningInfoNode otherInliningInfo = (InliningInfoNode)other;
            return _module.Assembly.GetName().Name.CompareTo(otherInliningInfo._module.Assembly.GetName().Name);
        }

        public override int ClassCode => -87382891;
    }
}
