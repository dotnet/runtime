// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Internal.Runtime;
using Internal.Text;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    internal abstract class GenericDefinitionEETypeNode : EETypeNode
    {
        public GenericDefinitionEETypeNode(NodeFactory factory, TypeDesc type) : base(factory, type)
        {
            Debug.Assert(type.IsGenericDefinition);
        }

        public override bool HasConditionalStaticDependencies => false;

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory) => null;

        public override ISymbolNode NodeForLinkage(NodeFactory factory)
        {
            return factory.NecessaryTypeSymbol(_type);
        }

        protected override ObjectData GetDehydratableData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataBuilder dataBuilder = new ObjectDataBuilder(factory, relocsOnly);

            dataBuilder.RequireInitialPointerAlignment();
            dataBuilder.AddSymbol(this);

            uint flags = EETypeBuilderHelpers.ComputeFlags(_type);

            // Generic array enumerators use special variance rules recognized by the runtime
            // Runtime casting logic relies on all interface types implemented on arrays
            // to have the variant flag set.
            if (_type == factory.ArrayOfTEnumeratorType || factory.TypeSystemContext.IsGenericArrayInterfaceType(_type))
                flags |= (uint)EETypeFlags.GenericVarianceFlag;

            if (_type.IsByRefLike)
                flags |= (uint)EETypeFlagsEx.IsByRefLikeFlag;

            dataBuilder.EmitUInt(flags);
            dataBuilder.EmitInt(checked((ushort)_type.Instantiation.Length)); // Base size (we put instantiation length)
            dataBuilder.EmitZeroPointer();  // No related type
            dataBuilder.EmitShort(0);       // No VTable
            dataBuilder.EmitShort(0);       // No interface map
            dataBuilder.EmitInt(_type.GetHashCode());
            OutputTypeManagerIndirection(factory, ref dataBuilder);
            OutputWritableData(factory, ref dataBuilder);

            // Generic composition only meaningful if there's variance
            if ((flags & (uint)EETypeFlags.GenericVarianceFlag) != 0)
                OutputGenericInstantiationDetails(factory, ref dataBuilder);

            return dataBuilder.ToObjectData();
        }
    }

    internal sealed class ReflectionInvisibleGenericDefinitionEETypeNode : GenericDefinitionEETypeNode
    {
        public ReflectionInvisibleGenericDefinitionEETypeNode(NodeFactory factory, TypeDesc type) : base(factory, type)
        {
        }

        public override bool ShouldSkipEmittingObjectNode(NodeFactory factory)
        {
            return factory.ConstructedTypeSymbol(_type).Marked;
        }

        protected override DependencyList ComputeNonRelocationBasedDependencies(NodeFactory factory)
        {
            return new DependencyList();
        }

        public override int ClassCode => -287423988;
    }

    internal sealed class ReflectionVisibleGenericDefinitionEETypeNode : GenericDefinitionEETypeNode
    {
        public ReflectionVisibleGenericDefinitionEETypeNode(NodeFactory factory, TypeDesc type) : base(factory, type)
        {
        }

        public override bool ShouldSkipEmittingObjectNode(NodeFactory factory)
        {
            return false;
        }

        protected override FrozenRuntimeTypeNode GetFrozenRuntimeTypeNode(NodeFactory factory)
        {
            return factory.SerializedConstructedRuntimeTypeObject(_type);
        }

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler) + " reflection visible";

        protected override DependencyList ComputeNonRelocationBasedDependencies(NodeFactory factory)
        {
            var dependencyList = new DependencyList();

            dependencyList.Add(factory.NecessaryTypeSymbol(_type), "Reflection invisible type for a visible type");

            // Ask the metadata manager if we have any dependencies due to the presence of the EEType.
            factory.MetadataManager.GetDependenciesDueToEETypePresence(ref dependencyList, factory, _type);

            return dependencyList;
        }

        public override int ClassCode => 983279111;
    }
}
