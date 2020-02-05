// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// Stores information about assemblies that this module has fragile dependencies on.
    /// </summary>
    public class NativeDependencieNode : HeaderTableNode
    {
        protected internal override int Phase => (int)ObjectNodePhase.Ordered;

        public override int ClassCode => (int)ObjectNodeOrder.NativeDependenciesNode;

        public NativeDependencieNode(TargetDetails target)
            : base(target)
        {
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append("__ReadyToRunNativeDependenciesNode");
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            // This node does not trigger generation of other nodes.
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });

            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.RequireInitialPointerAlignment();
            builder.AddSymbol(this);

            foreach (var nativeDependency in factory.ManifestMetadataTable.GetFragileDependencies(factory.TypeSystemContext))
            {
                builder.EmitInt(nativeDependency.Index);
                builder.EmitBytes(nativeDependency.Mvid.ToByteArray());
            }

            return builder.ToObjectData();
        }
    }
}
