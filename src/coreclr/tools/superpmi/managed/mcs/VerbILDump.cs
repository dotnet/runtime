// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using Internal.JitInterface;

namespace SuperPmi.Mcs;

internal static class VerbILDump
{
    public static int DoWork(string nameOfInput, int[] indexes)
    {
        Logger.LogVerbose($"// Reading from '{nameOfInput}' dumping raw IL for MC Indexes to console");

        using var mci = new MethodContextIterator(indexes);
        if (!mci.Initialize(nameOfInput))
            return -1;

        int dumpedCount = 0;
        while (mci.MoveNext())
        {
            ILDumper.DumpIL(mci.Current);
            dumpedCount++;
        }

        Logger.LogInfo($"// Dumped {dumpedCount}");

        return mci.Destroy() ? 0 : -1;
    }
}

internal static unsafe partial class ILDumper
{
    private static System.IO.TextWriter Out => Logger.Out;

    public static void DumpIL(MethodContext mc)
    {
        CORINFO_METHOD_INFO cmi = mc.repCompileMethod(out _, out _);

        Out.Write($"// ProcessName - '{mc.cr.repProcessName()}'\n");
        Out.Write(".assembly extern mscorlib{}\n");
        Out.Write(".assembly dumped_asm\n");
        Out.Write($".class {Encoding.UTF8.GetString(CallUtils.getClassName(mc, mc.repGetMethodClass(cmi.ftn)))}\n");
        Out.Write("{\n");
        Out.Write("   .method ");
        SigDumper.DumpAttributeToConsoleBare(mc.repGetMethodAttribs(cmi.ftn));
        SigDumper.DumpPrimToConsoleBare(mc, cmi.args.retType, cmi.args.retTypeClass);
        Out.Write($" {Encoding.UTF8.GetString(CallUtils.getMethodName(mc, cmi.ftn))}(");
        SigDumper.DumpSigToConsoleBare(mc, cmi.args);
        Out.Write(")\n");
        Out.Write("   {\n");
        Out.Write($"      .maxstack  {cmi.maxStack}\n");
        Out.Write((cmi.options & CorInfoOptions.CORINFO_OPT_INIT_LOCALS) == CorInfoOptions.CORINFO_OPT_INIT_LOCALS ? "      .locals init (" : "      .locals (");
        SigDumper.DumpSigToConsoleBare(mc, cmi.locals);
        Out.Write(")\n");
        DumpILToConsoleBare(Out, new ReadOnlySpan<byte>(cmi.ILCode, (int)cmi.ILCodeSize).ToArray(), (int)cmi.ILCodeSize);
        Out.Write("   }\n");
        Out.Write("}\n");
    }
}
