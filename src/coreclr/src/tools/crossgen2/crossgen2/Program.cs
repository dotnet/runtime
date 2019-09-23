// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.CommandLine;
using System.Runtime.InteropServices;

using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Internal.CommandLine;

namespace ILCompiler
{
    internal class Program
    {
        private const string DefaultSystemModule = "System.Private.CoreLib";

        private Dictionary<string, string> _inputFilePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, string> _referenceFilePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private string _outputFilePath;
        private bool _isInputVersionBubble;
        private bool _includeGenericsFromVersionBubble;
        private bool _isVerbose;

        private string _dgmlLogFileName;
        private bool _generateFullDgmlLog;

        private TargetArchitecture _targetArchitecture;
        private string _targetArchitectureStr;
        private TargetOS _targetOS;
        private string _targetOSStr;
        private OptimizationMode _optimizationMode;
        private string _systemModuleName = DefaultSystemModule;
        private bool _tuning;
        private bool _partial;
        private bool _resilient;

        private string _singleMethodTypeName;
        private string _singleMethodName;
        private IReadOnlyList<string> _singleMethodGenericArgs;

        private IReadOnlyList<string> _codegenOptions = Array.Empty<string>();

        private bool _help;

        private Program()
        {
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

        private ArgumentSyntax ParseCommandLine(string[] args)
        {
            IReadOnlyList<string> inputFiles = Array.Empty<string>();
            IReadOnlyList<string> referenceFiles = Array.Empty<string>();

            bool optimize = false;
            bool optimizeSpace = false;
            bool optimizeTime = false;

            bool waitForDebugger = false;
            AssemblyName name = typeof(Program).GetTypeInfo().Assembly.GetName();
            ArgumentSyntax argSyntax = ArgumentSyntax.Parse(args, syntax =>
            {
                syntax.ApplicationName = name.Name.ToString();

                // HandleHelp writes to error, fails fast with crash dialog and lacks custom formatting.
                syntax.HandleHelp = false;
                syntax.HandleErrors = true;

                syntax.DefineOption("h|help", ref _help, "Help message for ILC");
                syntax.DefineOptionList("r|reference", ref referenceFiles, "Reference file(s) for compilation");
                syntax.DefineOption("o|out", ref _outputFilePath, "Output file path");
                syntax.DefineOption("O", ref optimize, "Enable optimizations");
                syntax.DefineOption("Os", ref optimizeSpace, "Enable optimizations, favor code space");
                syntax.DefineOption("Ot", ref optimizeTime, "Enable optimizations, favor code speed");
                syntax.DefineOption("inputbubble", ref _isInputVersionBubble, "True when the entire input forms a version bubble (default = per-assembly bubble)");
                syntax.DefineOption("tuning", ref _tuning, "Generate IBC tuning image");
                syntax.DefineOption("partial", ref _partial, "Generate partial image driven by profile");
                syntax.DefineOption("compilebubblegenerics", ref _includeGenericsFromVersionBubble, "Compile instantiations from reference modules used in the current module");
                syntax.DefineOption("dgmllog", ref _dgmlLogFileName, "Save result of dependency analysis as DGML");
                syntax.DefineOption("fulllog", ref _generateFullDgmlLog, "Save detailed log of dependency analysis");
                syntax.DefineOption("verbose", ref _isVerbose, "Enable verbose logging");
                syntax.DefineOption("systemmodule", ref _systemModuleName, "System module name (default: System.Private.CoreLib)");
                syntax.DefineOption("waitfordebugger", ref waitForDebugger, "Pause to give opportunity to attach debugger");
                syntax.DefineOptionList("codegenopt", ref _codegenOptions, "Define a codegen option");
                syntax.DefineOption("resilient", ref _resilient, "Disable behavior where unexpected compilation failures cause overall compilation failure");

                syntax.DefineOption("targetarch", ref _targetArchitectureStr, "Target architecture for cross compilation");
                syntax.DefineOption("targetos", ref _targetOSStr, "Target OS for cross compilation");

                syntax.DefineOption("singlemethodtypename", ref _singleMethodTypeName, "Single method compilation: name of the owning type");
                syntax.DefineOption("singlemethodname", ref _singleMethodName, "Single method compilation: name of the method");
                syntax.DefineOptionList("singlemethodgenericarg", ref _singleMethodGenericArgs, "Single method compilation: generic arguments to the method");

                syntax.DefineParameterList("in", ref inputFiles, "Input file(s) to compile");
            });
            if (waitForDebugger)
            {
                Console.WriteLine("Waiting for debugger to attach. Press ENTER to continue");
                Console.ReadLine();
            }

            if (_includeGenericsFromVersionBubble)
            {
                if (!_isInputVersionBubble)
                {
                    Console.WriteLine("Warning: ignoring --compilebubblegenerics because --inputbubble was not specified");
                    _includeGenericsFromVersionBubble = false;
                }
            }

            _optimizationMode = OptimizationMode.None;
            if (optimizeSpace)
            {
                if (optimizeTime)
                    Console.WriteLine("Warning: overriding -Ot with -Os");
                _optimizationMode = OptimizationMode.PreferSize;
            }
            else if (optimizeTime)
                _optimizationMode = OptimizationMode.PreferSpeed;
            else if (optimize)
                _optimizationMode = OptimizationMode.Blended;

            foreach (var input in inputFiles)
                Helpers.AppendExpandedPaths(_inputFilePaths, input, true);

            foreach (var reference in referenceFiles)
                Helpers.AppendExpandedPaths(_referenceFilePaths, reference, false);

            return argSyntax;
        }

        private int Run(string[] args)
        {
            InitializeDefaultOptions();

            ArgumentSyntax syntax = ParseCommandLine(args);
            if (_help)
            {
                Help(syntax.GetHelpText());
                return 1;
            }

            if (_outputFilePath == null)
                throw new CommandLineException("Output filename must be specified (/out <file>)");

            //
            // Set target Architecture and OS
            //
            if (_targetArchitectureStr != null)
            {
                if (_targetArchitectureStr.Equals("x86", StringComparison.OrdinalIgnoreCase))
                    _targetArchitecture = TargetArchitecture.X86;
                else if (_targetArchitectureStr.Equals("x64", StringComparison.OrdinalIgnoreCase))
                    _targetArchitecture = TargetArchitecture.X64;
                else if (_targetArchitectureStr.Equals("arm", StringComparison.OrdinalIgnoreCase))
                    _targetArchitecture = TargetArchitecture.ARM;
                else if (_targetArchitectureStr.Equals("armel", StringComparison.OrdinalIgnoreCase))
                    _targetArchitecture = TargetArchitecture.ARM;
                else if (_targetArchitectureStr.Equals("arm64", StringComparison.OrdinalIgnoreCase))
                    _targetArchitecture = TargetArchitecture.ARM64;
                else
                    throw new CommandLineException("Target architecture is not supported");
            }
            if (_targetOSStr != null)
            {
                if (_targetOSStr.Equals("windows", StringComparison.OrdinalIgnoreCase))
                    _targetOS = TargetOS.Windows;
                else if (_targetOSStr.Equals("linux", StringComparison.OrdinalIgnoreCase))
                    _targetOS = TargetOS.Linux;
                else if (_targetOSStr.Equals("osx", StringComparison.OrdinalIgnoreCase))
                    _targetOS = TargetOS.OSX;
                else
                    throw new CommandLineException("Target OS is not supported");
            }

            PerfEventSource.Log.CompilationStart();
            PerfEventSource.Log.LoadingStart();
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

            typeSystemContext.SetSystemModule(typeSystemContext.GetModuleForSimpleName(_systemModuleName));

            if (typeSystemContext.InputFilePaths.Count == 0)
                throw new CommandLineException("No input files specified");

            //
            // Initialize compilation group and compilation roots
            //

            // Single method mode?
            MethodDesc singleMethod = CheckAndParseSingleMethodModeArguments(typeSystemContext);

            var logger = new Logger(Console.Out, _isVerbose);

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

                    if (!_isInputVersionBubble)
                    {
                        break;
                    }
                }


                List<ModuleDesc> versionBubbleModules = new List<ModuleDesc>();
                if (_isInputVersionBubble)
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
                    typeSystemContext, inputModules, versionBubbleModules, _includeGenericsFromVersionBubble, 
                    _partial ? profileDataManager : null);
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
                ibcTuning: _tuning,
                resilient: _resilient);

