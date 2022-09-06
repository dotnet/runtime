// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    public class RuntimeMethodHandleNode : ObjectNode, ISymbolDefinitionNode
    {
        private MethodDesc _targetMethod;

        public MethodDesc Method => _targetMethod;

        public RuntimeMethodHandleNode(MethodDesc targetMethod)
        {
            Debug.Assert(!targetMethod.IsSharedByGenericInstantiations);

            // IL is allowed to LDTOKEN an uninstantiated thing. Do not check IsRuntimeDetermined for the nonexact thing.
            Debug.Assert((targetMethod.HasInstantiation && targetMethod.IsMethodDefinition)
                || targetMethod.OwningType.IsGenericDefinition
                || !targetMethod.IsRuntimeDeterminedExactMethod);
            _targetMethod = targetMethod;
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix)
              .Append("__RuntimeMethodHandle_")
              .Append(nameMangler.GetMangledMethodName(_targetMethod));
        }
        public int Offset => 0;
        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);
        public override bool IsShareable => false;
        public override bool StaticDependenciesAreComputed => true;

        public override ObjectNodeSection Section
        {
            get
            {
                if (_targetMethod.Context.Target.IsWindows)
                    return ObjectNodeSection.ReadOnlyDataSection;
                else
                    return ObjectNodeSection.DataSection;
            }
        }

        protected override DependencyList ComputeNonRelocationBasedDependencies(NodeFactory factory)
        {
            DependencyList dependencies = null;

            if (!_targetMethod.IsMethodDefinition && !_targetMethod.OwningType.IsGenericDefinition
                && _targetMethod.HasInstantiation && _targetMethod.IsVirtual)
            {
                dependencies ??= new DependencyList();
                dependencies.Add(factory.GVMDependencies(_targetMethod.GetCanonMethodTarget(CanonicalFormKind.Specific)), "GVM dependencies for runtime method handle");
            }

            factory.MetadataManager.GetDependenciesDueToLdToken(ref dependencies, factory, _targetMethod);

            return dependencies;
        }

        private static Utf8String s_NativeLayoutSignaturePrefix = new Utf8String("__RMHSignature_");

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataBuilder objData = new ObjectDataBuilder(factory, relocsOnly);

            objData.RequireInitialPointerAlignment();
            objData.AddSymbol(this);

            NativeLayoutMethodLdTokenVertexNode ldtokenSigNode = factory.NativeLayout.MethodLdTokenVertex(_targetMethod);
            objData.EmitPointerReloc(factory.NativeLayout.NativeLayoutSignature(ldtokenSigNode, s_NativeLayoutSignaturePrefix, _targetMethod));

            return objData.ToObjectData();
        }

        public override int ClassCode => -274400625;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return comparer.Compare(_targetMethod, ((RuntimeMethodHandleNode)other)._targetMethod);
        }
    }
}
