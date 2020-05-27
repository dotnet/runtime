// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

internal class AotCompiler
{
    /// <summary>
    /// Precompile all assemblies in parallel
    /// </summary>
    public static void PrecompileLibraries(
        string crossCompiler,
        string arch,
        bool parallel,
        string binDir,
        string[] libsToPrecompile,
        IDictionary<string, string> envVariables,
        bool optimize)
    {
        Parallel.ForEach(libsToPrecompile,
            new ParallelOptions { MaxDegreeOfParallelism = parallel ? Environment.ProcessorCount : 1 },
            lib => PrecompileLibrary(crossCompiler, arch, binDir, lib, envVariables, optimize));
    }

    private static void PrecompileLibrary(
        string crossCompiler,
        string arch,
        string binDir,
        string libToPrecompile,
        IDictionary<string, string> envVariables,
        bool optimize)
    {
        Utils.LogInfo($"[AOT] {libToPrecompile}");

        var crossArgs = new StringBuilder();
        crossArgs
            .Append(" -O=gsharedvt,float32")
            .Append(" --nollvm")
            .Append(" --debug");

        string libName = Path.GetFileNameWithoutExtension(libToPrecompile);
        var aotArgs = new StringBuilder();
        aotArgs
            .Append("mtriple=").Append(arch).Append("-ios,")
            .Append("static,")
            .Append("asmonly,")
            .Append("direct-icalls,")
            .Append("no-direct-calls,")
            .Append("dwarfdebug,")
            .Append("outfile=").Append(Path.Combine(binDir, libName + ".dll.s,"))
            //  TODO: enable aotdata
            //.Append("data-outfile=").Append(Path.Combine(binDir, libName + ".aotdata,"))
            //  TODO: enable direct-pinvokes (to get rid of -force_loads)
            //.Append("direct-pinvoke,")
            .Append("full,");

        // TODO: enable Interpreter
        // TODO: enable LLVM
        // TODO: enable System.Runtime.Intrinsics.Arm (LLVM-only for now)
        // e.g. .Append("mattr=+crc,")

        crossArgs
            .Append(" --aot=").Append(aotArgs).Append(" ")
            .Append(libToPrecompile);

        Utils.RunProcess(crossCompiler, crossArgs.ToString(), envVariables, binDir);

        var clangArgs = new StringBuilder();
        if (optimize)
        {
            clangArgs.Append(" -Os");
        }
        clangArgs
            .Append(" -isysroot ").Append(Xcode.Sysroot)
            .Append(" -miphoneos-version-min=10.1")
            .Append(" -arch ").Append(arch)
            .Append(" -c ").Append(Path.Combine(binDir, libName)).Append(".dll.s")
            .Append(" -o ").Append(Path.Combine(binDir, libName)).Append(".dll.o");

        Utils.RunProcess("clang", clangArgs.ToString(), workingDir: binDir);
    }

    public static void GenerateLinkAllFile(string[] objFiles, string outputFile)
    {
        //  Generates 'modules.m' in order to register all managed libraries
        //
        //
        // extern void *mono_aot_module_Lib1_info;
        // extern void *mono_aot_module_Lib2_info;
        // ...
        //
        // void mono_ios_register_modules (void)
        // {
        //     mono_aot_register_module (mono_aot_module_Lib1_info);
        //     mono_aot_register_module (mono_aot_module_Lib2_info);
        //     ...
        // }

        Utils.LogInfo("Generating 'modules.m'...");

        var lsDecl = new StringBuilder();
        lsDecl
            .AppendLine("#include <mono/jit/jit.h>")
            .AppendLine("#include <TargetConditionals.h>")
            .AppendLine()
            .AppendLine("#if TARGET_OS_IPHONE && !TARGET_IPHONE_SIMULATOR")
            .AppendLine();

        var lsUsage = new StringBuilder();
        lsUsage
            .AppendLine("void mono_ios_register_modules (void)")
            .AppendLine("{");
        foreach (string objFile in objFiles)
        {
            string symbol = "mono_aot_module_" +
                            Path.GetFileName(objFile)
                                .Replace(".dll.o", "")
                                .Replace(".", "_")
                                .Replace("-", "_") + "_info";

            lsDecl.Append("extern void *").Append(symbol).Append(';').AppendLine();
            lsUsage.Append("\tmono_aot_register_module (").Append(symbol).Append(");").AppendLine();
        }
        lsDecl
            .AppendLine()
            .Append(lsUsage)
            .AppendLine("}")
            .AppendLine()
            .AppendLine("#endif")
            .AppendLine();

        File.WriteAllText(outputFile, lsDecl.ToString());
        Utils.LogInfo($"Saved to {outputFile}.");
    }
}
