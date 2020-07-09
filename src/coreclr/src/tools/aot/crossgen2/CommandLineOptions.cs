// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CommandLine;
using System.IO;

namespace ILCompiler
{
    public class CommandLineOptions
    {
        public FileInfo[] InputFilePaths { get; set; }
        public FileInfo[] UnrootedInputFilePaths { get; set; }
        public FileInfo[] Mibc { get; set; }
        public string[] Reference { get; set; }
        public string InstructionSet { get; set; }
        public FileInfo OutputFilePath { get; set; }

        public DirectoryInfo CompositeRootPath { get; set; }
        public bool Optimize { get; set; }
        public bool OptimizeSpace { get; set; }
        public bool OptimizeTime { get; set; }
        public bool InputBubble { get; set; }
        public bool CompileBubbleGenerics { get; set; }
        public bool Verbose { get; set; }
        public bool Composite { get; set; }
        public bool CompileNoMethods { get; set; }

        public FileInfo DgmlLogFileName { get; set; }
        public bool GenerateFullDgmlLog { get; set; }

        public string TargetArch { get; set; }
        public string TargetOS { get; set; }
        public FileInfo JitPath { get; set; }
        public string SystemModule { get; set; }
        public bool WaitForDebugger { get; set; }
        public bool Tuning { get; set; }
        public bool Partial { get; set; }
        public bool Resilient { get; set; }
        public bool Map { get; set; }
        public int Parallelism { get; set; }
        public ReadyToRunMethodLayoutAlgorithm MethodLayout { get; set; }
        public ReadyToRunFileLayoutAlgorithm FileLayout { get; set; }
        public int? CustomPESectionAlignment { get; set; }
        public bool VerifyTypeAndFieldLayout { get; set; }

        public string SingleMethodTypeName { get; set; }
        public string SingleMethodName { get; set; }
        public string[] SingleMethodGenericArg { get; set; }

        public string[] CodegenOptions { get; set; }

        public bool CompositeOrInputBubble => Composite || InputBubble;

