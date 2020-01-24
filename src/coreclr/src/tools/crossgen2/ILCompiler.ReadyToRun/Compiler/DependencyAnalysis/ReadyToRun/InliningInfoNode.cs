// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
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
        private readonly EcmaModule _globalContext;

        public InliningInfoNode(TargetDetails target, EcmaModule globalContext)
            : base(target)
        {
            _globalContext = globalContext;
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append("__ReadyToRunInliningInfoTable");
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            // This node does not trigger generation of other nodes.
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });

            Dictionary<EcmaMethod, HashSet<MethodDesc>> inlineeToInliners = new Dictionary<EcmaMethod, HashSet<MethodDesc>>();

            var r2rfactory = ((ReadyToRunCodegenNodeFactory)factory);

            // Build a map from inlinee to the list of inliners
            // We are only interested in the generic definitions of these.
            foreach (MethodWithGCInfo methodNode in r2rfactory.EnumerateCompiledMethods())
            {
                MethodDesc[] inlinees = methodNode.InlinedMethods;
                MethodDesc inliner = methodNode.Method;
                MethodDesc inlinerDefinition = inliner.GetTypicalMethodDefinition();

                foreach (MethodDesc inlinee in inlinees)
                {
                    MethodDesc inlineeDefinition = inlinee.GetTypicalMethodDefinition();
                    if (!(inlineeDefinition is EcmaMethod ecmaInlineeDefinition))
                    {
                        // We don't record non-ECMA methods because they don't have tokens that
                        // diagnostic tools could reason about anyway.
                        continue;
                    }

                    if (!inlineeToInliners.TryGetValue(ecmaInlineeDefinition, out HashSet<MethodDesc> inliners))
                    {
                        inliners = new HashSet<MethodDesc>();
                        inlineeToInliners.Add(ecmaInlineeDefinition, inliners);
                    }
                    inliners.Add(inlinerDefinition);
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
                // Followed by inliner RIDs with flag in the lowest bit
                // - if flag is set, followed by module ID

                var sig = new VertexSequence();

                bool isForeignInlinee = inlinee.Module != _globalContext;
                sig.Append(new UnsignedConstant((uint)(inlineeRid << 1 | (isForeignInlinee ? 1 : 0))));
                if (isForeignInlinee)
                {
                    sig.Append(new UnsignedConstant((uint)r2rfactory.ManifestMetadataTable.ModuleToIndex(inlinee.Module)));
                }

                foreach (EcmaMethod inliner in inlineeWithInliners.Value)
                {
                    int inlinerRid = MetadataTokens.GetRowNumber(inliner.Handle);
                    bool isForeignInliner = inliner.Module != _globalContext;
                    sig.Append(new UnsignedConstant((uint)(inlinerRid << 1 | (isForeignInliner ? 1 : 0))));
                    if (isForeignInliner)
                    {
                        sig.Append(new UnsignedConstant((uint)r2rfactory.ManifestMetadataTable.ModuleToIndex(inliner.Module)));
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

        public override int ClassCode => -87382891;
    }
}
