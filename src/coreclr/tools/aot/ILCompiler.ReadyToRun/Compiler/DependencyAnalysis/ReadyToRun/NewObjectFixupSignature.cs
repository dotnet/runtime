// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using Internal.ReadyToRunConstants;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class NewObjectFixupSignature : Signature
    {
        private readonly TypeDesc _typeDesc;

        public NewObjectFixupSignature(TypeDesc typeDesc)
        {
            _typeDesc = typeDesc;

            // Ensure types in signature are loadable and resolvable, otherwise we'll fail later while emitting the signature
            ((CompilerTypeSystemContext)typeDesc.Context).EnsureLoadableType(typeDesc);
        }

        public override int ClassCode => 551247760;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataSignatureBuilder dataBuilder = new ObjectDataSignatureBuilder();

            if (!relocsOnly)
            {
                dataBuilder.AddSymbol(this);

                IEcmaModule targetModule = factory.SignatureContext.GetTargetModule(_typeDesc);
                SignatureContext innerContext = dataBuilder.EmitFixup(factory, ReadyToRunFixupKind.NewObject, targetModule, factory.SignatureContext);
                dataBuilder.EmitTypeSignature(_typeDesc, innerContext);
            }

            return dataBuilder.ToObjectData();
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append($@"NewObjectSignature: ");
            sb.Append(nameMangler.GetMangledTypeName(_typeDesc));
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            NewObjectFixupSignature otherNode = (NewObjectFixupSignature)other;
            return comparer.Compare(_typeDesc, otherNode._typeDesc);
        }
    }
}
