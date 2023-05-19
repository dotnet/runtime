// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.Build.Framework;

// It would be ideal that this Task would always produce object files as EmitBundleObjectFiles does.
// EmitBundleObjectFiles could do it with clang by streaming code directly to clang input stream.
// For emcc it's not possible, so we need to write the code to disk first and then compile it in MSBuild.
public class EmitBundleSourceFiles : EmitBundleBase
{
    public override bool Emit(string destinationFile, Action<Stream> inputProvider)
    {
        using (var fileStream = File.Create(destinationFile))
        {
            inputProvider(fileStream);
        }

        return true;
    }
}
