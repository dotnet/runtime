// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace R2RTest
{
    /// <summary>
    /// No-op runner keeping the original IL assemblies to be directly run with full jitting.
    /// </summary>
    class JitRunner : CompilerRunner
    {
        public override CompilerIndex Index => CompilerIndex.Jit;

        protected override string CompilerRelativePath => ".";

        protected override string CompilerFileName => "clrjit.dll";

        public JitRunner(BuildOptions options)
            : base(options, new string[] { options.CoreRootDirectory.FullName }.Concat(options.ReferencePaths())) { }

        /// <summary>
        /// JIT runner has no compilation process as it doesn't transform the source IL code in any manner.
        /// </summary>
        /// <returns></returns>
        public override ProcessParameters CompilationProcess(string outputFileName, IEnumerable<string> inputAssemblyFileNames)
        {
            if (inputAssemblyFileNames.Count() != 1)
            {
                throw new Exception($@"JIT builder doesn't support composite mode for building input assemblies: {string.Join("; ", inputAssemblyFileNames)}");
            }

            File.Copy(inputAssemblyFileNames.First(), outputFileName, overwrite: true);
            return null;
        }

        protected override ProcessParameters ExecutionProcess(IEnumerable<string> modules, IEnumerable<string> folders, bool noEtw)
        {
            ProcessParameters processParameters = base.ExecutionProcess(modules, folders, noEtw);
            processParameters.EnvironmentOverrides["DOTNET_ReadyToRun"] = "0";
            processParameters.EnvironmentOverrides["COMPlus_ReadyToRun"] = "0";
            return processParameters;
        }

        protected override IEnumerable<string> BuildCommandLineArguments(IEnumerable<string> assemblyFileNames, string outputFileName)
        {
            // This should never get called as the overridden CompilationProcess returns null
            throw new NotImplementedException();
        }
    }
}
