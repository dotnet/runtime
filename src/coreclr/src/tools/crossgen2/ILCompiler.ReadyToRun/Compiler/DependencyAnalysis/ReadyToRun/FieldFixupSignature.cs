// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

using Internal.JitInterface;
using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using Internal.ReadyToRunConstants;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class FieldFixupSignature : Signature
    {
        private readonly ReadyToRunFixupKind _fixupKind;

        private readonly FieldDesc _fieldDesc;

        private readonly SignatureContext _signatureContext;

        public FieldFixupSignature(ReadyToRunFixupKind fixupKind, FieldDesc fieldDesc, SignatureContext signatureContext)
        {
            _fixupKind = fixupKind;
            _fieldDesc = fieldDesc;
            _signatureContext = signatureContext;

            // Ensure types in signature are loadable and resolvable, otherwise we'll fail later while emitting the signature
            signatureContext.Resolver.CompilerContext.EnsureLoadableType(fieldDesc.OwningType);
        }

        public override int ClassCode => 271828182;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataSignatureBuilder dataBuilder = new ObjectDataSignatureBuilder();

            if (!relocsOnly)
            {
                dataBuilder.AddSymbol(this);

                EcmaModule targetModule = _signatureContext.GetTargetModule(_fieldDesc);
                SignatureContext innerContext = dataBuilder.EmitFixup(factory, _fixupKind, targetModule, _signatureContext);

                dataBuilder.EmitFieldSignature(_fieldDesc, innerContext);
            }

            return dataBuilder.ToObjectData();
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append($@"TypeFixupSignature({_fixupKind.ToString()}): {_fieldDesc.ToString()}");
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            FieldFixupSignature otherNode = (FieldFixupSignature)other;
            int result = ((int)_fixupKind).CompareTo((int)otherNode._fixupKind);
            if (result != 0)
                return result;

            result = comparer.Compare(_fieldDesc, otherNode._fieldDesc);
            if (result != 0)
                return result;

            return _signatureContext.CompareTo(otherNode._signatureContext, comparer);
        }
    }
}
