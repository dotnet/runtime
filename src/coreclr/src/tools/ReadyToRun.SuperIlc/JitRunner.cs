// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ReadyToRun.SuperIlc
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
        public override ProcessParameters CompilationProcess(string outputRoot, string assemblyFileName)
        {
            File.Copy(assemblyFileName, GetOutputFileName(outputRoot, assemblyFileName), overwrite: true);
            return null;
        }

        protected override ProcessParameters ExecutionProcess(IEnumerable<string> modules, IEnumerable<string> folders, bool noEtw)
        {
            ProcessParameters processParameters = base.ExecutionProcess(modules, folders, noEtw);
            processParameters.EnvironmentOverrides["COMPLUS_ReadyToRun"] = "0";
            return processParameters;
        }

        protected override IEnumerable<string> BuildCommandLineArguments(string assemblyFileName, string outputFileName)
        {
            // This should never get called as the overridden CompilationProcess returns null
            throw new NotImplementedException();
        }
    }
}
