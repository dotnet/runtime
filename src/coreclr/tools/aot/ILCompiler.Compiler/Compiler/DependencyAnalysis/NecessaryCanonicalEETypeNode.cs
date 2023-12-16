// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// The node is used in ProjectX to represent a canonical type that does not have a vtable.
    /// </summary>
    public sealed class NecessaryCanonicalEETypeNode : EETypeNode
    {
        public NecessaryCanonicalEETypeNode(NodeFactory factory, TypeDesc type) : base(factory, type)
        {
            Debug.Assert(!type.IsCanonicalDefinitionType(CanonicalFormKind.Any));
            Debug.Assert(type.IsCanonicalSubtype(CanonicalFormKind.Any));
            Debug.Assert(type == type.ConvertToCanonForm(CanonicalFormKind.Specific));
            Debug.Assert(!type.IsMdArray || factory.Target.Abi == TargetAbi.CppCodegen);
        }

        protected override void OutputInterfaceMap(NodeFactory factory, ref ObjectDataBuilder objData)
        {
            for (int i = 0; i < _type.RuntimeInterfaces.Length; i++)
            {
                // Interface omitted for canonical instantiations (constructed at runtime for dynamic types from the native layout info)
                objData.EmitZeroPointer();
            }
        }

        protected override ISymbolNode GetBaseTypeNode(NodeFactory factory)
        {
            return _type.BaseType != null ? factory.NecessaryTypeSymbol(_type.BaseType.NormalizeInstantiation()) : null;
        }

        protected override FrozenRuntimeTypeNode GetFrozenRuntimeTypeNode(NodeFactory factory) => throw new NotSupportedException();

        public override int ClassCode => 1505000724;
    }
}
