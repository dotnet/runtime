// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace R2RTest
{
    /// <summary>
    /// Compiles assemblies using the Cross-Platform AOT compiler
    /// </summary>
    class CrossgenRunner : CompilerRunner
    {
        public override CompilerIndex Index => CompilerIndex.Crossgen;

        protected override string CompilerRelativePath => ".";

        protected override string CompilerFileName => "crossgen".AppendOSExeSuffix();

        protected override string CompilerPath
        {
            get
            {
                return _options.CrossgenPath != null ? _options.CrossgenPath.FullName : base.CompilerPath;
            }
        }

        public CrossgenRunner(BuildOptions options, IEnumerable<string> referencePaths)
            : base(options, referencePaths) { }

        protected override ProcessParameters ExecutionProcess(IEnumerable<string> modules, IEnumerable<string> folders, bool noEtw)
        {
            ProcessParameters processParameters = base.ExecutionProcess(modules, folders, noEtw);
            processParameters.EnvironmentOverrides["COMPLUS_ReadyToRun"] = "1";
            processParameters.EnvironmentOverrides["COMPLUS_NoGuiOnAssert"] = "1";
            return processParameters;
        }

        protected override IEnumerable<string> BuildCommandLineArguments(IEnumerable<string> assemblyFileNames, string outputFileName)
        {
            if (assemblyFileNames.Count() > 1)
            {
                throw new NotImplementedException($@"Crossgen1 doesn't support composite build mode for compiling multiple input assemblies: {string.Join("; ", assemblyFileNames)}");
            }

            // The file to compile
            yield return "/in";
            yield return assemblyFileNames.First();

            // Output
            yield return "/out";
            yield return outputFileName;

            if (_options.LargeBubble && Path.GetFileNameWithoutExtension(assemblyFileNames.First()) != "System.Private.CoreLib")
            {
                // There seems to be a bug in Crossgen on Linux we don't intend to fix -
                // it crashes when trying to compile S.P.C in large version bubble mode.
                yield return "/largeversionbubble";
            }

            yield return "/platform_assemblies_paths";

            HashSet<string> paths = new HashSet<string>();
            foreach (string assemblyFileName in assemblyFileNames)
            {
                paths.Add(Path.GetDirectoryName(assemblyFileName));
            }
            paths.UnionWith(_referenceFolders);

            yield return paths.ConcatenatePaths();
        }
    }
}
