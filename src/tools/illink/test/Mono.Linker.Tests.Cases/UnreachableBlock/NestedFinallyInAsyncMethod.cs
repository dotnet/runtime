using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.UnreachableBlock.Dependencies;

namespace Mono.Linker.Tests.Cases.UnreachableBlock
{
    [SetupCSharpCompilerToUse("csc")]
    [SetupCompileArgument("/optimize+")]
    [SetupCompileBefore(
        "library.dll",
        new[] { "Dependencies/NestedFinallyInAsyncMethod_Lib.cs" },
        additionalArguments: new[] { "/optimize+" },
        compilerToUse: "csc")]
    [SetupLinkerArgument("--enable-opt", "ipconstprop")]
    [KeptMemberInAssembly("library.dll", typeof(NestedFinallyInAsyncMethod_Lib), "Cleanup()")]
    public class NestedFinallyInAsyncMethod
    {
        public static void Main()
        {
            NestedFinallyInAsyncMethod_Lib.Test().GetAwaiter().GetResult();

            if (NestedFinallyInAsyncMethod_Lib.CleanupCount != 1)
                throw new InvalidOperationException();
        }
    }
}
