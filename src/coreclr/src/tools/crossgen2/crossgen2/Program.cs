// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Internal.CommandLine;
using System.Linq;
using System.IO;

namespace ILCompiler
{
    internal class Program
    {
        private const string DefaultSystemModule = "System.Private.CoreLib";

        private CommandLineOptions _commandLineOptions;
        public TargetOS _targetOS;
        public TargetArchitecture _targetArchitecture;
        public OptimizationMode _optimizationMode;
        private Dictionary<string, string> _inputFilePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, string> _referenceFilePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private Program(CommandLineOptions commandLineOptions)
        {
            _commandLineOptions = commandLineOptions;
        }

        private void Help(string helpText)
        {
            Console.WriteLine();
            Console.Write("Microsoft (R) CoreCLR Native Image Generator");
            Console.Write(" ");
            Console.Write(typeof(Program).GetTypeInfo().Assembly.GetName().Version);
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine(helpText);
        }

        private void InitializeDefaultOptions()
        {
            // We could offer this as a command line option, but then we also need to
            // load a different RyuJIT, so this is a future nice to have...
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                _targetOS = TargetOS.Windows;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                _targetOS = TargetOS.Linux;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                _targetOS = TargetOS.OSX;
            else
                throw new NotImplementedException();

            switch (RuntimeInformation.ProcessArchitecture)
            {
                case Architecture.X86:
                    _targetArchitecture = TargetArchitecture.X86;
                    break;
                case Architecture.X64:
                    _targetArchitecture = TargetArchitecture.X64;
                    break;
                case Architecture.Arm:
                    _targetArchitecture = TargetArchitecture.ARM;
                    break;
                case Architecture.Arm64:
                    _targetArchitecture = TargetArchitecture.ARM64;
                    break;
                default:
                    throw new NotImplementedException();
            }

            // Workaround for https://github.com/dotnet/corefx/issues/25267
            // If pointer size is 8, we're obviously not an X86 process...
            if (_targetArchitecture == TargetArchitecture.X86 && IntPtr.Size == 8)
                _targetArchitecture = TargetArchitecture.X64;
        }

        private void ProcessCommandLine()
        {
            AssemblyName name = typeof(Program).GetTypeInfo().Assembly.GetName();

            if (_commandLineOptions.WaitForDebugger)
            {
                Console.WriteLine("Waiting for debugger to attach. Press ENTER to continue");
                Console.ReadLine();
            }

            if (_commandLineOptions.CompileBubbleGenerics)
            {
                if (!_commandLineOptions.InputBubble)
                {
                    Console.WriteLine("Warning: ignoring --compilebubblegenerics because --inputbubble was not specified");
                    _commandLineOptions.CompileBubbleGenerics = false;
                }
            }

            _optimizationMode = OptimizationMode.None;
            if (_commandLineOptions.OptimizeSpace)
            {
                if (_commandLineOptions.OptimizeTime)
                    Console.WriteLine("Warning: overriding -Ot with -Os");
                _optimizationMode = OptimizationMode.PreferSize;
            }
            else if (_commandLineOptions.OptimizeTime)
                _optimizationMode = OptimizationMode.PreferSpeed;
            else if (_commandLineOptions.Optimize)
                _optimizationMode = OptimizationMode.Blended;

            foreach (var input in _commandLineOptions.InputFilePaths ?? Enumerable.Empty<FileInfo>())
                Helpers.AppendExpandedPaths(_inputFilePaths, input.FullName, true);

            foreach (var reference in _commandLineOptions.Reference ?? Enumerable.Empty<string>())
                Helpers.AppendExpandedPaths(_referenceFilePaths, reference, false);
        }

