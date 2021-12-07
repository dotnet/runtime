// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Internal.CommandLine
{
    //
    // Helpers for command line processing
    //
    internal class Helpers
    {
        // Helper to create a collection of paths unique in their simple names.
        public static void AppendExpandedPaths(Dictionary<string, string> dictionary, string pattern, bool strict)
        {
            bool empty = true;

            string directoryName = Path.GetDirectoryName(pattern);
            string searchPattern = Path.GetFileName(pattern);

            if (directoryName == "")
                directoryName = ".";

            if (Directory.Exists(directoryName))
            {
                foreach (string fileName in Directory.EnumerateFiles(directoryName, searchPattern))
                {
                    string fullFileName = Path.GetFullPath(fileName);

                    string simpleName = Path.GetFileNameWithoutExtension(fileName);

                    if (dictionary.ContainsKey(simpleName))
                    {
                        if (strict)
                        {
                            throw new CommandLineException("Multiple input files matching same simple name " +
                                fullFileName + " " + dictionary[simpleName]);
                        }
                    }
                    else
                    {
                        dictionary.Add(simpleName, fullFileName);
                    }

                    empty = false;
                }
            }

            if (empty)
            {
                if (strict)
                {
                    throw new CommandLineException("No files matching " + pattern);
                }
                else
                {
                    Console.WriteLine("Warning: No files matching " + pattern);
                }
            }
        }

// ILVerify needs to switch command line processing to Internal.CommandLine and then it can take advantage of this too
#if !ILVERIFY
        public static void MakeReproPackage(string makeReproPath, string outputFilePath, string[] args, ArgumentSyntax argSyntax, IEnumerable<string> inputOptions)
        {
            Directory.CreateDirectory(makeReproPath);

            List<string> details = new List<string>();
            details.Add("Tool version");
            try
            {
                details.Add(Environment.GetCommandLineArgs()[0]);
            }
            catch { }
            try
            {
                details.Add(System.Diagnostics.FileVersionInfo.GetVersionInfo(Environment.GetCommandLineArgs()[0]).ToString());
            }
            catch { }

            details.Add("------------------------");
            details.Add("Actual Command Line Args");
            details.Add("------------------------");
            details.AddRange(args);
            foreach (string arg in args)
            {
                if (arg.StartsWith('@'))
                {
                    string rspFileName = arg.Substring(1);
                    details.Add("------------------------");
                    details.Add(rspFileName);
                    details.Add("------------------------");
                    try
                    {
                        details.AddRange(File.ReadAllLines(rspFileName));
                    }
                    catch { }
                }
            }

            HashCode hashCodeOfArgs = new HashCode();
            foreach (string s in details)
                hashCodeOfArgs.Add(s);

            string zipFileName = ((uint)hashCodeOfArgs.ToHashCode()).ToString();

            if (outputFilePath != null)
                zipFileName = zipFileName + "_" + Path.GetFileName(outputFilePath);

            zipFileName = Path.Combine(makeReproPath, Path.ChangeExtension(zipFileName, ".zip"));

            Console.WriteLine($"Creating {zipFileName}");
            using (var archive = ZipFile.Open(zipFileName, ZipArchiveMode.Create))
            {
                ZipArchiveEntry commandEntry = archive.CreateEntry("command.txt");
                using (StreamWriter writer = new StreamWriter(commandEntry.Open()))
                {
                    foreach (string s in details)
                        writer.WriteLine(s);
                }

                HashSet<string> inputOptionNames = new HashSet<string>(inputOptions);
                Dictionary<string, string> inputToReproPackageFileName = new Dictionary<string, string>();

                List<string> rspFile = new List<string>();
                foreach (var option in argSyntax.GetOptions())
                {
                    if (option.GetDisplayName() == "--make-repro-path")
                    {
                        continue;
                    }

                    if (option.Value != null && !option.Value.Equals(option.DefaultValue))
                    {
                        if (option.IsList)
                        {
                            if (inputOptionNames.Contains(option.GetDisplayName()))
                            {
                                Dictionary<string, string> dictionary = new Dictionary<string, string>();
                                foreach (string optInList in (IEnumerable)option.Value)
                                {
                                    Helpers.AppendExpandedPaths(dictionary, optInList, false);
                                }
                                foreach (string inputFile in dictionary.Values)
                                {
                                    rspFile.Add($"{option.GetDisplayName()}:{ConvertFromInputPathToReproPackagePath(inputFile)}");
                                }
                            }
                            else
                            {
                                foreach (object optInList in (IEnumerable)option.Value)
                                {
                                    rspFile.Add($"{option.GetDisplayName()}:{optInList}");
                                }
                            }
                        }
                        else
                        {
                            rspFile.Add($"{option.GetDisplayName()}:{option.Value}");
                        }
                    }
                }

                foreach (var parameter in argSyntax.GetParameters())
                {
                    if (parameter.Value != null)
                    {
                        if (parameter.IsList)
                        {
                            foreach (object optInList in (IEnumerable)parameter.Value)
                            {
                                rspFile.Add($"{ConvertFromInputPathToReproPackagePath((string)optInList)}");
                            }
                        }
                        else
                        {
                            rspFile.Add($"{ConvertFromInputPathToReproPackagePath((string)parameter.Value.ToString())}");
                        }
                    }
                }

                ZipArchiveEntry rspEntry = archive.CreateEntry("repro.rsp");
                using (StreamWriter writer = new StreamWriter(rspEntry.Open()))
                {
                    foreach (string s in rspFile)
                        writer.WriteLine(s);
                }

                string ConvertFromInputPathToReproPackagePath(string inputPath)
                {
                    if (inputToReproPackageFileName.TryGetValue(inputPath, out string reproPackagePath))
                    {
                        return reproPackagePath;
                    }

                    try
                    {
                        string inputFileDir = inputToReproPackageFileName.Count.ToString();
                        reproPackagePath = Path.Combine(inputFileDir, Path.GetFileName(inputPath));
                        archive.CreateEntryFromFile(inputPath, reproPackagePath);
                        inputToReproPackageFileName.Add(inputPath, reproPackagePath);

                        return reproPackagePath;
                    }
                    catch
                    {
                        return inputPath;
                    }
                }
            }
        }
#endif // !ILVERIFY
    }
}
