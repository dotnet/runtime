// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;
using Internal.NativeFormat;
using Internal.Text;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.ReadyToRun
{
    internal class TypeMapAssemblyTargetsNode : ObjectNode, ISymbolDefinitionNode
    {
        private TypeMapMetadata _assemblyTypeMaps;
        private ImportReferenceProvider _importReferenceProvider;

        public TypeMapAssemblyTargetsNode(TypeMapMetadata assemblyTypeMaps, ImportReferenceProvider importReferenceProvider)
        {
            _assemblyTypeMaps = assemblyTypeMaps;
            _importReferenceProvider = importReferenceProvider;
        }

        public override bool IsShareable => false;

        public override int ClassCode => 1564556383;

        public override bool StaticDependenciesAreComputed => true;

        public int Offset => 0;

        protected override DependencyList ComputeNonRelocationBasedDependencies(NodeFactory factory)
        {
            DependencyList dependencies = [];
            foreach (var map in _assemblyTypeMaps.Maps)
            {
                var groupType = map.Key;
                dependencies.Add(new DependencyListEntry(_importReferenceProvider.GetImportToType(groupType), "Type Map Assembly Target"));
                foreach (var targetModule in map.Value.TargetModules)
                {
                    dependencies.Add(new DependencyListEntry(_importReferenceProvider.GetImportToModule(targetModule), "Type Map Assembly Target"));
                }
            }
            return dependencies;
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            // This node does not trigger generation of other nodes.
            if (relocsOnly)
                return new ObjectData([], [], 1, [this]);

            ObjectDataBuilder builder = new(factory, relocsOnly);
            builder.AddSymbol(this);

            NativeWriter writer = new();
            Section section = writer.NewSection();

            VertexHashtable table = new();
            section.Place(table);

            foreach (var map in _assemblyTypeMaps.Maps)
            {
                var groupType = map.Key;
                Vertex groupTypeVertex = _importReferenceProvider.EncodeReferenceToType(writer, groupType);
                VertexSequence modules = new();
                foreach (var targetModule in map.Value.TargetModules)
                {
                    Vertex targetModuleVertex = _importReferenceProvider.EncodeReferenceToModule(writer, targetModule);
                    modules.Append(targetModuleVertex);
                }
                Vertex entry = writer.GetTuple(groupTypeVertex, modules);
                table.Append((uint)groupType.GetHashCode(), section.Place(entry));
            }

            builder.EmitBytes(writer.Save());
            return builder.ToObjectData();
        }
        public override ObjectNodeSection GetSection(NodeFactory factory) => ObjectNodeSection.ReadOnlyDataSection;
        protected override string GetName(NodeFactory context) => "Type Map Assembly Targets Tables";
        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb) => sb.Append(nameMangler.CompilationUnitPrefix).Append("__TypeMapAssemblyTargets"u8);
    }
}
