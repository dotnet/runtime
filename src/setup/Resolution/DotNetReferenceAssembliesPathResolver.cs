// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Microsoft.Extensions.EnvironmentAbstractions;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.Extensions.DependencyModel.Resolution
{
    public class DotNetReferenceAssembliesPathResolver
    {
        public static readonly string DotNetReferenceAssembliesPathEnv = "DOTNET_REFERENCE_ASSEMBLIES_PATH";
        
        internal static string Resolve(IEnvironment envirnment, IFileSystem fileSystem, IRuntimeEnvironment runtimeEnvironment)
        {
            var path = envirnment.GetEnvironmentVariable(DotNetReferenceAssembliesPathEnv);
            if (!string.IsNullOrEmpty(path))
            {
                return path;
            }
            
            return GetDefaultDotNetReferenceAssembliesPath(fileSystem, runtimeEnvironment);
        }
        
        public static string Resolve()
        {
            return Resolve(EnvironmentWrapper.Default, FileSystemWrapper.Default, PlatformServices.Default.Runtime);
        }
                
        private static string GetDefaultDotNetReferenceAssembliesPath(IFileSystem fileSystem, IRuntimeEnvironment runtimeEnvironment)
        {            
            var os = runtimeEnvironment.OperatingSystemPlatform;
            
            if (os == Platform.Windows)
            {
                return null;
            }
            
            if (os == Platform.Darwin && 
                fileSystem.Directory.Exists("/Library/Frameworks/Mono.framework/Versions/Current/lib/mono/xbuild-frameworks"))
            {
                return "/Library/Frameworks/Mono.framework/Versions/Current/lib/mono/xbuild-frameworks";
            }
            
            if (fileSystem.Directory.Exists("/usr/local/lib/mono/xbuild-frameworks"))
            {
                return "/usr/local/lib/mono/xbuild-frameworks";
            }
            
            if (fileSystem.Directory.Exists("/usr/lib/mono/xbuild-frameworks"))
            {
                return "/usr/lib/mono/xbuild-frameworks";
            }
            
            return null;
        }
    }
}
