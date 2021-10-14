// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILTrim.DependencyAnalysis
{
    /// <summary>
    /// Represents a type that is considered allocated at runtime (e.g. with a "new").
    /// </summary>
    public class ConstructedTypeNode : DependencyNodeCore<NodeFactory>
    {
        private readonly EcmaType _type;

        public ConstructedTypeNode(EcmaType type)
        {
            _type = type;
        }

        public override bool HasConditionalStaticDependencies
        {
            get
            {
                // If there's any virtual method, we have conditional dependencies.
                foreach (MethodDesc method in _type.GetAllVirtualMethods())
                {
                    return true;
                }

                return false;
            }
        }

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory)
        {
            // For each virtual method slot (e.g. Object.GetHashCode()), check whether the current type
            // provides an implementation of the virtual method (e.g. SomeFoo.GetHashCode()).
            foreach (MethodDesc decl in _type.EnumAllVirtualSlots())
            {
                MethodDesc impl = _type.FindVirtualFunctionTargetMethodOnObjectType(decl);

                // We're only interested in the case when it's implemented on this type.
                // If the implementation comes from a base type, that's covered by the base type
                // ConstructedTypeNode.
                if (impl.OwningType == _type)
                {
                    // If the slot defining virtual method is used, make sure we generate the implementation method.
                    var ecmaImpl = (EcmaMethod)impl;
                    yield return new(
                        factory.MethodDefinition(ecmaImpl.Module, ecmaImpl.Handle),
                        factory.VirtualMethodUse((EcmaMethod)decl),
                        "Virtual method");
                }
            }
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            DefType baseType = _type.BaseType;
            if (baseType != null)
            {
                yield return new(factory.ConstructedType((EcmaType)baseType), "Base type");
            }
        }

        protected override string GetName(NodeFactory factory)
        {
            return $"{_type} constructed";
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory) => null;
    }
}
