// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace R2RTest
{
    /// <summary>
    /// Adds a list of Nuget packages to an empty console app, publishes the app, and runs Crossgen / CPAOT
    /// on the published assemblies.
    /// </summary>
    class CompileNugetCommand
    {
        public static int CompileNuget(BuildOptions options)
        {
            if (options.CoreRootDirectory == null)
            {
                Console.Error.WriteLine("--core-root-directory (--cr) is a required argument.");
                return 1;
            }

            // We don't want to launch these apps when building the folder set below
            options.NoExe = true;
            options.NoJit = true;
            options.InputDirectory = options.OutputDirectory;

            IList<string> packageList = ReadPackageNames(options.PackageList.FullName);

            if (options.OutputDirectory == null)
            {
                Console.Error.WriteLine("--output-directory is a required argument.");
                return 1;
            }

            IEnumerable<string> referencePaths = options.ReferencePaths();
            IEnumerable<CompilerRunner> runners = options.CompilerRunners(false);

            if (!options.Exe)
            {
                PathExtensions.DeleteOutputFolders(options.OutputDirectory.FullName, options.CoreRootDirectory.FullName, runners, recursive: false);
            }

            string nugetOutputFolder = Path.Combine(options.OutputDirectory.FullName, "nuget.out");
            Directory.CreateDirectory(nugetOutputFolder);

            var publishedAppFoldersToCompile = new List<BuildFolder>();
            using (StreamWriter nugetLog = File.CreateText(Path.Combine(nugetOutputFolder, "nugetLog.txt")))
            {
                foreach (var package in packageList)
                {
                    nugetLog.WriteLine($"Creating empty app for {package}");

                    // Create an app folder
                    string appFolder = Path.Combine(nugetOutputFolder, $"{package}.TestApp");
                    Directory.CreateDirectory(appFolder);

                    Version version = Environment.Version;
                    string targetFramework = $"net{version.Major}.{version.Minor}";
                    int exitCode = DotnetCli.New(appFolder, $"console -f {targetFramework}", nugetLog);
                    if (exitCode != 0)
                    {
                        nugetLog.WriteLine($"dotnet new console for {package} failed with exit code {exitCode}");
                        continue;
                    }

                    exitCode = DotnetCli.AddPackage(appFolder, package, nugetLog);
                    if (exitCode != 0)
                    {
                        nugetLog.WriteLine($"dotnet add package {package} failed with exit code {exitCode}");
                        continue;
                    }

                    exitCode = DotnetCli.Publish(appFolder, nugetLog);
                    if (exitCode != 0)
                    {
                        nugetLog.WriteLine($"dotnet publish failed with exit code {exitCode}");
                        continue;
                    }

                    // This is not a reliable way of building the publish folder
                   
                    string publishFolder = Path.Combine(appFolder, "artifacts", "Debug", targetFramework, "publish");
                    if (!Directory.Exists(publishFolder))
                    {
                        nugetLog.WriteLine($"Could not find folder {publishFolder} containing the published app.");
                        continue;
                    }

                    publishedAppFoldersToCompile.Add(BuildFolder.FromDirectory(publishFolder, runners, appFolder, options));
                }

                BuildFolderSet folderSet = new BuildFolderSet(publishedAppFoldersToCompile, runners, options);
                bool success = folderSet.Build();
                folderSet.WriteLogs();

                if (!options.NoCleanup && !options.Exe)
                {
                    PathExtensions.DeleteOutputFolders(options.OutputDirectory.FullName, options.CoreRootDirectory.FullName, runners, recursive: false);
                }

                return success ? 0 : 1;
            }
        }

        private static IList<string> ReadPackageNames(string packageListFile) => File.ReadAllLines(packageListFile);
    }
}
