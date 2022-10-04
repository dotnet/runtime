// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Internal.Text;
using Internal.TypeSystem;
using Internal.Runtime;

namespace ILCompiler.DependencyAnalysis
{
    public class InterfaceDispatchMapNode : ObjectNode, ISymbolDefinitionNode, ISortableSymbolNode
    {
        private readonly TypeDesc _type;

        public InterfaceDispatchMapNode(NodeFactory factory, TypeDesc type)
        {
            // Multidimensional arrays should not get a sealed vtable or a dispatch map. Runtime should use the
            // sealed vtable and dispatch map of the System.Array basetype instead.
            // Pointer arrays also follow the same path
            Debug.Assert(!type.IsArrayTypeWithoutGenericInterfaces());
            Debug.Assert(MightHaveInterfaceDispatchMap(type, factory));
            Debug.Assert(type.ConvertToCanonForm(CanonicalFormKind.Specific) == type);

            _type = type;
        }

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("__InterfaceDispatchMap_").Append(nameMangler.SanitizeName(nameMangler.GetMangledTypeName(_type)));
        }

        public int Offset => 0;
        public override bool IsShareable => false;

        public override bool StaticDependenciesAreComputed => true;

        public override ObjectNodeSection Section
        {
            get
            {
                if (_type.Context.Target.IsWindows)
                    return ObjectNodeSection.FoldableReadOnlyDataSection;
                else
                    return ObjectNodeSection.DataSection;
            }
        }

        protected override DependencyList ComputeNonRelocationBasedDependencies(NodeFactory factory)
        {
            var result = new DependencyList();
            result.Add(factory.InterfaceDispatchMapIndirection(_type), "Interface dispatch map indirection node");

            // VTable slots of implemented interfaces are consulted during emission
            foreach (TypeDesc runtimeInterface in _type.RuntimeInterfaces)
            {
                result.Add(factory.VTable(runtimeInterface), "Interface for a dispatch map");
            }

            return result;
        }

        /// <summary>
        /// Gets a value indicating whether '<paramref name="type"/>' might have a non-empty dispatch map.
        /// Note that this is only an approximation because we might not be able to take into account
        /// whether the interface methods are actually used.
        /// </summary>
        public static bool MightHaveInterfaceDispatchMap(TypeDesc type, NodeFactory factory)
        {
            if (type.IsArrayTypeWithoutGenericInterfaces())
                return false;

            if (!type.IsArray && !type.IsDefType)
                return false;

            // Interfaces don't have a dispatch map because we dispatch them based on the
            // dispatch map of the implementing class.
            // The only exception are IDynamicInterfaceCastable scenarios that dispatch
            // using the interface dispatch map.
            // We generate the dispatch map irrespective of whether the interface actually
            // implements any methods (we don't run the for loop below) so that at runtime
            // we can distinguish between "the interface returned by IDynamicInterfaceCastable
            // wasn't marked as [DynamicInterfaceCastableImplementation]" and "we couldn't find an
            // implementation". We don't want to use the custom attribute for that at runtime because
            // that's reflection and this should work without reflection.
            if (type.IsInterface)
                return ((MetadataType)type).IsDynamicInterfaceCastableImplementation();

            TypeDesc declType = type.GetClosestDefType();

            for (int interfaceIndex = 0; interfaceIndex < declType.RuntimeInterfaces.Length; interfaceIndex++)
            {
                DefType interfaceType = declType.RuntimeInterfaces[interfaceIndex];
                InstantiatedType interfaceOnDefinitionType = interfaceType.IsTypeDefinition ?
                    null :
                    (InstantiatedType)declType.GetTypeDefinition().RuntimeInterfaces[interfaceIndex];

                IEnumerable<MethodDesc> slots;

                // If the vtable has fixed slots, we can query it directly.
                // If it's a lazily built vtable, we might not be able to query slots
                // just yet, so approximate by looking at all methods.
                VTableSliceNode vtableSlice = factory.VTable(interfaceType);
                if (vtableSlice.HasFixedSlots)
                    slots = vtableSlice.Slots;
                else
                    slots = interfaceType.GetAllVirtualMethods();

                foreach (MethodDesc slotMethod in slots)
                {
                    MethodDesc declMethod = slotMethod;

                    Debug.Assert(declMethod.IsVirtual);

                    if (interfaceOnDefinitionType != null)
                        declMethod = factory.TypeSystemContext.GetMethodForInstantiatedType(declMethod.GetTypicalMethodDefinition(), interfaceOnDefinitionType);

                    var implMethod = declMethod.Signature.IsStatic ?
                        declType.GetTypeDefinition().ResolveInterfaceMethodToStaticVirtualMethodOnType(declMethod) :
                        declType.GetTypeDefinition().ResolveInterfaceMethodToVirtualMethodOnType(declMethod);
                    if (implMethod != null)
                    {
                        return true;
                    }
                    else
                    {
                        DefaultInterfaceMethodResolution result = declType.ResolveInterfaceMethodToDefaultImplementationOnType(slotMethod, out _);
                        if (result != DefaultInterfaceMethodResolution.None)
                            return true;
                    }
                }
            }

            return false;
        }

