// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using Xunit;

/* How to run this test?

1. Compile the runtime repo using --> PS D:\Git_repos\runtime>  .\build.cmd -subset clr -rc debug
2. Run the following command to setup the test repo --> PS D:\Git_repos\runtime\src\tests> .\build.cmd x64 Debug generatelayoutonly
3. Compile the JIT/HardwareIntrinsics test suite using --> PS D:\Git_repos\runtime\src\tests> .\build.cmd x64 Debug tree JIT/HardwareIntrinsics
4. Use the following variables to run / debug the Avx10 test
    {
        "name": "(Windows) Launch",
        "type": "cppvsdbg",
        "request": "launch",
        "program": "${workspaceFolder}/artifacts/tests/coreclr/windows.x64.Debug/Tests/Core_Root/corerun.exe",
        "args": [
            "D:/Git_repos/runtime/artifacts/tests/coreclr/windows.x64.Debug/JIT/HardwareIntrinsics/HardwareIntrinsics_X86_Avx10_r/HardwareIntrinsics_X86_Avx10_r.dll"
        ],
        "stopAtEntry": false,
        "cwd": "${fileDirname}",
        "environment": [
            {"name": "DOTNET_TieredCompilation", "value": "0"},
            {"name": "DOTNET_JitForceEvexEncoding", "value": "1"},
            {"name": "DOTNET_JitStressEvexEncoding", "value": "1"},
            {"name": "DOTNET_ENABLEINCOMPLETEISACLASS", "value": "1"},
            {"name": "DOTNET_JitDisasm", "value": "getAbs128"},
            {"name": "DOTNET_ReadyToRun", "value": "0"},
        ],
        "console": "integratedTerminal",
        "symbolSearchPath": "C:/Users/kmodi/Documents/Git_repos/runtime/artifacts/tests/coreclr/windows.x64.Debug/Tests/Core_Root/PDB"
    }

5. You can capture the disasm for getAbs128 if running with env variables above.

*/
namespace IntelHardwareIntrinsicTest._Avx10v1_Vector128
{
    public partial class Program
    {
        const float EPS = Single.Epsilon * 5;

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static Vector128<ulong> getAbs128(Vector128<long> val)
        {
            return Avx10v1.Abs(val);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static Vector256<ulong> getAbs256(Vector256<long> val)
        {
            return Avx10v1.Abs(val);
        }

        [Fact]
        public static unsafe void Avx10v1AbsTest ()
        {
            Console.WriteLine("Test exexuted");
            if (Avx10v1.IsSupported)
            {
                Console.WriteLine("Avx10v1 supported");
                Vector128<long> val = Vector128.Create<long>(-5);
                Vector128<ulong> absVal = getAbs128(val);
            }
            if (Avx10v1.IsSupported)
            {
                Console.WriteLine("Avx10v1_V256 supported");
                Vector256<long> val = Vector256.Create<long>(-5);
                Vector256<ulong> absVal = getAbs256(val);
            }
        }
    }
}
