// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace R2RTest
{
    class Crossgen2RunnerOptions
    {
        public bool Composite { get; set; }
        /// <summary>
        /// True for scenarios where the composite image has dependencies outside itself that should not be unrooted inputs
        /// </summary>
        public bool PartialComposite { get; set; }
        public string CompositeRoot { get; set; }
    }

    /// <summary>
    /// Compiles assemblies using the Cross-Platform AOT compiler
    /// </summary>
    class Crossgen2Runner : CompilerRunner
    {
        private Crossgen2RunnerOptions Crossgen2RunnerOptions;
        public override CompilerIndex Index => CompilerIndex.CPAOT;

        // Crossgen2 runs on top of corerun.
        protected override string CompilerRelativePath => "";

        protected override string CompilerFileName => _options.DotNetCli;
        protected readonly List<string> _referenceFiles = new List<string>();

        private string Crossgen2Path => _options.Crossgen2Path != null ? _options.Crossgen2Path.FullName : Path.Combine(_options.CoreRootDirectory.FullName, "crossgen2", "crossgen2.dll");
        private bool CompositeMode => Crossgen2RunnerOptions != null ? Crossgen2RunnerOptions.Composite : _options.Composite;

        public Crossgen2Runner(BuildOptions options, Crossgen2RunnerOptions crossgen2RunnerOptions, IEnumerable<string> references, string overrideOutputPath = null)
            : base(options, references, overrideOutputPath)
        {
            Crossgen2RunnerOptions = crossgen2RunnerOptions;

            // Some scenarios are easier to express when we give Crossgen2 a list of reference assemblies instead of directories,
            // so allow an override here.
            foreach (var reference in references)
            {
                if (File.Exists(reference))
                {
                    if (_referenceFolders.Count > 0)
                    {
                        // There's nothing wrong with this per se, but none of our current scenarios need it, so this is 
                        // just a consistency check.
                        throw new ArgumentException($"A mix of files and directories was found in {references}");
                    }
                    _referenceFiles.Add(reference);
                }
            }

            // Set R2RTest parallelism to a low enough value that ensures that each Crossgen2 invocation gets to use its parallelism
            if (options.DegreeOfParallelism == 0)
                options.DegreeOfParallelism = 2;
        }

        public override ProcessParameters CompilationProcess(string outputFileName, IEnumerable<string> inputAssemblyFileNames)
        {
            ProcessParameters processParameters = base.CompilationProcess(outputFileName, inputAssemblyFileNames);
            processParameters.Arguments = $"{Crossgen2Path} {processParameters.Arguments}";
            // DOTNET_ variables
            processParameters.EnvironmentOverrides["DOTNET_GCStress"] = "";
            processParameters.EnvironmentOverrides["DOTNET_HeapVerify"] = "";
            processParameters.EnvironmentOverrides["DOTNET_ReadyToRun"] = "";
            processParameters.EnvironmentOverrides["DOTNET_GCName"] = "";

            // COMPlus_ variables
            processParameters.EnvironmentOverrides["COMPlus_GCStress"] = "";
            processParameters.EnvironmentOverrides["COMPlus_HeapVerify"] = "";
            processParameters.EnvironmentOverrides["COMPlus_ReadyToRun"] = "";
            processParameters.EnvironmentOverrides["COMPlus_GCName"] = "";
            return processParameters;
        }

        protected override ProcessParameters ExecutionProcess(IEnumerable<string> modules, IEnumerable<string> folders, bool noEtw)
        {
            ProcessParameters processParameters = base.ExecutionProcess(modules, folders, noEtw);
            processParameters.EnvironmentOverrides["DOTNET_ReadyToRun"] = "1";
            return processParameters;
        }

        protected override IEnumerable<string> BuildCommandLineArguments(IEnumerable<string> assemblyFileNames, string outputFileName)
        {
            // The file to compile
            foreach (string inputAssembly in assemblyFileNames)
            {
                yield return inputAssembly;
            }

            // Output
            yield return $"-o:{outputFileName}";

            if (_options.Pdb)
            {
                yield return $"--pdb";
                yield return $"--pdb-path:{Path.GetDirectoryName(outputFileName)}";
            }

            if (_options.Perfmap)
            {
                yield return $"--perfmap";
                yield return $"--perfmap-path:{Path.GetDirectoryName(outputFileName)}";
                yield return $"--perfmap-format-version:{_options.PerfmapFormatVersion}";
            }

            if (_options.TargetArch != null)
            {
                yield return $"--targetarch={_options.TargetArch}";
            }

            if (_options.VerifyTypeAndFieldLayout)
            {
                yield return "--verify-type-and-field-layout";
            }

            if (_options.Map)
            {
                yield return "--map";
            }

            if (_options.Release)
            {
                yield return "-O";
            }

            if (_options.LargeBubble)
            {
                yield return "--inputbubble";
            }

            if (CompositeMode)
            {
                yield return "--composite";
            }

            if (_options.MibcPath != null && _options.MibcPath.Length > 0)
            {
                yield return "--embed-pgo-data";
                foreach (FileInfo mibc in _options.MibcPath)
                {
                    yield return $"-m:{mibc.FullName}";
                }
            }

            if (!string.IsNullOrEmpty(Crossgen2RunnerOptions.CompositeRoot))
            {
                yield return $"--compositerootpath={Crossgen2RunnerOptions.CompositeRoot}";
            }

            if (_options.Crossgen2Parallelism != 0)
            {
                yield return $"--parallelism={_options.Crossgen2Parallelism}";
            }

            if (_options.Crossgen2JitPath != null)
            {
                yield return $"--jitpath={_options.Crossgen2JitPath}";
            }

            string frameworkFolder = "";
            if (_options.Framework || _options.UseFramework)
            {
                frameworkFolder = GetOutputPath(_options.CoreRootDirectory.FullName);
                foreach (string frameworkRef in ResolveReferences(new string[] { frameworkFolder }, 'r'))
                {
                    yield return frameworkRef;
                }
            }

            if (_referenceFiles.Count == 0)
            {
                // Use reference folders and find the managed assemblies we want to reference from them.
                // This is the standard path when we want to compile folders and compare crossgen1 and crossgen2.
                StringComparer pathComparer = PathExtensions.OSPathCaseComparer;
                HashSet<string> uniqueFolders = new HashSet<string>(pathComparer);

                foreach (string assemblyFileName in assemblyFileNames)
                {
                    uniqueFolders.Add(Path.GetDirectoryName(assemblyFileName));
                }

                uniqueFolders.UnionWith(_referenceFolders);
                uniqueFolders.Remove(frameworkFolder);

                foreach (string reference in ResolveReferences(uniqueFolders, CompositeMode && !Crossgen2RunnerOptions.PartialComposite ? 'u' : 'r'))
                {
                    yield return reference;
                }
            }
            else
            {
                // Use an explicit set of reference assemblies.
                // This is useful for crossgen2-specific scenarios since crossgen2 expects a list of files unlike crossgen1
                foreach (var reference in _referenceFiles)
                {
                    yield return (CompositeMode && !Crossgen2RunnerOptions.PartialComposite ? "-u:" : "-r:") + reference;
                }
            }
        }

        private IEnumerable<string> ResolveReferences(IEnumerable<string> folders, char referenceOption)
        {
            foreach (string referenceFolder in folders)
            {
                foreach (string reference in ComputeManagedAssemblies.GetManagedAssembliesInFolder(referenceFolder))
                {
                    string simpleName = Path.GetFileNameWithoutExtension(reference);
                    if (!FrameworkExclusion.Exclude(simpleName, Index, out string reason))
                    {
                        yield return $"-{referenceOption}:{reference}";
                    }
                }
            }
        }
    }
}
