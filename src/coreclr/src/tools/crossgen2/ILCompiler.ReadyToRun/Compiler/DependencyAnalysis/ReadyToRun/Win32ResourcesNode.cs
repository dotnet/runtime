// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using Internal.NativeFormat;
using Internal.Text;
using ILCompiler.Win32Resources;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class Win32ResourcesNode : ObjectNode, ISymbolDefinitionNode
    {
        private ResourceData _resourceData;
        private int _size;

        public Win32ResourcesNode(ResourceData resourceData)
        {
            _resourceData = resourceData;
        }

        public override ObjectNodeSection Section => ObjectNodeSection.TextSection;

        public override bool IsShareable => false;

        public override int ClassCode => 315358339;

        public override bool StaticDependenciesAreComputed => true;

        public int Offset => 0;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("____Win32Resources");
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
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

        public int Size => _size;
    }
}
