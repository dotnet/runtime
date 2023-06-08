// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Build.Framework;

namespace Microsoft.WebAssembly.Build.Tasks;

public class EmitWasmBundleObjectFiles : EmitWasmBundleBase
{
    [Required]
    public string ClangExecutable { get; set; } = default!;

    public override bool Execute()
    {
        if (!File.Exists(ClangExecutable))
        {
            Log.LogError($"Cannot find {nameof(ClangExecutable)}={ClangExecutable}");
            return false;
        }

        return base.Execute();
    }

    public override bool Emit(string destinationFile, Action<Stream> inputProvider)
    {
        if (Path.GetDirectoryName(destinationFile) is string destDir && !string.IsNullOrEmpty(destDir))
            Directory.CreateDirectory(destDir);

        (int exitCode, string output) = Utils.TryRunProcess(Log,
                            ClangExecutable!,
                            args: $"-xc -o \"{destinationFile}\" -c -",
                            envVars: null, workingDir: null, silent: true, logStdErrAsMessage: false,
                            debugMessageImportance: MessageImportance.Low, label: null,
                            inputProvider);
        if (exitCode != 0)
        {
            Log.LogError($"Failed to compile with exit code {exitCode}{Environment.NewLine}Output: {output}");
        }
        return exitCode == 0;
    }

}
