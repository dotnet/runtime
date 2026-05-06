// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace R2RTest
{
    class CompileDirectoryCommand
    {
        public static int CompileDirectory(BuildOptions options)
        {
            if (options.InputDirectory == null)
            {
                Console.Error.WriteLine("--input-directory is a required argument.");
                return 1;
            }

            if (options.CoreRootDirectory == null)
            {
                Console.Error.WriteLine("--core-root-directory (--cr) is a required argument.");
                return 1;
            }

            if (options.OutputDirectory == null)
            {
                options.OutputDirectory = options.InputDirectory;
            }

            if (options.OutputDirectory.IsParentOf(options.InputDirectory))
            {
                Console.Error.WriteLine("Error: Input and output folders must be distinct, and the output directory (which gets deleted) better not be a parent of the input directory.");
                return 1;
            }

            IEnumerable<CompilerRunner> runners = options.CompilerRunners(isFramework: false);

            if (!options.Exe)
            {
                PathExtensions.DeleteOutputFolders(options.OutputDirectory.FullName, options.CoreRootDirectory.FullName, runners, recursive: false);
            }

            BuildFolder folder = BuildFolder.FromDirectory(options.InputDirectory.FullName, runners, options.OutputDirectory.FullName, options);
            if (folder == null)
            {
                Console.Error.WriteLine($"No managed app found in {options.InputDirectory.FullName}");
            }

            BuildFolderSet folderSet = new BuildFolderSet(new BuildFolder[] { folder }, runners, options);
            bool success = folderSet.Build();
            folderSet.WriteLogs();

            if (!options.NoCleanup && !options.Exe)
            {
                PathExtensions.DeleteOutputFolders(options.OutputDirectory.FullName, options.CoreRootDirectory.FullName, runners, recursive: false);
            }

            return success ? 0 : 1;
        }
    }
}
