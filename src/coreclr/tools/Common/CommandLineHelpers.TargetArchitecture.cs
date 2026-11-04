// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

using Internal.TypeSystem;

namespace System.CommandLine;

internal sealed class CommandLineException : Exception
{
    public CommandLineException(string message) : base(message) { }
}

// The target architecture options, apart from the rest so that the tools that don't use System.CommandLine
// (the SuperPMI ones) can share them.
internal static partial class Helpers
{
    public static TargetArchitecture GetTargetArchitecture(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X86 => TargetArchitecture.X86,
                Architecture.X64 => TargetArchitecture.X64,
                Architecture.Arm => TargetArchitecture.ARM,
                Architecture.Arm64 => TargetArchitecture.ARM64,
                Architecture.Wasm => TargetArchitecture.Wasm32,
                Architecture.LoongArch64 => TargetArchitecture.LoongArch64,
                Architecture.RiscV64 => TargetArchitecture.RiscV64,
                _ => throw new NotImplementedException()
            };
        }

        return TryGetTargetArchitecture(token) ?? throw new CommandLineException($"Target architecture '{token}' is not supported");
    }

    public static TargetArchitecture? TryGetTargetArchitecture(string token) => token.ToLowerInvariant() switch
    {
        "x86" => TargetArchitecture.X86,
        "x64" or "amd64" => TargetArchitecture.X64,
        "arm" or "armel" or "arm32" => TargetArchitecture.ARM,
        "arm64" => TargetArchitecture.ARM64,
        "wasm" or "wasm32" => TargetArchitecture.Wasm32,
        "loongarch64" => TargetArchitecture.LoongArch64,
        "riscv64" => TargetArchitecture.RiscV64,
        _ => null
    };
}
