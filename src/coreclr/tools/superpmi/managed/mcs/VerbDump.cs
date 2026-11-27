// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Internal.JitInterface;

namespace SuperPmi.Mcs;

internal static class VerbDump
{
    public static int DoWork(string nameOfInput, int[] indexes, bool simple)
    {
        Logger.LogVerbose($"Dumping '{nameOfInput}' to console");

        using var mci = new MethodContextIterator(indexes);
        if (!mci.Initialize(nameOfInput))
            return -1;

        int dumpedCount = 0;
        while (mci.MoveNext())
        {
            mci.Current.dumpToConsole(mci.MethodContextNumber, simple);
            dumpedCount++;
        }

        Logger.LogVerbose($"Dumped {dumpedCount} methodContexts");

        return mci.Destroy() ? 0 : -1;
    }
}

internal static unsafe class VerbASMDump
{
    // Writes one <output>-<number>.asm file per method context. Open-source builds have no disassembler,
    // so only the header is written.
    public static int DoWork(string nameOfInput, string nameOfOutput, int[] indexes)
    {
        Logger.LogVerbose($"Loading from '{nameOfInput}' and writing ASM output into '{nameOfOutput}-MC#.asm'");

        using var mci = new MethodContextIterator(indexes, progressReport: true);
        if (!mci.Initialize(nameOfInput))
            return -1;

        int savedCount = 0;
        while (mci.MoveNext())
        {
            MethodContext mc = mci.Current;
            using (System.IO.FileStream output = FileHelpers.CreateOutput($"{nameOfOutput}-{mci.MethodContextNumber}.asm"))
            {
                if (output == null)
                    return -1;

                string text = mc.cr.IsEmpty() ? ";;Method context has no compile result" : DumpToFile(mc);
                output.Write(System.Text.Encoding.UTF8.GetBytes(text));
            }
            savedCount++;
        }

        Logger.LogInfo($"Asm'd {savedCount}");

        return mci.Destroy() ? 0 : -1;
    }

    private static string DumpToFile(MethodContext mc)
    {
        CORINFO_METHOD_INFO info = mc.repCompileMethod(out _, out _);

        string header = $";;Generated from SuperPMI on original input '{mc.cr.repProcessName()}'"
            + $"\r\n Method Name \"{Encoding.UTF8.GetString(CallUtils.getMethodName(mc, info.ftn))}\"";

        // A disassembler would read the code here, and relocate it; reading it checks that it was recorded.
        mc.cr.AllocMem.Get(0);

        return header + ";; No disassembler available";
    }
}
