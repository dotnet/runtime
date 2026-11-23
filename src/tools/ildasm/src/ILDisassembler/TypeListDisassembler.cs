// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;

namespace ILDisassembler;

// /TYPELIST: the full list of the types, which preserves their order when the output is reassembled.
internal sealed class TypeListDisassembler : IMetadataDisassembler
{
    public DisassemblyPhase Phase => DisassemblyPhase.BeforeManifest;

    public void Disassemble(Dasm dasm)
    {
        if (dasm.Classes.Count > 1)
        {
            dasm.PrintLine("");
            dasm.PrintLine($"{dasm.Indent}{dasm.KEYWORD(".typelist")} ");
            dasm.PrintLine(dasm.Indent + dasm.SCOPE());
            dasm.PushIndent();
            foreach (TypeDefinitionHandle cl in dasm.Classes)
            {
                dasm.PrintLine(dasm.Indent + dasm.PrettyPrintClass(cl));
            }

            dasm.PopIndent();
            dasm.PrintLine(dasm.Indent + dasm.UNSCOPE());
            dasm.PrintLine("");
        }
    }
}
