// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ReadyToRun.SuperIlc
{
    class CompileFrameworkCommand
    {
        public static int CompileFramework(BuildOptions options)
        {
            if (options.CoreRootDirectory == null)
            {
                Console.Error.WriteLine("--core-root-directory (--cr) is a required argument.");
                return 1;
            }

            string logsFolder = Path.Combine(options.CoreRootDirectory.FullName, "logs");
            Directory.CreateDirectory(logsFolder);
            options.OutputDirectory = new DirectoryInfo(logsFolder);
            options.Framework = true;
            options.NoJit = true;
            options.NoEtw = true;
            options.NoExe = true;

            IEnumerable<CompilerRunner> runners = options.CompilerRunners(isFramework: false);

            BuildFolderSet folderSet = new BuildFolderSet(Array.Empty<BuildFolder>(), runners, options);
            bool success = folderSet.Build();
            folderSet.WriteLogs();

            return success ? 0 : 1;
        }
    }
}
