// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Internal.CommandLine;
using Internal.TypeSystem.Ecma;
using static System.Console;

namespace ILVerify
{
    class Program : IResolver
    {
        private readonly Dictionary<string, PEReader> _resolverCache = new Dictionary<string, PEReader>();

        private Options _options;
        private Dictionary<string, string> _inputFilePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // map of simple name to file path
        private Dictionary<string, string> _referenceFilePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // map of simple name to file path
        private IReadOnlyList<Regex> _includePatterns;
        private IReadOnlyList<Regex> _excludePatterns;
        private IReadOnlyList<Regex> _ignoreErrorPatterns;

        private Verifier _verifier;

        public static IReadOnlyList<Regex> StringPatternsToRegexList(IReadOnlyList<string> patterns)
        {
            List<Regex> patternList = new List<Regex>();
            if (patterns != null)
            {
                foreach (var pattern in patterns)
                    patternList.Add(new Regex(pattern, RegexOptions.Compiled));
            }
            return patternList;
        }

        private Program(Options options)
        {
            _options = options;

            if (options.InputFilePath != null)
            {
                foreach (var input in options.InputFilePath)
                    Helpers.AppendExpandedPaths(_inputFilePaths, input, true);
            }

            if (options.Reference != null)
            {
                foreach (var reference in options.Reference)
                    Helpers.AppendExpandedPaths(_referenceFilePaths, reference, false);
            }

            string[] includePatterns = options.Include;
            if (options.IncludeFile != null)
            {
                if (options.Include != null && options.Include.Length != 0)
                    WriteLine("[Warning] --include-file takes precedence over --include");
                includePatterns = File.ReadAllLines(options.IncludeFile.FullName);
            }
            _includePatterns = StringPatternsToRegexList(includePatterns);

            string[] excludePatterns = options.Exclude;
            if (options.ExcludeFile != null)
            {
                if (options.Exclude != null && options.Exclude.Length != 0)
                    WriteLine("[Warning] --exclude-file takes precedence over --exclude");
                excludePatterns = File.ReadAllLines(options.ExcludeFile.FullName);
            }
            _excludePatterns = StringPatternsToRegexList(excludePatterns);

            string[] ignoreErrorPatterns = options.IgnoreError;
            if (options.IgnoreErrorFile != null)
            {
                if (options.IgnoreError != null && options.IgnoreError.Length != 0)
                    WriteLine("[Warning] --ignore-error-file takes precedence over --ignore-error");
                ignoreErrorPatterns = File.ReadAllLines(options.IgnoreErrorFile.FullName);
            }
            _ignoreErrorPatterns = StringPatternsToRegexList(ignoreErrorPatterns);

            if (options.Verbose)
            {
                WriteLine();
                foreach (var path in _inputFilePaths)
                    WriteLine($"Using input file '{path.Value}'");

                WriteLine();
                foreach (var path in _referenceFilePaths)
                    WriteLine($"Using reference file '{path.Value}'");

                WriteLine();
                foreach (var pattern in _includePatterns)
                    WriteLine($"Using include pattern '{pattern}'");

                WriteLine();
                foreach (var pattern in _excludePatterns)
                    WriteLine($"Using exclude pattern '{pattern}'");

                WriteLine();
                foreach (var pattern in _ignoreErrorPatterns)
                    WriteLine($"Using ignore error pattern '{pattern}'");
            }
        }

        private int Run()
        {
            _verifier = new Verifier(this, new VerifierOptions
            {
                IncludeMetadataTokensInErrorMessages = _options.Tokens,
                SanityChecks = _options.SanityChecks
            });
            _verifier.SetSystemModuleName(new AssemblyName(_options.SystemModule ?? "mscorlib"));

            int numErrors = 0;

            foreach (var kvp in _inputFilePaths)
            {
                numErrors += VerifyAssembly(new AssemblyName(kvp.Key), kvp.Value);
            }

            if (numErrors > 0)
            {
                return 2;
            }
            else
            {
                return 0;
            }
        }

