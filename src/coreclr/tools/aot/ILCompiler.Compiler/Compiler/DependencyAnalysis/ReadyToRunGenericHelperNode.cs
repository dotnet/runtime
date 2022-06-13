// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Internal.IL;
using Internal.Text;
using Internal.TypeSystem;

using ILCompiler.DependencyAnalysisFramework;

namespace ILCompiler.DependencyAnalysis
{
    public abstract partial class ReadyToRunGenericHelperNode : AssemblyStubNode, INodeWithRuntimeDeterminedDependencies
    {
        private readonly ReadyToRunHelperId _id;
        private readonly object _target;
        protected readonly TypeSystemEntity _dictionaryOwner;
        protected readonly GenericLookupResult _lookupSignature;

        // True if any of slots in dictionaries associated with this layout could not be filled
        // at compile time due to a TypeSystemException. Only query through HandlesInvalidEntries
        // below so that we can assert this is not queried at an inappropriate time before
        // the whole program view has been established.
        private bool _hasInvalidEntries;

        public ReadyToRunHelperId Id => _id;
        public Object Target => _target;
        public TypeSystemEntity DictionaryOwner => _dictionaryOwner;
        public GenericLookupResult LookupSignature => _lookupSignature;

        public bool HandlesInvalidEntries(NodeFactory factory)
        {
            Debug.Assert(factory.MarkingComplete);
            return _hasInvalidEntries;
        }

        public ReadyToRunGenericHelperNode(NodeFactory factory, ReadyToRunHelperId helperId, object target, TypeSystemEntity dictionaryOwner)
        {
            _id = helperId;
            _dictionaryOwner = dictionaryOwner;
            _target = target;

            _lookupSignature = GetLookupSignature(factory, helperId, target);
        }

        public static GenericLookupResult GetLookupSignature(NodeFactory factory, ReadyToRunHelperId id, object target)
        {
            // Necessary type handle is not something you can put in a dictionary - someone should have normalized to TypeHandle
            Debug.Assert(id != ReadyToRunHelperId.NecessaryTypeHandle);

            switch (id)
            {
                case ReadyToRunHelperId.TypeHandle:
                    return factory.GenericLookup.Type((TypeDesc)target);
                case ReadyToRunHelperId.TypeHandleForCasting:
                    // Check that we unwrapped the cases that could be unwrapped to prevent duplicate entries
                    Debug.Assert(factory.GenericLookup.Type((TypeDesc)target) != factory.GenericLookup.UnwrapNullableType((TypeDesc)target));
                    return factory.GenericLookup.UnwrapNullableType((TypeDesc)target);
                case ReadyToRunHelperId.MethodHandle:
                    return factory.GenericLookup.MethodHandle((MethodDesc)target);
                case ReadyToRunHelperId.FieldHandle:
                    return factory.GenericLookup.FieldHandle((FieldDesc)target);
                case ReadyToRunHelperId.GetGCStaticBase:
                    return factory.GenericLookup.TypeGCStaticBase((TypeDesc)target);
                case ReadyToRunHelperId.GetNonGCStaticBase:
                    return factory.GenericLookup.TypeNonGCStaticBase((TypeDesc)target);
                case ReadyToRunHelperId.GetThreadStaticBase:
                    return factory.GenericLookup.TypeThreadStaticBaseIndex((TypeDesc)target);
                case ReadyToRunHelperId.MethodDictionary:
                    return factory.GenericLookup.MethodDictionary((MethodDesc)target);
                case ReadyToRunHelperId.VirtualDispatchCell:
                    return factory.GenericLookup.VirtualDispatchCell((MethodDesc)target);
                case ReadyToRunHelperId.MethodEntry:
                    return factory.GenericLookup.MethodEntry((MethodDesc)target);
                case ReadyToRunHelperId.DelegateCtor:
                    return ((DelegateCreationInfo)target).GetLookupKind(factory);
                case ReadyToRunHelperId.DefaultConstructor:
                    return factory.GenericLookup.DefaultCtorLookupResult((TypeDesc)target);
                case ReadyToRunHelperId.ObjectAllocator:
                    return factory.GenericLookup.ObjectAllocator((TypeDesc)target);
                case ReadyToRunHelperId.ConstrainedDirectCall:
                    return factory.GenericLookup.ConstrainedMethodUse(
                        ((ConstrainedCallInfo)target).Method,
                        ((ConstrainedCallInfo)target).ConstrainedType,
                        directCall: !((ConstrainedCallInfo)target).Method.HasInstantiation);
                default:
                    throw new NotImplementedException();
            }
        }

