// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Reflection.Metadata;
using System.Text;

namespace ILDisassembler;

// /CLASSLIST: the list of the classes defined in the module.
internal sealed class ClassListDisassembler : IMetadataDisassembler
{
    public DisassemblyPhase Phase => DisassemblyPhase.Summary;

    public void Disassemble(Dasm dasm) => PrintClassList(dasm);

    private static string Left(string s, int width) => s.Length >= width ? s : s.PadRight(width);

    private static void PrintClassList(Dasm dasm)
    {
        if (dasm.Classes.Count == 0)
        {
            dasm.PrintLine(dasm.COMMENT("// No classes defined in this module"));
            return;
        }

        dasm.PrintLine(dasm.COMMENT("// Classes defined in this module:"));
        dasm.PrintLine(dasm.COMMENT("//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~"));
        foreach (TypeDefinitionHandle cl in dasm.Classes)
        {
            string name = dasm.MakeNameIfNone(dasm.GetNameOfTypeDef(cl).Name, cl);
            TypeAttributes a = dasm.Metadata.GetTypeDefinition(cl).Attributes;
            TypeAttributes vis = a & TypeAttributes.VisibilityMask;
            StringBuilder sb = new("// ");
            sb.Append((a & TypeAttributes.Interface) != 0 ? "Interface " : "Class ");
            sb.Append(Left(name, 30)).Append(' ');
            if (vis == TypeAttributes.Public) sb.Append("(public) ");
            if ((a & TypeAttributes.Abstract) != 0) sb.Append("(abstract) ");
            switch (a & TypeAttributes.LayoutMask)
            {
                case TypeAttributes.AutoLayout: sb.Append("(auto) "); break;
                case TypeAttributes.SequentialLayout: sb.Append("(sequential) "); break;
                case TypeAttributes.ExtendedLayout: sb.Append("(extended) "); break;
                case TypeAttributes.ExplicitLayout: sb.Append("(explicit) "); break;
            }

            switch (a & TypeAttributes.StringFormatMask)
            {
                case TypeAttributes.AnsiClass: sb.Append("(ansi) "); break;
                case TypeAttributes.UnicodeClass: sb.Append("(unicode) "); break;
                case TypeAttributes.AutoClass: sb.Append("(autochar) "); break;
            }

            if ((a & TypeAttributes.Import) != 0) sb.Append("(import) ");
            if ((a & TypeAttributes.WindowsRuntime) != 0) sb.Append("(windowsruntime) ");
            if ((a & TypeAttributes.Sealed) != 0) sb.Append("(sealed) ");
            switch (vis)
            {
                case TypeAttributes.NestedPublic: sb.Append("(nested public) "); break;
                case TypeAttributes.NestedPrivate: sb.Append("(nested private) "); break;
                case TypeAttributes.NestedFamily: sb.Append("(nested family) "); break;
                case TypeAttributes.NestedAssembly: sb.Append("(nested assembly) "); break;
                case TypeAttributes.NestedFamANDAssem: sb.Append("(nested famANDassem) "); break;
                case TypeAttributes.NestedFamORAssem: sb.Append("(nested famORassem) "); break;
            }

            dasm.PrintLine(dasm.COMMENT(sb.ToString()));
        }

        dasm.PrintLine(dasm.COMMENT("//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~"));
        dasm.PrintLine("");
    }
}
