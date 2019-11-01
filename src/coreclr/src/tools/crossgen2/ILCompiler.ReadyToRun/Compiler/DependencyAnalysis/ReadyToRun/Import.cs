// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Internal.Text;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// This class represents a single indirection cell in one of the import tables.
    /// </summary>
    public class Import : EmbeddedObjectNode, ISymbolDefinitionNode, ISortableSymbolNode
    {
        public readonly ImportSectionNode Table;

        internal readonly RvaEmbeddedPointerIndirectionNode<Signature> ImportSignature;

        internal readonly string CallSite;

        public Import(ImportSectionNode tableNode, Signature importSignature, string callSite = null)
        {
            Table = tableNode;
            CallSite = callSite;
            ImportSignature = new RvaEmbeddedPointerIndirectionNode<Signature>(importSignature, callSite);
        }

        protected override void OnMarked(NodeFactory factory)
        {
            Table.AddImport(factory, this);
        }

        protected override string GetName(NodeFactory factory)
        {
            Utf8StringBuilder sb = new Utf8StringBuilder();
            AppendMangledName(factory.NameMangler, sb);
            return sb.ToString();
        }

        private const int ClassCodeValue = 667823013;

        public override int ClassCode => ClassCodeValue;

        public virtual bool EmitPrecode => Table.EmitPrecode;

        public override void EncodeData(ref ObjectDataBuilder dataBuilder, NodeFactory factory, bool relocsOnly)
        {
            // This needs to be an empty target pointer since it will be filled in with Module*
            // when loaded by CoreCLR
            dataBuilder.EmitZeroPointer();
        }

        public virtual void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(Table.Name);
            sb.Append("->");
            ImportSignature.AppendMangledName(nameMangler, sb);
        }

        public override bool StaticDependenciesAreComputed => true;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            return new DependencyListEntry[] { new DependencyListEntry(ImportSignature, "Signature for ready-to-run fixup import") };
        }

        public int CompareToImpl(ISortableSymbolNode other, CompilerComparer comparer)
        {
            return new ObjectNodeComparer(comparer).Compare(this, (Import)other);
        }

        public override bool RepresentsIndirectionCell => true;

        int ISymbolDefinitionNode.Offset => OffsetFromBeginningOfArray;
        int ISymbolNode.Offset => 0;
    }
}