        private void PrintVerifyMethodsResult(VerificationResult result, EcmaModule module, string pathOrModuleName)
        {
            Write("[IL]: Error [");
            if (result.Code != VerifierError.None)
            {
                Write(result.Code);
            }
            else
            {
                Write(result.ExceptionID);
            }
            Write("]: ");

            Write("[");
            Write(pathOrModuleName);
            Write(" : ");

            MetadataReader metadataReader = module.MetadataReader;

            TypeDefinition typeDef = metadataReader.GetTypeDefinition(metadataReader.GetMethodDefinition(result.Method).GetDeclaringType());
            string typeNamespace = metadataReader.GetString(typeDef.Namespace);
            Write(typeNamespace);
            Write(".");
            string typeName = metadataReader.GetString(typeDef.Name);
            Write(typeName);

            Write("::");
            var method = (EcmaMethod)module.GetMethod(result.Method);
            PrintMethod(method);
            Write("]");

            if (result.Code != VerifierError.None)
            {
                Write("[offset 0x");
                Write(result.GetArgumentValue<int>("Offset").ToString("X8"));
                Write("]");

                if (result.TryGetArgumentValue("Found", out string found))
                {
                    Write("[found ");
                    Write(found);
                    Write("]");
                }

                if (result.TryGetArgumentValue("Expected", out string expected))
                {
                    Write("[expected ");
                    Write(expected);
                    Write("]");
                }

                if (result.TryGetArgumentValue("Token", out int token))
                {
                    Write("[token  0x");
                    Write(token.ToString("X8"));
                    Write("]");
                }
            }

            Write(" ");
            WriteLine(result.Message);
        }

        private static void PrintMethod(EcmaMethod method)
        {
            Write(method.Name);
            Write("(");
            try
            {
                if (method.Signature.Length > 0)
                {
                    bool first = true;
                    for (int i = 0; i < method.Signature.Length; i++)
                    {
                        Internal.TypeSystem.TypeDesc parameter = method.Signature[i];
                        if (first)
                        {
                            first = false;
                        }
                        else
                        {
                            Write(", ");
                        }

                        Write(parameter.ToString());
                    }
                }
            }
            catch
            {
                Write("Error while getting method signature");
            }
            Write(")");
        }

        private int VerifyAssembly(AssemblyName name, string path)
        {
            PEReader peReader = Resolve(name.Name);
            EcmaModule module = _verifier.GetModule(peReader);

            return VerifyAssembly(peReader, module, path);
        }

        private int VerifyAssembly(PEReader peReader, EcmaModule module, string path)
        {
            int numErrors = 0;
            int verifiedMethodCounter = 0;
            int methodCounter = 0;
            int verifiedTypeCounter = 0;
            int typeCounter = 0;

            VerifyMethods(peReader, module, path, ref numErrors, ref verifiedMethodCounter, ref methodCounter);
            VerifyTypes(peReader, module, path, ref numErrors, ref verifiedTypeCounter, ref typeCounter);

            if (numErrors > 0)
                WriteLine(numErrors + " Error(s) Verifying " + path);
            else
                WriteLine("All Classes and Methods in " + path + " Verified.");

            if (_options.Statistics)
            {
                WriteLine($"Types found: {typeCounter}");
                WriteLine($"Types verified: {verifiedTypeCounter}");

                WriteLine($"Methods found: {methodCounter}");
                WriteLine($"Methods verified: {verifiedMethodCounter}");
            }

            return numErrors;
        }

        private void VerifyMethods(PEReader peReader, EcmaModule module, string path, ref int numErrors, ref int verifiedMethodCounter, ref int methodCounter)
        {
            numErrors = 0;
            verifiedMethodCounter = 0;
            methodCounter = 0;

            MetadataReader metadataReader = peReader.GetMetadataReader();
            foreach (var methodHandle in metadataReader.MethodDefinitions)
            {
                // get fully qualified method name
                var methodName = GetQualifiedMethodName(metadataReader, methodHandle);

                bool verifying = ShouldVerifyMemberName(methodName);
                if (_options.Verbose)
                {
                    Write(verifying ? "Verifying " : "Skipping ");
                    WriteLine(methodName);
                }

                if (verifying)
                {
                    var results = _verifier.Verify(peReader, methodHandle);
                    foreach (var result in results)
                    {
                        if (ShouldIgnoreVerificationResult(result))
                        {
                            if (_options.Verbose)
                            {
                                Write("Ignoring ");
                                PrintVerifyMethodsResult(result, module, path);
                            }
                        }
                        else
                        {
                            PrintVerifyMethodsResult(result, module, path);
                            numErrors++;
                        }
                    }

                    verifiedMethodCounter++;
                }

                methodCounter++;
            }
        }

