// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Parsing;
using System.IO;
using System.Runtime.InteropServices;

using Internal.TypeSystem;

namespace ILCompiler
{
    internal class Crossgen2RootCommand : RootCommand
    {
        public Argument<Dictionary<string, string>> InputFilePaths { get; } =
            new("input-file-path", result => Helpers.BuildPathDictionary(result.Tokens, true), false, "Input file(s)") { Arity = ArgumentArity.OneOrMore };
        public Option<Dictionary<string, string>> UnrootedInputFilePaths { get; } =
            new(new[] { "--unrooted-input-file-paths", "-u" }, result => Helpers.BuildPathDictionary(result.Tokens, true), true, SR.UnrootedInputFilesToCompile);
        public Option<Dictionary<string, string>> ReferenceFilePaths { get; } =
            new(new[] { "--reference", "-r" }, result => Helpers.BuildPathDictionary(result.Tokens, false), true, SR.ReferenceFiles);
        public Option<string> InstructionSet { get; } =
            new(new[] { "--instruction-set" }, SR.InstructionSets);
        public Option<string[]> MibcFilePaths { get; } =
            new(new[] { "--mibc", "-m" }, Array.Empty<string>, SR.MibcFiles);
        public Option<string> OutputFilePath { get; } =
            new(new[] { "--out", "-o" }, SR.OutputFilePath);
        public Option<string> CompositeRootPath { get; } =
            new(new[] { "--compositerootpath", "--crp" }, SR.CompositeRootPath);
        public Option<bool> Optimize { get; } =
            new(new[] { "--optimize", "-O" }, SR.EnableOptimizationsOption);
        public Option<bool> OptimizeDisabled { get; } =
            new(new[] { "--optimize-disabled", "-Od" }, SR.DisableOptimizationsOption);
        public Option<bool> OptimizeSpace { get; } =
            new(new[] { "--optimize-space", "-Os" }, SR.OptimizeSpaceOption);
        public Option<bool> OptimizeTime { get; } =
            new(new[] { "--optimize-time", "-Ot" }, SR.OptimizeSpeedOption);
        public Option<bool> InputBubble { get; } =
            new(new[] { "--inputbubble" }, SR.InputBubbleOption);
        public Option<Dictionary<string, string>> InputBubbleReferenceFilePaths { get; } =
            new(new[] { "--inputbubbleref" }, result => Helpers.BuildPathDictionary(result.Tokens, false), true, SR.InputBubbleReferenceFiles);
        public Option<bool> Composite { get; } =
            new(new[] { "--composite" }, SR.CompositeBuildMode);
        public Option<string> CompositeKeyFile { get; } =
            new(new[] { "--compositekeyfile" }, SR.CompositeKeyFile);
        public Option<bool> CompileNoMethods { get; } =
            new(new[] { "--compile-no-methods" }, SR.CompileNoMethodsOption);
        public Option<bool> OutNearInput { get; } =
            new(new[] { "--out-near-input" }, SR.OutNearInputOption);
        public Option<bool> SingleFileCompilation { get; } =
            new(new[] { "--single-file-compilation" }, SR.SingleFileCompilationOption);
        public Option<bool> Partial { get; } =
            new(new[] { "--partial" }, SR.PartialImageOption);
        public Option<bool> CompileBubbleGenerics { get; } =
            new(new[] { "--compilebubblegenerics" }, SR.BubbleGenericsOption);
        public Option<bool> EmbedPgoData { get; } =
            new(new[] { "--embed-pgo-data" }, SR.EmbedPgoDataOption);
        public Option<string> DgmlLogFileName { get; } =
            new(new[] { "--dgmllog" }, SR.SaveDependencyLogOption);
        public Option<bool> GenerateFullDgmlLog { get; } =
            new(new[] { "--fulllog" }, SR.SaveDetailedLogOption);
        public Option<bool> IsVerbose { get; } =
            new(new[] { "--verbose" }, SR.VerboseLoggingOption);
        public Option<string> SystemModuleName { get; } =
            new(new[] { "--systemmodule" }, () => Helpers.DefaultSystemModule, SR.SystemModuleOverrideOption);
        public Option<bool> WaitForDebugger { get; } =
            new(new[] { "--waitfordebugger" }, SR.WaitForDebuggerOption);
        public Option<string[]> CodegenOptions { get; } =
            new(new[] { "--codegenopt" }, Array.Empty<string>, SR.CodeGenOptions);
        public Option<bool> SupportIbc { get; } =
            new(new[] { "--support-ibc" }, SR.SupportIbc);
        public Option<bool> Resilient { get; } =
            new(new[] { "--resilient" }, SR.ResilientOption);
        public Option<string> ImageBase { get; } =
            new(new[] { "--imagebase" }, SR.ImageBase);
        public Option<TargetArchitecture> TargetArchitecture { get; } =
            new(new[] { "--targetarch" }, result =>
            {
                string firstToken = result.Tokens.Count > 0 ? result.Tokens[0].Value : null;
                if (firstToken != null && firstToken.Equals("armel", StringComparison.OrdinalIgnoreCase))
                {
                    IsArmel = true;
                    return Internal.TypeSystem.TargetArchitecture.ARM;
                }

                return Helpers.GetTargetArchitecture(firstToken);
            }, true, SR.TargetArchOption) { Arity = ArgumentArity.OneOrMore };
        public Option<TargetOS> TargetOS { get; } =
            new(new[] { "--targetos" }, result => Helpers.GetTargetOS(result.Tokens.Count > 0 ? result.Tokens[0].Value : null), true, SR.TargetOSOption);
        public Option<string> JitPath { get; } =
            new(new[] { "--jitpath" }, SR.JitPathOption);
        public Option<bool> PrintReproInstructions { get; } =
            new(new[] { "--print-repro-instructions" }, SR.PrintReproInstructionsOption);
        public Option<string> SingleMethodTypeName { get; } =
            new(new[] { "--singlemethodtypename" }, SR.SingleMethodTypeName);
        public Option<string> SingleMethodName { get; } =
            new(new[] { "--singlemethodname" }, SR.SingleMethodMethodName);
        public Option<int> SingleMethodIndex { get; } =
            new(new[] { "--singlemethodindex" }, SR.SingleMethodIndex);
        public Option<string[]> SingleMethodGenericArgs { get; } =
            new(new[] { "--singlemethodgenericarg" }, SR.SingleMethodGenericArgs);
        public Option<int> Parallelism { get; } =
            new(new[] { "--parallelism" }, result =>
            {
                if (result.Tokens.Count > 0)
                    return int.Parse(result.Tokens[0].Value);

                // Limit parallelism to 24 wide at most by default, more parallelism is unlikely to improve compilation speed
                // as many portions of the process are single threaded, and is known to use excessive memory.
                var parallelism = Math.Min(24, Environment.ProcessorCount);

                // On 32bit platforms restrict it more, as virtual address space is quite limited
                if (!Environment.Is64BitProcess)
                    parallelism = Math.Min(4, parallelism);

                return parallelism;
            }, true, SR.ParalellismOption);
        public Option<int> CustomPESectionAlignment { get; } =
            new(new[] { "--custom-pe-section-alignment" }, SR.CustomPESectionAlignmentOption);
        public Option<bool> Map { get; } =
            new(new[] { "--map" }, SR.MapFileOption);
        public Option<bool> MapCsv { get; } =
            new(new[] { "--mapcsv" }, SR.MapCsvFileOption);
        public Option<bool> Pdb { get; } =
            new(new[] { "--pdb" }, SR.PdbFileOption);
        public Option<string> PdbPath { get; } =
            new(new[] { "--pdb-path" }, SR.PdbFilePathOption);
        public Option<bool> PerfMap { get; } =
            new(new[] { "--perfmap" }, SR.PerfMapFileOption);
        public Option<string> PerfMapPath { get; } =
            new(new[] { "--perfmap-path" }, SR.PerfMapFilePathOption);
        public Option<int> PerfMapFormatVersion { get; } =
            new(new[] { "--perfmap-format-version" }, () => 0, SR.PerfMapFormatVersionOption);
        public Option<string[]> CrossModuleInlining { get; } =
            new(new[] { "--opt-cross-module" }, SR.CrossModuleInlining);
        public Option<bool> AsyncMethodOptimization { get; } =
            new(new[] { "--opt-async-methods" }, SR.AsyncModuleOptimization);
        public Option<string> NonLocalGenericsModule { get; } =
            new(new[] { "--non-local-generics-module" }, () => string.Empty, SR.NonLocalGenericsModule);
        public Option<ReadyToRunMethodLayoutAlgorithm> MethodLayout { get; } =
            new(new[] { "--method-layout" }, result =>
            {
                if (result.Tokens.Count == 0 )
                    return ReadyToRunMethodLayoutAlgorithm.DefaultSort;

                return result.Tokens[0].Value.ToLowerInvariant() switch
                {
                    "defaultsort" => ReadyToRunMethodLayoutAlgorithm.DefaultSort,
                    "exclusiveweight" => ReadyToRunMethodLayoutAlgorithm.ExclusiveWeight,
                    "hotcold" => ReadyToRunMethodLayoutAlgorithm.HotCold,
                    "hotwarmcold" => ReadyToRunMethodLayoutAlgorithm.HotWarmCold,
                    "callfrequency" => ReadyToRunMethodLayoutAlgorithm.CallFrequency,
                    "pettishansen" => ReadyToRunMethodLayoutAlgorithm.PettisHansen,
                    "random" => ReadyToRunMethodLayoutAlgorithm.Random,
                    _ => throw new CommandLineException(SR.InvalidMethodLayout)
                };
            }, true, SR.MethodLayoutOption);
        public Option<ReadyToRunFileLayoutAlgorithm> FileLayout { get; } =
            new(new[] { "--file-layout" }, result =>
            {
                if (result.Tokens.Count == 0 )
                    return ReadyToRunFileLayoutAlgorithm.DefaultSort;

                return result.Tokens[0].Value.ToLowerInvariant() switch
                {
                    "defaultsort" => ReadyToRunFileLayoutAlgorithm.DefaultSort,
                    "methodorder" => ReadyToRunFileLayoutAlgorithm.MethodOrder,
                    _ => throw new CommandLineException(SR.InvalidFileLayout)
                };
            }, true, SR.FileLayoutOption);
        public Option<bool> VerifyTypeAndFieldLayout { get; } =
            new(new[] { "--verify-type-and-field-layout" }, SR.VerifyTypeAndFieldLayoutOption);
        public Option<string> CallChainProfileFile { get; } =
            new(new[] { "--callchain-profile" }, SR.CallChainProfileFile);
        public Option<string> MakeReproPath { get; } =
            new(new[] { "--make-repro-path" }, "Path where to place a repro package");
        public Option<bool> HotColdSplitting { get; } =
            new(new[] { "--hot-cold-splitting" }, SR.HotColdSplittingOption);

