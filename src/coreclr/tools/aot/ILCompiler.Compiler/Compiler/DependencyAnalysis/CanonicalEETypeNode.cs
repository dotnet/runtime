// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

using Internal.TypeSystem;
using Internal.Runtime;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Canonical type instantiations are emitted, not because they are used directly by the user code, but because
    /// they are used by the dynamic type loader when dynamically instantiating types at runtime.
    /// The data that we emit on canonical type instantiations should just be the minimum that is needed by the template
    /// type loader.
    /// Similarly, the dependencies that we track for canonical type instantiations are minimal, and are just the ones used
    /// by the dynamic type loader
    /// </summary>
    public sealed class CanonicalEETypeNode : EETypeNode
    {
        public CanonicalEETypeNode(NodeFactory factory, TypeDesc type) : base(factory, type)
        {
            Debug.Assert(!type.IsCanonicalDefinitionType(CanonicalFormKind.Any));
            Debug.Assert(type.IsCanonicalSubtype(CanonicalFormKind.Any));
            Debug.Assert(type == type.ConvertToCanonForm(CanonicalFormKind.Specific));
            Debug.Assert(!type.IsMdArray || factory.Target.Abi == TargetAbi.CppCodegen);
        }

        public override bool StaticDependenciesAreComputed => true;
        public override bool IsShareable => IsTypeNodeShareable(_type);
        protected override bool EmitVirtualSlotsAndInterfaces => true;
        public override bool ShouldSkipEmittingObjectNode(NodeFactory factory) => false;

        protected override DependencyList ComputeNonRelocationBasedDependencies(NodeFactory factory)
        {
            DependencyList dependencyList = base.ComputeNonRelocationBasedDependencies(factory);

            // Ensure that we track the necessary type symbol if we are working with a constructed type symbol.
            // The emitter will ensure we don't emit both, but this allows us assert that we only generate
            // relocs to nodes we emit.
            dependencyList.Add(factory.NecessaryTypeSymbol(_type), "Necessary type symbol related to CanonicalEETypeNode");

            DefType closestDefType = _type.GetClosestDefType();

            dependencyList.Add(factory.VTable(closestDefType), "VTable");

            if (_type.IsCanonicalSubtype(CanonicalFormKind.Universal))
                dependencyList.Add(factory.NativeLayout.TemplateTypeLayout(_type), "Universal generic types always have template layout");

            // Track generic virtual methods that will get added to the GVM tables
            if ((_virtualMethodAnalysisFlags & VirtualMethodAnalysisFlags.NeedsGvmEntries) != 0)
            {
                dependencyList.Add(new DependencyListEntry(factory.TypeGVMEntries(_type.GetTypeDefinition()), "Type with generic virtual methods"));

                AddDependenciesForUniversalGVMSupport(factory, _type, ref dependencyList);
            }

            return dependencyList;
        }

        protected override ISymbolNode GetBaseTypeNode(NodeFactory factory)
        {
            return _type.BaseType != null ? factory.NecessaryTypeSymbol(_type.BaseType.NormalizeInstantiation()) : null;
        }

        protected override ISymbolNode GetNonNullableValueTypeArrayElementTypeNode(NodeFactory factory)
        {
            return factory.ConstructedTypeSymbol(((ArrayType)_type).ElementType);
        }

        protected override int GCDescSize
        {
            get
            {
                // No GCDescs for universal canonical types
                if (_type.IsCanonicalSubtype(CanonicalFormKind.Universal))
                    return 0;

                Debug.Assert(_type.IsCanonicalSubtype(CanonicalFormKind.Specific));
                return GCDescEncoder.GetGCDescSize(_type);
            }
        }

        protected override void OutputGCDesc(ref ObjectDataBuilder builder)
        {
            // No GCDescs for universal canonical types
            if (_type.IsCanonicalSubtype(CanonicalFormKind.Universal))
                return;

            Debug.Assert(_type.IsCanonicalSubtype(CanonicalFormKind.Specific));
            GCDescEncoder.EncodeGCDesc(ref builder, _type);
        }

        protected override void OutputInterfaceMap(NodeFactory factory, ref ObjectDataBuilder objData)
        {
            for (int i = 0; i < _type.RuntimeInterfaces.Length; i++)
            {
                // Interface omitted for canonical instantiations (constructed at runtime for dynamic types from the native layout info)
                objData.EmitZeroPointer();
            }
        }

        protected override int BaseSize
        {
            get
            {
                if (_type.IsCanonicalSubtype(CanonicalFormKind.Universal) && _type.IsDefType)
                {
                    LayoutInt instanceByteCount = ((DefType)_type).InstanceByteCount;

                    if (instanceByteCount.IsIndeterminate)
                    {
                        // For USG types, they may be of indeterminate size, and the size of the type may be meaningless.
                        // In that case emit a fixed constant.
                        return MinimumObjectSize;
                    }
                }

                return base.BaseSize;
            }
        }

        protected override void ComputeValueTypeFieldPadding()
        {
            DefType defType = _type as DefType;

            // Types of indeterminate sizes don't have computed ValueTypeFieldPadding
            if (defType != null && defType.InstanceByteCount.IsIndeterminate)
            {
                Debug.Assert(_type.IsCanonicalSubtype(CanonicalFormKind.Universal));
                return;
            }

            base.ComputeValueTypeFieldPadding();
        }

        public override int ClassCode => -1798018602;
    }
}
