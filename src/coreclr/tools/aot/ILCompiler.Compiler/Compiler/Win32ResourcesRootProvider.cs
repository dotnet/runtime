// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILCompiler.DependencyAnalysis;
using ILCompiler.Win32Resources;

using Internal.Text;
using Internal.TypeSystem.Ecma;

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
                rsrc1data.AddSymbol(rsrc1);
                var rsrc2data = new ObjectDataBuilder(_resourceModule.Context.Target, relocsOnly: true);
                rsrc2data.AddSymbol(rsrc2);

                resData.WriteResources(rsrc2, ref rsrc1data, ref rsrc2data);

                rsrc1.SetData(rsrc1data.ToObjectData());
                rsrc2.SetData(rsrc2data.ToObjectData());

                rootProvider.AddCompilationRoot(rsrc1, "Resource section from input module");
            }
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