        protected override bool IsVisibleFromManagedCode => false;

        protected sealed override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);
        public override bool IsShareable => true;

        protected sealed override void OnMarked(NodeFactory factory)
        {
            DictionaryLayoutNode layout = factory.GenericDictionaryLayout(_dictionaryOwner);

            if (layout.HasUnfixedSlots)
            {
                // When the helper call gets marked, ensure the generic layout for the associated dictionaries
                // includes the signature.
                layout.EnsureEntry(_lookupSignature);

                if ((_id == ReadyToRunHelperId.GetGCStaticBase || _id == ReadyToRunHelperId.GetThreadStaticBase) &&
                    factory.PreinitializationManager.HasLazyStaticConstructor((TypeDesc)_target))
                {
                    // If the type has a lazy static constructor, we also need the non-GC static base
                    // because that's where the class constructor context is.
                    layout.EnsureEntry(factory.GenericLookup.TypeNonGCStaticBase((TypeDesc)_target));
                }
            }
        }

        public IEnumerable<DependencyListEntry> InstantiateDependencies(NodeFactory factory, Instantiation typeInstantiation, Instantiation methodInstantiation)
        {
            DependencyList result = new DependencyList();

            var lookupContext = new GenericLookupResultContext(_dictionaryOwner, typeInstantiation, methodInstantiation);

            switch (_id)
            {
                case ReadyToRunHelperId.GetGCStaticBase:
                case ReadyToRunHelperId.GetThreadStaticBase:
                    {
                        // If the type has a lazy static constructor, we also need the non-GC static base
                        // because that's where the class constructor context is.
                        TypeDesc type = (TypeDesc)_target;

                        if (factory.PreinitializationManager.HasLazyStaticConstructor(type))
                        {
                            result.Add(
                                new DependencyListEntry(
                                    factory.GenericLookup.TypeNonGCStaticBase(type).GetTarget(factory, lookupContext),
                                    "Dictionary dependency"));
                        }
                    }
                    break;

                case ReadyToRunHelperId.DelegateCtor:
                    {
                        DelegateCreationInfo createInfo = (DelegateCreationInfo)_target;
                        if (createInfo.NeedsVirtualMethodUseTracking)
                        {
                            MethodDesc instantiatedTargetMethod = createInfo.TargetMethod.GetNonRuntimeDeterminedMethodFromRuntimeDeterminedMethodViaSubstitution(typeInstantiation, methodInstantiation);
                            if (!factory.VTable(instantiatedTargetMethod.OwningType).HasFixedSlots)
                            {
                                result.Add(
                                    new DependencyListEntry(
                                        factory.VirtualMethodUse(instantiatedTargetMethod),
                                        "Dictionary dependency"));
                            }

                            factory.MetadataManager.GetDependenciesDueToVirtualMethodReflectability(ref result, factory, instantiatedTargetMethod);
                        }
                    }
                    break;
            }

            try
            {
                // All generic lookups depend on the thing they point to
                result.Add(new DependencyListEntry(
                            _lookupSignature.GetTarget(factory, lookupContext),
                            "Dictionary dependency"));
            }
            catch (TypeSystemException)
            {
                // If there was an exception, we're going to generate a null slot in the associated
                // dictionary. The helper needs to be able to handle a null slot and tailcall
                // and exception throwing helper instead of returning a result.
                _hasInvalidEntries = true;
                result.Add(GetBadSlotHelper(factory), "Failure to build dictionary slot");
            }

            return result.ToArray();
        }

