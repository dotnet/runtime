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

namespace ReadyToRun.SuperIlc
{
    class CompileFromCrossgenRspCommand
    {
        /// <summary>
        /// Utility mode that allows compilation of a set of assemblies using their existing Crossgen response files.
        /// This is currently useful for workloads like Bing which have a large complicated web of binaries in different folders
        /// with potentially different sets of reference paths used for different assemblies.
        /// </summary>
        public static int CompileFromCrossgenRsp(BuildOptions options)
        {
            if (options.CrossgenResponseFile == null && options.InputDirectory == null)
            {
                Console.Error.WriteLine("Specify --response-file or --input-directory containing multiple response files.");
                return 1;
            }

            if (options.CoreRootDirectory == null)
            {
                Console.Error.WriteLine("--core-root-directory (--cr) is a required argument.");
                return 1;
            }

            if (options.OutputDirectory == null)
            {
                if (options.InputDirectory != null)
                {
                    options.OutputDirectory = options.InputDirectory;
                }
                else
                {
                    options.OutputDirectory = new DirectoryInfo(Path.GetDirectoryName(options.CrossgenResponseFile.FullName));
                }
            } else if (options.InputDirectory != null && options.OutputDirectory.IsParentOf(options.InputDirectory))
            {
                Console.Error.WriteLine("Error: Input and output folders must be distinct, and the output directory (which gets deleted) better not be a parent of the input directory.");
                return 1;
            }

            // This command does not work in the context of an app, just a loose set of rsp files so don't execute anything we compile
            options.NoJit = true;
            options.NoEtw = true;

            //
            // Determine whether we're compiling a single .rsp or a folder of them
            //
            var responseFiles = new List<string>();
            if (options.CrossgenResponseFile != null)
            {
                responseFiles.Add(options.CrossgenResponseFile.FullName);
            }
            else
            {
                responseFiles = Directory.EnumerateFiles(options.InputDirectory.FullName, "*.rsp", SearchOption.TopDirectoryOnly).ToList();
            }

            Dictionary<string, string> pathReplacements = new Dictionary<string, string>();

            if ((options.RewriteOldPath == null) != (options.RewriteNewPath == null))
            {
                Console.Error.WriteLine("Error: --rewrite-old-path and --rewrite-new-path must both be specified if either is used.");
                return 1;
            }

            if (options.RewriteOldPath != null && options.RewriteNewPath != null)
            {
                if (options.RewriteOldPath.Length != options.RewriteNewPath.Length)
                {
                    Console.Error.WriteLine("Error: --rewrite-old-path and --rewrite-new-path were specified a different number of times.");
                    return 1;
                }

                for (int i = 0; i < options.RewriteNewPath.Length; i++)
                {
                    pathReplacements.Add(options.RewriteOldPath[i].FullName, options.RewriteNewPath[i].FullName);
                    Console.WriteLine($"Re-writing path {options.RewriteOldPath[i].FullName} as {options.RewriteNewPath[i].FullName}");
                }
            }

            bool success = true;
            int compilationFailures = 0;
            int totalCompilations = 0;
            // Collect all the compilations first
            foreach (var inputRsp in responseFiles)
            {
                var crossgenArguments = CrossgenArguments.ParseFromResponseFile(inputRsp)
                                                         .ReplacePaths(pathReplacements);

                Console.WriteLine($"{inputRsp} -> {crossgenArguments.InputFile}");
                var compilerRunners = options.CompilerRunners(false, crossgenArguments.ReferencePaths);

                string responseFileOuputPath = Path.Combine(options.OutputDirectory.FullName, Path.GetFileNameWithoutExtension(inputRsp));
                responseFileOuputPath.RecreateDirectory();

                List<ProcessInfo> fileCompilations = new List<ProcessInfo>();
                foreach (CompilerRunner runner in compilerRunners)
                {
                    var compilationProcess = new ProcessInfo(new CompilationProcessConstructor(runner, responseFileOuputPath, crossgenArguments.InputFile));
                    fileCompilations.Add(compilationProcess);
                }

                ParallelRunner.Run(fileCompilations, options.DegreeOfParallelism);
                totalCompilations++;

                foreach (var compilationProcess in fileCompilations)
                {
                    if (!compilationProcess.Succeeded)
                    {
                        success = false;
                        compilationFailures++;

                        Console.WriteLine($"Failed compiling {compilationProcess.Parameters.InputFileName}");
                    }
                }
            }

            Console.WriteLine("Rsp Compilation Results");
            Console.WriteLine($"Total compilations: {totalCompilations}");
            Console.WriteLine($"Compilation failures: {compilationFailures}");

            return success ? 0 : 1;
        }

