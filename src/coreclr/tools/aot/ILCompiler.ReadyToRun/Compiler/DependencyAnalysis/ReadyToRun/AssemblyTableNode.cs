// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata.Ecma335;

using Internal.JitInterface;
using Internal.NativeFormat;
using Internal.Runtime;
using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class AssemblyTableNode : HeaderTableNode
    {
        private readonly List<AssemblyHeaderNode> _assemblyHeaders;

        public AssemblyTableNode(TargetDetails target)
            : base(target)
        {
            _assemblyHeaders = new List<AssemblyHeaderNode>();
        }

        public void Add(AssemblyHeaderNode componentAssemblyHeader)
        {
            _assemblyHeaders.Add(componentAssemblyHeader);
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append("__ReadyToRunAssemblyTable");
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.AddSymbol(this);
            foreach (AssemblyHeaderNode assemblyHeader in _assemblyHeaders)
            {
                // TODO: IMAGE_DATA_DIRECTORY CorHeader - no support for embedded MSIL yet
                builder.EmitInt(0);
                builder.EmitInt(0);
                // IMAGE_DATA_DIRECTORY ReadyToRunHeader
                builder.EmitReloc(assemblyHeader, RelocType.IMAGE_REL_BASED_ADDR32NB);
                builder.EmitReloc(assemblyHeader, RelocType.IMAGE_REL_SYMBOL_SIZE);
            }
            return builder.ToObjectData();
        }

        public override int ClassCode => 513314416;
    }
}
