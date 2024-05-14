// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Collections.Generic;
using System.Diagnostics;
using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    public class SealedVTableNode : ObjectNode, ISymbolDefinitionNode
    {
        private readonly TypeDesc _type;
        private List<SealedVTableEntry> _sealedVTableEntries;
        private DependencyList _nonRelocationDependencies;

        public SealedVTableNode(TypeDesc type)
        {
            // Multidimensional arrays should not get a sealed vtable or a dispatch map. Runtime should use the
            // sealed vtable and dispatch map of the System.Array basetype instead.
            // Pointer arrays also follow the same path
            Debug.Assert(!type.IsArrayTypeWithoutGenericInterfaces());
            Debug.Assert(!type.IsRuntimeDeterminedSubtype);

            _type = type;
        }

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override ObjectNodeSection GetSection(NodeFactory factory) => _type.Context.Target.IsWindows ? ObjectNodeSection.FoldableReadOnlyDataSection : ObjectNodeSection.DataSection;

        public virtual void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix + "__SealedVTable_" + nameMangler.NodeMangler.MethodTable(_type));
        }

        int ISymbolNode.Offset => 0;
        int ISymbolDefinitionNode.Offset => 0;
        public override bool IsShareable => EETypeNode.IsTypeNodeShareable(_type);
        public override bool StaticDependenciesAreComputed => true;

        /// <summary>
        /// Returns the number of sealed vtable slots on the type. This API should only be called after successfully
        /// building the sealed vtable slots.
        /// </summary>
        public int NumSealedVTableEntries
        {
            get
            {
                if (_sealedVTableEntries == null)
                    throw new NotSupportedException();

                return _sealedVTableEntries.Count;
            }
        }

        public override bool ShouldSkipEmittingObjectNode(NodeFactory factory)
        {
            BuildSealedVTableSlots(factory, relocsOnly: false);
            return NumSealedVTableEntries == 0;
        }

        /// <summary>
        /// Returns the slot of a method in the sealed vtable, or -1 if not found. This API should only be called after
        /// successfully building the sealed vtable slots.
        /// </summary>
        public int ComputeSealedVTableSlot(MethodDesc method)
        {
            if (_sealedVTableEntries == null)
                throw new NotSupportedException();

            for (int i = 0; i < _sealedVTableEntries.Count; i++)
            {
                if (_sealedVTableEntries[i].Matches(method))
                    return i;
            }

            return -1;
        }

        public int ComputeDefaultInterfaceMethodSlot(MethodDesc method, DefType interfaceOnDefinition)
        {
            if (_sealedVTableEntries == null)
                throw new NotSupportedException();

            for (int i = 0; i < _sealedVTableEntries.Count; i++)
            {
                if (_sealedVTableEntries[i].Matches(method, interfaceOnDefinition))
                    return i;
            }

            return -1;
        }

        public bool BuildSealedVTableSlots(NodeFactory factory, bool relocsOnly)
        {
            // Sealed vtable already built
            if (_sealedVTableEntries != null)
                return true;

            DefType declType = _type.GetClosestDefType();
            VTableSliceNode declTypeVTable = factory.VTable(declType);

            // It's only okay to touch the actual list of slots if we're in the final emission phase
            // or the vtable is not built lazily.
            if (relocsOnly && !declTypeVTable.HasKnownVirtualMethodUse)
                return false;

            _sealedVTableEntries = new List<SealedVTableEntry>();

            // Interfaces don't have any instance virtual slots with the exception of interfaces that provide
            // IDynamicInterfaceCastable implementation.
            // Normal interface don't need one because the dispatch is done at the class level.
            // For IDynamicInterfaceCastable, we don't have an implementing class.
            bool isInterface = declType.IsInterface;
            bool needsEntriesForInstanceInterfaceMethodImpls = !isInterface
                    || ((MetadataType)declType).IsDynamicInterfaceCastableImplementation();

            IReadOnlyList<MethodDesc> virtualSlots = declTypeVTable.Slots;

            for (int i = 0; i < virtualSlots.Count; i++)
            {
                if (!declTypeVTable.IsSlotUsed(virtualSlots[i]))
                    continue;

                if (!virtualSlots[i].Signature.IsStatic && !needsEntriesForInstanceInterfaceMethodImpls)
                    continue;

                MethodDesc implMethod = declType.FindVirtualFunctionTargetMethodOnObjectType(virtualSlots[i]);

                if (implMethod.CanMethodBeInSealedVTable(factory))
                {
                    IMethodNode node;

                    if (factory.DelegateTargetVirtualMethod(virtualSlots[i].GetCanonMethodTarget(CanonicalFormKind.Specific)).Marked)
                        node = factory.AddressTakenMethodEntrypoint(implMethod.GetCanonMethodTarget(CanonicalFormKind.Specific), unboxingStub: !implMethod.Signature.IsStatic && declType.IsValueType);
                    else
                        node = factory.MethodEntrypoint(implMethod.GetCanonMethodTarget(CanonicalFormKind.Specific), unboxingStub: !implMethod.Signature.IsStatic && declType.IsValueType);

                    _sealedVTableEntries.Add(SealedVTableEntry.FromVirtualMethod(implMethod, node));
                }
            }

            TypeDesc declTypeDefinition = declType.GetTypeDefinition();

            DefType[] declTypeRuntimeInterfaces = declType.RuntimeInterfaces;
            DefType[] declTypeDefinitionRuntimeInterfaces = declTypeDefinition.RuntimeInterfaces;

            // Catch any runtime interface collapsing. We shouldn't have any
            Debug.Assert(declTypeRuntimeInterfaces.Length == declTypeDefinitionRuntimeInterfaces.Length);

            for (int interfaceIndex = 0; interfaceIndex < declTypeRuntimeInterfaces.Length; interfaceIndex++)
            {
                var interfaceType = declTypeRuntimeInterfaces[interfaceIndex];
                var definitionInterfaceType = declTypeDefinitionRuntimeInterfaces[interfaceIndex];

                VTableSliceNode interfaceVTable = factory.VTable(interfaceType);
                virtualSlots = interfaceVTable.Slots;

                for (int interfaceMethodSlot = 0; interfaceMethodSlot < virtualSlots.Count; interfaceMethodSlot++)
                {
                    MethodDesc declMethod = virtualSlots[interfaceMethodSlot];

                    if (!interfaceVTable.IsSlotUsed(declMethod))
                        continue;

                    if (!declMethod.Signature.IsStatic && !needsEntriesForInstanceInterfaceMethodImpls)
                        continue;

                    MethodDesc interfaceDefDeclMethod = declMethod;
                    if  (!interfaceType.IsTypeDefinition)
                        interfaceDefDeclMethod = factory.TypeSystemContext.GetMethodForInstantiatedType(declMethod.GetTypicalMethodDefinition(), (InstantiatedType)definitionInterfaceType);

                    var implMethod = declMethod.Signature.IsStatic ?
                        declTypeDefinition.ResolveInterfaceMethodToStaticVirtualMethodOnType(interfaceDefDeclMethod) :
                        declTypeDefinition.ResolveInterfaceMethodToVirtualMethodOnType(interfaceDefDeclMethod);

                    // Interface methods first implemented by a base type in the hierarchy will return null for the implMethod (runtime interface
                    // dispatch will walk the inheritance chain).
                    if (implMethod != null)
                    {
                        if (implMethod.Signature.IsStatic || !implMethod.OwningType.HasSameTypeDefinition(declType))
                        {
                            TypeDesc implType = declType;
                            while (!implType.HasSameTypeDefinition(implMethod.OwningType))
                                implType = implType.BaseType;

                            MethodDesc targetMethod = implMethod;
                            if (!implType.IsTypeDefinition)
                                targetMethod = factory.TypeSystemContext.GetMethodForInstantiatedType(implMethod.GetTypicalMethodDefinition(), (InstantiatedType)implType);

                            if (targetMethod.CanMethodBeInSealedVTable(factory) || implMethod.Signature.IsStatic)
                            {
                                IMethodNode node;
                                if (factory.DelegateTargetVirtualMethod(declMethod.GetCanonMethodTarget(CanonicalFormKind.Specific)).Marked)
                                    node = factory.AddressTakenMethodEntrypoint(targetMethod.GetCanonMethodTarget(CanonicalFormKind.Specific), unboxingStub: !targetMethod.Signature.IsStatic && declType.IsValueType);
                                else
                                    node = factory.MethodEntrypoint(targetMethod.GetCanonMethodTarget(CanonicalFormKind.Specific), unboxingStub: !targetMethod.Signature.IsStatic && declType.IsValueType);

                                _sealedVTableEntries.Add(SealedVTableEntry.FromVirtualMethod(targetMethod, node));
                            }
                        }
                    }
                    else
                    {
                        // If the interface method is provided by a default implementation, add the default implementation
                        // to the sealed vtable.
                        var resolution = declTypeDefinition.ResolveInterfaceMethodToDefaultImplementationOnType(interfaceDefDeclMethod, out implMethod);
                        if (resolution == DefaultInterfaceMethodResolution.DefaultImplementation)
                        {
                            DefType providingInterfaceDefinitionType = (DefType)implMethod.OwningType;
                            implMethod = implMethod.InstantiateSignature(declType.Instantiation, Instantiation.Empty);

                            MethodDesc canonImplMethod = implMethod.GetCanonMethodTarget(CanonicalFormKind.Specific);
                            if (canonImplMethod.IsCanonicalMethod(CanonicalFormKind.Any) && !canonImplMethod.Signature.IsStatic)
                            {
                                // Canonical instance default interface methods need to go through a thunk that acquires the generic context from `this`.
                                // Static methods have their generic context passed explicitly.
                                canonImplMethod = factory.TypeSystemContext.GetDefaultInterfaceMethodImplementationThunk(canonImplMethod, declType.ConvertToCanonForm(CanonicalFormKind.Specific), providingInterfaceDefinitionType);

                                // The above thunk will index into interface list to find the right context. Make sure to keep all interfaces prior to this one
                                for (int i = 0; i < interfaceIndex; i++)
                                {
                                    _nonRelocationDependencies ??= new DependencyList();
                                    _nonRelocationDependencies.Add(factory.InterfaceUse(declTypeRuntimeInterfaces[i].GetTypeDefinition()), "Interface with shared default methods folows this");
                                }
                            }

                            IMethodNode node;
                            if (factory.DelegateTargetVirtualMethod(declMethod.GetCanonMethodTarget(CanonicalFormKind.Specific)).Marked)
                                node = factory.AddressTakenMethodEntrypoint(canonImplMethod, unboxingStub: implMethod.OwningType.IsValueType && !implMethod.Signature.IsStatic);
                            else
                                node = factory.MethodEntrypoint(canonImplMethod, unboxingStub: implMethod.OwningType.IsValueType && !implMethod.Signature.IsStatic);

                            _sealedVTableEntries.Add(SealedVTableEntry.FromDefaultInterfaceMethod(implMethod, providingInterfaceDefinitionType, node));
                        }
                    }
                }
            }

            return true;
        }

        protected override DependencyList ComputeNonRelocationBasedDependencies(NodeFactory factory)
        {
            BuildSealedVTableSlots(factory, relocsOnly: true);

            var result = new DependencyList(_nonRelocationDependencies ?? []);

            // When building the sealed vtable, we consult the vtable layout of these types
            TypeDesc declType = _type.GetClosestDefType();
            result.Add(factory.VTable(declType), "VTable of the type");

            foreach (var interfaceType in declType.RuntimeInterfaces)
                result.Add(factory.VTable(interfaceType), "VTable of the interface");

            return result;
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly)
        {
            ObjectDataBuilder objData = new ObjectDataBuilder(factory, relocsOnly);
            objData.RequireInitialAlignment(factory.Target.SupportsRelativePointers ? 4 : factory.Target.PointerSize);
            objData.AddSymbol(this);

            if (BuildSealedVTableSlots(factory, relocsOnly))
            {
                for (int i = 0; i < _sealedVTableEntries.Count; i++)
                {
                    IMethodNode relocTarget = _sealedVTableEntries[i].Target;

                    if (factory.Target.SupportsRelativePointers)
                        objData.EmitReloc(relocTarget, RelocType.IMAGE_REL_BASED_RELPTR32);
                    else
                        objData.EmitPointerReloc(relocTarget);
                }
            }

            return objData.ToObjectData();
        }

        public override int ClassCode => 1632890252;
        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return comparer.Compare(_type, ((SealedVTableNode)other)._type);
        }

        private readonly struct SealedVTableEntry
        {
            private readonly MethodDesc _method;
            private readonly DefType _interfaceDefinition;
            public readonly IMethodNode Target;

            private SealedVTableEntry(MethodDesc method, DefType interfaceDefinition, IMethodNode target)
            {
                Debug.Assert(interfaceDefinition == null || method.GetTypicalMethodDefinition().OwningType == interfaceDefinition.GetTypeDefinition());
                (_method, _interfaceDefinition, Target) = (method, interfaceDefinition, target);
            }

            public static SealedVTableEntry FromVirtualMethod(MethodDesc method, IMethodNode target)
                => new SealedVTableEntry(method, null, target);

            public static SealedVTableEntry FromDefaultInterfaceMethod(MethodDesc method, DefType interfaceOnDefinition, IMethodNode target)
                => new SealedVTableEntry(method, interfaceOnDefinition, target);

            public bool Matches(MethodDesc method)
            {
                if (_method == method)
                {
                    // It is not valid to ask for slots of default implementations of interfaces on canonical version of the type.
                    //
                    // Consider:
                    //
                    // interface IFoo<T> { void Frob() => Console.WriteLine(typeof(T)) }
                    // class Base<T> : IFoo<T> { }
                    // class Derived<T, U> : Base<T>, IFoo<U> { }
                    //
                    // If we ask what's the slot of IFoo<__Canon>.Frob on Derived<__Canon, __Canon>, the answer is actually
                    // "two slots". We need extra data (the interface implementation on the definition of the type -
                    // e.g. "IFace<!0>") to disambiguate. Use the other overload.
                    Debug.Assert(_interfaceDefinition == null || !method.IsCanonicalMethod(CanonicalFormKind.Any));
                    return true;
                }

                return false;
            }

            public bool Matches(MethodDesc method, DefType interfaceDefinition)
            {
                Debug.Assert(method.GetTypicalMethodDefinition().OwningType == interfaceDefinition.GetTypeDefinition());
                Debug.Assert(interfaceDefinition.IsInterface);

                if (_method == method && _interfaceDefinition == interfaceDefinition)
                {
                    return true;
                }

                return false;
            }
        }
    }
}