        public bool CompositeOrInputBubble { get; private set; }
        public OptimizationMode OptimizationMode { get; private set; }
        public ParseResult Result { get; private set; }

        public static bool IsArmel { get; private set; }

        public Crossgen2RootCommand(string[] args) : base(SR.Crossgen2BannerText)
        {
            AddArgument(InputFilePaths);
            AddOption(UnrootedInputFilePaths);
            AddOption(ReferenceFilePaths);
            AddOption(InstructionSet);
            AddOption(MibcFilePaths);
            AddOption(OutputFilePath);
            AddOption(CompositeRootPath);
            AddOption(Optimize);
            AddOption(OptimizeDisabled);
            AddOption(OptimizeSpace);
            AddOption(OptimizeTime);
            AddOption(InputBubble);
            AddOption(InputBubbleReferenceFilePaths);
            AddOption(Composite);
            AddOption(CompositeKeyFile);
            AddOption(CompileNoMethods);
            AddOption(OutNearInput);
            AddOption(SingleFileCompilation);
            AddOption(Partial);
            AddOption(CompileBubbleGenerics);
            AddOption(EmbedPgoData);
            AddOption(DgmlLogFileName);
            AddOption(GenerateFullDgmlLog);
            AddOption(IsVerbose);
            AddOption(SystemModuleName);
            AddOption(WaitForDebugger);
            AddOption(CodegenOptions);
            AddOption(SupportIbc);
            AddOption(Resilient);
            AddOption(ImageBase);
            AddOption(TargetArchitecture);
            AddOption(TargetOS);
            AddOption(JitPath);
            AddOption(PrintReproInstructions);
            AddOption(SingleMethodTypeName);
            AddOption(SingleMethodName);
            AddOption(SingleMethodIndex);
            AddOption(SingleMethodGenericArgs);
            AddOption(Parallelism);
            AddOption(CustomPESectionAlignment);
            AddOption(Map);
            AddOption(MapCsv);
            AddOption(Pdb);
            AddOption(PdbPath);
            AddOption(PerfMap);
            AddOption(PerfMapPath);
            AddOption(PerfMapFormatVersion);
            AddOption(CrossModuleInlining);
            AddOption(AsyncMethodOptimization);
            AddOption(NonLocalGenericsModule);
            AddOption(MethodLayout);
            AddOption(FileLayout);
            AddOption(VerifyTypeAndFieldLayout);
            AddOption(CallChainProfileFile);
            AddOption(MakeReproPath);
            AddOption(HotColdSplitting);

            this.SetHandler(context =>
            {
                Result = context.ParseResult;
                CompositeOrInputBubble = context.ParseResult.GetValueForOption(Composite) | context.ParseResult.GetValueForOption(InputBubble);
                if (context.ParseResult.GetValueForOption(OptimizeSpace))
                {
                    OptimizationMode = OptimizationMode.PreferSize;
                }
                else if (context.ParseResult.GetValueForOption(OptimizeTime))
                {
                    OptimizationMode = OptimizationMode.PreferSpeed;
                }
                else if (context.ParseResult.GetValueForOption(Optimize))
                {
                    OptimizationMode = OptimizationMode.Blended;
                }
                else
                {
                    OptimizationMode = OptimizationMode.None;
                }

                try
                {
                    int alignment = context.ParseResult.GetValueForOption(CustomPESectionAlignment);
                    if (alignment != 0)
                    {
                        // Must be a power of two and >= 4096
                        if (alignment < 4096 || (alignment & (alignment - 1)) != 0)
                            throw new CommandLineException(SR.InvalidCustomPESectionAlignment);
                    }

                    string makeReproPath = context.ParseResult.GetValueForOption(MakeReproPath);
                    if (makeReproPath != null)
                    {
                        // Create a repro package in the specified path
                        // This package will have the set of input files needed for compilation
                        // + the original command line arguments
                        // + a rsp file that should work to directly run out of the zip file

                        Helpers.MakeReproPackage(makeReproPath, context.ParseResult.GetValueForOption(OutputFilePath), args,
                            context.ParseResult, new[] { "r", "reference", "u", "unrooted-input-file-paths", "m", "mibc", "inputbubbleref" });
                    }

                    context.ExitCode = new Program(this).Run();
                }
#if DEBUG
                catch (CodeGenerationFailedException ex) when (DumpReproArguments(ex))
                {
                    throw new NotSupportedException(); // Unreachable
                }
#else
                catch (Exception e)
                {
                    Console.ResetColor();
                    Console.ForegroundColor = ConsoleColor.Red;

                    Console.Error.WriteLine("Error: " + e.Message);
                    Console.Error.WriteLine(e.ToString());

                    Console.ResetColor();

                    context.ExitCode = 1;
                }
#endif
            });
        }