        private static IMethodNode GetBadSlotHelper(NodeFactory factory)
        {
            return factory.MethodEntrypoint(factory.TypeSystemContext.GetHelperEntryPoint("ThrowHelpers", "ThrowUnavailableType"));
        }

        protected void AppendLookupSignatureMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            if (_id != ReadyToRunHelperId.DelegateCtor)
            {
                _lookupSignature.AppendMangledName(nameMangler, sb);
            }
            else
            {
                ((DelegateCreationInfo)_target).AppendMangledName(nameMangler, sb);
            }
        }

        protected override DependencyList ComputeNonRelocationBasedDependencies(NodeFactory factory)
        {
            DependencyList dependencies = new DependencyList();

            if (_dictionaryOwner is TypeDesc type)
            {
                // The generic lookup will need to consult the vtable of the owning type to find the
                // vtable slot where the generic dictionary is placed - report the dependency.
                dependencies.Add(factory.VTable(type), "Owning type vtable");
            }

            dependencies.Add(factory.GenericDictionaryLayout(_dictionaryOwner), "Layout");

            foreach (DependencyNodeCore<NodeFactory> dependency in _lookupSignature.NonRelocDependenciesFromUsage(factory))
            {
                dependencies.Add(new DependencyListEntry(dependency, "GenericLookupResultDependency"));
            }

            if (_id == ReadyToRunHelperId.DelegateCtor)
            {
                MethodDesc targetMethod = ((DelegateCreationInfo)_target).PossiblyUnresolvedTargetMethod.GetCanonMethodTarget(CanonicalFormKind.Specific);
                factory.MetadataManager.GetDependenciesDueToDelegateCreation(ref dependencies, factory, targetMethod);
            }

            return dependencies;
        }

        public override bool HasConditionalStaticDependencies => true;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory)
        {
            List<CombinedDependencyListEntry> conditionalDependencies = new List<CombinedDependencyListEntry>();
            NativeLayoutSavedVertexNode templateLayout;
            if (_dictionaryOwner is MethodDesc)
            {
                templateLayout = factory.NativeLayout.TemplateMethodLayout((MethodDesc)_dictionaryOwner);
                conditionalDependencies.Add(new CombinedDependencyListEntry(_lookupSignature.TemplateDictionaryNode(factory),
                                                                templateLayout,
                                                                "Type loader template"));
            }
            else
            {
                templateLayout = factory.NativeLayout.TemplateTypeLayout((TypeDesc)_dictionaryOwner);
                conditionalDependencies.Add(new CombinedDependencyListEntry(_lookupSignature.TemplateDictionaryNode(factory),
                                                                templateLayout,
                                                                "Type loader template"));
            }

            if (_id == ReadyToRunHelperId.GetGCStaticBase || _id == ReadyToRunHelperId.GetThreadStaticBase)
            {
                // If the type has a lazy static constructor, we also need the non-GC static base to be available as
                // a template dictionary node.
                TypeDesc type = (TypeDesc)_target;
                Debug.Assert(templateLayout != null);
                if (factory.PreinitializationManager.HasLazyStaticConstructor(type))
                {
                    GenericLookupResult nonGcRegionLookup = factory.GenericLookup.TypeNonGCStaticBase(type);
                    conditionalDependencies.Add(new CombinedDependencyListEntry(nonGcRegionLookup.TemplateDictionaryNode(factory),
                                                                templateLayout,
                                                                "Type loader template"));
                }
            }
            
            return conditionalDependencies;
        }
        
        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            var compare = _id.CompareTo(((ReadyToRunGenericHelperNode)other)._id);
            if (compare != 0)
                return compare;

            if (_dictionaryOwner is MethodDesc)
            {
                if (((ReadyToRunGenericHelperNode)other)._dictionaryOwner is TypeDesc)
                    return -1;

                compare = comparer.Compare((MethodDesc)_dictionaryOwner, (MethodDesc)((ReadyToRunGenericHelperNode)other)._dictionaryOwner);
            }
            else
            {
                if (((ReadyToRunGenericHelperNode)other)._dictionaryOwner is MethodDesc)
                    return 1;

                compare = comparer.Compare((TypeDesc)_dictionaryOwner, (TypeDesc)((ReadyToRunGenericHelperNode)other)._dictionaryOwner);
            }

            if (compare != 0)
                return compare;

            switch (_id)
            {
                case ReadyToRunHelperId.TypeHandle:
                case ReadyToRunHelperId.GetGCStaticBase:
                case ReadyToRunHelperId.GetNonGCStaticBase:
                case ReadyToRunHelperId.GetThreadStaticBase:
                case ReadyToRunHelperId.DefaultConstructor:
                case ReadyToRunHelperId.ObjectAllocator:
                    return comparer.Compare((TypeDesc)_target, (TypeDesc)((ReadyToRunGenericHelperNode)other)._target);
                case ReadyToRunHelperId.MethodHandle:
                case ReadyToRunHelperId.MethodDictionary:
                case ReadyToRunHelperId.VirtualDispatchCell:
                case ReadyToRunHelperId.MethodEntry:
                    return comparer.Compare((MethodDesc)_target, (MethodDesc)((ReadyToRunGenericHelperNode)other)._target);
                case ReadyToRunHelperId.FieldHandle:
                    return comparer.Compare((FieldDesc)_target, (FieldDesc)((ReadyToRunGenericHelperNode)other)._target);
                case ReadyToRunHelperId.DelegateCtor:
                    return ((DelegateCreationInfo)_target).CompareTo((DelegateCreationInfo)((ReadyToRunGenericHelperNode)other)._target, comparer);
                default:
                    throw new NotImplementedException();
            }
        }
    }

    public partial class ReadyToRunGenericLookupFromDictionaryNode : ReadyToRunGenericHelperNode
    {
        public ReadyToRunGenericLookupFromDictionaryNode(NodeFactory factory, ReadyToRunHelperId helperId, object target, TypeSystemEntity dictionaryOwner)
            : base(factory, helperId, target, dictionaryOwner)
        {
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            Utf8String mangledContextName;
            if (_dictionaryOwner is MethodDesc)
                mangledContextName = nameMangler.GetMangledMethodName((MethodDesc)_dictionaryOwner);
            else
                mangledContextName = nameMangler.GetMangledTypeName((TypeDesc)_dictionaryOwner);

            sb.Append("__GenericLookupFromDict_").Append(mangledContextName).Append("_");
            AppendLookupSignatureMangledName(nameMangler, sb);
        }

        public override int ClassCode => 1055354299;
    }

    public partial class ReadyToRunGenericLookupFromTypeNode : ReadyToRunGenericHelperNode
    {
        public ReadyToRunGenericLookupFromTypeNode(NodeFactory factory, ReadyToRunHelperId helperId, object target, TypeSystemEntity dictionaryOwner)
            : base(factory, helperId, target, dictionaryOwner)
        {
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            Utf8String mangledContextName;
            if (_dictionaryOwner is MethodDesc)
                mangledContextName = nameMangler.GetMangledMethodName((MethodDesc)_dictionaryOwner);
            else
                mangledContextName = nameMangler.GetMangledTypeName((TypeDesc)_dictionaryOwner);

            sb.Append("__GenericLookupFromType_").Append(mangledContextName).Append("_");
            AppendLookupSignatureMangledName(nameMangler, sb);
        }

        public override int ClassCode => 913214059;
    }
}
