// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.TypeSystem;
using ILCompiler.DependencyAnalysisFramework;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents the VTable for a type's slice. For example, System.String's VTableSliceNode includes virtual
    /// slots added by System.String itself, System.Object's VTableSliceNode contains the virtuals it defines.
    /// </summary>
    public abstract class VTableSliceNode : DependencyNodeCore<NodeFactory>
    {
        protected TypeDesc _type;

        public VTableSliceNode(TypeDesc type)
        {
            Debug.Assert(!type.IsArray, "Wanted to call GetClosestDefType?");
            Debug.Assert(!type.IsGenericDefinition);
            _type = type;
        }

        public abstract IReadOnlyList<MethodDesc> Slots
        {
            get;
        }

        public abstract bool IsSlotUsed(MethodDesc slot);

        public TypeDesc Type => _type;

        /// <summary>
        /// Gets a value indicating whether slots are assigned and known during dependency analysis.
        /// </summary>
        public abstract bool HasAssignedSlots
        {
            get;
        }

        /// <summary>
        /// Gets a value indicating whether <see cref="VirtualMethodUseNode"> is needed to track virtual method uses
        /// in this vtable slice.
        /// </summary>
        public abstract bool HasKnownVirtualMethodUse
        {
            get;
        }

        protected override string GetName(NodeFactory factory) => $"__vtable_{factory.NameMangler.GetMangledTypeName(_type)}";

        public override bool StaticDependenciesAreComputed => true;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            if (_type.HasBaseType)
            {
                yield return new DependencyListEntry(factory.VTable(_type.BaseType), "Base type VTable");
            }

            TypeDesc canonType = _type.ConvertToCanonForm(CanonicalFormKind.Specific);
            if (_type != canonType)
            {
                yield return new DependencyListEntry(factory.VTable(canonType), "Canonical type VTable");
            }
        }

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory) => null;

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
    }

    /// <summary>
    /// Represents a VTable slice with fixed slots whose assignment was determined at the time the slice was allocated.
    /// </summary>
    internal class PrecomputedVTableSliceNode : VTableSliceNode
    {
        private readonly MethodDesc[] _slots;

        public PrecomputedVTableSliceNode(TypeDesc type, MethodDesc[] slots)
            : base(type)
        {
            _slots = slots;
        }

        public override IReadOnlyList<MethodDesc> Slots
        {
            get
            {
                return _slots;
            }
        }

        public override bool IsSlotUsed(MethodDesc slot)
        {
            Debug.Assert(Array.IndexOf(_slots, slot) != -1);
            return true;
        }

        public override bool HasKnownVirtualMethodUse
        {
            get
            {
                return true;
            }
        }

        public override bool HasAssignedSlots
        {
            get
            {
                return true;
            }
        }
    }

    /// <summary>
    /// Represents a VTable slice for a complete type - a type with all virtual method slots generated,
    /// irrespective of whether they are used.
    /// </summary>
    internal sealed class EagerlyBuiltVTableSliceNode : PrecomputedVTableSliceNode
    {
        public EagerlyBuiltVTableSliceNode(TypeDesc type)
            : base(type, ComputeSlots(type))
        {
        }

        private static MethodDesc[] ComputeSlots(TypeDesc type)
        {
            var slots = default(ArrayBuilder<MethodDesc>);

            bool isObjectType = type.IsObject;
            DefType defType = type.GetClosestDefType();

            IEnumerable<MethodDesc> allSlots = type.IsInterface ?
                type.GetAllVirtualMethods() : defType.EnumAllVirtualSlots();

            foreach (var method in allSlots)
            {
                // GVMs are not emitted in the type's vtable.
                if (method.HasInstantiation)
                    continue;

                // Finalizers are called via a field on the MethodTable, not through the VTable
                if (isObjectType && method.Name == "Finalize")
                    continue;

                // Current type doesn't define this slot.
                if (method.OwningType != defType)
                    continue;

                slots.Add(method);
            }

            return slots.ToArray();
        }
    }

    /// <summary>
    /// Represents a VTable slice where slots are built on demand. Only the slots that are actually used
    /// will be generated.
    /// </summary>
    internal sealed class LazilyBuiltVTableSliceNode : VTableSliceNode
    {
        private HashSet<MethodDesc> _usedMethods = new HashSet<MethodDesc>();
        private MethodDesc[] _slots;
#if DEBUG
        private bool _isLocked;
#endif

        public LazilyBuiltVTableSliceNode(TypeDesc type, IReadOnlyList<MethodDesc> slots = null)
            : base(type)
        {
            if (slots != null)
            {
                _slots = new MethodDesc[slots.Count];
                for (int i = 0; i < _slots.Length; i++)
                    _slots[i] = slots[i];
            }
        }

        public override IReadOnlyList<MethodDesc> Slots
        {
            get
            {
                if (_slots == null)
                {
                    // Sort the lazily populated slots in metadata order (the order in which they show up
                    // in GetAllMethods()).
                    // This ensures that Foo<string> and Foo<object> will end up with the same vtable
                    // no matter the order in which VirtualMethodUse nodes populated it.
                    ArrayBuilder<MethodDesc> slotsBuilder = default(ArrayBuilder<MethodDesc>);
                    DefType defType = _type.GetClosestDefType();
                    foreach (var method in defType.GetAllMethods())
                    {
                        if (_usedMethods.Contains(method))
                            slotsBuilder.Add(method);
                    }
                    Debug.Assert(_usedMethods.Count == slotsBuilder.Count);
                    _slots = slotsBuilder.ToArray();
#if DEBUG
                    _isLocked = true;
#endif
                }

                return _slots;
            }
        }

        public override bool IsSlotUsed(MethodDesc slot)
        {
#if DEBUG
            // This has two effects: validates we're not asking about non-sensical method
            // and locking Slots so that AddEntry is not possible to call anymore.
            IReadOnlyList<MethodDesc> slots = Slots;
            bool contains = false;
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] == slot)
                {
                    contains = true;
                    break;
                }
            }
            Debug.Assert(contains);
