// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.WebAssembly.Build.Tasks
{
    // because on docker instance without swap file, MSBuild nodes need to make some room for LLVM
    // https://github.com/dotnet/runtime/issues/113724
    public class ForceMSBuildGC : Task
    {
        public override bool Execute()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return true;
        }
    }
}