        public static IEnumerable<HelpSectionDelegate> GetExtendedHelp(HelpContext _)
        {
            foreach (HelpSectionDelegate sectionDelegate in HelpBuilder.Default.GetLayout())
                yield return sectionDelegate;

            yield return _ =>
            {
                Console.WriteLine(SR.OptionPassingHelp);
                Console.WriteLine();
                Console.WriteLine(SR.DashDashHelp);
                Console.WriteLine();

                string[] ValidArchitectures = new string[] {"arm", "armel", "arm64", "x86", "x64"};
                string[] ValidOS = new string[] {"windows", "linux", "osx"};

                Console.WriteLine(String.Format(SR.SwitchWithDefaultHelp, "--targetos", String.Join("', '", ValidOS), Helpers.GetTargetOS(null).ToString().ToLowerInvariant()));
                Console.WriteLine();
                Console.WriteLine(String.Format(SR.SwitchWithDefaultHelp, "--targetarch", String.Join("', '", ValidArchitectures), Helpers.GetTargetArchitecture(null).ToString().ToLowerInvariant()));
                Console.WriteLine();

                Console.WriteLine(SR.InstructionSetHelp);
                foreach (string arch in ValidArchitectures)
                {
                    Console.Write(arch);
                    Console.Write(": ");

                    TargetArchitecture targetArch = Helpers.GetTargetArchitecture(arch);
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
                                Console.Write(", ");
                            }
                            Console.Write(instructionSet.Name);
                        }
                    }

                    Console.WriteLine();
                }

                Console.WriteLine();
                Console.WriteLine(SR.CpuFamilies);
                Console.WriteLine(string.Join(", ", Internal.JitInterface.InstructionSetFlags.AllCpuNames));
            };
        }

#if DEBUG
        private static bool DumpReproArguments(CodeGenerationFailedException ex)
        {
            Console.WriteLine(SR.DumpReproInstructions);

            MethodDesc failingMethod = ex.Method;
            Console.WriteLine(Program.CreateReproArgumentString(failingMethod));
            return false;
        }
#endif
    }
}
