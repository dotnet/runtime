// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    public struct FileSpec
    {
        public readonly string SourcePath;
        public readonly string BundleRelativePath;

        public FileSpec(string sourcePath, string bundleRelativePath)
        {
            SourcePath = sourcePath;
            BundleRelativePath = bundleRelativePath;
        }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(SourcePath) && 
                   !string.IsNullOrWhiteSpace(BundleRelativePath);
        }
    }
}

