// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;

using Internal.IL;
using Internal.IL.Stubs;
using Internal.JitInterface;
using Internal.TypeSystem;
using Internal.ReadyToRunConstants;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    /// <summary>
    /// IL scan analyzer of programs - this class analyzes what methods, types and other runtime artifact
    /// will need to be generated during a compilation. The result of analysis is a conservative superset of
    /// what methods will be compiled by the actual codegen backend.
    /// </summary>
    internal sealed class ILScanner : Compilation, IILScanner
    {
        private readonly int _parallelism;

        internal ILScanner(
            DependencyAnalyzerBase<NodeFactory> dependencyGraph,
            ILScanNodeFactory nodeFactory,
            IEnumerable<ICompilationRootProvider> roots,
            ILProvider ilProvider,
            DebugInformationProvider debugInformationProvider,
            Logger logger,
            int parallelism)
            : base(dependencyGraph, nodeFactory, roots, ilProvider, debugInformationProvider, null, nodeFactory.CompilationModuleGroup, logger)
        {
            _helperCache = new HelperCache(this);
            _parallelism = parallelism;
        }

        protected override void CompileInternal(string outputFile, ObjectDumper dumper)
        {
            // TODO: We should have a base class for compilation that doesn't implement ICompilation so that
            // we don't need this.
            throw new NotSupportedException();
        }

        protected override void ComputeDependencyNodeDependencies(List<DependencyNodeCore<NodeFactory>> obj)
        {
            // Determine the list of method we actually need to scan
            var methodsToCompile = new List<ScannedMethodNode>();
            var canonicalMethodsToCompile = new HashSet<MethodDesc>();

            foreach (DependencyNodeCore<NodeFactory> dependency in obj)
            {
                var methodCodeNodeNeedingCode = dependency as ScannedMethodNode;
                if (methodCodeNodeNeedingCode == null)
                {
                    // To compute dependencies of the shadow method that tracks dictionary
                    // dependencies we need to ensure there is code for the canonical method body.
                    var dependencyMethod = (ShadowConcreteMethodNode)dependency;
                    methodCodeNodeNeedingCode = (ScannedMethodNode)dependencyMethod.CanonicalMethodNode;
                }

                // We might have already queued this method for compilation
                MethodDesc method = methodCodeNodeNeedingCode.Method;
                if (method.IsCanonicalMethod(CanonicalFormKind.Any)
                    && !canonicalMethodsToCompile.Add(method))
                {
                    continue;
                }

                methodsToCompile.Add(methodCodeNodeNeedingCode);
            }

            if (_parallelism == 1)
            {
                CompileSingleThreaded(methodsToCompile);
            }
            else
            {
                CompileMultiThreaded(methodsToCompile);
            }
        }

        private void CompileMultiThreaded(List<ScannedMethodNode> methodsToCompile)
        {
            if (Logger.IsVerbose)
            {
                Logger.Writer.WriteLine($"Scanning {methodsToCompile.Count} methods...");
            }

            Parallel.ForEach(
                methodsToCompile,
                new ParallelOptions { MaxDegreeOfParallelism = _parallelism },
                CompileSingleMethod);
        }

        private void CompileSingleThreaded(List<ScannedMethodNode> methodsToCompile)
        {
            foreach (ScannedMethodNode methodCodeNodeNeedingCode in methodsToCompile)
            {
                if (Logger.IsVerbose)
                {
                    Logger.Writer.WriteLine($"Compiling {methodCodeNodeNeedingCode.Method}...");
                }

                CompileSingleMethod(methodCodeNodeNeedingCode);
            }
        }

        private void CompileSingleMethod(ScannedMethodNode methodCodeNodeNeedingCode)
        {
            MethodDesc method = methodCodeNodeNeedingCode.Method;

            try
            {
                var importer = new ILImporter(this, method);
                methodCodeNodeNeedingCode.InitializeDependencies(_nodeFactory, importer.Import());
            }
            catch (TypeSystemException ex)
            {
                // Try to compile the method again, but with a throwing method body this time.
                MethodIL throwingIL = TypeSystemThrowingILEmitter.EmitIL(method, ex);
                var importer = new ILImporter(this, method, throwingIL);
                methodCodeNodeNeedingCode.InitializeDependencies(_nodeFactory, importer.Import(), ex);
            }
            catch (Exception ex)
            {
                throw new CodeGenerationFailedException(method, ex);
            }
        }

        ILScanResults IILScanner.Scan()
        {
            _dependencyGraph.ComputeMarkedNodes();

            _nodeFactory.SetMarkingComplete();

            return new ILScanResults(_dependencyGraph, _nodeFactory);
        }

        public ISymbolNode GetHelperEntrypoint(ReadyToRunHelper helper)
        {
            return _helperCache.GetOrCreateValue(helper).Symbol;
        }

        class Helper
        {
            public ReadyToRunHelper HelperID { get; }
            public ISymbolNode Symbol { get; }

            public Helper(ReadyToRunHelper id, ISymbolNode symbol)
            {
                HelperID = id;
                Symbol = symbol;
            }
        }

        private HelperCache _helperCache;
        class HelperCache : LockFreeReaderHashtable<ReadyToRunHelper, Helper>
        {
            private Compilation _compilation;

            public HelperCache(Compilation compilation)
            {
                _compilation = compilation;
            }

            protected override bool CompareKeyToValue(ReadyToRunHelper key, Helper value) => key == value.HelperID;
            protected override bool CompareValueToValue(Helper value1, Helper value2) => value1.HelperID == value2.HelperID;
            protected override int GetKeyHashCode(ReadyToRunHelper key) => (int)key;
            protected override int GetValueHashCode(Helper value) => (int)value.HelperID;
            protected override Helper CreateValueFromKey(ReadyToRunHelper key)
            {
                string mangledName;
                MethodDesc methodDesc;
                JitHelper.GetEntryPoint(_compilation.TypeSystemContext, key, out mangledName, out methodDesc);
                Debug.Assert(mangledName != null || methodDesc != null);

                ISymbolNode entryPoint;
                if (mangledName != null)
                    entryPoint = _compilation.NodeFactory.ExternSymbol(mangledName);
                else
                    entryPoint = _compilation.NodeFactory.MethodEntrypoint(methodDesc);

                return new Helper(key, entryPoint);
            }

        }
    }

    public interface IILScanner
    {
        ILScanResults Scan();
    }

    internal class ScannerFailedException : InternalCompilerErrorException
    {
        public ScannerFailedException(string message)
            : base(message + " " + "You can work around by running the compilation with scanner disabled.")
        {
        }
    }

    public class ILScanResults : CompilationResults
    {
        internal ILScanResults(DependencyAnalyzerBase<NodeFactory> graph, NodeFactory factory)
            : base(graph, factory)
        {
        }

        public AnalysisBasedInteropStubManager GetInteropStubManager(InteropStateManager stateManager, PInvokeILEmitterConfiguration pinvokePolicy)
        {
            return new AnalysisBasedInteropStubManager(stateManager, pinvokePolicy,
                _factory.MetadataManager.GetTypesWithStructMarshalling(),
                _factory.MetadataManager.GetTypesWithDelegateMarshalling());
        }

        public VTableSliceProvider GetVTableLayoutInfo()
        {
            return new ScannedVTableProvider(MarkedNodes);
        }

        public DictionaryLayoutProvider GetDictionaryLayoutInfo()
        {
            return new ScannedDictionaryLayoutProvider(_factory, MarkedNodes);
        }

        public DevirtualizationManager GetDevirtualizationManager()
        {
            return new ScannedDevirtualizationManager(MarkedNodes);
        }

        public IInliningPolicy GetInliningPolicy()
        {
            return new ScannedInliningPolicy(_factory.CompilationModuleGroup, MarkedNodes);
        }

        public MethodImportationErrorProvider GetMethodImportationErrorProvider()
        {
            return new ScannedMethodImportationErrorProvider(MarkedNodes);
        }

        private class ScannedVTableProvider : VTableSliceProvider
        {
            private Dictionary<TypeDesc, IReadOnlyList<MethodDesc>> _vtableSlices = new Dictionary<TypeDesc, IReadOnlyList<MethodDesc>>();

            public ScannedVTableProvider(ImmutableArray<DependencyNodeCore<NodeFactory>> markedNodes)
            {
                foreach (var node in markedNodes)
                {
                    var vtableSliceNode = node as VTableSliceNode;
                    if (vtableSliceNode != null)
                    {
                        _vtableSlices.Add(vtableSliceNode.Type, vtableSliceNode.Slots);
                    }
                }
            }

            internal override VTableSliceNode GetSlice(TypeDesc type)
            {
                // TODO: move ownership of compiler-generated entities to CompilerTypeSystemContext.
                // https://github.com/dotnet/corert/issues/3873
                if (type.GetTypeDefinition() is Internal.TypeSystem.Ecma.EcmaType)
                {
                    if (!_vtableSlices.TryGetValue(type, out IReadOnlyList<MethodDesc> slots))
                    {
                        // If we couln't find the vtable slice information for this type, it's because the scanner
                        // didn't correctly predict what will be needed.
                        // To troubleshoot, compare the dependency graph of the scanner and the compiler.
                        // Follow the path from the node that requested this node to the root.
                        // On the path, you'll find a node that exists in both graphs, but it's predecessor
                        // only exists in the compiler's graph. That's the place to focus the investigation on.
                        // Use the ILCompiler-DependencyGraph-Viewer tool to investigate.
                        Debug.Assert(false);
                        string typeName = ExceptionTypeNameFormatter.Instance.FormatName(type);
                        throw new ScannerFailedException($"VTable of type '{typeName}' not computed by the IL scanner.");
                    }
                    return new PrecomputedVTableSliceNode(type, slots);
                }
                else
                    return new LazilyBuiltVTableSliceNode(type);
            }
        }

        private class ScannedDictionaryLayoutProvider : DictionaryLayoutProvider
        {
            private Dictionary<TypeSystemEntity, IEnumerable<GenericLookupResult>> _layouts = new Dictionary<TypeSystemEntity, IEnumerable<GenericLookupResult>>();
            private HashSet<TypeSystemEntity> _entitiesWithForcedLazyLookups = new HashSet<TypeSystemEntity>();

            public ScannedDictionaryLayoutProvider(NodeFactory factory, ImmutableArray<DependencyNodeCore<NodeFactory>> markedNodes)
            {
                foreach (var node in markedNodes)
                {
                    if (node is DictionaryLayoutNode layoutNode)
                    {
                        TypeSystemEntity owningMethodOrType = layoutNode.OwningMethodOrType;
                        _layouts.Add(owningMethodOrType, layoutNode.Entries);
                    }
                    else if (node is ReadyToRunGenericHelperNode genericLookup
                        && genericLookup.HandlesInvalidEntries(factory))
                    {
                        // If a dictionary layout has an associated lookup helper that contains handling of broken slots
                        // (because one of our precomputed dictionaries contained an uncompilable entry)
                        // we won't hand out a precomputed dictionary and keep using the lookup helpers.
                        // The inlined lookups using the precomputed dictionary wouldn't handle the broken slots.
                        _entitiesWithForcedLazyLookups.Add(genericLookup.DictionaryOwner);
                    }
                }
            }

            private DictionaryLayoutNode GetPrecomputedLayout(TypeSystemEntity methodOrType)
            {
                if (!_layouts.TryGetValue(methodOrType, out IEnumerable<GenericLookupResult> layout))
                {
                    // If we couln't find the dictionary layout information for this, it's because the scanner
                    // didn't correctly predict what will be needed.
                    // To troubleshoot, compare the dependency graph of the scanner and the compiler.
                    // Follow the path from the node that requested this node to the root.
                    // On the path, you'll find a node that exists in both graphs, but it's predecessor
                    // only exists in the compiler's graph. That's the place to focus the investigation on.
                    // Use the ILCompiler-DependencyGraph-Viewer tool to investigate.
                    Debug.Assert(false);
                    throw new ScannerFailedException($"A dictionary layout was not computed by the IL scanner.");
                }
                return new PrecomputedDictionaryLayoutNode(methodOrType, layout);
            }

            public override DictionaryLayoutNode GetLayout(TypeSystemEntity methodOrType)
            {
                if (_entitiesWithForcedLazyLookups.Contains(methodOrType))
                {
                    return new LazilyBuiltDictionaryLayoutNode(methodOrType);
                }

                if (methodOrType is TypeDesc type)
                {
                    // TODO: move ownership of compiler-generated entities to CompilerTypeSystemContext.
                    // https://github.com/dotnet/corert/issues/3873
                    if (type.GetTypeDefinition() is Internal.TypeSystem.Ecma.EcmaType)
                        return GetPrecomputedLayout(type);
                    else
                        return new LazilyBuiltDictionaryLayoutNode(type);
                }
                else
                {
                    Debug.Assert(methodOrType is MethodDesc);
                    MethodDesc method = (MethodDesc)methodOrType;

                    // TODO: move ownership of compiler-generated entities to CompilerTypeSystemContext.
                    // https://github.com/dotnet/corert/issues/3873
                    if (method.GetTypicalMethodDefinition() is Internal.TypeSystem.Ecma.EcmaMethod)
                        return GetPrecomputedLayout(method);
                    else
                        return new LazilyBuiltDictionaryLayoutNode(method);
                }
            }
        }

        private class ScannedDevirtualizationManager : DevirtualizationManager
        {
            private HashSet<TypeDesc> _constructedTypes = new HashSet<TypeDesc>();
            private HashSet<TypeDesc> _unsealedTypes = new HashSet<TypeDesc>();
            private HashSet<TypeDesc> _abstractButNonabstractlyOverridenTypes = new HashSet<TypeDesc>();

            public ScannedDevirtualizationManager(ImmutableArray<DependencyNodeCore<NodeFactory>> markedNodes)
            {
                foreach (var node in markedNodes)
                {
                    if (node is ConstructedEETypeNode eetypeNode)
                    {
                        TypeDesc type = eetypeNode.Type;

                        if (!type.IsInterface)
                        {
                            //
                            // We collect this information:
                            //
                            // 1. What types got allocated
                            // 2. What types are the base types of other types
                            //    This is needed for optimizations. We use this information to effectively
                            //    seal types that are not base types for any other type.
                            // 3. What abstract types got derived by non-abstract types.
                            //    This is needed for correctness. Abstract types that were never derived
                            //    by non-abstract types should never be devirtualized into - we probably
                            //    didn't scan the virtual methods on them.
                            //

                            _constructedTypes.Add(type);

                            TypeDesc canonType = type.ConvertToCanonForm(CanonicalFormKind.Specific);

                            bool hasNonAbstractTypeInHierarchy = canonType is not MetadataType mdType || !mdType.IsAbstract;
                            TypeDesc baseType = canonType.BaseType;
                            bool added = true;
                            while (baseType != null && added)
                            {
                                baseType = baseType.ConvertToCanonForm(CanonicalFormKind.Specific);
                                added = _unsealedTypes.Add(baseType);

                                bool currentTypeIsAbstract = ((MetadataType)baseType).IsAbstract;
                                if (currentTypeIsAbstract && hasNonAbstractTypeInHierarchy)
                                    added |= _abstractButNonabstractlyOverridenTypes.Add(baseType);
                                hasNonAbstractTypeInHierarchy |= !currentTypeIsAbstract;

                                baseType = baseType.BaseType;
                            }
                        }
                    }
                }
            }

            public override bool IsEffectivelySealed(TypeDesc type)
            {
                // If we know we scanned a type that derives from this one, this for sure can't be reported as sealed.
                TypeDesc canonType = type.ConvertToCanonForm(CanonicalFormKind.Specific);
                if (_unsealedTypes.Contains(canonType))
                    return false;

                // Don't report __Canon as sealed or it can cause trouble
                // (E.g. RyuJIT might think it's okay to omit array element type checks for __Canon[].)
                if (type.IsCanonicalDefinitionType(CanonicalFormKind.Any))
                    return false;

                if (type is MetadataType metadataType)
                {
                    // Due to how the compiler is structured, we might see "constructed" EETypes for things
                    // that never got allocated (doing a typeof() on a class that is otherwise never used is
                    // a good example of when that happens). This can put us into a position where we could
                    // report `sealed` on an `abstract` class, but that doesn't lead to anything good.
                    return !metadataType.IsAbstract;
                }

                // Everything else can be considered sealed.
                return true;
            }

            protected override MethodDesc ResolveVirtualMethod(MethodDesc declMethod, DefType implType, out CORINFO_DEVIRTUALIZATION_DETAIL devirtualizationDetail)
            {
                MethodDesc result = base.ResolveVirtualMethod(declMethod, implType, out devirtualizationDetail);
                if (result != null && result.IsFinal && result.OwningType is MetadataType mdType && mdType.IsAbstract)
                {
                    // If this type is abstract check that we saw a non-abstract type deriving from it.
                    // We don't look at virtual methods introduced by abstract classes unless there's a non-abstract
                    // class that needs them (i.e. the non-abstract class doesn't immediately override them).
                    // This lets us optimize out some unused virtual method implementations.
                    // Allowing this to devirtualize would cause trouble because we didn't scan the method
                    // and expected it would be optimized out.
                    if (!_abstractButNonabstractlyOverridenTypes.Contains(mdType.ConvertToCanonForm(CanonicalFormKind.Specific)))
                    {
                        // FAILED_BUBBLE_IMPL_NOT_REFERENCEABLE is close enough...
                        devirtualizationDetail = CORINFO_DEVIRTUALIZATION_DETAIL.CORINFO_DEVIRTUALIZATION_FAILED_BUBBLE_IMPL_NOT_REFERENCEABLE;
                        return null;
                    }
                }

                return result;
            }

            public override bool CanConstructType(TypeDesc type) => _constructedTypes.Contains(type);
        }

        private class ScannedInliningPolicy : IInliningPolicy
        {
            private readonly HashSet<TypeDesc> _constructedTypes = new HashSet<TypeDesc>();
            private readonly CompilationModuleGroup _baseGroup;

            public ScannedInliningPolicy(CompilationModuleGroup baseGroup, ImmutableArray<DependencyNodeCore<NodeFactory>> markedNodes)
            {
                _baseGroup = baseGroup;

                foreach (var node in markedNodes)
                {
                    if (node is ConstructedEETypeNode eetypeNode)
                    {
                        TypeDesc type = eetypeNode.Type;
                        _constructedTypes.Add(type);
                    }
                }
            }

            public bool CanInline(MethodDesc caller, MethodDesc callee)
            {
                if (_baseGroup.CanInline(caller, callee))
                {
                    // Since the scanner doesn't look at instance methods whose owning type
                    // wasn't allocated (done through TentativeInstanceMethodNode),
                    // we need to disallow inlining these methods. They could
                    // bring in dependencies that we didn't look at.
                    if (callee.NotCallableWithoutOwningEEType())
                    {
                        return _constructedTypes.Contains(callee.OwningType);
                    }
                    return true;
                }

                return false;
            }
        }

        private sealed class ScannedMethodImportationErrorProvider : MethodImportationErrorProvider
        {
            private readonly Dictionary<MethodDesc, TypeSystemException> _importationErrors = new Dictionary<MethodDesc, TypeSystemException>();

            public ScannedMethodImportationErrorProvider(ImmutableArray<DependencyNodeCore<NodeFactory>> markedNodes)
            {
                foreach (var markedNode in markedNodes)
                {
                    if (markedNode is ScannedMethodNode scannedMethod
                        && scannedMethod.Exception != null)
                    {
                        _importationErrors.Add(scannedMethod.Method, scannedMethod.Exception);
                    }
                }
            }

            public override TypeSystemException GetCompilationError(MethodDesc method)
                => _importationErrors.TryGetValue(method, out var exception) ? exception : null;
        }
    }
}
