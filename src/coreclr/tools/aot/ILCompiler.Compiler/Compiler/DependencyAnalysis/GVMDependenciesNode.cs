// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// This analysis node is used for computing GVM dependencies for the following cases:
    ///    1) Derived types where the GVM is overridden
    ///    2) Variant-interfaces GVMs
    /// This analysis node will ensure that the proper GVM instantiations are compiled on types.
    /// We only analyze the canonical forms of generic virtual methods to limit the amount of generic
    /// expansion we need to deal with in the compiler.
    /// </summary>
    public class GVMDependenciesNode : DependencyNodeCore<NodeFactory>
    {
        private const int UniversalCanonGVMDepthHeuristic_CanonDepth = 2;
        private readonly MethodDesc _method;

        public GVMDependenciesNode(MethodDesc method)
        {
            Debug.Assert(method.GetCanonMethodTarget(CanonicalFormKind.Specific) == method);
            Debug.Assert(method.IsVirtual && method.HasInstantiation);
            _method = method;
        }

        public override bool HasConditionalStaticDependencies => false;
        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool StaticDependenciesAreComputed => true;
        protected override string GetName(NodeFactory factory) => "__GVMDependenciesNode_" + factory.NameMangler.GetMangledMethodName(_method);

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context)
        {
            DependencyList dependencies = null;

            context.MetadataManager.GetDependenciesDueToVirtualMethodReflectability(ref dependencies, context, _method);

            if (!_method.IsAbstract)
            {
                bool validInstantiation =
                    _method.IsSharedByGenericInstantiations || (      // Non-exact methods are always valid instantiations (always pass constraints check)
                        _method.Instantiation.CheckValidInstantiationArguments() &&
                        _method.OwningType.Instantiation.CheckValidInstantiationArguments() &&
                        _method.CheckConstraints());

                if (validInstantiation)
                {
                    if (context.TypeSystemContext.SupportsUniversalCanon && _method.IsGenericDepthGreaterThan(UniversalCanonGVMDepthHeuristic_CanonDepth))
                    {
                        // fall back to using the universal generic variant of the generic method
                        return dependencies;
                    }

                    bool getUnboxingStub = _method.OwningType.IsValueType;
                    dependencies ??= new DependencyList();
                    dependencies.Add(context.MethodEntrypoint(_method, getUnboxingStub), "GVM Dependency - Canon method");

                    if (_method.IsSharedByGenericInstantiations)
                    {
                        dependencies.Add(context.NativeLayout.TemplateMethodEntry(_method), "GVM Dependency - Template entry");
                        dependencies.Add(context.NativeLayout.TemplateMethodLayout(_method), "GVM Dependency - Template");
                    }
                }
            }

            return dependencies;
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
                EETypeNode entryAsEETypeNode = entry as EETypeNode;

                if (entryAsEETypeNode == null)
                    continue;

                TypeDesc potentialOverrideType = entryAsEETypeNode.Type;
                if (!potentialOverrideType.IsDefType || potentialOverrideType.IsInterface)
                    continue;

                // If method is canonical, don't allow using it with non-canonical types - we can wait until
                // we see the __Canon instantiation. If there isn't one, the canonical method wouldn't be useful anyway.
                if (methodIsShared &&
                    potentialOverrideType.ConvertToCanonForm(CanonicalFormKind.Specific) != potentialOverrideType)
                    continue;

                // If this is an interface gvm, look for types that implement the interface
                // and other instantantiations that have the same canonical form.
                // This ensure the various slot numbers remain equivalent across all types where there is an equivalence
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

                            MethodDesc slotDecl = potentialOverrideDefinition.InstantiateAsOpen().ResolveInterfaceMethodTarget(interfaceMethod);
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
                                TypeDesc[] openInstantiation = new TypeDesc[_method.Instantiation.Length];
                                for (int instArg = 0; instArg < openInstantiation.Length; instArg++)
                                    openInstantiation[instArg] = context.GetSignatureVariable(instArg, method: true);
                                MethodDesc implementingMethodInstantiation = slotDecl.MakeInstantiatedMethod(openInstantiation).InstantiateSignature(potentialOverrideType.Instantiation, _method.Instantiation);
                                dynamicDependencies.Add(new CombinedDependencyListEntry(factory.GVMDependencies(implementingMethodInstantiation.GetCanonMethodTarget(CanonicalFormKind.Specific)), null, "ImplementingMethodInstantiation"));
                            }
                        }
                    }
                }
                else
                {
                    // This is not an interface GVM. Check whether the current type overrides the virtual method.
                    // We might need to change what virtual method we ask about - consider:
                    //
                    // class Base<T> { virtual Method(); }
                    // class Derived : Base<string> { override Method(); }
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
                        dynamicDependencies.Add(new CombinedDependencyListEntry(
                            factory.GVMDependencies(instantiatedTargetMethod), null, "DerivedMethodInstantiation"));
                }
            }

            return dynamicDependencies;
        }
    }
}
