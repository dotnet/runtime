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

        public override ObjectNodeSection Section => _type.Context.Target.IsWindows ? ObjectNodeSection.FoldableReadOnlyDataSection : ObjectNodeSection.DataSection;

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

            TypeDesc declType = _type.GetClosestDefType();

            // It's only okay to touch the actual list of slots if we're in the final emission phase
            // or the vtable is not built lazily.
            if (relocsOnly && !factory.VTable(declType).HasFixedSlots)
                return false;

            _sealedVTableEntries = new List<SealedVTableEntry>();

            // Interfaces don't have any virtual slots with the exception of interfaces that provide
            // IDynamicInterfaceCastable implementation.
            // Normal interface don't need one because the dispatch is done at the class level.
            // For IDynamicInterfaceCastable, we don't have an implementing class.
            if (_type.IsInterface && !((MetadataType)_type).IsDynamicInterfaceCastableImplementation())
                return true;

            IReadOnlyList<MethodDesc> virtualSlots = factory.VTable(declType).Slots;

            for (int i = 0; i < virtualSlots.Count; i++)
            {
                MethodDesc implMethod = declType.FindVirtualFunctionTargetMethodOnObjectType(virtualSlots[i]);

                if (implMethod.CanMethodBeInSealedVTable())
                    _sealedVTableEntries.Add(SealedVTableEntry.FromVirtualMethod(implMethod));
            }

            TypeDesc declTypeDefinition = declType.GetTypeDefinition();

            DefType[] declTypeRuntimeInterfaces = declType.RuntimeInterfaces;
            DefType[] declTypeDefinitionRuntimeInterfaces = declTypeDefinition.RuntimeInterfaces;

            // Catch any runtime interface collapsing. We shouldn't have any
            Debug.Assert(declTypeRuntimeInterfaces.Length == declTypeDefinitionRuntimeInterfaces.Length);

            for (int interfaceIndex = 0; interfaceIndex < declTypeRuntimeInterfaces.Length; interfaceIndex++)
            {
                var interfaceType = declTypeRuntimeInterfaces[interfaceIndex];
                var interfaceDefinitionType = declTypeDefinitionRuntimeInterfaces[interfaceIndex];

                virtualSlots = factory.VTable(interfaceType).Slots;

                for (int interfaceMethodSlot = 0; interfaceMethodSlot < virtualSlots.Count; interfaceMethodSlot++)
                {
                    MethodDesc declMethod = virtualSlots[interfaceMethodSlot];
                    if  (!interfaceType.IsTypeDefinition)
                        declMethod = factory.TypeSystemContext.GetMethodForInstantiatedType(declMethod.GetTypicalMethodDefinition(), (InstantiatedType)interfaceDefinitionType);

                    var implMethod = declMethod.Signature.IsStatic ?
                        declTypeDefinition.ResolveInterfaceMethodToStaticVirtualMethodOnType(declMethod) :
                        declTypeDefinition.ResolveInterfaceMethodToVirtualMethodOnType(declMethod);

                    // Interface methods first implemented by a base type in the hierarchy will return null for the implMethod (runtime interface
                    // dispatch will walk the inheritance chain).
                    if (implMethod != null)
                    {
                        if (implMethod.Signature.IsStatic ||
                            (implMethod.CanMethodBeInSealedVTable() && !implMethod.OwningType.HasSameTypeDefinition(declType)))
                        {
                            TypeDesc implType = declType;
                            while (!implType.HasSameTypeDefinition(implMethod.OwningType))
                                implType = implType.BaseType;

                            MethodDesc targetMethod = implMethod;
                            if (!implType.IsTypeDefinition)
                                targetMethod = factory.TypeSystemContext.GetMethodForInstantiatedType(implMethod.GetTypicalMethodDefinition(), (InstantiatedType)implType);

                            _sealedVTableEntries.Add(SealedVTableEntry.FromVirtualMethod(targetMethod));
                        }
                    }
                    else
                    {
                        // If the interface method is provided by a default implementation, add the default implementation
                        // to the sealed vtable.
                        var resolution = declTypeDefinition.ResolveInterfaceMethodToDefaultImplementationOnType(declMethod, out implMethod);
                        if (resolution == DefaultInterfaceMethodResolution.DefaultImplementation)
                        {
                            DefType providingInterfaceDefinitionType = (DefType)implMethod.OwningType;
                            implMethod = implMethod.InstantiateSignature(declType.Instantiation, Instantiation.Empty);
                            _sealedVTableEntries.Add(SealedVTableEntry.FromDefaultInterfaceMethod(implMethod, providingInterfaceDefinitionType));
                        }
                    }
                }
            }

            return true;
        }

        protected override DependencyList ComputeNonRelocationBasedDependencies(NodeFactory factory)
        {
            var result = new DependencyList();

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
                    IMethodNode relocTarget = _sealedVTableEntries[i].GetTarget(factory, _type);

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

            private SealedVTableEntry(MethodDesc method, DefType interfaceDefinition)
            {
                Debug.Assert(interfaceDefinition == null || method.GetTypicalMethodDefinition().OwningType == interfaceDefinition.GetTypeDefinition());
                (_method, _interfaceDefinition) = (method, interfaceDefinition);
            }

            public static SealedVTableEntry FromVirtualMethod(MethodDesc method)
                => new SealedVTableEntry(method, null);

            public static SealedVTableEntry FromDefaultInterfaceMethod(MethodDesc method, DefType interfaceOnDefinition)
                => new SealedVTableEntry(method, interfaceOnDefinition);

            public IMethodNode GetTarget(NodeFactory factory, TypeDesc implementingClass)
            {
                bool isStaticVirtualMethod = _method.Signature.IsStatic;
                MethodDesc implMethod = _method.GetCanonMethodTarget(CanonicalFormKind.Specific);
                if (_interfaceDefinition != null && !isStaticVirtualMethod && implMethod.IsCanonicalMethod(CanonicalFormKind.Any))
                {
                    // Canonical instance default interface methods need to go through a thunk that acquires the generic context from `this`.
                    // Static methods have their generic context passed explicitly.
                    implMethod = factory.TypeSystemContext.GetDefaultInterfaceMethodImplementationThunk(implMethod, implementingClass.ConvertToCanonForm(CanonicalFormKind.Specific), _interfaceDefinition);
                }
                return factory.MethodEntrypoint(implMethod, unboxingStub: !isStaticVirtualMethod && _method.OwningType.IsValueType);
            }

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
