// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;

using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    public struct TypeInfo<THandle>
    {
        public readonly MetadataReader MetadataReader;
        public readonly THandle Handle;

        public TypeInfo(MetadataReader metadataReader, THandle handle)
        {
            MetadataReader = metadataReader;
            Handle = handle;
        }
    }

    public enum CompiledMethodCategory
    {
        NonInstantiated,
        Instantiated,
        All
    }

    // TODO-REFACTOR: merge with the table manager
    public class MetadataManager
    {
        protected readonly CompilerTypeSystemContext _typeSystemContext;
        private class PerModuleMethodsGenerated
        {
            public PerModuleMethodsGenerated(EcmaModule module)
            {
                Module = module;
            }

            public readonly EcmaModule Module;
            public List<IMethodNode> MethodsGenerated = new List<IMethodNode>();
            public List<IMethodNode> GenericMethodsGenerated = new List<IMethodNode>();
        }

        private Dictionary<EcmaModule, PerModuleMethodsGenerated> _methodsGenerated = new Dictionary<EcmaModule, PerModuleMethodsGenerated>();
        private List<IMethodNode> _completeSortedMethods = new List<IMethodNode>();
        private List<IMethodNode> _completeSortedGenericMethods = new List<IMethodNode>();
        private NodeFactory _factory;

        private bool _sortedMethods = false;

        public MetadataManager(CompilerTypeSystemContext context)
        {
            _typeSystemContext = context;
        }

        public void AttachToDependencyGraph(DependencyAnalyzerBase<NodeFactory> graph, NodeFactory factory)
        {
            graph.NewMarkedNode += Graph_NewMarkedNode;
            _factory = factory;
        }

        protected virtual void Graph_NewMarkedNode(DependencyNodeCore<NodeFactory> obj)
        {
            IMethodBodyNode methodBodyNode = obj as IMethodBodyNode;
            var methodNode = methodBodyNode as IMethodNode;

            if (methodNode != null)
            {
                lock (_methodsGenerated)
                {
                    Debug.Assert(!_sortedMethods);
                    MethodDesc method = methodNode.Method;
                    EcmaModule module = (EcmaModule)((EcmaMethod)method.GetTypicalMethodDefinition()).Module;
                    if (!_methodsGenerated.TryGetValue(module, out var perModuleData))
                    {
                        perModuleData = new PerModuleMethodsGenerated(module);
                        _methodsGenerated[module] = perModuleData;
                    }
                    if (method.HasInstantiation || method.OwningType.HasInstantiation)
                    {
                        perModuleData.GenericMethodsGenerated.Add(methodNode);
                    }
                    else
                    {
                        perModuleData.MethodsGenerated.Add(methodNode);
                    }
                }
            }
        }

        public IEnumerable<IMethodNode> GetCompiledMethods(EcmaModule moduleToEnumerate, CompiledMethodCategory methodCategory)
        {
            lock (_methodsGenerated)
            {
                if (!_sortedMethods)
                {
                    CompilerComparer comparer = CompilerComparer.Instance;
                    SortableDependencyNode.ObjectNodeComparer objectNodeComparer = new SortableDependencyNode.ObjectNodeComparer(comparer);
                    Comparison<IMethodNode> sortHelper = (x, y) =>
                    {
                        int nodeComparerResult = objectNodeComparer.Compare((SortableDependencyNode)x, (SortableDependencyNode)y);
#if DEBUG
                        int methodOnlyResult = comparer.Compare(x.Method, y.Method);

                        // Assert the two sorting techniques produce the same result unless there is a CustomSort applied
                        Debug.Assert((nodeComparerResult == methodOnlyResult) || 
                            ((x is SortableDependencyNode sortableX && sortableX.CustomSort != Int32.MaxValue) ||
                             (y is SortableDependencyNode sortableY && sortableY.CustomSort != Int32.MaxValue)));
#endif
                        return nodeComparerResult;
                    };
                    Comparison<IMethodNode> sortHelperNoCustomSort = (x, y) => comparer.Compare(x, y);

                    List<PerModuleMethodsGenerated> perModuleDatas = new List<PerModuleMethodsGenerated>(_methodsGenerated.Values);
                    perModuleDatas.Sort((x, y) => x.Module.CompareTo(y.Module));

                    foreach (var perModuleData in perModuleDatas)
                    {
                        perModuleData.MethodsGenerated.MergeSort(sortHelperNoCustomSort);
                        perModuleData.GenericMethodsGenerated.MergeSort(sortHelperNoCustomSort);
                        _completeSortedMethods.AddRange(perModuleData.MethodsGenerated);
                        _completeSortedMethods.AddRange(perModuleData.GenericMethodsGenerated);
                        _completeSortedGenericMethods.AddRange(perModuleData.GenericMethodsGenerated);
                    }
                    _completeSortedMethods.MergeSort(sortHelper);
                    _completeSortedGenericMethods.MergeSort(sortHelper);

                    _sortedMethods = true;
                }
            }
            if (moduleToEnumerate == null)
            {
                if (methodCategory == CompiledMethodCategory.All)
                {
                    return _completeSortedMethods;
                }
                else if (methodCategory == CompiledMethodCategory.Instantiated)
                {
                    return _completeSortedGenericMethods;
                }
                else
                {
                    // This isn't expected to be needed, and thus isn't implemented
                    throw new ArgumentException();
                }
            }
            else if (_methodsGenerated.TryGetValue(moduleToEnumerate, out var perModuleData))
            {
                if (methodCategory == CompiledMethodCategory.All)
                {
                    return GetCompiledMethodsAllMethodsInModuleHelper(moduleToEnumerate);
                }

                if (methodCategory == CompiledMethodCategory.Instantiated)
                {
                    return perModuleData.GenericMethodsGenerated;
                }
                else
                {
                    Debug.Assert(methodCategory == CompiledMethodCategory.NonInstantiated);
                    return perModuleData.MethodsGenerated;
                }
            }
            else
            {
                return Array.Empty<IMethodNode>();
            }
        }

        private IEnumerable<IMethodNode> GetCompiledMethodsAllMethodsInModuleHelper(EcmaModule moduleToEnumerate)
        {
            foreach (var node in GetCompiledMethods(moduleToEnumerate, CompiledMethodCategory.Instantiated))
            {
                yield return node;
            }
            foreach (var node in GetCompiledMethods(moduleToEnumerate, CompiledMethodCategory.NonInstantiated))
            {
                yield return node;
            }
        }
    }

    public class ReadyToRunTableManager : MetadataManager
    {
        public ReadyToRunTableManager(CompilerTypeSystemContext typeSystemContext)
            : base(typeSystemContext) {}

        public IEnumerable<TypeInfo<TypeDefinitionHandle>> GetDefinedTypes(EcmaModule module)
        {
            foreach (TypeDefinitionHandle typeDefHandle in module.MetadataReader.TypeDefinitions)
            {
                yield return new TypeInfo<TypeDefinitionHandle>(module.MetadataReader, typeDefHandle);
            }
        }

        public IEnumerable<TypeInfo<ExportedTypeHandle>> GetExportedTypes(EcmaModule module)
        {
            foreach (ExportedTypeHandle exportedTypeHandle in module.MetadataReader.ExportedTypes)
            {
                yield return new TypeInfo<ExportedTypeHandle>(module.MetadataReader, exportedTypeHandle);
            }
        }
    }
}
