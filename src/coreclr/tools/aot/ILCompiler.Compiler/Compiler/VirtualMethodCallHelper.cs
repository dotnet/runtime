// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using Internal.TypeSystem;
using ILCompiler.DependencyAnalysis;

namespace ILCompiler
{
    public static class VirtualMethodSlotHelper
    {
        public static int GetDefaultInterfaceMethodSlot(NodeFactory factory, MethodDesc method, TypeDesc implType, DefType interfaceOnDefinition, bool countDictionarySlots = true)
        {
            Debug.Assert(method.GetTypicalMethodDefinition().OwningType == interfaceOnDefinition.GetTypeDefinition());

            SealedVTableNode sealedVTable = factory.SealedVTable(implType);

            // Ensure the sealed vtable is built before computing the slot
            sealedVTable.BuildSealedVTableSlots(factory, relocsOnly: false /* GetVirtualMethodSlot is called in the final emission phase */);

            int sealedVTableSlot = sealedVTable.ComputeDefaultInterfaceMethodSlot(method, interfaceOnDefinition);
            if (sealedVTableSlot == -1)
                return -1;

            int numVTableSlots = GetNumberOfSlotsInCurrentType(factory, implType, countDictionarySlots);

            return numVTableSlots + sealedVTableSlot;
        }

        /// <summary>
        /// Given a virtual method decl, return its VTable slot if the method is used on its containing type.
        /// Return -1 if the virtual method is not used.
        /// </summary>
        public static int GetVirtualMethodSlot(NodeFactory factory, MethodDesc method, TypeDesc implType, bool countDictionarySlots = true)
        {
            if (method.CanMethodBeInSealedVTable(factory))
            {
                // If the method is a sealed newslot method, it will be put in the sealed vtable instead of the type's vtable. In this
                // case, the slot index return should be the index in the sealed vtable, plus the total number of vtable slots.

                // First, make sure we are not attempting to resolve the slot of a sealed vtable method on a special array type, which
                // does not get any sealed vtable entries
                Debug.Assert(!implType.IsArrayTypeWithoutGenericInterfaces());

                SealedVTableNode sealedVTable = factory.SealedVTable(implType);

                // Ensure the sealed vtable is built before computing the slot
                sealedVTable.BuildSealedVTableSlots(factory, relocsOnly: false /* GetVirtualMethodSlot is called in the final emission phase */);

                int sealedVTableSlot = sealedVTable.ComputeSealedVTableSlot(method);
                if (sealedVTableSlot == -1)
                    return -1;

                int numVTableSlots = GetNumberOfSlotsInCurrentType(factory, implType, countDictionarySlots);

                return numVTableSlots + sealedVTableSlot;
            }
            else
            {
                // TODO: More efficient lookup of the slot
                TypeDesc owningType = method.OwningType;
                int baseSlots = GetNumberOfBaseSlots(factory, owningType, countDictionarySlots);

                // For types that have a generic dictionary, the introduced virtual method slots are
                // prefixed with a pointer to the generic dictionary.
                if (owningType.HasGenericDictionarySlot() && countDictionarySlots)
                    baseSlots++;

                IReadOnlyList<MethodDesc> virtualSlots = factory.VTable(owningType).Slots;
                int methodSlot = -1;
                int numSealedVTableEntries = 0;
                for (int slot = 0; slot < virtualSlots.Count; slot++)
                {
                    if (virtualSlots[slot].CanMethodBeInSealedVTable(factory))
                    {
                        numSealedVTableEntries++;
                        continue;
                    }

                    if (virtualSlots[slot] == method)
                    {
                        methodSlot = slot;
                        break;
                    }
                }

                return methodSlot == -1 ? -1 : baseSlots + methodSlot - numSealedVTableEntries;
            }
        }

        private static int GetNumberOfSlotsInCurrentType(NodeFactory factory, TypeDesc implType, bool countDictionarySlots)
        {
            if (implType.IsInterface)
            {
                // Interface types don't have physically assigned virtual slots, so the number of slots
                // is always 0. They may have sealed slots.
                return (implType.HasGenericDictionarySlot() && countDictionarySlots) ? 1 : 0;
            }

            // Now compute the total number of vtable slots that would exist on the type
            int baseSlots = GetNumberOfBaseSlots(factory, implType, countDictionarySlots);

            // For types that have a generic dictionary, the introduced virtual method slots are
            // prefixed with a pointer to the generic dictionary.
            if (implType.HasGenericDictionarySlot() && countDictionarySlots)
                baseSlots++;

            int numVTableSlots = baseSlots;
            IReadOnlyList<MethodDesc> virtualSlots = factory.VTable(implType).Slots;
            for (int slot = 0; slot < virtualSlots.Count; slot++)
            {
                if (virtualSlots[slot].CanMethodBeInSealedVTable(factory))
                    continue;
                numVTableSlots++;
            }

            return numVTableSlots;
        }

