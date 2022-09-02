// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using Internal.IL;
using Internal.Text;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// NEWOBJ operation on String type is actually a call to a static method that returns a String
    /// instance (i.e. there's an explicit call to the runtime allocator from the static method body).
    /// This node is used to model the behavior. It represents the symbol for the target allocator
    /// method and makes sure the String type is marked as constructed.
    /// </summary>
    internal sealed class StringAllocatorMethodNode : DependencyNodeCore<NodeFactory>, IMethodNode
    {
        private readonly MethodDesc _allocationMethod;
        private readonly MethodDesc _constructorMethod;

        public MethodDesc Method => _allocationMethod;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.GetMangledMethodName(_allocationMethod));
        }
        public int Offset => 0;
        public bool RepresentsIndirectionCell => false;

        public StringAllocatorMethodNode(MethodDesc constructorMethod)
        {
            Debug.Assert(constructorMethod.IsConstructor && constructorMethod.OwningType.IsString);

            // Find the allocator method that matches the constructor signature.
            var signatureBuilder = new MethodSignatureBuilder(constructorMethod.Signature);
            signatureBuilder.Flags = MethodSignatureFlags.Static;
            signatureBuilder.ReturnType = constructorMethod.OwningType;

            _allocationMethod = constructorMethod.OwningType.GetKnownMethod("Ctor", signatureBuilder.ToSignature());
            _constructorMethod = constructorMethod;
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            DependencyList result = new DependencyList();

            result.Add(
                factory.ConstructedTypeSymbol(factory.TypeSystemContext.GetWellKnownType(WellKnownType.String)),
                "String constructor call");
            result.Add(
                factory.MethodEntrypoint(_allocationMethod),
                "String constructor call");

            factory.MetadataManager.GetDependenciesDueToMethodCodePresence(ref result, factory, _constructorMethod, methodIL: null);

            return result;
        }

        public override bool HasConditionalStaticDependencies => false;
        public override bool HasDynamicDependencies => false;
        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool StaticDependenciesAreComputed => true;

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => null;

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        int ISortableNode.ClassCode => 1991750873;

        int ISortableNode.CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return comparer.Compare(_allocationMethod, ((StringAllocatorMethodNode)other)._allocationMethod);
        }
    }
}