        private int Run()
        {
            InitializeDefaultOptions();

            ProcessCommandLine();

            if (_commandLineOptions.OutputFilePath == null)
                throw new CommandLineException("Output filename must be specified (/out <file>)");

            //
            // Set target Architecture and OS
            //
            if (_commandLineOptions.TargetArch != null)
            {
                if (_commandLineOptions.TargetArch.Equals("x86", StringComparison.OrdinalIgnoreCase))
                    _targetArchitecture = TargetArchitecture.X86;
                else if (_commandLineOptions.TargetArch.Equals("x64", StringComparison.OrdinalIgnoreCase))
                    _targetArchitecture = TargetArchitecture.X64;
                else if (_commandLineOptions.TargetArch.Equals("arm", StringComparison.OrdinalIgnoreCase))
                    _targetArchitecture = TargetArchitecture.ARM;
                else if (_commandLineOptions.TargetArch.Equals("armel", StringComparison.OrdinalIgnoreCase))
                    _targetArchitecture = TargetArchitecture.ARM;
                else if (_commandLineOptions.TargetArch.Equals("arm64", StringComparison.OrdinalIgnoreCase))
                    _targetArchitecture = TargetArchitecture.ARM64;
                else
                    throw new CommandLineException("Target architecture is not supported");
            }
            if (_commandLineOptions.TargetOS != null)
            {
                if (_commandLineOptions.TargetOS.Equals("windows", StringComparison.OrdinalIgnoreCase))
                    _targetOS = TargetOS.Windows;
                else if (_commandLineOptions.TargetOS.Equals("linux", StringComparison.OrdinalIgnoreCase))
                    _targetOS = TargetOS.Linux;
                else if (_commandLineOptions.TargetOS.Equals("osx", StringComparison.OrdinalIgnoreCase))
                    _targetOS = TargetOS.OSX;
                else
                    throw new CommandLineException("Target OS is not supported");
            }

            using (PerfEventSource.StartStopEvents.CompilationEvents())
            {
                ICompilation compilation;
                using (PerfEventSource.StartStopEvents.LoadingEvents())
                {
                    //
                    // Initialize type system context
                    //

                    SharedGenericsMode genericsMode = SharedGenericsMode.CanonicalReferenceTypes;

                    var targetDetails = new TargetDetails(_targetArchitecture, _targetOS, TargetAbi.CoreRT, SimdVectorLength.None);
                    CompilerTypeSystemContext typeSystemContext = new ReadyToRunCompilerContext(targetDetails, genericsMode);

                    //
                    // TODO: To support our pre-compiled test tree, allow input files that aren't managed assemblies since
                    // some tests contain a mixture of both managed and native binaries.
                    //
                    // See: https://github.com/dotnet/corert/issues/2785
                    //
                    // When we undo this this hack, replace this foreach with
                    //  typeSystemContext.InputFilePaths = _inputFilePaths;
                    //
                    Dictionary<string, string> inputFilePaths = new Dictionary<string, string>();
                    foreach (var inputFile in _inputFilePaths)
                    {
                        try
                        {
                            var module = typeSystemContext.GetModuleFromPath(inputFile.Value);
                            inputFilePaths.Add(inputFile.Key, inputFile.Value);
                        }
                        catch (TypeSystemException.BadImageFormatException)
                        {
                            // Keep calm and carry on.
                        }
                    }

                    typeSystemContext.InputFilePaths = inputFilePaths;
                    typeSystemContext.ReferenceFilePaths = _referenceFilePaths;

                    string systemModuleName = _commandLineOptions.SystemModule ?? DefaultSystemModule;
                    typeSystemContext.SetSystemModule(typeSystemContext.GetModuleForSimpleName(systemModuleName));

                    if (typeSystemContext.InputFilePaths.Count == 0)
                        throw new CommandLineException("No input files specified");

                    //
                    // Initialize compilation group and compilation roots
                    //

                    // Single method mode?
                    MethodDesc singleMethod = CheckAndParseSingleMethodModeArguments(typeSystemContext);

                    var logger = new Logger(Console.Out, _commandLineOptions.Verbose);

                    List<ModuleDesc> referenceableModules = new List<ModuleDesc>();
                    foreach (var inputFile in inputFilePaths)
                    {
                        try
                        {
                            referenceableModules.Add(typeSystemContext.GetModuleFromPath(inputFile.Value));
                        }
                        catch { } // Ignore non-managed pe files
                    }

                    foreach (var referenceFile in _referenceFilePaths.Values)
                    {
                        try
                        {
                            referenceableModules.Add(typeSystemContext.GetModuleFromPath(referenceFile));
                        }
                        catch { } // Ignore non-managed pe files
                    }

                    ProfileDataManager profileDataManager = new ProfileDataManager(logger, referenceableModules);

                    CompilationModuleGroup compilationGroup;
                    List<ICompilationRootProvider> compilationRoots = new List<ICompilationRootProvider>();
                    if (singleMethod != null)
                    {
                        // Compiling just a single method
                        compilationGroup = new SingleMethodCompilationModuleGroup(singleMethod);
                        compilationRoots.Add(new SingleMethodRootProvider(singleMethod));
                    }
                    else
                    {
                        // Either single file, or multifile library, or multifile consumption.
                        EcmaModule entrypointModule = null;
                        foreach (var inputFile in typeSystemContext.InputFilePaths)
                        {
                            EcmaModule module = typeSystemContext.GetModuleFromPath(inputFile.Value);

                            if (module.PEReader.PEHeaders.IsExe)
                            {
                                if (entrypointModule != null)
                                    throw new Exception("Multiple EXE modules");
                                entrypointModule = module;
                            }
                        }

                        List<EcmaModule> inputModules = new List<EcmaModule>();

                        foreach (var inputFile in typeSystemContext.InputFilePaths)
                        {
                            EcmaModule module = typeSystemContext.GetModuleFromPath(inputFile.Value);
                            compilationRoots.Add(new ReadyToRunRootProvider(module, profileDataManager));
                            inputModules.Add(module);

                            if (!_commandLineOptions.InputBubble)
                            {
                                break;
                            }
                        }


                        List<ModuleDesc> versionBubbleModules = new List<ModuleDesc>();
                        if (_commandLineOptions.InputBubble)
                        {
                            // In large version bubble mode add reference paths to the compilation group
                            foreach (string referenceFile in _referenceFilePaths.Values)
                            {
                                try
                                {
                                    // Currently SimpleTest.targets has no easy way to filter out non-managed assemblies
                                    // from the reference list.
                                    EcmaModule module = typeSystemContext.GetModuleFromPath(referenceFile);
                                    versionBubbleModules.Add(module);
                                }
                                catch (TypeSystemException.BadImageFormatException ex)
                                {
                                    Console.WriteLine("Warning: cannot open reference assembly '{0}': {1}", referenceFile, ex.Message);
                                }
                            }
                        }

                        compilationGroup = new ReadyToRunSingleAssemblyCompilationModuleGroup(
                            typeSystemContext, inputModules, versionBubbleModules, _commandLineOptions.CompileBubbleGenerics,
                            _commandLineOptions.Partial ? profileDataManager : null);
                    }

                    //
                    // Compile
                    //

                    string inputFilePath = "";
                    foreach (var input in typeSystemContext.InputFilePaths)
                    {
                        inputFilePath = input.Value;
                        break;
                    }
                    CompilationBuilder builder = new ReadyToRunCodegenCompilationBuilder(typeSystemContext, compilationGroup, inputFilePath,
                        ibcTuning: _commandLineOptions.Tuning,
                        resilient: _commandLineOptions.Resilient);

                    string compilationUnitPrefix = "";
                    builder.UseCompilationUnitPrefix(compilationUnitPrefix);

                    ILProvider ilProvider = new ReadyToRunILProvider();

                    DependencyTrackingLevel trackingLevel = _commandLineOptions.DgmlLogFileName == null ?
                        DependencyTrackingLevel.None : (_commandLineOptions.GenerateFullDgmlLog ? DependencyTrackingLevel.All : DependencyTrackingLevel.First);

                    builder
                        .UseILProvider(ilProvider)
                        .UseJitPath(_commandLineOptions.JitPath)
                        .UseBackendOptions(_commandLineOptions.CodegenOptions)
                        .UseLogger(logger)
                        .UseDependencyTracking(trackingLevel)
                        .UseCompilationRoots(compilationRoots)
                        .UseOptimizationMode(_optimizationMode);

                    compilation = builder.ToCompilation();

                }
                compilation.Compile(_commandLineOptions.OutputFilePath.FullName);

                if (_commandLineOptions.DgmlLogFileName != null)
                    compilation.WriteDependencyLog(_commandLineOptions.DgmlLogFileName.FullName);
            }

            return 0;
        }

