// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

using Internal.TypeSystem;
using ILCompiler.DependencyAnalysisFramework;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// This node is used for GVM dependency analysis and GVM tables building. Given an input
    /// type, this node can scan the type for a list of GVM table entries, and compute their dependencies.
    /// </summary>
    internal sealed class TypeGVMEntriesNode : DependencyNodeCore<NodeFactory>
    {
        internal class TypeGVMEntryInfo
        {
            public TypeGVMEntryInfo(MethodDesc callingMethod, MethodDesc implementationMethod)
            {
                CallingMethod = callingMethod;
                ImplementationMethod = implementationMethod;
            }
            public MethodDesc CallingMethod { get; }
            public MethodDesc ImplementationMethod { get; }
        }

        internal sealed class InterfaceGVMEntryInfo : TypeGVMEntryInfo
        {
            public InterfaceGVMEntryInfo(MethodDesc callingMethod, MethodDesc implementationMethod,
                TypeDesc implementationType, DefaultInterfaceMethodResolution defaultResolution)
                : base(callingMethod, implementationMethod)
            {
                ImplementationType = implementationType;
                DefaultResolution = defaultResolution;
            }

            public TypeDesc ImplementationType { get; }
            public DefaultInterfaceMethodResolution DefaultResolution { get; }
        }

        private readonly TypeDesc _associatedType;
        private DependencyList _staticDependencies;

        public TypeGVMEntriesNode(TypeDesc associatedType)
        {
            Debug.Assert(associatedType.IsTypeDefinition);
            _associatedType = associatedType;
        }

        public TypeDesc AssociatedType => _associatedType;

        public override bool HasConditionalStaticDependencies => false;
        public override bool HasDynamicDependencies => false;
        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool StaticDependenciesAreComputed => true;
        protected override string GetName(NodeFactory factory) => "__TypeGVMEntriesNode_" + factory.NameMangler.GetMangledTypeName(_associatedType);
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => null;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context)
        {
            if (_staticDependencies == null)
            {
                _staticDependencies = new DependencyList();

                foreach(var entry in ScanForGenericVirtualMethodEntries())
                    GenericVirtualMethodTableNode.GetGenericVirtualMethodImplementationDependencies(ref _staticDependencies, context, entry.CallingMethod, entry.ImplementationMethod);

                foreach (var entry in ScanForInterfaceGenericVirtualMethodEntries())
                    InterfaceGenericVirtualMethodTableNode.GetGenericVirtualMethodImplementationDependencies(ref _staticDependencies, context, entry.CallingMethod, entry.ImplementationType, entry.ImplementationMethod);

                Debug.Assert(_staticDependencies.Count > 0);
            }

            return _staticDependencies;
        }

        public IEnumerable<TypeGVMEntryInfo> ScanForGenericVirtualMethodEntries()
        {
            foreach (MethodDesc decl in _associatedType.EnumAllVirtualSlots())
            {
                // Non-Generic virtual methods are tracked by an orthogonal mechanism.
                if (!decl.HasInstantiation)
                    continue;

                MethodDesc impl = _associatedType.FindVirtualFunctionTargetMethodOnObjectType(decl);

                if (impl.OwningType == _associatedType)
                    yield return new TypeGVMEntryInfo(decl, impl);
            }
        }

        public IEnumerable<InterfaceGVMEntryInfo> ScanForInterfaceGenericVirtualMethodEntries()
        {
            foreach (var iface in _associatedType.RuntimeInterfaces)
            {
                foreach (var method in iface.GetVirtualMethods())
                {
                    if (!method.HasInstantiation)
                        continue;

                    DefaultInterfaceMethodResolution resolution = DefaultInterfaceMethodResolution.None;
                    MethodDesc slotDecl = method.Signature.IsStatic ?
                        _associatedType.ResolveInterfaceMethodToStaticVirtualMethodOnType(method) : _associatedType.ResolveInterfaceMethodTarget(method);
                    if (slotDecl == null)
                    {
                        resolution = _associatedType.ResolveInterfaceMethodToDefaultImplementationOnType(method, out slotDecl);
                        if (resolution != DefaultInterfaceMethodResolution.DefaultImplementation)
                            slotDecl = null;
                    }

                    if (slotDecl != null
                        || resolution == DefaultInterfaceMethodResolution.Diamond
                        || resolution == DefaultInterfaceMethodResolution.Reabstraction)
                    {
                        yield return new InterfaceGVMEntryInfo(method, slotDecl, _associatedType, resolution);
                    }
                }
            }
        }
    }
}