        private class CrossgenArguments
        {
            public string InputFile;
            public List<string> ReferencePaths = new List<string>();

            public static CrossgenArguments ParseFromResponseFile(string responseFile)
            {
                var arguments = new CrossgenArguments();

                string[] tokenizedArguments = TokenizeArguments(responseFile);

                for (int i = 0; i < tokenizedArguments.Length; i++)
                {
                    string arg = tokenizedArguments[i];
                    if (MatchParameter("in", arg))
                    {
                        arguments.InputFile = tokenizedArguments[i + 1];
                    }
                    else if (MatchParameter("App_Paths", arg) || MatchParameter("Platform_Assemblies_Paths", arg))
                    {
                        string appPaths = tokenizedArguments[i + 1];
                        arguments.ReferencePaths.AddRange(appPaths.Split(';').TakeWhile(x => !string.IsNullOrWhiteSpace(x)));
                        ++i;
                    }
                    else if (MatchParameter("verbose", arg)
                        || MatchParameter("readytorun", arg))
                    {
                        // Skip unparameterized switches
                        continue;
                    }
                    else if (MatchParameter("jitpath", arg))
                    {
                        // Skip switches with one parameter
                        ++i;
                        continue;
                    }
                    else if (!IsSwitch(arg))
                    {
                        Debug.Assert(arguments.InputFile == null);
                        arguments.InputFile = arg;
                    }
                }

                return arguments;
            }

            public CrossgenArguments ReplacePaths(Dictionary<string, string> replacementPaths)
            {
                foreach (var replacePath in replacementPaths)
                {
                    if (InputFile.StartsWith(replacePath.Key, ignoreCase: Environment.OSVersion.Platform == PlatformID.Win32NT, culture: null))
                    {
                        InputFile = InputFile.Replace(replacePath.Key, replacePath.Value, ignoreCase: Environment.OSVersion.Platform == PlatformID.Win32NT, culture: null);
                    }

                    for (int i = 0; i < ReferencePaths.Count; i++)
                    {
                        if (ReferencePaths[i].StartsWith(replacePath.Key, ignoreCase: Environment.OSVersion.Platform == PlatformID.Win32NT, culture: null))
                        {
                            ReferencePaths[i] = ReferencePaths[i].Replace(replacePath.Key, replacePath.Value, ignoreCase: Environment.OSVersion.Platform == PlatformID.Win32NT, culture: null);
                        }
                    }
                }

                return this;
            }

            private static bool MatchParameter(string paramName, string inputArg)
            {
                if (inputArg.Length == 0)
                    return false;

                if (!IsSwitch(inputArg))
                    return false;

                return string.Equals(paramName, inputArg.Substring(1), StringComparison.OrdinalIgnoreCase);
            }

            private static bool IsSwitch(string inputArg) => inputArg.StartsWith('/') || inputArg.StartsWith('-');
            private static string[] TokenizeArguments(string responseFile)
            {
                var arguments = new List<string>();
                using (TextReader reader = File.OpenText(responseFile))
                {
                    StringBuilder sb = new StringBuilder();
                    while (true)
                    {
                        int nextChar = reader.Read();
                        if (nextChar == -1)
                        {
                            break;
                        }

                        char currentChar = (char)nextChar;
                        if (!char.IsWhiteSpace(currentChar))
                        {
                            sb.Append(currentChar);
                        }
                        else
                        {
                            if (sb.Length > 0)
                            {
                                arguments.Add(sb.ToString());
                                sb.Clear();
                            }
                        }
                    }

                    // Flush everything after the last white space
                    arguments.Add(sb.ToString());
                }

                return arguments.ToArray();
            }
        }
    }
}
