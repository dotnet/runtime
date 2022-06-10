// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using Internal.ReadyToRunConstants;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class NewArrayFixupSignature : Signature
    {
        private readonly ArrayType _arrayType;

        public NewArrayFixupSignature(ArrayType arrayType)
        {
            _arrayType = arrayType;

            // Ensure types in signature are loadable and resolvable, otherwise we'll fail later while emitting the signature
            ((CompilerTypeSystemContext)arrayType.Context).EnsureLoadableType(arrayType);
        }

        public override int ClassCode => 815543321;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataSignatureBuilder dataBuilder = new ObjectDataSignatureBuilder();

            if (!relocsOnly)
            {
                dataBuilder.AddSymbol(this);

                IEcmaModule targetModule = factory.SignatureContext.GetTargetModule(_arrayType);
                SignatureContext innerContext = dataBuilder.EmitFixup(factory, ReadyToRunFixupKind.NewArray, targetModule, factory.SignatureContext);
                dataBuilder.EmitTypeSignature(_arrayType, innerContext);
            }

            return dataBuilder.ToObjectData();
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append($@"NewArraySignature: ");
            sb.Append(nameMangler.GetMangledTypeName(_arrayType));
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            NewArrayFixupSignature otherNode = (NewArrayFixupSignature)other;
            return comparer.Compare(_arrayType, otherNode._arrayType);
        }
    }
}
