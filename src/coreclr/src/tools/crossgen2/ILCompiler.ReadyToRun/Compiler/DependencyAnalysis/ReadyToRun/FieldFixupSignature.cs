// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

using Internal.JitInterface;
using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

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
        }

        public override int ClassCode => 271828182;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ReadyToRunCodegenNodeFactory r2rFactory = (ReadyToRunCodegenNodeFactory)factory;
            ObjectDataSignatureBuilder dataBuilder = new ObjectDataSignatureBuilder();

            if (!relocsOnly)
            {
                dataBuilder.AddSymbol(this);

                EcmaModule targetModule = _signatureContext.GetTargetModule(_fieldDesc);
                SignatureContext innerContext = dataBuilder.EmitFixup(r2rFactory, _fixupKind, targetModule, _signatureContext);

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
            throw new NotImplementedException();
        }

        protected override DependencyList ComputeNonRelocationBasedDependencies(NodeFactory factory)
        {
            DependencyList dependencies = new DependencyList();
            dependencies.Add(factory.NecessaryTypeSymbol(_fieldDesc.OwningType), "Type referenced in a fixup signature");
            return dependencies;
        }
    }
}
