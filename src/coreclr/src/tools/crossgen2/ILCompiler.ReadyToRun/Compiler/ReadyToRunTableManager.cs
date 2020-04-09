// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        private bool _sortedMethods = false;

        public MetadataManager(CompilerTypeSystemContext context)
        {
            _typeSystemContext = context;
        }

        public void AttachToDependencyGraph(DependencyAnalyzerBase<NodeFactory> graph)
        {
            graph.NewMarkedNode += Graph_NewMarkedNode;
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

        public IEnumerable<IMethodNode> GetCompiledMethods(EcmaModule module, bool? genericInstantiations)
        {
            lock (_methodsGenerated)
            {
                if (!_sortedMethods)
                {
                    TypeSystemComparer comparer = new TypeSystemComparer();
                    Comparison<IMethodNode> sortHelper = (x, y) => comparer.Compare(x.Method, y.Method);

                    List<PerModuleMethodsGenerated> perModuleDatas = new List<PerModuleMethodsGenerated>(_methodsGenerated.Values);
                    perModuleDatas.Sort((x, y) => x.Module.CompareTo(y.Module));

                    foreach (var perModuleData in perModuleDatas)
                    {
                        perModuleData.MethodsGenerated.Sort(sortHelper);
                        perModuleData.GenericMethodsGenerated.Sort(sortHelper);
                        _completeSortedMethods.AddRange(perModuleData.MethodsGenerated);
                        _completeSortedMethods.AddRange(perModuleData.GenericMethodsGenerated);
                        _completeSortedGenericMethods.AddRange(perModuleData.GenericMethodsGenerated);
                    }
                    _sortedMethods = true;
                }
            }
            if (module == null)
            {
                if (!genericInstantiations.HasValue)
                {
                    return _completeSortedMethods;
                }
                else if (genericInstantiations.Value)
                {
                    return _completeSortedGenericMethods;
                }
                else
                {
                    // This isn't expected to be needed, and thus isn't implemented
                    throw new ArgumentException();
                }
            }
            else if (_methodsGenerated.TryGetValue(module, out var perModuleData))
            {
                if (!genericInstantiations.HasValue)
                {
                    return GetCompiledMethodsAllMethodsInModuleHelper(module);
                }

                if (genericInstantiations.Value)
                {
                    return perModuleData.GenericMethodsGenerated;
                }
                else
                {
                    return perModuleData.MethodsGenerated;
                }
            }
            else
            {
                return Array.Empty<IMethodNode>();
            }
        }

        private IEnumerable<IMethodNode> GetCompiledMethodsAllMethodsInModuleHelper(EcmaModule module)
        {
            foreach (var node in GetCompiledMethods(module, true))
            {
                yield return node;
            }
            foreach (var node in GetCompiledMethods(module, false))
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
