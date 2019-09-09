// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

using ILCompiler.DependencyAnalysisFramework;
using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class NewObjectFixupSignature : Signature
    {
        private readonly TypeDesc _typeDesc;
        private readonly SignatureContext _signatureContext;

        public NewObjectFixupSignature(TypeDesc typeDesc, SignatureContext signatureContext)
        {
            _typeDesc = typeDesc;
            _signatureContext = signatureContext;
        }

        public override int ClassCode => 551247760;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ReadyToRunCodegenNodeFactory r2rFactory = (ReadyToRunCodegenNodeFactory)factory;
            ObjectDataSignatureBuilder dataBuilder = new ObjectDataSignatureBuilder();

            if (!relocsOnly)
            {
                dataBuilder.AddSymbol(this);

                EcmaModule targetModule = _signatureContext.GetTargetModule(_typeDesc);
                SignatureContext innerContext = dataBuilder.EmitFixup(r2rFactory, ReadyToRunFixupKind.READYTORUN_FIXUP_NewObject, targetModule, _signatureContext);
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
            throw new NotImplementedException();
        }

        protected override DependencyList ComputeNonRelocationBasedDependencies(NodeFactory factory)
        {
            DependencyList dependencies = new DependencyList();
            dependencies.Add(factory.ConstructedTypeSymbol(_typeDesc), "Type constructed through new object fixup");
            return dependencies;
        }
    }
}
