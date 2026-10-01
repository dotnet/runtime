// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// Discovery node for the generic collection interface methods implemented by arrays.
    ///
    /// Arrays implement the generic collection interfaces (IEnumerable, IList, ICollection, IReadOnlyList,
    /// IReadOnlyCollection). At runtime, a call through these interfaces is dispatched to methods on
    /// SZArrayHelper. The InheritedVirtualMethodsNode doesn't see these methods as part of the array type.
    ///
    /// This node is created whenever an array type is used. It has conditional static dependencies:
    /// the resolved SZArrayHelper implementation is compiled only when the corresponding interface slot is
    /// actually used (via VirtualMethodUseNode).
    /// </summary>
    public class ArrayInterfaceMethodsNode : DependencyNodeCore<NodeFactory>
    {
        private readonly ArrayType _arrayType;

        public ArrayInterfaceMethodsNode(ArrayType arrayType)
        {
            Debug.Assert(arrayType.IsSzArray);
            _arrayType = arrayType;
        }

        public ArrayType ArrayType => _arrayType;

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => true;
        public override bool StaticDependenciesAreComputed => true;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => null;

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory)
        {
            List<CombinedDependencyListEntry> result = new List<CombinedDependencyListEntry>();

            MetadataType szArrayHelper = factory.TypeSystemContext.SystemModule.GetType("System"u8, "SZArrayHelper"u8, throwIfNotFound: false);
            if (szArrayHelper == null)
                return result;

            TypeDesc elementType = _arrayType.ElementType;

            foreach (DefType interfaceType in _arrayType.RuntimeInterfaces)
            {
                // Only the generic collection interfaces are dispatched through SZArrayHelper.
                if (!interfaceType.HasInstantiation)
                    continue;

                foreach (MethodDesc interfaceMethod in interfaceType.GetVirtualMethods())
                {
                    // SZArrayHelper provides one generic method per interface method, with a matching name.
                    MethodDesc helperMethodDef = szArrayHelper.GetMethod(interfaceMethod.Name, null);
                    if (helperMethodDef == null)
                        continue;

                    MethodDesc helperMethod = factory.TypeSystemContext.GetInstantiatedMethod(helperMethodDef, new Instantiation(elementType));
                    MethodDesc canonHelperMethod = helperMethod.GetCanonMethodTarget(CanonicalFormKind.Specific);

                    if (!factory.CompilationModuleGroup.ContainsMethodBody(canonHelperMethod, false))
                        continue;

                    result.Add(new CombinedDependencyListEntry(
                        factory.CompiledMethodNode(canonHelperMethod),
                        factory.VirtualMethodUse(interfaceMethod),
                        "Array generic interface method implemented by SZArrayHelper"));
                }
            }

            return result;
        }

        protected override string GetName(NodeFactory factory) => $"Array interface methods on {_arrayType}";
    }
}