        private TypeDesc FindType(CompilerTypeSystemContext context, string typeName)
        {
            ModuleDesc systemModule = context.SystemModule;

            TypeDesc foundType = systemModule.GetTypeByCustomAttributeTypeName(typeName, false, (typeDefName, module, throwIfNotFound) =>
            {
                return (MetadataType)context.GetCanonType(typeDefName)
                    ?? CustomAttributeTypeNameParser.ResolveCustomAttributeTypeDefinitionName(typeDefName, module, throwIfNotFound);
            });
            if (foundType == null)
                throw new CommandLineException($"Type '{typeName}' not found");

            return foundType;
        }

        private MethodDesc CheckAndParseSingleMethodModeArguments(CompilerTypeSystemContext context)
        {
            if (_commandLineOptions.SingleMethodName == null && _commandLineOptions.SingleMethodTypeName == null && _commandLineOptions.SingleMethodGenericArgs == null)
                return null;

            if (_commandLineOptions.SingleMethodName == null || _commandLineOptions.SingleMethodTypeName == null)
                throw new CommandLineException("Both method name and type name are required parameters for single method mode");

            TypeDesc owningType = FindType(context, _commandLineOptions.SingleMethodTypeName);

            // TODO: allow specifying signature to distinguish overloads
            MethodDesc method = owningType.GetMethod(_commandLineOptions.SingleMethodName, null);
            if (method == null)
                throw new CommandLineException($"Method '{_commandLineOptions.SingleMethodName}' not found in '{_commandLineOptions.SingleMethodTypeName}'");

            if (method.HasInstantiation != (_commandLineOptions.SingleMethodGenericArgs != null) ||
                (method.HasInstantiation && (method.Instantiation.Length != _commandLineOptions.SingleMethodGenericArgs.Length)))
            {
                throw new CommandLineException(
                    $"Expected {method.Instantiation.Length} generic arguments for method '{_commandLineOptions.SingleMethodName}' on type '{_commandLineOptions.SingleMethodTypeName}'");
            }

            if (method.HasInstantiation)
            {
                List<TypeDesc> genericArguments = new List<TypeDesc>();
                foreach (var argString in _commandLineOptions.SingleMethodGenericArgs)
                    genericArguments.Add(FindType(context, argString));
                method = method.MakeInstantiatedMethod(genericArguments.ToArray());
            }

            return method;
        }

