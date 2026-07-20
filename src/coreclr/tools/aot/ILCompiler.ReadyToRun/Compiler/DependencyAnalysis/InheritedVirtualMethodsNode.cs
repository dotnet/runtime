// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// Type discovery node for virtual dispatch dependency analysis.
    ///
    /// For GVMs: Marked InterestingForDynamicDependencyAnalysis when the type
    /// has generic virtual method slots in its hierarchy so that GVMDependenciesNode
    /// can iterate on these type nodes to discover new generic virtual method targets.
    ///
    /// For non-GVMs: Has conditional static dependencies that compile virtual method
    /// implementations when their corresponding virtual slot is marked as used
    /// (via VirtualMethodUseNode).
    /// </summary>
    public class InheritedVirtualMethodsNode : DependencyNodeCore<NodeFactory>
    {
        private readonly TypeDesc _type;
        private readonly bool _interestingForDynamicDependencyAnalysis;

        public InheritedVirtualMethodsNode(TypeDesc type)
        {
            Debug.Assert(type.IsDefType && !type.IsInterface);
            Debug.Assert(!type.IsGenericDefinition);
            _type = type;
            _interestingForDynamicDependencyAnalysis = TypeHasGVMSlots(type);
        }

        public TypeDesc Type => _type;

        public override bool InterestingForDynamicDependencyAnalysis => _interestingForDynamicDependencyAnalysis;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => true;
        public override bool StaticDependenciesAreComputed => true;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => null;

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory)
        {
            DefType defType = (DefType)_type;

            List<CombinedDependencyListEntry> result = new List<CombinedDependencyListEntry>();

            // Class virtual method path: for each virtual slot, compile the implementation
            // on this type if the slot-defining method is used.
            foreach (MethodDesc decl in defType.EnumAllVirtualSlots())
            {
                // GVMs are tracked by the orthogonal GVMDependenciesNode mechanism.
                if (decl.HasInstantiation)
                    continue;

                MethodDesc impl = defType.FindVirtualFunctionTargetMethodOnObjectType(decl);
                if (impl.IsAbstract)
                    continue;

                // Given we are scanning for non-GVMs here, if the type has no generic type arguments
                // or they are Canon, they should already be included in the compilation
                if (!impl.OwningType.HasInstantiation || !HasNonCanonicalInstantiationArguments(impl.OwningType))
                    continue;

                MethodDesc canonImpl = impl.GetCanonMethodTarget(CanonicalFormKind.Specific);
                Debug.Assert(!canonImpl.OwningType.IsGenericDefinition);
                DependencyNodeCore<NodeFactory> implNode = GetVirtualMethodImplNode(factory, canonImpl);
                if (implNode is null)
                    continue;

                result.Add(new CombinedDependencyListEntry(implNode, factory.VirtualMethodUse(decl), "Virtual method"));
            }

            // Interface method path: for each interface implemented by this type,
            // compile the implementation if the interface method slot is used.
            DefType[] runtimeInterfaces = defType.RuntimeInterfaces;
            DefType defTypeDefinition = (DefType)defType.GetTypeDefinition();
            DefType[] definitionRuntimeInterfaces = defTypeDefinition.RuntimeInterfaces;

            for (int interfaceIndex = 0; interfaceIndex < runtimeInterfaces.Length; interfaceIndex++)
            {
                DefType interfaceType = runtimeInterfaces[interfaceIndex];
                DefType definitionInterfaceType = definitionRuntimeInterfaces[interfaceIndex];

                foreach (MethodDesc interfaceMethod in interfaceType.EnumAllVirtualSlots())
                {
                    // GVMs handled in GVMDependenciesNode. SVMs handled at call site.
                    if (interfaceMethod.HasInstantiation || interfaceMethod.Signature.IsStatic)
                        continue;

                    MethodDesc interfaceMethodDefinition = interfaceMethod;
                    if (interfaceType != definitionInterfaceType)
                        interfaceMethodDefinition = factory.TypeSystemContext.GetMethodForInstantiatedType(
                            interfaceMethodDefinition.GetTypicalMethodDefinition(),
                            (InstantiatedType)definitionInterfaceType);

                    MethodDesc implMethod = defTypeDefinition.InstantiateAsOpen().ResolveInterfaceMethodTarget(interfaceMethodDefinition);
                    if (implMethod is null)
                    {
                        // The method might be implemented through a default interface method
                        var resolution = defTypeDefinition.InstantiateAsOpen().ResolveInterfaceMethodToDefaultImplementationOnType(interfaceMethodDefinition, out implMethod);
                        if (resolution != DefaultInterfaceMethodResolution.DefaultImplementation)
                        {
                            implMethod = null;
                        }
                    }

                    if (implMethod is not null)
                    {
                        implMethod = implMethod.InstantiateSignature(defType.Instantiation, Instantiation.Empty);

                        if (implMethod.IsVirtual && !implMethod.IsFinal && !implMethod.OwningType.IsInterface)
                        {
                            // The interface resolves to a virtual method that can be overridden.
                            // Mark the class virtual slot as used so the class path compiles the
                            // actual final target (which may be an override further down the hierarchy).
                            result.Add(new CombinedDependencyListEntry(factory.VirtualMethodUse(implMethod), factory.VirtualMethodUse(interfaceMethod), "Interface method"));
                        }
                        else
                        {
                            MethodDesc canonImpl = implMethod.GetCanonMethodTarget(CanonicalFormKind.Specific);
                            DependencyNodeCore<NodeFactory> implNode = GetVirtualMethodImplNode(factory, canonImpl);
                            if (implNode is not null)
                            {
                                result.Add(new CombinedDependencyListEntry(implNode, factory.VirtualMethodUse(interfaceMethod), "Interface method"));
                            }
                        }
                    }
                }
            }

            return result;
        }

        private static bool TypeHasGVMSlots(TypeDesc type)
        {
            TypeDesc currentType = type;
            while (currentType != null)
            {
                foreach (MethodDesc method in currentType.GetVirtualMethods())
                {
                    if (method.HasInstantiation)
                        return true;
                }
                currentType = currentType.BaseType;
            }

            // Look for generic DIMs from implemented interfaces
            foreach (DefType interfaceType in type.RuntimeInterfaces)
            {
                foreach (MethodDesc interfaceMethod in interfaceType.GetVirtualMethods())
                {
                    if (interfaceMethod.HasInstantiation && !interfaceMethod.IsAbstract)
                        return true;
                }
            }

            return false;
        }

        private static bool HasNonCanonicalInstantiationArguments(TypeDesc type)
        {
            TypeDesc canonType = type.Context.CanonType;
            foreach (TypeDesc arg in type.Instantiation)
            {
                if (arg != canonType)
                    return true;
            }
            return false;
        }

        private static DependencyNodeCore<NodeFactory> GetVirtualMethodImplNode(NodeFactory factory, MethodDesc method)
        {
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
        }

        protected override string GetName(NodeFactory factory) => $"Inherited virtual methods on {_type}";
    }
}