        public static Command RootCommand()
        {
            // For some reason, arity caps at 255 by default
            ArgumentArity arbitraryArity = new ArgumentArity(0, 100000);

            return new Command("Crossgen2Compilation")
            {
                new Argument<FileInfo[]>() 
                { 
                    Name = "input-file-paths", 
                    Description = SR.InputFilesToCompile,
                    Arity = arbitraryArity,
                },
                new Option(new[] { "--unrooted-input-file-paths", "-u" }, SR.UnrootedInputFilesToCompile)
                {
                    Argument = new Argument<FileInfo[]>()
                    {
                        Arity = arbitraryArity
                    }
                },
                new Option(new[] { "--reference", "-r" }, SR.ReferenceFiles)
                {
                    Argument = new Argument<string[]>() 
                    { 
                        Arity = arbitraryArity
                    } 
                },
                new Option(new[] { "--instruction-set" }, SR.InstructionSets)
                {
                    Argument = new Argument<string>() 
                },
                new Option(new[] { "--mibc", "-m" }, SR.MibcFiles)
                {
                    Argument = new Argument<string[]>()
                    {
                        Arity = arbitraryArity
                    }
                },
                new Option(new[] { "--outputfilepath", "--out", "-o" }, SR.OutputFilePath)
                {
                    Argument = new Argument<FileInfo>()
                },
                new Option(new[] { "--compositerootpath", "--crp" }, SR.CompositeRootPath)
                {
                    Argument = new Argument<DirectoryInfo>()
                },
                new Option(new[] { "--optimize", "-O" }, SR.EnableOptimizationsOption) 
                { 
                    Argument = new Argument<bool>() 
                },
                new Option(new[] { "--optimize-space", "--Os" }, SR.OptimizeSpaceOption) 
                { 
                    Argument = new Argument<bool>() 
                },
                new Option(new[] { "--optimize-time", "--Ot" }, SR.OptimizeSpeedOption),
                new Option(new[] { "--inputbubble" }, SR.InputBubbleOption),
                new Option(new[] { "--composite" }, SR.CompositeBuildMode),
                new Option(new[] { "--compile-no-methods" }, SR.CompileNoMethodsOption),
                new Option(new[] { "--tuning" }, SR.TuningImageOption) 
                {
                    Argument = new Argument<bool>() 
                },
                new Option(new[] { "--partial" }, SR.PartialImageOption) 
                { 
                    Argument = new Argument<bool>() 
                },
                new Option(new[] { "--compilebubblegenerics" }, SR.BubbleGenericsOption) 
                { 
                    Argument = new Argument<bool>() 
                },
                new Option(new[] { "--dgml-log-file-name", "--dmgllog" }, SR.SaveDependencyLogOption) 
                { 
                    Argument = new Argument<FileInfo>() 
                },
                new Option(new[] { "--generate-full-dmgl-log", "--fulllog" }, SR.SaveDetailedLogOption) 
                { 
                    Argument = new Argument<bool>() 
                },
                new Option(new[] { "--verbose" }, SR.VerboseLoggingOption) 
                { 
                    Argument = new Argument<bool>() 
                },
                new Option(new[] { "--systemmodule" }, SR.SystemModuleOverrideOption) 
                { 
                    Argument = new Argument<string>() 
                },
                new Option(new[] { "--waitfordebugger" }, SR.WaitForDebuggerOption) 
                { 
                    Argument = new Argument<bool>() 
                },
                new Option(new[] { "--codegen-options", "--codegenopt" }, SR.CodeGenOptions) 
                { 
                    Argument = new Argument<string[]>()
                    {
                        Arity = arbitraryArity
                    }
                },
                new Option(new[] { "--resilient" }, SR.ResilientOption) 
                { 
                    Argument = new Argument<bool>() 
                },
                new Option(new[] { "--targetarch" }, SR.TargetArchOption) 
                { 
                    Argument = new Argument<string>() 
                },
                new Option(new[] { "--targetos" }, SR.TargetOSOption) 
                { 
                    Argument = new Argument<string>() 
                },
                new Option(new[] { "--jitpath" }, SR.JitPathOption) 
                { 
                    Argument =  new Argument<FileInfo>() 
                },
                new Option(new[] { "--singlemethodtypename" }, SR.SingleMethodTypeName) 
                { 
                    Argument = new Argument<string>() 
                },
                new Option(new[] { "--singlemethodname" }, SR.SingleMethodMethodName) 
                { 
                    Argument = new Argument<string>() 
                },
                new Option(new[] { "--singlemethodgenericarg" }, SR.SingleMethodGenericArgs) 
                { 
                    // We don't need to override arity here as 255 is the maximum number of generic arguments
                    Argument = new Argument<string[]>()
                },
                new Option(new[] { "--parallelism" }, SR.ParalellismOption)
                { 
                    Argument = new Argument<int>(() => Environment.ProcessorCount)
                },
                new Option(new[] { "--custom-pe-section-alignment" }, SR.CustomPESectionAlignmentOption)
                { 
                    Argument = new Argument<int?>()
                },
                new Option(new[] { "--map" }, SR.MapFileOption)
                {
                    Argument = new Argument<bool>()
                },
                new Option(new[] { "--method-layout" }, SR.MethodLayoutOption)
                {
                    Argument = new Argument<ReadyToRunMethodLayoutAlgorithm>()
                },
                new Option(new[] { "--file-layout" }, SR.FileLayoutOption)
                {
                    Argument = new Argument<ReadyToRunFileLayoutAlgorithm>()
                },
                new Option(new[] { "--verify-type-and-field-layout" }, SR.VerifyTypeAndFieldLayoutOption)
                {
                    Argument = new Argument<bool>()
                }
            };
        }
    }
}
