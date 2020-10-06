// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
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
    class Program : ResolverBase
    {
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

        public class Options
        {
            public string[] InputFilePath { get; set; }
            public string[] Reference { get; set; }
            public string SystemModule { get; set; }
            public string[] Include { get; set; }
            public FileInfo IncludeFile { get; set; }
            public string[] Exclude { get; set; }
            public FileInfo ExcludeFile { get; set; }
            public string[] IgnoreError { get; set; }
            public FileInfo IgnoreErrorFile { get; set; }
            public bool Statistics { get; set; }
            public bool Verbose { get; set; }
            public bool Tokens { get; set; }
        }

        public static RootCommand RootCommand()
        {
            RootCommand command = new RootCommand();
            command.AddArgument(new Argument<string[]>("input-file-path", "Input file(s)") { Arity = new ArgumentArity(1, Int32.MaxValue) });
            command.AddOption(new Option<string[]>(new[] { "--reference", "-r" }, "Reference metadata from the specified assembly"));
            command.AddOption(new Option<string>(new[] { "--system-module", "-s" }, "System module name (default: mscorlib)"));
            command.AddOption(new Option<string[]>(new[] { "--include", "-i" }, "Use only methods/types/namespaces, which match the given regular expression(s)"));
            command.AddOption(new Option<FileInfo>(new[] { "--include-file" }, "Same as --include, but the regular expression(s) are declared line by line in the specified file.").ExistingOnly());
            command.AddOption(new Option<string[]>(new[] { "--exclude", "-e" }, "Skip methods/types/namespaces, which match the given regular expression(s)"));
            command.AddOption(new Option<FileInfo>(new[] { "--exclude-file" }, "Same as --exclude, but the regular expression(s) are declared line by line in the specified file.").ExistingOnly());
            command.AddOption(new Option<string[]>(new[] { "--ignore-error", "-g" }, "Ignore errors, which match the given regular expression(s)"));
            command.AddOption(new Option<FileInfo>(new[] { "--ignore-error-file" }, "Same as --ignore-error, but the regular expression(s) are declared line by line in the specified file.").ExistingOnly());
            command.AddOption(new Option<bool>(new[] { "--statistics" }, "Print verification statistics"));
            command.AddOption(new Option<bool>(new[] { "--verbose", "-v" }, "Verbose output"));
            command.AddOption(new Option<bool>(new[] { "--tokens", "-t" }, "Include metadata tokens in error messages"));
            return command;
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
                if (options.Include.Length != 0)
                    WriteLine("[Warning] --include-file takes precedence over --include");
                includePatterns = File.ReadAllLines(options.IncludeFile.FullName);
            }
            _includePatterns = StringPatternsToRegexList(includePatterns);

            string[] excludePatterns = options.Exclude;
            if (options.ExcludeFile != null)
            {
                if (options.Exclude.Length != 0)
                    WriteLine("[Warning] --exclude-file takes precedence over --exclude");
                excludePatterns = File.ReadAllLines(options.ExcludeFile.FullName);
            }
            _excludePatterns = StringPatternsToRegexList(excludePatterns);

            string[] ignoreErrorPatterns = options.IgnoreError;
            if (options.IgnoreErrorFile != null)
            {
                if (options.IgnoreError.Length != 0)
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
            _verifier = new Verifier(this, GetVerifierOptions());
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

        private VerifierOptions GetVerifierOptions()
        {
            return new VerifierOptions { IncludeMetadataTokensInErrorMessages = _options.Tokens };
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

        protected override PEReader ResolveCore(string simpleName)
        {
            string path = null;
            if (_inputFilePaths.TryGetValue(simpleName, out path) || _referenceFilePaths.TryGetValue(simpleName, out path))
            {
                return new PEReader(File.OpenRead(path));
            }

            return null;
        }

        private static int Run(Options options)
        {
            try
            {
                return new Program(options).Run();
            }
            catch (CommandLineException e)
            {
                Console.WriteLine("Error: " + e.Message);
                return 1;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.ToString());
                return 1;
            }
        }

        private static async Task<int> Main(string[] args)
        {
            var command = RootCommand();
            command.Handler = CommandHandler.Create<Options>(Run);
            return await command.InvokeAsync(args);
        }
    }
}
