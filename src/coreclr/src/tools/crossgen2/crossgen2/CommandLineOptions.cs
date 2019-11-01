// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.CommandLine;
using System.IO;

namespace ILCompiler
{
    public class CommandLineOptions
    {
        public FileInfo[] InputFilePaths { get; set; }
        public string[] Reference { get; set; }
        public FileInfo OutputFilePath { get; set; }
        public bool Optimize { get; set; }
        public bool OptimizeSpace { get; set; }
        public bool OptimizeTime { get; set; }
        public bool InputBubble { get; set; }
        public bool CompileBubbleGenerics { get; set; }
        public bool Verbose { get; set; }

        public FileInfo DgmlLogFileName { get; set; }
        public bool GenerateFullDgmlLog { get; set; }

        public string TargetArch { get; set; }
        public string TargetOS { get; set; }
        public string JitPath { get; set; }
        public string SystemModule { get; set; }
        public bool WaitForDebugger { get; set; }
        public bool Tuning { get; set; }
        public bool Partial { get; set; }
        public bool Resilient { get; set; }

        public string SingleMethodTypeName { get; set; }
        public string SingleMethodName { get; set; }
        public string[] SingleMethodGenericArgs { get; set; }

        public string[] CodegenOptions { get; set; }

        public static Command RootCommand()
        {
            // For some reason, arity caps at 255 by default
            ArgumentArity arbitraryArity = new ArgumentArity(0, 100000);

            return new Command("Crossgen2Compilation")
            {
                new Argument<FileInfo[]>() 
                { 
                    Name = "input-file-paths", 
                    Description = "Input file(s) to compile",
                    Arity = arbitraryArity,
                },
                new Option(new[] { "--reference", "-r" }, "Reference file(s) for compilation")
                { 
                    Argument = new Argument<string[]>() 
                    { 
                        Arity = arbitraryArity
                    } 
                },
                new Option(new[] { "--outputfilepath", "--out", "-o" }, "Output file path")
                {
                    Argument = new Argument<FileInfo>()
                },
                new Option(new[] { "--optimize", "-O" }, "Enable optimizations") 
                { 
                    Argument = new Argument<bool>() 
                },
                new Option(new[] { "--optimize-space", "--Os" }, "Enable optimizations, favor code space") 
                { 
                    Argument = new Argument<bool>() 
                },
                new Option(new[] { "--optimize-time", "--Ot" }, "Enable optimizations, favor code speed"),
                new Option(new[] { "--inputbubble" }, "True when the entire input forms a version bubble (default = per-assembly bubble)"),
                new Option(new[] { "--tuning" }, "Generate IBC tuning image") 
                {
                    Argument = new Argument<bool>() 
                },
                new Option(new[] { "--partial" }, "Generate partial image driven by profile") 
                { 
                    Argument = new Argument<bool>() 
                },
                new Option(new[] { "--compilebubblegenerics" }, "Compile instantiations from reference modules used in the current module") 
                { 
                    Argument = new Argument<bool>() 
                },
                new Option(new[] { "--dgml-log-file-name", "--dmgllog" }, "Save result of dependency analysis as DGML") 
                { 
                    Argument = new Argument<FileInfo>() 
                },
                new Option(new[] { "--generate-full-dmgl-log", "--fulllog" }, "Save detailed log of dependency analysis") 
                { 
                    Argument = new Argument<bool>() 
                },
                new Option(new[] { "--verbose" }, "Enable verbose logging") 
                { 
                    Argument = new Argument<bool>() 
                },
                new Option(new[] { "--systemmodule" }, "System module name (default: System.Private.CoreLib)") 
                { 
                    Argument = new Argument<string>() 
                },
                new Option(new[] { "--waitfordebugger" }, "Pause to give opportunity to attach debugger") 
                { 
                    Argument = new Argument<bool>() 
                },
                new Option(new[] { "--codegen-options", "--codegenopt" }, "Define a codegen option") 
                { 
                    Argument = new Argument<string[]>()
                    {
                        Arity = arbitraryArity
                    }
                },
                new Option(new[] { "--resilient" }, "Disable behavior where unexpected compilation failures cause overall compilation failure") 
                { 
                    Argument = new Argument<bool>() 
                },
                new Option(new[] { "--targetarch" }, "Target architecture for cross compilation") 
                { 
                    Argument = new Argument<string>() 
                },
                new Option(new[] { "--targetos" }, "Target OS for cross compilation") 
                { 
                    Argument = new Argument<string>() 
                },
                new Option(new[] { "--jitpath" }, "Path to JIT compiler library") 
                { 
                    Argument =  new Argument<string>() 
                },
                new Option(new[] { "--singlemethodtypename" }, "Single method compilation: name of the owning type") 
                { 
                    Argument = new Argument<string>() 
                },
                new Option(new[] { "--singlemethodname" }, "Single method compilation: generic arguments to the method") 
                { 
                    Argument = new Argument<string>() 
                },
                new Option(new[] { "--singlemethodgenericarg" }, "Single method compilation: generic arguments to the method") 
                { 
                    // We don't need to override arity here as 255 is the maximum number of generic arguments
                    Argument = new Argument<string[]>()
                },
            };
        }
    }
}
