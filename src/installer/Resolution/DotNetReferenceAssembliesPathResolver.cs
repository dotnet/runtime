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
        
        internal static string Resolve(IEnvironment envirnment)
        {
            var path = envirnment.GetEnvironmentVariable(DotNetReferenceAssembliesPathEnv);
            if (!string.IsNullOrEmpty(path))
            {
                return path;
            }
            
            return GetDefaultDotNetReferenceAssembliesPath();
        }
        
        public static string Resolve()
        {
            return Resolve(EnvironmentWrapper.Default);
        }
                
        private static string GetDefaultDotNetReferenceAssembliesPath()
        {            
            var os = PlatformServices.Default.Runtime.OperatingSystemPlatform;
            
            if (os == Platform.Windows)
            {
                return null;
            }
            
            if (os == Platform.Darwin && 
                Directory.Exists("/Library/Framework/Mono.Framework/Versions/Current/lib/mono/xbuild-frameworks"))
            {
                return "/Library/Framework/Mono.Framework/Versions/Current/lib/mono/xbuild-frameworks";
            }
            
            if (Directory.Exists("/usr/local/lib/mono/xbuild-frameworks"))
            {
                return "/usr/local/lib/mono/xbuild-frameworks";
            }
            
            if (Directory.Exists("/usr/lib/mono/xbuild-frameworks"))
            {
                return "/usr/lib/mono/xbuild-frameworks";
            }
            
            return null;
        }
    }
}