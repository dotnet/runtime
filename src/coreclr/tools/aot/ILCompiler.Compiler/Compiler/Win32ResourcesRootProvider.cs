// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;
using ILCompiler.Win32Resources;

using Internal.Text;
using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    public class Win32ResourcesRootProvider : ICompilationRootProvider
    {
        private readonly EcmaModule _resourceModule;

        public Win32ResourcesRootProvider(EcmaModule resourceModule)
            => _resourceModule = resourceModule;

        void ICompilationRootProvider.AddCompilationRoots(IRootingServiceProvider rootProvider)
        {
            var resData = new ResourceData(_resourceModule);
            if (!resData.IsEmpty)
            {
                var rsrc1 = new ObjectDataNode("_rsrc1", new ObjectNodeSection(".rsrc$01", SectionType.ReadOnly));
                var rsrc2 = new ObjectDataNode("_rsrc2", new ObjectNodeSection(".rsrc$02", SectionType.ReadOnly));

                var rsrc1data = new ObjectDataBuilder(_resourceModule.Context.Target, relocsOnly: true);
                var rsrc2data = new ObjectDataBuilder(_resourceModule.Context.Target, relocsOnly: true);

                resData.WriteResources(rsrc2, ref rsrc1data, ref rsrc2data);

                var data1 = rsrc1data.ToObjectData();
                var data2 = rsrc2data.ToObjectData();

                ArrayBuilder<ISymbolDefinitionNode> symbolDefs = default;
                symbolDefs.Add(rsrc2);
                ArrayBuilder<Relocation> relocs = default;
                foreach (Relocation reloc in data1.Relocs)
                {
                    Debug.Assert(reloc.RelocType == RelocType.IMAGE_REL_BASED_ADDR32NB);
                    int targetOffset =
                        data1.Data[reloc.Offset]
                        | data1.Data[reloc.Offset + 1] << 8
                        | data1.Data[reloc.Offset + 2] << 16
                        | data1.Data[reloc.Offset + 3] << 24;
                    data1.Data[reloc.Offset] = 0;
                    data1.Data[reloc.Offset + 1] = 0;
                    data1.Data[reloc.Offset + 2] = 0;
                    data1.Data[reloc.Offset + 3] = 0;
                    var newSymbolDef = new ObjectAndOffsetSymbolNode(rsrc2, targetOffset, string.Format("$R{0:X8}", targetOffset), false);
                    relocs.Add(new Relocation(RelocType.IMAGE_REL_BASED_ABSOLUTE, reloc.Offset, newSymbolDef));
                    symbolDefs.Add(newSymbolDef);
                }

                data1 = new ObjectNode.ObjectData(data1.Data, relocs.ToArray(), 1, new ISymbolDefinitionNode[] { rsrc1 });
                data2 = new ObjectNode.ObjectData(data2.Data, Array.Empty<Relocation>(), 1, symbolDefs.ToArray());

                rsrc1.SetData(data1);
                rsrc2.SetData(data2);

                System.IO.File.WriteAllBytes("c:\\temp\\rsrc", data1.Data);

                rootProvider.AddCompilationRoot(rsrc1, "Resource section from input module");
                rootProvider.AddCompilationRoot(rsrc2, "Resource section from input module");
            }
        }

        public class ObjectAndOffsetSymbolNode : DependencyNodeCore<NodeFactory>, ISymbolDefinitionNode
        {
            private ObjectNode _object;
            private int _offset;
            private Utf8String _name;
            private bool _includeCompilationUnitPrefix;

            public ObjectAndOffsetSymbolNode(ObjectNode obj, int offset, Utf8String name, bool includeCompilationUnitPrefix)
            {
                _object = obj;
                _offset = offset;
                _name = name;
                _includeCompilationUnitPrefix = includeCompilationUnitPrefix;
            }

            protected override string GetName(NodeFactory factory) => $"Symbol {_name} at offset {_offset.ToStringInvariant()}";

            public override bool HasConditionalStaticDependencies => false;
            public override bool HasDynamicDependencies => false;
            public override bool InterestingForDynamicDependencyAnalysis => false;
            public override bool StaticDependenciesAreComputed => true;

            public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
            {
                if (_includeCompilationUnitPrefix)
                    sb.Append(nameMangler.CompilationUnitPrefix);
                sb.Append(_name);
            }

            int ISymbolNode.Offset => 0;
            int ISymbolDefinitionNode.Offset => _offset;
            public bool RepresentsIndirectionCell => false;

            public void SetSymbolOffset(int offset)
            {
                _offset = offset;
            }

            public ObjectNode Target => _object;

            public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
            {
                return new DependencyListEntry[] { new DependencyListEntry(_object, "ObjectAndOffsetDependency") };
            }

            public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory) => null;
            public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory) => null;
        }

        private sealed class ObjectDataNode : ObjectNode, ISymbolDefinitionNode
        {
            private readonly string _name;
            private readonly ObjectNodeSection _section;
            private ObjectData _data;

            public ObjectDataNode(string name, ObjectNodeSection section)
                => (_name, _section) = (name, section);

            public int Offset => 0;

            public override bool IsShareable => false;

            public override int ClassCode => -45678932;

            public override bool StaticDependenciesAreComputed => true;

            public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb) => sb.Append(_name);

            public override ObjectNodeSection GetSection(NodeFactory factory) => _section;

            protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

            public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false) => _data;

            public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
                => _name.CompareTo(((ObjectDataNode)other)._name);

            public void SetData(ObjectData data) => _data = data;
        }
    }
}
