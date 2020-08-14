// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.NET.HostModel.Bundle
{
    /// <summary>
    ///  Information about files to embed into the Bundle (input to the Bundler).
    ///  
    ///   SourcePath: path to the file to be bundled at compile time
    ///   BundleRelativePath: path where the file is expected at run time, 
    ///                       relative to the app DLL.
    /// </summary>
    public class FileSpec
    {
        public readonly string SourcePath;
        public readonly string BundleRelativePath;
        public bool Excluded;

        public FileSpec(string sourcePath, string bundleRelativePath)
        {
            SourcePath = sourcePath;
            BundleRelativePath = bundleRelativePath;
            Excluded = false;
        }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(SourcePath) && 
                   !string.IsNullOrWhiteSpace(BundleRelativePath);
        }

        public override string ToString() => $"SourcePath: {SourcePath}, RelativePath: {BundleRelativePath} {(Excluded ? "[Excluded]" : "")}";
    }
}

