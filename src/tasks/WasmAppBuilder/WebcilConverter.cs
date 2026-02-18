// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Collections.Immutable;
using System.Reflection.PortableExecutable;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using WasmAppBuilder;

namespace Microsoft.WebAssembly.Build.Tasks;

/// <summary>
/// Reads a .NET assembly in a normal PE COFF file and writes it out as a WebCIL file
/// </summary>
public class WebCILConverter
{
    private readonly string _inputPath;
    private readonly string _outputPath;

    private readonly NET.WebAssembly.WebCIL.WebCILConverter _converter;

    private LogAdapter Log { get; }
    private WebCILConverter(NET.WebAssembly.WebCIL.WebCILConverter converter, string inputPath, string outputPath, LogAdapter logger)
    {
        _converter = converter;
        _inputPath = inputPath;
        _outputPath = outputPath;
        Log = logger;
    }

    public static WebCILConverter FromPortableExecutable(string inputPath, string outputPath, LogAdapter logger)
    {
        var converter = NET.WebAssembly.WebCIL.WebCILConverter.FromPortableExecutable(inputPath, outputPath);
        return new WebCILConverter(converter, inputPath, outputPath, logger);
    }

    public void ConvertToWebCIL()
    {
        Log.LogMessage(MessageImportance.Low, $"Converting to WebCIL: input {_inputPath} output: {_outputPath}");
        _converter.ConvertToWebCIL();
    }

}