        private void VerifyTypes(PEReader peReader, EcmaModule module, string path, ref int numErrors, ref int verifiedTypeCounter, ref int typeCounter)
        {
            MetadataReader metadataReader = peReader.GetMetadataReader();

            foreach (TypeDefinitionHandle typeHandle in metadataReader.TypeDefinitions)
            {
                // get fully qualified type name
                var className = GetQualifiedClassName(metadataReader, typeHandle);
                bool verifying = ShouldVerifyMemberName(className);
                if (_options.Verbose)
                {
                    Write(verifying ? "Verifying " : "Skipping ");
                    WriteLine(className);
                }
                if (verifying)
                {
                    var results = _verifier.Verify(peReader, typeHandle);
                    foreach (VerificationResult result in results)
                    {
                        if (ShouldIgnoreVerificationResult(result))
                        {
                            if (_options.Verbose)
                            {
                                Write("Ignoring ");
                                Console.WriteLine(result.Message, result.Args);
                            }
                        }
                        else
                        {
                            Console.WriteLine(result.Message, result.Args);
                            numErrors++;
                        }
                    }

                    typeCounter++;
                }

                verifiedTypeCounter++;
            }
        }

        /// <summary>
        /// This method returns the fully qualified class name.
        /// </summary>
        private string GetQualifiedClassName(MetadataReader metadataReader, TypeDefinitionHandle typeHandle)
        {
            var typeDef = metadataReader.GetTypeDefinition(typeHandle);
            var typeName = metadataReader.GetString(typeDef.Name);

            var namespaceName = metadataReader.GetString(typeDef.Namespace);
            var assemblyName = metadataReader.GetString(metadataReader.IsAssembly ? metadataReader.GetAssemblyDefinition().Name : metadataReader.GetModuleDefinition().Name);

            StringBuilder builder = new StringBuilder();
            builder.Append($"[{assemblyName}]");
            if (!string.IsNullOrEmpty(namespaceName))
                builder.Append($"{namespaceName}.");
            builder.Append($"{typeName}");

            return builder.ToString();
        }

        /// <summary>
        /// This method returns the fully qualified method name by concatenating assembly, type and method name.
        /// This method exists to avoid additional assembly resolving, which might be triggered by calling
        /// MethodDesc.ToString().
        /// </summary>
        private string GetQualifiedMethodName(MetadataReader metadataReader, MethodDefinitionHandle methodHandle)
        {
            var methodDef = metadataReader.GetMethodDefinition(methodHandle);
            var typeDef = metadataReader.GetTypeDefinition(methodDef.GetDeclaringType());

            var methodName = metadataReader.GetString(metadataReader.GetMethodDefinition(methodHandle).Name);
            var typeName = metadataReader.GetString(typeDef.Name);
            var namespaceName = metadataReader.GetString(typeDef.Namespace);
            var assemblyName = metadataReader.GetString(metadataReader.IsAssembly ? metadataReader.GetAssemblyDefinition().Name : metadataReader.GetModuleDefinition().Name);

            StringBuilder builder = new StringBuilder();
            builder.Append($"[{assemblyName}]");
            if (!string.IsNullOrEmpty(namespaceName))
                builder.Append($"{namespaceName}.");
            builder.Append($"{typeName}.{methodName}");

            return builder.ToString();
        }

        private bool ShouldVerifyMemberName(string memberName)
        {
            if (_includePatterns.Count > 0 && !_includePatterns.Any(p => p.IsMatch(memberName)))
            {
                return false;
            }

            if (_excludePatterns.Any(p => p.IsMatch(memberName)))
            {
                return false;
            }

            return true;
        }

        private bool ShouldIgnoreVerificationResult(VerificationResult result)
        {
            var error = result.Code.ToStringInvariant();
            if (result.Code == VerifierError.None && result.ExceptionID != null)
            {
                error = result.ExceptionID?.ToStringInvariant();
            }

            if (_ignoreErrorPatterns.Any(p => p.IsMatch(error)))
            {
                return true;
            }

            return false;
        }

        PEReader IResolver.ResolveAssembly(AssemblyName assemblyName)
            => Resolve(assemblyName.Name);

        PEReader IResolver.ResolveModule(AssemblyName referencingModule, string fileName)
            => Resolve(Path.GetFileNameWithoutExtension(fileName));

        public PEReader Resolve(string simpleName)
        {
            if (_resolverCache.TryGetValue(simpleName, out PEReader peReader))
            {
                return peReader;
            }

            string path = null;
            if (_inputFilePaths.TryGetValue(simpleName, out path) || _referenceFilePaths.TryGetValue(simpleName, out path))
            {
                PEReader result = new PEReader(File.OpenRead(path));
                _resolverCache.Add(simpleName, result);
                return result;
            }

            return null;
        }

        //
        // Command line parsing
        //

