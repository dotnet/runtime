// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

public class CompiledAssembly
{
    public string Path { get; set; } = ""!;

    /// <summary>
    /// The full path to the assembly file when aot mode is AsmOnly
    /// </summary>
    public string AssemblerFile { get; set; } = ""!;

    /// <summary>
    /// The full path to the object file when aot mode is Normal
    /// </summary>
    public string ObjectFile { get; set; } = ""!;

    /// <summary>
    /// The full path to the library file (.dylib, .so, .dll) when aot mode is Library
    /// </summary>
    public string LibraryFile { get; set; } = ""!;

    /// <summary>
    /// The full path to the aot data file when UseAotData is set to true
    /// </summary>
    public string DataFile { get; set; } = ""!;

    /// <summary>
    /// The full path to the LLVM object file when LLVM is used
    /// </summary>
    public string LlvmObjectFile { get; set; } = ""!;

    /// <summary>
    /// The full path to the LLVM bitcode file when LLVM only is specified
    /// </summary>
    public string LlvmBitCodeFile { get; set; } = ""!;

    /// <summary>
    /// The full path of symbols to export when building in library mode
    /// </summary>
    public string ExportsFile { get; set; } = ""!;
}
