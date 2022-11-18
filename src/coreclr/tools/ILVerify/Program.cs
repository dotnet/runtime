// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.RegularExpressions;
using Internal.TypeSystem.Ecma;

using static System.Console;

namespace ILVerify
{
    internal sealed class Program : IResolver
    {
        private readonly Dictionary<string, PEReader> _resolverCache = new();
        private readonly ILVerifyRootCommand _command;
        private readonly Dictionary<string, string> _inputFilePaths; // map of simple name to file path
        private readonly Dictionary<string, string> _referenceFilePaths; // map of simple name to file path
        private readonly Regex[] _includePatterns;
        private readonly Regex[] _excludePatterns;
        private readonly Regex[] _ignoreErrorPatterns;
        private readonly bool _verbose;

        private Verifier _verifier;

        public Program(ILVerifyRootCommand command)
        {
            _command = command;

            _inputFilePaths = Get(command.InputFilePath);
            _referenceFilePaths = Get(command.Reference);
            _verbose = Get(_command.Verbose);

            string[] includePatterns = Get(command.Include);
            FileInfo includeFile = Get(command.IncludeFile);
            if (includeFile != null)
            {
                if (includePatterns != null && includePatterns.Length != 0)
                    WriteLine("[Warning] --include-file takes precedence over --include");
                includePatterns = File.ReadAllLines(includeFile.FullName);
            }
            _includePatterns = StringPatternsToRegexArray(includePatterns);

            string[] excludePatterns = Get(command.Exclude);
            FileInfo excludeFile = Get(command.ExcludeFile);
            if (excludeFile != null)
            {
                if (excludePatterns != null && excludePatterns.Length != 0)
                    WriteLine("[Warning] --exclude-file takes precedence over --exclude");
                excludePatterns = File.ReadAllLines(excludeFile.FullName);
            }
            _excludePatterns = StringPatternsToRegexArray(excludePatterns);

            string[] ignoreErrorPatterns = Get(command.IgnoreError);
            FileInfo ignoreErrorFile = Get(command.IgnoreErrorFile);
            if (ignoreErrorFile != null)
            {
                if (ignoreErrorPatterns != null && ignoreErrorPatterns.Length != 0)
                    WriteLine("[Warning] --ignore-error-file takes precedence over --ignore-error");
                ignoreErrorPatterns = File.ReadAllLines(ignoreErrorFile.FullName);
            }
            _ignoreErrorPatterns = StringPatternsToRegexArray(ignoreErrorPatterns);

            if (_verbose)
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

            static Regex[] StringPatternsToRegexArray(string[] patterns)
            {
                if (patterns != null)
                {
                    var regexes = new Regex[patterns.Length];
                    for (var i = 0; i < patterns.Length; i++)
                    {
                        regexes[i] = new Regex(patterns[i], RegexOptions.Compiled);
                    }

                    return regexes;
                }
                return Array.Empty<Regex>();
            }
        }

        public int Run()
        {
            _verifier = new Verifier(this, new VerifierOptions
            {
                IncludeMetadataTokensInErrorMessages = Get(_command.Tokens),
                SanityChecks = Get(_command.SanityChecks)
            });
            _verifier.SetSystemModuleName(new AssemblyName(Get(_command.SystemModule) ?? "mscorlib"));

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

            if (Get(_command.Statistics))
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
                if (_verbose)
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
                            if (_verbose)
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
                if (_verbose)
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
                            if (_verbose)
                            {
                                Write("Ignoring ");
                                WriteLine(result.Message, result.Args);
                            }
                        }
                        else
                        {
                            WriteLine(result.Message, result.Args);
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
            if (_includePatterns.Length > 0 && !Array.Exists(_includePatterns, p => p.IsMatch(memberName)))
            {
                return false;
            }

            if (Array.Exists(_excludePatterns, p => p.IsMatch(memberName)))
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

            if (Array.Exists(_ignoreErrorPatterns, p => p.IsMatch(error)))
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

        private T Get<T>(Option<T> option) => _command.Result.GetValueForOption(option);
        private T Get<T>(Argument<T> argument) => _command.Result.GetValueForArgument(argument);

        private static int Main(string[] args) =>
            new CommandLineBuilder(new ILVerifyRootCommand())
                .UseTokenReplacer(Helpers.TryReadResponseFile)
                .UseVersionOption()
                .UseHelp()
                .UseParseErrorReporting()
                .Build()
                .Invoke(args);
    }
}
