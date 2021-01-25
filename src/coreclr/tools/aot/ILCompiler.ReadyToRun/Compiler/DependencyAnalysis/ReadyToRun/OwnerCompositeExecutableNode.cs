// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// R2R header section holding the name of the composite R2R executable with native code
    /// for this component module. This section gets put into R2R headers emitted when
    /// rewriting input MSIL into standalone MSIL components of a composite R2R image.
    /// It is used by the runtime as forwarding information to locate the composite R2R image
    /// with the native code for a given MSIL assembly.
    /// </summary>
    internal class OwnerCompositeExecutableNode : HeaderTableNode
    {
        public override ObjectNodeSection Section => ObjectNodeSection.ReadOnlyDataSection;

        public override int ClassCode => 240420333;

        private readonly string _ownerExecutableName;

        public OwnerCompositeExecutableNode(TargetDetails target, string ownerExecutableName)
            : base(target)
        {
            _ownerExecutableName = ownerExecutableName;
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("__ReadyToRunHeader_OwnerCompositeExecutable");
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.AddSymbol(this);
            builder.EmitBytes(Encoding.UTF8.GetBytes(_ownerExecutableName));
            builder.EmitByte(0); // zero string terminator
            return builder.ToObjectData();
        }
    }
}