        private static int GetNumberOfBaseSlots(NodeFactory factory, TypeDesc owningType, bool countDictionarySlots)
        {
            int baseSlots = 0;

            TypeDesc baseType = owningType.BaseType;
            TypeDesc templateBaseType = owningType.ConvertToCanonForm(CanonicalFormKind.Specific).BaseType;

            while (baseType != null)
            {
                // Normalize the base type. Necessary to make this work with the lazy vtable slot
                // concept - if we start with a canonical type, the base type could end up being
                // something like Base<__Canon, string>. We would get "0 slots used" for weird
                // base types like this.
                baseType = baseType.ConvertToCanonForm(CanonicalFormKind.Specific);
                templateBaseType = templateBaseType.ConvertToCanonForm(CanonicalFormKind.Specific);

                //
                // In the universal canonical types case, we could have base types in the hierarchy that are partial universal canonical types.
                // The presence of these types could cause incorrect vtable layouts, so we need to fully canonicalize them and walk the
                // hierarchy of the template type of the original input type to detect these cases.
                //
                // Exmaple: we begin with Derived<__UniversalCanon> and walk the template hierarchy:
                //
                //    class Derived<T> : Middle<T, MyStruct> { }    // -> Template is Derived<__UniversalCanon> and needs a dictionary slot
                //                                                  // -> Basetype tempalte is Middle<__UniversalCanon, MyStruct>. It's a partial
                //                                                        Universal canonical type, so we need to fully canonicalize it.
                //
                //    class Middle<T, U> : Base<U> { }              // -> Template is Middle<__UniversalCanon, __UniversalCanon> and needs a dictionary slot
                //                                                  // -> Basetype template is Base<__UniversalCanon>
                //
                //    class Base<T> { }                             // -> Template is Base<__UniversalCanon> and needs a dictionary slot.
                //
                // If we had not fully canonicalized the Middle class template, we would have ended up with Base<MyStruct>, which does not need
                // a dictionary slot, meaning we would have created a vtable layout that the runtime does not expect.
                //

                // For types that have a generic dictionary, the introduced virtual method slots are
                // prefixed with a pointer to the generic dictionary.
                if ((baseType.HasGenericDictionarySlot() || templateBaseType.HasGenericDictionarySlot()) && countDictionarySlots)
                    baseSlots++;

                IReadOnlyList<MethodDesc> baseVirtualSlots = factory.VTable(baseType).Slots;
                foreach (var vtableMethod in baseVirtualSlots)
                {
                    // Methods in the sealed vtable should be excluded from the count
                    if (vtableMethod.CanMethodBeInSealedVTable(factory))
                        continue;
                    baseSlots++;
                }

                baseType = baseType.BaseType;
                templateBaseType = templateBaseType.BaseType;
            }

            return baseSlots;
        }

        /// <summary>
        /// Gets the vtable slot that holds the generic dictionary of this type.
        /// </summary>
        public static int GetGenericDictionarySlot(NodeFactory factory, TypeDesc type)
        {
            Debug.Assert(type.HasGenericDictionarySlot());
            return GetNumberOfBaseSlots(factory, type, countDictionarySlots: true);
        }

        /// <summary>
        /// Gets a value indicating whether the virtual method slots introduced by this type are prefixed
        /// by a pointer to the generic dictionary of the type.
        /// </summary>
        public static bool HasGenericDictionarySlot(this TypeDesc type)
        {
            // Dictionary slots on generic interfaces are necessary to support static methods on interfaces
            // The reason behind making this unconditional is simplicity, and keeping method slot indices for methods on IFoo<int>
            // and IFoo<string> identical. That won't change.
            if (type.IsInterface)
                return type.HasInstantiation;

            return type.HasInstantiation &&
                (type.ConvertToCanonForm(CanonicalFormKind.Specific) != type || type.IsCanonicalSubtype(CanonicalFormKind.Any));
        }
    }
}
