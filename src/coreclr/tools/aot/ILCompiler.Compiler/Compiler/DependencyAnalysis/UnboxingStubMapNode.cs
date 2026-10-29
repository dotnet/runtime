// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.Text;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// UnboxingStubMap - WASM-only table that maps unboxing stubs to their targets. The layout is:
    /// uint simpleCount, uint instantiatingCount, followed by simpleCount pairs of (stub, target)
    /// function table indices for simple unboxing stubs and instantiatingCount pairs of (stub, target)
    /// function table indices for unboxing and instantiating stubs.
    /// </summary>
    public sealed class UnboxingStubMapNode : ObjectNode, ISymbolDefinitionNode
    {
        public override bool IsShareable => false;

        public override ObjectNodeSection GetSection(NodeFactory factory) => ObjectNodeSection.ReadOnlyDataSection;

        public override bool StaticDependenciesAreComputed => true;

        public int Offset => 0;

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;
        public override int ClassCode => (int)ObjectNodeOrder.UnboxingStubMapNode;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("_unboxing_stub_map"u8);
        }

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            // The data emission relies on the set of compiled methods which is incomplete during
            // dependency tracking. This node doesn't keep anything alive - it only describes stubs
            // that were generated for other reasons.
            if (relocsOnly)
            {
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });
            }

            ObjectDataBuilder objData = new ObjectDataBuilder(factory, relocsOnly);
            objData.RequireInitialPointerAlignment();
            objData.AddSymbol(this);

            ObjectDataBuilder.Reservation simpleCountReservation = objData.ReserveInt();
            ObjectDataBuilder.Reservation instantiatingCountReservation = objData.ReserveInt();

            int simpleCount = 0;
            foreach (UnboxingStubNode stub in factory.MetadataManager.GetUnboxingStubs())
            {
                if (!stub.IsInstantiating)
                {
                    EmitPair(ref objData, factory, stub);
                    simpleCount++;
                }
            }

            int instantiatingCount = 0;
            foreach (UnboxingStubNode stub in factory.MetadataManager.GetUnboxingStubs())
            {
                if (stub.IsInstantiating)
                {
                    EmitPair(ref objData, factory, stub);
                    instantiatingCount++;
                }
            }

            objData.EmitInt(simpleCountReservation, simpleCount);
            objData.EmitInt(instantiatingCountReservation, instantiatingCount);

            return objData.ToObjectData();
        }

        private static void EmitPair(ref ObjectDataBuilder objData, NodeFactory factory, UnboxingStubNode stub)
        {
            var target = factory.MethodEntrypoint(stub.Method);
            Debug.Assert(stub.Marked && target.Marked);
            objData.EmitReloc(stub, RelocType.WASM_TABLE_INDEX_I32);
            objData.EmitReloc(target, RelocType.WASM_TABLE_INDEX_I32);
        }
    }
}