        private void EmitDispatchMap(ref ObjectDataBuilder builder, NodeFactory factory)
        {
            var entryCountReservation = builder.ReserveShort();
            var defaultEntryCountReservation = builder.ReserveShort();
            var staticEntryCountReservation = builder.ReserveShort();
            var defaultStaticEntryCountReservation = builder.ReserveShort();
            int entryCount = 0;

            TypeDesc declType = _type.GetClosestDefType();
            TypeDesc declTypeDefinition = declType.GetTypeDefinition();
            DefType[] declTypeRuntimeInterfaces = declType.RuntimeInterfaces;
            DefType[] declTypeDefinitionRuntimeInterfaces = declTypeDefinition.RuntimeInterfaces;

            // Catch any runtime interface collapsing. We shouldn't have any
            Debug.Assert(declTypeRuntimeInterfaces.Length == declTypeDefinitionRuntimeInterfaces.Length);

            var defaultImplementations = new List<(int InterfaceIndex, int InterfaceMethodSlot, int ImplMethodSlot)>();
            var staticImplementations = new List<(int InterfaceIndex, int InterfaceMethodSlot, int ImplMethodSlot, int Context)>();
            var staticDefaultImplementations = new List<(int InterfaceIndex, int InterfaceMethodSlot, int ImplMethodSlot, int Context)>();

            // Resolve all the interfaces, but only emit non-static and non-default implementations
            for (int interfaceIndex = 0; interfaceIndex < declTypeRuntimeInterfaces.Length; interfaceIndex++)
            {
                var interfaceType = declTypeRuntimeInterfaces[interfaceIndex];
                var interfaceDefinitionType = declTypeDefinitionRuntimeInterfaces[interfaceIndex];
                Debug.Assert(interfaceType.IsInterface);

                IReadOnlyList<MethodDesc> virtualSlots = factory.VTable(interfaceType).Slots;

                for (int interfaceMethodSlot = 0; interfaceMethodSlot < virtualSlots.Count; interfaceMethodSlot++)
                {
                    MethodDesc declMethod = virtualSlots[interfaceMethodSlot];
                    if(!interfaceType.IsTypeDefinition)
                        declMethod = factory.TypeSystemContext.GetMethodForInstantiatedType(declMethod.GetTypicalMethodDefinition(), (InstantiatedType)interfaceDefinitionType);

                    var implMethod = declMethod.Signature.IsStatic ?
                        declTypeDefinition.ResolveInterfaceMethodToStaticVirtualMethodOnType(declMethod) :
                        declTypeDefinition.ResolveInterfaceMethodToVirtualMethodOnType(declMethod);

                    // Interface methods first implemented by a base type in the hierarchy will return null for the implMethod (runtime interface
                    // dispatch will walk the inheritance chain).
                    if (implMethod != null)
                    {
                        TypeDesc implType = declType;
                        while (!implType.HasSameTypeDefinition(implMethod.OwningType))
                            implType = implType.BaseType;

                        MethodDesc targetMethod = implMethod;
                        if (!implType.IsTypeDefinition)
                            targetMethod = factory.TypeSystemContext.GetMethodForInstantiatedType(implMethod.GetTypicalMethodDefinition(), (InstantiatedType)implType);

                        int emittedInterfaceSlot = interfaceMethodSlot + (interfaceType.HasGenericDictionarySlot() ? 1 : 0);
                        int emittedImplSlot = VirtualMethodSlotHelper.GetVirtualMethodSlot(factory, targetMethod, declType);
                        if (targetMethod.Signature.IsStatic)
                        {
                            // If this is a static virtual, also remember whether we need generic context.
                            // The implementation is not callable without the generic context.
                            // Instance methods acquire the generic context from `this` and don't need it.
                            // The pointer to the generic context is stored in the owning type's vtable.
                            int genericContext = targetMethod.GetCanonMethodTarget(CanonicalFormKind.Specific).RequiresInstArg()
                                ? StaticVirtualMethodContextSource.ContextFromThisClass
                                : StaticVirtualMethodContextSource.None;
                            staticImplementations.Add((interfaceIndex, emittedInterfaceSlot, emittedImplSlot, genericContext));
                        }
                        else
                        {
                            builder.EmitShort((short)checked((ushort)interfaceIndex));
                            builder.EmitShort((short)checked((ushort)emittedInterfaceSlot));
                            builder.EmitShort((short)checked((ushort)emittedImplSlot));
                            entryCount++;
                        }
                    }
                    else
                    {
                        // Is there a default implementation?

                        int? implSlot = null;

                        DefaultInterfaceMethodResolution result = declTypeDefinition.ResolveInterfaceMethodToDefaultImplementationOnType(declMethod, out implMethod);
                        DefType providingInterfaceDefinitionType = null;
                        if (result == DefaultInterfaceMethodResolution.DefaultImplementation)
                        {
                            providingInterfaceDefinitionType = (DefType)implMethod.OwningType;
                            implMethod = implMethod.InstantiateSignature(declType.Instantiation, Instantiation.Empty);
                            implSlot = VirtualMethodSlotHelper.GetDefaultInterfaceMethodSlot(factory, implMethod, declType, providingInterfaceDefinitionType);
                        }
                        else if (result == DefaultInterfaceMethodResolution.Reabstraction)
                        {
                            implSlot = SpecialDispatchMapSlot.Reabstraction;
                        }
                        else if (result == DefaultInterfaceMethodResolution.Diamond)
                        {
                            implSlot = SpecialDispatchMapSlot.Diamond;
                        }

                        if (implSlot.HasValue)
                        {
                            int emittedInterfaceSlot = interfaceMethodSlot + (interfaceType.HasGenericDictionarySlot() ? 1 : 0);
                            if (declMethod.Signature.IsStatic)
                            {
                                int genericContext = StaticVirtualMethodContextSource.None;
                                if (result == DefaultInterfaceMethodResolution.DefaultImplementation &&
                                    implMethod.GetCanonMethodTarget(CanonicalFormKind.Specific).RequiresInstArg())
                                {
                                    // If this is a static virtual, also remember whether we need generic context.
                                    // The implementation is not callable without the generic context.
                                    // Instance methods acquire the generic context from `this` and don't need it.
                                    // For default interface methods, the generic context is acquired by indexing
                                    // into the interface list of the owning type.
                                    Debug.Assert(providingInterfaceDefinitionType != null);
                                    int indexOfInterface = Array.IndexOf(declTypeDefinitionRuntimeInterfaces, providingInterfaceDefinitionType);
                                    Debug.Assert(indexOfInterface >= 0);
                                    genericContext = StaticVirtualMethodContextSource.ContextFromFirstInterface + indexOfInterface;
                                }
                                staticDefaultImplementations.Add((
                                    interfaceIndex,
                                    emittedInterfaceSlot,
                                    implSlot.Value,
                                    genericContext));
                            }
                            else
                            {
                                defaultImplementations.Add((
                                    interfaceIndex,
                                    emittedInterfaceSlot,
                                    implSlot.Value));
                            }
                        }
                    }
                }
            }

            // Now emit the default implementations
            foreach (var defaultImplementation in defaultImplementations)
            {
                builder.EmitShort((short)checked((ushort)defaultImplementation.InterfaceIndex));
                builder.EmitShort((short)checked((ushort)defaultImplementation.InterfaceMethodSlot));
                builder.EmitShort((short)checked((ushort)defaultImplementation.ImplMethodSlot));
            }

            // Now emit the static implementations
            foreach (var staticImplementation in staticImplementations)
            {
                builder.EmitShort((short)checked((ushort)staticImplementation.InterfaceIndex));
                builder.EmitShort((short)checked((ushort)staticImplementation.InterfaceMethodSlot));
                builder.EmitShort((short)checked((ushort)staticImplementation.ImplMethodSlot));
                builder.EmitShort((short)checked((ushort)staticImplementation.Context));
            }

            // Now emit the static default implementations
            foreach (var staticImplementation in staticDefaultImplementations)
            {
                builder.EmitShort((short)checked((ushort)staticImplementation.InterfaceIndex));
                builder.EmitShort((short)checked((ushort)staticImplementation.InterfaceMethodSlot));
                builder.EmitShort((short)checked((ushort)staticImplementation.ImplMethodSlot));
                builder.EmitShort((short)checked((ushort)staticImplementation.Context));
            }

            // Update the header
            builder.EmitShort(entryCountReservation, (short)checked((ushort)entryCount));
            builder.EmitShort(defaultEntryCountReservation, (short)checked((ushort)defaultImplementations.Count));
            builder.EmitShort(staticEntryCountReservation, (short)checked((ushort)staticImplementations.Count));
            builder.EmitShort(defaultStaticEntryCountReservation, (short)checked((ushort)staticDefaultImplementations.Count));
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataBuilder objData = new ObjectDataBuilder(factory, relocsOnly);
            objData.RequireInitialAlignment(2);
            objData.AddSymbol(this);

            if (!relocsOnly)
            {
                EmitDispatchMap(ref objData, factory);
            }

            return objData.ToObjectData();
        }

        public override int ClassCode => 848664602;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return comparer.Compare(_type, ((InterfaceDispatchMapNode)other)._type);
        }
    }
}
