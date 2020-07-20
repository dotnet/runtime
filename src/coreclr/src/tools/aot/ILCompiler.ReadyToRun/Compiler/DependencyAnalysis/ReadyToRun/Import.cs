// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// This class represents a single indirection cell in one of the import tables.
    /// </summary>
    public class Import : EmbeddedObjectNode, ISymbolDefinitionNode, ISortableSymbolNode
    {
        public readonly ImportSectionNode Table;

        internal readonly SignatureEmbeddedPointerIndirectionNode ImportSignature;

        internal readonly MethodDesc CallingMethod;

        public Import(ImportSectionNode tableNode, Signature importSignature, MethodDesc callingMethod = null)
        {
            Table = tableNode;
            CallingMethod = callingMethod;
            ImportSignature = new SignatureEmbeddedPointerIndirectionNode(this, importSignature);
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

        public override int ClassCode => 667823013;

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

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            Import otherNode = (Import)other;
            int result = comparer.Compare(CallingMethod, otherNode.CallingMethod);
            if (result != 0)
                return result;

            result = comparer.Compare(ImportSignature.Target, otherNode.ImportSignature.Target);
            if (result != 0)
                return result;

            return Table.CompareToImpl(otherNode.Table, comparer);
        }

        public override bool RepresentsIndirectionCell => true;

        int ISymbolDefinitionNode.Offset => OffsetFromBeginningOfArray;
        int ISymbolNode.Offset => 0;
    }
}
