// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;

using Internal.CommandLine;
using Internal.TypeSystem;

namespace ILCompiler
{
    internal class CommandLineOptions
    {
        public const int DefaultPerfMapFormatVersion = 0;

        public bool Help;
        public string HelpText;

        public IReadOnlyList<string> InputFilePaths;
        public IReadOnlyList<string> InputBubbleReferenceFilePaths;
        public IReadOnlyList<string> UnrootedInputFilePaths;
        public IReadOnlyList<string> ReferenceFilePaths;
        public IReadOnlyList<string> MibcFilePaths;
        public string InstructionSet;
        public string OutputFilePath;

        public string CompositeRootPath;
        public bool Optimize;
        public bool OptimizeDisabled;
        public bool OptimizeSpace;
        public bool OptimizeTime;
        public bool InputBubble;
        public bool CompileBubbleGenerics;
        public bool Verbose;
        public bool Composite;
        public string CompositeKeyFile;
        public bool CompileNoMethods;
        public bool EmbedPgoData;
        public bool OutNearInput;
        public bool SingleFileCompilation;

        public string DgmlLogFileName;
        public bool GenerateFullDgmlLog;

        public string TargetArch;
        public string TargetOS;
        public string JitPath;
        public string SystemModule;
        public bool WaitForDebugger;
        public bool Tuning;
        public bool Partial;
        public bool Resilient;
        public bool Map;
        public bool MapCsv;
        public bool PrintReproInstructions;
        public bool Pdb;
        public string PdbPath;
        public bool PerfMap;
        public string PerfMapPath;
        public int PerfMapFormatVersion;
        public int Parallelism;
        public int CustomPESectionAlignment;
        public string MethodLayout;
        public string FileLayout;
        public bool VerifyTypeAndFieldLayout;
        public string CallChainProfileFile;

        public string SingleMethodTypeName;
        public string SingleMethodName;
        public int SingleMethodIndex;
        public IReadOnlyList<string> SingleMethodGenericArg;

        public IReadOnlyList<string> CodegenOptions;

        public string MakeReproPath;

        public bool CompositeOrInputBubble => Composite || InputBubble;

