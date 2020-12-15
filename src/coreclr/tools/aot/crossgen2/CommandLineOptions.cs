// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using Internal.CommandLine;

namespace ILCompiler
{
    internal class CommandLineOptions
    {
        public bool Help;
        public string HelpText;

        public IReadOnlyList<string> InputFilePaths;
        public IReadOnlyList<string> UnrootedInputFilePaths;
        public IReadOnlyList<string> ReferenceFilePaths;
        public IReadOnlyList<string> MibcFilePaths;
        public string InstructionSet;
        public string OutputFilePath;

        public string CompositeRootPath;
        public bool Optimize;
        public bool OptimizeSpace;
        public bool OptimizeTime;
        public bool InputBubble;
        public bool CompileBubbleGenerics;
        public bool Verbose;
        public bool Composite;
        public bool CompileNoMethods;

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
        public int Parallelism;
        public int CustomPESectionAlignment;
        public string MethodLayout;
        public string FileLayout;
        public bool VerifyTypeAndFieldLayout;

        public string SingleMethodTypeName;
        public string SingleMethodName;
        public int SingleMethodIndex;
        public IReadOnlyList<string> SingleMethodGenericArg;

        public IReadOnlyList<string> CodegenOptions;

        public bool CompositeOrInputBubble => Composite || InputBubble;

        public CommandLineOptions(string[] args)
        {
            InputFilePaths = Array.Empty<string>();
            UnrootedInputFilePaths = Array.Empty<string>();
            ReferenceFilePaths = Array.Empty<string>();
            MibcFilePaths = Array.Empty<string>();
            CodegenOptions = Array.Empty<string>();

            Parallelism = Environment.ProcessorCount;
            SingleMethodGenericArg = null;

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
                syntax.DefineOption("Os|optimize-space", ref OptimizeSpace, SR.OptimizeSpaceOption);
                syntax.DefineOption("Ot|optimize-time", ref OptimizeTime, SR.OptimizeSpeedOption);
                syntax.DefineOption("inputbubble", ref InputBubble, SR.InputBubbleOption);
                syntax.DefineOption("composite", ref Composite, SR.CompositeBuildMode);
                syntax.DefineOption("compile-no-methods", ref CompileNoMethods, SR.CompileNoMethodsOption);
                syntax.DefineOption("tuning", ref Tuning, SR.TuningImageOption);
                syntax.DefineOption("partial", ref Partial, SR.PartialImageOption);
                syntax.DefineOption("compilebubblegenerics", ref CompileBubbleGenerics, SR.BubbleGenericsOption);
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

                syntax.DefineOption("singlemethodtypename", ref SingleMethodTypeName, SR.SingleMethodTypeName);
                syntax.DefineOption("singlemethodname", ref SingleMethodName, SR.SingleMethodMethodName);
                syntax.DefineOption("singlemethodindex", ref SingleMethodIndex, SR.SingleMethodIndex);
                syntax.DefineOptionList("singlemethodgenericarg", ref SingleMethodGenericArg, SR.SingleMethodGenericArgs);

                syntax.DefineOption("parallelism", ref Parallelism, SR.ParalellismOption);
                syntax.DefineOption("custom-pe-section-alignment", ref CustomPESectionAlignment, SR.CustomPESectionAlignmentOption);
                syntax.DefineOption("map", ref Map, SR.MapFileOption);
                syntax.DefineOption("mapcsv", ref MapCsv, SR.MapCsvFileOption);

                syntax.DefineOption("method-layout", ref MethodLayout, SR.MethodLayoutOption);
                syntax.DefineOption("file-layout", ref FileLayout, SR.FileLayoutOption);
                syntax.DefineOption("verify-type-and-field-layout", ref VerifyTypeAndFieldLayout, SR.VerifyTypeAndFieldLayoutOption);

                syntax.DefineOption("h|help", ref Help, SR.HelpOption);

                syntax.DefineParameterList("in", ref InputFilePaths, SR.InputFilesToCompile);
            });

            if (Help)
            {
                HelpText = argSyntax.GetHelpText();
            }
        }
    }
}
