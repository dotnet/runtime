// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        private List<MethodDesc> _methodsGenerated = new List<MethodDesc>();
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
                    _methodsGenerated.Add(methodNode.Method);
                }
            }
        }
        public IEnumerable<MethodDesc> GetCompiledMethods()
        {
            lock (_methodsGenerated)
            {
                if (!_sortedMethods)
                {
                    TypeSystemComparer comparer = new TypeSystemComparer();
                    _methodsGenerated.Sort((x, y) => comparer.Compare(x, y));
                    _sortedMethods = true;
                }
            }
            return _methodsGenerated;
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