        public CommandLineOptions(string[] args)
        {
            InputFilePaths = Array.Empty<string>();
            InputBubbleReferenceFilePaths = Array.Empty<string>();
            UnrootedInputFilePaths = Array.Empty<string>();
            ReferenceFilePaths = Array.Empty<string>();
            MibcFilePaths = Array.Empty<string>();
            CodegenOptions = Array.Empty<string>();

            PerfMapFormatVersion = DefaultPerfMapFormatVersion;
            Parallelism = Environment.ProcessorCount;
            SingleMethodGenericArg = null;

            bool forceHelp = false;
            if (args.Length == 0)
            {
                forceHelp = true;
            }

            foreach (string arg in args)
            {
                if (arg == "-?")
                    forceHelp = true;
            }

            if (forceHelp)
            {
                args = new string[] {"--help"};
            }

            ArgumentSyntax argSyntax = ArgumentSyntax.Parse(args, syntax =>
            {
                syntax.ApplicationName = typeof(Program).Assembly.GetName().Name.ToString();

                // HandleHelp writes to error, fails fast with crash dialog and lacks custom formatting.
                syntax.HandleHelp = false;
                syntax.HandleErrors = true;

                syntax.DefineOptionList("u|unrooted-input-file-paths", ref UnrootedInputFilePaths, SR.UnrootedInputFilesToCompile);
                syntax.DefineOptionList("r|reference", ref ReferenceFilePaths, SR.ReferenceFiles);
                syntax.DefineOption("instruction-set", ref InstructionSet, SR.InstructionSets);
                syntax.DefineOptionList("m|mibc", ref MibcFilePaths, SR.MibcFiles);
                syntax.DefineOption("o|out|outputfilepath", ref OutputFilePath, SR.OutputFilePath);
                syntax.DefineOption("crp|compositerootpath", ref CompositeRootPath, SR.CompositeRootPath);
                syntax.DefineOption("O|optimize", ref Optimize, SR.EnableOptimizationsOption);
                syntax.DefineOption("Od|optimize-disabled", ref OptimizeDisabled, SR.DisableOptimizationsOption);
                syntax.DefineOption("Os|optimize-space", ref OptimizeSpace, SR.OptimizeSpaceOption);
                syntax.DefineOption("Ot|optimize-time", ref OptimizeTime, SR.OptimizeSpeedOption);
                syntax.DefineOption("inputbubble", ref InputBubble, SR.InputBubbleOption);
                syntax.DefineOptionList("inputbubbleref", ref InputBubbleReferenceFilePaths, SR.InputBubbleReferenceFiles);
                syntax.DefineOption("composite", ref Composite, SR.CompositeBuildMode);
                syntax.DefineOption("compositekeyfile", ref CompositeKeyFile, SR.CompositeKeyFile);
                syntax.DefineOption("compile-no-methods", ref CompileNoMethods, SR.CompileNoMethodsOption);
                syntax.DefineOption("out-near-input", ref OutNearInput, SR.OutNearInputOption);
                syntax.DefineOption("single-file-compilation", ref SingleFileCompilation, SR.SingleFileCompilationOption);
                syntax.DefineOption("tuning", ref Tuning, SR.TuningImageOption);
                syntax.DefineOption("partial", ref Partial, SR.PartialImageOption);
                syntax.DefineOption("compilebubblegenerics", ref CompileBubbleGenerics, SR.BubbleGenericsOption);
                syntax.DefineOption("embed-pgo-data", ref EmbedPgoData, SR.EmbedPgoDataOption);
                syntax.DefineOption("dgmllog|dgml-log-file-name", ref DgmlLogFileName, SR.SaveDependencyLogOption);
                syntax.DefineOption("fulllog|generate-full-dmgl-log", ref GenerateFullDgmlLog, SR.SaveDetailedLogOption);
                syntax.DefineOption("verbose", ref Verbose, SR.VerboseLoggingOption);
                syntax.DefineOption("systemmodule", ref SystemModule, SR.SystemModuleOverrideOption);
                syntax.DefineOption("waitfordebugger", ref WaitForDebugger, SR.WaitForDebuggerOption);
                syntax.DefineOptionList("codegenopt|codegen-options", ref CodegenOptions, SR.CodeGenOptions);
                syntax.DefineOption("resilient", ref Resilient, SR.ResilientOption);

                syntax.DefineOption("targetarch", ref TargetArch, SR.TargetArchOption);
                syntax.DefineOption("targetos", ref TargetOS, SR.TargetOSOption);
                syntax.DefineOption("jitpath", ref JitPath, SR.JitPathOption);

                syntax.DefineOption("print-repro-instructions", ref PrintReproInstructions, SR.PrintReproInstructionsOption);
                syntax.DefineOption("singlemethodtypename", ref SingleMethodTypeName, SR.SingleMethodTypeName);
                syntax.DefineOption("singlemethodname", ref SingleMethodName, SR.SingleMethodMethodName);
                syntax.DefineOption("singlemethodindex", ref SingleMethodIndex, SR.SingleMethodIndex);
                syntax.DefineOptionList("singlemethodgenericarg", ref SingleMethodGenericArg, SR.SingleMethodGenericArgs);

                syntax.DefineOption("parallelism", ref Parallelism, SR.ParalellismOption);
                syntax.DefineOption("custom-pe-section-alignment", ref CustomPESectionAlignment, SR.CustomPESectionAlignmentOption);
                syntax.DefineOption("map", ref Map, SR.MapFileOption);
                syntax.DefineOption("mapcsv", ref MapCsv, SR.MapCsvFileOption);
                syntax.DefineOption("pdb", ref Pdb, SR.PdbFileOption);
                syntax.DefineOption("pdb-path", ref PdbPath, SR.PdbFilePathOption);
                syntax.DefineOption("perfmap", ref PerfMap, SR.PerfMapFileOption);
                syntax.DefineOption("perfmap-path", ref PerfMapPath, SR.PerfMapFilePathOption);
                syntax.DefineOption("perfmap-format-version", ref PerfMapFormatVersion, SR.PerfMapFormatVersionOption);

                syntax.DefineOption("method-layout", ref MethodLayout, SR.MethodLayoutOption);
                syntax.DefineOption("file-layout", ref FileLayout, SR.FileLayoutOption);
                syntax.DefineOption("verify-type-and-field-layout", ref VerifyTypeAndFieldLayout, SR.VerifyTypeAndFieldLayoutOption);
                syntax.DefineOption("callchain-profile", ref CallChainProfileFile, SR.CallChainProfileFile);

                syntax.DefineOption("make-repro-path", ref MakeReproPath, SR.MakeReproPathHelp);

                syntax.DefineOption("h|help", ref Help, SR.HelpOption);

                syntax.DefineParameterList("in", ref InputFilePaths, SR.InputFilesToCompile);
            });

            if (Help)
            {
                List<string> extraHelp = new List<string>();
                extraHelp.Add(SR.OptionPassingHelp);
                extraHelp.Add("");
                extraHelp.Add(SR.DashDashHelp);
                extraHelp.Add("");

                string[] ValidArchitectures = new string[] {"arm", "armel", "arm64", "x86", "x64"};
                string[] ValidOS = new string[] {"windows", "linux", "osx"};
                TargetOS defaultOs;
                TargetArchitecture defaultArch;
                Program.ComputeDefaultOptions(out defaultOs, out defaultArch);

                extraHelp.Add(String.Format(SR.SwitchWithDefaultHelp, "--targetos", String.Join("', '", ValidOS), defaultOs.ToString().ToLowerInvariant()));

                extraHelp.Add("");

                extraHelp.Add(String.Format(SR.SwitchWithDefaultHelp, "--targetarch", String.Join("', '", ValidArchitectures), defaultArch.ToString().ToLowerInvariant()));

                extraHelp.Add("");

                extraHelp.Add(SR.InstructionSetHelp);
                foreach (string arch in ValidArchitectures)
                {
                    StringBuilder archString = new StringBuilder();

                    archString.Append(arch);
                    archString.Append(": ");

                    TargetArchitecture targetArch = Program.GetTargetArchitectureFromArg(arch, out _);
                    bool first = true;
                    foreach (var instructionSet in Internal.JitInterface.InstructionSetFlags.ArchitectureToValidInstructionSets(targetArch))
                    {
                        // Only instruction sets with are specifiable should be printed to the help text
                        if (instructionSet.Specifiable)
                        {
                            if (first)
                            {
                                first = false;
                            }
                            else
                            {
                                archString.Append(", ");
                            }
                            archString.Append(instructionSet.Name);
                        }
                    }

                    extraHelp.Add(archString.ToString());
                }

                argSyntax.ExtraHelpParagraphs = extraHelp;

                HelpText = argSyntax.GetHelpText();
            }

            if (MakeReproPath != null)
            {
                // Create a repro package in the specified path
                // This package will have the set of input files needed for compilation
                // + the original command line arguments
                // + a rsp file that should work to directly run out of the zip file

                string makeReproPath = MakeReproPath;
                Directory.CreateDirectory(makeReproPath);

                List<string> crossgenDetails = new List<string>();
                crossgenDetails.Add("CrossGen2 version");
                try
                {
                    crossgenDetails.Add(Environment.GetCommandLineArgs()[0]);
                } catch  {}
                try
                {
                    crossgenDetails.Add(System.Diagnostics.FileVersionInfo.GetVersionInfo(Environment.GetCommandLineArgs()[0]).ToString());
                } catch  {}

                crossgenDetails.Add("------------------------");
                crossgenDetails.Add("Actual Command Line Args");
                crossgenDetails.Add("------------------------");
                crossgenDetails.AddRange(args);
                foreach (string arg in args)
                {
                    if (arg.StartsWith('@'))
                    {
                        string rspFileName = arg.Substring(1);
                        crossgenDetails.Add("------------------------");
                        crossgenDetails.Add(rspFileName);
                        crossgenDetails.Add("------------------------");
                        try
                        {
                            crossgenDetails.AddRange(File.ReadAllLines(rspFileName));
                        } catch  {}
                    }
                }

                HashCode hashCodeOfArgs = new HashCode();
                foreach (string s in crossgenDetails)
                    hashCodeOfArgs.Add(s);

                string zipFileName = ((uint)hashCodeOfArgs.ToHashCode()).ToString();

                if (OutputFilePath != null)
                    zipFileName = zipFileName + "_" + Path.GetFileName(OutputFilePath);

                zipFileName = Path.Combine(MakeReproPath, Path.ChangeExtension(zipFileName, ".zip"));

                Console.WriteLine($"Creating {zipFileName}");
                using (var archive = ZipFile.Open(zipFileName, ZipArchiveMode.Create))
                {
                    ZipArchiveEntry commandEntry = archive.CreateEntry("crossgen2command.txt");
                    using (StreamWriter writer = new StreamWriter(commandEntry.Open()))
                    {
                        foreach (string s in crossgenDetails)
                            writer.WriteLine(s);
                    }

                    HashSet<string> inputOptionNames = new HashSet<string>();
                    inputOptionNames.Add("-r");
                    inputOptionNames.Add("-u");
                    inputOptionNames.Add("-m");
                    inputOptionNames.Add("--inputbubbleref");
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

                    ZipArchiveEntry rspEntry = archive.CreateEntry("crossgen2repro.rsp");
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
        }
    }
}
