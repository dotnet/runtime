// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace R2RTest
{
    /// <summary>
    /// Compiles assemblies using the Cross-Platform AOT compiler
    /// </summary>
    class CpaotRunner : CompilerRunner
    {
        public override CompilerIndex Index => CompilerIndex.CPAOT;

        // Crossgen2 runs on top of corerun.
        protected override string CompilerRelativePath => "";

        protected override string CompilerFileName => "corerun".AppendOSExeSuffix();
        protected readonly List<string> _referenceFiles = new List<string>();

        private string Crossgen2Path => Path.Combine(_options.CoreRootDirectory.FullName, "crossgen2", "crossgen2.dll");

        public CpaotRunner(BuildOptions options, IEnumerable<string> references)
            : base(options, references)
        {
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
            return processParameters;
        }

        protected override ProcessParameters ExecutionProcess(IEnumerable<string> modules, IEnumerable<string> folders, bool noEtw)
        {
            ProcessParameters processParameters = base.ExecutionProcess(modules, folders, noEtw);
            processParameters.EnvironmentOverrides["COMPLUS_ReadyToRun"] = "1";
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

            // Todo: Allow cross-architecture compilation
            //yield return "--targetarch=x64";

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

            if (_options.Composite)
            {
                yield return "--composite";
            }

            if (_options.Crossgen2Parallelism != 0)
            {
                yield return $"--parallelism={_options.Crossgen2Parallelism}";
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

                foreach (string reference in ResolveReferences(uniqueFolders, _options.Composite ? 'u' : 'r'))
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
                    yield return (_options.Composite && !_options.PartialComposite ? "-u:" : "-r:") + reference;
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
