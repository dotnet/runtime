// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysis.ReadyToRun;
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
        private HashSet<MethodDesc> _methodsGenerated = new HashSet<MethodDesc>();

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
                _methodsGenerated.Add(methodNode.Method);
            }
        }
        public IEnumerable<MethodDesc> GetCompiledMethods()
        {
            return _methodsGenerated;
        }
    }

    public class ReadyToRunTableManager : MetadataManager
    {
        public ReadyToRunTableManager(CompilerTypeSystemContext typeSystemContext)
            : base(typeSystemContext) {}

        public IEnumerable<TypeInfo<TypeDefinitionHandle>> GetDefinedTypes()
        {
            foreach (string inputFile in _typeSystemContext.InputFilePaths.Values)
            {
                EcmaModule module = _typeSystemContext.GetModuleFromPath(inputFile);
                foreach (TypeDefinitionHandle typeDefHandle in module.MetadataReader.TypeDefinitions)
                {
                    yield return new TypeInfo<TypeDefinitionHandle>(module.MetadataReader, typeDefHandle);
                }
            }
        }

            public IEnumerable<TypeInfo<ExportedTypeHandle>> GetExportedTypes()
        {
            foreach (string inputFile in _typeSystemContext.InputFilePaths.Values)
            {
                EcmaModule module = _typeSystemContext.GetModuleFromPath(inputFile);
                foreach (ExportedTypeHandle exportedTypeHandle in module.MetadataReader.ExportedTypes)
                {
                    yield return new TypeInfo<ExportedTypeHandle>(module.MetadataReader, exportedTypeHandle);
                }
            }
        }
    }
}
