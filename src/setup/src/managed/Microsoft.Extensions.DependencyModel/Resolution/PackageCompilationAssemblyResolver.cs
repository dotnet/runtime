// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.DotNet.PlatformAbstractions;

namespace Microsoft.Extensions.DependencyModel.Resolution
{
    public class PackageCompilationAssemblyResolver: ICompilationAssemblyResolver
    {
        private readonly IFileSystem _fileSystem;
        private readonly string[] _nugetPackageDirectories;

        public PackageCompilationAssemblyResolver()
            : this(EnvironmentWrapper.Default, FileSystemWrapper.Default)
        {
        }

        public PackageCompilationAssemblyResolver(string nugetPackageDirectory)
            : this(FileSystemWrapper.Default, new string[] { nugetPackageDirectory })
        {
        }

        internal PackageCompilationAssemblyResolver(IEnvironment environment,
            IFileSystem fileSystem)
            : this(fileSystem, GetDefaultProbeDirectories(environment))
        {
        }

        internal PackageCompilationAssemblyResolver(IFileSystem fileSystem, string[] nugetPackageDirectories)
        {
            _fileSystem = fileSystem;
            _nugetPackageDirectories = nugetPackageDirectories;
        }

        private static string[] GetDefaultProbeDirectories(IEnvironment environment) =>
            GetDefaultProbeDirectories(RuntimeEnvironment.OperatingSystemPlatform, environment);

        internal static string[] GetDefaultProbeDirectories(Platform osPlatform, IEnvironment environment)
        {
#if !NETSTANDARD1_3            
#if NETSTANDARD1_6
            var probeDirectories = AppContext.GetData("PROBING_DIRECTORIES");
#else
            var probeDirectories = AppDomain.CurrentDomain.GetData("PROBING_DIRECTORIES");
#endif

           var listOfDirectories = probeDirectories as string;

           if (!string.IsNullOrEmpty(listOfDirectories))
           {
               return listOfDirectories.Split(new char [] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries );
           }
#endif

           var packageDirectory = environment.GetEnvironmentVariable("NUGET_PACKAGES");

           if (!string.IsNullOrEmpty(packageDirectory))
           {
               return new string[] { packageDirectory };
           }

           string basePath;
           if (osPlatform == Platform.Windows)
           {
               basePath = environment.GetEnvironmentVariable("USERPROFILE");
           }
           else
           {
               basePath = environment.GetEnvironmentVariable("HOME");
           }

           if (string.IsNullOrEmpty(basePath))
           {
               return new string[] { string.Empty };
           }

           return new string[] { Path.Combine(basePath, ".nuget", "packages") };

        }

        public bool TryResolveAssemblyPaths(CompilationLibrary library, List<string> assemblies)
        {
            if (_nugetPackageDirectories == null || _nugetPackageDirectories.Length == 0 ||
                !string.Equals(library.Type, "package", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            foreach (var directory in _nugetPackageDirectories)
            {
                string packagePath;

                if (ResolverUtils.TryResolvePackagePath(_fileSystem, library, directory, out packagePath))
                {
                    assemblies.AddRange(ResolverUtils.ResolveFromPackagePath(_fileSystem, library, packagePath));
                    return true;
                }
            }
            return false;
        }
    }
}
