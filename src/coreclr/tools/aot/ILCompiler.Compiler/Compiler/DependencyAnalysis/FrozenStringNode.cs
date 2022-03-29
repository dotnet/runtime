// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    public class FrozenStringNode : EmbeddedObjectNode, ISymbolDefinitionNode
    {
        private string _data;
        private int _syncBlockSize;

        public FrozenStringNode(string data, TargetDetails target)
        {
            _data = data;
            _syncBlockSize = target.PointerSize;
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("__Str_").Append(nameMangler.GetMangledStringName(_data));
        }

        public override bool StaticDependenciesAreComputed => true;

        int ISymbolNode.Offset => 0;

        int ISymbolDefinitionNode.Offset
        {
            get
            {
                // The frozen string symbol points at the MethodTable portion of the object, skipping over the sync block
                return OffsetFromBeginningOfArray + _syncBlockSize;
            }
        }

        private static IEETypeNode GetEETypeNode(NodeFactory factory)
        {
            DefType systemStringType = factory.TypeSystemContext.GetWellKnownType(WellKnownType.String);

            //
            // The GC requires a direct reference to frozen objects' EETypes. If System.String will be compiled into a separate
            // binary, it must be cloned into this one.
            //
            IEETypeNode stringSymbol = factory.ConstructedTypeSymbol(systemStringType);

            if (stringSymbol.RepresentsIndirectionCell)
            {
                return factory.ConstructedClonedTypeSymbol(systemStringType);
            }
            else
            {
                return stringSymbol;
            }
        }

        public override void EncodeData(ref ObjectDataBuilder dataBuilder, NodeFactory factory, bool relocsOnly)
        {
            dataBuilder.EmitZeroPointer(); // Sync block

            dataBuilder.EmitPointerReloc(GetEETypeNode(factory));

            dataBuilder.EmitInt(_data.Length);

            foreach (char c in _data)
            {
                dataBuilder.EmitShort((short)c);
            }

            // Null-terminate for friendliness with interop
            dataBuilder.EmitShort(0);

        }

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            return new DependencyListEntry[]
            {
                new DependencyListEntry(GetEETypeNode(factory), "Frozen string literal MethodTable"),
            };
        }

        protected override void OnMarked(NodeFactory factory)
        {
            factory.FrozenSegmentRegion.AddEmbeddedObject(this);
        }

        public override int ClassCode => -1733946122;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return string.CompareOrdinal(_data, ((FrozenStringNode)other)._data);
        }
    }
}
