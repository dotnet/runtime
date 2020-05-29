// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace R2RTest
{
    class CompileSerpCommand
    {
        public static int CompileSerpAssemblies(BuildOptions options)
        {
            if (options.InputDirectory == null)
            {
                Console.Error.WriteLine("Specify --response-file or --input-directory containing multiple response files.");
                return 1;
            }

            if (options.CoreRootDirectory == null)
            {
                Console.Error.WriteLine("--core-root-directory (--cr) is a required argument.");
                return 1;
            }


            // This command does not work in the context of an app, just a loose set of rsp files so don't execute anything we compile
            options.NoJit = true;
            options.NoEtw = true;

            string serpDir = options.InputDirectory.FullName;
            if (!File.Exists(Path.Combine(serpDir, "runserp.cmd")))
            {
                Console.Error.WriteLine($"Error: InputDirectory must point at a SERP build. Could not find {Path.Combine(serpDir, "runserp.cmd")}");
                return 1;
            }

            string whiteListFilePath = Path.Combine(serpDir, "WhitelistDlls.txt");
            if (!File.Exists(whiteListFilePath))
            {
                Console.Error.WriteLine($"File {whiteListFilePath} was not found");
                return 1;
            }

            if (!File.Exists(Path.Combine(options.AspNetPath.FullName, "Microsoft.AspNetCore.dll")))
            {
                Console.Error.WriteLine($"Error: Asp.NET Core path must contain Microsoft.AspNetCore.dll");
                return 1;
            }

            string binDir = Path.Combine(serpDir, "bin");

            // Remove existing native images
            foreach (var file in Directory.GetFiles(Path.Combine(serpDir, "App_Data\\Answers\\Services\\Packages"), "*.dll", SearchOption.AllDirectories))
            {
                if (file.EndsWith(".ni.dll") || file.EndsWith(".ni.exe"))
                {
                    File.Delete(file);
                }
            }

            foreach (var file in Directory.GetFiles(binDir, "*.dll", SearchOption.AllDirectories))
            {
                if (file.EndsWith(".ni.dll") || file.EndsWith(".ni.exe"))
                {
                    File.Delete(file);
                }
            }

            // Add all assemblies from the various SERP packages (filtered by ShouldInclude)
            List<string> binFiles = Directory.GetFiles(Path.Combine(serpDir, "App_Data\\Answers\\Services\\Packages"), "*.dll", SearchOption.AllDirectories)
                .Where((string x) => ShouldInclude(x))
                .ToList();

            // Add a whitelist of assemblies from bin
            foreach (string item in new HashSet<string>(File.ReadAllLines(whiteListFilePath)))
            {
                binFiles.Add(Path.Combine(binDir, item));
            }

            HashSet<string> referenceAssemblyDirectories = new HashSet<string>();
            foreach (var binFile in binFiles)
            {
                var directory = Path.GetDirectoryName(binFile);
                if (!referenceAssemblyDirectories.Contains(directory))
                    referenceAssemblyDirectories.Add(directory);
            }

            // TestILC needs a list of all directories containing assemblies that are referenced from crossgen
            List<string> referenceAssemblies = new List<string>();
            HashSet<string> simpleNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Reference all managed assemblies in /bin and /App_Data/answers/services/packages
            foreach (string binFile in ResolveReferences(referenceAssemblyDirectories))
            {
                simpleNames.Add(Path.GetFileNameWithoutExtension(binFile));
                referenceAssemblies.Add(binFile);
            }

            referenceAssemblies.AddRange(ComputeManagedAssemblies.GetManagedAssembliesInFolderNoSimpleNameDuplicates(simpleNames, options.AspNetPath.FullName, "*.dll"));

            // Add CoreRoot last because it contains various non-framework assemblies that are duplicated in SERP and we want SERP's to be used
            referenceAssemblies.AddRange(ComputeManagedAssemblies.GetManagedAssembliesInFolderNoSimpleNameDuplicates(simpleNames, options.CoreRootDirectory.FullName, "System.*.dll"));
            referenceAssemblies.AddRange(ComputeManagedAssemblies.GetManagedAssembliesInFolderNoSimpleNameDuplicates(simpleNames, options.CoreRootDirectory.FullName, "Microsoft.*.dll"));
            referenceAssemblies.Add(Path.Combine(options.CoreRootDirectory.FullName, "mscorlib.dll"));
            referenceAssemblies.Add(Path.Combine(options.CoreRootDirectory.FullName, "netstandard.dll"));

            //
            // binFiles is now all the assemblies that we want to compile (either individually or as composite)
            // referenceAssemblies is all managed assemblies that are referenceable
            //

            // Remove all bin files except serp.dll so they're just referenced (eventually we'll be able to compile all these in a single composite)
            foreach (string item in new HashSet<string>(File.ReadAllLines(whiteListFilePath)))
            {
                if (item == "Serp.dll")
                    continue;

                binFiles.Remove(Path.Combine(binDir, item));
            }

            List<ProcessInfo> fileCompilations = new List<ProcessInfo>();
            if (options.Composite)
            {
                string serpDll = Path.Combine(binDir, "Serp.dll");
                var runner = new CpaotRunner(options, referenceAssemblies);
                var compilationProcess = new ProcessInfo(new CompilationProcessConstructor(runner, Path.ChangeExtension(serpDll, ".ni.dll"), binFiles));
                fileCompilations.Add(compilationProcess);
            }
            else
            {
                var runner = new CpaotRunner(options, referenceAssemblies);
                foreach (string assemblyName in binFiles)
                {
                    var compilationProcess = new ProcessInfo(new CompilationProcessConstructor(runner, Path.ChangeExtension(assemblyName, ".ni.dll"), new string[] {assemblyName}));
                    fileCompilations.Add(compilationProcess);
                }
            }
            
            ParallelRunner.Run(fileCompilations, options.DegreeOfParallelism);
            
            bool success = true;
            int compilationFailures = 0;
            foreach (var compilationProcess in fileCompilations)
            {
                if (!compilationProcess.Succeeded)
                {
                    success = false;
                    compilationFailures++;

                    Console.WriteLine($"Failed compiling {compilationProcess.Parameters.OutputFileName}");
                }
            }

            Console.WriteLine("Serp Compilation Results");
            Console.WriteLine($"Total compilations: {fileCompilations.Count}");
            Console.WriteLine($"Compilation failures: {compilationFailures}");

            return success ? 0 : 1;
        }

        private static bool ShouldInclude(string file)
        {
            if (!string.IsNullOrEmpty(file))
            {
                if (file.EndsWith(".ni.dll", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".ni.exe", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                if (file.EndsWith("Shared.Exports.dll", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                if (file.EndsWith(".parallax.dll", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                if (!file.EndsWith("Exports.dll", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private static IEnumerable<string> ResolveReferences(IEnumerable<string> folders)
        {
            foreach (string referenceFolder in folders)
            {
                foreach (string reference in ComputeManagedAssemblies.GetManagedAssembliesInFolder(referenceFolder))
                {
                    if (reference.EndsWith(".ni.dll"))
                        continue;
                    yield return reference;
                }
            }
        }
    }
}