        private class ILVerifyRootCommand : RootCommand
        {
            public Argument<string[]> InputFilePath { get; } =
                new("input-file-path", "Input file(s)") { Arity = ArgumentArity.OneOrMore };
            public Option<string[]> Reference { get; } =
                new(new[] { "--reference", "-r" }, "Reference metadata from the specified assembly");
            public Option<string> SystemModule { get; } =
                new(new[] { "--system-module", "-s" }, "System module name (default: mscorlib)");
            public Option<bool> SanityChecks { get; } =
                new(new[] { "--sanity-checks", "-c" }, "Check for valid constructs that are likely mistakes");
            public Option<string[]> Include { get; } =
                new(new[] { "--include", "-i" }, "Use only methods/types/namespaces, which match the given regular expression(s)");
            public Option<FileInfo> IncludeFile { get; } =
                new Option<FileInfo>(new[] { "--include-file" }, "Same as --include, but the regular expression(s) are declared line by line in the specified file.").ExistingOnly();
            public Option<string[]> Exclude { get; } =
                new(new[] { "--exclude", "-e" }, "Skip methods/types/namespaces, which match the given regular expression(s)");
            public Option<FileInfo> ExcludeFile { get; } =
                new Option<FileInfo>(new[] { "--exclude-file" }, "Same as --exclude, but the regular expression(s) are declared line by line in the specified file.").ExistingOnly();
            public Option<string[]> IgnoreError { get; } =
                new(new[] { "--ignore-error", "-g" }, "Ignore errors, which match the given regular expression(s)");
            public Option<FileInfo> IgnoreErrorFile { get; } =
                new Option<FileInfo>(new[] { "--ignore-error-file" }, "Same as --ignore-error, but the regular expression(s) are declared line by line in the specified file.").ExistingOnly();
            public Option<bool> Statistics { get; } =
                new(new[] { "--statistics" }, "Print verification statistics");
            public Option<bool> Verbose { get; } =
                new(new[] { "--verbose", "-v" }, "Verbose output");
            public Option<bool> Tokens { get; } =
                new(new[] { "--tokens", "-t" }, "Include metadata tokens in error messages");

            public ILVerifyRootCommand()
                : base("Tool for verifying MSIL code based on ECMA-335.")
            {
                AddArgument(InputFilePath);
                AddOption(Reference);
                AddOption(SystemModule);
                AddOption(SanityChecks);
                AddOption(Include);
                AddOption(IncludeFile);
                AddOption(Exclude);
                AddOption(ExcludeFile);
                AddOption(IgnoreError);
                AddOption(IgnoreErrorFile);
                AddOption(Statistics);
                AddOption(Verbose);
                AddOption(Tokens);

                this.SetHandler<InvocationContext>((InvocationContext context) =>
                {
                    try
                    {
                        context.ExitCode = new Program(new Options(this, context.ParseResult)).Run();
                    }
                    catch (Exception e)
                    {
                        Console.ResetColor();
                        Console.ForegroundColor = ConsoleColor.Red;

                        // Omit the stacktrace from the error (different from the default System.CommandLine exception handler)
                        Console.Error.WriteLine("Error: " + e.Message);

                        Console.ResetColor();

                        context.ExitCode = 1;
                    }
                });
            }
        }

        private class Options
        {
            public Options(ILVerifyRootCommand cmd, ParseResult res)
            {
                InputFilePath = res.GetValueForArgument(cmd.InputFilePath);
                Reference = res.GetValueForOption(cmd.Reference);
                SystemModule = res.GetValueForOption(cmd.SystemModule);
                SanityChecks = res.GetValueForOption(cmd.SanityChecks);
                Include = res.GetValueForOption(cmd.Include);
                IncludeFile = res.GetValueForOption(cmd.IncludeFile);
                Exclude = res.GetValueForOption(cmd.Exclude);
                ExcludeFile = res.GetValueForOption(cmd.ExcludeFile);
                IgnoreError = res.GetValueForOption(cmd.IgnoreError);
                IgnoreErrorFile = res.GetValueForOption(cmd.IgnoreErrorFile);
                Statistics = res.GetValueForOption(cmd.Statistics);
                Verbose = res.GetValueForOption(cmd.Verbose);
                Tokens = res.GetValueForOption(cmd.Tokens);
            }

            public string[] InputFilePath { get; }
            public string[] Reference { get; }
            public string SystemModule { get; }
            public bool SanityChecks { get; }
            public string[] Include { get; }
            public FileInfo IncludeFile { get; }
            public string[] Exclude { get; }
            public FileInfo ExcludeFile { get; }
            public string[] IgnoreError { get; }
            public FileInfo IgnoreErrorFile { get; }
            public bool Statistics { get; }
            public bool Verbose { get; }
            public bool Tokens { get; }
        }

        private static int Main(string[] args) =>
            new CommandLineBuilder(new ILVerifyRootCommand())
                .UseVersionOption()
                .UseHelp()
                .UseParseErrorReporting()
                .Build()
                .Invoke(args);
    }
}