#endif
            return _usedMethods.Contains(slot);
        }

        public override bool HasKnownVirtualMethodUse
        {
            get
            {
                return false;
            }
        }

        public override bool HasAssignedSlots
        {
            get
            {
                return _slots != null;
            }
        }

        public void AddEntry(MethodDesc virtualMethod)
        {
            // GVMs are not emitted in the type's vtable.
            Debug.Assert(!virtualMethod.HasInstantiation);
            Debug.Assert(virtualMethod.IsVirtual);
            Debug.Assert(virtualMethod.OwningType == _type);
#if DEBUG
            Debug.Assert(!_isLocked);
            Debug.Assert(_slots == null || Array.IndexOf(_slots, virtualMethod) != -1);
#endif

            // Finalizers are called via a field on the MethodTable, not through the VTable
            if (_type.IsObject && virtualMethod.Name == "Finalize")
                return;

            _usedMethods.Add(virtualMethod);
        }

        public override bool HasConditionalStaticDependencies
        {
            get
            {
                return _type.ConvertToCanonForm(CanonicalFormKind.Specific) != _type;
            }
        }

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory)
        {
            // VirtualMethodUse of Foo<SomeType>.Method will bring in VirtualMethodUse
            // of Foo<__Canon>.Method. This in turn should bring in Foo<OtherType>.Method.
            DefType defType = _type.GetClosestDefType();

            IEnumerable<MethodDesc> allSlots = _type.IsInterface ?
                _type.GetAllVirtualMethods() : defType.EnumAllVirtualSlots();

            foreach (var method in allSlots)
            {
                // Generic virtual methods are tracked by an orthogonal mechanism.
                if (method.HasInstantiation)
                    continue;

                // Current type doesn't define this slot. Another VTableSlice will take care of this.
                if (method.OwningType != defType)
                    continue;

                if (defType.Context.SupportsCanon)
                    yield return new CombinedDependencyListEntry(
                        factory.VirtualMethodUse(method),
                        factory.VirtualMethodUse(method.GetCanonMethodTarget(CanonicalFormKind.Specific)),
                        "Canonically equivalent virtual method use");

                if (defType.Context.SupportsUniversalCanon)
                    yield return new CombinedDependencyListEntry(
                        factory.VirtualMethodUse(method),
                        factory.VirtualMethodUse(method.GetCanonMethodTarget(CanonicalFormKind.Universal)),
                        "Universal Canonically equivalent virtual method use");
            }
        }
    }
}
