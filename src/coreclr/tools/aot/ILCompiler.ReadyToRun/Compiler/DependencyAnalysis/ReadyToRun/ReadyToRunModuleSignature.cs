// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

using Internal.Text;
using Internal.ReadyToRunConstants;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class ReadyToRunModuleSignature(IEcmaModule module) : Signature
    {
        public override int ClassCode => 208107955;

        public IEcmaModule Module { get; } = module;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataSignatureBuilder builder = new ObjectDataSignatureBuilder(factory, relocsOnly);
            builder.AddSymbol(this);
            builder.EmitFixup(factory, ReadyToRunFixupKind.Helper, Module, factory.SignatureContext);
            builder.EmitUInt((uint)ReadyToRunHelper.Module);
            return builder.ToObjectData();
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append("ReadyToRunModule_"u8);
            sb.Append(Module.Assembly.Name);
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return Module.CompareTo(((ReadyToRunModuleSignature)other).Module);
        }
    }
}
