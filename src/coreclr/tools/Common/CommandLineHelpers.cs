// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.CommandLine.Binding;
using System.CommandLine.Parsing;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;

using Internal.TypeSystem;

namespace System.CommandLine
{
    internal sealed class CommandLineException : Exception
    {
        public CommandLineException(string message) : base(message) { }
    }

    //
    // Helpers for command line processing
    //
    internal static partial class Helpers
    {
        public const string DefaultSystemModule = "System.Private.CoreLib";

        public static Dictionary<string, string> BuildPathDictionary(IReadOnlyList<Token> tokens, bool strict)
        {
            Dictionary<string, string> dictionary = new(StringComparer.OrdinalIgnoreCase);

            foreach (Token token in tokens)
            {
                AppendExpandedPaths(dictionary, token.Value, strict);
            }

            return dictionary;
        }

        public static List<string> BuildPathList(IReadOnlyList<Token> tokens)
        {
            List<string> paths = new();
            Dictionary<string, string> dictionary = new(StringComparer.OrdinalIgnoreCase);
            foreach (Token token in tokens)
            {
                AppendExpandedPaths(dictionary, token.Value, false);
                foreach (string file in dictionary.Values)
                {
                    paths.Add(file);
                }

                dictionary.Clear();
            }

            return paths;
        }

        public static TargetOS GetTargetOS(string token)
        {
            if(string.IsNullOrEmpty(token))
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return TargetOS.Windows;
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    return TargetOS.Linux;
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    return TargetOS.OSX;
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
                    return TargetOS.FreeBSD;

                throw new NotImplementedException();
            }

            return token.ToLowerInvariant() switch
            {
                "linux" => TargetOS.Linux,
                "win" or "windows" => TargetOS.Windows,
                "osx" => TargetOS.OSX,
                "freebsd" => TargetOS.FreeBSD,
                "maccatalyst" => TargetOS.MacCatalyst,
                "iossimulator" => TargetOS.iOSSimulator,
                "ios" => TargetOS.iOS,
                "tvossimulator" => TargetOS.tvOSSimulator,
                "tvos" => TargetOS.tvOS,
                _ => throw new CommandLineException($"Target OS '{token}' is not supported")
            };
        }

        public static TargetArchitecture GetTargetArchitecture(string token)
        {
            if(string.IsNullOrEmpty(token))
            {
                return RuntimeInformation.ProcessArchitecture switch
                {
                    Architecture.X86 => TargetArchitecture.X86,
                    Architecture.X64 => TargetArchitecture.X64,
                    Architecture.Arm => TargetArchitecture.ARM,
                    Architecture.Arm64 => TargetArchitecture.ARM64,
                    Architecture.LoongArch64 => TargetArchitecture.LoongArch64,
                    _ => throw new NotImplementedException()
                };
            }
            else
            {
                return token.ToLowerInvariant() switch
                {
                    "x86" => TargetArchitecture.X86,
                    "x64" => TargetArchitecture.X64,
                    "arm" or "armel" => TargetArchitecture.ARM,
                    "arm64" => TargetArchitecture.ARM64,
                    "loongarch64" => TargetArchitecture.LoongArch64,
                    _ => throw new CommandLineException($"Target architecture '{token}' is not supported")
                };
            }
        }

        public static void MakeReproPackage(string makeReproPath, string outputFilePath, string[] args, ParseResult res, IEnumerable<string> inputOptions, IEnumerable<string> outputOptions = null)
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

            HashCode hashCodeOfArgs = default;
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
                Dictionary<string, string> inputToReproPackageFileName = new();
                HashSet<string> outputOptionNames = outputOptions == null ? new HashSet<string>() : new HashSet<string>(outputOptions);
                Dictionary<string, string> outputToReproPackageFileName = new();

                List<string> rspFile = new List<string>();
                foreach (var option in res.CommandResult.Command.Options)
                {
                    if (!res.HasOption(option) || option.Name == "make-repro-path")
                    {
                        continue;
                    }

                    IValueDescriptor descriptor = option;
                    object val = res.CommandResult.GetValue(option);
                    if (val is not null && !(descriptor.HasDefaultValue && descriptor.GetDefaultValue().Equals(val)))
                    {
                        if (val is IEnumerable<string> || val is IDictionary<string, string>)
                        {
                            if (val is not IEnumerable<string> values)
                                values = ((IDictionary<string, string>)val).Values;

                            if (inputOptionNames.Contains(option.Name))
                            {
                                Dictionary<string, string> dictionary = new();
                                foreach (string optInList in values)
                                {
                                    if (!string.IsNullOrEmpty(optInList))
                                        AppendExpandedPaths(dictionary, optInList, false);
                                }
                                foreach (string inputFile in dictionary.Values)
                                {
                                    rspFile.Add($"--{option.Name}:{ConvertFromOriginalPathToReproPackagePath(input: true, inputFile)}");
                                }
                            }
                            else
                            {
                                foreach (string optInList in values)
                                {
                                    if (!string.IsNullOrEmpty(optInList))
                                        rspFile.Add($"--{option.Name}:{optInList}");
                                }
                            }
                        }
                        else
                        {
                            if (val is string stringVal && !string.IsNullOrEmpty(stringVal))
                            {
                                if (outputOptionNames.Contains(option.Name))
                                {
                                    // if output option is used, overwrite the path to the repro package
                                    stringVal = ConvertFromOriginalPathToReproPackagePath(input: false, stringVal);
                                }
                                rspFile.Add($"--{option.Name}:{stringVal}");
                            }
                            else
                            {
                                rspFile.Add($"--{option.Name}:{val}");
                            }
                        }
                    }
                }

