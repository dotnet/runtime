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

        public TypeFixupSignature(ReadyToRunFixupKind fixupKind, TypeDesc typeDesc)
        {
            _fixupKind = fixupKind;
            _typeDesc = typeDesc;

            // Ensure types in signature are loadable and resolvable, otherwise we'll fail later while emitting the signature
            ((CompilerTypeSystemContext)typeDesc.Context).EnsureLoadableType(typeDesc);
        }

        public override int ClassCode => 255607008;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataSignatureBuilder dataBuilder = new ObjectDataSignatureBuilder();

            if (!relocsOnly)
            {
                dataBuilder.AddSymbol(this);

                EcmaModule targetModule = factory.SignatureContext.GetTargetModule(_typeDesc);
                SignatureContext innerContext = dataBuilder.EmitFixup(factory, _fixupKind, targetModule, factory.SignatureContext);
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

            return comparer.Compare(_typeDesc, otherNode._typeDesc);
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