        private static bool DumpReproArguments(CodeGenerationFailedException ex)
        {
            Console.WriteLine("To repro, add following arguments to the command line:");

            MethodDesc failingMethod = ex.Method;

            var formatter = new CustomAttributeTypeNameFormatter((IAssemblyDesc)failingMethod.Context.SystemModule);

            Console.Write($"--singlemethodtypename \"{formatter.FormatName(failingMethod.OwningType, true)}\"");
            Console.Write($" --singlemethodname {failingMethod.Name}");

            for (int i = 0; i < failingMethod.Instantiation.Length; i++)
                Console.Write($" --singlemethodgenericarg \"{formatter.FormatName(failingMethod.Instantiation[i], true)}\"");

            return false;
        }

        public static async Task<int> Main(string[] args)
        {
            var command = CommandLineOptions.RootCommand();
            command.Handler = CommandHandler.Create<CommandLineOptions>((CommandLineOptions options) => InnerMain(options));
            return await command.InvokeAsync(args);
        }

        private static int InnerMain(CommandLineOptions buildOptions)
        {
#if DEBUG
            try
            {
                return new Program(buildOptions).Run();
            }
            catch (CodeGenerationFailedException ex) when (DumpReproArguments(ex))
            {
                throw new NotSupportedException(); // Unreachable
            }
#else
            try
            {
                return new Program(buildOptions).Run();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error: " + e.Message);
                Console.Error.WriteLine(e.ToString());
                return 1;
            }
#endif
        }
    }
}
