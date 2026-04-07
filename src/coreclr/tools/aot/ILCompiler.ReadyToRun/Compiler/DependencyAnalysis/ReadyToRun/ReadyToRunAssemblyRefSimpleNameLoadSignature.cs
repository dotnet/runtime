// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Text;
using Internal.ReadyToRunConstants;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public sealed class ReadyToRunAssemblyRefSimpleNameLoadSignature(IEcmaModule module) : Signature
    {
        public override int ClassCode => -1334381239;

        public IEcmaModule Module { get; } = module;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataSignatureBuilder builder = new(factory, relocsOnly);
            builder.AddSymbol(this);

            if (relocsOnly)
            {
                return builder.ToObjectData();
            }

            builder.EmitFixup(factory, ReadyToRunFixupKind.AssemblyRefSimpleNameLoad, Module, factory.SignatureContext);

            return builder.ToObjectData();
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append("ReadyToRunAssemblyRefSimpleNameLoad_"u8);
            sb.Append(Module.Assembly.Name);
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return Module.CompareTo(((ReadyToRunAssemblyRefSimpleNameLoadSignature)other).Module);
        }
    }
}
