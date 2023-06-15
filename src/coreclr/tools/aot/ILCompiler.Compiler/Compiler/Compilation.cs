// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;

using Internal.IL;
using Internal.IL.Stubs;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using CORINFO_DEVIRTUALIZATION_DETAIL = Internal.JitInterface.CORINFO_DEVIRTUALIZATION_DETAIL;
using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    public abstract class Compilation : ICompilation
    {
        protected readonly DependencyAnalyzerBase<NodeFactory> _dependencyGraph;
        protected readonly NodeFactory _nodeFactory;
        protected readonly Logger _logger;
        protected readonly DebugInformationProvider _debugInformationProvider;
        protected readonly DevirtualizationManager _devirtualizationManager;
        private readonly IInliningPolicy _inliningPolicy;

        public NameMangler NameMangler => _nodeFactory.NameMangler;
        public NodeFactory NodeFactory => _nodeFactory;
        public CompilerTypeSystemContext TypeSystemContext => NodeFactory.TypeSystemContext;
        public Logger Logger => _logger;
        public PInvokeILProvider PInvokeILProvider { get; }

        private readonly TypeGetTypeMethodThunkCache _typeGetTypeMethodThunks;
        private readonly AssemblyGetExecutingAssemblyMethodThunkCache _assemblyGetExecutingAssemblyMethodThunks;
        private readonly MethodBaseGetCurrentMethodThunkCache _methodBaseGetCurrentMethodThunks;

        protected Compilation(
            DependencyAnalyzerBase<NodeFactory> dependencyGraph,
            NodeFactory nodeFactory,
            IEnumerable<ICompilationRootProvider> compilationRoots,
            ILProvider ilProvider,
            DebugInformationProvider debugInformationProvider,
            DevirtualizationManager devirtualizationManager,
            IInliningPolicy inliningPolicy,
            Logger logger)
        {
            _dependencyGraph = dependencyGraph;
            _nodeFactory = nodeFactory;
            _logger = logger;
            _debugInformationProvider = debugInformationProvider;
            _devirtualizationManager = devirtualizationManager;
            _inliningPolicy = inliningPolicy;

            _dependencyGraph.ComputeDependencyRoutine += ComputeDependencyNodeDependencies;
            NodeFactory.AttachToDependencyGraph(_dependencyGraph);

            var rootingService = new RootingServiceProvider(nodeFactory, _dependencyGraph.AddRoot);
            foreach (var rootProvider in compilationRoots)
                rootProvider.AddCompilationRoots(rootingService);

            MetadataType globalModuleGeneratedType = nodeFactory.TypeSystemContext.GeneratedAssembly.GetGlobalModuleType();
            _typeGetTypeMethodThunks = new TypeGetTypeMethodThunkCache(globalModuleGeneratedType);
            _assemblyGetExecutingAssemblyMethodThunks = new AssemblyGetExecutingAssemblyMethodThunkCache(globalModuleGeneratedType);
            _methodBaseGetCurrentMethodThunks = new MethodBaseGetCurrentMethodThunkCache();

            PInvokeILProvider = _nodeFactory.InteropStubManager.CreatePInvokeILProvider();
            if (PInvokeILProvider != null)
            {
                ilProvider = new CombinedILProvider(ilProvider, PInvokeILProvider);
            }

            _methodILCache = new ILCache(ilProvider);
        }

        private ILCache _methodILCache;

        public virtual MethodIL GetMethodIL(MethodDesc method)
        {
            // Flush the cache when it grows too big
            if (_methodILCache.Count > 1000)
                _methodILCache = new ILCache(_methodILCache.ILProvider);

            return _methodILCache.GetOrCreateValue(method).MethodIL;
        }

        protected abstract void ComputeDependencyNodeDependencies(List<DependencyNodeCore<NodeFactory>> obj);

        protected abstract void CompileInternal(string outputFile, ObjectDumper dumper);

        public void DetectGenericCycles(MethodDesc caller, MethodDesc callee)
        {
            _nodeFactory.TypeSystemContext.DetectGenericCycles(caller, callee);
        }

        public virtual IEETypeNode NecessaryTypeSymbolIfPossible(TypeDesc type)
        {
            return _nodeFactory.NecessaryTypeSymbol(type);
        }

        public bool CanInline(MethodDesc caller, MethodDesc callee)
        {
            return _inliningPolicy.CanInline(caller, callee);
        }

        public bool CanConstructType(TypeDesc type)
        {
            return _devirtualizationManager.CanConstructType(type);
        }

        public DelegateCreationInfo GetDelegateCtor(TypeDesc delegateType, MethodDesc target, TypeDesc constrainedType, bool followVirtualDispatch)
        {
            // If we're creating a delegate to a virtual method that cannot be overridden, devirtualize.
            // This is not just an optimization - it's required for correctness in the presence of sealed
            // vtable slots.
            if (followVirtualDispatch && (target.IsFinal || target.OwningType.IsSealed()))
                followVirtualDispatch = false;

            if (followVirtualDispatch)
                target = MetadataVirtualMethodAlgorithm.FindSlotDefiningMethodForVirtualMethod(target);

            return DelegateCreationInfo.Create(delegateType, target, constrainedType, NodeFactory, followVirtualDispatch);
        }

        /// <summary>
        /// Gets an object representing the static data for RVA mapped fields from the PE image.
        /// </summary>
        public virtual ISymbolNode GetFieldRvaData(FieldDesc field)
        {
            if (field.GetType() == typeof(PInvokeLazyFixupField))
            {
                return NodeFactory.PInvokeMethodFixup(new PInvokeMethodData((PInvokeLazyFixupField)field));
            }
            else if (field is ExternSymbolMappedField externField)
            {
                return NodeFactory.ExternSymbol(externField.SymbolName);
            }
            else
            {
                // Use the typical field definition in case this is an instantiated generic type
                return NodeFactory.FieldRvaData((EcmaField)field.GetTypicalFieldDefinition());
            }
        }

        public bool HasLazyStaticConstructor(TypeDesc type)
        {
            return NodeFactory.PreinitializationManager.HasLazyStaticConstructor(type);
        }

        public MethodDebugInformation GetDebugInfo(MethodIL methodIL)
        {
            return _debugInformationProvider.GetDebugInfo(methodIL);
        }

        /// <summary>
        /// Resolves a reference to an intrinsic method to a new method that takes it's place in the compilation.
        /// This is used for intrinsics where the intrinsic expansion depends on the callsite.
        /// </summary>
        /// <param name="intrinsicMethod">The intrinsic method called.</param>
        /// <param name="callsiteMethod">The callsite that calls the intrinsic.</param>
        /// <returns>The intrinsic implementation to be called for this specific callsite.</returns>
        public MethodDesc ExpandIntrinsicForCallsite(MethodDesc intrinsicMethod, MethodDesc callsiteMethod)
        {
            Debug.Assert(intrinsicMethod.IsIntrinsic);

            var intrinsicOwningType = intrinsicMethod.OwningType as MetadataType;
            if (intrinsicOwningType == null)
                return intrinsicMethod;

            if (intrinsicOwningType.Module != TypeSystemContext.SystemModule)
                return intrinsicMethod;

            if (intrinsicOwningType.Name == "Type" && intrinsicOwningType.Namespace == "System")
            {
                if (intrinsicMethod.Signature.IsStatic && intrinsicMethod.Name == "GetType")
                {
                    ModuleDesc callsiteModule = (callsiteMethod.OwningType as MetadataType)?.Module;
                    if (callsiteModule != null)
                    {
                        Debug.Assert(callsiteModule is IAssemblyDesc, "Multi-module assemblies");
                        return _typeGetTypeMethodThunks.GetHelper(intrinsicMethod, ((IAssemblyDesc)callsiteModule).GetName().FullName);
                    }
                }
            }
            else if (intrinsicOwningType.Name == "Assembly" && intrinsicOwningType.Namespace == "System.Reflection")
            {
                if (intrinsicMethod.Signature.IsStatic && intrinsicMethod.Name == "GetExecutingAssembly")
                {
                    ModuleDesc callsiteModule = (callsiteMethod.OwningType as MetadataType)?.Module;
                    if (callsiteModule != null)
                    {
                        Debug.Assert(callsiteModule is IAssemblyDesc, "Multi-module assemblies");
                        return _assemblyGetExecutingAssemblyMethodThunks.GetHelper((IAssemblyDesc)callsiteModule);
                    }
                }
            }
            else if (intrinsicOwningType.Name == "MethodBase" && intrinsicOwningType.Namespace == "System.Reflection")
            {
                if (intrinsicMethod.Signature.IsStatic && intrinsicMethod.Name == "GetCurrentMethod")
                {
                    return _methodBaseGetCurrentMethodThunks.GetHelper(callsiteMethod).InstantiateAsOpen();
                }
            }

            return intrinsicMethod;
        }

        public bool HasFixedSlotVTable(TypeDesc type)
        {
            return NodeFactory.VTable(type).HasFixedSlots;
        }

        public bool IsEffectivelySealed(TypeDesc type)
        {
            return _devirtualizationManager.IsEffectivelySealed(type);
        }

        public TypeDesc[] GetImplementingClasses(TypeDesc type)
        {
            return _devirtualizationManager.GetImplementingClasses(type);
        }

        public bool IsEffectivelySealed(MethodDesc method)
        {
            return _devirtualizationManager.IsEffectivelySealed(method);
        }

        public MethodDesc ResolveVirtualMethod(MethodDesc declMethod, TypeDesc implType, out CORINFO_DEVIRTUALIZATION_DETAIL devirtualizationDetail)
        {
            return _devirtualizationManager.ResolveVirtualMethod(declMethod, implType, out devirtualizationDetail);
        }

        public bool NeedsRuntimeLookup(ReadyToRunHelperId lookupKind, object targetOfLookup)
        {
            switch (lookupKind)
            {
                case ReadyToRunHelperId.TypeHandle:
                case ReadyToRunHelperId.NecessaryTypeHandle:
                case ReadyToRunHelperId.DefaultConstructor:
                case ReadyToRunHelperId.TypeHandleForCasting:
                case ReadyToRunHelperId.ObjectAllocator:
                    return ((TypeDesc)targetOfLookup).IsRuntimeDeterminedSubtype;

                case ReadyToRunHelperId.MethodDictionary:
                case ReadyToRunHelperId.MethodEntry:
                case ReadyToRunHelperId.VirtualDispatchCell:
                case ReadyToRunHelperId.MethodHandle:
                    return ((MethodDesc)targetOfLookup).IsRuntimeDeterminedExactMethod;

                case ReadyToRunHelperId.FieldHandle:
                    return ((FieldDesc)targetOfLookup).OwningType.IsRuntimeDeterminedSubtype;

                case ReadyToRunHelperId.ConstrainedDirectCall:
                    return ((ConstrainedCallInfo)targetOfLookup).Method.IsRuntimeDeterminedExactMethod
                        || ((ConstrainedCallInfo)targetOfLookup).ConstrainedType.IsRuntimeDeterminedSubtype;

                default:
                    throw new NotImplementedException();
            }
        }

        public ReadyToRunHelperId GetLdTokenHelperForType(TypeDesc type)
        {
            bool canConstructPerWholeProgramAnalysis = _devirtualizationManager == null ? true : _devirtualizationManager.CanConstructType(type);
            bool creationAllowed = ConstructedEETypeNode.CreationAllowed(type);
            return (canConstructPerWholeProgramAnalysis && creationAllowed)
                ? ReadyToRunHelperId.TypeHandle
                : ReadyToRunHelperId.NecessaryTypeHandle;
        }

        public static MethodDesc GetConstructorForCreateInstanceIntrinsic(TypeDesc type)
        {
            MethodDesc ctor = type.GetDefaultConstructor();
            if (ctor == null)
            {
                MetadataType activatorType = type.Context.SystemModule.GetKnownType("System", "Activator");
                if (type.IsValueType && type.GetParameterlessConstructor() == null)
                {
                    ctor = activatorType.GetKnownNestedType("StructWithNoConstructor").GetKnownMethod(".ctor", null);
                }
                else
                {
                    ctor = activatorType.GetKnownMethod("MissingConstructorMethod", null);
                }
            }

            return ctor;
        }

        public ISymbolNode ComputeConstantLookup(ReadyToRunHelperId lookupKind, object targetOfLookup)
        {
            switch (lookupKind)
            {
                case ReadyToRunHelperId.TypeHandle:
                    return NodeFactory.ConstructedTypeSymbol((TypeDesc)targetOfLookup);
                case ReadyToRunHelperId.NecessaryTypeHandle:
                    return NecessaryTypeSymbolIfPossible((TypeDesc)targetOfLookup);
                case ReadyToRunHelperId.TypeHandleForCasting:
                    {
                        var type = (TypeDesc)targetOfLookup;
                        if (type.IsNullable)
                            targetOfLookup = type.Instantiation[0];
                        return NecessaryTypeSymbolIfPossible((TypeDesc)targetOfLookup);
                    }
                case ReadyToRunHelperId.MethodDictionary:
                    return NodeFactory.MethodGenericDictionary((MethodDesc)targetOfLookup);
                case ReadyToRunHelperId.MethodEntry:
                    return NodeFactory.FatFunctionPointer((MethodDesc)targetOfLookup);
                case ReadyToRunHelperId.MethodHandle:
                    return NodeFactory.RuntimeMethodHandle((MethodDesc)targetOfLookup);
                case ReadyToRunHelperId.FieldHandle:
                    return NodeFactory.RuntimeFieldHandle((FieldDesc)targetOfLookup);
                case ReadyToRunHelperId.DefaultConstructor:
                    {
                        var type = (TypeDesc)targetOfLookup;
                        MethodDesc ctor = GetConstructorForCreateInstanceIntrinsic(type);
                        return NodeFactory.CanonicalEntrypoint(ctor);
                    }
                case ReadyToRunHelperId.ObjectAllocator:
                    {
                        var type = (TypeDesc)targetOfLookup;
                        return NodeFactory.ExternSymbol(JitHelper.GetNewObjectHelperForType(type));
                    }

                default:
                    throw new NotImplementedException();
            }
        }

        public GenericDictionaryLookup ComputeGenericLookup(MethodDesc contextMethod, ReadyToRunHelperId lookupKind, object targetOfLookup)
        {
            if (targetOfLookup is TypeSystemEntity typeSystemEntity)
            {
                _nodeFactory.TypeSystemContext.DetectGenericCycles(contextMethod, typeSystemEntity);
            }

            GenericContextSource contextSource;

            if (contextMethod.RequiresInstMethodDescArg())
            {
                contextSource = GenericContextSource.MethodParameter;
            }
            else if (contextMethod.RequiresInstMethodTableArg())
            {
                contextSource = GenericContextSource.TypeParameter;
            }
            else
            {
                Debug.Assert(contextMethod.AcquiresInstMethodTableFromThis());
                contextSource = GenericContextSource.ThisObject;
            }

            //
            // Some helpers represent logical concepts that might not be something that can be looked up in a dictionary
            //

            // Downgrade type handle for casting to a normal type handle if possible
            if (lookupKind == ReadyToRunHelperId.TypeHandleForCasting)
            {
                var type = (TypeDesc)targetOfLookup;
                if (!type.IsRuntimeDeterminedType ||
                    (!((RuntimeDeterminedType)type).CanonicalType.IsCanonicalDefinitionType(CanonicalFormKind.Universal) &&
                    !((RuntimeDeterminedType)type).CanonicalType.IsNullable))
                {
                    if (type.IsNullable)
                    {
                        targetOfLookup = type.Instantiation[0];
                    }
                    lookupKind = ReadyToRunHelperId.NecessaryTypeHandle;
                }
            }

            // We don't have separate entries for necessary type handles to avoid possible duplication
            if (lookupKind == ReadyToRunHelperId.NecessaryTypeHandle)
            {
                lookupKind = ReadyToRunHelperId.TypeHandle;
            }

            // Can we do a fixed lookup? Start by checking if we can get to the dictionary.
            // Context source having a vtable with fixed slots is a prerequisite.
            if (contextSource == GenericContextSource.MethodParameter
                || HasFixedSlotVTable(contextMethod.OwningType))
            {
                DictionaryLayoutNode dictionaryLayout;
                if (contextSource == GenericContextSource.MethodParameter)
                    dictionaryLayout = _nodeFactory.GenericDictionaryLayout(contextMethod);
                else
                    dictionaryLayout = _nodeFactory.GenericDictionaryLayout(contextMethod.OwningType);

                // If the dictionary layout has fixed slots, we can compute the lookup now. Otherwise defer to helper.
                if (dictionaryLayout.HasFixedSlots)
                {
                    int pointerSize = _nodeFactory.Target.PointerSize;

                    GenericLookupResult lookup = ReadyToRunGenericHelperNode.GetLookupSignature(_nodeFactory, lookupKind, targetOfLookup);
                    if (dictionaryLayout.TryGetSlotForEntry(lookup, out int dictionarySlot))
                    {
                        int dictionaryOffset = dictionarySlot * pointerSize;

                        bool indirectLastOffset = lookup.LookupResultReferenceType(_nodeFactory) == GenericLookupResultReferenceType.Indirect;

                        if (contextSource == GenericContextSource.MethodParameter)
                        {
                            return GenericDictionaryLookup.CreateFixedLookup(contextSource, dictionaryOffset, indirectLastOffset: indirectLastOffset);
                        }
                        else
                        {
                            int vtableSlot = VirtualMethodSlotHelper.GetGenericDictionarySlot(_nodeFactory, contextMethod.OwningType);
                            int vtableOffset = EETypeNode.GetVTableOffset(pointerSize) + vtableSlot * pointerSize;
                            return GenericDictionaryLookup.CreateFixedLookup(contextSource, vtableOffset, dictionaryOffset, indirectLastOffset: indirectLastOffset);
                        }
                    }
                    else
                    {
                        return GenericDictionaryLookup.CreateNullLookup(contextSource);
                    }
                }
            }

            // Fixed lookup not possible - use helper.
            return GenericDictionaryLookup.CreateHelperLookup(contextSource, lookupKind, targetOfLookup);
        }

        public bool IsFatPointerCandidate(MethodDesc containingMethod, MethodSignature signature)
        {
            // Unmanaged calls are never fat pointers
            if ((signature.Flags & MethodSignatureFlags.UnmanagedCallingConventionMask) != 0)
                return false;

            if (containingMethod.OwningType is MetadataType owningType)
            {
                // RawCalliHelper is a way for the class library to opt out of fat calls
                if (owningType.Name == "RawCalliHelper")
                    return false;

                // Delegate invocation never needs fat calls
                if (owningType.IsDelegate && containingMethod.Name == "Invoke")
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Retrieves method whose runtime handle is suitable for use with GVMLookupForSlot.
        /// </summary>
        public MethodDesc GetTargetOfGenericVirtualMethodCall(MethodDesc calledMethod)
        {
            // Should be a generic virtual method
            Debug.Assert(calledMethod.HasInstantiation && calledMethod.IsVirtual);

            // Needs to be either a concrete method, or a runtime determined form.
            Debug.Assert(!calledMethod.IsCanonicalMethod(CanonicalFormKind.Specific));

            MethodDesc targetMethod = calledMethod.GetCanonMethodTarget(CanonicalFormKind.Specific);
            MethodDesc targetMethodDefinition = targetMethod.GetMethodDefinition();

            MethodDesc slotNormalizedMethodDefinition = MetadataVirtualMethodAlgorithm.FindSlotDefiningMethodForVirtualMethod(targetMethodDefinition);

            // If the method defines the slot, we can use that.
            if (slotNormalizedMethodDefinition == targetMethodDefinition)
            {
                return calledMethod;
            }

            // Normalize to the slot defining method
            InstantiatedMethod slotNormalizedMethod = TypeSystemContext.GetInstantiatedMethod(
                slotNormalizedMethodDefinition,
                targetMethod.Instantiation);

            // Since the slot normalization logic modified what method we're looking at, we need to compute the new target of lookup.
            //
            // If we could use virtual method resolution logic with runtime determined methods, we wouldn't need what we're going
            // to do below.
            MethodDesc runtimeDeterminedSlotNormalizedMethod;
            if (!slotNormalizedMethod.OwningType.IsCanonicalSubtype(CanonicalFormKind.Any))
            {
                // If the owning type is not generic, we can use it as-is, potentially only replacing the runtime-determined
                // method instantiation part.
                runtimeDeterminedSlotNormalizedMethod = slotNormalizedMethod.GetMethodDefinition();
            }
            else
            {
                // If we need a runtime lookup but a normalization to the slot defining method happened above, we need to compute
                // the runtime lookup in terms of the base type that introduced the slot.
                //
                // To do that, we walk the base hierarchy of the runtime determined thing, looking for a type definition that matches
                // the slot-normalized virtual method. We then find the method on that type.
                TypeDesc runtimeDeterminedOwningType = calledMethod.OwningType;

                Debug.Assert(!runtimeDeterminedOwningType.IsInterface);

                while (!slotNormalizedMethod.OwningType.HasSameTypeDefinition(runtimeDeterminedOwningType))
                {
                    DefType runtimeDeterminedBaseTypeDefinition = runtimeDeterminedOwningType.GetTypeDefinition().BaseType;
                    if (runtimeDeterminedBaseTypeDefinition.HasInstantiation)
                    {
                        runtimeDeterminedOwningType = runtimeDeterminedBaseTypeDefinition.InstantiateSignature(runtimeDeterminedOwningType.Instantiation, default);
                    }
                    else
                    {
                        runtimeDeterminedOwningType = runtimeDeterminedBaseTypeDefinition;
                    }
                }

                // Now get the method on the newly found type
                Debug.Assert(runtimeDeterminedOwningType.HasInstantiation);
                runtimeDeterminedSlotNormalizedMethod = TypeSystemContext.GetMethodForInstantiatedType(
                    slotNormalizedMethod.GetTypicalMethodDefinition(),
                    (InstantiatedType)runtimeDeterminedOwningType);
            }

            return TypeSystemContext.GetInstantiatedMethod(runtimeDeterminedSlotNormalizedMethod, calledMethod.Instantiation);
        }

        CompilationResults ICompilation.Compile(string outputFile, ObjectDumper dumper)
        {
            dumper?.Begin();

            CompileInternal(outputFile, dumper);

            dumper?.End();

            return new CompilationResults(_dependencyGraph, _nodeFactory);
        }

        private sealed class ILCache : LockFreeReaderHashtable<MethodDesc, ILCache.MethodILData>
        {
            public ILProvider ILProvider { get; }

            public ILCache(ILProvider provider)
            {
                ILProvider = provider;
            }

            protected override int GetKeyHashCode(MethodDesc key)
            {
                return key.GetHashCode();
            }
            protected override int GetValueHashCode(MethodILData value)
            {
                return value.Method.GetHashCode();
            }
            protected override bool CompareKeyToValue(MethodDesc key, MethodILData value)
            {
                return ReferenceEquals(key, value.Method);
            }
            protected override bool CompareValueToValue(MethodILData value1, MethodILData value2)
            {
                return ReferenceEquals(value1.Method, value2.Method);
            }
            protected override MethodILData CreateValueFromKey(MethodDesc key)
            {
                return new MethodILData() { Method = key, MethodIL = ILProvider.GetMethodIL(key) };
            }

            internal sealed class MethodILData
            {
                public MethodDesc Method;
                public MethodIL MethodIL;
            }
        }

        private sealed class CombinedILProvider : ILProvider
        {
            private readonly ILProvider _primaryILProvider;
            private readonly PInvokeILProvider _pinvokeProvider;

            public CombinedILProvider(ILProvider primaryILProvider, PInvokeILProvider pinvokeILProvider)
            {
                _primaryILProvider = primaryILProvider;
                _pinvokeProvider = pinvokeILProvider;
            }

            public override MethodIL GetMethodIL(MethodDesc method)
            {
                MethodIL result = _primaryILProvider.GetMethodIL(method);
                if (result == null && method.IsPInvoke)
                    result = _pinvokeProvider.GetMethodIL(method);

                return result;
            }
        }
    }

    // Interface under which Compilation is exposed externally.
    public interface ICompilation
    {
        CompilationResults Compile(string outputFileName, ObjectDumper dumper);
    }

    public class CompilationResults
    {
        private readonly DependencyAnalyzerBase<NodeFactory> _graph;
        protected readonly NodeFactory _factory;

        protected ImmutableArray<DependencyNodeCore<NodeFactory>> MarkedNodes
        {
            get
            {
                return _graph.MarkedNodeList;
            }
        }

        internal CompilationResults(DependencyAnalyzerBase<NodeFactory> graph, NodeFactory factory)
        {
            _graph = graph;
            _factory = factory;
        }

        public void WriteDependencyLog(string fileName)
        {
            using (FileStream dgmlOutput = new FileStream(fileName, FileMode.Create))
            {
                DgmlWriter.WriteDependencyGraphToStream(dgmlOutput, _graph, _factory);
                dgmlOutput.Flush();
            }
        }

        public IEnumerable<MethodDesc> CompiledMethodBodies
        {
            get
            {
                foreach (var node in MarkedNodes)
                {
                    if (node is IMethodBodyNode methodBodyNode)
                        yield return methodBodyNode.Method;
                }
            }
        }

        public IEnumerable<TypeDesc> ConstructedEETypes
        {
            get
            {
                foreach (var node in MarkedNodes)
                {
                    if (node is ConstructedEETypeNode || node is CanonicalEETypeNode)
                        yield return ((IEETypeNode)node).Type;
                }
            }
        }

        public IEnumerable<TypeDesc> AllEETypes
        {
            get
            {
                foreach (var node in MarkedNodes)
                {
                    if (node is IEETypeNode typeNode)
                        yield return typeNode.Type;
                }
            }
        }

        public IEnumerable<MethodDesc> ReflectedMethods
        {
            get
            {
                foreach (var node in MarkedNodes)
                {
                    if (node is ReflectedMethodNode reflectedMethod)
                        yield return reflectedMethod.Method;
                }
            }
        }
    }

    public sealed class ConstrainedCallInfo
    {
        public readonly TypeDesc ConstrainedType;
        public readonly MethodDesc Method;
        public ConstrainedCallInfo(TypeDesc constrainedType, MethodDesc method)
            => (ConstrainedType, Method) = (constrainedType, method);
        public int CompareTo(ConstrainedCallInfo other, TypeSystemComparer comparer)
        {
            int result = comparer.Compare(ConstrainedType, other.ConstrainedType);
            if (result == 0)
                result = comparer.Compare(Method, other.Method);
            return result;
        }
        public override bool Equals(object obj) =>
            obj is ConstrainedCallInfo other
            && ConstrainedType == other.ConstrainedType
            && Method == other.Method;

        public override int GetHashCode() => HashCode.Combine(ConstrainedType, Method);
    }
}
