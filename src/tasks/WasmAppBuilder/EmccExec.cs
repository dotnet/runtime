// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Tasks;

/// <summary>
/// Wrapper around 'emcc' exec command that enrich ouput with msbuild configuration for produced errors.
/// </summary>
public class EmccExec : Exec
{
    public EmccExec()
    {
        ConsoleToMSBuild = true;
    }

    public override bool Execute()
    {
        bool result = base.Execute();

        if (ExitCode != 0 && ConsoleOutput.Any(m => m.ItemSpec.Contains("wasm-ld: error:")) && ConsoleOutput.Any(m => m.ItemSpec.Contains("undefined symbol")))
        {
            Log.LogWarning("Use '-p:WasmAllowUndefinedSymbols=true' to allow undefined symbols");
        }

        return result;
    }
}
