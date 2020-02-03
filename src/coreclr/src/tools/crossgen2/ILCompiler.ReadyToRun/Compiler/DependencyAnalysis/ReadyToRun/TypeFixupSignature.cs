// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using Internal.ReadyToRunConstants;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class TypeFixupSignature : Signature
    {
        private readonly ReadyToRunFixupKind _fixupKind;

        private readonly TypeDesc _typeDesc;

        private readonly SignatureContext _signatureContext;

        public TypeFixupSignature(ReadyToRunFixupKind fixupKind, TypeDesc typeDesc, SignatureContext signatureContext)
        {
            _fixupKind = fixupKind;
            _typeDesc = typeDesc;
            _signatureContext = signatureContext;

            // Ensure types in signature are loadable and resolvable, otherwise we'll fail later while emitting the signature
            signatureContext.Resolver.CompilerContext.EnsureLoadableType(typeDesc);
        }

        public override int ClassCode => 255607008;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataSignatureBuilder dataBuilder = new ObjectDataSignatureBuilder();

            if (!relocsOnly)
            {
                dataBuilder.AddSymbol(this);

                EcmaModule targetModule = _signatureContext.GetTargetModule(_typeDesc);
                SignatureContext innerContext = dataBuilder.EmitFixup(factory, _fixupKind, targetModule, _signatureContext);
                dataBuilder.EmitTypeSignature(_typeDesc, innerContext);
            }

            return dataBuilder.ToObjectData();
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append($@"TypeFixupSignature({_fixupKind.ToString()}): ");
            sb.Append(nameMangler.GetMangledTypeName(_typeDesc));
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            TypeFixupSignature otherNode = (TypeFixupSignature)other;
            int result = ((int)_fixupKind).CompareTo((int)otherNode._fixupKind);
            if (result != 0)
                return result;

            result = comparer.Compare(_typeDesc, otherNode._typeDesc);
            if (result != 0)
                return result;

            return _signatureContext.CompareTo(otherNode._signatureContext, comparer);
        }

        protected override DependencyList ComputeNonRelocationBasedDependencies(NodeFactory factory)
        {
            DependencyList dependencies = new DependencyList();

            if (_typeDesc.HasInstantiation && !_typeDesc.IsGenericDefinition)
            {
                dependencies.Add(factory.AllMethodsOnType(_typeDesc), "Methods on generic type instantiation");
            }
            return dependencies;
        }
    }
}