                foreach (var argument in res.CommandResult.Command.Arguments)
                {
                    object val = res.CommandResult.GetValue(argument);
                    if (val is IEnumerable<string> || val is IDictionary<string, string>)
                    {
                        if (val is not IEnumerable<string> values)
                            values = ((IDictionary<string, string>)val).Values;

                        foreach (string optInList in values)
                        {
                            rspFile.Add($"{ConvertFromOriginalPathToReproPackagePath(input: true, optInList)}");
                        }
                    }
                    else
                    {
                        rspFile.Add($"{ConvertFromOriginalPathToReproPackagePath(input: true, (string)val)}");
                    }
                }

                ZipArchiveEntry rspEntry = archive.CreateEntry("repro.rsp");
                using (StreamWriter writer = new StreamWriter(rspEntry.Open()))
                {
                    foreach (string s in rspFile)
                        writer.WriteLine(s);
                }

                string ConvertFromOriginalPathToReproPackagePath(bool input, string originalPath)
                {
                    var originalToReproPackageFileName = input ? inputToReproPackageFileName : outputToReproPackageFileName;
                    if (originalToReproPackageFileName.TryGetValue(originalPath, out string reproPackagePath))
                    {
                        return reproPackagePath;
                    }

                    try
                    {
                        string prefix = input ? string.Empty : "out_"; // prefix output directories for clarity
                        string reproFileDir = prefix + originalToReproPackageFileName.Count.ToString() + Path.DirectorySeparatorChar;
                        reproPackagePath = Path.Combine(reproFileDir, Path.GetFileName(originalPath));
                        if (!input)
                            archive.CreateEntry(reproFileDir); // for outputs just create output directory
                        else
                            archive.CreateEntryFromFile(originalPath, reproPackagePath);
                        originalToReproPackageFileName.Add(originalPath, reproPackagePath);

                        return reproPackagePath;
                    }
                    catch
                    {
                        return originalPath;
                    }
                }
            }
        }

        // Helper to create a collection of paths unique in their simple names.
        private static void AppendExpandedPaths(Dictionary<string, string> dictionary, string pattern, bool strict)
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

                    if (dictionary.TryGetValue(simpleName, out string otherFullFileName))
                    {
                        if (strict)
                        {
                            throw new CommandLineException("Multiple input files matching same simple name " +
                                fullFileName + " " + otherFullFileName);
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

        /// <summary>
        /// Read the response file line by line and treat each line as a single token.
        /// Skip the comment lines that start with `#`.
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        /// <remarks>
        /// This method does not support:
        ///   * referencing another response file.
        ///   * inline `#` comments.
        /// </remarks>
        public static bool TryReadResponseFile(string filePath, out IReadOnlyList<string> newTokens, out string error)
        {
            try
            {
                var tokens = new List<string>();
                foreach (string line in File.ReadAllLines(filePath))
                {
                    string token = line.Trim();
                    if (token.Length > 0 && token[0] != '#')
                    {
                        if (token.EndsWith('"'))
                        {
                            int firstQuotePosition = token.IndexOf('"');

                            // strip leading and trailing quotes from value.
                            if (firstQuotePosition >= 0 && firstQuotePosition < token.Length - 1 &&
                                (firstQuotePosition == 0 || token[firstQuotePosition - 1] != '\\'))
                            {
                                token = token[..firstQuotePosition] + token[(firstQuotePosition + 1)..^1];
                            }
                        }

                        tokens.Add(token);
                    }
                }

                newTokens = tokens;
                error = null;
                return true;
            }
            catch (FileNotFoundException)
            {
                error = $"Response file not found: '{filePath}'";
            }
            catch (IOException e)
            {
                error = $"Error reading response file '{filePath}': {e}";
            }

            newTokens = null;
            return false;
        }
    }
}
