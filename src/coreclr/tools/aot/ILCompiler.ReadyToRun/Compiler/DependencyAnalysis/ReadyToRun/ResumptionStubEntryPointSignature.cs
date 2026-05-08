// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.ReadyToRunConstants;
using Internal.Text;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    internal class ResumptionStubEntryPointSignature : Signature
    {
        private readonly MethodWithGCInfo _resumptionStub;

        public ResumptionStubEntryPointSignature(MethodWithGCInfo resumptionStub) => _resumptionStub = resumptionStub;

        public override int ClassCode => 1927438562;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataSignatureBuilder builder = new ObjectDataSignatureBuilder(factory, relocsOnly);
            builder.AddSymbol(this);
            builder.EmitByte((byte)ReadyToRunFixupKind.ResumptionStubEntryPoint);
            // Emit a relocation to the resumption stub code; at link time this becomes the RVA.
            builder.EmitReloc(_resumptionStub, RelocType.IMAGE_REL_BASED_ADDR32NB, delta: factory.Target.CodeDelta);

            return builder.ToObjectData();
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("ResumptionStubEntryPoint_"u8);
            sb.Append(nameMangler.GetMangledMethodName(_resumptionStub.Method));
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return comparer.Compare(_resumptionStub.Method, ((ResumptionStubEntryPointSignature)other)._resumptionStub.Method);
        }
    }
}
