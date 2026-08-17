// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

using Microsoft.DotNet.XUnitExtensions;

using Xunit;

namespace ILCompiler.Compiler.Tests
{
    public class WasmSingleMethodTests
    {
        private const string ExportName = "ILCompiler_Compiler_Tests_Assets_SwitchTest__TestEntryPoint";
        private static readonly byte[] WasmHeader = [0x00, 0x61, 0x73, 0x6D, 0x01, 0x00, 0x00, 0x00];

        public static bool IsWasmCompilationSupported =>
            string.Equals(
                AppContext.GetData("NativeAotWasmTest.IsSupported") as string,
                "true",
                StringComparison.OrdinalIgnoreCase);

        public static bool IsWasmExecutionSupported =>
            IsWasmCompilationSupported &&
            RunProcess(
                "node",
                ["-e", "process.exit(typeof WebAssembly.Tag === 'function' ? 0 : 1)"],
                throwOnError: false).ExitCode == 0;

        [ConditionalFact(nameof(IsWasmCompilationSupported))]
        public void NativeAotWasmSingleMethodCompiles()
        {
            string outputPath = CompileSwitchTest();
            try
            {
                byte[] output = File.ReadAllBytes(outputPath);
                Assert.True(output.Length >= WasmHeader.Length);
                Assert.Equal(WasmHeader, output.AsSpan(0, WasmHeader.Length).ToArray());
            }
            finally
            {
                File.Delete(outputPath);
            }
        }

        [ConditionalFact(nameof(IsWasmExecutionSupported))]
        public void NativeAotWasmSingleMethodExecutes()
        {
            string outputPath = CompileSwitchTest();
            string scriptPath = Path.ChangeExtension(outputPath, ".js");
            try
            {
                File.WriteAllText(scriptPath,
                    $$"""
                    const fs = require("fs");
                    const bytes = fs.readFileSync({{ToJavaScriptString(outputPath)}});
                    if (!WebAssembly.validate(bytes)) {
                        throw new Error("NativeAOT produced an invalid WebAssembly module.");
                    }
                    const webcil = {
                        __stack_pointer: new WebAssembly.Global({ value: "i32", mutable: true }, 65000),
                        __memory_base: new WebAssembly.Global({ value: "i32", mutable: false }, 0),
                        __table_base: new WebAssembly.Global({ value: "i32", mutable: false }, 0),
                        __async_continuation: new WebAssembly.Global({ value: "i32", mutable: true }, 0),
                        table: new WebAssembly.Table({ initial: 4096, element: "anyfunc" }),
                        rtlRestoreContextTag: new WebAssembly.Tag({ parameters: [] }),
                        memory: new WebAssembly.Memory({ initial: 32 }),
                    };
                    WebAssembly.instantiate(bytes, { webcil }).then(({ instance }) => {
                        const result = instance.exports.{{ExportName}}(65000, 0);
                        if (result !== 100) {
                            throw new Error(`Expected 100, got ${result}.`);
                        }
                    });
                    """);

                ProcessResult result = RunProcess("node", [scriptPath], throwOnError: false);
                Assert.True(result.ExitCode == 0, result.Output);
            }
            finally
            {
                File.Delete(scriptPath);
                File.Delete(outputPath);
            }
        }

        private static string CompileSwitchTest()
        {
            string coreClrArtifactsDir = Assert.IsType<string>(AppContext.GetData("NativeAotWasmTest.CoreCLRArtifactsDir"));
            string buildArchitecture = Assert.IsType<string>(AppContext.GetData("NativeAotWasmTest.BuildArchitecture"));
            string ilcPath = Path.Combine(
                coreClrArtifactsDir,
                buildArchitecture,
                "ilc",
                OperatingSystem.IsWindows() ? "ilc.exe" : "ilc");
            string jitFileName = OperatingSystem.IsWindows()
                ? $"clrjit_universal_wasm_{buildArchitecture}.dll"
                : OperatingSystem.IsMacOS()
                    ? $"libclrjit_universal_wasm_{buildArchitecture}.dylib"
                    : $"libclrjit_universal_wasm_{buildArchitecture}.so";
            string jitPath = Path.Combine(coreClrArtifactsDir, jitFileName);
            if (!File.Exists(jitPath))
            {
                jitPath = Path.Combine(coreClrArtifactsDir, buildArchitecture, jitFileName);
            }

            Assert.True(File.Exists(jitPath), $"WASM JIT not found at '{jitPath}'.");

            string outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.wasm");
            try
            {
                RunProcess(
                    ilcPath,
                    [
                        "--singlemethodtypename", "SwitchTest, ILCompiler.Compiler.Tests.Assets",
                        "--singlemethodname", "TestEntryPoint",
                        Path.Combine(AppContext.BaseDirectory, "ILCompiler.Compiler.Tests.Assets.dll"),
                        $"-r:{Path.Combine(AppContext.BaseDirectory, "Test.CoreLib.dll")}",
                        "--systemmodule:Test.CoreLib",
                        $"-o:{outputPath}",
                        "--targetarch:wasm",
                        "--targetos:browser",
                        $"--jitpath:{jitPath}",
                        "--stacktracedata:none",
                        "--reflectiondata:none",
                    ],
                    throwOnError: true);

                return outputPath;
            }
            catch
            {
                File.Delete(outputPath);
                throw;
            }
        }

        private static ProcessResult RunProcess(string fileName, IEnumerable<string> arguments, bool throwOnError)
        {
            var startInfo = new ProcessStartInfo(fileName)
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };
            foreach (string argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            try
            {
                using Process process = Process.Start(startInfo) ??
                    throw new InvalidOperationException($"Failed to start '{fileName}'.");
                Task<string> standardOutput = process.StandardOutput.ReadToEndAsync();
                Task<string> standardError = process.StandardError.ReadToEndAsync();
                process.WaitForExit();

                var result = new ProcessResult(
                    process.ExitCode,
                    standardOutput.GetAwaiter().GetResult() + standardError.GetAwaiter().GetResult());
                if (throwOnError && result.ExitCode != 0)
                {
                    throw new InvalidOperationException(result.Output);
                }

                return result;
            }
            catch (Exception ex) when (!throwOnError && ex is Win32Exception or InvalidOperationException)
            {
                return new ProcessResult(-1, ex.ToString());
            }
        }

        private static string ToJavaScriptString(string value) =>
            '"' + value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + '"';

        private readonly struct ProcessResult
        {
            public ProcessResult(int exitCode, string output)
            {
                ExitCode = exitCode;
                Output = output;
            }

            public int ExitCode { get; }
            public string Output { get; }
        }
    }
}
