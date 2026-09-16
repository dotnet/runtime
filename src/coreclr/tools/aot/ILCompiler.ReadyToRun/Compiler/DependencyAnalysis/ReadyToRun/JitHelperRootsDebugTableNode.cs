// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Internal.ReadyToRunConstants;
using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    internal sealed class JitHelperRootsDebugTableNode : HeaderTableNode
    {
        private readonly TargetDetails _target;

        public JitHelperRootsDebugTableNode(TargetDetails target)
        {
            _target = target;
        }

        public override ObjectNodeSection GetSection(NodeFactory factory) => ObjectNodeSection.ReadOnlyDataSection;

        public override int ClassCode => -1527761278;

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("__ReadyToRunHeader_JitHelperRoots"u8);
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.RequireInitialPointerAlignment();
            builder.AddSymbol(this);

            List<ReadyToRunHelper> helpers = new List<ReadyToRunHelper>(JitHelperRoots.GetReadyToRunHelpers(_target));
            builder.EmitUInt((uint)helpers.Count);
            foreach (ReadyToRunHelper helper in helpers)
            {
                builder.EmitUInt((uint)helper);
            }

            return builder.ToObjectData();
        }
    }
}
