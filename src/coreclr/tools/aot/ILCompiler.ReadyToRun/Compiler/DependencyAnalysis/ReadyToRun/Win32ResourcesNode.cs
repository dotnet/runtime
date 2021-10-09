// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.Text;
using ILCompiler.Win32Resources;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class Win32ResourcesNode : ObjectNode, ISymbolDefinitionNode
    {
        private ResourceData _resourceData;
        private int _size;

        public Win32ResourcesNode(ResourceData resourceData)
        {
            _resourceData = resourceData;
            _size = -1;
        }

        public override ObjectNodeSection Section => ObjectNodeSection.TextSection;

        public override bool IsShareable => false;

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;

        public override int ClassCode => (int)ObjectNodeOrder.Win32ResourcesNode;

        public override bool StaticDependenciesAreComputed => true;

        public int Offset => 0;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("____Win32Resources");
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            return GetDataInternal();
        }

        private ObjectData GetDataInternal()
        {
            ObjectDataBuilder builder = new ObjectDataBuilder();
            builder.AddSymbol(this);
            _resourceData.WriteResources(this, ref builder);
            _size = builder.CountBytes;
            return builder.ToObjectData();
        }

        protected override string GetName(NodeFactory context)
        {
            return "____Win32Resources";
        }

        public int Size
        {
            get
            {
                if (_size < 0)
                {
                    GetDataInternal();
                }
                return _size;
            }
        }
    }
}
