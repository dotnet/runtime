// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    public class RuntimeFieldHandleNode : ObjectNode, ISymbolDefinitionNode
    {
        private FieldDesc _targetField;

        public RuntimeFieldHandleNode(FieldDesc targetField)
        {
            Debug.Assert(!targetField.OwningType.IsCanonicalSubtype(CanonicalFormKind.Any));
            Debug.Assert(!targetField.OwningType.IsRuntimeDeterminedSubtype);
            _targetField = targetField;
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix)
              .Append("__RuntimeFieldHandle_")
              .Append(nameMangler.GetMangledFieldName(_targetField));
        }
        public int Offset => 0;
        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);
        public override bool IsShareable => false;
        public override bool StaticDependenciesAreComputed => true;

        public override ObjectNodeSection Section
        {
            get
            {
                if (_targetField.Context.Target.IsWindows)
                    return ObjectNodeSection.ReadOnlyDataSection;
                else
                    return ObjectNodeSection.DataSection;
            }
        }

        private static Utf8String s_NativeLayoutSignaturePrefix = new Utf8String("__RFHSignature_");

        protected override DependencyList ComputeNonRelocationBasedDependencies(NodeFactory factory)
        {
            DependencyList result = null;
            factory.MetadataManager.GetDependenciesDueToLdToken(ref result, factory, _targetField);
            return result;
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataBuilder objData = new ObjectDataBuilder(factory, relocsOnly);

            objData.RequireInitialPointerAlignment();
            objData.AddSymbol(this);

            NativeLayoutFieldLdTokenVertexNode ldtokenSigNode = factory.NativeLayout.FieldLdTokenVertex(_targetField);
            objData.EmitPointerReloc(factory.NativeLayout.NativeLayoutSignature(ldtokenSigNode, s_NativeLayoutSignaturePrefix, _targetField));

            return objData.ToObjectData();
        }

        public override int ClassCode => -1326215725;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return comparer.Compare(_targetField, ((RuntimeFieldHandleNode)other)._targetField);
        }
    }
}
