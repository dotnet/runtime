// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Internal.Text;

namespace ILCompiler.DependencyAnalysis.ReadyToRun;

public sealed class AuxiliaryModuleMetadataNode : HeaderTableNode
{
    public const int HeaderSize = 16;
    public const int EntrySize = 16;
    public const int EntryPointsStatusOffset = 0;
    public const int ModuleStatusOffset = HeaderSize + 12;

    public string OutputFileName { get; set; }

    public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder builder)
        => builder.Append(nameMangler.CompilationUnitPrefix).Append("__AuxiliaryModuleMetadata"u8);

    public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
    {
        ObjectDataBuilder builder = new(factory, relocsOnly);
        builder.RequireInitialAlignment(4);
        builder.AddSymbol(this);
        int primaryCount = 0;
        int auxiliaryCount = 0;
        if (!relocsOnly)
        {
            foreach (MethodWithGCInfo method in factory.EnumerateCompiledMethods())
            {
                if (factory.IsAuxiliaryMethod(method))
                    auxiliaryCount += method.FrameInfos.Length;
                else
                    primaryCount += method.FrameInfos.Length;
            }
        }

        byte[] entryPointsName = Encoding.UTF8.GetBytes(OutputFileName + ".entrypoints.wasm\0");
        byte[] moduleName = Encoding.UTF8.GetBytes(OutputFileName + ".1.wasm\0");
        builder.EmitInt(0);
        builder.EmitInt(primaryCount);
        builder.EmitInt(1);
        builder.EmitReloc(this, RelocType.IMAGE_REL_BASED_ADDR32NB, HeaderSize + EntrySize);
        builder.EmitReloc(this, RelocType.IMAGE_REL_BASED_ADDR32NB, HeaderSize + EntrySize + entryPointsName.Length);
        builder.EmitInt(primaryCount);
        builder.EmitInt(auxiliaryCount);
        builder.EmitInt(0);
        builder.EmitBytes(entryPointsName);
        builder.EmitBytes(moduleName);
        return builder.ToObjectData();
    }

    public override int ClassCode => 193275841;
}