            string compilationUnitPrefix = "";
            builder.UseCompilationUnitPrefix(compilationUnitPrefix);

            ILProvider ilProvider = new ReadyToRunILProvider();


            DependencyTrackingLevel trackingLevel = _dgmlLogFileName == null ?
                DependencyTrackingLevel.None : (_generateFullDgmlLog ? DependencyTrackingLevel.All : DependencyTrackingLevel.First);

            builder
                .UseILProvider(ilProvider)
                .UseBackendOptions(_codegenOptions)
                .UseLogger(logger)
                .UseDependencyTracking(trackingLevel)
                .UseCompilationRoots(compilationRoots)
                .UseOptimizationMode(_optimizationMode);

            ICompilation compilation = builder.ToCompilation();

            PerfEventSource.Log.LoadingStop();
            compilation.Compile(_outputFilePath);
            PerfEventSource.Log.CompilationStop();

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
            if (_singleMethodName == null && _singleMethodTypeName == null && _singleMethodGenericArgs == null)
                return null;

            if (_singleMethodName == null || _singleMethodTypeName == null)
                throw new CommandLineException("Both method name and type name are required parameters for single method mode");

            TypeDesc owningType = FindType(context, _singleMethodTypeName);

            // TODO: allow specifying signature to distinguish overloads
            MethodDesc method = owningType.GetMethod(_singleMethodName, null);
            if (method == null)
                throw new CommandLineException($"Method '{_singleMethodName}' not found in '{_singleMethodTypeName}'");

            if (method.HasInstantiation != (_singleMethodGenericArgs != null) ||
                (method.HasInstantiation && (method.Instantiation.Length != _singleMethodGenericArgs.Count)))
            {
                throw new CommandLineException(
                    $"Expected {method.Instantiation.Length} generic arguments for method '{_singleMethodName}' on type '{_singleMethodTypeName}'");
            }

            if (method.HasInstantiation)
            {
                List<TypeDesc> genericArguments = new List<TypeDesc>();
                foreach (var argString in _singleMethodGenericArgs)
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

        private static int Main(string[] args)
        {
#if DEBUG
            try
            {
                return new Program().Run(args);
            }
            catch (CodeGenerationFailedException ex) when (DumpReproArguments(ex))
            {
                throw new NotSupportedException(); // Unreachable
            }
#else
            try
            {
                return new Program().Run(args);
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
