// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;

#if READYTORUN
using ILCompiler.DependencyAnalysis.ReadyToRun;
#endif

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a use of a generic virtual method slot. This node only tracks
    /// the use of the slot definition.
    /// This analysis node is used for computing GVM dependencies for the following cases:
    ///    1) Derived types where the GVM is overridden
    ///    2) Variant-interfaces GVMs
    /// This analysis node will ensure that the proper GVM instantiations are compiled on types.
    /// We only analyze the canonical forms of generic virtual methods to limit the amount of generic
    /// expansion we need to deal with in the compiler.
    /// </summary>
    public class GVMDependenciesNode : DependencyNodeCore<NodeFactory>
    {
        private readonly MethodDesc _method;

        public GVMDependenciesNode(MethodDesc method)
        {
            Debug.Assert(method.GetCanonMethodTarget(CanonicalFormKind.Specific) == method);
            Debug.Assert(method.HasInstantiation);
            Debug.Assert(method.IsVirtual);
            Debug.Assert(MetadataVirtualMethodAlgorithm.FindSlotDefiningMethodForVirtualMethod(method) == method);

            _method = method;
        }

        public override bool HasConditionalStaticDependencies => false;
        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool StaticDependenciesAreComputed => true;
        protected override string GetName(NodeFactory factory) => "__GVMDependenciesNode_" + factory.NameMangler.GetMangledMethodName(_method);

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            if (!_method.IsAbstract)
            {
                DependencyNodeCore<NodeFactory> node = GetVirtualMethodImplNode(factory, _method);
                if (node != null)
                    yield return new DependencyListEntry(node, "Implementation of the generic virtual method");
            }
#if !READYTORUN
            if (!_method.OwningType.IsInterface)
            {
                yield return new DependencyListEntry(factory.TypeGVMEntries(_method.OwningType.GetTypeDefinition()), "Resolution metadata");
            }
#endif
        }

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => null;

        public override bool HasDynamicDependencies
        {
            get
            {
                TypeDesc methodOwningType = _method.OwningType;

                // SearchDynamicDependencies wouldn't come up with anything for these
                if (!methodOwningType.IsInterface &&
                    (methodOwningType.IsSealed() || _method.IsFinal))
                    return false;

                return true;
            }
        }

        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory)
        {
            List<CombinedDependencyListEntry> dynamicDependencies = new List<CombinedDependencyListEntry>();

            TypeDesc methodOwningType = _method.OwningType;
            bool methodIsShared = _method.IsSharedByGenericInstantiations;

            TypeSystemContext context = _method.Context;

            for (int i = firstNode; i < markedNodes.Count; i++)
            {
                DependencyNodeCore<NodeFactory> entry = markedNodes[i];

#if !READYTORUN
                // This method is often called with a long list of ScannedMethodNode
                // or MethodCodeNode nodes. We are not interested in those. In order
                // to make the type check as cheap as possible we check for specific
                // *sealed* types instead of doing `entry is EETypeNode` which has
                // to walk the whole class hierarchy for the non matching nodes.
                if (entry is not ConstructedEETypeNode typeNode)
                    continue;
#else
                if (entry is not InheritedVirtualMethodsNode typeNode)
                    continue;
#endif

                TypeDesc potentialOverrideType = typeNode.Type;
                if (!potentialOverrideType.IsDefType || potentialOverrideType.IsInterface)
                    continue;

                // If method is canonical, don't allow using it with non-canonical types - we can wait until
                // we see the __Canon instantiation. If there isn't one, the canonical method wouldn't be useful anyway.
                if (methodIsShared &&
                    potentialOverrideType.ConvertToCanonForm(CanonicalFormKind.Specific) != potentialOverrideType)
                    continue;

#if !READYTORUN
                bool foundImpl = false;
#endif
                // If this is an interface gvm, look for types that implement the interface
                // and other instantiations that have the same canonical form.
                // This ensures the various slot numbers remain equivalent across all types where there is an equivalence
                // relationship in the vtable.
                if (methodOwningType.IsInterface)
                {
                    // We go over definitions because a single canonical interface method could actually be implemented
                    // by multiple methods - consider:
                    //
                    // class Foo<T, U> : IFoo<T>, IFoo<U>, IFoo<string> { }
                    //
                    // If we ask what implements IFoo<__Canon>.Method, the answer could be "three methods"
                    // and that's expected. We therefore resolve IFoo<__Canon>.Method for each IFoo<!0>.Method,
                    // IFoo<!1>.Method, and IFoo<string>.Method, adding GVMDependencies for each.
                    TypeDesc potentialOverrideDefinition = potentialOverrideType.GetTypeDefinition();
                    DefType[] potentialInterfaces = potentialOverrideType.RuntimeInterfaces;
                    DefType[] potentialDefinitionInterfaces = potentialOverrideDefinition.RuntimeInterfaces;
                    for (int interfaceIndex = 0; interfaceIndex < potentialInterfaces.Length; interfaceIndex++)
                    {
                        if (potentialInterfaces[interfaceIndex].ConvertToCanonForm(CanonicalFormKind.Specific) == methodOwningType)
                        {
                            MethodDesc interfaceMethod = _method.GetMethodDefinition();
                            if (methodOwningType.HasInstantiation)
                                interfaceMethod = context.GetMethodForInstantiatedType(
                                    _method.GetTypicalMethodDefinition(), (InstantiatedType)potentialDefinitionInterfaces[interfaceIndex]);

                            MethodDesc slotDecl = interfaceMethod.Signature.IsStatic ?
                                potentialOverrideDefinition.InstantiateAsOpen().ResolveInterfaceMethodToStaticVirtualMethodOnType(interfaceMethod)
                                : potentialOverrideDefinition.InstantiateAsOpen().ResolveInterfaceMethodTarget(interfaceMethod);
                            if (slotDecl == null)
                            {
                                // The method might be implemented through a default interface method
                                var result = potentialOverrideDefinition.InstantiateAsOpen().ResolveInterfaceMethodToDefaultImplementationOnType(interfaceMethod, out slotDecl);
                                if (result != DefaultInterfaceMethodResolution.DefaultImplementation)
                                {
                                    slotDecl = null;
                                }
                            }

                            if (slotDecl != null)
                            {
                                MethodDesc implementingMethodInstantiation = slotDecl
                                    .MakeInstantiatedMethod(_method.Instantiation)
                                    .InstantiateSignature(potentialOverrideType.Instantiation, _method.Instantiation);

                                MethodDesc canonImpl = implementingMethodInstantiation.GetCanonMethodTarget(CanonicalFormKind.Specific);

                                // Static virtuals cannot be further overridden so this is an impl use. Otherwise it's a virtual slot use.
                                if (implementingMethodInstantiation.Signature.IsStatic)
                                {
                                    DependencyNodeCore<NodeFactory> node = GetVirtualMethodImplNode(factory, canonImpl);
                                    if (node != null)
                                        dynamicDependencies.Add(new CombinedDependencyListEntry(node, null, "ImplementingMethodInstantiation"));
                                }
                                else
                                {
                                    dynamicDependencies.Add(new CombinedDependencyListEntry(factory.GVMDependencies(canonImpl), null, "ImplementingMethodInstantiation"));
                                }

#if !READYTORUN
                                TypeSystemEntity origin = (implementingMethodInstantiation.OwningType != potentialOverrideType) ? potentialOverrideType : null;
                                factory.MetadataManager.NoteOverridingMethod(_method, implementingMethodInstantiation, origin);
#endif
                            }

#if !READYTORUN
                            foundImpl = true;
#endif
                        }
                    }
                }
                else
                {
                    // This is not an interface GVM. Check whether the current type overrides the virtual method.
                    // We might need to change what virtual method we ask about - consider:
                    //
                    // class Base<T> { virtual Method<U>(); }
                    // class Derived : Base<string> { override Method<U>(); }
                    //
                    // We need to resolve Base<__Canon>.Method on Derived, but if we were to ask the virtual
                    // method resolution algorithm, the answer would be "does not override" because Base<__Canon>
                    // is not even in the inheritance hierarchy.
                    //
                    // So we need to modify the question to resolve Base<string>.Method instead and then
                    // canonicalize the result.

                    TypeDesc overrideTypeCur = potentialOverrideType;
                    do
                    {
                        if (overrideTypeCur.ConvertToCanonForm(CanonicalFormKind.Specific) == methodOwningType)
                            break;

                        overrideTypeCur = overrideTypeCur.BaseType;
                    }
                    while (overrideTypeCur != null);

                    if (overrideTypeCur == null)
                        continue;

                    MethodDesc methodToResolve;
                    if (methodOwningType == overrideTypeCur)
                    {
                        methodToResolve = _method;
                    }
                    else
                    {
                        methodToResolve = context
                            .GetMethodForInstantiatedType(_method.GetTypicalMethodDefinition(), (InstantiatedType)overrideTypeCur)
                            .MakeInstantiatedMethod(_method.Instantiation);
                    }

                    MethodDesc instantiatedTargetMethod = potentialOverrideType.FindVirtualFunctionTargetMethodOnObjectType(methodToResolve)
                        .GetCanonMethodTarget(CanonicalFormKind.Specific);
                    if (instantiatedTargetMethod != _method)
                    {
                        DependencyNodeCore<NodeFactory> node = GetVirtualMethodImplNode(factory, instantiatedTargetMethod);
                        if (node != null)
                            dynamicDependencies.Add(new CombinedDependencyListEntry(node, null, "DerivedMethodInstantiation"));
#if !READYTORUN
                        factory.MetadataManager.NoteOverridingMethod(_method, instantiatedTargetMethod);

                        foundImpl = true;
#endif
                    }
                }

#if !READYTORUN
                if (foundImpl)
                {
                    TypeDesc currentType = potentialOverrideType;
                    do
                    {
                        dynamicDependencies.Add(new CombinedDependencyListEntry(factory.TypeGVMEntries(currentType.GetTypeDefinition()), null, "Resolution metadata"));
                        currentType = currentType.BaseType;
                    }
                    while (currentType != null);
                }
#endif
            }

            return dynamicDependencies;
        }

        private static DependencyNodeCore<NodeFactory> GetVirtualMethodImplNode(NodeFactory factory, MethodDesc method)
        {
#if !READYTORUN
            return factory.GenericVirtualMethodImpl(method);
#else
            if (!factory.CompilationModuleGroup.ContainsMethodBody(method, false))
                return null;

            try
            {
                factory.DetectGenericCycles(method, method);
                return factory.CompiledMethodNode(method);
            }
            catch (TypeSystemException)
            {
                return null;
            }
#endif
        }
    }
}
